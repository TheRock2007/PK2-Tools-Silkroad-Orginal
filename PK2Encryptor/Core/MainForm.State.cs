using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PK2Encryptor;

public sealed partial class MainForm : Form
{
    #region State and helpers

    private static string MakeDisplayPath(string root, string file)
    {
        try
        {
            if(!string.IsNullOrWhiteSpace(root))
            {
                return Path.GetRelativePath(root, file);
            }
        }
        catch
        {
        }
        return Path.GetFileName(file);
    }


    private void UpdateProgress(long processedBytes, long totalBytes)
    {
        if(totalBytes <= 0)
        {
            _progressBar.Value = 0;
            return;
        }

        var scaled = (int)Math.Clamp((processedBytes * (long)_progressBar.Maximum) / totalBytes, 0, _progressBar.Maximum);
        _progressBar.Value = scaled;
        _summaryLabel.Text = $"{FormatBytes(Math.Min(processedBytes, totalBytes))} / {FormatBytes(totalBytes)}";
    }


    private string[] GetSelectedPk2Paths(ListView list)
    {
        var selected = new List<string>();
        var sourceEntries = ReferenceEquals(list, _extractPreviewList)
            ? _scannedExtractPk2Entries
            : _scannedPk2Entries;

        foreach(var item in list.SelectedItems.Cast<ListViewItem>())
        {
            switch(item.Tag)
            {
                case string path when !string.IsNullOrWhiteSpace(path):
                    selected.Add(path);
                    break;

                case ExplorerItemTag { Kind: ExplorerItemKind.Pk2File } tag when !string.IsNullOrWhiteSpace(tag.Path):
                    selected.Add(tag.Path);
                    break;

                case ExplorerItemTag { Kind: ExplorerItemKind.Pk2Directory } tag when !string.IsNullOrWhiteSpace(tag.Path):
                    var folderPrefix = NormalizeInternalFolder(tag.Path).TrimEnd('\\') + "\\";
                    selected.AddRange(sourceEntries
                        .Select(entry => NormalizeInternalFolder(entry.Path))
                        .Where(path => path.StartsWith(folderPrefix, StringComparison.OrdinalIgnoreCase)));
                    break;
            }
        }

        return selected
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }


    private void EnsureDefaultExtractOutputFolder()
    {
        var pk2File = _extractPk2PathBox.Text.Trim();
        if(string.IsNullOrWhiteSpace(pk2File))
        {
            return;
        }

        var baseFolder = Path.GetDirectoryName(pk2File);
        if(string.IsNullOrWhiteSpace(baseFolder))
        {
            return;
        }

        var name = Path.GetFileNameWithoutExtension(pk2File);
        if(string.IsNullOrWhiteSpace(name))
        {
            name = "pk2";
        }
        _extractOutputFolderBox.Text = Path.Combine(baseFolder, name);
    }


    private static string NormalizeInternalFolderForDisplay(string value)
    {
        value = value.Trim().Replace('/', '\\');
        while(value.StartsWith("\\", StringComparison.Ordinal))
        {
            value = value[1..];
        }
        while(value.EndsWith("\\", StringComparison.Ordinal))
        {
            value = value[..^1];
        }
        return value;
    }


    private static void SetItemStatus(ListViewItem item, string status)
    {
        while(item.SubItems.Count < 4)
        {
            item.SubItems.Add(string.Empty);
        }
        item.SubItems[3].Text = status;
    }


    private void RefreshNativeProgressUi(bool force = false)
    {
        var now = Environment.TickCount64;
        if(!force && now - _lastNativeUiRefreshTicks < 80)
        {
            return;
        }

        _lastNativeUiRefreshTicks = now;
        _progressBar.Invalidate();
        _progressBar.Update();
        _statusLabel.Invalidate();
        _statusLabel.Update();
        _detailLabel.Invalidate();
        _detailLabel.Update();
        _summaryLabel.Invalidate();
        _summaryLabel.Update();
    }


