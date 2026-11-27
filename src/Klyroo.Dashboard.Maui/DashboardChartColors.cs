namespace Klyroo.Dashboard.Maui;

/// <summary>The colors of the series and slices: the chart tokens of Klyroo.UI (<c>chart-1</c> to <c>chart-6</c>, which follow the semantic colors until the app registers its own), then neutral.</summary>
internal static class DashboardChartColors
{
    /// <summary>The names in the order the series take them.</summary>
    public static readonly string[] Palette = ["chart-1", "chart-2", "chart-3", "chart-4", "chart-5", "chart-6", "neutral"];

    /// <summary>The color of the series at a position of the list, which cycles.</summary>
    public static KlyColor ColorOf(int index) => new(Palette[index % Palette.Length]);

    /// <summary>The colors of a color: as registered, or for a chart token nobody registered the semantic color it follows.</summary>
    public static KlyColorSet Resolve(KlyColor color)
    {
        try
        {
            return KlyTheme.Resolve(color);
        }
        catch (InvalidOperationException) when (Fallback(color) is { } fallback)
        {
            return KlyTheme.Resolve(fallback);
        }
    }

    private static KlyColor? Fallback(KlyColor color) => color.Value switch
    {
        "chart-1" => (KlyColor?)KlyColor.Primary,
        "chart-2" => KlyColor.Secondary,
        "chart-3" => KlyColor.Info,
        "chart-4" => KlyColor.Error,
        "chart-5" => KlyColor.Success,
        "chart-6" => KlyColor.Warning,
        _ => (KlyColor?)null,
    };
}
