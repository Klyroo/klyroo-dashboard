using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Klyroo.Dashboard.Tests;

public sealed class ComponentBehaviorTests : BunitContext
{
    private const string ModulePath = "./_content/Klyroo.Dashboard/klyroo-dashboard.js";
    private readonly BunitJSModuleInterop _module;

    public ComponentBehaviorTests()
    {
        Services.AddKlyrooDashboard();
        _module = JSInterop.SetupModule(ModulePath);
        _module.SetupVoid("downloadText", _ => true);
    }

    [Fact]
    public void Download_CSV_saves_the_file_through_the_module_when_no_handler_is_set()
    {
        var cut = Render<KlyDashboardBars>(p => p
            .Add(x => x.Title, "Top Regions")
            .Add(x => x.Menu, true)
            .Add(x => x.Items, [new DashboardBarItem("EMEA", 12_600)]));

        cut.Find("[data-slot=dashboard-card-csv]").Click();

        var invocation = _module.VerifyInvoke("downloadText");
        Assert.Equal(DashboardFormat.ResolveFileName("Top Regions"), invocation.Arguments[0]);
        Assert.Equal("top-regions.csv", invocation.Arguments[0]);
        Assert.Equal("text/csv;charset=utf-8", invocation.Arguments[2]);
    }

    [Fact]
    public void Bars_export_honours_Limit_and_carries_raw_numbers()
    {
        var cut = Render<KlyDashboardBars>(p => p
            .Add(x => x.Title, "Regions")
            .Add(x => x.Menu, true)
            .Add(x => x.Limit, 2)
            .Add(x => x.Format, DashboardFormatPreset.Compact)
            .Add(x => x.Items, [
                new DashboardBarItem("EMEA", 12_600),
                new DashboardBarItem("AMER", 9_000.5),
                new DashboardBarItem("APAC", 4_000),
            ]));

        cut.Find("[data-slot=dashboard-card-csv]").Click();
        var csv = (string)_module.VerifyInvoke("downloadText").Arguments[1]!;

        Assert.Contains("\"EMEA\",12600\r\n", csv);
        Assert.Contains("\"AMER\",9000.5\r\n", csv);
        Assert.DoesNotContain("APAC", csv);
        Assert.DoesNotContain("12.6K", csv);

        cut.Find("[data-slot=dashboard-card-table-toggle]").Click();
        Assert.Equal(2, cut.FindAll("[data-slot=dashboard-data-table] tbody tr").Count);
    }

    [Fact]
    public void List_and_Table_exports_honour_Limit()
    {
        var list = Render<KlyDashboardList>(p => p
            .Add(x => x.Title, "Accounts")
            .Add(x => x.Menu, true)
            .Add(x => x.Limit, 1)
            .Add(x => x.Items, [
                new DashboardListItem("Acme", Value: 120_000, Delta: 4.25),
                new DashboardListItem("Beta", Value: 85_000),
            ]));
        list.Find("[data-slot=dashboard-card-csv]").Click();
        var listCsv = (string)_module.VerifyInvoke("downloadText").Arguments[1]!;
        Assert.Contains("\"Acme\",4.25,120000\r\n", listCsv);
        Assert.DoesNotContain("Beta", listCsv);

        var table = Render<KlyDashboardTable<RegionRow>>(p => p
            .Add(x => x.Title, "Regions")
            .Add(x => x.Menu, true)
            .Add(x => x.Limit, 1)
            .Add(x => x.Sort, new DashboardTableSort("revenue", Descending: true))
            .Add(x => x.Columns, [
                DashboardTableColumn.For<RegionRow, string>("region", "Region", r => r.Name),
                DashboardTableColumn.For<RegionRow, double>("revenue", "Revenue", r => r.Revenue, DashboardFormatPreset.Compact),
                DashboardTableColumn.For<RegionRow, double?>("growth", "Growth", r => r.Growth, kind: DashboardTableColumnKind.Delta),
            ])
            .Add(x => x.Rows, [
                new RegionRow("EMEA", 21_300.0, -4.5),
                new RegionRow("AMER", 18_900.0, 2.0),
            ]));
        table.Find("[data-slot=dashboard-card-csv]").Click();
        var tableCsv = (string)_module.Invocations["downloadText"][1].Arguments[1]!;
        Assert.Contains("\"EMEA\",21300,-4.5\r\n", tableCsv);
        Assert.DoesNotContain("AMER", tableCsv);
    }

