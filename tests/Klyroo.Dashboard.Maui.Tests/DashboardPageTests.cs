using static Klyroo.Dashboard.Maui.Tests.TestHelpers;

namespace Klyroo.Dashboard.Maui.Tests;

public class DashboardPageTests
{
    private static readonly IReadOnlyList<DashboardOption> Regions = [new("emea", "EMEA"), new("us", "United States"), new("apac", "APAC", "Asia and Pacific", "globe")];

    private static KlyDashboardPage PageOf(DashboardController controller, string? title = "Sales") => new() { Title = title, Dashboard = controller };

    private static DashboardFilterControl Add(DashboardController controller, DashboardFilterControl filter) => controller.AddFilter(filter);

    private static void Click(Button button) => button.SendClicked();

    private static void Complete(Entry entry) => ((IEntryController)entry).SendCompleted();

    // --- header ----------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void A_page_has_the_defaults_of_the_blazor_page_and_shows_its_title_as_the_main_heading()
    {
        var page = new KlyDashboardPage { Title = "Sales" };

        Assert.True(page.ShowRefresh);
        Assert.True(page.ShowTabs);
        Assert.True(page.ShowFilters);
        Assert.True(page.Sticky);
        Assert.True(page.Scrollable);
        Assert.Null(page.Dashboard);
        Assert.Equal("Sales", page.TitleLabel.Text);
        Assert.Equal(SemanticHeadingLevel.Level1, SemanticProperties.GetHeadingLevel(page.TitleLabel));
        Assert.True(page.HeaderBox.IsVisible);
        Assert.False(page.Toolbar.IsVisible);
        Assert.False(page.DescriptionLabel.IsVisible);
        Assert.DoesNotContain(page.RefreshButton, All<Button>(page));
    }

    [Fact]
    public void A_page_with_nothing_for_a_header_has_none()
    {
        Assert.False(new KlyDashboardPage().HeaderBox.IsVisible);
    }

    [Fact]
    public void The_description_wins_and_otherwise_the_page_says_when_the_data_was_loaded()
    {
        var controller = new DashboardController();
        var resource = controller.RegisterResource(new DashboardResource<int>("r"));
        var page = PageOf(controller);
        Assert.False(page.DescriptionLabel.IsVisible);

        resource.SetData(1);
        Assert.True(page.DescriptionLabel.IsVisible);
        Assert.Equal("Updated just now", page.DescriptionLabel.Text);

        page.Description = "All regions";
        Assert.Equal("All regions", page.DescriptionLabel.Text);
    }

    [Fact]
    public void The_refresh_button_reloads_the_controller_and_is_disabled_while_it_runs()
    {
        var gate = new TaskCompletionSource<int>();
        var loads = 0;
        var controller = new DashboardController();
        controller.RegisterResource(new DashboardResource<int>("slow", _ =>
        {
            loads++;
            return gate.Task;
        }));
        var page = PageOf(controller);
        Assert.Equal("Refresh", page.RefreshButton.Text);
        Assert.True(page.RefreshButton.IsEnabled);
        Assert.Contains(page.RefreshButton, All<Button>(page));

        Click(page.RefreshButton);

        Assert.Equal(1, loads);
        Assert.Equal("Refreshing…", page.RefreshButton.Text);
        Assert.False(page.RefreshButton.IsEnabled);

        gate.SetResult(1);
        Assert.Equal("Refresh", page.RefreshButton.Text);
        Assert.True(page.RefreshButton.IsEnabled);
    }

    [Fact]
    public void The_refresh_button_can_be_hidden_and_the_actions_come_before_it()
    {
        var controller = new DashboardController();
        var export = new KlyButton { Text = "Export" };
        var page = new KlyDashboardPage { Dashboard = controller, Actions = export };

        var buttons = All<Button>(page.HeaderBox);
        Assert.Equal(["Export", "Refresh"], buttons.Select(b => b.Text));

        page.ShowRefresh = false;
        Assert.Equal(["Export"], All<Button>(page.HeaderBox).Select(b => b.Text));
    }

    [Fact]
    public void A_failing_load_does_not_throw_out_of_the_refresh_button()
    {
        var controller = new DashboardController();
        controller.RegisterResource(new DashboardResource<int>("bad", _ => throw new InvalidOperationException("boom")));
        var page = PageOf(controller);

        Click(page.RefreshButton);

        Assert.Equal(DashboardResourceState.Error, controller.Resources[0].State);
    }

