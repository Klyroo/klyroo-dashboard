using AngleSharp.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Klyroo.Dashboard.Tests;

// A value of zero or below must not look positive in any block.
public sealed class ZeroAndNegativeTests : BunitContext
{
    public ZeroAndNegativeTests()
    {
        Services.AddKlyrooDashboard();
    }

    private static string Style(IElement element, string name) =>
        element.GetAttribute("style")!.Split(';').Select(x => x.Split(':')).First(x => x[0] == name)[1];

    [Fact]
    public void A_bar_of_zero_has_no_length_and_no_bar_has_a_minimum_width()
    {
        var cut = Render<KlyDashboardBars>(p => p.Add(x => x.Items, [
            new DashboardBarItem("A", 100), new DashboardBarItem("B", 0), new DashboardBarItem("C", 0.5)]));
        var fills = cut.FindAll("[data-slot=dashboard-bar-fill]");

        Assert.Equal("0%", Style(fills[0], "left"));
        Assert.Equal("100%", Style(fills[0], "width"));
        Assert.Equal("0%", Style(fills[1], "width"));
        Assert.Equal("0.5%", Style(fills[2], "width"));
        Assert.Empty(cut.FindAll("[data-slot=dashboard-bar-zero]"));
    }

    [Fact]
    public void A_negative_bar_extends_to_the_left_of_a_zero_line()
    {
        var cut = Render<KlyDashboardBars>(p => p.Add(x => x.Items, [
            new DashboardBarItem("Up", 30), new DashboardBarItem("Down", -10), new DashboardBarItem("Flat", 0)]));
        var fills = cut.FindAll("[data-slot=dashboard-bar-fill]");

        // The scale is -10..30: the zero line is at 25%.
        Assert.Equal("25%", Style(cut.Find("[data-slot=dashboard-bar-zero]"), "left"));
        Assert.Equal("25%", Style(fills[0], "left"));
        Assert.Equal("75%", Style(fills[0], "width"));
        Assert.Equal("0%", Style(fills[1], "left"));
        Assert.Equal("25%", Style(fills[1], "width"));
        Assert.Equal("0%", Style(fills[2], "width"));
        // The text still tells the sign.
        Assert.Equal("-10", cut.FindAll("[data-slot=dashboard-bar-value]")[1].TextContent.Trim());
    }

    [Fact]
    public void Bars_of_small_values_are_scaled_to_the_largest_value_not_to_one()
    {
        var cut = Render<KlyDashboardBars>(p => p.Add(x => x.Items, [new DashboardBarItem("A", 0.5), new DashboardBarItem("B", 0.25)]));
        var fills = cut.FindAll("[data-slot=dashboard-bar-fill]");
        Assert.Equal("100%", Style(fills[0], "width"));
        Assert.Equal("50%", Style(fills[1], "width"));
    }

    [Fact]
    public void Bars_with_a_fixed_Max_clamp_a_larger_value_and_all_zero_bars_stay_empty()
    {
        var capped = Render<KlyDashboardBars>(p => p.Add(x => x.Max, 50.0).Add(x => x.Items, [new DashboardBarItem("A", 100), new DashboardBarItem("B", 25)]));
        var fills = capped.FindAll("[data-slot=dashboard-bar-fill]");
        Assert.Equal("100%", Style(fills[0], "width"));
        Assert.Equal("50%", Style(fills[1], "width"));

        var zeros = Render<KlyDashboardBars>(p => p.Add(x => x.Items, [new DashboardBarItem("A", 0), new DashboardBarItem("B", 0)]));
        Assert.All(zeros.FindAll("[data-slot=dashboard-bar-fill]"), f => Assert.Equal("0%", Style(f, "width")));
    }

    [Fact]
    public void A_funnel_step_of_zero_has_no_bar()
    {
        var cut = Render<KlyDashboardBars>(p => p.Add(x => x.Variant, DashboardBarsVariant.Funnel).Add(x => x.Items, [new DashboardBarItem("Visits", 200), new DashboardBarItem("Paid", 0)]));
        var fills = cut.FindAll("[data-slot=dashboard-bar-fill]");
        Assert.Equal("100%", Style(fills[0], "width"));
        Assert.Equal("0%", Style(fills[1], "width"));
        Assert.Equal("0% of the previous step", cut.FindAll("[data-slot=dashboard-funnel-note]")[1].TextContent.Trim());
    }

