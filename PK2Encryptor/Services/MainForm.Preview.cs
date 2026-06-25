using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PK2Encryptor;

public sealed partial class MainForm : Form
{
    #region Explorer preview, navigation and fast scanning

    private async Task PreviewFolderIfValidAsync(bool force = false)
    {
        if(_busy || CurrentWorkspacePage != _encryptorPage)
        {
            return;
        }

        var folder = _folderPathBox.Text.Trim();
        if(string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            if(CurrentModePage == _folderTab)
            {
                _previewList.Items.Clear();
                _summaryLabel.Text = T("Choose a valid folder.");
            }
            return;
        }

        if(force || !string.Equals(_previewRootFolder, folder, StringComparison.OrdinalIgnoreCase))
        {
            _previewRootFolder = folder;
            _previewCurrentFolder = folder;
            _scannedFolder = string.Empty;
            _scannedFiles = Array.Empty<string>();
        }

        await LoadFolderExplorerAsync(folder, string.IsNullOrWhiteSpace(_previewCurrentFolder) ? folder : _previewCurrentFolder);
    }


    private async Task LoadFolderExplorerAsync(string rootFolder, string currentFolder)
    {
        if(string.IsNullOrWhiteSpace(currentFolder) || !Directory.Exists(currentFolder))
        {
            currentFolder = rootFolder;
        }

        _statusLabel.Text = T("Ready.");
        _detailLabel.Text = "Folder preview uses Explorer mode: open folders instantly, then press Start to process the selected root.";
        _summaryLabel.Text = T("Opening folder view...");

        try
        {
            var includeHidden = _includeHiddenBox.Checked;
            var snapshot = await Task.Run(() => ReadFolderSnapshot(rootFolder, currentFolder, includeHidden));
            _previewCurrentFolder = currentFolder;
            PopulateFolderExplorerList(_previewList, snapshot, rootFolder, currentFolder);
            var relative = GetRelativeFolderCaption(rootFolder, currentFolder);
            _summaryLabel.Text = $"Folder view: {relative} - {snapshot.Directories.Count:N0} folder(s), {snapshot.Files.Count:N0} file(s).";
        }
        catch(Exception ex)
        {
            _previewList.Items.Clear();
            _statusLabel.Text = T("Folder view failed.");
            _detailLabel.Text = ex.Message;
            _summaryLabel.Text = T("Could not show this folder.");
        }
    }


    private async Task ScanFolderAsync(string folder)
    {
        await CollectFolderFilesForOperationAsync(folder);
        await LoadFolderExplorerAsync(folder, string.IsNullOrWhiteSpace(_previewCurrentFolder) ? folder : _previewCurrentFolder);
    }


    private async Task CollectFolderFilesForOperationAsync(string folder)
    {
        SetBusy(true, allowCancel: false);
        _statusLabel.Text = T("Preparing operation list...");
        _detailLabel.Text = folder;
        _summaryLabel.Text = T("Scanning files in background...");
        _progressBar.Style = ProgressBarStyle.Marquee;
        _progressBar.MarqueeAnimationSpeed = 22;

        try
        {
            var option = _recursiveBox.Checked ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var includeHidden = _includeHiddenBox.Checked;
            var files = await Task.Run(() => Directory.EnumerateFiles(folder, "*", option)
                .Where(path => ShouldProcessFile(path, includeHidden))
                .ToArray());

            _scannedFolder = folder;
            _scannedFiles = files;
            _statusLabel.Text = T("Ready.");
            _detailLabel.Text = T("Operation list prepared. Preview remains in Explorer mode so navigation stays instant.");
            _summaryLabel.Text = $"Prepared {files.Length:N0} file(s), {FormatBytes(files.Sum(GetSafeLength))}.";
        }
        catch(Exception ex)
        {
            InvalidateFolderScan();
            _statusLabel.Text = T("Scan failed.");
            _detailLabel.Text = ex.Message;
            _summaryLabel.Text = T("Could not prepare operation list.");
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.MarqueeAnimationSpeed = 0;
            _progressBar.Value = 0;
            SetBusy(false);
        }
    }


    private async Task PreviewPk2IfValidAsync(bool force = false)
    {
        if(_busy || CurrentWorkspacePage != _encryptorPage || CurrentModePage != _pk2Tab)
        {
            return;
        }

        var path = _pk2PathBox.Text.Trim();
        if(string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            _previewList.Items.Clear();
            InvalidatePk2Scan();
            _summaryLabel.Text = T("Choose a valid PK2 file.");
            _statusLabel.Text = T("Ready.");
            _detailLabel.Text = T("PK2 mode keeps the archive path and filename unchanged.");
            return;
        }

        if(!force && string.Equals(_scannedPk2, path, StringComparison.OrdinalIgnoreCase) && _scannedPk2Entries.Count > 0)
        {
            PopulatePk2ExplorerList(_previewList, _scannedPk2Entries, _previewCurrentPk2Folder);
            return;
        }

        await ScanPk2Async(path, force);
    }


