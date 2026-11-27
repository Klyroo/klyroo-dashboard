using System.Reflection;
using static Klyroo.Dashboard.Maui.Tests.TestHelpers;

namespace Klyroo.Dashboard.Maui.Tests;

public class DashboardChartTests
{
    private static readonly string[] Days = ["Mon", "Tue", "Wed"];

    private static IReadOnlyList<DashboardChartSeries> Sales() =>
    [
        new("sales", "Sales", [10, 20, 30]),
        new("returns", "Returns", [1, double.NaN, 3]),
    ];

    private static KlyDashboardChart Chart(DashboardChartType type = DashboardChartType.Bar, IReadOnlyList<DashboardChartSeries>? series = null) =>
        new() { Type = type, Categories = Days, Series = series ?? Sales() };

    private static KlyChart<int, string> Native(KlyDashboardChart chart) => All<KlyChart<int, string>>(chart).Single();

    // The picks of KlyChart come from its canvas: this raises its event the way a tap does.
    private static void Raise(KlyChart<int, string> chart, KlyChartSelectEventArgs<int, string> args)
    {
        var field = typeof(KlyChart<int, string>).GetField("Selected", BindingFlags.Instance | BindingFlags.NonPublic)!;
        ((EventHandler<KlyChartSelectEventArgs<int, string>>?)field.GetValue(chart))?.Invoke(chart, args);
    }

    private static KlyChartSelectEventArgs<int, string> Pick(int row, int series) => new(row, Days[Math.Max(row, 0) % 3], null, series, null, row);

    // --- defaults and state ----------------------------------------------------------------------------------------------------------------

    [Fact]
    public void A_chart_is_eight_wide_a_bar_chart_with_a_legend_and_a_height_of_220()
    {
        var chart = new KlyDashboardChart();

        Assert.Equal(8, chart.Span);
        Assert.Equal(DashboardChartType.Bar, chart.Type);
        Assert.Equal(220, chart.PlotHeight);
        Assert.True(chart.ShowLegend);
        Assert.False(chart.ShowLabels);
        Assert.False(chart.Stacked);
        Assert.False(chart.Horizontal);
        Assert.Equal(DashboardChartTotals.None, chart.Totals);
        Assert.Equal(DashboardFormatPreset.Number, chart.Format);
    }

    [Fact]
    public void A_chart_without_categories_or_series_is_empty()
    {
        var chart = new KlyDashboardChart();
        Assert.True(chart.IsEmpty);

        chart.Categories = Days;
        Assert.True(chart.IsEmpty);

        chart.Series = Sales();
        Assert.False(chart.IsEmpty);
        Assert.Equal(DashboardBlockPhase.Content, chart.Phase);

        chart.Categories = [];
        Assert.True(chart.IsEmpty);
    }

    [Theory]
    [InlineData(DashboardChartType.Bar, DashboardSkeletonKind.Bars)]
    [InlineData(DashboardChartType.Line, DashboardSkeletonKind.Lines)]
    [InlineData(DashboardChartType.Area, DashboardSkeletonKind.Lines)]
    [InlineData(DashboardChartType.Donut, DashboardSkeletonKind.Donut)]
    public void The_type_picks_the_loading_skeleton(DashboardChartType type, DashboardSkeletonKind skeleton) =>
        Assert.Equal(skeleton, Chart(type).Skeleton);

    // --- the drawing of KlyChart -----------------------------------------------------------------------------------------------------------

    [Fact]
    public void A_bar_chart_is_a_klychart_with_a_category_per_series_and_the_chart_colors()
    {
        var chart = Chart();
        var native = Native(chart);

        Assert.False(chart.UsesCompactDrawing);
        Assert.Equal(KlyChartType.Bar, native.Type);
        Assert.Equal([0, 1, 2], native.Data);
        Assert.Equal(["Mon", "Tue", "Wed"], native.Data!.Select(native.Index!));
        Assert.Equal(["sales", "returns"], native.Categories!.Select(c => c.Key));
        Assert.Equal(["Sales", "Returns"], native.Categories!.Select(c => c.Label));
        Assert.Equal(["chart-1", "chart-2"], native.Colors!.Select(c => c.Value));
        Assert.True(native.Legend);
        Assert.Equal(220, native.PlotHeight);
        Assert.Equal(4096, native.PlotWidth);
        Assert.Equal(System.Globalization.CultureInfo.CurrentCulture, native.Culture);
    }

