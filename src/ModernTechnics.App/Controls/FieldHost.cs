using ModernTechnics.App.Ui;

namespace ModernTechnics.App.Controls;

/// <summary>
/// Gives any native input (text box, combo box, date picker…) the same rounded frame,
/// focus ring and error state. The native border is hidden by clipping it away.
/// </summary>
internal sealed class FieldHost : Control
{
    private readonly Panel _clip = new();
    private readonly Control _inner;
    private readonly int _clipInset;
    private readonly bool _stretch;
    private readonly int _verticalInset;
    private bool _hasError;

    public FieldHost(Control inner, string? glyph = null, int clipInset = 0, bool stretch = false, int verticalInset = 4)
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        _inner = inner;
        _clipInset = Theme.Px(clipInset);
        _stretch = stretch;
        _verticalInset = verticalInset;
        Glyph = glyph;

        BackColor = Theme.Card;
        Height = Theme.Px(38);

        inner.Font = Theme.Body;
        inner.ForeColor = Theme.Text;
        inner.BackColor = Fill;
        inner.Enter += (_, _) => Invalidate();
        inner.Leave += (_, _) => Invalidate();
        inner.EnabledChanged += (_, _) => Invalidate();

        _clip.BackColor = Fill;
        _clip.Controls.Add(inner);
        Controls.Add(_clip);
    }

    public string? Glyph { get; }

    public bool HasError
    {
        get => _hasError;
        set
        {
            _hasError = value;
            Invalidate();
        }
    }

    private Color Fill => _inner is TextBoxBase { ReadOnly: true } ? Theme.Subtle : Theme.Card;

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);

        var left = Theme.Px(Glyph is null ? 12 : 38);
        var vertical = Theme.Px(_verticalInset);
        _clip.BackColor = Fill;
        _inner.BackColor = Fill;
        _clip.Bounds = new Rectangle(left, vertical, Width - left - Theme.Px(12), Height - (vertical * 2));

        if (_stretch)
        {
            _inner.Bounds = new Rectangle(
                -_clipInset, -_clipInset, _clip.Width + (_clipInset * 2), _clip.Height + (_clipInset * 2));
            return;
        }

        // Native inputs choose their own height. Shrink the clip window around the control
        // so exactly its border is cut off, then centre that window in the frame.
        _inner.Bounds = new Rectangle(-_clipInset, -_clipInset, _clip.Width + (_clipInset * 2), _inner.PreferredSize.Height);
        var visible = Math.Min(_inner.Height - (_clipInset * 2), _clip.Height);
        _clip.Bounds = new Rectangle(_clip.Left, (Height - visible) / 2, _clip.Width, visible);
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        _inner.Focus();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.Smooth();
        graphics.Clear(Parent?.BackColor ?? Theme.Card);

        var bounds = new RectangleF(0, 0, Width, Height);
        var radius = Theme.Px(8f);
        var focused = _inner.ContainsFocus;

        graphics.FillRounded(Fill, bounds, radius);
        graphics.StrokeRounded(
            _hasError ? Theme.Danger : focused ? Theme.Accent : Theme.BorderStrong,
            bounds, radius, focused || _hasError ? Theme.Px(1.6f) : 1f);

        if (Glyph is not null)
        {
            graphics.Glyph(
                Glyph, 10.5f, focused ? Theme.Accent : Theme.TextMuted,
                new Rectangle(Theme.Px(8), 0, Theme.Px(28), Height));
        }
    }
}
