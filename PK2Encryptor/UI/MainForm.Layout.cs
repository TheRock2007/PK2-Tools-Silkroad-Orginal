using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PK2Encryptor;

public sealed partial class MainForm : Form
{
    #region Layout and visible sections

    private void BuildLayout()
    {
        _layoutReady = false;
        SuspendLayout();
        Controls.Clear();
        _cards.Clear();
        _navButtons.Clear();
        _modeButtons.Clear();
        _themedLists.Clear();
        _primaryButtons.Clear();
        _secondaryButtons.Clear();
        _successButtons.Clear();
        _dangerButtons.Clear();

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(20),
            BackColor = AppBack
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 306));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(shell);

        shell.Controls.Add(BuildSidebar(), 0, 0);

        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = AppBack,
            Margin = new Padding(12, 0, 0, 0)
        };
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 124));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        shell.Controls.Add(main, 1, 0);

        main.Controls.Add(BuildHeader(), 0, 0);
        main.Controls.Add(BuildWorkspaceTabs(), 0, 1);
        main.Controls.Add(BuildStatusCard(), 0, 2);

        _layoutReady = true;
        UpdateNavigationState();
        ResumeLayout(true);
    }

    private Control BuildSidebar()
    {
        var side = CreateCard(strong: true, accentLine: false, radius: 22);
        side.Margin = new Padding(0);
        side.Padding = new Padding(16);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 11,
            BackColor = side.SurfaceColor,
            Margin = new Padding(0)
        };
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 126));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 2));
        side.Controls.Add(grid);

        var logoGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            BackColor = _theme.Surface,
            Margin = new Padding(2, 0, 2, 12)
        };
        logoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));
        logoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        logoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        logoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 32));
        logoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 26));

        var logoMark = new Panel { Dock = DockStyle.Fill, BackColor = _theme.Surface, Margin = new Padding(0, 6, 10, 8) };
        logoMark.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(7, 7, Math.Min(logoMark.Width, logoMark.Height) - 14, Math.Min(logoMark.Width, logoMark.Height) - 14);
            using var pen = new Pen(_theme.Accent, 2.4f);
            e.Graphics.TranslateTransform(rect.Left + rect.Width / 2f, rect.Top + rect.Height / 2f);
            e.Graphics.RotateTransform(45);
            e.Graphics.DrawRectangle(pen, -rect.Width / 2, -rect.Height / 2, rect.Width, rect.Height);
            using var inner = new Pen(Color.FromArgb(_theme.IsDark ? 190 : 170, _theme.Accent2), 1.4f);
            e.Graphics.DrawRectangle(inner, -rect.Width / 4, -rect.Height / 4, rect.Width / 2, rect.Height / 2);
            e.Graphics.ResetTransform();
        };
        logoGrid.Controls.Add(logoMark, 0, 0);
        logoGrid.SetRowSpan(logoMark, 3);
        logoGrid.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = T("PK2 Tools"),
            ForeColor = TextDark,
            Font = new Font("Segoe UI Semibold", 15.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.BottomLeft,
            AutoEllipsis = true
        }, 1, 0);
        logoGrid.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = T("Studio"),
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9.4f),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        }, 1, 1);
        logoGrid.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = T("Build • Extract • Inject • Secure"),
            ForeColor = Color.FromArgb(_theme.IsDark ? 150 : 180, _theme.Accent),
            Font = new Font("Segoe UI Semibold", 8.4f, FontStyle.Bold),
            TextAlign = ContentAlignment.TopLeft,
            AutoEllipsis = true
        }, 1, 2);
        grid.Controls.Add(logoGrid, 0, 0);

        var navCaption = new Label
        {
            Dock = DockStyle.Fill,
            Text = T("WORKSPACE"),
            ForeColor = TextMuted,
            Font = new Font("Segoe UI Semibold", 8.4f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0)
        };
        grid.Controls.Add(navCaption, 0, 1);

        grid.Controls.Add(CreateNavButton("Encryptor", _encryptorPage), 0, 2);
        grid.Controls.Add(CreateNavButton("Extractor", _extractorPage), 0, 3);
        grid.Controls.Add(CreateNavButton("Import / Inject", _importPage), 0, 4);
        grid.Controls.Add(CreateNavButton("Build PK2", _builderPage), 0, 5);

        var divider = new Panel { Dock = DockStyle.Fill, Margin = new Padding(6, 8, 6, 8), Height = 1, BackColor = _theme.Border, Tag = "Divider" };
        grid.Controls.Add(divider, 0, 6);

        grid.Controls.Add(BuildMiniStatsCard(), 0, 7);

        var ornament = new Panel { Dock = DockStyle.Fill, BackColor = _theme.Surface, Margin = new Padding(0, 8, 0, 8) };
        ornament.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var c = new PointF(ornament.Width / 2f, ornament.Height / 2f);
            using var pen = new Pen(Color.FromArgb(_theme.IsDark ? 95 : 70, _theme.Accent), 1.3f);
            e.Graphics.DrawLine(pen, c.X - 30, c.Y, c.X + 30, c.Y);
            e.Graphics.DrawLine(pen, c.X, c.Y - 30, c.X, c.Y + 30);
            e.Graphics.DrawEllipse(pen, c.X - 24, c.Y - 24, 48, 48);
            TextRenderer.DrawText(e.Graphics, "PK2", new Font("Segoe UI Semibold", 18f, FontStyle.Bold), new Rectangle(0, (int)c.Y - 18, ornament.Width, 36), Color.FromArgb(_theme.IsDark ? 120 : 105, TextMuted), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };
        grid.Controls.Add(ornament, 0, 8);

        var status = CreateCard(strong: false, accentLine: false, radius: 16);
        status.Margin = new Padding(0, 8, 0, 0);
        status.Padding = new Padding(14, 10, 14, 10);
        var version = new Label
        {
            Dock = DockStyle.Fill,
            Text = T("GFX Compatible\r\nPayload Secure      ●\r\nv1.0.0"),
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9f),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        status.Controls.Add(version);
        grid.Controls.Add(status, 0, 9);

        return side;
    }

    private Control BuildMiniStatsCard()
    {
        var card = CreateCard(strong: false, accentLine: false, radius: 14);
        card.Padding = new Padding(12, 10, 12, 10);
        card.Margin = new Padding(0, 8, 0, 8);
        var text = new Label
        {
            Dock = DockStyle.Fill,
            Text = T("PK2 workflow\r\n• Build PK2 archives\r\n• Extract PK2 files\r\n• Import and secure payloads"),
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9f),
            TextAlign = ContentAlignment.MiddleLeft
        };
        card.Controls.Add(text);
        return card;
    }

    private ModernButton CreateNavButton(string text, Panel page)
    {
        var button = new ModernButton
        {
            Dock = DockStyle.Fill,
            Text = T(text),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 10.2f, FontStyle.Bold),
            Margin = new Padding(0, 4, 0, 4),
            Padding = new Padding(22, 0, 14, 0),
            CornerRadius = 0,
            SelectionStyle = ModernButtonSelectionStyle.LeftBar,
            UseAccentTextWhenSelected = true,
            Tag = page
        };
        button.Click += async (_, _) =>
        {
            ShowWorkspacePage(page);
            await RefreshCurrentPagePreviewAsync();
        };
        _navButtons.Add(button);
        return button;
    }

    private Control BuildHeader()
    {
        var header = CreateCard(strong: true, accentLine: true, radius: 22);
        header.Margin = new Padding(0, 0, 0, 14);
        header.Padding = new Padding(20, 16, 20, 16);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = header.SurfaceColor,
            Margin = new Padding(0)
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 74));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 820));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        header.Controls.Add(grid);

        var headerMark = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = header.SurfaceColor,
            Margin = new Padding(0, 4, 18, 4)
        };
        headerMark.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var size = Math.Min(headerMark.Width - 14, headerMark.Height - 14);
            var rect = new Rectangle((headerMark.Width - size) / 2, (headerMark.Height - size) / 2, size, size);
            using var glow = new Pen(Color.FromArgb(_theme.IsDark ? 90 : 70, _theme.Accent2), 5f);
            using var pen = new Pen(_theme.Accent, 2.4f);
            using var inner = new Pen(Color.FromArgb(_theme.IsDark ? 220 : 190, _theme.Accent2), 1.4f);
            e.Graphics.TranslateTransform(rect.Left + rect.Width / 2f, rect.Top + rect.Height / 2f);
            e.Graphics.RotateTransform(45);
            e.Graphics.DrawRectangle(glow, -rect.Width / 2 + 2, -rect.Height / 2 + 2, rect.Width - 4, rect.Height - 4);
            e.Graphics.DrawRectangle(pen, -rect.Width / 2 + 4, -rect.Height / 2 + 4, rect.Width - 8, rect.Height - 8);
            e.Graphics.DrawRectangle(inner, -rect.Width / 4, -rect.Height / 4, rect.Width / 2, rect.Height / 2);
            e.Graphics.ResetTransform();
        };
        grid.Controls.Add(headerMark, 0, 0);

        var titleGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = header.SurfaceColor,
            Margin = new Padding(0, 0, 20, 0)
        };
        titleGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
        titleGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 28));
        titleGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
        grid.Controls.Add(titleGrid, 1, 0);

        titleGrid.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = T("PK2 Tools - Studio"),
            Font = new Font("Segoe UI Semibold", 22f, FontStyle.Bold),
            ForeColor = TextDark,
            TextAlign = ContentAlignment.BottomLeft,
            AutoEllipsis = true,
            Padding = new Padding(0, 0, 0, 0)
        }, 0, 0);
        titleGrid.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = string.Empty,
            Font = new Font("Segoe UI", 10.2f),
            ForeColor = TextMuted,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        }, 0, 1);
        titleGrid.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = string.Empty,
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(_theme.IsDark ? 170 : 190, _theme.Accent),
            TextAlign = ContentAlignment.TopLeft,
            AutoEllipsis = true
        }, 0, 2);

        var toolGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2,
            BackColor = header.SurfaceColor,
            Margin = new Padding(0)
        };
        toolGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 185));
        toolGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 185));
        toolGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        toolGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        toolGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grid.Controls.Add(toolGrid, 2, 0);

        var themeLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = T("INTERFACE THEME"),
            ForeColor = TextMuted,
            TextAlign = ContentAlignment.BottomLeft,
            Font = new Font("Segoe UI Semibold", 8.3f, FontStyle.Bold),
            AutoEllipsis = true,
            Margin = new Padding(0, 0, 14, 0)
        };
        RegisterLocalizedText(themeLabel, "INTERFACE THEME");
        toolGrid.Controls.Add(themeLabel, 0, 0);

        ConfigureComboBox(_themeBox);
        _themeBox.Margin = new Padding(0, 2, 14, 12);
        _themeBox.MinimumSize = new Size(0, 34);
        PopulateThemeCombo();
        _themeBox.SelectedIndexChanged += (_, _) =>
        {
            if(!_layoutReady || _applyingTheme || _applyingLanguage)
            {
                return;
            }
            if(_themeBox.SelectedIndex >= 0 && _themeBox.SelectedIndex < ThemeProfiles.Length)
            {
                ApplyTheme(ThemeProfiles[_themeBox.SelectedIndex]);
                SaveAppSettings();
            }
        };
        toolGrid.Controls.Add(_themeBox, 0, 1);

        var languageLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = T("LANGUAGE"),
            ForeColor = TextMuted,
            TextAlign = ContentAlignment.BottomLeft,
            Font = new Font("Segoe UI Semibold", 8.3f, FontStyle.Bold),
            AutoEllipsis = true,
            Margin = new Padding(0, 0, 14, 0)
        };
        RegisterLocalizedText(languageLabel, "LANGUAGE");
        toolGrid.Controls.Add(languageLabel, 1, 0);

        ConfigureComboBox(_languageBox);
        _languageBox.Margin = new Padding(0, 2, 14, 12);
        _languageBox.MinimumSize = new Size(0, 34);
        PopulateLanguageCombo();
        _languageBox.SelectedIndexChanged += (_, _) =>
        {
            if(!_layoutReady || _applyingLanguage)
            {
                return;
            }
            if(_languageBox.SelectedIndex >= 0 && _languageBox.SelectedIndex < LanguageProfiles.Length)
            {
                _language = LanguageProfiles[_languageBox.SelectedIndex];
                ApplyLanguage();
                SaveAppSettings();
            }
        };
        toolGrid.Controls.Add(_languageBox, 1, 1);



        var blowfishKeyLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = T("BLOWFISH KEY"),
            ForeColor = TextMuted,
            TextAlign = ContentAlignment.BottomLeft,
            Font = new Font("Segoe UI Semibold", 8.3f, FontStyle.Bold),
            AutoEllipsis = true,
            Margin = new Padding(0, 0, 14, 0)
        };
        RegisterLocalizedText(blowfishKeyLabel, "BLOWFISH KEY");
        toolGrid.Controls.Add(blowfishKeyLabel, 2, 0);

        ConfigurePathBox(_blowfishKeyBox, "Default: 169841");
        _blowfishKeyBox.Margin = new Padding(0, 2, 14, 12);
        _blowfishKeyBox.MinimumSize = new Size(0, 34);
        _blowfishKeyBox.Text = string.IsNullOrWhiteSpace(_settings.BlowfishKey) ? "169841" : _settings.BlowfishKey;
        _blowfishKeyBox.MaxLength = 56;
        RegisterLocalizedPlaceholder(_blowfishKeyBox, "Default: 169841");
        _blowfishKeyBox.TextChanged += (_, _) => SaveAppSettings();
        toolGrid.Controls.Add(_blowfishKeyBox, 2, 1);

        var badge = CreateCard(strong: true, accentLine: false, radius: 16);
        badge.Margin = new Padding(0, 4, 0, 10);
        badge.Padding = new Padding(14, 10, 14, 10);
        var badgeGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            BackColor = badge.SurfaceColor
        };
        badgeGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        badgeGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        badgeGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 26));
        badgeGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
        badgeGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
        badge.Controls.Add(badgeGrid);
        var badgeIcon = new Label
        {
            Dock = DockStyle.Fill,
            Text = "◆",
            ForeColor = _theme.Accent,
            Font = new Font("Segoe UI Semibold", 18f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };
        badgeGrid.Controls.Add(badgeIcon, 0, 0);
        badgeGrid.SetRowSpan(badgeIcon, 2);
        badgeGrid.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = T("GFX Compatible"),
            TextAlign = ContentAlignment.BottomLeft,
            Font = new Font("Segoe UI Semibold", 9.4f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoEllipsis = true
        }, 1, 0);
        badgeGrid.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = T("Payload Secure"),
            TextAlign = ContentAlignment.TopLeft,
            Font = new Font("Segoe UI", 9f),
            ForeColor = TextMuted,
            AutoEllipsis = true
        }, 1, 1);
        var badgeDot = new Label
        {
            Dock = DockStyle.Fill,
            Text = "●",
            ForeColor = _theme.Success,
            Font = new Font("Segoe UI Semibold", 13.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };
        badgeGrid.Controls.Add(badgeDot, 2, 0);
        badgeGrid.SetRowSpan(badgeDot, 2);
        toolGrid.Controls.Add(badge, 3, 0);
        toolGrid.SetRowSpan(badge, 2);

        return header;
    }

    private Control BuildWorkspaceTabs()
    {
        var contentCard = CreateCard(strong: true, accentLine: false, radius: 20);
        contentCard.Margin = new Padding(0, 0, 0, 12);
        contentCard.Padding = new Padding(18);
        _workspaceHost.Dock = DockStyle.Fill;
        _workspaceHost.Margin = new Padding(0);
        _workspaceHost.Padding = new Padding(0);
        _workspaceHost.BackColor = contentCard.SurfaceColor;
        contentCard.Controls.Add(_workspaceHost);

        BuildEncryptorPage();
        BuildExtractorPage();
        BuildImportPage();
        BuildBuilderPage();
        ShowWorkspacePage(_importPage);
        return contentCard;
    }

    private ModernButton CreateTopNavButton(string text, Panel page)
    {
        var button = new ModernButton
        {
            Dock = DockStyle.Fill,
            Text = T(text),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 10.2f, FontStyle.Bold),
            Margin = new Padding(8, 4, 8, 4),
            Padding = new Padding(10, 0, 10, 0),
            SelectionStyle = ModernButtonSelectionStyle.BottomBar,
            UseAccentTextWhenSelected = true,
            Tag = page
        };
        button.Click += async (_, _) =>
        {
            ShowWorkspacePage(page);
            await RefreshCurrentPagePreviewAsync();
        };
        _navButtons.Add(button);
        return button;
    }

    private void ShowWorkspacePage(Panel page)
    {
        if(_currentWorkspacePage == page && _workspaceHost.Controls.Contains(page))
        {
            return;
        }

        _workspaceHost.SuspendLayout();
        _workspaceHost.Controls.Clear();
        page.Dock = DockStyle.Fill;
        page.Margin = new Padding(0);
        page.BackColor = CardBack;
        _workspaceHost.Controls.Add(page);
        _currentWorkspacePage = page;
        _workspaceHost.ResumeLayout(true);
        UpdateNavigationState();
        RefreshActionState();
    }

    private async Task RefreshCurrentPagePreviewAsync()
    {
        if(CurrentWorkspacePage == _extractorPage)
        {
            await PreviewExtractorPk2IfValidAsync();
        }
        else if(CurrentWorkspacePage == _encryptorPage)
        {
            if(CurrentModePage == _pk2Tab)
            {
                await PreviewPk2IfValidAsync();
            }
            else
            {
                await PreviewFolderIfValidAsync();
            }
        }
    }

    private void UpdateNavigationState()
    {
        foreach(var button in _navButtons)
        {
            button.Selected = ReferenceEquals(button.Tag, CurrentWorkspacePage);
        }
    }

    private void BuildEncryptorPage()
    {
        _encryptorPage.BackColor = CardBack;
        _encryptorPage.Padding = new Padding(4);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            BackColor = _theme.Surface
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        _encryptorPage.Controls.Add(grid);

        grid.Controls.Add(BuildModeCard(), 0, 0);
        grid.Controls.Add(BuildOperationInfoCard("Encryptor", "Protect folders or internal PK2 payloads without changing the readable PK2 header.", "Mode\r\nFolder or PK2", "Output\r\nIn-place secure payload"), 1, 0);
        var previewCard = BuildPreviewCard("Encryption target list");
        grid.Controls.Add(previewCard, 0, 1);
        grid.SetColumnSpan(previewCard, 2);
        var actionCard = BuildEncryptActionCard();
        grid.Controls.Add(actionCard, 0, 2);
        grid.SetColumnSpan(actionCard, 2);
    }

    private Control BuildModeCard()
    {
        var card = CreateCard(strong: true, accentLine: false, radius: 14);
        card.Padding = new Padding(10);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = card.SurfaceColor
        };
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.Controls.Add(grid);

        var modes = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = _theme.Surface,
            Margin = new Padding(0, 0, 0, 8)
        };
        modes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        modes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        modes.Controls.Add(CreateModeButton("Folder mode", _folderTab), 0, 0);
        modes.Controls.Add(CreateModeButton("PK2 file mode", _pk2Tab), 1, 0);
        grid.Controls.Add(modes, 0, 0);

        _modeHost.Dock = DockStyle.Fill;
        _modeHost.Margin = new Padding(0);
        _modeHost.BackColor = CardBack;
        grid.Controls.Add(_modeHost, 0, 1);

        BuildFolderTab();
        BuildPk2Tab();
        ShowModePage(_folderTab);
        return card;
    }

    private ModernButton CreateModeButton(string text, Panel page)
    {
        var button = new ModernButton
        {
            Dock = DockStyle.Fill,
            Text = T(text),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            Margin = text.StartsWith("PK2", StringComparison.OrdinalIgnoreCase) ? new Padding(10, 0, 0, 0) : new Padding(0, 0, 10, 0),
            Padding = new Padding(8, 0, 8, 0),
            CornerRadius = 0,
            SelectionStyle = ModernButtonSelectionStyle.SoftFill,
            UseAccentTextWhenSelected = true,
            Tag = page
        };
        button.Click += async (_, _) =>
        {
            ShowModePage(page);
            if(CurrentModePage == _pk2Tab)
            {
                await PreviewPk2IfValidAsync();
            }
            else
            {
                await PreviewFolderIfValidAsync();
            }
        };
        _modeButtons.Add(button);
        return button;
    }

    private void ShowModePage(Panel page)
    {
        if(_currentModePage == page && _modeHost.Controls.Contains(page))
        {
            return;
        }

        _modeHost.SuspendLayout();
        _modeHost.Controls.Clear();
        page.Dock = DockStyle.Fill;
        page.Margin = new Padding(0);
        page.BackColor = CardBack;
        _modeHost.Controls.Add(page);
        _currentModePage = page;
        _modeHost.ResumeLayout(true);
        RefreshActionState();
        UpdateModeButtonState();
    }

    private void UpdateModeButtonState()
    {
        foreach(var button in _modeButtons)
        {
            button.Selected = ReferenceEquals(button.Tag, CurrentModePage);
        }
    }

    private void BuildFolderTab()
    {
        _folderTab.BackColor = CardBack;
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(12, 14, 12, 12),
            BackColor = CardBack
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 176));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 4));
        _folderTab.Controls.Add(grid);

        AddLabel(grid, "Folder path", 0, 0, 2);
        ConfigurePathBox(_folderPathBox, "Paste or browse the folder path...");
        _folderPathBox.TextChanged += async (_, _) =>
        {
            InvalidateFolderScan();
            RefreshActionState();
            await PreviewFolderIfValidAsync();
        };
        grid.Controls.Add(_folderPathBox, 0, 1);

        ConfigureButton(_browseFolderButton, "Browse", ButtonRole.Primary);
        _browseFolderButton.Click += async (_, _) => await BrowseFolderAsync();
        grid.Controls.Add(_browseFolderButton, 1, 1);

        var options = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = _theme.Surface,
            Margin = new Padding(0)
        };
        ConfigureCheckBox(_recursiveBox, "Include subfolders", true);
        ConfigureCheckBox(_includeHiddenBox, "Include hidden/system files", false);
        _recursiveBox.CheckedChanged += async (_, _) =>
        {
            InvalidateFolderScan();
            await PreviewFolderIfValidAsync(force: true);
        };
        _includeHiddenBox.CheckedChanged += async (_, _) =>
        {
            InvalidateFolderScan();
            await PreviewFolderIfValidAsync(force: true);
        };
        options.Controls.Add(_recursiveBox);
        options.Controls.Add(_includeHiddenBox);
        grid.Controls.Add(options, 0, 2);
        grid.SetColumnSpan(options, 2);

        var help = CreateHintLabel("Explorer preview opens the current folder instantly. Include subfolders affects the Start operation, not the preview navigation.");
        grid.Controls.Add(help, 0, 3);
        grid.SetColumnSpan(help, 2);
    }

    private void BuildPk2Tab()
    {
        _pk2Tab.BackColor = CardBack;
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(12, 14, 12, 12),
            BackColor = CardBack
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 176));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _pk2Tab.Controls.Add(grid);

        AddLabel(grid, "PK2 file path", 0, 0, 2);
        ConfigurePathBox(_pk2PathBox, "Paste or browse a .pk2 file path...");
        _pk2PathBox.TextChanged += async (_, _) =>
        {
            InvalidatePk2Scan();
            RefreshActionState();
            await PreviewPk2IfValidAsync();
        };
        grid.Controls.Add(_pk2PathBox, 0, 1);

        ConfigureButton(_browsePk2Button, "Browse", ButtonRole.Primary);
        _browsePk2Button.Click += async (_, _) => await BrowsePk2FileAsync();
        grid.Controls.Add(_browsePk2Button, 1, 1);

        var help = CreateHintLabel("PK2 file mode encrypts or decrypts stored payloads inside the selected archive. Import and Extractor have their own workspaces.");
        grid.Controls.Add(help, 0, 2);
        grid.SetColumnSpan(help, 2);
    }

    private Control BuildOperationInfoCard(string titleText, string body, string stat1, string stat2)
    {
        var card = CreateCard(strong: false, accentLine: true, radius: 16);
        card.Margin = new Padding(12, 0, 0, 12);
        card.Padding = new Padding(16);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = _theme.Surface
        };
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        card.Controls.Add(grid);

        grid.Controls.Add(CreateSectionTitle(titleText), 0, 0);
        var label = CreateHintLabel(body);
        label.TextAlign = ContentAlignment.TopLeft;
        grid.Controls.Add(label, 0, 1);
        grid.Controls.Add(CreateInfoPill(stat1), 0, 2);
        grid.Controls.Add(CreateInfoPill(stat2), 0, 3);
        return card;
    }

    private Control CreateInfoPill(string text)
    {
        var pill = CreateCard(strong: false, accentLine: false, radius: 12);
        pill.Margin = new Padding(0, 5, 0, 0);
        pill.Padding = new Padding(10, 4, 10, 4);
        var label = new Label
        {
            Dock = DockStyle.Fill,
            Text = T(text),
            ForeColor = TextDark,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        RegisterLocalizedText(label, text);
        pill.Controls.Add(label);
        return pill;
    }

    private Control BuildPreviewCard(string titleText)
    {
        var card = CreateCard(strong: true, accentLine: true, radius: 16);
        card.Margin = new Padding(0, 0, 0, 12);
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(14),
            BackColor = _theme.Surface
        };
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.Controls.Add(grid);

        grid.Controls.Add(CreateSectionTitle(titleText), 0, 0);
        ConfigureListView(_previewList);
        _previewList.SelectedIndexChanged += (_, _) => RefreshActionState();
        grid.Controls.Add(CreateListFrame(_previewList), 0, 1);
        return card;
    }

    private Control CreateListFrame(ListView list)
    {
        var frame = CreateCard(strong: true, accentLine: true, radius: 10);
        frame.Margin = new Padding(0);
        frame.Padding = new Padding(4);
        frame.Tag = "ListFrame";
        frame.BorderColor = _theme.IsDark ? Color.FromArgb(84, 98, 118) : Color.FromArgb(174, 198, 229);
        frame.SurfaceColor = _theme.IsDark ? Color.FromArgb(7, 10, 14) : Color.FromArgb(252, 254, 255);
        frame.SurfaceColor2 = _theme.IsDark ? Color.FromArgb(7, 10, 14) : Color.FromArgb(252, 254, 255);
        list.Margin = new Padding(0);
        list.BorderStyle = BorderStyle.None;
        frame.Controls.Add(list);
        return frame;
    }

    private void ConfigureListView(ListView list)
    {
        list.Dock = DockStyle.Fill;
        list.View = View.Details;
        list.FullRowSelect = true;
        list.MultiSelect = true;
        list.GridLines = false;
        list.HideSelection = false;
        list.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        list.BorderStyle = BorderStyle.None;
        list.OwnerDraw = true;
        list.BackColor = _theme.IsDark ? Color.FromArgb(7, 10, 14) : Color.FromArgb(252, 254, 255);
        list.ForeColor = TextDark;
        list.Font = new Font("Segoe UI", 9.25f);
        list.ShowItemToolTips = true;
        EnsureListRowHeight(list, 30);
        ConfigureExplorerColumns(list);
        list.DrawColumnHeader += DrawListHeader;
        list.DrawSubItem += DrawListSubItem;
        list.DrawItem += (_, e) => { if(list.View != View.Details) e.DrawDefault = true; };
        list.HandleCreated += (_, _) => FitListColumns(list);
        list.Resize += (_, _) => FitListColumns(list);
        if(ReferenceEquals(list, _previewList))
        {
            list.DoubleClick += (_, _) => HandlePreviewListDoubleClick();
        }
        else if(ReferenceEquals(list, _extractPreviewList))
        {
            list.DoubleClick += (_, _) => HandleExtractPreviewListDoubleClick();
        }
        if(!_themedLists.Contains(list))
        {
            _themedLists.Add(list);
        }
    }

    private void ConfigureExplorerColumns(ListView list)
    {
        if(list.Columns.Count != 4)
        {
            list.Columns.Clear();
            list.Columns.Add(string.Empty, 720);
            list.Columns.Add(string.Empty, 120, HorizontalAlignment.Right);
            list.Columns.Add(string.Empty, 160);
            list.Columns.Add(string.Empty, 170);
        }

        list.Columns[0].Text = T("Name");
        list.Columns[1].Text = T("Size");
        list.Columns[2].Text = T("Type");
        list.Columns[3].Text = T("State");
    }

    private static void EnsureListRowHeight(ListView list, int height)
    {
        if(list.SmallImageList is not null && list.SmallImageList.ImageSize.Height == height)
        {
            return;
        }

        var imageHeight = Math.Max(18, height);
        var images = new ImageList
        {
            ColorDepth = ColorDepth.Depth32Bit,
            ImageSize = new Size(1, imageHeight)
        };
        using var bitmap = new Bitmap(1, imageHeight);
        images.Images.Add((Bitmap)bitmap.Clone());
        list.SmallImageList = images;
    }

    private void DrawListHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
    {
        using var fill = new SolidBrush(_theme.IsDark ? Color.FromArgb(20, 26, 34) : Color.FromArgb(234, 242, 252));
        using var border = new Pen(_theme.IsDark ? Color.FromArgb(70, 84, 104) : Color.FromArgb(184, 205, 232));
        using var accent = new Pen(Color.FromArgb(_theme.IsDark ? 150 : 130, _theme.Accent));
        e.Graphics.FillRectangle(fill, e.Bounds);
        e.Graphics.DrawLine(border, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        e.Graphics.DrawLine(accent, e.Bounds.Left, e.Bounds.Top, e.Bounds.Right, e.Bounds.Top);
        var flags = e.Header.TextAlign == HorizontalAlignment.Right
            ? TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
            : TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
        var rect = new Rectangle(e.Bounds.Left + 10, e.Bounds.Top, e.Bounds.Width - 16, e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, e.Header.Text, new Font("Segoe UI Semibold", 9f, FontStyle.Bold), rect, TextDark, flags);
    }

    private void DrawListSubItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        DrawListCell(e, e.SubItem.Text, e.Bounds, leftPadding: 10, bold: false);
    }

    private void DrawListCell(DrawListViewSubItemEventArgs e, string text, Rectangle bounds, int leftPadding, bool bold)
    {
        var selected = e.Item.Selected;
        var evenBack = _theme.IsDark ? Color.FromArgb(7, 10, 14) : Color.FromArgb(252, 254, 255);
        var oddBack = _theme.IsDark ? Color.FromArgb(11, 15, 21) : Color.FromArgb(244, 248, 253);
        var selectedBack = _theme.IsDark ? ModernUiPaint.Blend(evenBack, _theme.Accent, 18) : Color.FromArgb(39, 112, 232);
        using var back = new SolidBrush(selected ? selectedBack : (e.ItemIndex % 2 == 0 ? evenBack : oddBack));
        e.Graphics.FillRectangle(back, bounds);
        using(var grid = new Pen(_theme.IsDark ? Color.FromArgb(42, 52, 66) : Color.FromArgb(220, 230, 242)))
        {
            e.Graphics.DrawLine(grid, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);
            if(e.ColumnIndex > 0)
            {
                e.Graphics.DrawLine(grid, bounds.Left, bounds.Top + 4, bounds.Left, bounds.Bottom - 4);
            }
        }
        var color = selected ? Color.White : TextDark;
        var flags = e.Header.TextAlign == HorizontalAlignment.Right
            ? TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
            : TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
        using var font = bold ? new Font(e.Item.ListView?.Font ?? Font, FontStyle.Bold) : null;
        var rect = new Rectangle(bounds.Left + leftPadding, bounds.Top, Math.Max(0, bounds.Width - leftPadding - 10), bounds.Height);
        TextRenderer.DrawText(e.Graphics, text, font ?? e.Item.ListView?.Font ?? Font, rect, color, flags);
    }

    private static void FitListColumns(ListView list)
    {
        if(list.Columns.Count < 4 || list.ClientSize.Width <= 0)
        {
            return;
        }

        const int sizeColumnWidth = 120;
        const int typeColumnWidth = 170;
        const int stateColumnWidth = 170;
        var available = Math.Max(1, list.ClientSize.Width - 2);
        var nameColumnWidth = Math.Max(520, available - sizeColumnWidth - typeColumnWidth - stateColumnWidth);
        list.Columns[0].Width = nameColumnWidth;
        list.Columns[1].Width = sizeColumnWidth;
        list.Columns[2].Width = typeColumnWidth;
        list.Columns[3].Width = Math.Max(stateColumnWidth, available - nameColumnWidth - sizeColumnWidth - typeColumnWidth);
    }

    private Control BuildEncryptActionCard()
    {
        var card = CreateCard(strong: false, accentLine: false, radius: 16);
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 1,
            Padding = new Padding(14, 8, 14, 8),
            BackColor = _theme.Surface
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 188));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 156));
        card.Controls.Add(grid);

        var operationLabel = new Label
        {
            Text = T("Operation"),
            Dock = DockStyle.Fill,
            ForeColor = TextDark,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        RegisterLocalizedText(operationLabel, "Operation");
        grid.Controls.Add(operationLabel, 0, 0);

        ConfigureComboBox(_operationBox);
        _operationBox.Items.Clear();
        _operationBox.Items.AddRange(new object[] { T("Encrypt"), T("Decrypt") });
        _operationBox.SelectedIndex = 0;
        _operationBox.Margin = new Padding(0, 10, 12, 10);
        _operationBox.SelectedIndexChanged += (_, _) => RefreshActionState();
        grid.Controls.Add(_operationBox, 1, 0);

        var note = CreateHintLabel("Choose a source, review the target list, then start the protected payload operation.");
        grid.Controls.Add(note, 2, 0);

        ConfigureButton(_startButton, "Start", ButtonRole.Success);
        _startButton.Click += async (_, _) => await StartOperationAsync();
        grid.Controls.Add(_startButton, 3, 0);

        ConfigureButton(_cancelButton, "Cancel", ButtonRole.Danger);
        _cancelButton.Click += (_, _) => _cancelSource?.Cancel();
        grid.Controls.Add(_cancelButton, 5, 0);

        return card;
    }

    private Control BuildStatusCard()
    {
        var card = CreateCard(strong: true, accentLine: false, radius: 20);
        card.Margin = new Padding(0);
        card.Padding = new Padding(18, 14, 18, 14);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 3,
            BackColor = _theme.Surface,
            Margin = new Padding(0)
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 98));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.Controls.Add(grid);

        var statusTitle = new Label
        {
            Dock = DockStyle.Fill,
            Text = T("LIVE STATUS"),
            ForeColor = TextDark,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
            AutoEllipsis = true
        };
        RegisterLocalizedText(statusTitle, "LIVE STATUS");
        grid.Controls.Add(statusTitle, 0, 0);

        _progressBar.Dock = DockStyle.Fill;
        _progressBar.Margin = new Padding(0, 9, 0, 9);
        _progressBar.Minimum = 0;
        _progressBar.Maximum = 10000;
        grid.Controls.Add(_progressBar, 1, 0);
        grid.SetColumnSpan(_progressBar, 2);

        _summaryLabel.Dock = DockStyle.Fill;
        _summaryLabel.ForeColor = TextMuted;
        _summaryLabel.Text = T("Choose a valid folder.");
        _summaryLabel.TextAlign = ContentAlignment.MiddleRight;
        _summaryLabel.AutoEllipsis = true;
        _summaryLabel.Font = new Font("Segoe UI", 9.2f);
        grid.Controls.Add(_summaryLabel, 3, 0);

        var divider = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = _theme.Border,
            Margin = new Padding(0, 6, 0, 6),
            Height = 1,
            Tag = "Divider"
        };
        grid.Controls.Add(divider, 0, 1);
        grid.SetColumnSpan(divider, 4);

        grid.Controls.Add(CreateStatusCaption("Status"), 0, 2);
        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.ForeColor = TextDark;
        _statusLabel.Text = T("Ready.");
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
        _statusLabel.AutoEllipsis = true;
        grid.Controls.Add(_statusLabel, 1, 2);


        _detailLabel.Dock = DockStyle.Fill;
        _detailLabel.ForeColor = TextMuted;
        _detailLabel.Text = T("Each page has its own controls. Select Encryptor, Extractor, Import, or Builder from the sidebar.");
        _detailLabel.TextAlign = ContentAlignment.MiddleLeft;
        _detailLabel.Font = new Font("Consolas", 9f);
        _detailLabel.AutoEllipsis = true;
        _detailLabel.AutoSize = false;
        grid.Controls.Add(_detailLabel, 2, 2);
        grid.SetColumnSpan(_detailLabel, 2);
        return card;
    }

    private Label CreateStatusCaption(string text)
    {
        var label = new Label
        {
            Text = T(text),
            Dock = DockStyle.Fill,
            ForeColor = TextMuted,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold)
        };
        RegisterLocalizedText(label, text);
        return label;
    }

    private void BuildExtractorPage()
    {
        _extractorPage.BackColor = CardBack;
        _extractorPage.Padding = new Padding(4);
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            BackColor = _theme.Surface
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 240));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        _extractorPage.Controls.Add(grid);

        grid.Controls.Add(BuildExtractorInputCard(), 0, 0);
        grid.Controls.Add(BuildOperationInfoCard("Extractor", "Read the internal file tree, restore payloads, or export raw stored bytes.", "Read\r\nPK2 contents", "Extract\r\nSelected or all"), 1, 0);
        var listCard = BuildExtractorListCard();
        grid.Controls.Add(listCard, 0, 1);
        grid.SetColumnSpan(listCard, 2);
        var actionCard = BuildExtractorActionCard();
        grid.Controls.Add(actionCard, 0, 2);
        grid.SetColumnSpan(actionCard, 2);
    }

    private Control BuildExtractorInputCard()
    {
        var card = CreateCard(strong: false, accentLine: true, radius: 16);
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(14),
            BackColor = _theme.Surface
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 168));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.Controls.Add(grid);

        AddLabel(grid, "PK2 file", 0, 0, 2);
        ConfigurePathBox(_extractPk2PathBox, "Select the PK2 file to read...");
        _extractPk2PathBox.TextChanged += async (_, _) =>
        {
            InvalidateExtractPk2Scan();
            EnsureDefaultExtractOutputFolder();
            RefreshActionState();
            await PreviewExtractorPk2IfValidAsync();
        };
        grid.Controls.Add(_extractPk2PathBox, 0, 1);

        ConfigureButton(_browseExtractPk2Button, "Browse PK2", ButtonRole.Primary);
        _browseExtractPk2Button.Click += async (_, _) => await BrowseExtractorPk2FileAsync();
        grid.Controls.Add(_browseExtractPk2Button, 1, 1);

        AddLabel(grid, "Output folder", 0, 2, 2);
        ConfigurePathBox(_extractOutputFolderBox, "Select where the extracted files will be written...");
        _extractOutputFolderBox.TextChanged += (_, _) => RefreshActionState();
        grid.Controls.Add(_extractOutputFolderBox, 0, 3);

        ConfigureButton(_browseExtractOutputFolderButton, "Browse Output", ButtonRole.Primary);
        _browseExtractOutputFolderButton.Click += (_, _) => BrowseExtractOutputFolder();
        grid.Controls.Add(_browseExtractOutputFolderButton, 1, 3);

        AddLabel(grid, "Output file state", 0, 4, 2);
        ConfigureComboBox(_extractPayloadModeBox);
        _extractPayloadModeBox.Items.Clear();
        _extractPayloadModeBox.Items.AddRange(new object[] { T("Extract restored/plain files"), T("Extract raw stored payload files") });
        _extractPayloadModeBox.SelectedIndex = 0;
        _extractPayloadModeBox.Margin = new Padding(0, 8, 12, 6);
        _extractPayloadModeBox.SelectedIndexChanged += (_, _) => RefreshActionState();
        grid.Controls.Add(_extractPayloadModeBox, 0, 5);

        var help = CreateHintLabel("Plain output restores custom payload encryption; raw output keeps bytes exactly as stored.");
        grid.Controls.Add(help, 1, 5);
        return card;
    }

    private Control BuildExtractorListCard()
    {
        var card = CreateCard(strong: false, accentLine: true, radius: 16);
        card.Margin = new Padding(0, 0, 0, 12);
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(14),
            BackColor = _theme.Surface
        };
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.Controls.Add(grid);
        grid.Controls.Add(CreateSectionTitle("PK2 internal files"), 0, 0);
        ConfigureListView(_extractPreviewList);
        _extractPreviewList.SelectedIndexChanged += (_, _) => RefreshActionState();
        grid.Controls.Add(CreateListFrame(_extractPreviewList), 0, 1);
        return card;
    }

    private Control BuildExtractorActionCard()
    {
        var card = CreateCard(strong: false, accentLine: false, radius: 16);
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 1,
            Padding = new Padding(14, 12, 14, 12),
            BackColor = _theme.Surface
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 184));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 158));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 138));
        card.Controls.Add(grid);

        grid.Controls.Add(CreateHintLabel("Select one or more internal files, or extract the complete archive."), 0, 0);
        ConfigureButton(_extractSelectedButton, "Extract Selected", ButtonRole.Success);
        _extractSelectedButton.Click += async (_, _) => await StartExtractAsync(extractAll: false);
        grid.Controls.Add(_extractSelectedButton, 1, 0);
        ConfigureButton(_extractAllButton, "Extract All", ButtonRole.Success);
        _extractAllButton.Click += async (_, _) => await StartExtractAsync(extractAll: true);
        grid.Controls.Add(_extractAllButton, 3, 0);
        var clearButton = new ModernButton();
        ConfigureButton(clearButton, "Clear", ButtonRole.Secondary);
        clearButton.Click += (_, _) => _extractPreviewList.SelectedItems.Clear();
        grid.Controls.Add(clearButton, 5, 0);
        return card;
    }

    private void BuildImportPage()
    {
        _importPage.BackColor = CardBack;
        _importPage.Padding = new Padding(4);
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            BackColor = _theme.Surface
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 232));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _importPage.Controls.Add(grid);

        grid.Controls.Add(BuildImportInputCard(), 0, 0);
        grid.Controls.Add(BuildOperationInfoCard("Import / Injection", "Inject update folders into the client archive and keep payload mode consistent for GFXFileManager.", "Target\r\nExisting PK2", "Mode\r\nPlain or encrypted"), 1, 0);
        var actionCard = BuildImportActionCard();
        grid.Controls.Add(actionCard, 0, 1);
        grid.SetColumnSpan(actionCard, 2);
        var notesCard = BuildImportNotesCard();
        grid.Controls.Add(notesCard, 0, 2);
        grid.SetColumnSpan(notesCard, 2);
    }

    private Control BuildImportInputCard()
    {
        var card = CreateCard(strong: false, accentLine: true, radius: 16);
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(14),
            BackColor = _theme.Surface
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 168));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.Controls.Add(grid);

        AddLabel(grid, "Target PK2 file", 0, 0, 2);
        ConfigurePathBox(_importPk2PathBox, "Select the client .pk2 file to inject into...");
        _importPk2PathBox.TextChanged += (_, _) => RefreshActionState();
        grid.Controls.Add(_importPk2PathBox, 0, 1);

        ConfigureButton(_browseImportPk2Button, "Browse PK2", ButtonRole.Primary);
        _browseImportPk2Button.Click += (_, _) => BrowseImportPk2File();
        grid.Controls.Add(_browseImportPk2Button, 1, 1);

        AddLabel(grid, "Import source folder", 0, 2, 2);
        ConfigurePathBox(_importSourcePathBox, "Select the folder that contains the files to inject...");
        _importSourcePathBox.TextChanged += (_, _) => RefreshActionState();
        grid.Controls.Add(_importSourcePathBox, 0, 3);

        ConfigureButton(_browseImportFolderButton, "Browse Folder", ButtonRole.Primary);
        _browseImportFolderButton.Click += (_, _) => BrowseImportFolder();
        grid.Controls.Add(_browseImportFolderButton, 1, 3);

        AddLabel(grid, "Stored payload state", 0, 4, 2);
        ConfigureComboBox(_importPayloadModeBox);
        _importPayloadModeBox.Items.Clear();
        _importPayloadModeBox.Items.AddRange(new object[] { T("Store internal payloads plain"), T("Store internal payloads encrypted") });
        _importPayloadModeBox.SelectedIndex = 1;
        _importPayloadModeBox.Margin = new Padding(0, 8, 12, 6);
        _importPayloadModeBox.SelectedIndexChanged += (_, _) => RefreshActionState();
        grid.Controls.Add(_importPayloadModeBox, 0, 5);

        var help = CreateHintLabel("Encrypted mode is readable only through the matching GFXFileManager.dll.");
        grid.Controls.Add(help, 1, 5);
        return card;
    }

    private Control BuildImportActionCard()
    {
        var card = CreateCard(strong: false, accentLine: false, radius: 16);
        card.Margin = new Padding(0, 0, 0, 12);
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Padding = new Padding(14, 12, 14, 12),
            BackColor = _theme.Surface
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 188));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 146));
        card.Controls.Add(grid);
        grid.Controls.Add(CreateHintLabel("Choose Target PK2, choose Import source folder, select payload state, then Import."), 0, 0);
        ConfigureButton(_importFolderButton, "Import", ButtonRole.Success);
        _importFolderButton.Click += async (_, _) => await StartImportAsync(importFolder: true);
        grid.Controls.Add(_importFolderButton, 1, 0);
        var clearButton = new ModernButton();
        ConfigureButton(clearButton, "Clear", ButtonRole.Secondary);
        clearButton.Click += (_, _) =>
        {
            _importSourcePathBox.Clear();
            _importInternalPathBox.Clear();
        };
        grid.Controls.Add(clearButton, 3, 0);
        return card;
    }

    private Control BuildImportNotesCard()
    {
        var card = CreateCard(strong: false, accentLine: true, radius: 16);
        card.Padding = new Padding(16);
        var label = new Label
        {
            Dock = DockStyle.Fill,
            Text = T("Import notes\r\n\r\nThe selected archive is updated in-place. Choose a folder such as Data, Media, Map, Music, Particles, or a folder containing those folders. Plain/encrypted payload mode is applied consistently to the whole PK2 so GFXFileManager.dll can read it correctly."),
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 10f),
            TextAlign = ContentAlignment.TopLeft
        };
        RegisterLocalizedText(label, "Import notes\r\n\r\nThe selected archive is updated in-place. Choose a folder such as Data, Media, Map, Music, Particles, or a folder containing those folders. Plain/encrypted payload mode is applied consistently to the whole PK2 so GFXFileManager.dll can read it correctly.");
        card.Controls.Add(label);
        return card;
    }

    private void BuildBuilderPage()
    {
        _builderPage.BackColor = CardBack;
        _builderPage.Padding = new Padding(4);
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            BackColor = _theme.Surface
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 98));
        _builderPage.Controls.Add(grid);

        grid.Controls.Add(BuildBuilderInputCard(), 0, 0);
        grid.Controls.Add(BuildOperationInfoCard("Build PK2", "Create Data.pk2, Media.pk2, Map.pk2, Music.pk2, or Particles.pk2 from source folders.", "Queue\r\nKnown client folders", "Build\r\nGFX compatible"), 1, 0);
        var queueCard = BuildBuilderQueueCard();
        grid.Controls.Add(queueCard, 0, 1);
        grid.SetColumnSpan(queueCard, 2);
        var actionCard = BuildBuilderActionCard();
        grid.Controls.Add(actionCard, 0, 2);
        grid.SetColumnSpan(actionCard, 2);
    }

    private Control BuildBuilderInputCard()
    {
        var card = CreateCard(strong: false, accentLine: true, radius: 16);
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(14),
            BackColor = _theme.Surface
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 168));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.Controls.Add(grid);

        AddLabel(grid, "Source client/folder", 0, 0, 2);
        ConfigurePathBox(_builderSourceFolderBox, "Select client root, or one folder such as Media/Data/Map...");
        _builderSourceFolderBox.TextChanged += (_, _) =>
        {
            EnsureDefaultBuilderOutputFolder();
            PopulateBuilderQueue();
            RefreshActionState();
        };
        grid.Controls.Add(_builderSourceFolderBox, 0, 1);
        ConfigureButton(_browseBuilderSourceButton, "Browse Source", ButtonRole.Primary);
        _browseBuilderSourceButton.Click += (_, _) => BrowseBuilderSourceFolder();
        grid.Controls.Add(_browseBuilderSourceButton, 1, 1);

        AddLabel(grid, "Output folder", 0, 2, 2);
        ConfigurePathBox(_builderOutputFolderBox, "Select where Data.pk2 / Media.pk2 / Map.pk2 will be created...");
        _builderOutputFolderBox.TextChanged += (_, _) =>
        {
            PopulateBuilderQueue();
            RefreshActionState();
        };
        grid.Controls.Add(_builderOutputFolderBox, 0, 3);
        ConfigureButton(_browseBuilderOutputButton, "Browse Output", ButtonRole.Primary);
        _browseBuilderOutputButton.Click += (_, _) => BrowseBuilderOutputFolder();
        grid.Controls.Add(_browseBuilderOutputButton, 1, 3);
        return card;
    }

    private Control BuildBuilderQueueCard()
    {
        var card = CreateCard(strong: false, accentLine: true, radius: 16);
        card.Margin = new Padding(0, 0, 0, 12);
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(14),
            BackColor = _theme.Surface
        };
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.Controls.Add(grid);
        grid.Controls.Add(CreateSectionTitle("Builder PK2 queue"), 0, 0);
        _builderQueueList.Dock = DockStyle.Fill;
        _builderQueueList.View = View.Details;
        _builderQueueList.FullRowSelect = true;
        _builderQueueList.GridLines = false;
        _builderQueueList.CheckBoxes = true;
        _builderQueueList.HideSelection = false;
        _builderQueueList.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        _builderQueueList.BorderStyle = BorderStyle.None;
        _builderQueueList.OwnerDraw = true;
        ConfigureBuilderColumns();
        _builderQueueList.DrawColumnHeader += DrawListHeader;
        _builderQueueList.DrawSubItem += DrawBuilderSubItem;
        _builderQueueList.DrawItem += (_, e) => { if(_builderQueueList.View != View.Details) e.DrawDefault = true; };
        _builderQueueList.ShowItemToolTips = true;
        EnsureListRowHeight(_builderQueueList, 32);
        _builderQueueList.ItemChecked += (_, _) => RefreshActionState();
        _builderQueueList.DoubleClick += (_, _) => OpenSelectedBuilderSourceFolder();
        _builderQueueList.HandleCreated += (_, _) => FitBuilderColumns();
        _builderQueueList.Resize += (_, _) => FitBuilderColumns();
        if(!_themedLists.Contains(_builderQueueList))
        {
            _themedLists.Add(_builderQueueList);
        }
        grid.Controls.Add(CreateListFrame(_builderQueueList), 0, 1);
        return card;
    }

    private void DrawBuilderSubItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        if(e.ColumnIndex != 0)
        {
            var boldStatus = e.ColumnIndex == 3 && string.Equals(e.SubItem.Text, T("Ready"), StringComparison.OrdinalIgnoreCase);
            DrawListCell(e, e.SubItem.Text, e.Bounds, leftPadding: 10, bold: boldStatus);
            return;
        }

        DrawListCell(e, string.Empty, e.Bounds, leftPadding: 10, bold: false);
        var selected = e.Item.Selected;
        var textColor = selected ? Color.White : TextDark;
        var box = new Rectangle(e.Bounds.Left + 10, e.Bounds.Top + (e.Bounds.Height - 15) / 2, 15, 15);
        using(var border = new Pen(e.Item.Checked ? _theme.Accent : (_theme.IsDark ? Color.FromArgb(95, 108, 128) : Color.FromArgb(166, 188, 216)), 1.4f))
        using(var fill = new SolidBrush(e.Item.Checked ? Color.FromArgb(_theme.IsDark ? 80 : 45, _theme.Accent) : (_theme.IsDark ? Color.FromArgb(11, 15, 21) : Color.White)))
        {
            e.Graphics.FillRectangle(fill, box);
            e.Graphics.DrawRectangle(border, box);
        }
        if(e.Item.Checked)
        {
            using var mark = new Pen(_theme.Accent, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            e.Graphics.DrawLine(mark, box.Left + 3, box.Top + 8, box.Left + 6, box.Top + 11);
            e.Graphics.DrawLine(mark, box.Left + 6, box.Top + 11, box.Right - 3, box.Top + 4);
        }
        var textRect = new Rectangle(e.Bounds.Left + 34, e.Bounds.Top, Math.Max(0, e.Bounds.Width - 42), e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, e.Item.Text, new Font(_builderQueueList.Font, FontStyle.Bold), textRect, textColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    private Control BuildBuilderActionCard()
    {
        var card = CreateCard(strong: false, accentLine: false, radius: 16);
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 8,
            RowCount = 1,
            Padding = new Padding(14, 12, 14, 12),
            BackColor = _theme.Surface
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 248));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 235));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 148));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 188));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        card.Controls.Add(grid);

        ConfigureCheckBox(_builderEncryptEntriesBox, "Encrypt directory entries", true);
        _builderEncryptEntriesBox.CheckedChanged += (_, _) => RefreshActionState();
        grid.Controls.Add(_builderEncryptEntriesBox, 0, 0);
        ConfigureCheckBox(_builderEncryptPayloadsBox, "Encrypt internal payloads", true);
        _builderEncryptPayloadsBox.CheckedChanged += (_, _) => RefreshActionState();
        grid.Controls.Add(_builderEncryptPayloadsBox, 1, 0);
        grid.Controls.Add(CreateHintLabel("Encrypted payloads are decrypted by GFXFileManager.dll at runtime."), 2, 0);
        ConfigureButton(_builderRefreshButton, "Refresh", ButtonRole.Secondary);
        _builderRefreshButton.Click += (_, _) => PopulateBuilderQueue();
        grid.Controls.Add(_builderRefreshButton, 3, 0);
        ConfigureButton(_builderBuildButton, "Build selected", ButtonRole.Success);
        _builderBuildButton.Click += async (_, _) => await StartBuilderPk2Async();
        grid.Controls.Add(_builderBuildButton, 5, 0);
        var clearButton = new ModernButton();
        ConfigureButton(clearButton, "Clear", ButtonRole.Secondary);
        clearButton.Click += (_, _) =>
        {
            _builderSourceFolderBox.Clear();
            _builderOutputFolderBox.Clear();
            _builderQueueList.Items.Clear();
            RefreshActionState();
        };
        grid.Controls.Add(clearButton, 7, 0);
        return card;
    }

    private void FitBuilderColumns()
    {
        if(_builderQueueList.Columns.Count < 4 || _builderQueueList.ClientSize.Width <= 0)
        {
            return;
        }

        var available = Math.Max(640, _builderQueueList.ClientSize.Width - 6);
        var nameWidth = Math.Min(190, Math.Max(150, available / 7));
        var statusWidth = Math.Min(160, Math.Max(128, available / 9));
        var remaining = Math.Max(320, available - nameWidth - statusWidth);
        var sourceWidth = Math.Max(260, (int)(remaining * 0.48));
        var outputWidth = Math.Max(260, remaining - sourceWidth);
        var total = nameWidth + sourceWidth + outputWidth + statusWidth;
        if(total > available)
        {
            outputWidth = Math.Max(220, outputWidth - (total - available));
        }

        _builderQueueList.Columns[0].Width = nameWidth;
        _builderQueueList.Columns[1].Width = sourceWidth;
        _builderQueueList.Columns[2].Width = outputWidth;
        _builderQueueList.Columns[3].Width = Math.Max(110, available - nameWidth - sourceWidth - outputWidth);
    }

    private void ConfigureBuilderColumns()
    {
        if(_builderQueueList.Columns.Count != 4)
        {
            _builderQueueList.Columns.Clear();
            _builderQueueList.Columns.Add(string.Empty, 160);
            _builderQueueList.Columns.Add(string.Empty, 480);
            _builderQueueList.Columns.Add(string.Empty, 480);
            _builderQueueList.Columns.Add(string.Empty, 140);
        }

        _builderQueueList.Columns[0].Text = T("PK2 archive");
        _builderQueueList.Columns[1].Text = T("Source folder");
        _builderQueueList.Columns[2].Text = T("Output file");
        _builderQueueList.Columns[3].Text = T("Status");
    }

    private ModernPanel CreateCard(bool strong = false, bool accentLine = false, int radius = 16)
    {
        var panel = new ModernPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 12),
            Padding = new Padding(0),
            CornerRadius = radius,
            StrongBorder = strong,
            DrawAccentLine = accentLine,
            SurfaceColor = _theme.Surface,
            SurfaceColor2 = _theme.Surface2,
            BorderColor = _theme.Border,
            AccentColor = _theme.Accent
        };
        _cards.Add(panel);
        return panel;
    }

    private Label CreateSectionTitle(string text)
    {
        var label = new Label
        {
            Text = T(text),
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 10.25f, FontStyle.Bold),
            ForeColor = TextDark,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        RegisterLocalizedText(label, text);
        return label;
    }

    private Label CreateHintLabel(string text)
    {
        var label = new Label
        {
            Text = T(text),
            Dock = DockStyle.Fill,
            ForeColor = TextMuted,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            AutoSize = false
        };
        RegisterLocalizedText(label, text);
        return label;
    }

    private void AddLabel(TableLayoutPanel grid, string text, int column, int row, int span)
    {
        var label = new Label
        {
            Text = T(text),
            Dock = DockStyle.Fill,
            ForeColor = TextDark,
            TextAlign = ContentAlignment.BottomLeft,
            Padding = new Padding(0, 0, 0, 4),
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            AutoEllipsis = true
        };
        RegisterLocalizedText(label, text);
        grid.Controls.Add(label, column, row);
        if(span > 1)
        {
            grid.SetColumnSpan(label, span);
        }
    }

    private void ConfigurePathBox(TextBox box, string placeholder)
    {
        box.Dock = DockStyle.Fill;
        box.Margin = new Padding(0, 6, 10, 6);
        box.BorderStyle = BorderStyle.FixedSingle;
        box.MinimumSize = new Size(0, 30);
        RegisterLocalizedPlaceholder(box, placeholder);
        box.BackColor = _theme.IsDark ? Color.FromArgb(10, 10, 9) : Color.White;
        box.ForeColor = TextDark;
        box.Font = new Font("Segoe UI", 9.5f);
    }

    private void ConfigureComboBox(ComboBox combo)
    {
        combo.Dock = DockStyle.Fill;
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.FlatStyle = FlatStyle.Flat;
        combo.Font = new Font("Segoe UI Semibold", 9.3f, FontStyle.Bold);
        combo.BackColor = _theme.IsDark ? Color.FromArgb(10, 14, 20) : Color.FromArgb(255, 255, 255);
        combo.ForeColor = TextDark;
        combo.Height = 30;
        combo.ItemHeight = 24;
        if(combo is ModernComboBox modern)
        {
            modern.BorderColor = _theme.IsDark ? Color.FromArgb(78, 91, 112) : Color.FromArgb(174, 198, 229);
            modern.FocusColor = _theme.Accent;
            modern.DropBackColor = combo.BackColor;
            modern.DropTextColor = TextDark;
            modern.BorderThickness = 2;
        }
    }

    private void ConfigureCheckBox(CheckBox checkBox, string text, bool isChecked)
    {
        RegisterLocalizedText(checkBox, text);
        checkBox.Checked = isChecked;
        checkBox.AutoSize = false;
        checkBox.Width = Math.Max(170, TextRenderer.MeasureText(checkBox.Text, checkBox.Font).Width + 34);
        checkBox.Height = 30;
        checkBox.ForeColor = TextDark;
        checkBox.BackColor = _theme.Surface;
        checkBox.Margin = new Padding(0, 4, 16, 2);
        checkBox.TextAlign = ContentAlignment.MiddleLeft;
        checkBox.Font = new Font("Segoe UI", 9.3f);
    }

    private enum ButtonRole
    {
        Primary,
        Secondary,
        Success,
        Danger
    }

    private void ConfigureButton(Button button, string text, ButtonRole role)
    {
        if(!_localizedControls.TryGetValue(button, out var key))
        {
            key = text;
        }
        RegisterLocalizedText(button, key);
        button.Dock = DockStyle.Fill;
        button.Margin = new Padding(6, 8, 6, 8);
        button.UseVisualStyleBackColor = false;
        button.ForeColor = role == ButtonRole.Secondary ? TextDark : Color.White;
        button.Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold);
        button.MinimumSize = new Size(124, 44);
        button.BackColor = role switch
        {
            ButtonRole.Primary => _theme.Button,
            ButtonRole.Success => _theme.Success,
            ButtonRole.Danger => _theme.Danger,
            _ => _theme.Panel
        };

        if(button is ModernButton modern)
        {
            modern.CornerRadius = 0;
            modern.AccentColor = _theme.Accent;
            modern.ButtonColor = role switch
            {
                ButtonRole.Primary => _theme.IsDark ? Color.FromArgb(45, 56, 70) : Color.FromArgb(68, 140, 236),
                ButtonRole.Success => _theme.IsDark ? Color.FromArgb(21, 99, 68) : Color.FromArgb(38, 170, 100),
                ButtonRole.Danger => _theme.IsDark ? Color.FromArgb(104, 42, 48) : Color.FromArgb(222, 86, 92),
                _ => _theme.IsDark ? Color.FromArgb(23, 28, 36) : Color.FromArgb(245, 249, 254)
            };
            modern.ButtonColor2 = role switch
            {
                ButtonRole.Primary => _theme.IsDark ? Color.FromArgb(28, 34, 45) : Color.FromArgb(47, 119, 222),
                ButtonRole.Success => _theme.IsDark ? Color.FromArgb(13, 66, 48) : Color.FromArgb(27, 147, 82),
                ButtonRole.Danger => _theme.IsDark ? Color.FromArgb(72, 32, 36) : Color.FromArgb(203, 65, 74),
                _ => _theme.IsDark ? Color.FromArgb(16, 20, 26) : Color.FromArgb(232, 240, 250)
            };
            modern.HoverColor = role switch
            {
                ButtonRole.Primary => _theme.IsDark ? Color.FromArgb(56, 68, 86) : Color.FromArgb(87, 155, 242),
                ButtonRole.Success => _theme.IsDark ? Color.FromArgb(28, 121, 82) : Color.FromArgb(52, 184, 113),
                ButtonRole.Danger => _theme.IsDark ? Color.FromArgb(124, 51, 58) : Color.FromArgb(232, 99, 104),
                _ => _theme.IsDark ? Color.FromArgb(34, 41, 52) : Color.FromArgb(255, 255, 255)
            };
            modern.PressedColor = role switch
            {
                ButtonRole.Primary => _theme.IsDark ? Color.FromArgb(19, 25, 34) : Color.FromArgb(38, 100, 194),
                ButtonRole.Success => _theme.IsDark ? Color.FromArgb(10, 53, 39) : Color.FromArgb(21, 128, 72),
                ButtonRole.Danger => _theme.IsDark ? Color.FromArgb(56, 27, 31) : Color.FromArgb(184, 55, 64),
                _ => _theme.IsDark ? Color.FromArgb(12, 15, 20) : Color.FromArgb(218, 230, 246)
            };
            modern.BorderColor = role switch
            {
                ButtonRole.Primary => _theme.IsDark ? Color.FromArgb(88, 104, 128) : Color.FromArgb(105, 157, 232),
                ButtonRole.Success => _theme.IsDark ? Color.FromArgb(58, 150, 108) : Color.FromArgb(60, 178, 111),
                ButtonRole.Danger => _theme.IsDark ? Color.FromArgb(145, 70, 76) : Color.FromArgb(224, 103, 108),
                _ => _theme.Border
            };
        }

        switch(role)
        {
            case ButtonRole.Primary:
                if(!_primaryButtons.Contains(button)) _primaryButtons.Add(button);
                break;
            case ButtonRole.Success:
                if(!_successButtons.Contains(button)) _successButtons.Add(button);
                break;
            case ButtonRole.Danger:
                if(!_dangerButtons.Contains(button)) _dangerButtons.Add(button);
                break;
            default:
                if(!_secondaryButtons.Contains(button)) _secondaryButtons.Add(button);
                break;
        }
    }

    private void ApplyTheme(ThemeProfile theme)
    {
        if(_applyingTheme)
        {
            return;
        }

        _applyingTheme = true;
        try
        {
            _theme = theme;
            BackColor = AppBack;

            var visited = new HashSet<Control>();
            void ApplyRoot(Control root)
            {
                if(root is null || visited.Contains(root))
                {
                    return;
                }
                visited.Add(root);
                ApplyThemeToControl(root);
            }

            foreach(Control control in Controls)
            {
                ApplyRoot(control);
            }

            // Some workspace pages are detached while another page is visible. Theme them too
            // so switching page after changing theme never shows stale dark/light surfaces.
            ApplyRoot(_encryptorPage);
            ApplyRoot(_extractorPage);
            ApplyRoot(_importPage);
            ApplyRoot(_builderPage);
            ApplyRoot(_folderTab);
            ApplyRoot(_pk2Tab);

            foreach(var list in _themedLists)
            {
                list.BackColor = _theme.IsDark ? Color.FromArgb(7, 10, 14) : Color.FromArgb(252, 254, 255);
                list.ForeColor = TextDark;
                list.Invalidate();
            }

            foreach(var button in _primaryButtons) ConfigureButton(button, button.Text, ButtonRole.Primary);
            foreach(var button in _secondaryButtons) ConfigureButton(button, button.Text, ButtonRole.Secondary);
            foreach(var button in _successButtons) ConfigureButton(button, button.Text, ButtonRole.Success);
            foreach(var button in _dangerButtons) ConfigureButton(button, button.Text, ButtonRole.Danger);

            foreach(var nav in _navButtons)
            {
                nav.CornerRadius = 0;
                nav.SelectionStyle = ModernButtonSelectionStyle.LeftBar;
                nav.DrawBorder = true;
                nav.ButtonColor = _theme.IsDark ? Color.FromArgb(15, 20, 27) : Color.FromArgb(248, 251, 255);
                nav.ButtonColor2 = _theme.IsDark ? Color.FromArgb(11, 15, 21) : Color.FromArgb(238, 245, 253);
                nav.HoverColor = _theme.IsDark ? Color.FromArgb(24, 31, 42) : Color.FromArgb(235, 244, 255);
                nav.PressedColor = _theme.IsDark ? Color.FromArgb(9, 12, 18) : Color.FromArgb(222, 236, 255);
                nav.BorderColor = _theme.IsDark ? Color.FromArgb(44, 54, 68) : Color.FromArgb(198, 214, 235);
                nav.AccentColor = _theme.Accent;
                nav.ForeColor = TextDark;
            }

            foreach(var mode in _modeButtons)
            {
                mode.CornerRadius = 0;
                mode.SelectionStyle = ModernButtonSelectionStyle.SoftFill;
                mode.DrawBorder = true;
                mode.ButtonColor = _theme.IsDark ? Color.FromArgb(15, 20, 27) : Color.FromArgb(248, 251, 255);
                mode.ButtonColor2 = _theme.IsDark ? Color.FromArgb(11, 15, 21) : Color.FromArgb(238, 245, 253);
                mode.HoverColor = _theme.IsDark ? Color.FromArgb(25, 32, 42) : Color.FromArgb(235, 244, 255);
                mode.PressedColor = _theme.IsDark ? Color.FromArgb(9, 12, 18) : Color.FromArgb(222, 236, 255);
                mode.BorderColor = _theme.IsDark ? Color.FromArgb(44, 54, 68) : Color.FromArgb(198, 214, 235);
                mode.AccentColor = _theme.Accent;
                mode.ForeColor = TextDark;
            }

            if(_progressBar is ModernProgressBar progress)
            {
                progress.TrackColor = _theme.IsDark ? Color.FromArgb(14, 18, 24) : Color.FromArgb(230, 237, 245);
                progress.BarColor = _theme.Accent;
                progress.BarColor2 = _theme.Accent2;
                progress.BorderColor = _theme.Border;
            }

            if(_themeBox.Items.Count == ThemeProfiles.Length)
            {
                var index = Array.IndexOf(ThemeProfiles, theme);
                if(index >= 0 && _themeBox.SelectedIndex != index)
                {
                    _themeBox.SelectedIndex = index;
                }
            }

            _workspaceHost.BackColor = CardBack;
            _modeHost.BackColor = CardBack;
            _modeHost.Invalidate(true);
            _workspaceHost.Invalidate(true);
            UpdateNavigationState();
            UpdateModeButtonState();
            ApplyImmersiveTitleBar();
            Invalidate(true);
            Refresh();
        }
        finally
        {
            _applyingTheme = false;
        }
    }

    private void ApplyThemeToControl(Control control)
    {
        if(control is ModernPanel panel)
        {
            if(panel.Tag as string == "ListFrame")
            {
                panel.SurfaceColor = _theme.IsDark ? Color.FromArgb(7, 10, 14) : Color.FromArgb(252, 254, 255);
                panel.SurfaceColor2 = panel.SurfaceColor;
                panel.BorderColor = _theme.IsDark ? Color.FromArgb(84, 98, 118) : Color.FromArgb(174, 198, 229);
                panel.AccentColor = _theme.Accent;
                panel.DrawAccentLine = true;
                panel.StrongBorder = true;
            }
            else
            {
                panel.SurfaceColor = _theme.Surface;
                panel.SurfaceColor2 = _theme.Surface2;
                panel.BorderColor = _theme.Border;
                panel.AccentColor = _theme.Accent;
            }
            SafeSetBackColor(panel, ModernUiPaint.OpaqueParentBack(panel, AppBack));
        }
        else if(control is TableLayoutPanel or FlowLayoutPanel)
        {
            SafeSetBackColor(control, control.Parent is ModernPanel modernParent ? modernParent.SurfaceColor : ModernUiPaint.OpaqueParentBack(control, CardBack));
        }
        else if(control is ListView list)
        {
            list.BackColor = _theme.IsDark ? Color.FromArgb(7, 10, 14) : Color.FromArgb(252, 254, 255);
            list.ForeColor = TextDark;
            list.BorderStyle = BorderStyle.None;
        }
        else if(control is Panel && control is not ModernPanel)
        {
            if(control.Tag as string == "Divider")
            {
                SafeSetBackColor(control, _theme.Border);
                control.ForeColor = TextDark;
                foreach(Control child in control.Controls)
                {
                    ApplyThemeToControl(child);
                }
                return;
            }

            var desiredBack = ReferenceEquals(control, _workspaceHost) || ReferenceEquals(control, _modeHost) ||
                              ReferenceEquals(control, _encryptorPage) || ReferenceEquals(control, _extractorPage) ||
                              ReferenceEquals(control, _importPage) || ReferenceEquals(control, _builderPage) ||
                              ReferenceEquals(control, _folderTab) || ReferenceEquals(control, _pk2Tab)
                ? CardBack
                : (control.Parent is ModernPanel parentCard ? parentCard.SurfaceColor : ModernUiPaint.OpaqueParentBack(control, CardBack));
            SafeSetBackColor(control, desiredBack);
        }
        else if(control is Label label)
        {
            if(label.Text == "◆")
            {
                label.ForeColor = _theme.Accent;
            }
            else if(label.Text == "●")
            {
                label.ForeColor = _theme.Success;
            }
            else if(label.Text.Contains("Build • Extract", StringComparison.Ordinal))
            {
                label.ForeColor = Color.FromArgb(_theme.IsDark ? 170 : 190, _theme.Accent);
            }
            else
            {
                label.ForeColor = label.Font.Bold ? TextDark : TextMuted;
            }
            SafeSetBackColor(label, ModernUiPaint.OpaqueParentBack(label, AppBack));
        }
        else if(control is TextBox textBox)
        {
            SafeSetBackColor(textBox, _theme.IsDark ? Color.FromArgb(9, 12, 17) : Color.White);
            textBox.ForeColor = TextDark;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }
        else if(control is ComboBox combo)
        {
            ConfigureComboBox(combo);
            SafeSetBackColor(combo, _theme.IsDark ? Color.FromArgb(10, 14, 20) : Color.White);
        }
        else if(control is CheckBox check)
        {
            SafeSetBackColor(check, ModernUiPaint.OpaqueParentBack(check, AppBack));
            check.ForeColor = TextDark;
        }
        else if(control is ModernProgressBar progress)
        {
            progress.TrackColor = _theme.IsDark ? Color.FromArgb(14, 18, 24) : Color.FromArgb(230, 237, 245);
            progress.BarColor = _theme.Accent;
            progress.BarColor2 = _theme.Accent2;
            progress.BorderColor = _theme.Border;
            SafeSetBackColor(progress, ModernUiPaint.OpaqueParentBack(progress, CardBack));
        }
        else if(control is Button button)
        {
            button.UseVisualStyleBackColor = false;
            button.ForeColor = TextDark;
            SafeSetBackColor(button, ModernUiPaint.OpaqueParentBack(button, CardBack));
        }
        else
        {
            SafeSetBackColor(control, ModernUiPaint.OpaqueParentBack(control, AppBack));
            control.ForeColor = TextDark;
        }

        foreach(Control child in control.Controls)
        {
            ApplyThemeToControl(child);
        }
    }

    private static void SafeSetBackColor(Control control, Color color)
    {
        try
        {
            if(color == Color.Empty || color == Color.Transparent || color.A == 0)
            {
                color = SystemColors.Control;
            }
            control.BackColor = color;
        }
        catch
        {
            try
            {
                control.BackColor = SystemColors.Control;
            }
            catch
            {
            }
        }
    }

    #endregion
}