    [Fact]
    public void A_value_the_series_does_not_hold_or_that_is_not_a_number_is_a_gap_never_a_zero()
    {
        var native = Native(Chart());

        Assert.Equal([10.0, 20.0, 30.0], native.Data!.Select(i => native.Categories![0].Value(i)));
        Assert.Equal([1.0, null, 3.0], native.Data!.Select(i => native.Categories![1].Value(i)));

        var short_ = Native(Chart(series: [new("s", "S", [5])]));
        Assert.Equal([5.0, null, null], short_.Data!.Select(i => short_.Categories![0].Value(i)));
    }

    [Fact]
    public void A_comparison_series_is_muted_and_the_others_keep_the_color_of_their_position()
    {
        var native = Native(Chart(series: [new("a", "A", [1, 2, 3]), new("prev", "Previous", [1, 1, 1], IsComparison: true), new("b", "B", [3, 2, 1])]));

        Assert.Equal(["chart-1", "neutral", "chart-3"], native.Colors!.Select(c => c.Value));
    }

    [Fact]
    public void Values_are_formatted_with_the_preset_of_the_chart()
    {
        Invariant();
        var chart = Chart();
        chart.Format = DashboardFormatPreset.Compact;
        chart.Series = [new("s", "S", [12600, 2_400_000, 5])];

        var native = Native(chart);
        Assert.Equal(["12.6K", "2.4M", "5"], new[] { 12600d, 2_400_000d, 5d }.Select(native.Format!));
    }

    [Theory]
    [InlineData(DashboardChartType.Line, KlyChartType.Line)]
    [InlineData(DashboardChartType.Area, KlyChartType.Area)]
    [InlineData(DashboardChartType.Bar, KlyChartType.Bar)]
    public void Each_chart_type_is_a_kind_of_klychart(DashboardChartType type, KlyChartType kind) =>
        Assert.Equal(kind, Native(Chart(type)).Type);

    [Fact]
    public void A_bar_chart_whose_series_are_all_lines_is_a_line_chart()
    {
        var chart = Chart(series: [new("a", "A", [1, 2, 3], DashboardChartSeriesType.Line), new("b", "B", [3, 2, 1], DashboardChartSeriesType.Line)]);

        Assert.False(chart.UsesCompactDrawing);
        Assert.Equal(KlyChartType.Line, Native(chart).Type);
    }

    [Fact]
    public void Horizontal_applies_to_bars_and_stacking_to_bars_and_areas()
    {
        var chart = Chart();
        chart.Horizontal = true;
        chart.Stacked = true;
        Assert.True(Native(chart).Horizontal);
        Assert.True(Native(chart).Stacked);

        chart.Type = DashboardChartType.Area;
        Assert.False(Native(chart).Horizontal);
        Assert.True(Native(chart).Stacked);

        chart.Type = DashboardChartType.Line;
        Assert.False(Native(chart).Horizontal);
        Assert.False(Native(chart).Stacked);
    }

    [Fact]
    public void The_legend_follows_show_legend()
    {
        var chart = Chart();
        chart.ShowLegend = false;

        Assert.False(Native(chart).Legend);
    }

    [Fact]
    public void A_donut_draws_the_first_series_that_is_not_a_comparison_with_the_total_in_the_middle()
    {
        Invariant();
        var chart = Chart(DashboardChartType.Donut, [new("prev", "Previous", [9, 9, 9], IsComparison: true), new("sales", "Sales", [10, 20, double.NaN]), new("other", "Other", [1, 1, 1])]);
        var native = Native(chart);

        Assert.Equal(KlyChartType.Donut, native.Type);
        Assert.Equal(["sales"], native.Categories!.Select(c => c.Key));
        Assert.Equal(["chart-1", "chart-2", "chart-3"], native.Colors!.Select(c => c.Value));
        Assert.Equal(["30", "Sales"], Texts(native.Center!));
        Assert.Equal(220, native.PlotHeight);
        Assert.Equal(native.PlotHeight, native.PlotWidth);
    }

