using System.Runtime.CompilerServices;
using static Klyroo.Dashboard.Maui.Tests.TestHelpers;

namespace Klyroo.Dashboard.Maui.Tests;

// What a refresh must leave alone: the views of the app, the content that is on screen, an open dialog. A native view that is taken out of its parent and put back loses its focus and closes its menus.
public class DashboardRefreshTests
{
    private sealed class Counter
    {
        public int Count;
    }

    private static Counter Watch(View view)
    {
        var counter = new Counter();
        view.ParentChanged += (_, _) => counter.Count++;
        return counter;
    }

    // --- views of the app ------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void A_refresh_does_not_take_the_views_of_the_app_out_of_the_card()
    {
        var controller = new DashboardController();
        var resource = controller.RegisterResource(new DashboardResource<int>("r"));
        var body = new Entry();
        var headerRight = new Entry();
        var footer = new Entry();
        var card = new KlyDashboardCard { Source = resource, Body = body, HeaderRight = headerRight, Footer = footer, Menu = true, Tabulate = () => new DashboardDataTable([], []), Expandable = false };
        _ = new KlyDashboardPage { Dashboard = controller, Body = card };
        resource.SetData(1);
        var watched = new[] { Watch(body), Watch(headerRight), Watch(footer) };

        resource.SetData(2);
        controller.ResetFilters();
        card.Refresh();
        card.Refresh();

        Assert.All(watched, w => Assert.Equal(0, w.Count));
        Assert.Same(card.Tools, headerRight.Parent);
        Assert.Same(body, card.PhaseHost.Content);
    }

    [Fact]
    public void A_refresh_does_not_take_the_actions_of_the_page_out_of_the_header()
    {
        var controller = new DashboardController();
        var resource = controller.RegisterResource(new DashboardResource<int>("r"));
        var actions = new KlyButton { Text = "Export" };
        var page = new KlyDashboardPage { Dashboard = controller, Actions = actions };
        var watched = Watch(actions);

        resource.SetData(1);
        resource.SetData(2);
        controller.ResetFilters();

        Assert.Equal(0, watched.Count);
        Assert.Contains(actions, All<Button>(page.HeaderBox));
    }

    [Fact]
    public void An_open_dialog_in_the_header_of_a_card_stays_open_through_a_refresh()
    {
        var controller = new DashboardController();
        var resource = controller.RegisterResource(new DashboardResource<int>("r"));
        var menu = new KlyModal { Title = "Settings", Body = new Label { Text = "x" } };
        var card = new KlyDashboardCard { Source = resource, HeaderRight = menu };
        var host = new KlyOverlayHost { Content = new KlyDashboardPage { Dashboard = controller, Body = card } };
        resource.SetData(1);
        menu.IsOpen = true;
        Assert.Single(host.Layers);

        resource.SetData(2);
        resource.SetData(3);

        Assert.True(menu.IsOpen);
        Assert.Single(host.Layers);
    }

    [Fact]
    public void An_open_filter_list_and_its_search_box_stay_where_they_are_when_the_controller_changes()
    {
        var controller = new DashboardController();
        var resource = controller.RegisterResource(new DashboardResource<int>("r"));
        var region = controller.AddFilter(new DashboardFilterControl("region", "Region", items: [new("a", "A"), new("b", "B")], multiple: true, searchable: true));
        var page = new KlyDashboardPage { Dashboard = controller };
        page.ViewOf(region)!.PillButton!.SendTap();
        var menu = page.ViewOf(region)!.Menu!;
        var pill = page.ViewOf(region)!.Pill;
        var menuWatch = Watch(menu);
        var pillWatch = Watch(pill);
        var searchWatch = Watch(page.ViewOf(region)!.Search!);
        Assert.Contains(menu, page.MenuHost.Children);

        ((DashboardTapArea)page.ViewOf(region)!.Options!.Children[0]).SendTap();
        resource.SetData(1);
        resource.SetData(2);

        Assert.Equal(0, menuWatch.Count);
        Assert.Equal(0, pillWatch.Count);
        Assert.Equal(0, searchWatch.Count);
        Assert.Contains(menu, page.MenuHost.Children);
        Assert.Equal(["a"], region.Values);
    }

    // --- the content on screen -------------------------------------------------------------------------------------------------------------

    [Fact]
    public void A_refresh_keeps_the_content_that_is_on_screen_until_something_it_shows_changes()
    {
        var resource = new DashboardResource<int>("r");
        resource.SetData(1);
        var table = new KlyDashboardTable<string> { Source = resource, Columns = [DashboardTableColumn.For<string, string>("n", "Name", r => r)], Rows = ["a", "b"] };
        var shown = table.PhaseHost.Content;

        table.Refresh();
        resource.SetData(2);
        table.Refresh();
        Assert.Same(shown, table.PhaseHost.Content);

        table.Rows = ["a", "b", "c"];
        Assert.NotSame(shown, table.PhaseHost.Content);

        shown = table.PhaseHost.Content;
        resource.SetError("down");
        table.Refresh();
        Assert.NotSame(shown, table.PhaseHost.Content);

        shown = table.PhaseHost.Content;
        resource.SetData(3);
        table.Refresh();
        Assert.NotSame(shown, table.PhaseHost.Content);
    }