    private async Task ScanPk2Async(string pk2File, bool force = false)
    {
        _previewList.Items.Clear();
        _previewCurrentPk2Folder = string.Empty;

        if(!force && TryLoadPk2EntryCache(pk2File, out var cachedEntries))
        {
            _scannedPk2 = pk2File;
            _scannedPk2Entries = cachedEntries;
            PopulatePk2ExplorerList(_previewList, cachedEntries, string.Empty);
            _summaryLabel.Text = $"PK2 cache loaded instantly: {Path.GetFileName(pk2File)} - {cachedEntries.Count:N0} internal file(s).";
            _statusLabel.Text = T("Ready.");
            _detailLabel.Text = T("Cached PK2 listing is being used. Use Browse again or refresh after rebuilding the archive.");
            return;
        }

        SetBusy(true, allowCancel: false);
        _progressBar.Style = ProgressBarStyle.Marquee;
        _progressBar.MarqueeAnimationSpeed = 24;
        _statusLabel.Text = T("Reading PK2 contents...");
        _detailLabel.Text = pk2File;
        _summaryLabel.Text = T("Loading internal PK2 file tree...");

        try
        {
            var entries = await ReadPk2EntriesViaBuilderAsync(pk2File);
            _scannedPk2 = pk2File;
            _scannedPk2Entries = entries;
            SavePk2EntryCache(pk2File, entries);
            PopulatePk2ExplorerList(_previewList, entries, string.Empty);

            var totalBytes = entries.Sum(entry => entry.Size);
            _summaryLabel.Text = $"PK2 selected: {Path.GetFileName(pk2File)} - {entries.Count:N0} internal file(s), {FormatBytes(totalBytes)}.";
            _statusLabel.Text = T("Ready.");
            _detailLabel.Text = entries.Count == 0
                ? "No internal files were listed from this PK2."
                : "Double-click folders to browse internal PK2 contents like Windows Explorer.";
        }
        catch(Exception ex)
        {
            InvalidatePk2Scan();
            _previewList.Items.Clear();
            _statusLabel.Text = T("Could not read PK2 contents.");
            _detailLabel.Text = ex.Message;
            _summaryLabel.Text = T("Choose a readable PK2 file.");
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.MarqueeAnimationSpeed = 0;
            _progressBar.Value = 0;
            SetBusy(false);
        }
    }


    private async Task PreviewExtractorPk2IfValidAsync(bool force = false)
    {
        if(_busy || CurrentWorkspacePage != _extractorPage)
        {
            return;
        }

        var path = _extractPk2PathBox.Text.Trim();
        if(string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            _extractPreviewList.Items.Clear();
            InvalidateExtractPk2Scan();
            _summaryLabel.Text = T("Choose a valid PK2 file for extraction.");
            _statusLabel.Text = T("Ready.");
            _detailLabel.Text = T("Extractor writes files under the selected output folder.");
            return;
        }

        if(!force && string.Equals(_scannedExtractPk2, path, StringComparison.OrdinalIgnoreCase) && _scannedExtractPk2Entries.Count > 0)
        {
            PopulatePk2ExplorerList(_extractPreviewList, _scannedExtractPk2Entries, _extractCurrentPk2Folder);
            return;
        }

        await ScanExtractorPk2Async(path, force);
    }