    [Fact]
    public void Stat_Gauge_Feed_and_Alerts_export_a_table()
    {
        var now = new DateTimeOffset(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);
        Render<KlyDashboardStat>(p => p.Add(x => x.Label, "Revenue").Add(x => x.Menu, true)
                .Add(x => x.Value, 48_500).Add(x => x.Compare, 40_000))
            .Find("[data-slot=dashboard-card-csv]").Click();
        Render<KlyDashboardGauge>(p => p.Add(x => x.Title, "Uptime").Add(x => x.Menu, true)
                .Add(x => x.Value, 99.5).Add(x => x.Target, 99.9))
            .Find("[data-slot=dashboard-card-csv]").Click();
        Render<KlyDashboardFeed>(p => p.Add(x => x.Title, "Activity").Add(x => x.Menu, true).Add(x => x.Now, now)
                .Add(x => x.Limit, 1)
                .Add(x => x.Items, [new DashboardFeedItem("Deployed", now), new DashboardFeedItem("Merged", now.AddDays(-1))]))
            .Find("[data-slot=dashboard-card-csv]").Click();
        Render<KlyDashboardAlerts>(p => p.Add(x => x.Title, "Alerts").Add(x => x.Menu, true)
                .Add(x => x.Items, [new DashboardAlertItem(DashboardAlertSeverity.Info, "Backup", Value: "=2+2"), new DashboardAlertItem(DashboardAlertSeverity.Error, "Latency")]))
            .Find("[data-slot=dashboard-card-csv]").Click();
        var csvs = _module.Invocations["downloadText"].Select(i => (string)i.Arguments[1]!).ToList();

        Assert.Contains("\"Revenue\",48500,40000,21.25\r\n", csvs[0]);
        Assert.Contains("\"Change (%)\"", csvs[0]);
        Assert.Contains("\"Uptime\",99.5,0,100,99.9\r\n", csvs[1]);
        Assert.Contains("\"2026-11-24T12:00:00.0000000+00:00\",\"Deployed\",", csvs[2]);
        Assert.DoesNotContain("Merged", csvs[2]);
        var alertLines = csvs[3].Split("\r\n");
        Assert.Equal("\"Critical\",\"Latency\",,", alertLines[1]);
        Assert.Equal("\"Info\",\"Backup\",,\"'=2+2\"", alertLines[2]);
    }

    [Fact]
    public void Stats_show_the_missing_value_placeholder()
    {
        var cut = Render<KlyDashboardStats>(p => p
            .Add(x => x.Items, [new DashboardStatsItem("mrr", "MRR", null)]));

        Assert.Equal(DashboardFormat.MissingValue, cut.Find("[data-slot=dashboard-stats-value]").TextContent.Trim());
    }

    [Fact]
    public void Alerts_render_no_dead_link_and_a_button_when_OnAction_is_set()
    {
        var item = new DashboardAlertItem(DashboardAlertSeverity.Warning, "Disk full", ActionLabel: "Clean up");
        var withoutHandler = Render<KlyDashboardAlerts>(p => p.Add(x => x.Items, [item]));
        Assert.Empty(withoutHandler.FindAll("a"));
        Assert.Empty(withoutHandler.FindAll("[data-slot=dashboard-alert-action]"));

        DashboardAlertItem? clicked = null;
        var withHandler = Render<KlyDashboardAlerts>(p => p
            .Add(x => x.Items, [item])
            .Add(x => x.OnAction, (DashboardAlertItem a) => clicked = a));
        var action = withHandler.Find("[data-slot=dashboard-alert-action]");
        Assert.Equal("BUTTON", action.TagName);
        action.Click();
        Assert.Same(item, clicked);

        var linked = Render<KlyDashboardAlerts>(p => p
            .Add(x => x.Items, [item with { ActionHref = "/disks" }]));
        Assert.Equal("/disks", linked.Find("a[data-slot=dashboard-alert-action]").GetAttribute("href"));
    }