    // --- tabs ------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void The_tabs_need_two_enabled_views()
    {
        var controller = new DashboardController();
        controller.AddView("a", "Overview");
        var page = PageOf(controller);
        Assert.False(page.Toolbar.IsVisible);
        Assert.Empty(page.TabViews);

        // Adding a view does not raise Changed on the controller: the page shows it when it builds itself again.
        controller.AddView("b", "Details");
        page.Rebuild();
        Assert.True(page.Toolbar.IsVisible);
        Assert.Equal(["Overview", "Details"], page.TabViews.Select(t => t.View.Label));

        page.ShowTabs = false;
        Assert.False(page.Toolbar.IsVisible);
    }

    [Fact]
    public void A_tab_shows_its_badge_unless_it_is_empty_or_zero_and_a_tap_changes_the_view()
    {
        var controller = new DashboardController();
        controller.AddView("a", "Overview");
        controller.AddView("b", "Alerts", "3");
        controller.AddView("c", "Errors", "0");
        var page = PageOf(controller);

        var badges = All<KlyBadge>(page.Toolbar);
        Assert.Equal(["3"], badges.Select(b => b.Text));
        Assert.Equal(["Overview", "Alerts 3", "Errors"], page.TabViews.Select(t => t.Area.Description));
        Assert.All(page.TabViews, t => Assert.Equal(40, t.Area.MinimumTarget));
        Assert.Equal("Overview", controller.Views.First(v => v.Key == controller.CurrentView).Label);

        page.TabViews[1].Area.SendTap();

        Assert.Equal("b", controller.CurrentView);
        Assert.Equal(["a", "b", "c"], page.TabViews.Select(t => t.View.Key));
        Assert.Equal(KlyTheme.TextHighlighted.Light, LabelIn(page.TabViews[1].Area, "Alerts").TextColor);
        Assert.Equal(KlyTheme.TextMuted.Light, LabelIn(page.TabViews[0].Area, "Overview").TextColor);
    }

    private static Label LabelIn(View root, string text) => All<Label>(root).First(l => l.Text == text);

    [Fact]
    public void A_disabled_view_has_no_tab_and_a_badge_change_shows_at_once()
    {
        var controller = new DashboardController();
        controller.AddView("a", "Overview");
        var details = controller.AddView("b", "Details");
        var hidden = controller.AddView("c", "Hidden", enabled: false);
        var page = PageOf(controller);
        Assert.Equal(2, page.TabViews.Count);

        details.Badge = "7";
        Assert.Equal(["7"], All<KlyBadge>(page.Toolbar).Select(b => b.Text));

        hidden.Enabled = true;
        Assert.Equal(3, page.TabViews.Count);
    }

    // --- option filters --------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void An_option_filter_is_a_pill_with_its_label_and_selection()
    {
        var controller = new DashboardController();
        var region = Add(controller, new DashboardFilterControl("region", "Region", items: Regions, placeholder: "All regions"));
        var page = PageOf(controller);

        var view = page.ViewOf(region)!;
        Assert.True(page.Toolbar.IsVisible);
        Assert.Contains(view.Pill, page.FilterRow.Children);
        Assert.Equal("All regions", view.Display.Text);
        Assert.Equal("Region:", LabelIn(view.Pill, "Region:").Text);
        Assert.Equal("Region: All regions", view.PillButton!.Description);
        Assert.False(view.Clear.IsVisible);

        region.Value = "emea";
        Assert.Equal("EMEA", view.Display.Text);
        Assert.Equal("Region: EMEA", view.PillButton!.Description);
        Assert.True(view.Clear.IsVisible);
        Assert.Equal("Clear Region", view.Clear.Description);
    }

    [Fact]
    public void Tapping_the_pill_opens_its_list_under_the_filters_and_closes_the_others()
    {
        var controller = new DashboardController();
        var region = Add(controller, new DashboardFilterControl("region", "Region", items: Regions));
        var plan = Add(controller, new DashboardFilterControl("plan", "Plan", items: [new("free", "Free")]));
        var page = PageOf(controller);
        Assert.Empty(page.MenuHost.Children);

        page.ViewOf(region)!.PillButton!.SendTap();
        Assert.True(region.Open);
        Assert.Equal([page.ViewOf(region)!.Menu], page.MenuHost.Children.Cast<View>());

        page.ViewOf(plan)!.PillButton!.SendTap();
        Assert.False(region.Open);
        Assert.True(plan.Open);
        Assert.Equal([page.ViewOf(plan)!.Menu], page.MenuHost.Children.Cast<View>());

        page.ViewOf(plan)!.PillButton!.SendTap();
        Assert.Empty(page.MenuHost.Children);
    }

