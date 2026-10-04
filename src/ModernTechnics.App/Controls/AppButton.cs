using ModernTechnics.App.Ui;

namespace ModernTechnics.App.Controls;

internal enum ButtonKind
{
    Primary,
    Secondary,
    Danger,
    Ghost,
}

/// <summary>Flat rounded button with an optional leading icon.</summary>
internal sealed class AppButton : Button
{
    private bool _hover;
    private bool _pressed;

    public AppButton(string text, ButtonKind kind = ButtonKind.Secondary, string? glyph = null)
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        Kind = kind;
        Glyph = glyph;
        Font = Theme.BodyBold;
        Cursor = Cursors.Hand;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Text = text;
        Height = Theme.Px(38);
        FitWidth();
    }

    public ButtonKind Kind { get; }

    public string? Glyph { get; }

    public void FitWidth()
    {
        var textWidth = TextRenderer.MeasureText(Text, Font).Width;
        Width = textWidth + Theme.Px(Glyph is null ? 28 : 50);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        _pressed = true;
        Invalidate();
        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(mevent);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var graphics = pevent.Graphics;
        graphics.Smooth();
        graphics.Clear(Parent?.BackColor ?? Theme.Surface);

        var (fill, border, text) = Palette();
        var bounds = new RectangleF(0, 0, Width, Height);
        var radius = Theme.Px(8f);

        graphics.FillRounded(fill, bounds, radius);
        if (border != Color.Empty)
        {
            graphics.StrokeRounded(border, bounds, radius);
        }

        if (Focused && ShowFocusCues)
        {
            graphics.StrokeRounded(Theme.AccentMuted, bounds, radius, Theme.Px(2f));
        }

        var content = ClientRectangle;
        if (Glyph is not null)
        {
            var textWidth = TextRenderer.MeasureText(Text, Font).Width;
            var iconWidth = Theme.Px(22);
            var start = (Width - (iconWidth + textWidth)) / 2;
            graphics.Glyph(Glyph, 10.5f, text, new Rectangle(start, 0, iconWidth, Height));
            content = new Rectangle(start + iconWidth, 0, textWidth, Height);
        }

        TextRenderer.DrawText(graphics, Text, Font, content, text, Draw.Center);
    }

    private (Color Fill, Color Border, Color Text) Palette()
    {
        if (!Enabled)
        {
            return Kind is ButtonKind.Primary or ButtonKind.Danger
                ? (Theme.BorderStrong, Color.Empty, Color.White)
                : (Theme.Subtle, Theme.Border, Theme.BorderStrong);
        }

        return Kind switch
        {
            ButtonKind.Primary => (
                _pressed ? Theme.AccentPressed : _hover ? Theme.AccentHover : Theme.Accent, Color.Empty, Color.White),
            ButtonKind.Danger => (
                _pressed || _hover ? Theme.DangerSoft : Theme.Card,
                _hover ? Theme.Danger : Theme.BorderStrong, Theme.Danger),
            ButtonKind.Ghost => (
                _pressed ? Theme.AccentMuted : _hover ? Theme.AccentSoft : Parent?.BackColor ?? Theme.Card,
                Color.Empty, Theme.Accent),
            _ => (
                _pressed ? Theme.Border : _hover ? Theme.Subtle : Theme.Card,
                _hover ? Theme.AccentMuted : Theme.BorderStrong, Theme.Text),
        };
    }
}
