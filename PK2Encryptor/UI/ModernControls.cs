using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PK2Encryptor;

internal enum ModernButtonSelectionStyle
{
    Outline,
    LeftBar,
    BottomBar,
    SoftFill
}

internal static class ModernUiPaint
{
    internal static Color OpaqueParentBack(Control? control, Color fallback)
    {
        var current = control?.Parent;
        while(current is not null)
        {
            var color = current.BackColor;
            if(color != Color.Empty && color.A > 0 && color != Color.Transparent)
            {
                return color;
            }
            current = current.Parent;
        }
        return fallback;
    }

    internal static Color Blend(Color first, Color second, int secondPercent)
    {
        var b = Math.Max(0, Math.Min(100, secondPercent));
        var a = 100 - b;
        return Color.FromArgb(
            (first.R * a + second.R * b) / 100,
            (first.G * a + second.G * b) / 100,
            (first.B * a + second.B * b) / 100);
    }

    internal static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        if(bounds.Width <= 0 || bounds.Height <= 0)
        {
            return path;
        }

        radius = Math.Max(0, Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2));
        if(radius <= 0)
        {
            path.AddRectangle(bounds);
            path.CloseFigure();
            return path;
        }

        var d = radius * 2;
        path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class ModernPanel : Panel
{
    private int _cornerRadius = 16;
    private Color _borderColor = Color.FromArgb(64, 218, 175, 55);
    private Color _surfaceColor = Color.FromArgb(22, 22, 20);
    private Color _surfaceColor2 = Color.FromArgb(12, 12, 11);
    private Color _accentColor = Color.FromArgb(218, 175, 55);
    private bool _drawAccentLine;
    private bool _strongBorder;

    public ModernPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        BackColor = SystemColors.Control;
        Padding = new Padding(1);
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CornerRadius
    {
        get => _cornerRadius;
        set { _cornerRadius = Math.Max(0, value); Invalidate(); }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor
    {
        get => _borderColor;
        set { _borderColor = value; Invalidate(); }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color SurfaceColor
    {
        get => _surfaceColor;
        set { _surfaceColor = value; Invalidate(true); }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color SurfaceColor2
    {
        get => _surfaceColor2;
        set { _surfaceColor2 = value; Invalidate(true); }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color AccentColor
    {
        get => _accentColor;
        set { _accentColor = value; Invalidate(); }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool DrawAccentLine
    {
        get => _drawAccentLine;
        set { _drawAccentLine = value; Invalidate(); }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool StrongBorder
    {
        get => _strongBorder;
        set { _strongBorder = value; Invalidate(); }
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var back = new SolidBrush(ModernUiPaint.OpaqueParentBack(this, BackColor));
        e.Graphics.FillRectangle(back, ClientRectangle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        try
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            var rect = Rectangle.Inflate(ClientRectangle, -1, -1);
            if(rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            using var path = ModernUiPaint.RoundedRect(rect, _cornerRadius);
            using(var brush = new LinearGradientBrush(rect, _surfaceColor, _surfaceColor2, LinearGradientMode.Vertical))
            {
                e.Graphics.FillPath(brush, path);
            }

            if(_drawAccentLine)
            {
                using var accent = new Pen(Color.FromArgb(_strongBorder ? 190 : 130, _accentColor), _strongBorder ? 2.0f : 1.2f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                };
                var y = rect.Top + 1.0f;
                e.Graphics.DrawLine(accent, rect.Left + _cornerRadius, y, rect.Right - _cornerRadius, y);
            }

            using(var pen = new Pen(_borderColor, _strongBorder ? 1.35f : 1.0f))
            {
                pen.LineJoin = LineJoin.Round;
                e.Graphics.DrawPath(pen, path);
            }
        }
        catch
        {
            base.OnPaint(e);
        }
    }

    internal static GraphicsPath RoundedRect(Rectangle bounds, int radius) => ModernUiPaint.RoundedRect(bounds, radius);
}

internal class ModernButton : Button
{
    private int _cornerRadius = 12;
    private Color _buttonColor = Color.FromArgb(54, 43, 26);
    private Color _buttonColor2 = Color.FromArgb(34, 28, 18);
    private Color _borderColor = Color.FromArgb(146, 116, 35);
    private Color _hoverColor = Color.FromArgb(74, 58, 30);
    private Color _pressedColor = Color.FromArgb(28, 24, 18);
    private Color _accentColor = Color.FromArgb(218, 175, 55);
    private ModernButtonSelectionStyle _selectionStyle = ModernButtonSelectionStyle.Outline;
    private bool _selected;
    private bool _useAccentTextWhenSelected;
    private bool _drawBorder = true;

    public ModernButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseDownBackColor = Color.Transparent;
        FlatAppearance.MouseOverBackColor = Color.Transparent;
        UseVisualStyleBackColor = false;
        Cursor = Cursors.Hand;
        TextAlign = ContentAlignment.MiddleCenter;
        TabStop = false;
        BackColor = SystemColors.Control;
    }

    protected override bool ShowFocusCues => false;

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); Invalidate(); }
    protected override void OnMouseDown(MouseEventArgs mevent) { base.OnMouseDown(mevent); Invalidate(); }
    protected override void OnMouseUp(MouseEventArgs mevent) { base.OnMouseUp(mevent); Invalidate(); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CornerRadius
    {
        get => _cornerRadius;
        set { _cornerRadius = Math.Max(0, value); Invalidate(); }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ButtonColor
    {
        get => _buttonColor;
        set { _buttonColor = value; Invalidate(); }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ButtonColor2
    {
        get => _buttonColor2;
        set { _buttonColor2 = value; Invalidate(); }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor
    {
        get => _borderColor;
        set { _borderColor = value; Invalidate(); }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HoverColor
    {
        get => _hoverColor;
        set { _hoverColor = value; Invalidate(); }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color PressedColor
    {
        get => _pressedColor;
        set { _pressedColor = value; Invalidate(); }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color AccentColor
    {
        get => _accentColor;
        set { _accentColor = value; Invalidate(); }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ModernButtonSelectionStyle SelectionStyle
    {
        get => _selectionStyle;
        set { _selectionStyle = value; Invalidate(); }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool UseAccentTextWhenSelected
    {
        get => _useAccentTextWhenSelected;
        set { _useAccentTextWhenSelected = value; Invalidate(); }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool DrawBorder
    {
        get => _drawBorder;
        set { _drawBorder = value; Invalidate(); }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Selected
    {
        get => _selected;
        set { _selected = value; Invalidate(); }
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        using var back = new SolidBrush(ModernUiPaint.OpaqueParentBack(this, BackColor));
        pevent.Graphics.FillRectangle(back, ClientRectangle);
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        // ProgressBar does not automatically repaint reliably when UserPaint is enabled.
        // Force a repaint after native progress messages so long PK2 jobs show live movement.
        const int WM_USER = 0x0400;
        const int PBM_SETPOS = WM_USER + 2;
        const int PBM_DELTAPOS = WM_USER + 3;
        const int PBM_SETSTEP = WM_USER + 4;
        const int PBM_STEPIT = WM_USER + 5;
        const int PBM_SETRANGE32 = WM_USER + 6;
        if(m.Msg == PBM_SETPOS || m.Msg == PBM_DELTAPOS || m.Msg == PBM_SETSTEP || m.Msg == PBM_STEPIT || m.Msg == PBM_SETRANGE32)
        {
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        try
        {
            e.Graphics.SmoothingMode = SmoothingMode.None;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Default;
            e.Graphics.CompositingMode = CompositingMode.SourceOver;

            var parentBack = ModernUiPaint.OpaqueParentBack(this, BackColor);
            using(var outside = new SolidBrush(parentBack))
            {
                e.Graphics.FillRectangle(outside, ClientRectangle);
            }

            if(Width <= 3 || Height <= 3)
            {
                return;
            }

            // Draw every button as a clean, true rectangle.  The small inset prevents
            // neighbouring buttons from touching and avoids the broken corner artefacts
            // that WinForms can leave when custom controls repaint during theme changes.
            var rect = new Rectangle(1, 1, Width - 3, Height - 3);
            var mouseInside = ClientRectangle.Contains(PointToClient(Cursor.Position));
            var activeColor = !Enabled
                ? ControlPaint.Dark(_buttonColor, 0.35f)
                : (Capture && mouseInside ? _pressedColor : (mouseInside ? _hoverColor : _buttonColor));
            var activeColor2 = !Enabled
                ? ControlPaint.Dark(_buttonColor2, 0.35f)
                : _buttonColor2;

            if(_selected && Enabled)
            {
                activeColor = ModernUiPaint.Blend(activeColor, _accentColor, _selectionStyle == ModernButtonSelectionStyle.SoftFill ? 18 : 7);
                activeColor2 = ModernUiPaint.Blend(activeColor2, _accentColor, _selectionStyle == ModernButtonSelectionStyle.SoftFill ? 10 : 4);
            }

            using(var brush = new LinearGradientBrush(rect, activeColor, activeColor2, LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(brush, rect);
            }

            if(_selected && Enabled && _selectionStyle == ModernButtonSelectionStyle.SoftFill)
            {
                using var overlay = new SolidBrush(Color.FromArgb(18, _accentColor));
                e.Graphics.FillRectangle(overlay, Rectangle.Inflate(rect, -1, -1));
            }

            if(_drawBorder)
            {
                var selectedOutline = _selected && Enabled && (_selectionStyle == ModernButtonSelectionStyle.Outline || _selectionStyle == ModernButtonSelectionStyle.SoftFill);
                var borderColor = selectedOutline ? _accentColor : (!Enabled ? ControlPaint.Dark(_borderColor, 0.45f) : _borderColor);
                using var border = new Pen(borderColor, selectedOutline ? 2f : 1f);
                e.Graphics.DrawRectangle(border, rect);
            }

            if(_selected && Enabled)
            {
                using var accentPen = new Pen(Color.FromArgb(245, _accentColor), 3.0f);
                switch(_selectionStyle)
                {
                    case ModernButtonSelectionStyle.LeftBar:
                        e.Graphics.DrawLine(accentPen, rect.Left + 2, rect.Top + 5, rect.Left + 2, rect.Bottom - 5);
                        break;
                    case ModernButtonSelectionStyle.BottomBar:
                        e.Graphics.DrawLine(accentPen, rect.Left + 14, rect.Bottom - 3, rect.Right - 14, rect.Bottom - 3);
                        break;
                    case ModernButtonSelectionStyle.SoftFill:
                        using(var soft = new Pen(Color.FromArgb(170, _accentColor), 1f))
                        {
                            e.Graphics.DrawRectangle(soft, Rectangle.Inflate(rect, -3, -3));
                        }
                        break;
                }
            }

            var textColor = Enabled ? ForeColor : Color.FromArgb(120, ForeColor);
            if(_selected && _useAccentTextWhenSelected)
            {
                textColor = _accentColor;
            }

            var textRect = new Rectangle(
                rect.Left + Padding.Left,
                rect.Top + Padding.Top,
                Math.Max(0, rect.Width - Padding.Left - Padding.Right),
                Math.Max(0, rect.Height - Padding.Top - Padding.Bottom));
            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
            flags |= TextAlign is ContentAlignment.MiddleLeft or ContentAlignment.TopLeft or ContentAlignment.BottomLeft
                ? TextFormatFlags.Left
                : TextAlign is ContentAlignment.MiddleRight or ContentAlignment.TopRight or ContentAlignment.BottomRight
                    ? TextFormatFlags.Right
                    : TextFormatFlags.HorizontalCenter;
            TextRenderer.DrawText(e.Graphics, Text, Font, textRect, textColor, flags);
        }
        catch
        {
            base.OnPaint(e);
        }
    }

}


internal sealed class ModernComboBox : ComboBox
{
    private Color _borderColor = Color.FromArgb(180, 204, 235);
    private Color _focusColor = Color.FromArgb(39, 112, 232);
    private Color _dropBackColor = Color.White;
    private Color _dropTextColor = Color.FromArgb(23, 34, 49);
    private int _borderThickness = 1;

    public ModernComboBox()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat;
        DrawMode = DrawMode.OwnerDrawFixed;
        DropDownStyle = ComboBoxStyle.DropDownList;
        ItemHeight = 24;
        IntegralHeight = false;
        DropDownHeight = 160;
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor
    {
        get => _borderColor;
        set { _borderColor = value; Invalidate(); }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FocusColor
    {
        get => _focusColor;
        set { _focusColor = value; Invalidate(); }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color DropBackColor
    {
        get => _dropBackColor;
        set { _dropBackColor = value; Invalidate(); }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color DropTextColor
    {
        get => _dropTextColor;
        set { _dropTextColor = value; Invalidate(); }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int BorderThickness
    {
        get => _borderThickness;
        set { _borderThickness = Math.Max(1, Math.Min(3, value)); Invalidate(); }
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if(e.Index < 0)
        {
            e.DrawBackground();
            return;
        }

        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var backColor = selected ? _focusColor : _dropBackColor;
        var textColor = selected ? Color.White : _dropTextColor;
        using(var back = new SolidBrush(backColor))
        {
            e.Graphics.FillRectangle(back, e.Bounds);
        }

        var text = GetItemText(Items[e.Index]);
        var rect = new Rectangle(e.Bounds.Left + 8, e.Bounds.Top, Math.Max(0, e.Bounds.Width - 12), e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, text, Font, rect, textColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
    protected override void OnSelectedIndexChanged(EventArgs e) { base.OnSelectedIndexChanged(e); Invalidate(); }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        const int wmPaint = 0x000F;
        const int wmNcPaint = 0x0085;
        if(m.Msg == wmPaint || m.Msg == wmNcPaint)
        {
            DrawModernBorder();
        }
    }

    private void DrawModernBorder()
    {
        if(Width <= 1 || Height <= 1 || !IsHandleCreated)
        {
            return;
        }

        try
        {
            using var g = Graphics.FromHwnd(Handle);
            var color = Focused ? _focusColor : _borderColor;
            for(var i = 0; i < _borderThickness; i++)
            {
                using var pen = new Pen(color);
                g.DrawRectangle(pen, new Rectangle(i, i, Width - 1 - (i * 2), Height - 1 - (i * 2)));
            }
        }
        catch
        {
            // Native ComboBox painting can race during handle recreation; ignore.
        }
    }
}

internal sealed class ModernProgressBar : ProgressBar
{
    private Color _trackColor = Color.FromArgb(30, 30, 28);
    private Color _barColor = Color.FromArgb(218, 175, 55);
    private Color _barColor2 = Color.FromArgb(255, 224, 126);
    private Color _borderColor = Color.FromArgb(75, 62, 32);
    private int _cornerRadius = 8;

    public ModernProgressBar()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Minimum = 0;
        Maximum = 10000;
        BackColor = SystemColors.Control;
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color TrackColor
    {
        get => _trackColor;
        set { _trackColor = value; Invalidate(); }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BarColor
    {
        get => _barColor;
        set { _barColor = value; Invalidate(); }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BarColor2
    {
        get => _barColor2;
        set { _barColor2 = value; Invalidate(); }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor
    {
        get => _borderColor;
        set { _borderColor = value; Invalidate(); }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CornerRadius
    {
        get => _cornerRadius;
        set { _cornerRadius = Math.Max(0, value); Invalidate(); }
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        using var back = new SolidBrush(ModernUiPaint.OpaqueParentBack(this, BackColor));
        pevent.Graphics.FillRectangle(back, ClientRectangle);
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        // ProgressBar does not automatically repaint reliably when UserPaint is enabled.
        // Force a repaint after native progress messages so long PK2 jobs show live movement.
        const int WM_USER = 0x0400;
        const int PBM_SETPOS = WM_USER + 2;
        const int PBM_DELTAPOS = WM_USER + 3;
        const int PBM_SETSTEP = WM_USER + 4;
        const int PBM_STEPIT = WM_USER + 5;
        const int PBM_SETRANGE32 = WM_USER + 6;
        if(m.Msg == PBM_SETPOS || m.Msg == PBM_DELTAPOS || m.Msg == PBM_SETSTEP || m.Msg == PBM_STEPIT || m.Msg == PBM_SETRANGE32)
        {
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        try
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            var rect = Rectangle.Inflate(ClientRectangle, -1, -1);
            if(rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            using var path = ModernUiPaint.RoundedRect(rect, _cornerRadius);
            using(var track = new SolidBrush(_trackColor))
            {
                e.Graphics.FillPath(track, path);
            }
            using(var border = new Pen(_borderColor))
            {
                border.LineJoin = LineJoin.Round;
                e.Graphics.DrawPath(border, path);
            }

            if(Maximum <= Minimum || Value <= Minimum)
            {
                return;
            }

            var pct = (double)(Value - Minimum) / (Maximum - Minimum);
            var fillWidth = (int)Math.Round((rect.Width - 2) * pct);
            if(fillWidth <= 0)
            {
                return;
            }

            var fill = new Rectangle(rect.Left + 1, rect.Top + 1, Math.Min(fillWidth, rect.Width - 2), rect.Height - 2);
            using var fillPath = ModernUiPaint.RoundedRect(fill, Math.Min(_cornerRadius, fill.Height / 2));
            using(var brush = new LinearGradientBrush(fill, _barColor, _barColor2, LinearGradientMode.Horizontal))
            {
                e.Graphics.FillPath(brush, fillPath);
            }
        }
        catch
        {
            base.OnPaint(e);
        }
    }
}