    [Fact]
    public void The_list_shows_the_options_with_icon_description_and_a_check_on_the_selection()
    {
        var controller = new DashboardController();
        var region = Add(controller, new DashboardFilterControl("region", "Region", items: Regions, defaultValues: ["us"]));
        var page = PageOf(controller);
        region.Open = true;
        controller.ResetFilters();

        var view = page.ViewOf(region)!;
        Assert.Equal(["EMEA", "United States", "APAC", "Asia and Pacific"], Texts(view.Menu!));
        Assert.Equal(["globe"], All<KlyIcon>(view.Menu!).Where(i => i.Name != KlyIconNames.Check).Select(i => i.Name));
        Assert.Equal(1, All<KlyIcon>(view.Menu!).Count(i => i.Name == KlyIconNames.Check));
        var rows = view.Options!.Children.Cast<DashboardTapArea>().ToList();
        Assert.Equal(["EMEA", "United States", "APAC"], rows.Select(r => r.Description));
        Assert.Equal(KlyTheme.TextHighlighted.Light, LabelIn(rows[1], "United States").TextColor);
    }

    [Fact]
    public void Picking_an_option_selects_it_and_a_single_filter_closes_its_list()
    {
        var controller = new DashboardController();
        var region = Add(controller, new DashboardFilterControl("region", "Region", items: Regions));
        var page = PageOf(controller);
        page.ViewOf(region)!.PillButton!.SendTap();

        ((DashboardTapArea)page.ViewOf(region)!.Options!.Children[1]).SendTap();

        Assert.Equal(["us"], region.Values);
        Assert.False(region.Open);
        Assert.Empty(page.MenuHost.Children);
        Assert.Equal("United States", page.ViewOf(region)!.Display.Text);
        Assert.Equal(KlyTheme.TextHighlighted.Light, page.ViewOf(region)!.Display.TextColor);
        Assert.Equal(DashboardParts.PrimaryText.Light, page.ViewOf(region)!.PillFrame.Stroke is SolidColorBrush brush ? brush.Color : null);
    }

    [Fact]
    public void A_multiple_filter_keeps_its_list_open_and_stops_at_its_maximum()
    {
        var controller = new DashboardController();
        var region = Add(controller, new DashboardFilterControl("region", "Region", items: Regions, multiple: true, max: 2));
        var page = PageOf(controller);
        page.ViewOf(region)!.PillButton!.SendTap();

        var options = () => page.ViewOf(region)!.Options!.Children.Cast<DashboardTapArea>().ToList();
        options()[0].SendTap();
        options()[1].SendTap();

        Assert.Equal(["emea", "us"], region.Values);
        Assert.True(region.Open);
        Assert.Equal("EMEA, United States", page.ViewOf(region)!.Display.Text);
        Assert.Equal([true, true, false], options().Select(o => o.IsEnabled));
        Assert.Equal(0.4, options()[2].Opacity);

        options()[0].SendTap();
        Assert.Equal(["us"], region.Values);
        Assert.All(options(), o => Assert.True(o.IsEnabled));
    }

    [Fact]
    public void A_disabled_option_cannot_be_picked()
    {
        var controller = new DashboardController();
        var region = Add(controller, new DashboardFilterControl("region", "Region", items: [new("a", "A"), new("b", "B", Disabled: true)]));
        var page = PageOf(controller);
        region.Open = true;
        controller.ResetFilters();

        Assert.Equal([true, false], page.ViewOf(region)!.Options!.Children.Cast<DashboardTapArea>().Select(o => o.IsEnabled));
    }

    [Fact]
    public void A_searchable_filter_narrows_its_list_while_typing_and_keeps_the_box()
    {
        var controller = new DashboardController();
        var region = Add(controller, new DashboardFilterControl("region", "Region", items: Regions, searchable: true));
        var page = PageOf(controller);
        region.Open = true;
        controller.ResetFilters();
        var view = page.ViewOf(region)!;
        var search = view.Search!;
        Assert.Equal("Search…", search.Placeholder);
        Assert.Equal("Search Region", SemanticProperties.GetDescription(search));

        search.Text = "ap";

        Assert.Equal("ap", region.Search);
        Assert.Equal(["APAC"], view.Options!.Children.Cast<DashboardTapArea>().Select(o => o.Description));
        Assert.Same(search, page.ViewOf(region)!.Search);
    }