    private void SetBuilderJobStatus(BuildPk2Job job, string status)
    {
        foreach(ListViewItem item in _builderQueueList.Items)
        {
            if(ReferenceEquals(item.Tag, job) || (item.Tag is BuildPk2Job other && string.Equals(other.Name, job.Name, StringComparison.OrdinalIgnoreCase)))
            {
                while(item.SubItems.Count < 4)
                {
                    item.SubItems.Add(string.Empty);
                }
                item.SubItems[3].Text = TryResolveTranslationKey(status, out var statusKey) ? T(statusKey) : status;
                item.EnsureVisible();
                _builderQueueList.Refresh();
                return;
            }
        }
    }


    private void SetBusy(bool busy, bool allowCancel = true)
    {
        _busy = busy;
        UseWaitCursor = busy;
        _cancelButton.Enabled = busy && allowCancel;
        RefreshActionState();
    }


    private void RefreshActionState()
    {
        var folderSelected = Directory.Exists(_folderPathBox.Text.Trim());
        var pk2Selected = File.Exists(_pk2PathBox.Text.Trim());
        var isFolderMode = CurrentModePage == _folderTab;

        // Keep page containers enabled while a native operation is running.
        // Disabling entire custom-painted panels can make WinForms repaint them
        // as broken grey/white blocks during long PK2 jobs.
        _workspaceHost.Enabled = true;
        _modeHost.Enabled = true;
        _folderPathBox.Enabled = !_busy;
        _browseFolderButton.Enabled = !_busy;
        _recursiveBox.Enabled = !_busy;
        _includeHiddenBox.Enabled = !_busy;

        _pk2PathBox.Enabled = !_busy;
        _browsePk2Button.Enabled = !_busy;

        _operationBox.Enabled = !_busy;
        var canStartPk2 = pk2Selected;
        _startButton.Enabled = !_busy && ((isFolderMode && folderSelected) || (!isFolderMode && canStartPk2));

        _extractPk2PathBox.Enabled = !_busy;
        _browseExtractPk2Button.Enabled = !_busy;
        _extractOutputFolderBox.Enabled = !_busy;
        _browseExtractOutputFolderButton.Enabled = !_busy;
        _extractPayloadModeBox.Enabled = !_busy;
        _extractPreviewList.Enabled = true;
        var extractPk2Selected = File.Exists(_extractPk2PathBox.Text.Trim());
        var extractOutputSelected = !string.IsNullOrWhiteSpace(_extractOutputFolderBox.Text.Trim());
        _extractSelectedButton.Enabled = !_busy && extractPk2Selected && extractOutputSelected && GetSelectedPk2Paths(_extractPreviewList).Length > 0;
        _extractAllButton.Enabled = !_busy && extractPk2Selected && extractOutputSelected;

        _importPk2PathBox.Enabled = !_busy;
        _browseImportPk2Button.Enabled = !_busy;
        _importSourcePathBox.Enabled = !_busy;
        _browseImportFileButton.Enabled = false;
        _browseImportFolderButton.Enabled = !_busy;
        _importInternalPathBox.Enabled = false;
        _importPayloadModeBox.Enabled = !_busy;
        var importPk2Selected = File.Exists(_importPk2PathBox.Text.Trim());
        _importFileButton.Enabled = false;
        _importFolderButton.Enabled = !_busy && importPk2Selected && Directory.Exists(_importSourcePathBox.Text.Trim());

        _builderSourceFolderBox.Enabled = !_busy;
        _browseBuilderSourceButton.Enabled = !_busy;
        _builderOutputFolderBox.Enabled = !_busy;
        _browseBuilderOutputButton.Enabled = !_busy;
        _builderQueueList.Enabled = true;
        _builderEncryptEntriesBox.Enabled = !_busy;
        _builderEncryptPayloadsBox.Enabled = !_busy;
        _builderRefreshButton.Enabled = !_busy;
        _builderBuildButton.Enabled = !_busy && GetSelectedBuilderJobs().Length > 0;

        if(!_busy)
        {
            _cancelButton.Enabled = false;
        }
    }


    private static readonly string[] KnownPk2BuildFolders =
    {
        "Data", "Map", "Media", "Music", "Particles"
    };


    private void EnsureDefaultBuilderOutputFolder()
    {
        var source = _builderSourceFolderBox.Text.Trim();
        if(string.IsNullOrWhiteSpace(source) || !Directory.Exists(source))
        {
            return;
        }

        if(!string.IsNullOrWhiteSpace(_builderOutputFolderBox.Text.Trim()))
        {
            return;
        }

        _builderOutputFolderBox.Text = source;
    }