    [Fact]
    public void A_bar_column_of_a_table_follows_the_same_scale()
    {
        var cut = Render<KlyDashboardTable<RegionRow>>(p => p
            .Add(x => x.Columns, [
                DashboardTableColumn.For<RegionRow, string>("name", "Name", r => r.Name, sortable: false),
                DashboardTableColumn.For<RegionRow, double>("net", "Net", r => r.Revenue, kind: DashboardTableColumnKind.Bar, sortable: false)])
            .Add(x => x.Rows, [new RegionRow("A", 20.0), new RegionRow("B", 0.0), new RegionRow("C", -20.0)]));
        var fills = cut.FindAll("[data-slot=dashboard-table-cell] span.bg-\\(--klyroo-primary\\)");

        Assert.Equal(3, fills.Count);
        Assert.Equal("50%", Style(fills[0], "left"));
        Assert.Equal("50%", Style(fills[0], "width"));
        Assert.Equal("0%", Style(fills[1], "width"));
        Assert.Equal("0%", Style(fills[2], "left"));
        Assert.Equal("50%", Style(fills[2], "width"));
    }

    [Fact]
    public void A_ring_gauge_at_zero_draws_no_arc_and_a_bar_gauge_has_no_fill_width()
    {
        var ring = Render<KlyDashboardGauge>(p => p.Add(x => x.Value, 0.0));
        Assert.Empty(ring.FindAll("[data-slot=gauge-ring-fill]"));
        Assert.Equal("0", ring.Find("[data-slot=gauge-value]").TextContent.Trim());

        var negative = Render<KlyDashboardGauge>(p => p.Add(x => x.Value, -20.0));
        Assert.Empty(negative.FindAll("[data-slot=gauge-ring-fill]"));
        Assert.Equal("-20", negative.Find("[data-slot=gauge-value]").TextContent.Trim());
        // The meter value stays inside its range.
        Assert.Equal("0", negative.Find("[data-slot=dashboard-gauge]").GetAttribute("aria-valuenow"));

        var half = Render<KlyDashboardGauge>(p => p.Add(x => x.Value, 50.0));
        Assert.Single(half.FindAll("[data-slot=gauge-ring-fill]"));

        var bar = Render<KlyDashboardGauge>(p => p.Add(x => x.Variant, DashboardGaugeVariant.Bar).Add(x => x.Value, 0.0));
        Assert.Equal("width:0%", bar.Find("[data-slot=gauge-bar-fill]").GetAttribute("style"));
    }

    [Fact]
    public void A_change_of_zero_is_muted_in_a_list_a_table_and_stats_not_red_or_green()
    {
        var list = Render<KlyDashboardList>(p => p.Add(x => x.Items, [new DashboardListItem("A", Delta: 0), new DashboardListItem("B", Delta: 5), new DashboardListItem("C", Delta: -5)]));
        var deltas = list.FindAll("[data-slot=dashboard-list-delta]");
        Assert.Contains("text-(--klyroo-text-muted)", deltas[0].ClassName);
        Assert.Contains("success", deltas[1].ClassName);
        Assert.Contains("error", deltas[2].ClassName);

        var table = Render<KlyDashboardTable<RegionRow>>(p => p
            .Add(x => x.Columns, [DashboardTableColumn.For<RegionRow, double?>("d", "Growth", r => r.Growth, kind: DashboardTableColumnKind.Delta)])
            .Add(x => x.Rows, [new RegionRow("A", 0, 0.0), new RegionRow("B", 0, -3.0)]));
        var cells = table.FindAll("[data-slot=dashboard-table-cell] span");
        Assert.Contains("text-(--klyroo-text-muted)", cells[0].ClassName);
        Assert.Contains("error", cells[1].ClassName);

        var stats = Render<KlyDashboardStats>(p => p.Add(x => x.Items, [
            new DashboardStatsItem("a", "A", 1, Delta: 0), new DashboardStatsItem("b", "B", 1, Delta: -2)]));
        var statDeltas = stats.FindAll("[data-slot=dashboard-stats-delta]");
        Assert.Contains("text-(--klyroo-text-muted)", statDeltas[0].ClassName);
        Assert.Contains("error", statDeltas[1].ClassName);
    }
}
