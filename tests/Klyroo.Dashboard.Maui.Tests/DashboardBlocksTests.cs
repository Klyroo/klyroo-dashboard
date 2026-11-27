using static Klyroo.Dashboard.Maui.Tests.TestHelpers;

namespace Klyroo.Dashboard.Maui.Tests;

public class DashboardBlocksTests
{
    private static void Click(Button button) => button.SendClicked();

    private static IReadOnlyList<string?> Row(DashboardDataTable table, int row, params string[] keys) => keys.Select(k => table.Rows[row][k]).ToList();

    // --- KlyDashboardStat ------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void A_stat_is_four_wide_not_expandable_and_names_the_card_with_its_label()
    {
        var stat = new KlyDashboardStat { Label = "Revenue" };

        Assert.Equal(4, stat.Span);
        Assert.False(stat.Expandable);
        Assert.Equal(DashboardSkeletonKind.Stat, stat.Skeleton);
        Assert.Equal("Revenue", stat.Title);
        Assert.Equal("Revenue", stat.TitleLabel.Text);
        Assert.Equal(DashboardFormatPreset.Number, stat.Format);
        Assert.Equal(DashboardCompareMode.Change, stat.CompareMode);
    }

    [Fact]
    public void A_stat_shows_its_value_in_its_format_a_text_instead_or_a_dash()
    {
        Invariant();
        var stat = new KlyDashboardStat { Label = "Revenue", Value = 48500, Format = DashboardFormatPreset.Compact };
        Assert.Contains("48.5K", Texts(stat));

        stat.TextValue = "Healthy";
        Assert.Contains("Healthy", Texts(stat));
        Assert.DoesNotContain("48.5K", Texts(stat));

        stat.TextValue = null;
        stat.Value = null;
        Assert.Contains(DashboardFormat.MissingValue, Texts(stat));
    }

    [Fact]
    public void The_delta_is_computed_from_the_previous_value_and_names_it_in_the_caption()
    {
        Invariant();
        var stat = new KlyDashboardStat { Label = "Revenue", Value = 120, Compare = 100 };

        Assert.Equal(20, stat.EffectiveDelta);
        Assert.Equal(DashboardFormatPreset.Delta, stat.EffectiveDeltaFormat);
        var texts = Texts(stat);
        Assert.Contains("↑", texts);
        Assert.Contains("+20%", texts);
        Assert.Contains("vs 100 previous period", texts);
    }

    [Fact]
    public void A_change_from_a_previous_value_of_zero_shows_no_delta()
    {
        var stat = new KlyDashboardStat { Label = "Revenue", Value = 120, Compare = 0 };

        Assert.Null(stat.EffectiveDelta);
        Assert.DoesNotContain("↑", Texts(stat));
        Assert.Contains("vs 0 previous period", Texts(stat));
    }

    [Fact]
    public void The_difference_mode_uses_the_raw_difference_scaled_for_a_ratio_and_points_for_percentages()
    {
        var stat = new KlyDashboardStat { Label = "Conversion", Value = 0.57, Compare = 0.5, Format = DashboardFormatPreset.Ratio, CompareMode = DashboardCompareMode.Difference };

        Assert.Equal(7, stat.EffectiveDelta!.Value, 6);
        Assert.Equal(DashboardFormatPreset.Points, stat.EffectiveDeltaFormat);

        stat.Format = DashboardFormatPreset.Number;
        stat.Value = 12;
        stat.Compare = 10;
        Assert.Equal(2, stat.EffectiveDelta);
        Assert.Equal(DashboardFormatPreset.Signed, stat.EffectiveDeltaFormat);

        stat.DeltaFormat = DashboardFormatPreset.Integer;
        Assert.Equal(DashboardFormatPreset.Integer, stat.EffectiveDeltaFormat);
    }

    [Fact]
    public void An_explicit_delta_wins_over_the_comparison_and_a_nan_one_shows_nothing()
    {
        Invariant();
        var stat = new KlyDashboardStat { Label = "R", Value = 120, Compare = 100, Delta = -3.5 };

        Assert.Equal(-3.5, stat.EffectiveDelta);
        Assert.Contains("↓", Texts(stat));
        Assert.Contains("-3.5%", Texts(stat));

        stat.Delta = double.NaN;
        Assert.DoesNotContain("↓", Texts(stat));
        Assert.DoesNotContain("↑", Texts(stat));
    }