    [Fact]
    public void Grid_Columns_sets_the_template_and_clamps_the_spans()
    {
        var cut = Render<KlyDashboardGrid>(p => p
            .Add(x => x.Columns, 3)
            .AddChildContent<KlyDashboardStat>(s => s.Add(x => x.Label, "Orders").Add(x => x.Span, 12)));

        var grid = cut.Find("[data-slot=dashboard-grid]");
        Assert.Contains("grid-template-columns: repeat(3, minmax(0, 1fr))", grid.GetAttribute("style"));
        Assert.DoesNotContain("grid-cols-12", grid.ClassName);
        Assert.Equal("grid-column: span 3 / span 3", cut.Find("[data-slot=dashboard-card]").GetAttribute("style"));
    }

    [Fact]
    public void A_block_in_a_grid_takes_the_full_row_on_a_narrow_screen()
    {
        var cut = Render<KlyDashboardGrid>(p => p
            .AddChildContent<KlyDashboardStat>(s => s.Add(x => x.Label, "Orders").Add(x => x.Span, 4)));

        Assert.Contains("max-sm:col-span-full!", cut.Find("[data-slot=dashboard-card]").ClassName);
        Assert.Equal("grid-column: span 4 / span 4", cut.Find("[data-slot=dashboard-card]").GetAttribute("style"));
    }

    [Fact]
    public void Gauge_bar_variant_draws_a_horizontal_meter_with_a_target_marker()
    {
        var bar = Render<KlyDashboardGauge>(p => p
            .Add(x => x.Variant, DashboardGaugeVariant.Bar)
            .Add(x => x.Value, 75)
            .Add(x => x.Target, 90));

        Assert.Equal("bar", bar.Find("[data-slot=dashboard-gauge]").GetAttribute("data-variant"));
        Assert.Equal("width:75%", bar.Find("[data-slot=gauge-bar-fill]").GetAttribute("style"));
        Assert.Contains("left:calc(90% - 1px)", bar.Find("[data-slot=gauge-target-marker]").GetAttribute("style"));
        Assert.Empty(bar.FindAll("svg"));

        var ring = Render<KlyDashboardGauge>(p => p.Add(x => x.Value, 75));
        Assert.Equal("ring", ring.Find("[data-slot=dashboard-gauge]").GetAttribute("data-variant"));
        Assert.Single(ring.FindAll("svg"));
    }

    [Fact]
    public void Details_Copy_writes_the_value_to_the_clipboard()
    {
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        JSInterop.SetupVoid("navigator.clipboard.writeText", "ord_123");
        var cut = Render<KlyDashboardDetails>(p => p
            .Add(x => x.Items, [
                new DashboardDetailsItem("id", "Order id", "ord_123", Copy: true),
                new DashboardDetailsItem("region", "Region", "EMEA"),
            ]));

        var button = Assert.Single(cut.FindAll("[data-slot=dashboard-details-copy]"));
        Assert.Equal("Copy Order id", button.GetAttribute("aria-label"));
        button.Click();

        JSInterop.VerifyInvoke("navigator.clipboard.writeText");
    }