    [Fact]
    public void A_new_error_message_replaces_the_one_on_screen()
    {
        var resource = new DashboardResource<int>("r");
        resource.SetError("first");
        var card = new KlyDashboardCard { Source = resource };
        Assert.Contains("first", Texts(card));

        resource.SetError("second");
        card.Refresh();

        Assert.Contains("second", Texts(card));
        Assert.DoesNotContain("first", Texts(card));
    }

    [Fact]
    public void A_property_the_content_shows_builds_it_again_but_one_it_does_not_show_does_not()
    {
        var stat = new KlyDashboardStat { Label = "Revenue", Value = 1 };
        var shown = stat.PhaseHost.Content;

        stat.Span = 8;
        Assert.Same(shown, stat.PhaseHost.Content);

        stat.Value = 2;
        Assert.NotSame(shown, stat.PhaseHost.Content);
    }

    [Fact]
    public void The_footer_sits_at_the_bottom_of_the_card_below_a_stretching_row()
    {
        var card = new KlyDashboardCard { Footer = new Label { Text = "Source" } };

        var stack = Assert.IsType<Grid>(((Grid)card.Content!).Children[0]);
        var content = Assert.IsType<Grid>(stack.Children.OfType<Grid>().Last());
        Assert.Equal([GridLength.Auto, GridLength.Star, GridLength.Auto], content.RowDefinitions.Select(r => r.Height));
        Assert.Equal(0, Grid.GetRow(card.PhaseHost));
        Assert.Equal(2, Grid.GetRow(card.FooterBox));
        Assert.Equal(GridLength.Star, stack.RowDefinitions.Last().Height);
    }

    // --- the dialog ------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void The_expanded_dialog_stays_open_when_the_source_fails_and_shows_the_error_in_it()
    {
        var resource = new DashboardResource<int>("r");
        resource.SetData(1);
        var card = new KlyDashboardCard { Title = "Revenue", Source = resource, Menu = true, Body = new Label { Text = "Chart" } };
        _ = new KlyOverlayHost { Content = card };
        card.Refresh();
        card.MenuButtons.Expand.SendClicked();
        Assert.True(card.Modal.IsOpen);

        resource.SetError("Boom");
        card.Refresh();

        Assert.True(card.Expanded);
        Assert.True(card.Modal.IsOpen);
        Assert.Contains("Boom", Texts(card.Modal.Body!));
        Assert.DoesNotContain(card.MenuButtons.Expand, card.Tools.Children);

        resource.SetData(2);
        card.Refresh();

        Assert.True(card.Expanded);
        Assert.Equal("Chart", Texts(card.Modal.Body!).Single());
        Assert.Contains(card.MenuButtons.Expand, card.Tools.Children);
    }

    [Fact]
    public void The_scroll_position_of_the_expanded_content_survives_a_refresh()
    {
        var resource = new DashboardResource<int>("r");
        resource.SetData(1);
        var card = new KlyDashboardCard { Source = resource, Menu = true, Body = new Label { Text = "Chart" } };
        _ = new KlyOverlayHost { Content = card };
        card.Refresh();
        card.MenuButtons.Expand.SendClicked();
        var inDialog = card.Modal.Body;

        resource.SetData(2);
        card.Refresh();

        Assert.Same(inDialog, card.Modal.Body);
    }

    [Fact]
    public void The_holder_of_the_dialog_takes_no_touch_and_no_room()
    {
        var card = new KlyDashboardCard();

        Assert.True(card.Modal.InputTransparent);
        Assert.Equal(LayoutOptions.Start, card.Modal.HorizontalOptions);
        Assert.Equal(LayoutOptions.Start, card.Modal.VerticalOptions);
    }

    // --- sources ---------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task A_card_outside_a_page_shows_the_content_when_the_deferred_source_it_activated_has_loaded()
    {
        var gate = new TaskCompletionSource<int>();
        var resource = new DashboardResource<int>("later", _ => gate.Task, DashboardQueryStage.Deferred);
        var card = new KlyDashboardCard { Source = resource, Body = new Label { Text = "Done" } };
        Assert.NotEmpty(All<KlySkeleton>(card));

        gate.SetResult(1);
        await Task.Yield();

        Assert.Equal(DashboardBlockPhase.Content, card.Phase);
        Assert.Equal(["Done"], Texts(card));
        Assert.Empty(All<KlySkeleton>(card));
    }