    private void PopulateBuilderQueue()
    {
        _builderQueueList.BeginUpdate();
        try
        {
            _builderQueueList.Items.Clear();
            var sourceText = _builderSourceFolderBox.Text.Trim();
            var outputText = _builderOutputFolderBox.Text.Trim();
            if(string.IsNullOrWhiteSpace(sourceText) || string.IsNullOrWhiteSpace(outputText))
            {
                return;
            }

            var sourceRoot = new DirectoryInfo(sourceText);
            var outputRoot = new DirectoryInfo(outputText);
            var sourceName = sourceRoot.Name;
            var sourceIsSingleKnownFolder = KnownPk2BuildFolders.Contains(sourceName, StringComparer.OrdinalIgnoreCase);

            foreach(var name in KnownPk2BuildFolders)
            {
                var sourceFolder = sourceIsSingleKnownFolder && sourceName.Equals(name, StringComparison.OrdinalIgnoreCase)
                    ? sourceRoot.FullName
                    : Path.Combine(sourceRoot.FullName, name);
                var outputFile = Path.Combine(outputRoot.FullName, name + ".pk2");
                var exists = Directory.Exists(sourceFolder);
                if(sourceIsSingleKnownFolder && !sourceName.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var item = new ListViewItem(name);
                item.SubItems.Add(sourceFolder);
                item.SubItems.Add(outputFile);
                item.SubItems.Add(exists ? T("Ready") : T("Missing folder"));
                item.Checked = exists;
                item.Tag = new BuildPk2Job(name, sourceFolder, outputFile, exists);
                _builderQueueList.Items.Add(item);
            }
        }
        finally
        {
            _builderQueueList.EndUpdate();
            FitBuilderColumns();
            RefreshActionState();
        }
    }


    private BuildPk2Job[] GetSelectedBuilderJobs()
    {
        return _builderQueueList.Items
            .Cast<ListViewItem>()
            .Where(item => item.Checked)
            .Select(item => item.Tag as BuildPk2Job)
            .Where(job => job is not null && job.Exists)
            .Cast<BuildPk2Job>()
            .ToArray();
    }




    private void OpenSelectedBuilderSourceFolder()
    {
        if(_builderQueueList.SelectedItems.Count == 0 || _builderQueueList.SelectedItems[0].Tag is not BuildPk2Job job)
        {
            return;
        }

        OpenPathInExplorer(job.SourceFolder);
    }


    private static void OpenPathInExplorer(string path)
    {
        try
        {
            if(string.IsNullOrWhiteSpace(path) || (!Directory.Exists(path) && !File.Exists(path)))
            {
                return;
            }

            var info = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            };
            Process.Start(info);
        }
        catch
        {
        }
    }


    private void InvalidatePk2Scan()
    {
        _scannedPk2 = string.Empty;
        _previewCurrentPk2Folder = string.Empty;
        _scannedPk2Entries = new List<Pk2PreviewEntry>();
    }


    private void InvalidateExtractPk2Scan()
    {
        _scannedExtractPk2 = string.Empty;
        _extractCurrentPk2Folder = string.Empty;
        _scannedExtractPk2Entries = new List<Pk2PreviewEntry>();
    }


    private void InvalidateFolderScan()
    {
        _scannedFolder = string.Empty;
        _scannedFiles = Array.Empty<string>();
        _previewRootFolder = string.Empty;
        _previewCurrentFolder = string.Empty;
    }


    private static long GetSafeLength(string path)
    {
        try
        {
            return new FileInfo(path).Length;
        }
        catch
        {
            return 0;
        }
    }


    private static string FormatBytes(long value)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = value;
        var unit = 0;
        while(size >= 1024 && unit + 1 < units.Length)
        {
            size /= 1024;
            unit++;
        }
        return unit == 0 ? $"{value:N0} {units[unit]}" : $"{size:N2} {units[unit]}";
    }


    private static string QuoteArg(string value)
    {
        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }


    private static void TryDelete(string path)
    {
        try
        {
            if(File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }


    #endregion
}
