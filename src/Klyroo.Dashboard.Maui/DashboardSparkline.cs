namespace Klyroo.Dashboard.Maui;

/// <summary>The line of a <see cref="KlyDashboardStat"/>: a polyline through the points, scaled to their range and to the width of the view, in the primary text color.</summary>
internal sealed class DashboardSparkline : ThemedGraphicsView
{
    // The margins of the web drawing: a box of 100 by 30 units where the line spans 24 of the 30 units, 4 above and 2 below.
    private const double Top = 4d / 30;
    private const double Span = 24d / 30;

    private IReadOnlyList<double> _points = [];

    public DashboardSparkline()
    {
        HeightRequest = 32;
        HorizontalOptions = LayoutOptions.Fill;
        AutomationProperties.SetIsInAccessibleTree(this, false);
    }

    /// <summary>The series. A series of fewer than two points draws nothing.</summary>
    public IReadOnlyList<double> Points
    {
        get => _points;
        set
        {
            _points = value;
            Invalidate();
        }
    }

    /// <summary>The points of the line in a box of the given size: x spreads evenly, y goes from the maximum (top) to the minimum.</summary>
    public static PointF[] Project(IReadOnlyList<double> points, float width, float height)
    {
        if (points.Count < 2) return [];
        var min = points.Min();
        var max = points.Max();
        var range = max - min <= 0 ? 1 : max - min;
        var result = new PointF[points.Count];
        for (var i = 0; i < points.Count; i++)
        {
            var x = (float)(i * (double)width / (points.Count - 1));
            var y = (float)(height * (Top + Span - (points[i] - min) / range * Span));
            result[i] = new PointF(x, y);
        }
        return result;
    }

    public override void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var line = Project(_points, dirtyRect.Width, dirtyRect.Height);
        if (line.Length < 2) return;
        var path = new PathF(line[0]);
        for (var i = 1; i < line.Length; i++) path.LineTo(line[i]);
        canvas.StrokeColor = DashboardParts.PrimaryText.Resolve();
        canvas.StrokeSize = 2;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.DrawPath(path);
    }
}
