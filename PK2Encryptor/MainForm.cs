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
    #region Shared UI state

    private sealed class ThemeProfile
    {
        public ThemeProfile(string name, bool isDark, Color appBack, Color surface, Color surface2, Color panel, Color text, Color muted, Color accent, Color accent2, Color border, Color button, Color success, Color danger)
        {
            Name = name;
            IsDark = isDark;
            AppBack = appBack;
            Surface = surface;
            Surface2 = surface2;
            Panel = panel;
            Text = text;
            Muted = muted;
            Accent = accent;
            Accent2 = accent2;
            Border = border;
            Button = button;
            Success = success;
            Danger = danger;
        }

        public string Name { get; }
        public bool IsDark { get; }
        public Color AppBack { get; }
        public Color Surface { get; }
        public Color Surface2 { get; }
        public Color Panel { get; }
        public Color Text { get; }
        public Color Muted { get; }
        public Color Accent { get; }
        public Color Accent2 { get; }
        public Color Border { get; }
        public Color Button { get; }
        public Color Success { get; }
        public Color Danger { get; }
    }

    private static readonly ThemeProfile[] ThemeProfiles =
    {
        new("Light Premium / Ivory Blue", false,
            Color.FromArgb(240, 244, 249),
            Color.FromArgb(255, 255, 255),
            Color.FromArgb(247, 250, 254),
            Color.FromArgb(235, 242, 250),
            Color.FromArgb(23, 34, 49),
            Color.FromArgb(91, 105, 126),
            Color.FromArgb(39, 112, 232),
            Color.FromArgb(88, 164, 255),
            Color.FromArgb(204, 216, 232),
            Color.FromArgb(225, 235, 248),
            Color.FromArgb(34, 171, 102),
            Color.FromArgb(220, 78, 86)),
        new("Dark Premium / Obsidian Gold", true,
            Color.FromArgb(8, 11, 15),      // app background
            Color.FromArgb(18, 22, 28),     // main cards
            Color.FromArgb(12, 15, 20),     // card gradient end
            Color.FromArgb(24, 30, 38),     // inner panels
            Color.FromArgb(242, 245, 249),
            Color.FromArgb(152, 163, 178),
            Color.FromArgb(231, 181, 61),
            Color.FromArgb(255, 215, 105),
            Color.FromArgb(55, 64, 78),
            Color.FromArgb(42, 48, 58),
            Color.FromArgb(35, 165, 104),
            Color.FromArgb(188, 70, 76))
    };

    private ThemeProfile _theme = ThemeProfiles[0];
    private bool _layoutReady;
    private bool _applyingTheme;

    private Color AppBack => _theme.AppBack;
    private Color CardBack => _theme.Surface;
    private Color TextDark => _theme.Text;
    private Color TextMuted => _theme.Muted;
    private Color Blue => _theme.Button;
    private Color BlueDark => _theme.Accent;
    private Color Green => _theme.Success;
    private Color GreenDark => _theme.IsDark ? Color.FromArgb(220, 255, 229) : Color.FromArgb(20, 92, 55);
    private Color Red => _theme.Danger;
    private Color RedDark => _theme.IsDark ? Color.FromArgb(255, 224, 224) : Color.FromArgb(150, 24, 24);
    private Color GrayButton => _theme.Panel;
    private Color GrayButtonText => _theme.Text;

    private readonly Panel _workspaceHost = new();
    private readonly Panel _modeHost = new();
    private static Panel CreatePage(string name) => new()
    {
        Name = name,
        Margin = new Padding(0),
        Padding = new Padding(0)
    };

    private readonly Panel _encryptorPage = CreatePage("Encryptor");
    private readonly Panel _extractorPage = CreatePage("Extractor");
    private readonly Panel _importPage = CreatePage("Import");
    private readonly Panel _builderPage = CreatePage("Builder PK2");
    private readonly Panel _folderTab = CreatePage("Folder mode");
    private readonly Panel _pk2Tab = CreatePage("PK2 file mode");
    private Panel? _currentWorkspacePage;
    private Panel? _currentModePage;

    private Panel CurrentWorkspacePage => _currentWorkspacePage ?? _encryptorPage;
    private Panel CurrentModePage => _currentModePage ?? _folderTab;

    private readonly TextBox _folderPathBox = new();
    private readonly Button _browseFolderButton = new ModernButton();
    private readonly CheckBox _recursiveBox = new();
    private readonly CheckBox _includeHiddenBox = new();

    private readonly TextBox _pk2PathBox = new();
    private readonly Button _browsePk2Button = new ModernButton();

    private readonly ComboBox _operationBox = new ModernComboBox();
    private readonly Button _startButton = new ModernButton();
    private readonly Button _cancelButton = new ModernButton();

    private readonly ListView _previewList = new();

    private readonly TextBox _extractPk2PathBox = new();
    private readonly Button _browseExtractPk2Button = new ModernButton();
    private readonly TextBox _extractOutputFolderBox = new();
    private readonly Button _browseExtractOutputFolderButton = new ModernButton();
    private readonly ComboBox _extractPayloadModeBox = new ModernComboBox();
    private readonly Button _extractSelectedButton = new ModernButton();
    private readonly Button _extractAllButton = new ModernButton();
    private readonly ListView _extractPreviewList = new();

    private readonly TextBox _importPk2PathBox = new();
    private readonly Button _browseImportPk2Button = new ModernButton();
    private readonly TextBox _importSourcePathBox = new();
    private readonly Button _browseImportFileButton = new ModernButton();
    private readonly Button _browseImportFolderButton = new ModernButton();
    private readonly TextBox _importInternalPathBox = new();
    private readonly ComboBox _importPayloadModeBox = new ModernComboBox();
    private readonly Button _importFileButton = new ModernButton();
    private readonly Button _importFolderButton = new ModernButton();

    private readonly TextBox _builderSourceFolderBox = new();
    private readonly Button _browseBuilderSourceButton = new ModernButton();
    private readonly TextBox _builderOutputFolderBox = new();
    private readonly Button _browseBuilderOutputButton = new ModernButton();
    private readonly ListView _builderQueueList = new();
    private readonly CheckBox _builderEncryptEntriesBox = new();
    private readonly CheckBox _builderEncryptPayloadsBox = new();
    private readonly Button _builderRefreshButton = new ModernButton();
    private readonly Button _builderBuildButton = new ModernButton();

    private readonly ProgressBar _progressBar = new ModernProgressBar();
    private readonly Label _statusLabel = new();
    private readonly Label _detailLabel = new();
    private readonly Label _summaryLabel = new();
    private readonly ComboBox _themeBox = new ModernComboBox();
    private readonly TextBox _blowfishKeyBox = new();
    private readonly List<ModernButton> _navButtons = new();
    private readonly List<ModernButton> _modeButtons = new();
    private readonly List<Control> _cards = new();
    private readonly List<ListView> _themedLists = new();
    private readonly List<Button> _primaryButtons = new();
    private readonly List<Button> _secondaryButtons = new();
    private readonly List<Button> _successButtons = new();
    private readonly List<Button> _dangerButtons = new();

    private CancellationTokenSource? _cancelSource;
    private bool _busy;
    private long _lastNativeUiRefreshTicks;
    private string _scannedFolder = string.Empty;
    private string[] _scannedFiles = Array.Empty<string>();
    private string _previewRootFolder = string.Empty;
    private string _previewCurrentFolder = string.Empty;
    private string _previewCurrentPk2Folder = string.Empty;
    private string _extractCurrentPk2Folder = string.Empty;
    private string _scannedPk2 = string.Empty;
    private List<Pk2PreviewEntry> _scannedPk2Entries = new();
    private string _scannedExtractPk2 = string.Empty;
    private List<Pk2PreviewEntry> _scannedExtractPk2Entries = new();

    #endregion

    #region Form bootstrap

    public MainForm()
    {
        LoadAppSettings();
        Text = T("PK2 Tools");
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1450, 860);
        Size = new Size(1600, 980);
        Font = new Font("Segoe UI", 9.5f);
        AutoScaleMode = AutoScaleMode.Dpi;
        DoubleBuffered = true;

        BuildLayout();
        ApplyTheme(_theme);
        ApplyLanguage();
        SaveAppSettings();
        RefreshActionState();
        ApplyImmersiveTitleBar();
    }


    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyImmersiveTitleBar();
    }

    private void ApplyImmersiveTitleBar()
    {
        if(!OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            var dark = _theme.IsDark ? 1 : 0;
            _ = DwmSetWindowAttribute(Handle, 20, ref dark, sizeof(int));
            _ = DwmSetWindowAttribute(Handle, 19, ref dark, sizeof(int));
        }
        catch
        {
        }
    }

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private string CurrentBlowfishKey
    {
        get
        {
            var key = _blowfishKeyBox.Text.Trim();
            return string.IsNullOrWhiteSpace(key) ? "169841" : key;
        }
    }

    #endregion
}
