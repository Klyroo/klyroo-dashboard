namespace Klyroo.Dashboard.Maui;

/// <summary>
/// One bar on its track: the track in the muted background, the bar from <see cref="Left"/> over <see cref="Length"/> (both in percent of the track, see <see cref="DashboardBarGeometry"/>),
/// and a hairline at zero when the scale goes below it. Both ends are rounded.
/// </summary>
internal sealed class DashboardBarView : ThemedGraphicsView
{
    private double _left;
    private double _width;
    private double? _zero;
    private KlyThemeColor _fill = DashboardParts.PrimaryFill;

    public DashboardBarView(double height)
    {
        HeightRequest = height;
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.Center;
        AutomationProperties.SetIsInAccessibleTree(this, false);
    }

    /// <summary>The left edge of the bar, in percent of the track.</summary>
    public double Left { get => _left; set => Set(ref _left, value); }

    /// <summary>The length of the bar, in percent of the track.</summary>
    public double Length { get => _width; set => Set(ref _width, value); }

    /// <summary>The position of zero in percent of the track, or <see langword="null"/> when no value is below zero.</summary>
    public double? ZeroPercent
    {
        get => _zero;
        set
        {
            _zero = value;
            Invalidate();
        }
    }

    /// <summary>The color of the bar.</summary>
    public KlyThemeColor Fill
    {
        get => _fill;
        set
        {
            _fill = value;
            Invalidate();
        }
    }

    private void Set(ref double field, double value)
    {
        field = value;
        Invalidate();
    }

    public override void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var radius = dirtyRect.Height / 2;
        canvas.FillColor = KlyTheme.BackgroundMuted.Resolve();
        canvas.FillRoundedRectangle(dirtyRect, radius);
        if (_width > 0)
        {
            var x = (float)(dirtyRect.Width * _left / 100);
            var w = (float)(dirtyRect.Width * _width / 100);
            canvas.FillColor = _fill.Resolve();
            canvas.FillRoundedRectangle(x, 0, w, dirtyRect.Height, radius);
        }
        if (_zero is { } zero)
        {
            canvas.StrokeColor = KlyTheme.TextMuted.Resolve();
            canvas.StrokeSize = 1;
            var x = (float)(dirtyRect.Width * zero / 100);
            canvas.DrawLine(x, 0, x, dirtyRect.Height);
        }
    }
}