    [Fact]
    public void A_filter_without_search_has_no_box()
    {
        var controller = new DashboardController();
        var region = Add(controller, new DashboardFilterControl("region", "Region", items: Regions));
        var page = PageOf(controller);

        Assert.Null(page.ViewOf(region)!.Search);
    }

    [Fact]
    public void Presets_select_a_set_of_values_in_one_tap_and_an_open_filter_without_options_shows_them()
    {
        var controller = new DashboardController();
        var region = Add(controller, new DashboardFilterControl("region", "Region", items: Regions, multiple: true, presets: [new("Europe", ["emea", "us"], "globe")]));
        var page = PageOf(controller);
        page.ViewOf(region)!.PillButton!.SendTap();

        var preset = All<DashboardTapArea>(page.ViewOf(region)!.Menu!).First(a => a.Description == "Europe");
        Assert.Contains(All<KlyIcon>(preset), i => i.Name == "globe");
        preset.SendTap();

        Assert.Equal(["emea", "us"], region.Values);
    }

    [Fact]
    public void An_open_filter_with_no_options_and_no_presets_shows_no_list()
    {
        var controller = new DashboardController();
        var remote = Add(controller, new DashboardFilterControl("remote", "Remote", DashboardFilterKind.Remote));
        var page = PageOf(controller);

        page.ViewOf(remote)!.PillButton!.SendTap();

        Assert.True(remote.Open);
        Assert.Empty(page.MenuHost.Children);

        remote.Items = Regions;
        Assert.Single(page.MenuHost.Children);
    }

    [Fact]
    public void The_clear_button_resets_one_filter_and_the_reset_button_every_filter()
    {
        var controller = new DashboardController();
        var region = Add(controller, new DashboardFilterControl("region", "Region", items: Regions));
        var plan = Add(controller, new DashboardFilterControl("plan", "Plan", items: [new("free", "Free")]));
        var page = PageOf(controller);
        region.Value = "emea";
        plan.Value = "free";
        Assert.Contains(page.ResetButton, page.FilterRow.Children);
        Assert.Equal("Reset", page.ResetButton.Text);

        page.ViewOf(region)!.Clear.SendTap();
        Assert.False(region.Changed);
        Assert.True(plan.Changed);

        Click(page.ResetButton);
        Assert.False(controller.Filtered);
        Assert.DoesNotContain(page.ResetButton, page.FilterRow.Children);
    }

    [Fact]
    public void Headless_disabled_and_custom_filters_without_a_template_are_left_out()
    {
        var controller = new DashboardController();
        var shown = Add(controller, new DashboardFilterControl("a", "A", items: Regions));
        var headless = Add(controller, new DashboardFilterControl("b", "B", items: Regions, headless: true));
        var disabled = Add(controller, new DashboardFilterControl("c", "C", items: Regions));
        var custom = Add(controller, new DashboardFilterControl("d", "D", DashboardFilterKind.Custom));
        disabled.Enabled = false;
        var page = PageOf(controller);

        Assert.Equal([shown], page.VisibleControls);
        Assert.Null(page.ViewOf(headless));
        Assert.Null(page.ViewOf(custom));

        page.CustomFilter = f => new Label { Text = "Custom " + f.Label };
        Assert.Equal([shown, custom], page.VisibleControls);
        Assert.Equal(["Custom D"], Texts(page.ViewOf(custom)!.Pill));
        Assert.False(page.ViewOf(custom)!.Clear.IsVisible);
        Assert.DoesNotContain(page.ViewOf(custom)!.Clear, All<DashboardTapArea>(page.ViewOf(custom)!.Pill));

        page.ShowFilters = false;
        Assert.False(page.Toolbar.IsVisible);
    }

