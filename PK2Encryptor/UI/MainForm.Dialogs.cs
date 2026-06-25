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
    #region Browse dialogs

    private async Task BrowseFolderAsync()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = T("Select folder to encrypt or decrypt in-place"),
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if(dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _folderPathBox.Text = dialog.SelectedPath;
        await PreviewFolderIfValidAsync(force: true);
    }


    private async Task BrowsePk2FileAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "PK2 archive (*.pk2)|*.pk2|All files (*.*)|*.*",
            Title = T("Select PK2 file"),
            CheckFileExists = true
        };

        if(dialog.ShowDialog(this) == DialogResult.OK)
        {
            _pk2PathBox.Text = dialog.FileName;
            await PreviewPk2IfValidAsync(force: true);
        }
    }


    private async Task BrowseExtractorPk2FileAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "PK2 archive (*.pk2)|*.pk2|All files (*.*)|*.*",
            Title = T("Select PK2 file to extract"),
            CheckFileExists = true
        };

        if(dialog.ShowDialog(this) == DialogResult.OK)
        {
            _extractPk2PathBox.Text = dialog.FileName;
            EnsureDefaultExtractOutputFolder();
            await PreviewExtractorPk2IfValidAsync(force: true);
        }
    }


    private void BrowseExtractOutputFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = T("Select folder for extracted PK2 files"),
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true
        };

        if(dialog.ShowDialog(this) == DialogResult.OK)
        {
            _extractOutputFolderBox.Text = dialog.SelectedPath;
        }
    }


    private void BrowseImportPk2File()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "PK2 archive (*.pk2)|*.pk2|All files (*.*)|*.*",
            Title = T("Select PK2 file to import into"),
            CheckFileExists = true
        };

        if(dialog.ShowDialog(this) == DialogResult.OK)
        {
            _importPk2PathBox.Text = dialog.FileName;
        }
    }


    private void BrowseImportFile()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "All files (*.*)|*.*",
            Title = T("Select file to import into PK2"),
            CheckFileExists = true
        };

        if(dialog.ShowDialog(this) == DialogResult.OK)
        {
            _importSourcePathBox.Text = dialog.FileName;
        }
    }


    private void BrowseImportFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = T("Select source folder to inject into the selected client PK2"),
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if(dialog.ShowDialog(this) == DialogResult.OK)
        {
            _importSourcePathBox.Text = dialog.SelectedPath;
        }
    }


    private void BrowseBuilderSourceFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = T("Select client root or a single PK2 source folder"),
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if(dialog.ShowDialog(this) == DialogResult.OK)
        {
            _builderSourceFolderBox.Text = dialog.SelectedPath;
            EnsureDefaultBuilderOutputFolder();
            PopulateBuilderQueue();
        }
    }


    private void BrowseBuilderOutputFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = T("Select output folder for built PK2 archives"),
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true
        };

        if(dialog.ShowDialog(this) == DialogResult.OK)
        {
            _builderOutputFolderBox.Text = dialog.SelectedPath;
            PopulateBuilderQueue();
        }
    }


    #endregion
}
