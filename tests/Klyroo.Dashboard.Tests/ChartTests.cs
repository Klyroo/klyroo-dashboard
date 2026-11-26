using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Klyroo.UI;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Klyroo.Dashboard.Tests;

// KlyDashboardChart draws with KlyChart of Klyroo.UI: axes, a zero baseline, negative values, and no minimum length for a bar.
public sealed class ChartTests : BunitContext
{
    private static readonly IReadOnlyList<string> Months = ["Jan", "Feb", "Mar"];
    private static readonly DashboardChartSeries Revenue = new("revenue", "Revenue", [10, 20, 30]);
    private static readonly DashboardChartSeries Target = new("target", "Target", [15, 15, 15], Type: "line");
    private static readonly DashboardChartSeries Previous = new("previous", "Previous", [8, 18, 25], IsComparison: true);

    public ChartTests()
    {
        Services.AddKlyrooDashboard();
    }

    private IRenderedComponent<KlyDashboardChart> Chart(string type, IReadOnlyList<DashboardChartSeries> series, Action<ComponentParameterCollectionBuilder<KlyDashboardChart>>? more = null) =>
        Render<KlyDashboardChart>(p =>
        {
            p.Add(x => x.Type, type).Add(x => x.Categories, Months).Add(x => x.Series, series);
            more?.Invoke(p);
        });

    private static double Num(IElement e, string attribute) => double.Parse(e.GetAttribute(attribute)!, CultureInfo.InvariantCulture);

    private static IReadOnlyList<IElement> BarsOf(IRenderedComponent<KlyDashboardChart> cut, int series) =>
        cut.FindAll($"kly-chart svg g[data-series='{series}'] rect");

    [Fact]
    public void Bars_are_drawn_by_KlyChart_with_axes_and_a_text_alternative()
    {
        var cut = Chart("bar", [Revenue, Previous]);

        Assert.Equal("dashboard-chart-plot", cut.Find("kly-chart").GetAttribute("data-slot"));
        Assert.Equal(3, BarsOf(cut, 0).Count);
        Assert.Equal(3, BarsOf(cut, 1).Count);
        Assert.NotEmpty(cut.FindAll("kly-chart svg line"));
        Assert.Contains("Jan", cut.FindAll("kly-chart svg text").Select(t => t.TextContent));
        Assert.Equal("Revenue", cut.FindAll("kly-chart table thead th")[1].TextContent.Trim());
        Assert.Contains("Revenue", cut.Find("kly-chart [data-slot=plot]").GetAttribute("aria-label"));
        Assert.Equal(3, cut.FindAll("kly-chart svg g[data-hit]").Count);
    }

    [Fact]
    public void A_zero_has_no_bar_and_a_negative_value_goes_below_the_axis()
    {
        var cut = Chart("bar", [new DashboardChartSeries("net", "Net", [10, 0, -5])]);
        var bars = BarsOf(cut, 0);

        // The positive bar ends on the axis, the negative bar starts on it and goes down, the zero is a hairline on it.
        var axis = Num(bars[0], "y") + Num(bars[0], "height");
        Assert.Equal(axis, Num(bars[2], "y"), 2);
        Assert.True(Num(bars[2], "height") > 5);
        Assert.Equal(axis, Num(bars[1], "y"), 2);
        Assert.True(Num(bars[1], "height") <= 1);
    }

    [Fact]
    public void Series_that_are_all_zero_or_missing_do_not_draw_a_bar_for_the_gaps()
    {
        var zeros = Chart("bar", [new DashboardChartSeries("z", "Zero", [0, 0, 0])]);
        Assert.All(BarsOf(zeros, 0), bar => Assert.True(Num(bar, "height") <= 1));

        // A value the list does not hold is a gap, not a zero bar.
        var short1 = Chart("bar", [new DashboardChartSeries("s", "Short", [4])]);
        Assert.Single(BarsOf(short1, 0));
    }