    [Fact]
    public void Enabling_a_filter_adds_its_control_and_changing_the_template_builds_the_custom_ones_again()
    {
        var controller = new DashboardController();
        var custom = Add(controller, new DashboardFilterControl("d", "D", DashboardFilterKind.Custom));
        var late = Add(controller, new DashboardFilterControl("e", "E", items: Regions));
        late.Enabled = false;
        var page = new KlyDashboardPage { Dashboard = controller, CustomFilter = f => new Label { Text = "One" } };
        Assert.Equal(["One"], Texts(page.ViewOf(custom)!.Pill));

        late.Enabled = true;
        Assert.NotNull(page.ViewOf(late));

        page.CustomFilter = f => new Label { Text = "Two" };
        Assert.Equal(["Two"], Texts(page.ViewOf(custom)!.Pill));
    }

    // --- text, number and date filters -----------------------------------------------------------------------------------------------------

    [Fact]
    public void A_text_filter_applies_when_the_box_is_confirmed_and_an_empty_box_clears_it()
    {
        var controller = new DashboardController();
        var query = Add(controller, new DashboardFilterControl("q", "Customer", DashboardFilterKind.String, placeholder: "Any customer"));
        var page = PageOf(controller);
        var entry = page.ViewOf(query)!.Entry!;
        Assert.Equal("Any customer", entry.Placeholder);
        Assert.Equal("Customer", SemanticProperties.GetDescription(entry));

        entry.Text = "ada";
        Assert.Null(query.Value);
        Complete(entry);
        Assert.Equal("ada", query.Value);
        Assert.True(page.ViewOf(query)!.Clear.IsVisible);

        entry.Text = "";
        Complete(entry);
        Assert.Null(query.Value);
        Assert.False(query.Changed);
    }

    [Fact]
    public void A_text_box_follows_the_filter_when_code_changes_it()
    {
        var controller = new DashboardController();
        var query = Add(controller, new DashboardFilterControl("q", "Customer", DashboardFilterKind.String));
        var page = PageOf(controller);

        query.Value = "grace";

        Assert.Equal("grace", page.ViewOf(query)!.Entry!.Text);
        page.ViewOf(query)!.Clear.SendTap();
        Assert.Equal(string.Empty, page.ViewOf(query)!.Entry!.Text);
    }

    [Fact]
    public void A_number_filter_stores_the_number_invariantly_and_refuses_text()
    {
        Invariant();
        var controller = new DashboardController();
        var amount = Add(controller, new DashboardFilterControl("n", "Amount", DashboardFilterKind.Number));
        var page = PageOf(controller);
        var entry = page.ViewOf(amount)!.Entry!;
        Assert.Equal(Keyboard.Numeric, entry.Keyboard);
        Assert.Equal(80, entry.WidthRequest);

        entry.Text = "1234.5";
        Complete(entry);
        Assert.Equal("1234.5", amount.Value);

        entry.Text = "abc";
        Complete(entry);
        Assert.Equal("1234.5", amount.Value);
        Assert.Equal("1234.5", entry.Text);

        entry.Text = "NaN";
        Complete(entry);
        Assert.Equal("1234.5", amount.Value);
    }

    [Fact]
    public void A_date_filter_stores_an_iso_date_and_shows_it_in_its_picker()
    {
        var controller = new DashboardController();
        var day = Add(controller, new DashboardFilterControl("d", "Day", DashboardFilterKind.Date));
        var page = PageOf(controller);
        var picker = page.ViewOf(day)!.Pickers.Start!;
        Assert.Null(picker.Date);

        picker.Date = new DateTime(2026, 3, 9);
        Assert.Equal("2026-03-09", day.Value);

        day.Value = "2025-12-31";
        Assert.Equal(new DateTime(2025, 12, 31), picker.Date);

        page.ViewOf(day)!.Clear.SendTap();
        Assert.Null(picker.Date);
    }

    [Fact]
    public void A_date_range_writes_start_then_end_and_keeps_the_other_bound()
    {
        var controller = new DashboardController();
        var period = Add(controller, new DashboardFilterControl("p", "Period", DashboardFilterKind.DateRange));
        var page = PageOf(controller);
        var (start, end) = page.ViewOf(period)!.Pickers;
        Assert.Equal("Period from", SemanticProperties.GetDescription(start!));
        Assert.Equal("Period to", SemanticProperties.GetDescription(end!));

        start!.Date = new DateTime(2026, 1, 1);
        Assert.Equal(["2026-01-01", ""], period.Values);

        end!.Date = new DateTime(2026, 2, 1);
        Assert.Equal(["2026-01-01", "2026-02-01"], period.Values);
        Assert.Equal(new DateTime(2026, 2, 1), start.MaximumDate);
        Assert.Equal(new DateTime(2026, 1, 1), end.MinimumDate);

        period.SetValues(["", "2026-05-05"]);
        Assert.Null(start.Date);
        Assert.Equal(new DateTime(2026, 5, 5), end.Date);
    }

