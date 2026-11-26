using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Klyroo.Dashboard.Tests;

// The closed sets of values are enums, and the markup they produce is what the text values produced.
public sealed class EnumParameterTests : BunitContext
{
    public EnumParameterTests()
    {
        Services.AddKlyrooDashboard();
    }

    [Fact]
    public void Grid_stats_and_bars_expose_the_variant_as_lowercase_text()
    {
        Assert.Equal("cards", Render<KlyDashboardGrid>().Find("[data-slot=dashboard-grid]").GetAttribute("data-variant"));
        Assert.Equal("panels", Render<KlyDashboardGrid>(p => p.Add(x => x.Variant, DashboardGridVariant.Panels)).Find("[data-slot=dashboard-grid]").GetAttribute("data-variant"));
        Assert.Equal("plain", Render<KlyDashboardStats>(p => p.Add(x => x.Items, [new DashboardStatsItem("a", "A", 1)])).Find("[data-slot=dashboard-stats]").GetAttribute("data-variant"));
        Assert.Equal("tiles", Render<KlyDashboardStats>(p => p.Add(x => x.Variant, DashboardStatsVariant.Tiles).Add(x => x.Items, [new DashboardStatsItem("a", "A", 1)])).Find("[data-slot=dashboard-stats]").GetAttribute("data-variant"));
        Assert.Equal("bars", Render<KlyDashboardBars>(p => p.Add(x => x.Items, [new DashboardBarItem("A", 1)])).Find("[data-slot=dashboard-bars]").GetAttribute("data-variant"));
        Assert.Equal("funnel", Render<KlyDashboardBars>(p => p.Add(x => x.Variant, DashboardBarsVariant.Funnel).Add(x => x.Items, [new DashboardBarItem("A", 1)])).Find("[data-slot=dashboard-bars]").GetAttribute("data-variant"));
    }

    [Theory]
    [InlineData(DashboardChartType.Bar, "bar")]
    [InlineData(DashboardChartType.Line, "line")]
    [InlineData(DashboardChartType.Area, "area")]
    [InlineData(DashboardChartType.Donut, "donut")]
    public void The_chart_type_is_exposed_as_lowercase_text(DashboardChartType type, string expected)
    {
        var cut = Render<KlyDashboardChart>(p => p
            .Add(x => x.Type, type)
            .Add(x => x.Categories, ["a", "b"])
            .Add(x => x.Series, [new DashboardChartSeries("s", "S", [1, 2])]));

        Assert.Equal(expected, cut.Find("[data-slot=dashboard-chart]").GetAttribute("data-type"));
    }

    [Fact]
    public void Alerts_are_ordered_by_severity_or_left_as_given()
    {
        DashboardAlertItem[] items = [new(DashboardAlertSeverity.Success, "Fine"), new(DashboardAlertSeverity.Error, "Broken")];

        var bySeverity = Render<KlyDashboardAlerts>(p => p.Add(x => x.Items, items));
        var asGiven = Render<KlyDashboardAlerts>(p => p.Add(x => x.Order, DashboardAlertOrder.AsGiven).Add(x => x.Items, items));

        Assert.StartsWith("Broken", bySeverity.Find("[data-slot=dashboard-alert-label]").TextContent.Trim(), StringComparison.Ordinal);
        Assert.StartsWith("Fine", asGiven.Find("[data-slot=dashboard-alert-label]").TextContent.Trim(), StringComparison.Ordinal);
    }

    [Fact]
    public void Compare_mode_difference_gives_points_and_emphasis_first_mutes_the_other_bars()
    {
        var change = Render<KlyDashboardStat>(p => p.Add(x => x.Label, "L").Add(x => x.Value, 0.032).Add(x => x.Compare, 0.030).Add(x => x.Format, DashboardFormatPreset.Ratio));
        var difference = Render<KlyDashboardStat>(p => p.Add(x => x.Label, "L").Add(x => x.Value, 0.032).Add(x => x.Compare, 0.030).Add(x => x.Format, DashboardFormatPreset.Ratio).Add(x => x.CompareMode, DashboardCompareMode.Difference));
        Assert.Contains("%", change.Markup, StringComparison.Ordinal);
        Assert.Contains("pts", difference.Markup, StringComparison.Ordinal);

        var bars = Render<KlyDashboardBars>(p => p.Add(x => x.Emphasis, DashboardBarsEmphasis.First).Add(x => x.Items, [new DashboardBarItem("A", 2), new DashboardBarItem("B", 1)]));
        Assert.Contains("bg-(--klyroo-text-muted)/50", bars.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void The_totals_row_is_off_by_default_and_the_series_can_be_summed_or_averaged()
    {
        DashboardChartSeries[] series = [new("s", "S", [1, 3])];
        DashboardChartTotals[] modes = [DashboardChartTotals.None, DashboardChartTotals.Sum, DashboardChartTotals.Average];
        var rendered = modes.Select(m => Render<KlyDashboardChart>(p => p.Add(x => x.Categories, ["a", "b"]).Add(x => x.Series, series).Add(x => x.Totals, m))).ToList();

        Assert.Empty(rendered[0].FindAll("[data-slot=dashboard-chart-total]"));
        Assert.Contains("4", rendered[1].Find("[data-slot=dashboard-chart-total]").TextContent, StringComparison.Ordinal);
        Assert.Contains("2", rendered[2].Find("[data-slot=dashboard-chart-total]").TextContent, StringComparison.Ordinal);
    }
}