    [Fact]
    public void Lines_areas_and_stacked_or_horizontal_bars_come_from_KlyChart()
    {
        var line = Chart("line", [Revenue, Previous]);
        Assert.Equal(KlyLocales.En.Messages.ChartLine, line.Find("kly-chart").GetAttribute("data-type"));
        Assert.Empty(line.FindAll("kly-chart svg g[data-series] rect"));
        Assert.Equal(2, line.FindAll("kly-chart svg g[data-series] path").Count);

        var area = Chart("area", [Revenue]);
        Assert.Equal(KlyLocales.En.Messages.ChartArea, area.Find("kly-chart").GetAttribute("data-type"));

        var horizontal = Chart("bar", [Revenue], p => p.Add(x => x.Horizontal, true));
        Assert.Equal(KlyLocales.En.Messages.ChartBarHorizontal, horizontal.Find("kly-chart").GetAttribute("data-type"));

        var stacked = Chart("bar", [Revenue, new DashboardChartSeries("other", "Other", [1, 2, 3])], p => p.Add(x => x.Stacked, true));
        // The second segment sits on top of the first.
        Assert.Equal(Num(BarsOf(stacked, 0)[0], "y"), Num(BarsOf(stacked, 1)[0], "y") + Num(BarsOf(stacked, 1)[0], "height"), 1);

        // A bar chart whose series are all lines is a line chart.
        var allLines = Chart("bar", [Target]);
        Assert.Equal(KlyLocales.En.Messages.ChartLine, allLines.Find("kly-chart").GetAttribute("data-type"));
    }

    [Fact]
    public void A_comparison_series_is_muted_and_the_other_series_keep_the_color_of_their_position()
    {
        var cut = Chart("bar", [Previous, Revenue]);
        Assert.Equal("neutral", cut.Find("kly-chart svg g[data-series='0']").GetAttribute("data-color"));
        Assert.Equal("chart-2", cut.Find("kly-chart svg g[data-series='1']").GetAttribute("data-color"));
    }

    [Fact]
    public void A_donut_draws_the_first_series_that_is_not_a_comparison_and_shows_the_total()
    {
        var donut = Chart("donut", [Previous, Revenue]);
        Assert.Equal(3, donut.FindAll("kly-chart svg path[data-index]").Count);
        Assert.Equal("60", donut.Find("[data-slot=dashboard-chart-donut-total]").TextContent);
        Assert.Contains("Mar 30 (50%)", donut.Find("kly-chart [data-slot=plot]").GetAttribute("aria-label"));
    }

    [Fact]
    public void A_donut_does_not_draw_a_slice_for_a_zero_or_a_negative_value()
    {
        var donut = Chart("donut", [new DashboardChartSeries("n", "Net", [10, 0, -5])]);
        Assert.Single(donut.FindAll("kly-chart svg path[data-index]"));
        Assert.Equal("10", donut.Find("[data-slot=dashboard-chart-donut-total]").TextContent);

        var empty = Chart("donut", [new DashboardChartSeries("n", "Net", [0, 0, 0])]);
        Assert.Empty(empty.FindAll("kly-chart svg path[data-index]"));
        Assert.Equal("0", empty.Find("[data-slot=dashboard-chart-donut-total]").TextContent);
    }

    [Fact]
    public void Bars_with_a_line_series_or_with_labels_use_the_compact_drawing()
    {
        var combo = Chart("bar", [Revenue, Target]);
        Assert.Empty(combo.FindAll("kly-chart"));
        Assert.Equal(3, combo.FindAll("[data-slot=dashboard-chart-bar]").Count);
        Assert.Equal("target", combo.Find("[data-slot=dashboard-chart-line]").GetAttribute("data-series"));
        Assert.Equal(3, combo.FindAll("[data-slot=dashboard-chart-point]").Count);

        var dashed = Chart("bar", [Revenue, new DashboardChartSeries("previous", "Previous", [8, 18, 25], Type: "line", IsComparison: true)]);
        Assert.Equal("4 3", dashed.Find("[data-slot=dashboard-chart-line]").GetAttribute("stroke-dasharray"));

        var labelled = Chart("bar", [Revenue], p => p.Add(x => x.ShowLabels, true));
        Assert.Empty(labelled.FindAll("kly-chart"));
        Assert.Equal(["10", "20", "30"], labelled.FindAll("[data-slot=dashboard-chart-bar]").Select(b => b.TextContent.Trim()));
    }

