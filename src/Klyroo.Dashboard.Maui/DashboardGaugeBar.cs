namespace Klyroo.Dashboard.Maui;

/// <summary>The bar of a <see cref="KlyDashboardGauge"/>: a track, a fill of <see cref="Share"/> of it, and a marker at <see cref="TargetShare"/> that stands out above and below the track.</summary>
internal sealed class DashboardGaugeBar : ThemedGraphicsView
{
    private double _share;
    private double? _target;

    public DashboardGaugeBar()
    {
        // h-2.5 for the track, and 4 units of the marker above and below it.
        HeightRequest = 18;
        HorizontalOptions = LayoutOptions.Fill;
        AutomationProperties.SetIsInAccessibleTree(this, false);
    }

    /// <summary>The filled fraction of the track, from 0 to 1.</summary>
    public double Share
    {
        get => _share;
        set
        {
            _share = Math.Clamp(value, 0, 1);
            Invalidate();
        }
    }

    /// <summary>The position of the target, from 0 to 1, or <see langword="null"/> for no marker.</summary>
    public double? TargetShare
    {
        get => _target;
        set
        {
            _target = value;
            Invalidate();
        }
    }

    public override void Draw(ICanvas canvas, RectF dirtyRect)
    {
        const float trackHeight = 10;
        var top = (dirtyRect.Height - trackHeight) / 2;
        canvas.FillColor = KlyTheme.BackgroundMuted.Resolve();
        canvas.FillRoundedRectangle(0, top, dirtyRect.Width, trackHeight, trackHeight / 2);
        if (_share > 0)
        {
            canvas.FillColor = DashboardParts.PrimaryFill.Resolve();
            canvas.FillRoundedRectangle(0, top, (float)(dirtyRect.Width * _share), trackHeight, trackHeight / 2);
        }
        if (_target is { } target)
        {
            canvas.FillColor = KlyTheme.TextHighlighted.Resolve();
            canvas.FillRoundedRectangle((float)(dirtyRect.Width * target) - 1, 0, 2, dirtyRect.Height, 1);
        }
    }
}