    private async Task ScanExtractorPk2Async(string pk2File, bool force = false)
    {
        _extractPreviewList.Items.Clear();
        _extractCurrentPk2Folder = string.Empty;

        if(!force && TryLoadPk2EntryCache(pk2File, out var cachedEntries))
        {
            _scannedExtractPk2 = pk2File;
            _scannedExtractPk2Entries = cachedEntries;
            PopulatePk2ExplorerList(_extractPreviewList, cachedEntries, string.Empty);
            _summaryLabel.Text = $"Extractor cache loaded instantly: {Path.GetFileName(pk2File)} - {cachedEntries.Count:N0} internal file(s).";
            _statusLabel.Text = T("Ready.");
            _detailLabel.Text = T("Double-click folders to browse; select files to extract or use Extract All.");
            return;
        }

        SetBusy(true, allowCancel: false);
        _progressBar.Style = ProgressBarStyle.Marquee;
        _progressBar.MarqueeAnimationSpeed = 24;
        _statusLabel.Text = T("Reading PK2 contents for extraction...");
        _detailLabel.Text = pk2File;
        _summaryLabel.Text = T("Loading internal PK2 file tree...");

        try
        {
            var entries = await ReadPk2EntriesViaBuilderAsync(pk2File);
            _scannedExtractPk2 = pk2File;
            _scannedExtractPk2Entries = entries;
            SavePk2EntryCache(pk2File, entries);
            PopulatePk2ExplorerList(_extractPreviewList, entries, string.Empty);

            var totalBytes = entries.Sum(entry => entry.Size);
            _summaryLabel.Text = $"Extractor ready: {Path.GetFileName(pk2File)} - {entries.Count:N0} internal file(s), {FormatBytes(totalBytes)}.";
            _statusLabel.Text = T("Ready.");
            _detailLabel.Text = entries.Count == 0
                ? "No internal files were listed. Make sure the selected archive is a real Silkroad PK2 and GFXFileManager.dll is rebuilt from this source."
                : "Double-click folders to browse; select files to extract or use Extract All.";
        }
        catch(Exception ex)
        {
            InvalidateExtractPk2Scan();
            _extractPreviewList.Items.Clear();
            _statusLabel.Text = T("Could not read PK2 contents.");
            _detailLabel.Text = ex.Message;
            _summaryLabel.Text = T("The selected PK2 could not be listed. Details are shown in Status.");
        }
        finally
        {
            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.MarqueeAnimationSpeed = 0;
            _progressBar.Value = 0;
            SetBusy(false);
        }
    }


    private static async Task<List<Pk2PreviewEntry>> ReadPk2EntriesViaBuilderAsync(string pk2File)
    {
        return await NativePk2Tools.ReadPk2EntriesAsync(pk2File, default);
    }


    private void PopulatePk2Preview(ListView list, IReadOnlyList<Pk2PreviewEntry> entries)
    {
        var currentFolder = ReferenceEquals(list, _extractPreviewList) ? _extractCurrentPk2Folder : _previewCurrentPk2Folder;
        PopulatePk2ExplorerList(list, entries, currentFolder);
    }


