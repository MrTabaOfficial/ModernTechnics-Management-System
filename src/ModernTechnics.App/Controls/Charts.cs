using ModernTechnics.App.Ui;

namespace ModernTechnics.App.Controls;

internal sealed record ChartPoint(string Label, decimal Value, string ValueText);

/// <summary>Vertical bar chart for a short time series; the last bar is emphasised.</summary>
internal sealed class BarChart : Control
{
    private IReadOnlyList<ChartPoint> _points = [];

    public BarChart()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Card;
        Dock = DockStyle.Fill;
    }

    public string EmptyText { get; set; } = string.Empty;

    public void Show(IReadOnlyList<ChartPoint> points)
    {
        _points = points;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.Smooth();
        graphics.Clear(BackColor);

        var max = _points.Count == 0 ? 0 : _points.Max(p => p.Value);
        if (max <= 0)
        {
            TextRenderer.DrawText(graphics, EmptyText, Theme.Body, ClientRectangle, Theme.TextMuted, Draw.Center);
            return;
        }

        var labelHeight = Theme.Px(24);
        var valueHeight = Theme.Px(22);
        var plotTop = valueHeight;
        var plotBottom = Height - labelHeight;
        var plotHeight = plotBottom - plotTop;
        var slot = (float)Width / _points.Count;
        var barWidth = Math.Min(slot * 0.56f, Theme.Px(54f));

        using (var baseline = new Pen(Theme.Border))
        {
            graphics.DrawLine(baseline, 0, plotBottom, Width, plotBottom);
        }

        for (var i = 0; i < _points.Count; i++)
        {
            var point = _points[i];
            var isLast = i == _points.Count - 1;
            var height = Math.Max((float)(point.Value / max) * plotHeight, point.Value > 0 ? Theme.Px(4f) : 0);
            var left = (slot * i) + ((slot - barWidth) / 2);
            var bar = new RectangleF(left, plotBottom - height, barWidth, height);

            if (height > 0)
            {
                graphics.FillRounded(isLast ? Theme.Accent : Theme.AccentMuted, bar, Theme.Px(5f));
                // Square off the bottom so bars sit on the baseline.
                using var brush = new SolidBrush(isLast ? Theme.Accent : Theme.AccentMuted);
                graphics.FillRectangle(brush, bar.Left, bar.Bottom - Math.Min(height, Theme.Px(5f)), bar.Width, Math.Min(height, Theme.Px(5f)));
            }

            var slotLeft = (int)(slot * i);
            if (point.Value > 0)
            {
                TextRenderer.DrawText(
                    graphics, point.ValueText, Theme.Small,
                    new Rectangle(slotLeft, (int)bar.Top - valueHeight, (int)slot, valueHeight),
                    isLast ? Theme.Text : Theme.TextMuted, Draw.Center);
            }

            TextRenderer.DrawText(
                graphics, point.Label, isLast ? Theme.SmallBold : Theme.Small,
                new Rectangle(slotLeft, plotBottom + Theme.Px(4), (int)slot, labelHeight - Theme.Px(4)),
                isLast ? Theme.Text : Theme.TextMuted, Draw.Center);
        }
    }
}

/// <summary>Horizontal ranked bars: label, proportional bar, value.</summary>
internal sealed class RankChart : Control
{
    private IReadOnlyList<ChartPoint> _points = [];

    public RankChart()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Card;
        Dock = DockStyle.Fill;
    }

    public void Show(IReadOnlyList<ChartPoint> points)
    {
        _points = points;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.Smooth();
        graphics.Clear(BackColor);
        if (_points.Count == 0)
        {
            return;
        }

        var max = Math.Max(_points.Max(p => p.Value), 1);
        var rowHeight = Math.Min(Theme.Px(34), Height / _points.Count);
        var labelWidth = (int)(Width * 0.42f);
        var valueWidth = Theme.Px(32);
        var trackLeft = labelWidth + Theme.Px(8);
        var trackWidth = Width - trackLeft - valueWidth - Theme.Px(8);
        var barHeight = Theme.Px(10f);

        for (var i = 0; i < _points.Count; i++)
        {
            var point = _points[i];
            var top = i * rowHeight;

            TextRenderer.DrawText(
                graphics, point.Label, Theme.Body, new Rectangle(0, top, labelWidth, rowHeight), Theme.Text, Draw.Left);

            var track = new RectangleF(trackLeft, top + ((rowHeight - barHeight) / 2), trackWidth, barHeight);
            graphics.FillRounded(Theme.Surface, track, barHeight / 2);
            var fill = track with { Width = Math.Max(track.Width * (float)(point.Value / max), barHeight) };
            graphics.FillRounded(i == 0 ? Theme.Accent : Theme.AccentMuted, fill, barHeight / 2);

            TextRenderer.DrawText(
                graphics, point.ValueText, Theme.BodyBold,
                new Rectangle(Width - valueWidth, top, valueWidth, rowHeight), Theme.Text, Draw.Right);
        }
    }
}
