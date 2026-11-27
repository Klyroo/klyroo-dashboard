using System.Globalization;
using static Klyroo.Dashboard.Maui.Tests.TestHelpers;

namespace Klyroo.Dashboard.Maui.Tests;

public class DashboardListsTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 10, 12, 0, 0, TimeSpan.Zero);

    private static (int Row, int Column, int Span) At(View view) => (Grid.GetRow(view), Grid.GetColumn(view), Grid.GetColumnSpan(view));

    // --- KlyDashboardList ------------------------------------------------------------------------------------------------------------------

    private static IReadOnlyList<DashboardListItem> People() =>
    [
        new("Ada Lovelace", "Engineering", 1200, 40, 12.5, "AL"),
        new("Grace Hopper", Value: 800, Percent: 26.7, Delta: -3),
        new("Linus", Delta: 0, Href: "https://example.com/linus", Selected: true),
    ];

    [Fact]
    public void A_list_is_six_wide_and_empty_without_rows()
    {
        var list = new KlyDashboardList();

        Assert.Equal(6, list.Span);
        Assert.Equal(DashboardSkeletonKind.Rows, list.Skeleton);
        Assert.True(list.IsEmpty);

        list.Items = People();
        Assert.False(list.IsEmpty);
    }

    [Fact]
    public void A_row_shows_its_leading_text_label_description_percent_change_and_value()
    {
        Invariant();
        var list = new KlyDashboardList { Items = People() };

        var texts = Texts(PhaseOf(list));
        Assert.Equal(["AL", "Ada Lovelace", "Engineering", "40%", "+12.5%", "1,200", "Grace Hopper", "26.7%", "-3%", "800", "Linus", "0%"], texts);
    }

    [Fact]
    public void Rows_are_separated_by_dividers_and_the_first_and_last_have_no_outer_padding()
    {
        var list = new KlyDashboardList { Items = People() };

        var body = Assert.IsType<VerticalStackLayout>(PhaseOf(list));
        Assert.Equal(5, body.Children.Count);
        Assert.Equal(2, body.Children.OfType<Border>().Count(b => b.HeightRequest == 1));
        var rows = body.Children.OfType<Border>().Where(b => b.Content is Grid).Select(b => (Grid)b.Content!).ToList();
        Assert.Equal(new Thickness(0, 0, 0, 8), rows[0].Padding);
        Assert.Equal(new Thickness(0, 8, 0, 8), rows[1].Padding);
        Assert.Equal(new Thickness(0, 8, 0, 0), rows[2].Padding);
    }

    [Fact]
    public void A_change_is_green_red_or_muted_by_its_sign()
    {
        Invariant();
        var list = new KlyDashboardList { Items = People() };

        Assert.Equal(DashboardParts.TextOf(KlyColor.Success).Light, LabelWith(list, "+12.5%").TextColor);
        Assert.Equal(DashboardParts.TextOf(KlyColor.Error).Light, LabelWith(list, "-3%").TextColor);
        Assert.Equal(KlyTheme.TextMuted.Light, All<Label>(list).Last(l => l.Text == "0%").TextColor);
    }

    [Fact]
    public void A_limit_shows_the_first_rows_in_the_view_and_the_table()
    {
        var list = new KlyDashboardList { Items = People(), Limit = 2 };

        Assert.Equal(2, list.Visible.Count);
        Assert.DoesNotContain("Linus", Texts(list));
        Assert.Equal(2, list.Tabulate!().Rows.Count);
    }

    [Fact]
    public void A_row_with_a_link_underlines_its_label_and_opens_it_when_tapped()
    {
        var list = new KlyDashboardList { Items = People() };

        var link = LabelWith(list, "Linus");
        Assert.Equal(TextDecorations.Underline, link.TextDecorations);
        Assert.Single(link.GestureRecognizers.OfType<TapGestureRecognizer>());
        Assert.Equal("https://example.com/linus", SemanticProperties.GetHint(link));
        Assert.Equal(TextDecorations.None, LabelWith(list, "Ada Lovelace").TextDecorations);
    }

    [Fact]
    public void With_a_listener_a_row_is_a_button_unless_it_has_a_link()
    {
        var list = new KlyDashboardList { Items = People() };
        Assert.Empty(All<DashboardTapArea>(list));

        var clicked = new List<string>();
        list.RowClicked += (_, item) => clicked.Add(item.Label);

        var areas = All<DashboardTapArea>(list);
        Assert.Equal(2, areas.Count);
        Assert.Equal("Ada Lovelace, Engineering, 40%, +12.5%, 1,200", areas[0].Description);
        Assert.All(areas, a => Assert.Equal(40, a.MinimumTarget));

        areas[1].SendTap();
        Assert.Equal(["Grace Hopper"], clicked);
    }

    [Fact]
    public void The_selected_row_is_highlighted()
    {
        var list = new KlyDashboardList { Items = [new("A", Selected: true), new("B")] };

        var frames = All<Border>(PhaseOf(list)).Where(b => b.Content is Grid).ToList();
        Assert.Equal(KlyTheme.BackgroundMuted.WithAlpha(0.5f).Light, frames[0].BackgroundColor);
        Assert.NotEqual(frames[0].BackgroundColor, frames[1].BackgroundColor);
    }

    [Fact]
    public void A_list_tabulates_only_the_columns_its_rows_use()
    {
        Invariant();
        var table = new KlyDashboardList { Items = People() }.Tabulate!();
        Assert.Equal(["label", "description", "percent", "change", "value"], table.Columns.Select(c => c.Key));
        Assert.Equal(["Ada Lovelace", "Engineering", "40", "12.5", "1200"], new[] { table.Rows[0]["label"], table.Rows[0]["description"], table.Rows[0]["percent"], table.Rows[0]["change"], table.Rows[0]["value"] });

        var plain = new KlyDashboardList { Items = [new("A", Value: 1)] }.Tabulate!();
        Assert.Equal(["label", "value"], plain.Columns.Select(c => c.Key));
    }

    // --- KlyDashboardFeed ------------------------------------------------------------------------------------------------------------------

    private static IReadOnlyList<DashboardFeedItem> Events() =>
    [
        new("Deploy finished", Now.AddMinutes(-5), "v1.4.2", "CI"),
        new("Invoice paid", Now.AddHours(-2)),
        new("User signed up", Now.AddDays(-1).AddHours(-1), AvatarText: "AL"),
        new("Plan changed", Now.AddDays(-3)),
    ];

    [Fact]
    public void A_feed_is_six_wide_and_empty_without_events()
    {
        var feed = new KlyDashboardFeed();

        Assert.Equal(6, feed.Span);
        Assert.Equal(DashboardSkeletonKind.Feed, feed.Skeleton);
        Assert.True(feed.GroupByDay);
        Assert.True(feed.IsEmpty);
    }

    [Fact]
    public void A_feed_inserts_a_heading_when_the_day_changes()
    {
        var feed = new KlyDashboardFeed { Items = Events(), Now = Now };

        var texts = Texts(PhaseOf(feed));
        Assert.Equal(["Today", "CI", "Deploy finished", "5 min ago", "v1.4.2", "•", "Invoice paid", "2 hr ago", "Yesterday", "AL", "User signed up", "yesterday", "2026-06-07", "•", "Plan changed", "3 days ago"], texts);
    }

    [Fact]
    public void The_day_headings_are_uppercase_headings_for_a_screen_reader()
    {
        var feed = new KlyDashboardFeed { Items = Events(), Now = Now };

        var day = LabelWith(feed, "Today");
        Assert.Equal(TextTransform.Uppercase, day.TextTransform);
        Assert.Equal(SemanticHeadingLevel.Level4, SemanticProperties.GetHeadingLevel(day));
    }

    [Fact]
    public void Without_grouping_there_are_no_day_headings()
    {
        var feed = new KlyDashboardFeed { Items = Events(), Now = Now, GroupByDay = false };

        var texts = Texts(PhaseOf(feed));
        Assert.DoesNotContain("Today", texts);
        Assert.DoesNotContain("Yesterday", texts);
        Assert.DoesNotContain("2026-06-07", texts);
    }

    [Fact]
    public void A_feed_keeps_the_order_it_is_given_and_cuts_at_the_limit()
    {
        var feed = new KlyDashboardFeed { Items = [Events()[3], Events()[0]], Now = Now, Limit = 1 };

        Assert.Equal(["2026-06-07", "•", "Plan changed", "3 days ago"], Texts(PhaseOf(feed)));
        Assert.Single(feed.Tabulate!().Rows);
    }

    [Fact]
    public void The_time_of_an_event_is_spoken_in_full_for_a_screen_reader()
    {
        var feed = new KlyDashboardFeed { Items = Events(), Now = Now };

        Assert.Equal("5 min ago, 2026-06-10 11:55:00Z", SemanticProperties.GetDescription(LabelWith(feed, "5 min ago")));
    }

    [Fact]
    public void A_feed_tabulates_iso_times()
    {
        var table = new KlyDashboardFeed { Items = Events(), Now = Now }.Tabulate!();

        Assert.Equal(["time", "label", "description"], table.Columns.Select(c => c.Key));
        Assert.Equal("2026-06-10T11:55:00.0000000+00:00", table.Rows[0]["time"]);
        Assert.Equal("Deploy finished", table.Rows[0]["label"]);
    }

    // --- KlyDashboardAlerts ----------------------------------------------------------------------------------------------------------------

    private static IReadOnlyList<DashboardAlertItem> Alerts() =>
    [
        new(DashboardAlertSeverity.Success, "Backup done", Value: "OK"),
        new(DashboardAlertSeverity.Warning, "Disk 85%", "node-3", "85%", "Review", "https://example.com/disk"),
        new(DashboardAlertSeverity.Error, "API down", ActionLabel: "Restart"),
        new(DashboardAlertSeverity.Info, "Update available"),
    ];

    [Fact]
    public void Alerts_are_six_wide_and_show_nothing_needs_attention_when_empty()
    {
        var alerts = new KlyDashboardAlerts();

        Assert.Equal(6, alerts.Span);
        Assert.True(alerts.IsEmpty);
        Assert.Equal(["Nothing needs attention"], Texts(PhaseOf(alerts)));
    }

    [Fact]
    public void Alerts_are_ordered_by_severity_unless_asked_to_keep_the_order()
    {
        var alerts = new KlyDashboardAlerts { Items = Alerts() };

        Assert.Equal(["API down", "Disk 85%", "Update available", "Backup done"], alerts.OrderedItems.Select(i => i.Label));

        alerts.Order = DashboardAlertOrder.AsGiven;
        Assert.Equal(["Backup done", "Disk 85%", "API down", "Update available"], alerts.OrderedItems.Select(i => i.Label));
    }

    [Fact]
    public void The_limit_applies_after_the_ordering_and_to_the_table()
    {
        var alerts = new KlyDashboardAlerts { Items = Alerts(), Limit = 2 };

        Assert.Equal(["API down", "Disk 85%"], alerts.OrderedItems.Select(i => i.Label));
        Assert.Equal(["API down", "Disk 85%"], alerts.Tabulate!().Rows.Select(r => r["label"]));
    }

    [Fact]
    public void Each_alert_has_a_badge_of_its_severity_in_its_color()
    {
        var alerts = new KlyDashboardAlerts { Items = Alerts() };

        var badges = All<KlyBadge>(alerts);
        Assert.Equal(["Critical", "Warning", "Info", "Resolved"], badges.Select(b => b.Text));
        Assert.Equal(["error", "warning", "info", "success"], badges.Select(b => b.Color.Value));
        Assert.All(badges, b =>
        {
            Assert.Equal(KlyVariant.Subtle, b.Variant);
            Assert.Equal(KlySize.Sm, b.Size);
        });
    }

    [Fact]
    public void An_alert_shows_its_label_description_and_value()
    {
        var alerts = new KlyDashboardAlerts { Items = [Alerts()[1]] };

        Assert.Equal(["Warning", "Disk 85%", "node-3", "85%"], Texts(PhaseOf(alerts)).Concat([]).Take(4));
    }

    [Fact]
    public void An_action_with_a_link_always_shows_one_and_without_a_link_only_when_something_listens()
    {
        var alerts = new KlyDashboardAlerts { Items = Alerts() };

        Assert.Equal(["Review"], All<Button>(alerts).Where(b => b is not KlyButton).Select(b => b.Text));

        var restarted = new List<string>();
        alerts.ActionInvoked += (_, item) => restarted.Add(item.Label);
        var buttons = All<Button>(alerts).Where(b => b is not KlyButton).ToList();
        Assert.Equal(["Restart", "Review"], buttons.Select(b => b.Text));
        Assert.All(buttons, b => Assert.Equal(40, b.MinimumHeightRequest));

        buttons[0].SendClicked();
        Assert.Equal(["API down"], restarted);

        // The link keeps its own action: it opens the address and does not raise the event.
        buttons[1].SendClicked();
        Assert.Equal(["API down"], restarted);
    }

    [Fact]
    public void Alerts_tabulate_the_severity_label_in_the_order_shown()
    {
        var table = new KlyDashboardAlerts { Items = Alerts() }.Tabulate!();

        Assert.Equal(["severity", "label", "description", "value"], table.Columns.Select(c => c.Key));
        Assert.Equal(["Critical", "API down", null, null], new[] { table.Rows[0]["severity"], table.Rows[0]["label"], table.Rows[0]["description"], table.Rows[0]["value"] });
    }

    // --- KlyDashboardDetails ---------------------------------------------------------------------------------------------------------------

    private static IReadOnlyList<DashboardDetailsItem> Facts() =>
    [
        new("id", "Customer ID", "cus_123", Mono: true, Copy: true),
        new("site", "Website", "https://example.com", Href: "https://example.com", Hint: "Public"),
        new("note", "Notes", "Long text", FullSpan: true),
        new("empty", "Phone", "  ", Copy: true),
        new("secret", "Token", "hidden", Hidden: true),
        new("plan", "Plan", "Pro"),
    ];

    [Fact]
    public void Details_are_six_wide_in_two_columns_and_skip_hidden_rows()
    {
        var details = new KlyDashboardDetails { Items = Facts() };

        Assert.Equal(6, details.Span);
        Assert.Equal(DashboardSkeletonKind.Details, details.Skeleton);
        Assert.Equal(2, details.Columns);
        Assert.Equal(5, details.VisibleItems.Count);
        Assert.DoesNotContain("Token", Texts(details));
        Assert.True(new KlyDashboardDetails().IsEmpty);
        Assert.True(new KlyDashboardDetails { Items = [new("x", "X", "1", Hidden: true)] }.IsEmpty);
    }

    [Fact]
    public void Rows_fill_the_columns_and_a_full_span_row_takes_both()
    {
        var details = new KlyDashboardDetails { Items = Facts() };

        var grid = Assert.IsType<Grid>(PhaseOf(details));
        var cells = grid.Children.OfType<View>().ToList();
        Assert.Equal([(0, 0, 1), (0, 1, 1), (1, 0, 2), (2, 0, 1), (2, 1, 1)], cells.Select(At));
    }

    [Theory]
    [InlineData(500, 1)]
    [InlineData(700, 2)]
    public void The_rows_sit_in_one_column_below_640_units_and_two_above(double width, int columns)
    {
        var details = new KlyDashboardDetails { Items = Facts() };

        details.Resize(width);

        Assert.Equal(columns, details.Columns);
        var grid = Assert.IsType<Grid>(PhaseOf(details));
        Assert.Equal(columns, grid.ColumnDefinitions.Count);
        Assert.All(grid.Children.OfType<View>(), v => Assert.True(Grid.GetColumn(v) < columns));
        if (columns == 1) Assert.Equal([0, 1, 2, 3, 4], grid.Children.OfType<View>().Select(Grid.GetRow));
    }

    [Fact]
    public void A_value_is_shown_in_monospace_a_link_underlined_and_an_empty_one_as_a_dash()
    {
        var details = new KlyDashboardDetails { Items = Facts() };

        Assert.Equal(DashboardParts.MonoFont(), LabelWith(details, "cus_123").FontFamily);
        var link = LabelWith(details, "https://example.com");
        Assert.Equal(TextDecorations.Underline, link.TextDecorations);
        Assert.Equal("https://example.com", SemanticProperties.GetHint(link));
        Assert.Equal(DashboardFormat.MissingValue, LabelWith(details, DashboardFormat.MissingValue).Text);
        Assert.Contains("Public", Texts(details));
    }

    [Fact]
    public void Only_a_value_that_is_not_empty_gets_a_copy_button_named_for_its_row()
    {
        var details = new KlyDashboardDetails { Items = Facts() };

        var copies = All<DashboardTapArea>(details);
        Assert.Equal(["Copy Customer ID"], copies.Select(c => c.Description));
        Assert.Equal(40, copies[0].WidthRequest);
        Assert.Contains(All<KlyIcon>(copies[0]), i => i.Name == KlyIconNames.Copy);
        // The tap hands the text to the clipboard, which a device without one refuses silently.
        copies[0].SendTap();
    }

    [Fact]
    public void Details_tabulate_label_and_value_of_the_rows_that_are_shown()
    {
        var table = new KlyDashboardDetails { Items = Facts() }.Tabulate!();

        Assert.Equal(["label", "value"], table.Columns.Select(c => c.Key));
        Assert.Equal(5, table.Rows.Count);
        Assert.Equal(["Customer ID", "cus_123"], new[] { table.Rows[0]["label"], table.Rows[0]["value"] });
    }

    // --- KlyDashboardTable -----------------------------------------------------------------------------------------------------------------

    private sealed record Region(string Name, double Revenue, double? Change, double? Share, DateOnly Since, int? Orders);

    private static IReadOnlyList<Region> Regions() =>
    [
        new("emea", 300, 12.5, 40, new DateOnly(2024, 3, 1), 10),
        new("Apac", 100, -4, 10, new DateOnly(2023, 1, 15), null),
        new("amer", 500, null, 50, new DateOnly(2025, 7, 9), 30),
        new("latam", 200, 0, null, new DateOnly(2022, 5, 5), 5),
    ];

    private static IReadOnlyList<DashboardTableColumn<Region>> Columns() =>
    [
        DashboardTableColumn.For<Region, string>("name", "Region", r => r.Name),
        DashboardTableColumn.For<Region, double>("revenue", "Revenue", r => r.Revenue, DashboardFormatPreset.Compact, DashboardTableColumnKind.Bar),
        DashboardTableColumn.For<Region, double?>("change", "Change", r => r.Change, kind: DashboardTableColumnKind.Delta),
        DashboardTableColumn.For<Region, double?>("share", "Share", r => r.Share, kind: DashboardTableColumnKind.Percent),
        DashboardTableColumn.For<Region, int?>("orders", "Orders", r => r.Orders),
        DashboardTableColumn.For<Region, DateOnly>("since", "Since", r => r.Since, sortable: false),
    ];

    private static KlyDashboardTable<Region> Table() => new() { Columns = Columns(), Rows = Regions() };

    private static List<string> FirstColumn(KlyDashboardTable<Region> table) =>
        All<Label>(PhaseOf(table)).Where(l => Grid.GetColumn(l) == 0 && Grid.GetRow(l) > 0).OrderBy(Grid.GetRow).Select(l => l.Text!).ToList();

    private static KlyButton Header(KlyDashboardTable<Region> table, string key)
    {
        var label = Columns().First(c => c.Key == key).Label;
        return All<KlyButton>(PhaseOf(table)).First(b => b.Text.StartsWith(label));
    }

    [Fact]
    public void A_table_is_twelve_wide_without_a_view_as_table_button_and_empty_without_rows()
    {
        var table = new KlyDashboardTable<Region>();

        Assert.Equal(12, table.Span);
        Assert.Equal(DashboardSkeletonKind.Table, table.Skeleton);
        Assert.False(table.ViewAsTable);
        Assert.True(table.IsEmpty);

        table.Rows = Regions();
        Assert.False(table.IsEmpty);
    }

    [Fact]
    public void A_table_has_a_header_and_a_row_per_item_that_scrolls_sideways()
    {
        Invariant();
        var table = Table();

        var scroll = Assert.IsType<ScrollView>(PhaseOf(table));
        Assert.Equal(ScrollOrientation.Horizontal, scroll.Orientation);
        Assert.Equal(6, Assert.IsType<Grid>(scroll.Content).ColumnDefinitions.Count);
        Assert.Equal(["emea", "Apac", "amer", "latam"], FirstColumn(table));
        Assert.Equal("Region", Header(table, "name").Text);
        Assert.Equal(0, Grid.GetRow(Header(table, "name")));
    }

    [Fact]
    public void Cells_show_numbers_deltas_percentages_dates_and_a_dash_for_a_missing_value()
    {
        Invariant();
        var table = Table();

        var first = All<Label>(PhaseOf(table)).Where(l => Grid.GetRow(l) == 2).ToDictionary(Grid.GetColumn, l => l.Text);
        Assert.Equal("emea", first[0]);
        Assert.Equal("+12.5%", first[2]);
        Assert.Equal("40%", first[3]);
        Assert.Equal("10", first[4]);
        Assert.Equal(new DateOnly(2024, 3, 1).ToString(CultureInfo.CurrentCulture), first[5]);

        var amer = All<Label>(PhaseOf(table)).Where(l => Grid.GetRow(l) == 6).ToDictionary(Grid.GetColumn, l => l.Text);
        Assert.Equal(DashboardFormat.MissingValue, amer[2]);
        Assert.Equal(KlyTheme.TextDimmed.Light, All<Label>(PhaseOf(table)).First(l => Grid.GetRow(l) == 6 && Grid.GetColumn(l) == 2).TextColor);
    }

    [Fact]
    public void A_delta_cell_is_green_red_or_muted_and_the_invert_flag_swaps_the_good_direction()
    {
        Invariant();
        var table = Table();

        Color Of(string text) => LabelWith(PhaseOf(table), text).TextColor;
        Assert.Equal(DashboardParts.TextOf(KlyColor.Success).Light, Of("+12.5%"));
        Assert.Equal(DashboardParts.TextOf(KlyColor.Error).Light, Of("-4%"));
        Assert.Equal(KlyTheme.TextMuted.Light, All<Label>(PhaseOf(table)).First(l => l.Text == "0%" && l.FontAttributes == FontAttributes.Bold).TextColor);

        table.Columns = [DashboardTableColumn.For<Region, double?>("change", "Change", r => r.Change, kind: DashboardTableColumnKind.Delta, invertDelta: true)];
        Assert.Equal(DashboardParts.TextOf(KlyColor.Error).Light, Of("+12.5%"));
        Assert.Equal(DashboardParts.TextOf(KlyColor.Success).Light, Of("-4%"));
    }

    [Fact]
    public void A_bar_cell_scales_to_the_column_and_a_negative_value_goes_left_of_a_zero_line()
    {
        Invariant();
        var table = new KlyDashboardTable<Region>
        {
            Columns = [DashboardTableColumn.For<Region, double>("revenue", "Revenue", r => r.Revenue, DashboardFormatPreset.Integer, DashboardTableColumnKind.Bar)],
            Rows = [Regions()[0] with { Revenue = 50 }, Regions()[1] with { Revenue = 100 }, Regions()[2] with { Revenue = -50 }],
        };

        var bars = All<DashboardBarView>(PhaseOf(table));
        Assert.Equal(3, bars.Count);
        Assert.Equal([64.0, 64.0, 64.0], bars.Select(b => b.WidthRequest));
        Assert.Equal(6, bars[0].HeightRequest);
        Assert.Equal([33.333, 66.667, 33.333], bars.Select(b => Math.Round(b.Length, 3)));
        Assert.Equal([33.333, 33.333, 0.0], bars.Select(b => Math.Round(b.Left, 3)));
        Assert.All(bars, b => Assert.Equal(33.333, Math.Round(b.ZeroPercent!.Value, 3)));
        Assert.Contains("50", Texts(PhaseOf(table)));
    }

    [Fact]
    public void A_numeric_column_sorts_descending_then_ascending_then_not_at_all_and_reports_each_change()
    {
        var table = Table();
        var changes = new List<DashboardTableSort?>();
        table.SortChanged += (_, sort) => changes.Add(sort);

        Header(table, "revenue").SendClicked();
        Assert.Equal(new DashboardTableSort("revenue", true), table.Sort);
        Assert.Equal(["amer", "emea", "latam", "Apac"], FirstColumn(table));
        Assert.EndsWith("↓", Header(table, "revenue").Text);

        Header(table, "revenue").SendClicked();
        Assert.Equal(new DashboardTableSort("revenue", false), table.Sort);
        Assert.Equal(["Apac", "latam", "emea", "amer"], FirstColumn(table));
        Assert.EndsWith("↑", Header(table, "revenue").Text);

        Header(table, "revenue").SendClicked();
        Assert.Null(table.Sort);
        Assert.Equal(["emea", "Apac", "amer", "latam"], FirstColumn(table));

        Assert.Equal([new DashboardTableSort("revenue", true), new DashboardTableSort("revenue", false), null], changes);
    }

    [Fact]
    public void A_text_column_sorts_ascending_first_ignoring_case_and_a_new_column_starts_over()
    {
        var table = Table();

        Header(table, "name").SendClicked();
        Assert.Equal(new DashboardTableSort("name", false), table.Sort);
        Assert.Equal(["amer", "Apac", "emea", "latam"], FirstColumn(table));

        Header(table, "name").SendClicked();
        Assert.Equal(new DashboardTableSort("name", true), table.Sort);
        Assert.Equal(["latam", "emea", "Apac", "amer"], FirstColumn(table));

        Header(table, "revenue").SendClicked();
        Assert.Equal(new DashboardTableSort("revenue", true), table.Sort);
    }

    [Fact]
    public void Empty_cells_come_last_whatever_the_direction()
    {
        var table = Table();
        table.Sort = new DashboardTableSort("orders", true);
        Assert.Equal(["amer", "emea", "latam", "Apac"], FirstColumn(table));

        table.Sort = new DashboardTableSort("orders", false);
        Assert.Equal(["latam", "emea", "amer", "Apac"], FirstColumn(table));
    }

    [Fact]
    public void A_sort_on_an_unknown_column_keeps_the_order_and_a_limit_cuts_after_sorting()
    {
        var table = Table();
        table.Sort = new DashboardTableSort("nope", true);
        Assert.Equal(["emea", "Apac", "amer", "latam"], FirstColumn(table));

        table.Sort = new DashboardTableSort("revenue", true);
        table.Limit = 2;
        Assert.Equal(["amer", "emea"], FirstColumn(table));
        Assert.Equal(["amer", "emea"], table.Tabulate!().Rows.Select(r => r["name"]));
    }

    [Fact]
    public void A_column_that_is_not_sortable_has_a_plain_header()
    {
        var table = Table();

        Assert.DoesNotContain(All<KlyButton>(PhaseOf(table)), b => b.Text.StartsWith("Since"));
        Assert.Contains("Since", Texts(PhaseOf(table)));
    }

    [Fact]
    public void Numeric_headers_and_cells_are_right_aligned_and_the_header_button_follows_them()
    {
        var table = Table();

        Assert.Equal(LayoutOptions.End, Header(table, "revenue").HorizontalOptions);
        Assert.Equal(LayoutOptions.Start, Header(table, "name").HorizontalOptions);
        Assert.Equal(40, Header(table, "name").MinimumHeightRequest);
    }

    [Fact]
    public void With_a_listener_the_whole_row_is_a_button_named_by_its_first_cell()
    {
        var table = Table();
        Assert.Equal(0, All<KlyButton>(PhaseOf(table)).Count(b => b.Text == string.Empty));

        var clicked = new List<string>();
        table.RowClicked += (_, row) => clicked.Add(row.Name);

        var rows = All<KlyButton>(PhaseOf(table)).Where(b => b.Text == string.Empty).ToList();
        Assert.Equal(4, rows.Count);
        Assert.Equal("emea", SemanticProperties.GetDescription(rows[0]));
        Assert.Equal(6, Grid.GetColumnSpan(rows[0]));
        Assert.Equal(40, rows[0].MinimumHeightRequest);

        rows[2].SendClicked();
        Assert.Equal(["amer"], clicked);
    }

    [Fact]
    public void A_table_tabulates_what_it_shows_with_invariant_text_and_raw_numbers()
    {
        var table = Table();
        table.Sort = new DashboardTableSort("revenue", true);
        var data = table.Tabulate!();

        Assert.Equal(["name", "revenue", "change", "share", "orders", "since"], data.Columns.Select(c => c.Key));
        Assert.Equal([false, true, true, true, true, false], data.Columns.Select(c => c.Numeric));
        Assert.Equal(["amer", "500", null, "50", "30", "2025-07-09"], data.Columns.Select(c => data.Rows[0][c.Key]));
        Assert.Equal(["amer", "emea", "latam", "Apac"], data.Rows.Select(r => r["name"]));
    }
}
