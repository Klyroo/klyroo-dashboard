namespace Klyroo.Dashboard.Maui;

/// <summary>One series of the compact drawing: the series, the color of its position in the list, and whether it is drawn as a line.</summary>
internal sealed record DashboardCompactSeries(DashboardChartSeries Series, KlyColor Color, bool IsLine);

/// <summary>
/// The drawing of a <see cref="KlyDashboardChart"/> that <see cref="KlyChart{TItem, TIndex}"/> cannot do: bars with lines over them, or bars with their values above them.
/// Bars and lines come from a baseline at zero, with no axes or gridlines, and the category names under them.
/// </summary>
internal sealed class DashboardCompactChart : ThemedGraphicsView
{
    private const float Top = 8;
    private const float LabelHeight = 16;
    private const float Gap = 4;
    private const float ValueLabel = 16;
    private const float MaxBarWidth = 48;
    private const float BarGap = 2;

    private IReadOnlyList<string> _categories = [];
    private IReadOnlyList<DashboardCompactSeries> _series = [];
    private bool _showLabels;
    private Func<double, string> _format = value => value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    public DashboardCompactChart()
    {
        HorizontalOptions = LayoutOptions.Fill;
    }

    /// <summary>Sets what the chart draws.</summary>
    public void Set(IReadOnlyList<string> categories, IReadOnlyList<DashboardCompactSeries> series, bool showLabels, Func<double, string> format, int height)
    {
        _categories = categories;
        _series = series;
        _showLabels = showLabels;
        _format = format;
        HeightRequest = height;
        Invalidate();
    }

    /// <summary>The bar of a value in a category: the left edge, the top and the size of the rectangle, for a plot of the given extent. <see langword="null"/> for a gap.</summary>
    public static RectF? BarRect(double? value, int category, int count, int barIndex, int barCount, float width, Func<double, float> y, float zeroY)
    {
        if (value is not { } v || count <= 0 || barCount <= 0) return null;
        var column = width / count;
        var barWidth = Math.Max(1, Math.Min(MaxBarWidth, (column - 8 - (barCount - 1) * BarGap) / barCount));
        var group = barCount * barWidth + (barCount - 1) * BarGap;
        var x = category * column + (column - group) / 2 + barIndex * (barWidth + BarGap);
        var top = v >= 0 ? y(v) : zeroY;
        return new RectF(x, top, barWidth, Math.Abs(y(v) - zeroY));
    }

    public override void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var count = _categories.Count;
        if (count == 0 || _series.Count == 0) return;
        var bars = _series.Where(s => !s.IsLine).ToList();
        var lines = _series.Where(s => s.IsLine).ToList();
        var (lo, hi) = DashboardBarGeometry.Domain(_series.SelectMany(s => s.Series.Values));
        var hasNegative = lo < 0;
        var plotTop = Top + (_showLabels && bars.Count > 0 ? ValueLabel : 0);
        var plotBottom = dirtyRect.Height - LabelHeight - Gap - (_showLabels && hasNegative ? ValueLabel : 0);
        var plotHeight = Math.Max(1, plotBottom - plotTop);
        float Y(double v) => plotTop + (float)((hi - v) / (hi - lo)) * plotHeight;
        var zeroY = Y(0);
        var column = dirtyRect.Width / count;

        canvas.StrokeSize = 1;
        canvas.StrokeColor = KlyTheme.Border.Resolve();
        canvas.DrawLine(0, zeroY, dirtyRect.Width, zeroY);

        canvas.Font = Microsoft.Maui.Graphics.Font.Default;
        for (var i = 0; i < count; i++)
        {
            for (var j = 0; j < bars.Count; j++)
            {
                var series = bars[j];
                var value = ValueOrNull(series.Series, i);
                if (BarRect(value, i, count, j, bars.Count, dirtyRect.Width, Y, zeroY) is not { } rect) continue;
                if (rect.Height > 0)
                {
                    canvas.FillColor = series.Series.IsComparison ? KlyTheme.TextMuted.WithAlpha(0.5f).Resolve() : DashboardChartColors.Resolve(series.Color).Fill.Resolve();
                    canvas.FillRoundedRectangle(rect, 2);
                    if (!series.Series.IsComparison)
                    {
                        canvas.StrokeColor = DashboardChartColors.Resolve(series.Color).Text.Resolve();
                        canvas.DrawRoundedRectangle(rect, 2);
                    }
                }
                if (_showLabels && value is { } labelled)
                {
                    canvas.FontSize = 10;
                    canvas.FontColor = KlyTheme.TextMuted.Resolve();
                    var labelY = labelled < 0 ? rect.Bottom + 2 : rect.Top - ValueLabel + 2;
                    canvas.DrawString(_format(labelled), rect.Center.X - column / 2, labelY, column, ValueLabel - 2, HorizontalAlignment.Center, VerticalAlignment.Center);
                }
            }
        }

        foreach (var line in lines)
        {
            var color = line.Series.IsComparison ? KlyTheme.TextMuted.Resolve() : DashboardChartColors.Resolve(line.Color).Text.Resolve();
            canvas.StrokeColor = color;
            canvas.StrokeSize = 2;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeLineJoin = LineJoin.Round;
            canvas.StrokeDashPattern = line.Series.IsComparison ? [2f, 1.5f] : null;
            // A gap in the data breaks the line instead of drawing a zero.
            PathF? run = null;
            for (var i = 0; i < count; i++)
            {
                if (ValueOrNull(line.Series, i) is { } point)
                {
                    var p = new PointF((i + 0.5f) * column, Y(point));
                    if (run is null) run = new PathF(p);
                    else run.LineTo(p);
                }
                else if (run is not null)
                {
                    canvas.DrawPath(run);
                    run = null;
                }
            }
            if (run is not null) canvas.DrawPath(run);
            canvas.StrokeDashPattern = null;

            for (var i = 0; i < count; i++)
            {
                if (ValueOrNull(line.Series, i) is not { } point) continue;
                var center = new PointF((i + 0.5f) * column, Y(point));
                canvas.FillColor = color;
                canvas.FillCircle(center, 4);
                canvas.StrokeColor = KlyTheme.Background.Resolve();
                canvas.StrokeSize = 2;
                canvas.DrawCircle(center, 4);
            }
        }

        canvas.FontSize = 11;
        canvas.FontColor = KlyTheme.TextMuted.Resolve();
        for (var i = 0; i < count; i++)
        {
            canvas.DrawString(_categories[i], i * column + 4, dirtyRect.Height - LabelHeight, Math.Max(1, column - 8), LabelHeight, HorizontalAlignment.Center, VerticalAlignment.Top, TextFlow.ClipBounds);
        }
    }

    /// <summary>A value the list does not hold, or that is not a number, is a gap in the chart, never a zero.</summary>
    public static double? ValueOrNull(DashboardChartSeries series, int index) =>
        index < series.Values.Count && double.IsFinite(series.Values[index]) ? series.Values[index] : null;
}