    [Fact]
    public void The_compact_drawing_starts_bars_at_zero_and_gives_zero_no_height()
    {
        var cut = Chart("bar", [new DashboardChartSeries("net", "Net", [10, 0, -10]), Target], p => p.Add(x => x.ShowLabels, true));
        var bars = cut.FindAll("[data-slot=dashboard-chart-bar]");
        string Style(IElement e, string name) => e.GetAttribute("style")!.Split(';').Select(x => x.Split(':')).First(x => x[0] == name)[1];

        // The scale is -10..15 with a baseline at 60% from the top.
        Assert.Equal("20%", Style(bars[0], "top"));
        Assert.Equal("40%", Style(bars[0], "height"));
        Assert.Equal("60%", Style(bars[1], "top"));
        Assert.Equal("0%", Style(bars[1], "height"));
        Assert.Equal("60%", Style(bars[2], "top"));
        Assert.Equal("40%", Style(bars[2], "height"));
        Assert.Equal("top:60%", cut.Find("[data-slot=dashboard-chart-baseline]").GetAttribute("style"));
        Assert.Equal("-10", bars[2].TextContent.Trim());
    }

    [Fact]
    public void A_line_breaks_at_a_missing_value_in_the_compact_drawing()
    {
        var gap = new DashboardChartSeries("t", "Target", [1, double.NaN, 3], Type: "line");
        var cut = Render<KlyDashboardChart>(p => p
            .Add(x => x.Categories, Months)
            .Add(x => x.Series, [Revenue, gap]));
        Assert.Equal(2, cut.FindAll("[data-slot=dashboard-chart-line]").Count);
        Assert.Equal(2, cut.FindAll("[data-slot=dashboard-chart-point]").Count);
    }

    [Fact]
    public void OnSelect_shows_one_button_per_data_point_and_reports_category_series_and_value()
    {
        var picked = new List<DashboardChartSelection>();
        var cut = Chart("bar", [Revenue, Previous], p => p.Add(x => x.OnSelect, (DashboardChartSelection s) => picked.Add(s)));

        var group = cut.Find("[data-slot=dashboard-chart-select]");
        Assert.Equal("group", group.GetAttribute("role"));
        Assert.Equal("Select a data point", group.GetAttribute("aria-label"));
        var buttons = cut.FindAll("[data-slot=dashboard-chart-select-point]");
        // The comparison series gives no button.
        Assert.Equal(3, buttons.Count);
        Assert.All(buttons, b => Assert.Equal("BUTTON", b.TagName));
        Assert.All(buttons, b => Assert.Equal("button", b.GetAttribute("type")));

        buttons[1].Click();
        Assert.Equal(new DashboardChartSelection("Feb", "revenue", 20), Assert.Single(picked));
    }

    [Fact]
    public void OnSelect_with_several_series_names_the_series_and_a_donut_gives_one_button_per_slice()
    {
        var picked = new List<DashboardChartSelection>();
        var two = Chart("bar", [Revenue, new DashboardChartSeries("cost", "Cost", [1, 2, 3])], p => p.Add(x => x.OnSelect, (DashboardChartSelection s) => picked.Add(s)));
        Assert.Equal(6, two.FindAll("[data-slot=dashboard-chart-select-point]").Count);
        Assert.Contains("Jan · Cost", two.FindAll("[data-slot=dashboard-chart-select-point]")[1].TextContent);

        var donut = Chart("donut", [Revenue], p => p.Add(x => x.OnSelect, (DashboardChartSelection s) => picked.Add(s)));
        var slices = donut.FindAll("[data-slot=dashboard-chart-select-point]");
        Assert.Equal(3, slices.Count);
        slices[2].Click();
        Assert.Equal(new DashboardChartSelection("Mar", "revenue", 30), picked[^1]);
    }

    [Fact]
    public void Without_OnSelect_the_chart_has_no_selection_buttons()
    {
        Assert.Empty(Chart("bar", [Revenue]).FindAll("[data-slot=dashboard-chart-select]"));
    }

    [Fact]
    public void Totals_keep_negative_values_and_leave_out_comparison_series()
    {
        var cut = Chart("bar", [new DashboardChartSeries("net", "Net", [10, 0, -15]), Previous], p => p.Add(x => x.Totals, "sum"));
        Assert.Equal(["Net:"], cut.FindAll("[data-slot=dashboard-chart-total] span:first-child").Select(s => s.TextContent.Trim()));
        Assert.Equal("-5", cut.Find("[data-slot=dashboard-chart-total] span:last-child").TextContent.Trim());
    }
}
