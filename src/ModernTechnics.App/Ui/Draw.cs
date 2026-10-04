using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace ModernTechnics.App.Ui;

internal static class Draw
{
    public const TextFormatFlags Left =
        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;

    public const TextFormatFlags Center =
        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix;

    public const TextFormatFlags Right =
        TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix;

    public static GraphicsPath RoundedPath(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        if (diameter <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void Smooth(this Graphics graphics)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
    }

    public static void FillRounded(this Graphics graphics, Color color, RectangleF bounds, float radius)
    {
        using var brush = new SolidBrush(color);
        using var path = RoundedPath(bounds, radius);
        graphics.FillPath(brush, path);
    }

    public static void StrokeRounded(
        this Graphics graphics, Color color, RectangleF bounds, float radius, float width = 1f)
    {
        // Inset by half the pen so the stroke is not clipped at the control edge.
        var half = width / 2f;
        var inset = RectangleF.FromLTRB(
            bounds.Left + half, bounds.Top + half, bounds.Right - half - 1, bounds.Bottom - half - 1);
        using var pen = new Pen(color, width);
        using var path = RoundedPath(inset, radius);
        graphics.DrawPath(pen, path);
    }

    /// <summary>Draws an icon-font glyph centred in <paramref name="bounds"/>.</summary>
    public static void Glyph(this Graphics graphics, string glyph, float size, Color color, Rectangle bounds) =>
        TextRenderer.DrawText(graphics, glyph, Theme.Icon(size), bounds, color, Center | TextFormatFlags.NoPadding);

    public static Color Blend(Color from, Color to, float amount) => Color.FromArgb(
        (int)(from.R + ((to.R - from.R) * amount)),
        (int)(from.G + ((to.G - from.G) * amount)),
        (int)(from.B + ((to.B - from.B) * amount)));
}
