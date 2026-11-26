namespace Klyroo.Dashboard;

/// <summary>A change of zero is neither good nor bad, so it takes the muted color instead of the red of a worsening.</summary>
internal static class DashboardDelta
{
    public static bool IsZero(double delta) => Math.Abs(delta) < 0.0001;
}
