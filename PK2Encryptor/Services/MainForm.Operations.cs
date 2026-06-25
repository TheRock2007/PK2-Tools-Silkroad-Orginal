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
    #region Operations

    private async Task StartOperationAsync()
    {
        if(_busy)
        {
            return;
        }

        var encrypt = _operationBox.SelectedIndex <= 0;
        var isFolderMode = CurrentModePage == _folderTab;

        if(isFolderMode)
        {
            var folder = _folderPathBox.Text.Trim();
            if(string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                MessageBox.Show(this, T("Please select a valid folder."), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if(_scannedFiles.Length == 0 || !string.Equals(_scannedFolder, folder, StringComparison.OrdinalIgnoreCase))
            {
                await ScanFolderAsync(folder);
            }

            var files = _scannedFiles.Where(File.Exists).ToArray();
            if(files.Length == 0)
            {
                MessageBox.Show(this, T("No files were found in the selected folder."), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PopulatePreview(folder, files);
            var operation = encrypt ? T("Encrypt") : T("Decrypt");
            var confirm = TF("This will {0} {1:N0} file(s) in the selected folder using the same names and paths. No backup files will be created. Continue?", operation, files.Length);
            if(MessageBox.Show(this, confirm, Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
            {
                return;
            }

            await RunFolderOperationAsync(files, folder, encrypt);
        }
        else
        {
            var pk2File = _pk2PathBox.Text.Trim();
            if(string.IsNullOrWhiteSpace(pk2File) || !File.Exists(pk2File))
            {
                MessageBox.Show(this, T("Please select a valid PK2 file."), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if(!pk2File.EndsWith(".pk2", StringComparison.OrdinalIgnoreCase))
            {
                var answer = MessageBox.Show(this, T("The selected file is not .pk2. Continue anyway?"), Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                if(answer != DialogResult.OK)
                {
                    return;
                }
            }

            var verb = encrypt ? T("Encrypt") : T("Decrypt");
            var confirm = TF("This will {0} all internal file payloads inside the selected PK2 in-place. Encrypted payloads require the matching GFXFileManager.dll. The PK2 filename and path will not change. Continue?", verb);
            if(MessageBox.Show(this, confirm, Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
            {
                return;
            }

            await RunPk2OperationAsync(pk2File, encrypt);
        }
    }


    private async Task StartImportAsync(bool importFolder)
    {
        if(_busy)
        {
            return;
        }

        // Import is intentionally folder-only in the GUI.
        // The folder contents are injected into the selected client PK2 root.
        importFolder = true;

        var pk2File = _importPk2PathBox.Text.Trim();
        if(string.IsNullOrWhiteSpace(pk2File) || !File.Exists(pk2File))
        {
            MessageBox.Show(this, T("Please select a valid target PK2 file."), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if(!pk2File.EndsWith(".pk2", StringComparison.OrdinalIgnoreCase))
        {
            var answer = MessageBox.Show(this, T("The selected target file is not .pk2. Continue anyway?"), Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if(answer != DialogResult.OK)
            {
                return;
            }
        }

        var source = _importSourcePathBox.Text.Trim();
        if(string.IsNullOrWhiteSpace(source) || !Directory.Exists(source))
        {
            MessageBox.Show(this, T("Please select a valid import source folder."), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var encryptedFiles = false;
        var mode = encryptedFiles ? T("encrypted/GFXFileManager-readable") : T("plain");

        // Import must inject the selected folder CONTENTS directly into the PK2 root.
        // Example: selecting a local folder named "Media" for Media.pk2 should import
        // its files and subfolders into the archive root, not create an extra "Media"
        // folder inside the PK2. If the user wants a top-level folder to be created,
        // they can select the parent folder that contains that folder.
        var internalFolder = string.Empty;
        var confirm = TF("This will inject the selected folder contents directly into the client PK2 root. The source folder name itself will not be created inside the PK2. Subfolder structure and all PK2 payloads will be stored as {0} data. Continue?", mode);
        if(MessageBox.Show(this, confirm, Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
        {
            return;
        }

        await RunPk2ImportAsync(pk2File, source, importFolder, internalFolder, encryptedFiles);
    }


    private async Task StartExtractAsync(bool extractAll)
    {
        if(_busy)
        {
            return;
        }

        var pk2File = _extractPk2PathBox.Text.Trim();
        if(string.IsNullOrWhiteSpace(pk2File) || !File.Exists(pk2File))
        {
            MessageBox.Show(this, T("Please select a valid PK2 file to extract."), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var outputFolder = _extractOutputFolderBox.Text.Trim();
        if(string.IsNullOrWhiteSpace(outputFolder))
        {
            MessageBox.Show(this, T("Please select an output folder."), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string[]? selectedPaths = null;
        if(!extractAll)
        {
            selectedPaths = GetSelectedPk2Paths(_extractPreviewList);
            if(selectedPaths.Length == 0)
            {
                MessageBox.Show(this, T("Please select one or more PK2 files from the extractor list."), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        var encryptedFiles = _extractPayloadModeBox.SelectedIndex == 1;
        var mode = encryptedFiles ? T("raw stored") : T("restored/plain");
        var countText = selectedPaths is null ? T("all listed PK2 files") : TF("{0:N0} selected PK2 file(s)", selectedPaths.Length);
        var confirm = TF("This will extract {0} to the output folder and keep the internal PK2 folder layout. Output files will be {1}. Continue?", countText, mode);
        if(MessageBox.Show(this, confirm, Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
        {
            return;
        }

        await RunPk2ExtractAsync(pk2File, outputFolder, selectedPaths, encryptedFiles);
    }


    private async Task StartBuilderPk2Async()
    {
        if(_busy)
        {
            return;
        }

        var jobs = GetSelectedBuilderJobs();
        if(jobs.Length == 0)
        {
            MessageBox.Show(this, T("Select at least one source folder to build."), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var missing = jobs.FirstOrDefault(job => !Directory.Exists(job.SourceFolder));
        if(missing is not null)
        {
            MessageBox.Show(this, T("Missing source folder: ") + missing.SourceFolder, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var encryptEntries = false;
        var encryptPayloads = false;
        var payloadMode = T("plain internal file payloads");
        var headerMode = T("standard readable header + plain directory entries");
        var confirm = TF("This will build {0:N0} PK2 archive(s) with {1} and {2}. Continue?", jobs.Length, headerMode, payloadMode);
        if(MessageBox.Show(this, confirm, Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
        {
            return;
        }

        await RunBuilderPk2Async(jobs, encryptEntries, encryptPayloads);
    }


    private async Task RunFolderOperationAsync(IReadOnlyList<string> files, string rootForPreview, bool encrypt)
    {
        SetBusy(true, allowCancel: true);
        _cancelSource = new CancellationTokenSource();
        var token = _cancelSource.Token;
        var totalBytes = Math.Max(1, files.Sum(GetSafeLength));
        var processedBytes = 0L;
        var processed = 0;
        var skipped = 0;
        var failed = 0;
        var operationName = encrypt ? T("Encrypt") : T("Decrypt");
        _progressBar.Style = ProgressBarStyle.Continuous;
        _progressBar.Value = 0;
        _statusLabel.Text = TF("Starting {0}...", operationName);
        _detailLabel.Text = T("Preparing selected files...");

        var itemByPath = _previewList.Items
            .Cast<ListViewItem>()
            .Where(item => item.Tag is string)
            .GroupBy(item => (string)item.Tag!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        try
        {
            for(var index = 0; index < files.Count; ++index)
            {
                token.ThrowIfCancellationRequested();
                var file = files[index];
                if(!itemByPath.TryGetValue(file, out var item))
                {
                    var name = Path.GetFileName(file);
                    item = new ListViewItem("  " + name);
                    item.SubItems.Add(FormatBytes(GetSafeLength(file)));
                    item.SubItems.Add(GetFileTypeCaption(name));
                    item.SubItems.Add(T("Queued"));
                    item.Tag = file;
                    _previewList.Items.Add(item);
                }

                SetItemStatus(item, T("Processing"));
                item.EnsureVisible();
                _statusLabel.Text = $"{operationName}: {index + 1:N0} / {files.Count:N0}";
                _detailLabel.Text = MakeDisplayPath(rootForPreview, file);

                var fileBytesDone = 0L;
                var progress = new Progress<long>(currentFileBytesDone =>
                {
                    processedBytes += Math.Max(0, currentFileBytesDone - fileBytesDone);
                    fileBytesDone = currentFileBytesDone;
                    UpdateProgress(processedBytes, totalBytes);
                });

                try
                {
                    CryptoFileResult result;
                    if(encrypt)
                    {
                        result = await Task.Run(() => LoosePayloadCrypto.EncryptFileInPlace(file, progress), token);
                    }
                    else
                    {
                        result = await Task.Run(() => LoosePayloadCrypto.DecryptFileInPlace(file, progress), token);
                    }

                    processedBytes += Math.Max(0, GetSafeLength(file) - fileBytesDone);
                    UpdateProgress(processedBytes, totalBytes);

                    switch(result)
                    {
                        case CryptoFileResult.Processed:
                            processed++;
                            SetItemStatus(item, operationName + " " + T("done"));
                            break;
                        case CryptoFileResult.AlreadyEncrypted:
                            skipped++;
                            SetItemStatus(item, T("Already encrypted"));
                            break;
                        case CryptoFileResult.NotEncrypted:
                            skipped++;
                            SetItemStatus(item, T("Not encrypted"));
                            break;
                    }
                }
                catch(OperationCanceledException)
                {
                    SetItemStatus(item, T("Cancelled"));
                    throw;
                }
                catch(Exception ex)
                {
                    failed++;
                    SetItemStatus(item, T("Failed"));
                    _detailLabel.Text = ex.Message;
                }
            }

            _progressBar.Value = _progressBar.Maximum;
            var message = failed == 0
                ? $"Finished. {processed:N0} file(s) processed, {skipped:N0} skipped."
                : $"Finished with errors. {processed:N0} processed, {skipped:N0} skipped, {failed:N0} failed.";
            _statusLabel.Text = message;
            _detailLabel.Text = failed == 0 ? T("Operation completed successfully.") : T("Some files failed. Check the Status column.");
            MessageBox.Show(this, message, Text, MessageBoxButtons.OK, failed == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        catch(OperationCanceledException)
        {
            _statusLabel.Text = $"Cancelled. {processed:N0} file(s) completed before cancellation.";
            _detailLabel.Text = T("The current file is never cancelled in the middle of writing to avoid partial encryption.");
            MessageBox.Show(this, _statusLabel.Text, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            _cancelSource?.Dispose();
            _cancelSource = null;
            SetBusy(false);
            PopulatePreview(rootForPreview, files.Where(File.Exists).ToArray());
        }
    }


    private async Task RunBuilderPk2Async(IReadOnlyList<BuildPk2Job> jobs, bool encryptEntries, bool encryptPayloads)
    {
        SetBusy(true, allowCancel: true);
        _cancelSource = new CancellationTokenSource();
        var token = _cancelSource.Token;
        _progressBar.Style = ProgressBarStyle.Continuous;
        _progressBar.Value = 0;
        _statusLabel.Text = T("Building selected PK2 archives...");
        _detailLabel.Text = T("Starting builder queue.");
        _summaryLabel.Text = "0%";

        var completed = 0;
        BuildPk2Job? activeJob = null;
        try
        {
            foreach(var job in jobs)
            {
                token.ThrowIfCancellationRequested();
                activeJob = job;
                SetBuilderJobStatus(job, T("Starting"));
                Directory.CreateDirectory(Path.GetDirectoryName(job.OutputFile) ?? _builderOutputFolderBox.Text.Trim());
                _statusLabel.Text = $"Building {job.Name}.pk2 ({completed + 1:N0}/{jobs.Count:N0})...";
                _detailLabel.Text = job.SourceFolder;
                RefreshNativeProgressUi(force: true);

                var jobIndex = completed;
                var progress = new Progress<NativePk2Progress>(update =>
                {
                    var scaled = ((jobIndex * _progressBar.Maximum) + update.Percent) / Math.Max(1, jobs.Count);
                    _progressBar.Value = Math.Clamp(scaled, 0, _progressBar.Maximum);
                    _statusLabel.Text = $"Building {job.Name}.pk2 ({jobIndex + 1:N0}/{jobs.Count:N0})";
                    _detailLabel.Text = string.IsNullOrWhiteSpace(update.Status) ? job.SourceFolder : update.Status;
                    _summaryLabel.Text = $"{(_progressBar.Value / 10.0):N1}%";
                    SetBuilderJobStatus(job, update.Percent >= 1000 ? "Finalizing" : "Building");
                    RefreshNativeProgressUi(update.Percent == 0 || update.Percent >= 1000);
                });

                await NativePk2Tools.BuildPk2Async(job.SourceFolder, job.OutputFile, encryptEntries, encryptPayloads, progress, token);
                completed++;
                SetBuilderJobStatus(job, T("Done"));
                UpdateProgress(completed, jobs.Count);
                RefreshNativeProgressUi(force: true);
            }

            _progressBar.Value = _progressBar.Maximum;
            _summaryLabel.Text = "100%";
            _statusLabel.Text = T("Builder PK2 completed.");
            _detailLabel.Text = "Selected PK2 archives were created with plain internal payloads.";
            MessageBox.Show(this, T("Builder PK2 completed successfully."), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch(OperationCanceledException)
        {
            if(activeJob is not null)
            {
                SetBuilderJobStatus(activeJob, "Cancelled");
            }
            _statusLabel.Text = T("Builder PK2 cancelled.");
            _detailLabel.Text = T("The active build stopped before the full queue completed.");
            MessageBox.Show(this, _statusLabel.Text, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch(Exception ex)
        {
            if(activeJob is not null)
            {
                SetBuilderJobStatus(activeJob, "Failed");
            }
            _progressBar.Value = 0;
            _summaryLabel.Text = T("Build failed.");
            _statusLabel.Text = T("Builder PK2 failed.");
            _detailLabel.Text = ex.Message;
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _cancelSource?.Dispose();
            _cancelSource = null;
            SetBusy(false);
        }
    }


    private async Task RunPk2ImportAsync(string pk2File, string sourcePath, bool importFolder, string internalFolder, bool encryptedFiles)
    {
        SetBusy(true, allowCancel: true);
        _cancelSource = new CancellationTokenSource();
        var token = _cancelSource.Token;
        _progressBar.Style = ProgressBarStyle.Continuous;
        _progressBar.Value = 0;
        _progressBar.MarqueeAnimationSpeed = 0;
        _statusLabel.Text = T("Importing folder into PK2...");
        _detailLabel.Text = sourcePath;
        _summaryLabel.Text = "0%";

        try
        {
            var progress = new Progress<NativePk2Progress>(update =>
            {
                _progressBar.Value = Math.Clamp(update.Percent, 0, _progressBar.Maximum);
                _statusLabel.Text = T("Importing folder into PK2...");
                _detailLabel.Text = string.IsNullOrWhiteSpace(update.Status) ? sourcePath : update.Status;
                _summaryLabel.Text = $"{(_progressBar.Value / 10.0):N1}%";
                RefreshNativeProgressUi(update.Percent == 0 || update.Percent >= 1000);
            });

            await NativePk2Tools.ImportFolderAsync(pk2File, sourcePath, internalFolder, encryptedFiles, progress, token);

            _progressBar.Value = _progressBar.Maximum;
            _summaryLabel.Text = "100%";
            var message = "Folder import completed.";
            _statusLabel.Text = message;
            _detailLabel.Text = "The selected PK2 file was updated in-place with plain internal payloads.";
            MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            InvalidatePk2Scan();
            InvalidateExtractPk2Scan();
            SetBusy(false);
            if(CurrentWorkspacePage == _extractorPage)
            {
                await PreviewExtractorPk2IfValidAsync(force: true);
            }
            else
            {
                await PreviewPk2IfValidAsync(force: true);
            }
        }
        catch(OperationCanceledException)
        {
            _statusLabel.Text = T("PK2 import cancelled.");
            _detailLabel.Text = T("The import was stopped before completion.");
            MessageBox.Show(this, _statusLabel.Text, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch(Exception ex)
        {
            _progressBar.Value = 0;
            _summaryLabel.Text = T("Import failed.");
            _statusLabel.Text = T("PK2 import failed.");
            _detailLabel.Text = ex.Message;
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _cancelSource?.Dispose();
            _cancelSource = null;
            SetBusy(false);
        }
    }


    private async Task RunPk2ExtractAsync(string pk2File, string outputFolder, IReadOnlyList<string>? selectedPaths, bool encryptedFiles)
    {
        SetBusy(true, allowCancel: true);
        _cancelSource = new CancellationTokenSource();
        var token = _cancelSource.Token;
        _progressBar.Style = ProgressBarStyle.Continuous;
        _progressBar.Value = 0;
        _progressBar.MarqueeAnimationSpeed = 0;
        _statusLabel.Text = selectedPaths is null ? "Extracting all PK2 files..." : "Extracting selected PK2 files...";
        _detailLabel.Text = outputFolder;
        _summaryLabel.Text = "0%";

        var selectionFile = selectedPaths is null ? null : Path.Combine(Path.GetTempPath(), "pk2tools_selection_" + Guid.NewGuid().ToString("N") + ".txt");
        try
        {
            if(selectedPaths is not null)
            {
                File.WriteAllLines(selectionFile!, selectedPaths, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }

            var progress = new Progress<NativePk2Progress>(update =>
            {
                _progressBar.Value = Math.Clamp(update.Percent, 0, _progressBar.Maximum);
                _statusLabel.Text = selectedPaths is null ? "Extracting all PK2 files..." : "Extracting selected PK2 files...";
                _detailLabel.Text = string.IsNullOrWhiteSpace(update.Status) ? outputFolder : update.Status;
                _summaryLabel.Text = $"{(_progressBar.Value / 10.0):N1}%";
                RefreshNativeProgressUi(update.Percent == 0 || update.Percent >= 1000);
            });

            await NativePk2Tools.ExtractPk2Async(pk2File, outputFolder, selectionFile, selectedPaths is null, encryptedFiles, progress, token);

            _progressBar.Value = _progressBar.Maximum;
            _summaryLabel.Text = "100%";
            var message = "PK2 extraction completed.";
            _statusLabel.Text = message;
            _detailLabel.Text = encryptedFiles
                ? "Raw encrypted payloads were preserved under the output folder."
                : "Files were restored/decrypted under the output folder.";
            MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch(OperationCanceledException)
        {
            _statusLabel.Text = T("PK2 extraction cancelled.");
            _detailLabel.Text = T("Extraction stopped before all selected files were written.");
            MessageBox.Show(this, _statusLabel.Text, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch(Exception ex)
        {
            _progressBar.Value = 0;
            _summaryLabel.Text = T("Extraction failed.");
            _statusLabel.Text = T("PK2 extraction failed.");
            _detailLabel.Text = ex.Message;
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if(!string.IsNullOrWhiteSpace(selectionFile))
            {
                TryDelete(selectionFile);
            }
            _cancelSource?.Dispose();
            _cancelSource = null;
            SetBusy(false);
        }
    }


    private async Task RunPk2OperationAsync(string pk2File, bool encrypt)
    {
        SetBusy(true, allowCancel: true);
        _cancelSource = new CancellationTokenSource();
        var token = _cancelSource.Token;
        _progressBar.Style = ProgressBarStyle.Continuous;
        _progressBar.Value = 0;
        _progressBar.MarqueeAnimationSpeed = 0;
        _statusLabel.Text = encrypt ? "Encrypting PK2 internal file payloads..." : "Decrypting PK2 internal file payloads...";
        _detailLabel.Text = pk2File;
        _summaryLabel.Text = "0%";

        try
        {
            var progress = new Progress<NativePk2Progress>(update =>
            {
                _progressBar.Value = Math.Clamp(update.Percent, 0, _progressBar.Maximum);
                _statusLabel.Text = encrypt ? "Encrypting PK2 internal file payloads..." : "Decrypting PK2 internal file payloads...";
                _detailLabel.Text = string.IsNullOrWhiteSpace(update.Status) ? pk2File : update.Status;
                _summaryLabel.Text = $"{(_progressBar.Value / 10.0):N1}%";
                RefreshNativeProgressUi(update.Percent == 0 || update.Percent >= 1000);
            });

            await NativePk2Tools.CryptPk2Async(pk2File, encrypt, progress, token);

            _progressBar.Value = _progressBar.Maximum;
            _summaryLabel.Text = "100%";
            var message = encrypt
                ? "PK2 internal payload encryption completed."
                : "PK2 internal payload decryption completed.";
            _statusLabel.Text = message;
            _detailLabel.Text = encrypt
                ? "GFXFileManager.dll can decrypt these payloads while the client reads the PK2."
                : "The same PK2 file was restored to plain internal payloads.";
            MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            InvalidatePk2Scan();
            InvalidateExtractPk2Scan();
            SetBusy(false);
            await PreviewPk2IfValidAsync(force: true);
        }
        catch(OperationCanceledException)
        {
            _statusLabel.Text = T("PK2 operation cancelled.");
            _detailLabel.Text = T("The operation stopped before completion.");
            MessageBox.Show(this, _statusLabel.Text, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch(Exception ex)
        {
            _progressBar.Value = 0;
            _summaryLabel.Text = T("Operation failed.");
            _statusLabel.Text = T("PK2 operation failed.");
            _detailLabel.Text = ex.Message;
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _cancelSource?.Dispose();
            _cancelSource = null;
            SetBusy(false);
        }
    }


    private static string InferImportInternalFolder(string sourceFolder)
    {
        var trimmed = sourceFolder.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var folderName = Path.GetFileName(trimmed);
        if(string.IsNullOrWhiteSpace(folderName))
        {
            return string.Empty;
        }

        string[] knownClientRoots = { "Data", "Map", "Media", "Music", "Particles" };
        return knownClientRoots.Contains(folderName, StringComparer.OrdinalIgnoreCase) ? folderName : string.Empty;
    }




    #endregion
}
