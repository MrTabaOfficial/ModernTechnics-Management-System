using ModernTechnics.App.Ui;

namespace ModernTechnics.App.Controls;

/// <summary>Sidebar navigation entry.</summary>
internal sealed class NavButton : Control
{
    private readonly string _glyph;
    private bool _hover;
    private bool _active;

    public NavButton(string text, string glyph)
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
            | ControlStyles.Selectable, true);

        _glyph = glyph;
        Text = text;
        Font = Theme.Body;
        Cursor = Cursors.Hand;
        TabStop = true;
        Height = Theme.Px(42);
        Margin = new Padding(0, 0, 0, Theme.Px(4));
        AccessibleRole = AccessibleRole.MenuItem;
        AccessibleName = text;
    }

    public bool Active
    {
        get => _active;
        set
        {
            _active = value;
            Invalidate();
        }
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
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        Invalidate();
        base.OnLostFocus(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Enter or Keys.Space)
        {
            OnClick(EventArgs.Empty);
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.Smooth();
        graphics.Clear(Theme.Sidebar);

        var bounds = new RectangleF(0, 0, Width, Height);
        if (_active || _hover || (Focused && ShowFocusCues))
        {
            graphics.FillRounded(_active ? Theme.SidebarActive : Theme.SidebarHover, bounds, Theme.Px(8f));
        }

        if (_active)
        {
            var bar = new RectangleF(0, Theme.Px(11f), Theme.Px(3f), Height - Theme.Px(22f));
            graphics.FillRounded(Theme.Accent, bar, Theme.Px(1.5f));
        }

        var color = _active ? Theme.OnSidebar : Theme.OnSidebarMuted;
        graphics.Glyph(_glyph, 11.5f, color, new Rectangle(Theme.Px(12), 0, Theme.Px(28), Height));

        var textBounds = new Rectangle(Theme.Px(48), 0, Width - Theme.Px(56), Height);
        TextRenderer.DrawText(
            graphics, Text, _active ? Theme.BodyBold : Theme.Body, textBounds, color, Draw.Left);
    }
}