    [Fact]
    public void A_range_set_from_code_moves_both_pickers_even_when_the_new_dates_lie_outside_the_old_limits()
    {
        var controller = new DashboardController();
        var period = Add(controller, new DashboardFilterControl("p", "Period", DashboardFilterKind.DateRange));
        var page = PageOf(controller);
        var (start, end) = page.ViewOf(period)!.Pickers;

        period.SetValues(["2026-01-01", "2026-02-01"]);
        period.SetValues(["2026-06-01", "2026-07-01"]);

        Assert.Equal(new DateTime(2026, 6, 1), start!.Date);
        Assert.Equal(new DateTime(2026, 7, 1), end!.Date);

        period.SetValues(["2024-01-01", "2024-02-01"]);
        Assert.Equal(new DateTime(2024, 1, 1), start.Date);
        Assert.Equal(new DateTime(2024, 2, 1), end.Date);
        Assert.Equal(["2024-01-01", "2024-02-01"], period.Values);
    }

    [Fact]
    public void Presets_sit_beside_a_text_date_or_range_box()
    {
        var controller = new DashboardController();
        var period = Add(controller, new DashboardFilterControl("p", "Period", DashboardFilterKind.DateRange, presets: [new("Q1", ["2026-01-01", "2026-03-31"])]));
        var page = PageOf(controller);

        var chip = Assert.Single(All<DashboardTapArea>(page.ViewOf(period)!.Presets));
        chip.SendTap();

        Assert.Equal(["2026-01-01", "2026-03-31"], period.Values);
        Assert.Equal(new DateTime(2026, 3, 31), page.ViewOf(period)!.Pickers.End!.Date);
    }

    // --- following the controller ----------------------------------------------------------------------------------------------------------

    [Fact]
    public void The_blocks_of_the_body_follow_the_controller_wherever_they_are_nested()
    {
        var controller = new DashboardController();
        var resource = controller.RegisterResource(new DashboardResource<int>("r"));
        var direct = new KlyDashboardCard { Source = resource, Body = new Label { Text = "Direct" } };
        var inGrid = new KlyDashboardStat { Label = "Nested", Source = resource, Value = 1 };
        var grid = new KlyDashboardGrid();
        grid.Children.Add(inGrid);
        var page = new KlyDashboardPage { Dashboard = controller, Body = new VerticalStackLayout { Children = { direct, new Border { Content = grid }, new ScrollView { Content = new ContentView { Content = new KlyDashboardCard { Source = resource } } } } } };
        Assert.Equal(DashboardBlockPhase.Idle, direct.Phase);
        Assert.NotEmpty(All<KlySkeleton>(direct));

        resource.SetData(5);

        Assert.Empty(All<KlySkeleton>(direct));
        Assert.Contains("Direct", Texts(direct));
        Assert.DoesNotContain(All<KlySkeleton>(inGrid), s => s.IsVisible && s.HeightRequest == 32);
        Assert.Equal(3, page.CardsCount());
    }

    [Fact]
    public void Changing_the_controller_unsubscribes_from_the_old_one()
    {
        var first = new DashboardController();
        var second = new DashboardController();
        first.AddView("a", "One");
        first.AddView("b", "Two");
        var page = PageOf(first);
        Assert.Equal(2, page.TabViews.Count);

        page.Dashboard = second;
        Assert.Empty(page.TabViews);
        Assert.False(page.Toolbar.IsVisible);

        first.AddView("c", "Three");
        first.AddFilter(new DashboardFilterControl("x", "X", items: Regions));
        first.CurrentView = "b";
        Assert.Empty(page.TabViews);

        second.AddView("s1", "S1");
        second.AddView("s2", "S2");
        page.Rebuild();
        Assert.Equal(["S1", "S2"], page.TabViews.Select(t => t.View.Label));
    }

    // --- layout ----------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void A_sticky_page_keeps_the_header_and_toolbar_outside_the_scrolling_body()
    {
        var body = new Label { Text = "Body" };
        var page = new KlyDashboardPage { Body = body };

        Assert.Equal([page.HeaderBox, page.Toolbar, page.Scroll], page.Root.Children.Cast<View>());
        Assert.Same(page.BodyHost, page.Scroll.Content);
        Assert.Same(body, page.BodyHost.Content);
        Assert.Equal([0, 1, 2], page.Root.Children.Cast<View>().Select(Grid.GetRow));
    }