    private void PopulatePk2ExplorerList(ListView list, IReadOnlyList<Pk2PreviewEntry> entries, string currentFolder)
    {
        ConfigureExplorerColumns(list);
        currentFolder = NormalizeInternalFolder(currentFolder);
        list.BeginUpdate();
        try
        {
            list.Items.Clear();
            if(!string.IsNullOrWhiteSpace(currentFolder))
            {
                var up = new ListViewItem("..  Parent folder");
                up.SubItems.Add(string.Empty);
                up.SubItems.Add(T("Folder"));
                up.SubItems.Add(T("Up"));
                up.Tag = new ExplorerItemTag(ExplorerItemKind.Up, GetPk2ParentFolder(currentFolder));
                list.Items.Add(up);
            }

            var folders = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            var files = new List<Pk2PreviewEntry>();
            var prefix = string.IsNullOrWhiteSpace(currentFolder) ? string.Empty : currentFolder.TrimEnd('\\') + "\\";

            foreach(var entry in entries)
            {
                var normalized = NormalizeInternalFolder(entry.Path);
                if(!string.IsNullOrWhiteSpace(prefix))
                {
                    if(!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    normalized = normalized[prefix.Length..];
                }

                if(string.IsNullOrWhiteSpace(normalized))
                {
                    continue;
                }

                var slash = normalized.IndexOf('\\');
                if(slash >= 0)
                {
                    folders.Add(normalized[..slash]);
                }
                else
                {
                    files.Add(entry);
                }
            }

            foreach(var folder in folders)
            {
                var folderPath = string.IsNullOrWhiteSpace(currentFolder) ? folder : currentFolder.TrimEnd('\\') + "\\" + folder;
                var item = new ListViewItem("📁 " + folder);
                item.SubItems.Add(string.Empty);
                item.SubItems.Add(T("Folder"));
                item.SubItems.Add(T("Open"));
                item.Tag = new ExplorerItemTag(ExplorerItemKind.Pk2Directory, folderPath);
                list.Items.Add(item);
            }

            foreach(var entry in files.OrderBy(entry => Path.GetFileName(entry.Path), StringComparer.OrdinalIgnoreCase))
            {
                var name = Path.GetFileName(NormalizeInternalFolder(entry.Path));
                if(string.IsNullOrWhiteSpace(name))
                {
                    name = entry.Path;
                }
                var item = new ListViewItem("  " + name);
                item.SubItems.Add(FormatBytes(entry.Size));
                item.SubItems.Add(GetFileTypeCaption(name));
                item.SubItems.Add(T(entry.State));
                item.Tag = new ExplorerItemTag(ExplorerItemKind.Pk2File, entry.Path);
                list.Items.Add(item);
            }
        }
        finally
        {
            list.EndUpdate();
            FitListColumns(list);
        }
    }


    private void PopulateFolderExplorerList(ListView list, FolderSnapshot snapshot, string rootFolder, string currentFolder)
    {
        ConfigureExplorerColumns(list);
        list.BeginUpdate();
        try
        {
            list.Items.Clear();
            if(!PathsEqual(rootFolder, currentFolder))
            {
                var parent = Directory.GetParent(currentFolder)?.FullName ?? rootFolder;
                if(!parent.StartsWith(rootFolder, StringComparison.OrdinalIgnoreCase))
                {
                    parent = rootFolder;
                }
                var up = new ListViewItem("..  Parent folder");
                up.SubItems.Add(string.Empty);
                up.SubItems.Add(T("Folder"));
                up.SubItems.Add(T("Up"));
                up.Tag = new ExplorerItemTag(ExplorerItemKind.Up, parent);
                list.Items.Add(up);
            }

            foreach(var dir in snapshot.Directories)
            {
                var item = new ListViewItem("📁 " + dir.Name);
                item.SubItems.Add(string.Empty);
                item.SubItems.Add(T("Folder"));
                item.SubItems.Add(T("Open"));
                item.Tag = new ExplorerItemTag(ExplorerItemKind.LocalDirectory, dir.FullName);
                list.Items.Add(item);
            }

            foreach(var file in snapshot.Files)
            {
                var item = new ListViewItem("  " + file.Name);
                item.SubItems.Add(FormatBytes(file.Length));
                item.SubItems.Add(GetFileTypeCaption(file.Name));
                item.SubItems.Add(T("Ready"));
                item.Tag = new ExplorerItemTag(ExplorerItemKind.LocalFile, file.FullName);
                list.Items.Add(item);
            }
        }
        finally
        {
            list.EndUpdate();
            FitListColumns(list);
        }
    }


    private static FolderSnapshot ReadFolderSnapshot(string rootFolder, string currentFolder, bool includeHidden)
    {
        static bool IsVisible(FileSystemInfo info, bool includeHidden)
        {
            if(includeHidden)
            {
                return true;
            }
            return (info.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0;
        }

        var dir = new DirectoryInfo(currentFolder);
        var folders = dir.EnumerateDirectories()
            .Where(info => IsVisible(info, includeHidden))
            .OrderBy(info => info.Name, StringComparer.OrdinalIgnoreCase)
            .Take(2500)
            .ToList();
        var files = dir.EnumerateFiles()
            .Where(info => IsVisible(info, includeHidden))
            .OrderBy(info => info.Name, StringComparer.OrdinalIgnoreCase)
            .Take(5000)
            .ToList();
        return new FolderSnapshot(folders, files);
    }


    private sealed class FolderSnapshot
    {
        public FolderSnapshot(IReadOnlyList<DirectoryInfo> directories, IReadOnlyList<FileInfo> files)
        {
            Directories = directories;
            Files = files;
        }

        public IReadOnlyList<DirectoryInfo> Directories { get; }
        public IReadOnlyList<FileInfo> Files { get; }
    }


    private void HandlePreviewListDoubleClick()
    {
        if(_previewList.SelectedItems.Count == 0 || _previewList.SelectedItems[0].Tag is not ExplorerItemTag tag)
        {
            return;
        }

        _ = tag.Kind switch
        {
            ExplorerItemKind.LocalDirectory or ExplorerItemKind.Up when CurrentModePage == _folderTab => LoadFolderExplorerAsync(_previewRootFolder, tag.Path),
            ExplorerItemKind.Pk2Directory or ExplorerItemKind.Up when CurrentModePage == _pk2Tab => NavigatePk2PreviewAsync(_previewList, tag.Path, extractor: false),
            _ => Task.CompletedTask
        };
    }


    private void HandleExtractPreviewListDoubleClick()
    {
        if(_extractPreviewList.SelectedItems.Count == 0 || _extractPreviewList.SelectedItems[0].Tag is not ExplorerItemTag tag)
        {
            return;
        }

        _ = tag.Kind switch
        {
            ExplorerItemKind.Pk2Directory or ExplorerItemKind.Up => NavigatePk2PreviewAsync(_extractPreviewList, tag.Path, extractor: true),
            _ => Task.CompletedTask
        };
    }


    private Task NavigatePk2PreviewAsync(ListView list, string folder, bool extractor)
    {
        folder = NormalizeInternalFolder(folder);
        if(extractor)
        {
            _extractCurrentPk2Folder = folder;
            PopulatePk2ExplorerList(list, _scannedExtractPk2Entries, folder);
            _summaryLabel.Text = string.IsNullOrWhiteSpace(folder) ? "PK2 root folder." : "PK2 folder: " + folder;
        }
        else
        {
            _previewCurrentPk2Folder = folder;
            PopulatePk2ExplorerList(list, _scannedPk2Entries, folder);
            _summaryLabel.Text = string.IsNullOrWhiteSpace(folder) ? "PK2 root folder." : "PK2 folder: " + folder;
        }
        return Task.CompletedTask;
    }


    private static bool ShouldProcessFile(string path, bool includeHidden)
    {
        if(includeHidden)
        {
            return true;
        }

        try
        {
            var attributes = File.GetAttributes(path);
            return (attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0;
        }
        catch
        {
            return false;
        }
    }


    private void PopulatePreview(string rootFolder, IReadOnlyList<string> files)
    {
        ConfigureExplorerColumns(_previewList);
        _previewList.BeginUpdate();
        try
        {
            _previewList.Items.Clear();
            foreach(var file in files)
            {
                var name = Path.GetFileName(file);
                var item = new ListViewItem("  " + name);
                item.SubItems.Add(FormatBytes(GetSafeLength(file)));
                item.SubItems.Add(GetFileTypeCaption(name));
                item.SubItems.Add("Processing");
                item.Tag = file;
                _previewList.Items.Add(item);
            }
        }
        finally
        {
            _previewList.EndUpdate();
            FitListColumns(_previewList);
        }
    }


    private static string NormalizeInternalFolder(string value)
    {
        value = (value ?? string.Empty).Trim().Replace('/', '\\');
        while(value.StartsWith("\\", StringComparison.Ordinal)) value = value[1..];
        while(value.EndsWith("\\", StringComparison.Ordinal)) value = value[..^1];
        return value;
    }


    private static string GetPk2ParentFolder(string folder)
    {
        folder = NormalizeInternalFolder(folder);
        var slash = folder.LastIndexOf('\\');
        return slash <= 0 ? string.Empty : folder[..slash];
    }


    private static string GetRelativeFolderCaption(string root, string current)
    {
        try
        {
            if(PathsEqual(root, current))
            {
                return "Root";
            }
            return Path.GetRelativePath(root, current);
        }
        catch
        {
            return current;
        }
    }


    private static bool PathsEqual(string first, string second)
    {
        try
        {
            first = Path.GetFullPath(first).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            second = Path.GetFullPath(second).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch
        {
        }
        return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
    }


    private string GetFileTypeCaption(string name)
    {
        var ext = Path.GetExtension(name);
        return string.IsNullOrWhiteSpace(ext) ? T("File") : ext.TrimStart('.').ToUpperInvariant() + " " + T("File");
    }


    private static string GetPk2CachePath(string pk2File)
    {
        var info = new FileInfo(pk2File);
        var identity = pk2File + "|" + info.Length + "|" + info.LastWriteTimeUtc.Ticks;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
        var cacheRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PK2Tools", "cache");
        Directory.CreateDirectory(cacheRoot);
        return Path.Combine(cacheRoot, hash + ".tsv");
    }


    private static bool TryLoadPk2EntryCache(string pk2File, out List<Pk2PreviewEntry> entries)
    {
        entries = new List<Pk2PreviewEntry>();
        try
        {
            var cache = GetPk2CachePath(pk2File);
            if(!File.Exists(cache))
            {
                return false;
            }

            foreach(var line in File.ReadLines(cache, Encoding.UTF8))
            {
                var parts = line.Split('\t');
                if(parts.Length < 3)
                {
                    continue;
                }
                long.TryParse(parts[1], out var size);
                entries.Add(new Pk2PreviewEntry(parts[0], Math.Max(0, size), parts[2]));
            }
            return entries.Count > 0;
        }
        catch
        {
            entries = new List<Pk2PreviewEntry>();
            return false;
        }
    }


    private static void SavePk2EntryCache(string pk2File, IReadOnlyList<Pk2PreviewEntry> entries)
    {
        try
        {
            var cache = GetPk2CachePath(pk2File);
            var lines = entries.Select(entry => string.Join("\t", entry.Path.Replace('\t', ' '), entry.Size.ToString(), entry.State.Replace('\t', ' ')));
            File.WriteAllLines(cache, lines, new UTF8Encoding(false));
        }
        catch
        {
        }
    }


    #endregion
}