    [Theory]
    [InlineData(50, 96)]
    [InlineData(150, 150)]
    [InlineData(500, 240)]
    public void A_donut_is_as_wide_as_it_is_high_between_96_and_240(int height, int expected)
    {
        var chart = Chart(DashboardChartType.Donut);
        chart.PlotHeight = height;

        Assert.Equal(expected, Native(chart).PlotHeight);
        Assert.Equal(expected, Native(chart).PlotWidth);
    }

    [Fact]
    public void A_donut_of_comparison_series_only_draws_nothing()
    {
        var chart = Chart(DashboardChartType.Donut, [new("prev", "Previous", [1, 2, 3], IsComparison: true)]);

        Assert.Empty(All<KlyChart<int, string>>(chart));
    }

    // --- the compact drawing ---------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Bars_with_a_line_over_them_use_the_compact_drawing()
    {
        var chart = Chart(series: [new("sales", "Sales", [10, 20, 30]), new("trend", "Trend", [12, 18, 28], DashboardChartSeriesType.Line)]);

        Assert.True(chart.UsesCompactDrawing);
        Assert.Empty(All<KlyChart<int, string>>(chart));
        Assert.Contains(chart.Compact, All<DashboardCompactChart>(chart));
        Assert.Contains("Sales: Mon 10, Tue 20, Wed 30; Trend: Mon 12, Tue 18, Wed 28", SemanticProperties.GetDescription(chart.Compact));
    }

    [Fact]
    public void Value_labels_also_use_the_compact_drawing_and_so_do_not_a_line_chart_with_labels()
    {
        var chart = Chart();
        chart.ShowLabels = true;
        Assert.True(chart.UsesCompactDrawing);

        chart.Type = DashboardChartType.Line;
        Assert.False(chart.UsesCompactDrawing);
        Assert.Single(All<KlyChart<int, string>>(chart));
    }

    [Fact]
    public void The_compact_legend_has_an_entry_per_series_with_a_dashed_swatch_for_a_comparison()
    {
        var chart = Chart(series: [new("sales", "Sales", [1, 2, 3]), new("prev", "Previous", [1, 1, 1], IsComparison: true), new("t", "Trend", [1, 2, 3], DashboardChartSeriesType.Line)]);

        var texts = Texts(PhaseOf(chart));
        Assert.Equal(["Sales", "Previous", "Trend"], texts);
        var dashed = All<Border>(PhaseOf(chart)).Where(b => b.StrokeDashArray is { Count: > 0 }).ToList();
        Assert.Single(dashed);

        chart.ShowLegend = false;
        Assert.Empty(Texts(PhaseOf(chart)));
    }

    // --- selection -------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void A_pick_reports_the_category_the_series_and_the_value()
    {
        var chart = Chart();
        var picks = new List<DashboardChartSelection>();
        chart.Selected += (_, e) => picks.Add(e);

        Raise(Native(chart), Pick(2, 1));

        Assert.Equal([new DashboardChartSelection("Wed", "returns", 3)], picks);
    }

    [Fact]
    public void A_pick_that_names_no_series_or_a_gap_stands_for_the_first_series_with_a_value()
    {
        var chart = Chart();
        var picks = new List<DashboardChartSelection>();
        chart.Selected += (_, e) => picks.Add(e);
        var native = Native(chart);

        Raise(native, Pick(1, -1));
        Raise(native, Pick(1, 1));

        Assert.Equal([new DashboardChartSelection("Tue", "sales", 20), new DashboardChartSelection("Tue", "sales", 20)], picks);
    }

    [Fact]
    public void A_pick_on_a_comparison_series_reports_the_comparison_series()
    {
        var chart = Chart(series: [new("sales", "Sales", [1, 2, 3]), new("prev", "Previous", [4, 5, 6], IsComparison: true)]);
        var picks = new List<DashboardChartSelection>();
        chart.Selected += (_, e) => picks.Add(e);

        Raise(Native(chart), Pick(0, 1));

        Assert.Equal([new DashboardChartSelection("Mon", "prev", 4)], picks);
    }

