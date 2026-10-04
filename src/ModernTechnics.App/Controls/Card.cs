using ModernTechnics.App.Ui;

namespace ModernTechnics.App.Controls;

/// <summary>White rounded container with an optional heading.</summary>
internal class Card : Panel
{
    private string? _title;

    public Card()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Card;
        ForeColor = Theme.Text;
        Padding = Theme.Pad(20);
    }

    public string? Title
    {
        get => _title;
        set
        {
            _title = value;
            Padding = value is null ? Theme.Pad(20) : Theme.Pad(20, 56, 20, 20);
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.Smooth();
        graphics.Clear(Parent?.BackColor ?? Theme.Surface);

        var bounds = new RectangleF(0, 0, Width, Height);
        var radius = Theme.Px(12f);
        graphics.FillRounded(BackColor, bounds, radius);
        graphics.StrokeRounded(Theme.Border, bounds, radius);

        if (_title is not null)
        {
            var titleBounds = new Rectangle(Theme.Px(20), Theme.Px(14), Width - Theme.Px(40), Theme.Px(30));
            TextRenderer.DrawText(graphics, _title, Theme.H2, titleBounds, Theme.Text, Draw.Left);
        }

        PaintContent(graphics);
    }

    protected virtual void PaintContent(Graphics graphics)
    {
    }
}
