namespace Klyroo.Dashboard.Maui;

/// <summary>The ring of a <see cref="KlyDashboardGauge"/>: a track and an arc of <see cref="Share"/> of the circle from the top, clockwise, with a round cap.</summary>
internal sealed class DashboardGaugeRing : ThemedGraphicsView
{
    // The proportions of the web drawing: a box of 100 units, a circle of radius 42 and a stroke of 8.
    private const float BoxSize = 100;
    private const float Radius = 42;
    private const float StrokeWidth = 8;

    private double _share;

    public DashboardGaugeRing()
    {
        WidthRequest = 140;
        HeightRequest = 140;
        HorizontalOptions = LayoutOptions.Center;
        VerticalOptions = LayoutOptions.Center;
        AutomationProperties.SetIsInAccessibleTree(this, false);
    }

    /// <summary>The filled fraction of the ring, from 0 to 1. Zero draws no arc at all: a round cap on an empty arc would leave a dot.</summary>
    public double Share
    {
        get => _share;
        set
        {
            _share = Math.Clamp(value, 0, 1);
            Invalidate();
        }
    }

    /// <summary>The path of an arc: points every 2 degrees from the top, clockwise, so no angle convention of a platform is involved.</summary>
    public static PathF ArcPath(float centerX, float centerY, float radius, double share)
    {
        var sweep = Math.Clamp(share, 0, 1) * 360;
        var steps = Math.Max(1, (int)Math.Ceiling(sweep / 2));
        PathF? path = null;
        for (var i = 0; i <= steps; i++)
        {
            var angle = sweep * i / steps * Math.PI / 180;
            var point = new PointF(centerX + radius * (float)Math.Sin(angle), centerY - radius * (float)Math.Cos(angle));
            if (path is null) path = new PathF(point);
            else path.LineTo(point);
        }
        return path!;
    }

    public override void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var scale = Math.Min(dirtyRect.Width, dirtyRect.Height) / BoxSize;
        var centerX = dirtyRect.Center.X;
        var centerY = dirtyRect.Center.Y;
        canvas.StrokeSize = StrokeWidth * scale;
        canvas.StrokeColor = KlyTheme.BackgroundMuted.Resolve();
        canvas.DrawCircle(centerX, centerY, Radius * scale);
        if (_share <= 0) return;
        canvas.StrokeColor = DashboardParts.PrimaryFill.Resolve();
        canvas.StrokeLineCap = LineCap.Round;
        canvas.DrawPath(ArcPath(centerX, centerY, Radius * scale, _share));
    }
}
