using System.Drawing.Drawing2D;

namespace ModernTechnics.App.Ui;

/// <summary>The application mark, drawn in code so it stays sharp at any size.</summary>
internal static class Brand
{
    public static void DrawMark(Graphics graphics, RectangleF bounds)
    {
        graphics.Smooth();
        using (var gradient = new LinearGradientBrush(
            bounds, Color.FromArgb(0x9B, 0x7D, 0xFF), Theme.AccentHover, LinearGradientMode.ForwardDiagonal))
        using (var path = Draw.RoundedPath(bounds, bounds.Width * 0.28f))
        {
            graphics.FillPath(gradient, path);
        }

        // A stylised "M": two peaks that also read as a rising chart.
        var unit = bounds.Width / 10f;
        PointF[] stroke =
        [
            new(bounds.Left + (unit * 2.6f), bounds.Top + (unit * 7.2f)),
            new(bounds.Left + (unit * 2.6f), bounds.Top + (unit * 3.0f)),
            new(bounds.Left + (unit * 5.0f), bounds.Top + (unit * 5.6f)),
            new(bounds.Left + (unit * 7.4f), bounds.Top + (unit * 3.0f)),
            new(bounds.Left + (unit * 7.4f), bounds.Top + (unit * 7.2f)),
        ];
        using var pen = new Pen(Color.White, unit * 1.25f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round,
        };
        graphics.DrawLines(pen, stroke);
    }

    public static Icon LoadIcon()
    {
        using var stream = typeof(Brand).Assembly.GetManifestResourceStream("app.ico")
            ?? throw new InvalidOperationException("The application icon resource is missing.");
        return new Icon(stream);
    }
}