    [Fact]
    public void Chart_export_carries_every_series_as_raw_numbers()
    {
        var cut = Render<KlyDashboardChart>(p => p
            .Add(x => x.Title, "Revenue")
            .Add(x => x.Menu, true)
            .Add(x => x.Format, DashboardFormatPreset.Compact)
            .Add(x => x.Categories, ["Jan"])
            .Add(x => x.Series, [
                new DashboardChartSeries("revenue", "Revenue", [12_600]),
                new DashboardChartSeries("previous", "Previous", [10_000.5], IsComparison: true),
            ]));

        cut.Find("[data-slot=dashboard-card-csv]").Click();
        var csv = (string)_module.VerifyInvoke("downloadText").Arguments[1]!;
        Assert.Contains("\"Jan\",12600,10000.5\r\n", csv);
    }

    [Theory]
    [InlineData(DashboardSkeletonKind.Paired, "[data-slot=dashboard-skeleton-pair]")]
    [InlineData(DashboardSkeletonKind.Stack, "[data-slot=dashboard-skeleton-stack]")]
    public void Paired_and_Stack_skeletons_have_their_own_shape(DashboardSkeletonKind kind, string selector)
    {
        var idle = new DashboardResource<int>("idle", _ => Task.FromResult(1));
        var cut = Render<KlyDashboardCard>(p => p
            .Add(x => x.Source, idle)
            .Add(x => x.Skeleton, kind));

        Assert.Equal(3, cut.FindAll(selector).Count);
    }

    [Fact]
    public void A_card_loads_its_disabled_essential_source_once_it_is_enabled()
    {
        var enabled = false;
        var source = new DashboardResource<int>("kpis", _ => Task.FromResult(3), enabled: () => enabled);
        var cut = Render<KlyDashboardCard>(p => p
            .Add(x => x.Source, source)
            .AddChildContent("<p data-slot=\"loaded\">ok</p>"));
        Assert.Equal(DashboardResourceState.Disabled, source.State);

        enabled = true;
        cut.Render();

        cut.WaitForAssertion(() => Assert.Equal(DashboardResourceState.Ready, source.State));
        Assert.NotNull(cut.Find("[data-slot=loaded]"));
    }

    [Fact]
    public void A_gauge_with_a_NaN_value_draws_an_empty_bar()
    {
        var cut = Render<KlyDashboardGauge>(p => p
            .Add(x => x.Value, double.NaN)
            .Add(x => x.Variant, DashboardGaugeVariant.Bar));

        Assert.DoesNotContain("NaN", cut.Markup);
    }

    [Fact]
    public void A_card_loads_its_disabled_source_once_it_is_enabled()
    {
        var enabled = false;
        var source = new DashboardResource<int>(
            "audit", _ => Task.FromResult(3), DashboardQueryStage.Deferred, enabled: () => enabled);
        var cut = Render<KlyDashboardCard>(p => p
            .Add(x => x.Source, source)
            .AddChildContent("<p data-slot=\"loaded\">ok</p>"));
        Assert.Equal(DashboardResourceState.Disabled, source.State);
        Assert.Empty(cut.FindAll("[data-slot=dashboard-card]"));

        enabled = true;
        cut.Render();

        cut.WaitForAssertion(() => Assert.Equal(DashboardResourceState.Ready, source.State));
        Assert.Equal(3, source.Data);
    }

