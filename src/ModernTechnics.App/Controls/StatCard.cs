using ModernTechnics.App.Ui;

namespace ModernTechnics.App.Controls;

/// <summary>Dashboard tile: an icon badge, a headline figure and a caption.</summary>
internal sealed class StatCard : Card
{
    private readonly string _glyph;
    private readonly Color _tint;
    private readonly Color _tintSoft;
    private string _value = "—";
    private string _caption = string.Empty;

    public StatCard(string label, string glyph, Color tint, Color tintSoft)
    {
        Label = label;
        _glyph = glyph;
        _tint = tint;
        _tintSoft = tintSoft;
        Height = Theme.Px(124);
    }

    public string Label { get; }

    public void Show(string value, string caption)
    {
        _value = value;
        _caption = caption;
        Invalidate();
    }

    protected override void PaintContent(Graphics graphics)
    {
        var pad = Theme.Px(20);
        var badgeSize = Theme.Px(40);
        var badge = new Rectangle(Width - pad - badgeSize, pad, badgeSize, badgeSize);
        graphics.FillRounded(_tintSoft, badge, Theme.Px(10f));
        graphics.Glyph(_glyph, 13f, _tint, badge);

        var textWidth = Width - (pad * 3) - badgeSize;
        TextRenderer.DrawText(
            graphics, Label, Theme.Small, new Rectangle(pad, pad - Theme.Px(2), textWidth, Theme.Px(20)),
            Theme.TextMuted, Draw.Left);
        TextRenderer.DrawText(
            graphics, _value, Theme.Display,
            new Rectangle(pad - Theme.Px(2), pad + Theme.Px(20), Width - (pad * 2), Theme.Px(44)),
            Theme.Text, Draw.Left);
        TextRenderer.DrawText(
            graphics, _caption, Theme.Small,
            new Rectangle(pad, Height - pad - Theme.Px(18), Width - (pad * 2), Theme.Px(20)),
            Theme.TextMuted, Draw.Left);
    }
}
