namespace Klyroo.Dashboard;

/// <summary>
/// The position of a bar on a scale that always includes zero: a value of zero has no length, a negative value extends to the left of the zero
/// line, and nothing is rounded up to a minimum width, so the length of a bar is proportional to its value.
/// </summary>
internal static class DashboardBarGeometry
{
    /// <summary>The scale that holds <paramref name="values"/> and zero. <paramref name="max"/> replaces the largest value when it is given. An empty or all-zero scale spans 1.</summary>
    public static (double Lo, double Hi) Domain(IEnumerable<double> values, double? max = null)
    {
        double lo = 0, hi = 0;
        foreach (var value in values)
        {
            if (!double.IsFinite(value)) continue;
            lo = Math.Min(lo, value);
            hi = Math.Max(hi, value);
        }
        if (max is { } fixedMax && double.IsFinite(fixedMax))
        {
            hi = Math.Max(0, fixedMax);
        }
        return hi - lo > 0 ? (lo, hi) : (lo, lo + 1);
    }

    /// <summary>The left edge and the width of the bar of <paramref name="value"/>, in percent of the track, which spans <paramref name="lo"/> to <paramref name="hi"/>.</summary>
    public static (double Left, double Width) Segment(double value, double lo, double hi)
    {
        var span = hi - lo;
        if (!double.IsFinite(value) || span <= 0) return (0, 0);
        var zero = Math.Clamp(-lo / span * 100, 0, 100);
        var length = Math.Abs(value) / span * 100;
        if (value >= 0)
        {
            return (zero, Math.Clamp(length, 0, 100 - zero));
        }
        var width = Math.Clamp(length, 0, zero);
        return (zero - width, width);
    }

    /// <summary>The position of zero in percent of the track.</summary>
    public static double Zero(double lo, double hi) => Math.Clamp(-lo / (hi - lo) * 100, 0, 100);

    // "-0" would be written for a negative zero: a bar at zero is plain 0.
    public static string Percent(double value) => (value == 0 ? 0 : value).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
}