    [Fact]
    public void A_page_that_is_not_sticky_scrolls_the_header_and_toolbar_with_the_body()
    {
        var page = new KlyDashboardPage { Sticky = false };

        Assert.Equal([page.Scroll], page.Root.Children.Cast<View>());
        var flow = Assert.IsType<VerticalStackLayout>(page.Scroll.Content);
        Assert.Equal([page.HeaderBox, page.Toolbar, page.BodyHost], flow.Children.Cast<View>());

        page.Sticky = true;
        Assert.Equal([page.HeaderBox, page.Toolbar, page.Scroll], page.Root.Children.Cast<View>());
        Assert.Same(page.BodyHost, page.Scroll.Content);
    }

    [Fact]
    public void A_page_that_does_not_scroll_has_no_scroll_view_for_the_app_to_nest_it_in_its_own()
    {
        var page = new KlyDashboardPage { Scrollable = false };

        Assert.Equal([page.HeaderBox, page.Toolbar, page.BodyHost], page.Root.Children.Cast<View>());
        Assert.Null(page.Scroll.Content);

        page.Sticky = false;
        Assert.Equal([page.HeaderBox, page.Toolbar, page.BodyHost], page.Root.Children.Cast<View>());
    }

    [Fact]
    public void The_body_can_be_replaced()
    {
        var first = new Label { Text = "First" };
        var second = new Label { Text = "Second" };
        var page = new KlyDashboardPage { Body = first };

        page.Body = second;

        Assert.Same(second, page.BodyHost.Content);
        Assert.Null(first.Parent);
    }

    [Theory]
    [InlineData(400, 16)]
    [InlineData(1023, 16)]
    [InlineData(1024, 28)]
    [InlineData(-1, 28)]
    public void The_page_pads_its_sides_by_the_width_16_and_28_from_1024_up(double width, double padding)
    {
        var page = new KlyDashboardPage { Title = "T" };

        page.Resize(width);

        Assert.Equal(padding, page.SidePadding);
        Assert.Equal(new Thickness(padding), page.BodyHost.Padding);
        Assert.Equal(new Thickness(padding, 16), page.Header.Padding);
    }

    [Fact]
    public void The_page_follows_the_width_of_its_own_page_when_it_is_laid_out()
    {
        var page = new KlyDashboardPage { Title = "T" };
        var host = new ContentPage { Content = page };

        ((IView)host).Arrange(new Rect(0, 0, 500, 800));
        page.Resize(DashboardBreakpoints.ViewportWidth(page));
        Assert.Equal(16, page.SidePadding);

        ((IView)host).Arrange(new Rect(0, 0, 1200, 800));
        page.Resize(DashboardBreakpoints.ViewportWidth(page));
        Assert.Equal(28, page.SidePadding);
    }

    // --- end to end ------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task A_page_a_grid_and_a_stat_work_together_from_the_first_load_to_a_refresh()
    {
        Invariant();
        var total = 48_500.0;
        var controller = new DashboardController("sales");
        var revenue = controller.RegisterResource(new DashboardResource<double>("revenue", _ => Task.FromResult(total)));
        controller.AddFilter(new DashboardFilterControl("region", "Region", items: [new("emea", "EMEA"), new("us", "US")]));
        var stat = new KlyDashboardStat { Label = "Revenue", Source = revenue, Format = DashboardFormatPreset.Compact, Span = 4 };
        controller.Changed += () => stat.Value = revenue.Data;
        var grid = new KlyDashboardGrid { Menu = true, Children = { stat } };
        var page = new KlyDashboardPage { Title = "Sales", Dashboard = controller, Body = grid };

        Assert.Equal(DashboardBlockPhase.Idle, stat.Phase);
        Assert.NotEmpty(All<KlySkeleton>(stat));

        await controller.LoadAsync();

        Assert.Equal(DashboardBlockPhase.Content, stat.Phase);
        Assert.Contains("48.5K", Texts(stat));
        Assert.Equal("Updated just now", page.DescriptionLabel.Text);

        total = 51_000;
        Click(page.RefreshButton);
        await Task.Yield();
        Assert.Contains("51K", Texts(stat));
    }
}