    [Fact]
    public void A_source_that_stays_disabled_does_not_send_the_card_round_in_circles()
    {
        var calls = 0;
        var resource = new DashboardResource<int>("gated", enabled: () =>
        {
            calls++;
            return false;
        });
        var card = new KlyDashboardCard { Source = resource };

        card.Refresh();
        card.Refresh();

        Assert.False(card.IsVisible);
        Assert.InRange(calls, 1, 20);
    }

    [Fact]
    public void A_card_the_app_hid_stays_hidden_when_its_source_comes_back()
    {
        var enabled = false;
        var resource = new DashboardResource<int>("gated", _ => Task.FromResult(1), enabled: () => enabled);
        var card = new KlyDashboardCard { IsVisible = false, Source = resource };

        enabled = true;
        card.Refresh();

        Assert.False(card.HiddenBySource);
        Assert.False(card.IsVisible);
    }

    // --- grid ------------------------------------------------------------------------------------------------------------------------------

    private static (int Row, int Column, int Span) At(View view) => (Grid.GetRow(view), Grid.GetColumn(view), Grid.GetColumnSpan(view));

    [Fact]
    public void A_block_that_hides_or_shows_itself_leaves_no_gap_and_a_new_span_places_it_again()
    {
        var grid = new KlyDashboardGrid();
        var a = new KlyDashboardCard { Span = 6 };
        var b = new KlyDashboardCard { Span = 6 };
        var c = new KlyDashboardCard { Span = 6 };
        foreach (var block in new[] { a, b, c }) grid.Children.Add(block);
        Assert.Equal((1, 0, 6), At(c));

        b.IsVisible = false;
        Assert.Equal((0, 6, 6), At(c));

        b.IsVisible = true;
        Assert.Equal((1, 0, 6), At(c));

        a.Span = 12;
        Assert.Equal((0, 0, 12), At(a));
        Assert.Equal((1, 0, 6), At(b));
        Assert.Equal((1, 6, 6), At(c));
    }

    [Fact]
    public void A_block_taken_out_of_the_grid_stops_being_followed()
    {
        var grid = new KlyDashboardGrid();
        var a = new KlyDashboardCard { Span = 6 };
        var b = new KlyDashboardCard { Span = 6 };
        grid.Children.Add(a);
        grid.Children.Add(b);

        grid.Children.Remove(a);
        a.IsVisible = false;

        Assert.Equal((0, 0, 6), At(b));
    }

    // --- tables ----------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void The_first_column_of_a_table_takes_the_room_the_others_leave_so_that_a_narrow_table_fills_the_card()
    {
        var table = new KlyDashboardTable<string> { Columns = [DashboardTableColumn.For<string, string>("n", "Name", r => r), DashboardTableColumn.For<string, double>("v", "Value", r => r.Length)], Rows = ["a"] };

        var grid = Assert.IsType<Grid>(Assert.IsType<ScrollView>(table.PhaseHost.Content).Content);
        Assert.Equal([GridLength.Star, GridLength.Auto], grid.ColumnDefinitions.Select(c => c.Width));

        var card = new KlyDashboardCard { Menu = true, Expandable = false, Tabulate = () => new DashboardDataTable([new("a", "A"), new("b", "B"), new("c", "C")], []) };
        card.MenuButtons.Table.SendClicked();
        var view = Assert.IsType<Grid>(Assert.IsType<ScrollView>(card.PhaseHost.Content).Content);
        Assert.Equal([GridLength.Star, GridLength.Auto, GridLength.Auto], view.ColumnDefinitions.Select(c => c.Width));
    }

    // --- the page and the controller -------------------------------------------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (DashboardController Controller, WeakReference Page) PageThatIsNeverShown()
    {
        var controller = new DashboardController();
        controller.AddFilter(new DashboardFilterControl("a", "A", items: [new("x", "X")]));
        var page = new KlyDashboardPage { Dashboard = controller, Title = "T" };
        return (controller, new WeakReference(page));
    }

    [Fact]
    public void A_page_that_was_built_and_never_shown_is_not_kept_alive_by_the_controller()
    {
        var (controller, page) = PageThatIsNeverShown();

        for (var i = 0; i < 3 && page.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.False(page.IsAlive);
        // The relay of the dead page takes itself off the controller on the next change, and the controller goes on.
        controller.ResetFilters();
        controller.ResetFilters();
    }

    [Fact]
    public void A_page_that_is_alive_follows_the_controller_through_the_weak_relay()
    {
        var controller = new DashboardController();
        var page = new KlyDashboardPage { Dashboard = controller };
        controller.AddView("a", "One");
        controller.AddView("b", "Two");

        controller.CurrentView = "b";

        Assert.Equal(2, page.TabViews.Count);
        GC.KeepAlive(page);
    }
}
