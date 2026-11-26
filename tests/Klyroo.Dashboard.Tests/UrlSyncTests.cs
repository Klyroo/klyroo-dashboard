using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Klyroo.Dashboard.Tests;

// KlyDashboardPage.SyncUrl: the URL is the starting state of the filters and follows them without filling the history.
public sealed class UrlSyncTests : BunitContext
{
    private readonly BunitNavigationManager _nav;

    public UrlSyncTests()
    {
        Services.AddKlyrooDashboard();
        _nav = Services.GetRequiredService<BunitNavigationManager>();
    }

    private static DashboardController Dashboard(out DashboardFilterControl region, out DashboardFilterControl search)
    {
        var dashboard = new DashboardController();
        region = dashboard.AddFilter(new DashboardFilterControl("region", "Region", items: [new("EMEA", "EMEA"), new("APAC", "APAC")]));
        search = dashboard.AddFilter(new DashboardFilterControl("q", "Search", DashboardFilterKind.String));
        return dashboard;
    }

    private IRenderedComponent<KlyDashboardPage> Page(DashboardController dashboard, bool sync = true, string? prefix = null) =>
        Render<KlyDashboardPage>(p => p
            .Add(x => x.Dashboard, dashboard)
            .Add(x => x.SyncUrl, sync)
            .Add(x => x.UrlPrefix, prefix));

    [Fact]
    public void The_query_of_the_url_is_the_starting_state_of_the_filters()
    {
        _nav.NavigateTo("/sales?region=APAC&q=acme%20corp&page=2");
        var dashboard = Dashboard(out var region, out var search);

        var cut = Page(dashboard);

        Assert.Equal(["APAC"], region.Values);
        Assert.Equal("acme corp", search.Value);
        // The toolbar shows it too.
        Assert.Equal("APAC", cut.Find("[data-key=region] [data-slot=dashboard-filter-display]").TextContent.Trim());
    }

    [Fact]
    public void In_a_static_render_the_url_is_applied_but_never_written()
    {
        _nav.NavigateTo("/sales?region=APAC");
        var dashboard = Dashboard(out var region, out _);
        var writes = 0;
        _nav.LocationChanged += (_, _) => writes++;

        Page(dashboard);
        // A change after the render, as a resource finishing would cause.
        region.Value = "EMEA";

        Assert.Equal(["EMEA"], region.Values);
        Assert.Equal(0, writes);
        Assert.Equal("http://localhost/sales?region=APAC", _nav.Uri);
    }

    [Fact]
    public void A_filter_change_replaces_the_url_and_keeps_the_other_parameters_and_the_fragment()
    {
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        _nav.NavigateTo("/sales?page=2#top");
        var dashboard = Dashboard(out var region, out _);
        var cut = Page(dashboard);
        var entries = _nav.History.Count;

        region.Value = "EMEA";

        cut.WaitForAssertion(() => Assert.Equal("http://localhost/sales?page=2&region=EMEA#top", _nav.Uri));
        var last = _nav.History.First();
        Assert.True(last.Options.ReplaceHistoryEntry);
        Assert.False(last.Options.ForceLoad);
        // Replaced, not added: the page still has one history entry.
        Assert.Equal(entries, _nav.History.Count);
    }

    [Fact]
    public void Clearing_a_filter_removes_its_key_and_a_url_that_already_holds_the_state_is_left_alone()
    {
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        _nav.NavigateTo("/sales?region=EMEA");
        var dashboard = Dashboard(out var region, out _);
        Page(dashboard);
        var writes = 0;
        _nav.LocationChanged += (_, _) => writes++;

        // The same value again: the URL already says it.
        region.Value = "EMEA";
        Assert.Equal(0, writes);

        region.Reset();
        Assert.Equal(1, writes);
        Assert.Equal("http://localhost/sales", _nav.Uri);
    }

    [Fact]
    public void A_change_is_written_once_however_many_times_the_controller_reports_it()
    {
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        _nav.NavigateTo("/sales");
        var dashboard = Dashboard(out var region, out _);
        Page(dashboard);
        var writes = new List<string>();
        _nav.LocationChanged += (_, e) => writes.Add(e.Location);

        region.Value = "APAC";
        dashboard.ResetFilters();
        region.Value = "APAC";

        // APAC, then back to none, then APAC: each a real change of the URL, nothing more.
        Assert.Equal(["http://localhost/sales?region=APAC", "http://localhost/sales", "http://localhost/sales?region=APAC"], writes);
    }

    [Fact]
    public void A_navigation_to_the_same_page_with_another_query_sets_the_filters_but_our_own_writes_do_not_loop()
    {
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        _nav.NavigateTo("/sales");
        var dashboard = Dashboard(out var region, out _);
        Page(dashboard);

        _nav.NavigateTo("/sales?region=APAC");
        Assert.Equal(["APAC"], region.Values);

        region.Value = "EMEA";
        Assert.Equal(["EMEA"], region.Values);
        Assert.Equal("http://localhost/sales?region=EMEA", _nav.Uri);
    }

    [Fact]
    public void Leaving_the_page_does_not_reset_the_filters()
    {
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        _nav.NavigateTo("/sales?region=APAC");
        var dashboard = Dashboard(out var region, out _);
        Page(dashboard);

        _nav.NavigateTo("/other");

        Assert.Equal(["APAC"], region.Values);
    }

    [Fact]
    public void A_prefix_keeps_two_dashboards_apart()
    {
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        _nav.NavigateTo("/sales?a.region=APAC&b.region=EMEA");
        var first = Dashboard(out var firstRegion, out _);
        var second = Dashboard(out var secondRegion, out _);

        Page(first, prefix: "a.");
        Page(second, prefix: "b.");

        Assert.Equal(["APAC"], firstRegion.Values);
        Assert.Equal(["EMEA"], secondRegion.Values);

        firstRegion.Value = "EMEA";
        Assert.Contains("a.region=EMEA", _nav.Uri);
        Assert.Contains("b.region=EMEA", _nav.Uri);
    }

    [Fact]
    public void Without_SyncUrl_the_url_is_neither_read_nor_written()
    {
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        _nav.NavigateTo("/sales?region=APAC");
        var dashboard = Dashboard(out var region, out _);
        var writes = 0;
        _nav.LocationChanged += (_, _) => writes++;

        Page(dashboard, sync: false);
        region.Value = "EMEA";

        Assert.Equal(["EMEA"], region.Values);
        Assert.Equal(0, writes);
        Assert.Equal("http://localhost/sales?region=APAC", _nav.Uri);
    }

    [Fact]
    public void A_value_the_filter_does_not_take_is_ignored_in_the_url()
    {
        _nav.NavigateTo("/sales?region=MARS");
        var dashboard = Dashboard(out var region, out _);
        Page(dashboard);
        Assert.Empty(region.Values);
    }

    [Fact]
    public void The_page_stops_listening_to_navigation_when_it_is_disposed()
    {
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        _nav.NavigateTo("/sales");
        var dashboard = Dashboard(out var region, out _);
        var cut = Page(dashboard);
        cut.Instance.Dispose();

        _nav.NavigateTo("/sales?region=APAC");
        Assert.Empty(region.Values);
    }
}