    [Theory]
    [InlineData(5.0, false, "↑", true)]
    [InlineData(-5.0, false, "↓", false)]
    [InlineData(-5.0, true, "↓", true)]
    [InlineData(5.0, true, "↑", false)]
    [InlineData(0.0, false, "–", false)]
    public void The_delta_arrow_follows_the_sign_and_the_tint_follows_whether_the_change_is_good(double delta, bool invert, string arrow, bool good)
    {
        var stat = new KlyDashboardStat { Label = "R", Value = 1, Delta = delta, InvertDelta = invert };

        var pill = All<Border>(stat).Last(b => All<Label>(b).Select(l => l.Text).Contains(arrow));
        Assert.Equal(DashboardParts.DeltaTone(delta, good).Background.Light, pill.BackgroundColor);
        Assert.False(AutomationProperties.GetIsInAccessibleTree(All<Label>(pill).First(l => l.Text == arrow)));
    }

    [Fact]
    public void A_caption_replaces_the_default_one()
    {
        var stat = new KlyDashboardStat { Label = "R", Value = 1, Compare = 2, Caption = "Since January" };

        Assert.Contains("Since January", Texts(stat));
        Assert.DoesNotContain(Texts(stat), t => t.StartsWith("vs "));
    }

    [Fact]
    public void A_goal_adds_a_percentage_and_a_progress_bar_that_turns_to_success_when_reached()
    {
        Invariant();
        var stat = new KlyDashboardStat { Label = "Signups", Value = 50, Goal = 200 };

        Assert.Contains("Goal 200", Texts(stat));
        Assert.Contains("25%", Texts(stat));
        var bar = All<Grid>(stat).Single(g => g.ColumnDefinitions.Count == 2 && g.ColumnDefinitions[0].Width.Value == 25);
        Assert.Equal(75, bar.ColumnDefinitions[1].Width.Value);
        var fill = Assert.IsType<Border>(bar.Children[0]);
        Assert.Equal(DashboardParts.PrimaryFill.Light, fill.BackgroundColor);

        stat.Value = 300;
        Assert.Contains("100%", Texts(stat));
        fill = Assert.IsType<Border>(All<Grid>(stat).Single(g => g.ColumnDefinitions.Count == 2 && g.ColumnDefinitions[0].Width.Value == 100).Children[0]);
        Assert.Equal(DashboardParts.FillOf(KlyColor.Success).Light, fill.BackgroundColor);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void A_goal_that_is_not_above_zero_is_ignored(double goal)
    {
        var stat = new KlyDashboardStat { Label = "R", Value = 5, Goal = goal };

        Assert.DoesNotContain(Texts(stat), t => t.StartsWith("Goal"));
    }

    [Fact]
    public void A_trend_of_two_points_or_more_draws_a_sparkline()
    {
        var stat = new KlyDashboardStat { Label = "R", Value = 1, Trend = [1, 2] };
        Assert.Single(All<DashboardSparkline>(stat));

        stat.Trend = [1];
        Assert.Empty(All<DashboardSparkline>(stat));
    }

    [Fact]
    public void A_status_is_a_badge_in_the_header_and_leaves_with_the_status()
    {
        var stat = new KlyDashboardStat { Label = "Uptime", Value = 99.9, Status = new DashboardStatus("Healthy", "success") };

        Assert.Same(stat.StatusBadge, stat.HeaderRight);
        Assert.Equal("Healthy", stat.StatusBadge.Text);
        Assert.Equal("success", stat.StatusBadge.Color.Value);
        Assert.Equal(KlyVariant.Subtle, stat.StatusBadge.Variant);
        Assert.Equal(KlySize.Sm, stat.StatusBadge.Size);

        stat.Status = new DashboardStatus("Unknown");
        Assert.Equal(KlyColor.Neutral, stat.StatusBadge.Color);

        stat.Status = null;
        Assert.Null(stat.HeaderRight);
    }

    [Fact]
    public void A_stat_tabulates_its_label_value_comparison_change_and_goal()
    {
        Invariant();
        var stat = new KlyDashboardStat { Label = "Revenue", Value = 120, Compare = 100, Goal = 200 };
        var table = stat.Tabulate!();

        Assert.Equal(["label", "value", "previous", "change", "goal"], table.Columns.Select(c => c.Key));
        Assert.Equal(["Revenue", "120", "100", "20", "200"], Row(table, 0, "label", "value", "previous", "change", "goal"));
        Assert.Equal("Change (%)", table.Columns[3].Label);

        stat.TextValue = "Fine";
        stat.Compare = null;
        stat.Goal = null;
        table = stat.Tabulate!();
        Assert.Equal(["label", "value"], table.Columns.Select(c => c.Key));
        Assert.Equal("Fine", table.Rows[0]["value"]);
    }

    [Fact]
    public void The_change_column_is_named_for_how_the_delta_is_formatted()
    {
        var stat = new KlyDashboardStat { Label = "C", Value = 0.6, Compare = 0.5, Format = DashboardFormatPreset.Ratio, CompareMode = DashboardCompareMode.Difference };
        Assert.Equal("Change (pts)", stat.Tabulate!().Columns.Last().Label);

        stat.Format = DashboardFormatPreset.Number;
        Assert.Equal("Change", stat.Tabulate!().Columns.Last().Label);
    }

    // --- KlyDashboardStats -----------------------------------------------------------------------------------------------------------------

    private static IReadOnlyList<DashboardStatsItem> Items() =>
    [
        new("a", "Users", 1200, Delta: 12.5, Caption: "this week", Progress: 40, Status: new DashboardStatus("Live", "success")),
        new("b", "Churn", 2.1, DashboardFormatPreset.Percent, Delta: -0.4, InvertDelta: true),
        new("c", "Errors", null),
        new("d", "Latency", 180, Delta: 0),
        new("e", "Orders", 70),
    ];

    [Fact]
    public void Stats_are_twelve_wide_and_empty_without_items()
    {
        var stats = new KlyDashboardStats();

        Assert.Equal(12, stats.Span);
        Assert.Equal(DashboardSkeletonKind.Stats, stats.Skeleton);
        Assert.True(stats.IsEmpty);
        Assert.Equal(DashboardBlockPhase.Empty, stats.Phase);

        stats.Items = Items();
        Assert.False(stats.IsEmpty);
        Assert.Equal(DashboardBlockPhase.Content, stats.Phase);
    }

    [Theory]
    [InlineData(1200, 4)]
    [InlineData(800, 2)]
    [InlineData(640, 2)]
    [InlineData(500, 1)]
    public void Stats_sit_in_four_two_or_one_columns_by_the_width_of_the_page(double width, int columns)
    {
        var stats = new KlyDashboardStats { Items = Items() };

        stats.Resize(width);

        Assert.Equal(columns, stats.Columns);
        var grid = All<Grid>(PhaseOf(stats)).First(g => g.ColumnDefinitions.Count == columns && g.RowSpacing == 16);
        Assert.Equal(columns, grid.ColumnDefinitions.Count);
        Assert.Equal((int)Math.Ceiling(5.0 / columns), grid.RowDefinitions.Count);
    }

    [Fact]
    public void Each_metric_shows_its_value_change_caption_status_and_progress()
    {
        Invariant();
        var stats = new KlyDashboardStats { Items = Items() };
        stats.Resize(1200);

        var texts = Texts(stats);
        Assert.Contains("Users", texts);
        Assert.Contains("1.2K", texts.Concat(["1.2K"]));
        Assert.Contains(DashboardFormat.Format(1200, DashboardFormatPreset.Number), texts);
        Assert.Contains("+12.5%", texts);
        Assert.Contains("this week", texts);
        Assert.Contains("Live", texts);
        Assert.Contains("2.1%", texts);
        Assert.Contains("-0.4%", texts);
        Assert.Contains(DashboardFormat.MissingValue, texts);
        Assert.Contains("0%", texts);
        Assert.Single(All<Grid>(stats), g => g.ColumnDefinitions.Count == 2 && g.ColumnDefinitions[0].Width.Value == 40);
    }

    [Fact]
    public void A_change_is_green_when_good_red_when_bad_muted_when_zero_and_inverted_on_request()
    {
        var stats = new KlyDashboardStats { Items = Items() };

        Color Of(string text) => LabelWith(stats, text).TextColor;
        Assert.Equal(DashboardParts.TextOf(KlyColor.Success).Light, Of("+12.5%"));
        Assert.Equal(DashboardParts.TextOf(KlyColor.Success).Light, Of("-0.4%"));
        Assert.Equal(KlyTheme.TextMuted.Light, Of("0%"));

        stats.Items = [new("x", "Cost", 1, Delta: 4, InvertDelta: true)];
        Assert.Equal(DashboardParts.TextOf(KlyColor.Error).Light, Of("+4%"));
    }

    [Fact]
    public void Progress_is_clamped_between_zero_and_a_hundred()
    {
        var stats = new KlyDashboardStats { Items = [new("a", "A", 1, Progress: 250), new("b", "B", 1, Progress: -20)] };
        stats.Resize(500);

        var grids = All<Grid>(stats).Where(g => g.ColumnDefinitions.Count == 2 && g.ColumnDefinitions[0].Width.Value is 100 or 0.0001).ToList();
        Assert.Equal(2, grids.Count);
    }

    [Fact]
    public void Tiles_put_each_metric_in_a_bordered_tile()
    {
        var stats = new KlyDashboardStats { Items = Items() };
        var plain = All<Border>(PhaseOf(stats)).Count(b => b.StrokeThickness == 1);

        stats.Variant = DashboardStatsVariant.Tiles;

        Assert.Equal(plain + 5, All<Border>(PhaseOf(stats)).Count(b => b.StrokeThickness == 1));
    }

    [Fact]
    public void Stats_tabulate_the_label_value_and_change_of_every_item()
    {
        Invariant();
        var table = new KlyDashboardStats { Items = Items() }.Tabulate!();

        Assert.Equal(["label", "value", "change"], table.Columns.Select(c => c.Key));
        Assert.Equal(5, table.Rows.Count);
        Assert.Equal(["Users", "1200", "12.5"], Row(table, 0, "label", "value", "change"));
        Assert.Null(table.Rows[2]["value"]);
    }

    // --- KlyDashboardBars ------------------------------------------------------------------------------------------------------------------

    private static IReadOnlyList<DashboardBarItem> Bars() =>
    [
        new("Direct", 400, Tag: "web"),
        new("Search", 200, Meta: "200 visits"),
        new("Social", 0),
    ];

    private static List<DashboardBarView> BarViews(KlyDashboardCard card) => All<DashboardBarView>(card);

    [Fact]
    public void Bars_are_six_wide_and_scale_to_the_largest_item_with_zero_on_the_left()
    {
        var bars = new KlyDashboardBars { Items = Bars() };

        Assert.Equal(6, bars.Span);
        Assert.Equal(DashboardSkeletonKind.Bars, bars.Skeleton);
        var views = BarViews(bars);
        Assert.Equal([100.0, 50.0, 0.0], views.Select(v => v.Length));
        Assert.All(views, v => Assert.Equal(0, v.Left));
        Assert.All(views, v => Assert.Null(v.ZeroPercent));
    }

    [Fact]
    public void A_negative_value_extends_to_the_left_of_a_zero_line_and_max_replaces_the_largest_value()
    {
        var bars = new KlyDashboardBars { Items = [new("Up", 30), new("Down", -10)] };

        var views = BarViews(bars);
        Assert.Equal(25, views[0].Left);
        Assert.Equal(75, views[0].Length);
        Assert.Equal(0, views[1].Left);
        Assert.Equal(25, views[1].Length);
        Assert.Equal(25, views[1].ZeroPercent);

        bars.Max = 60;
        bars.Items = [new("Half", 30)];
        Assert.Equal(50, BarViews(bars)[0].Length);
    }

    [Fact]
    public void A_bar_shows_its_label_tag_and_value_or_its_own_text()
    {
        Invariant();
        var bars = new KlyDashboardBars { Items = Bars() };

        Assert.Equal(["Direct", "web", "400", "Search", "200 visits", "Social", "0"], Texts(PhaseOf(bars)));
    }

    [Fact]
    public void A_limit_keeps_the_first_bars_in_the_view_and_the_table()
    {
        var bars = new KlyDashboardBars { Items = Bars(), Limit = 2 };

        Assert.Equal(2, BarViews(bars).Count);
        Assert.Equal(2, bars.Tabulate!().Rows.Count);

        bars.Limit = 0;
        Assert.Equal(3, BarViews(bars).Count);
    }

    [Fact]
    public void The_first_emphasis_mutes_every_bar_but_the_first()
    {
        var bars = new KlyDashboardBars { Items = Bars() };
        var all = BarViews(bars);
        Assert.All(all, v => Assert.Equal(DashboardParts.PrimaryFill, v.Fill));

        bars.Emphasis = DashboardBarsEmphasis.First;
        var views = BarViews(bars);
        Assert.Equal(DashboardParts.PrimaryFill, views[0].Fill);
        Assert.NotEqual(DashboardParts.PrimaryFill, views[1].Fill);
        Assert.Equal(views[1].Fill, views[2].Fill);
    }

    [Fact]
    public void A_funnel_scales_to_the_first_step_and_shows_the_share_of_the_previous_one()
    {
        var bars = new KlyDashboardBars
        {
            Variant = DashboardBarsVariant.Funnel,
            Max = 5,
            Items = [new("Visits", 1000), new("Signups", 250), new("Paid", 50), new("Retained", 0)],
        };

        Assert.Equal(DashboardSkeletonKind.Funnel, bars.Skeleton);
        Assert.Equal([100.0, 25.0, 5.0, 0.0], BarViews(bars).Select(v => v.Length));
        var texts = Texts(PhaseOf(bars));
        Assert.Contains("base 100%", texts);
        Assert.Contains("25% of the previous step", texts);
        Assert.Contains("20% of the previous step", texts);
        Assert.Contains("0% of the previous step", texts);

        bars.Items = [new("A", 0), new("B", 5)];
        Assert.Contains(DashboardFormat.MissingValue, Texts(PhaseOf(bars)));
    }

    [Fact]
    public void Without_a_listener_a_bar_is_not_a_button_and_with_one_the_row_is()
    {
        var bars = new KlyDashboardBars { Items = Bars() };
        Assert.Empty(All<DashboardTapArea>(bars));

        var picked = new List<string>();
        bars.Selected += (_, item) => picked.Add(item.Label);
        var areas = All<DashboardTapArea>(bars);
        Assert.Equal(3, areas.Count);
        Assert.All(areas, a => Assert.Equal(40, a.MinimumTarget));
        Assert.StartsWith("Direct, ", areas[0].Description);

        areas[1].SendTap();
        Assert.Equal(["Search"], picked);
    }

    [Fact]
    public void Bars_tabulate_labels_and_raw_values()
    {
        var table = new KlyDashboardBars { Items = Bars() }.Tabulate!();

        Assert.Equal(["label", "value"], table.Columns.Select(c => c.Key));
        Assert.Equal(["Direct", "400"], Row(table, 0, "label", "value"));
        Assert.True(table.Columns[1].Numeric);
    }

    [Fact]
    public void Bars_without_items_are_empty()
    {
        Assert.True(new KlyDashboardBars().IsEmpty);
        Assert.Equal(DashboardBlockPhase.Empty, new KlyDashboardBars { Items = [] }.Phase);
    }

    // --- KlyDashboardGauge -----------------------------------------------------------------------------------------------------------------

    [Fact]
    public void A_gauge_is_four_wide_a_ring_from_zero_to_a_hundred()
    {
        var gauge = new KlyDashboardGauge();

        Assert.Equal(4, gauge.Span);
        Assert.Equal(DashboardSkeletonKind.Gauge, gauge.Skeleton);
        Assert.Equal(DashboardGaugeVariant.Ring, gauge.Variant);
        Assert.Equal(0, gauge.Min);
        Assert.Equal(100, gauge.Max);
    }

    [Theory]
    [InlineData(50.0, 0.0, 100.0, 0.5)]
    [InlineData(150.0, 0.0, 100.0, 1.0)]
    [InlineData(-10.0, 0.0, 100.0, 0.0)]
    [InlineData(75.0, 50.0, 100.0, 0.5)]
    [InlineData(5.0, 10.0, 10.0, 0.0)]
    [InlineData(5.0, 20.0, 10.0, 0.0)]
    [InlineData(double.NaN, 0.0, 100.0, 0.0)]
    [InlineData(double.PositiveInfinity, 0.0, 100.0, 0.0)]
    public void The_share_is_the_value_in_its_range_clamped_and_zero_when_there_is_no_range_or_no_number(double value, double min, double max, double share)
    {
        var gauge = new KlyDashboardGauge { Value = value, Min = min, Max = max };

        Assert.Equal(share, gauge.Share, 6);
    }

    [Fact]
    public void A_gauge_without_a_value_has_a_zero_share_and_a_dash()
    {
        var gauge = new KlyDashboardGauge();

        Assert.Equal(0, gauge.Share);
        Assert.Equal(DashboardFormat.MissingValue, gauge.FormattedValue);
        Assert.Equal(0, All<DashboardGaugeRing>(gauge).Single().Share);
    }

    [Fact]
    public void The_ring_shows_the_value_in_the_middle_with_its_caption_and_a_target_line()
    {
        Invariant();
        var gauge = new KlyDashboardGauge { Value = 72, Caption = "of quota", Target = 90, Format = DashboardFormatPreset.Percent, Title = "Quota" };

        Assert.Equal(0.72, All<DashboardGaugeRing>(gauge).Single().Share, 6);
        Assert.Empty(All<DashboardGaugeBar>(gauge));
        Assert.Equal(["Quota", "72%", "of quota", "Target: 90%"], Texts(gauge));
    }

    [Fact]
    public void The_bar_shows_the_scale_and_a_marker_at_the_target()
    {
        Invariant();
        var gauge = new KlyDashboardGauge { Value = 30, Min = 0, Max = 200, Target = 150, Variant = DashboardGaugeVariant.Bar, Caption = "used" };

        var bar = All<DashboardGaugeBar>(gauge).Single();
        Assert.Empty(All<DashboardGaugeRing>(gauge));
        Assert.Equal(0.15, bar.Share, 6);
        Assert.Equal(0.75, bar.TargetShare);
        Assert.Equal(["30", "used", "0", "200", "Target: 150"], Texts(PhaseOf(gauge)));

        gauge.Target = null;
        Assert.Null(All<DashboardGaugeBar>(gauge).Single().TargetShare);
        gauge.Target = 500;
        Assert.Equal(1, gauge.TargetShare);
        gauge.Target = double.NaN;
        Assert.Null(gauge.TargetShare);
    }

    [Fact]
    public void A_gauge_is_a_meter_for_a_screen_reader()
    {
        Invariant();
        var gauge = new KlyDashboardGauge { Value = 40, Min = 0, Max = 80, Title = "CPU", Format = DashboardFormatPreset.Integer };
        var meter = Assert.IsType<VerticalStackLayout>(PhaseOf(gauge));

        Assert.Equal("CPU: 40", SemanticProperties.GetDescription(meter));
        Assert.Equal("0 - 80", SemanticProperties.GetHint(meter));

        gauge.Title = null;
        Assert.Equal("Gauge: 40", SemanticProperties.GetDescription(PhaseOf(gauge)));
    }

    [Fact]
    public void A_gauge_tabulates_its_range_and_its_target_when_it_has_one()
    {
        Invariant();
        var gauge = new KlyDashboardGauge { Value = 40, Min = 10, Max = 80, Title = "CPU" };
        var table = gauge.Tabulate!();

        Assert.Equal(["label", "value", "min", "max"], table.Columns.Select(c => c.Key));
        Assert.Equal(["CPU", "40", "10", "80"], Row(table, 0, "label", "value", "min", "max"));

        gauge.Target = 60;
        gauge.Value = double.NaN;
        table = gauge.Tabulate!();
        Assert.Equal("target", table.Columns.Last().Key);
        Assert.Equal("60", table.Rows[0]["target"]);
        Assert.Null(table.Rows[0]["value"]);
    }
}