    [Fact]
    public void A_pick_of_a_row_that_does_not_exist_or_has_nothing_is_ignored()
    {
        var chart = Chart(series: [new("a", "A", [1, double.NaN, 3])]);
        var picks = new List<DashboardChartSelection>();
        chart.Selected += (_, e) => picks.Add(e);
        var native = Native(chart);

        Raise(native, Pick(1, 0));
        Raise(native, Pick(7, 0));
        Raise(native, Pick(-1, 0));

        Assert.Empty(picks);
    }

    [Fact]
    public void A_pick_of_a_donut_slice_names_the_category_of_the_slice()
    {
        var chart = Chart(DashboardChartType.Donut, [new("sales", "Sales", [10, 20, 30])]);
        var picks = new List<DashboardChartSelection>();
        chart.Selected += (_, e) => picks.Add(e);

        Raise(Native(chart), Pick(1, 0));

        Assert.Equal([new DashboardChartSelection("Tue", "sales", 20)], picks);
    }

    [Fact]
    public void The_compact_drawing_offers_a_button_per_data_point_once_something_listens()
    {
        var chart = Chart(series: [new("sales", "Sales", [10, 20, double.NaN]), new("prev", "Previous", [1, 2, 3], IsComparison: true), new("t", "Trend", [4, 5, 6], DashboardChartSeriesType.Line)]);
        Assert.Empty(All<KlyButton>(chart));

        var picks = new List<DashboardChartSelection>();
        chart.Selected += (_, e) => picks.Add(e);

        var buttons = All<KlyButton>(PhaseOf(chart));
        // One per category and series: the comparison series is left out, and the series with a gap has no button for it.
        Assert.Equal(["Mon · Sales  10", "Mon · Trend  4", "Tue · Sales  20", "Tue · Trend  5", "Wed · Trend  6"], buttons.Select(b => b.Text));
        Assert.All(buttons, b => Assert.Equal(40, b.MinimumHeightRequest));

        buttons.First(b => b.Text == "Tue · Trend  5").SendClicked();
        Assert.Equal([new DashboardChartSelection("Tue", "t", 5)], picks);
    }

    [Fact]
    public void Removing_the_last_listener_removes_the_buttons()
    {
        var chart = Chart(series: [new("sales", "Sales", [1, 2, 3]), new("t", "Trend", [4, 5, 6], DashboardChartSeriesType.Line)]);
        EventHandler<DashboardChartSelection> handler = (_, _) => { };
        chart.Selected += handler;
        Assert.NotEmpty(All<KlyButton>(chart));

        chart.Selected -= handler;

        Assert.Empty(All<KlyButton>(chart));
    }

    // --- totals and the table --------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Totals_sum_or_average_each_series_without_the_comparison_ones_and_without_gaps()
    {
        Invariant();
        var chart = Chart(series: [new("a", "A", [10, 20, double.NaN]), new("prev", "Previous", [100, 100, 100], IsComparison: true), new("b", "B", [double.NaN, double.NaN, double.NaN])]);

        chart.Totals = DashboardChartTotals.Sum;
        Assert.Equal(["A:", "30", "B:", "0"], Texts(PhaseOf(chart)).SkipWhile(t => t != "A:").ToList());

        chart.Totals = DashboardChartTotals.Average;
        Assert.Equal(["A:", "15", "B:", DashboardFormat.MissingValue], Texts(PhaseOf(chart)).SkipWhile(t => t != "A:").ToList());

        chart.Totals = DashboardChartTotals.None;
        Assert.DoesNotContain("A:", Texts(PhaseOf(chart)));
    }

    [Fact]
    public void A_chart_tabulates_a_column_per_series_with_empty_cells_for_gaps()
    {
        Invariant();
        var table = Chart().Tabulate!();

        Assert.Equal(["category", "sales", "returns"], table.Columns.Select(c => c.Key));
        Assert.Equal(["Category", "Sales", "Returns"], table.Columns.Select(c => c.Label));
        Assert.Equal([false, true, true], table.Columns.Select(c => c.Numeric));
        Assert.Equal(["Mon", "10", "1"], new[] { table.Rows[0]["category"], table.Rows[0]["sales"], table.Rows[0]["returns"] });
        Assert.Null(table.Rows[1]["returns"]);

        var short_ = new KlyDashboardChart { Categories = Days, Series = [new("s", "S", [5])] }.Tabulate!();
        Assert.Null(short_.Rows[2]["s"]);
    }
}
