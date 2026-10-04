using ModernTechnics.App.Ui;

namespace ModernTechnics.App.Controls;

/// <summary>Brief confirmation that slides out of the way on its own.</summary>
internal sealed class Toast : Control
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 2800 };

    public Toast()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Font = Theme.BodyBold;
        Height = Theme.Px(44);
        Visible = false;
        TabStop = false;
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            Visible = false;
        };
    }

    public void Show(string message)
    {
        Text = message;
        Width = TextRenderer.MeasureText(message, Font).Width + Theme.Px(64);
        if (Parent is { } parent)
        {
            Location = new Point(parent.ClientSize.Width - Width - Theme.Px(28), parent.ClientSize.Height - Height - Theme.Px(24));
        }

        Visible = true;
        BringToFront();
        Invalidate();
        _timer.Stop();
        _timer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.Smooth();
        graphics.Clear(Theme.Surface);
        graphics.FillRounded(Theme.Sidebar, new RectangleF(0, 0, Width, Height), Theme.Px(10f));

        var badge = new Rectangle(Theme.Px(12), (Height - Theme.Px(22)) / 2, Theme.Px(22), Theme.Px(22));
        graphics.FillRounded(Theme.Success, badge, badge.Width / 2f);
        graphics.Glyph(Glyphs.Check, 8.5f, Color.White, badge);

        TextRenderer.DrawText(
            graphics, Text, Font, new Rectangle(Theme.Px(44), 0, Width - Theme.Px(56), Height), Color.White, Draw.Left);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
        }

        base.Dispose(disposing);
    }
}