    [Fact]
    public void Page_renders_again_when_a_view_badge_or_filter_items_change()
    {
        var controller = new DashboardController("ops");
        var overview = controller.AddView("overview", "Overview");
        controller.AddView("details", "Details");
        var region = controller.AddFilter(new DashboardFilterControl("region", "Region"));
        var cut = Render<KlyDashboardPage>(p => p.Add(x => x.Dashboard, controller));
        Assert.Empty(cut.FindAll("[data-slot=dashboard-view-badge]"));

        overview.Badge = "7";
        cut.WaitForAssertion(() => Assert.Equal("7", cut.Find("[data-slot=dashboard-view-badge]").TextContent));

        region.Items = [new DashboardOption("emea", "EMEA", Description: "Europe", Icon: "globe")];
        cut.Find("[data-slot=dashboard-filter-pill]").Click();
        var option = cut.Find("[data-slot=dashboard-filter-option]");
        Assert.Equal("Europe", option.QuerySelector("[data-slot=dashboard-filter-option-description]")!.TextContent);
        Assert.NotNull(option.QuerySelector("svg[data-icon=globe]"));

        overview.Enabled = false;
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-slot=dashboard-view-tab]")));
        Assert.Equal("details", controller.CurrentView);
    }

    [Fact]
    public void Toolbar_draws_a_control_for_each_filter_kind()
    {
        var controller = new DashboardController("sales");
        var search = controller.AddFilter(new DashboardFilterControl("q", "Search", DashboardFilterKind.String));
        var amount = controller.AddFilter(new DashboardFilterControl("min", "Min amount", DashboardFilterKind.Number));
        var day = controller.AddFilter(new DashboardFilterControl("day", "Day", DashboardFilterKind.Date));
        var period = controller.AddFilter(new DashboardFilterControl(
            "period", "Period", DashboardFilterKind.DateRange,
            presets: [new DashboardFilterPreset("Q1", ["2026-01-01", "2026-03-31"], Icon: "calendar")]));
        var active = controller.AddFilter(new DashboardFilterControl("active", "Active", DashboardFilterKind.Boolean));
        controller.AddFilter(new DashboardFilterControl("owner", "Owner", DashboardFilterKind.Custom));

        var cut = Render<KlyDashboardPage>(p => p
            .Add(x => x.Dashboard, controller)
            .Add(x => x.CustomFilter, filter => $"<span data-slot=\"custom\">{filter.Label}</span>"));

        Filter(cut, "q").QuerySelector("input")!.Change("acme");
        Assert.Equal("acme", search.Value);

        Filter(cut, "min").QuerySelector("input[type=number]")!.Change("1234.50");
        Assert.Equal("1234.5", amount.Value);

        Filter(cut, "day").QuerySelector("input[type=date]")!.Change("2026-03-05");
        Assert.Equal("2026-03-05", day.Value);
        Filter(cut, "day").QuerySelector("input[type=date]")!.Change("not a date");
        Assert.Equal("2026-03-05", day.Value);

        Filter(cut, "period").QuerySelector("[data-slot=dashboard-filter-end]")!.Change("2026-02-28");
        Assert.Equal(["", "2026-02-28"], period.Values);
        Filter(cut, "period").QuerySelector("[data-slot=dashboard-filter-start]")!.Change("2026-02-01");
        Assert.Equal(["2026-02-01", "2026-02-28"], period.Values);
        var preset = Filter(cut, "period").QuerySelector("[data-slot=dashboard-filter-preset]")!;
        Assert.NotNull(preset.QuerySelector("svg[data-icon=calendar]"));
        preset.Click();
        Assert.Equal(["2026-01-01", "2026-03-31"], period.Values);

        Filter(cut, "active").QuerySelector("[data-slot=dashboard-filter-pill]")!.Click();
        Assert.Equal(["true", "false"], cut.FindAll("[data-key=active] [data-slot=dashboard-filter-option]").Select(o => o.GetAttribute("data-value")));
        cut.Find("[data-key=active] [data-slot=dashboard-filter-option][data-value=false]").Click();
        Assert.Equal("No", Filter(cut, "active").QuerySelector("[data-slot=dashboard-filter-display]")!.TextContent);
        Assert.Equal("false", active.Value);

        Assert.Equal("Owner", Filter(cut, "owner").QuerySelector("[data-slot=custom]")!.TextContent);

        var withoutTemplate = Render<KlyDashboardPage>(p => p.Add(x => x.Dashboard, controller));
        Assert.Empty(withoutTemplate.FindAll("[data-key=owner]"));
    }

    private static AngleSharp.Dom.IElement Filter(IRenderedComponent<KlyDashboardPage> cut, string key) =>
        cut.Find($"[data-slot=dashboard-filter][data-key={key}]");
}
