# Guides

Task-oriented recipes. They assume the setup from [Getting started](getting-started.md). `SalesService`, `Summary` and `RegionSales` stand for your own data access and records.

- [Load data in two stages](#load-data-in-two-stages)
- [Reload when a filter changes](#reload-when-a-filter-changes)
- [Load a block only when its tab opens](#load-a-block-only-when-its-tab-opens)
- [Handle failures and skip queries that cannot run](#handle-failures-and-skip-queries-that-cannot-run)
- [Compare with the previous period](#compare-with-the-previous-period)
- [Show a sortable table](#show-a-sortable-table)
- [Export a block to CSV](#export-a-block-to-csv)
- [Refresh on a timer](#refresh-on-a-timer)
- [Test a dashboard](#test-a-dashboard)

## Load data in two stages

Goal: show the headline numbers as soon as they are ready and load the heavier queries afterwards.

```csharp
using Klyroo.Dashboard;

var controller = new DashboardController("sales");

var kpis = controller.RegisterResource(new DashboardResource<Summary>(
    "kpis", ct => sales.GetSummaryAsync(null, ct), DashboardQueryStage.Essential));

var regions = controller.RegisterResource(new DashboardResource<IReadOnlyList<RegionSales>>(
    "regions", ct => sales.GetRegionsAsync(null, ct), DashboardQueryStage.Background));

await controller.RefreshAsync();

Console.WriteLine(controller.State);            // Ready
Console.WriteLine(kpis.Data?.Revenue);
Console.WriteLine(regions.Data?.Count);
```

`RefreshAsync` runs every Essential resource in parallel and waits for all of them. Only then does it start the Background resources, together with the Deferred resources that are already active. Essential is the default stage.

`controller.State` only looks at the Essential resources, so a failing background query does not turn the dashboard state to `Error`. A loader that throws never makes `RefreshAsync` throw: the exception message is stored in the resource and the block shows it.

## Reload when a filter changes

Goal: change the data when the user picks another region.

```razor
@inject SalesService Sales

<KlyDashboardPage Dashboard="_dashboard" Title="Sales by region">
    <KlyDashboardGrid>
        <KlyDashboardStat Source="_summary" Label="Revenue" Value="_summary.Data?.Revenue"
                          Format="DashboardFormatPreset.Compact" />
    </KlyDashboardGrid>
</KlyDashboardPage>

@code {
    private readonly DashboardController _dashboard = new("sales");
    private DashboardResource<Summary> _summary = default!;

    protected override async Task OnInitializedAsync()
    {
        var region = _dashboard.AddFilter(new DashboardFilterControl(
            "region", "Region",
            items: [new("emea", "EMEA"), new("amer", "Americas"), new("apac", "Asia-Pacific")],
            multiple: false,
            placeholder: "All regions"));

        _summary = _dashboard.RegisterResource(new DashboardResource<Summary>(
            "summary", ct => Sales.GetSummaryAsync(region.Value, ct)));

        region.ChangedEvent += () => _ = _dashboard.RefreshAsync();

        await _dashboard.RefreshAsync();
    }
}
```

The page draws a pill for every filter that is enabled and not headless. Selecting an option updates the filter and raises `ChangedEvent`, but nothing reloads on its own, so the handler calls `RefreshAsync`. The loader reads `region.Value` when it runs, which is why it needs no other wiring. With no default values, `Value` is `null` while nothing is selected and the pill shows the placeholder.

Refreshes are neither serialized nor cancelled, and a loader always receives `CancellationToken.None`. If the user changes the filter twice quickly, both loads run and the one that finishes last wins. `ResetFilters()` also raises `ChangedEvent` once for every enabled, non-headless filter, so it can trigger several refreshes at once.

## Load a block only when its tab opens

Goal: run an expensive query only if the user opens the tab that needs it.

```razor
@inject SalesService Sales

<KlyDashboardPage Dashboard="_dashboard" Title="Operations">
    @if (_dashboard.CurrentView == "overview")
    {
        <KlyDashboardGrid>
            <KlyDashboardStat Source="_summary" Label="Orders" Value="_summary.Data?.Orders" />
        </KlyDashboardGrid>
    }
    else if (_dashboard.CurrentView == "audit")
    {
        <KlyDashboardGrid>
            <KlyDashboardFeed Source="_audit" Title="Audit log" Span="12" Items="_audit.Data" />
        </KlyDashboardGrid>
    }
</KlyDashboardPage>

@code {
    private readonly DashboardController _dashboard = new("operations");
    private DashboardResource<Summary> _summary = default!;
    private DashboardResource<IReadOnlyList<DashboardFeedItem>> _audit = default!;

    protected override async Task OnInitializedAsync()
    {
        _dashboard.AddView("overview", "Overview");
        _dashboard.AddView("audit", "Audit");

        _summary = _dashboard.RegisterResource(
            new DashboardResource<Summary>("summary", ct => Sales.GetSummaryAsync(null, ct)));
        _audit = _dashboard.RegisterResource(
            new DashboardResource<IReadOnlyList<DashboardFeedItem>>(
                "audit", ct => Sales.GetAuditAsync(ct), DashboardQueryStage.Deferred));

        await _dashboard.RefreshAsync();
    }
}
```

The page draws the tabs (when at least two views are enabled) and sets `CurrentView` on click, but it does not decide what each view contains: you branch on `CurrentView` yourself. The Deferred resource is skipped by `RefreshAsync` until it is active. A card bound to a Deferred source activates it the first time the card is initialized, which here happens when the audit tab is first rendered. From then on the controller refreshes it with the Background stage. `DashboardViewMeta.Opened` tells you whether a view has been shown at least once.

## Handle failures and skip queries that cannot run

Goal: keep the dashboard usable when a query fails, and avoid queries whose inputs are missing.

An exception in a loader puts the resource in the `Error` state. Its card replaces the content with the message and a Retry button that calls `RefreshAsync` on that resource, while the rest of the dashboard keeps working. The card prints `Error`, which is the exception message, so throw a message that is safe to show:

```csharp
var controller = new DashboardController("sales");
controller.RegisterResource(new DashboardResource<Summary>("summary", async ct =>
{
    try
    {
        return await sales.GetSummaryAsync(null, ct);
    }
    catch (HttpRequestException)
    {
        throw new InvalidOperationException("The sales service is not reachable.");
    }
}));
```

A `requires` predicate skips the loader while a precondition is missing and puts the default value in `Data`:

```csharp
var controller = new DashboardController("customers");

var customer = controller.AddFilter(new DashboardFilterControl(
    "customer", "Customer",
    items: [new("c1", "Acme"), new("c2", "Globex")]));

var orders = controller.RegisterResource(new DashboardResource<IReadOnlyList<RegionSales>>(
    "orders",
    _ => sales.GetOrdersAsync(customer.Value!),
    defaultValue: [],
    requires: () => customer.Value is not null));
```

Without a `defaultValue` (and for a reference type), a resource whose `requires` returns `false` stays `Idle`, so its card keeps showing the skeleton. Give it a default value to make it `Ready` with that value.

An `enabled` predicate removes the block entirely:

```csharp
var controller = new DashboardController("sales");

controller.RegisterResource(new DashboardResource<Summary>(
    "margin",
    ct => sales.GetSummaryAsync(null, ct),
    enabled: () => isAdmin));
```

A `Disabled` resource never loads, and a card bound to it renders nothing. The predicate is evaluated when the resource is created and on each activation and refresh.

## Compare with the previous period

Goal: show a metric next to the value of the previous period.

For the dates, `ResolveComparisonRange` returns the range to query:

```csharp
using Klyroo.Dashboard;

var march = (
    Start: new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
    End: new DateTimeOffset(2026, 3, 31, 0, 0, 0, TimeSpan.Zero));

var previous = DashboardFormat.ResolveComparisonRange(march, DashboardComparisonMode.Previous);
// Jan 29 to Feb 28: the same 31 days, ending the day before March 1

var lastYear = DashboardFormat.ResolveComparisonRange(march, DashboardComparisonMode.Year);
// Mar 1 to Mar 31, 2025
```

`Previous` returns the same number of days ending the day before the range starts. `Year` shifts both bounds back one year and moves February 29 to February 28. The result is `null` for `DashboardComparisonMode.None` or a `null` range. Both bounds are inclusive days.

For the display, give the stat the previous value and it computes the delta and the caption:

```razor
<KlyDashboardStat Label="Revenue" Value="_summary.Data?.Revenue"
                  Compare="_summary.Data?.PreviousRevenue"
                  Format="DashboardFormatPreset.Compact" />
```

By default the delta is the percent change, `+15.5%` for 48,500 against 42,000. For a metric that is already a ratio, set `CompareMode="difference"` and the delta becomes a difference in points: 0.032 against 0.030 with `Format="DashboardFormatPreset.Ratio"` shows `+0.2 pts`. Set `InvertDelta` when a decrease is good news, as for churn, so the badge turns green when the value falls. A `Delta` you pass explicitly takes precedence over `Compare`.

## Show a sortable table

Goal: a table whose columns the user can sort, with typed cells.

```razor
<KlyDashboardTable Title="Regions" Columns="_columns" Rows="_rows" @bind-Sort="_sort" />

@code {
    private DashboardTableSort? _sort = new("revenue", Descending: true);

    private readonly IReadOnlyList<DashboardTableColumn> _columns =
    [
        new("region", "Region"),
        new("revenue", "Revenue", Type: "bar", Format: DashboardFormatPreset.Compact),
        new("orders", "Orders", Type: "number", Format: DashboardFormatPreset.Integer),
        new("growth", "Growth", Type: "delta"),
    ];

    private readonly IReadOnlyList<IReadOnlyDictionary<string, object?>> _rows =
    [
        new Dictionary<string, object?> { ["region"] = "EMEA", ["revenue"] = 21_300.0, ["orders"] = 140, ["growth"] = 12.5 },
        new Dictionary<string, object?> { ["region"] = "AMER", ["revenue"] = 18_900.0, ["orders"] = 120, ["growth"] = -4.0 },
    ];
}
```

Column types are strings: `text`, `number`, `delta`, `percent` and `bar`. Any other value behaves like `text`. A `delta` cell is colored green when positive and red when negative (reversed by `InvertDelta`), and a `bar` cell adds a bar scaled to the largest value of the column. Rows hold raw values: the table formats them.

Clicking a header cycles the sort. A text column goes ascending, descending, then unsorted. A numeric column goes descending first. Rows with an empty value always come last. Sorting happens in the component on the rows you pass, so for a large result set sort and page on the server and leave the table to display one page.

## Export a block to CSV

Goal: let the user download the content of a block.

The Download CSV button of a card only hands the CSV text to your code, through `OnCsvExported`, and only the `KlyDashboardCard` exposes that parameter. The ready-made blocks do not, so their Download CSV button does nothing. To offer an export, build the block on `KlyDashboardCard`:

```razor
@using System.Globalization
@inject IJSRuntime Js

<KlyDashboardCard Title="Orders by region" Menu="true" Source="_orders"
                  IsEmpty="@(_orders.Data is not { Count: > 0 })"
                  Tabulate="BuildTable" OnCsvExported="DownloadAsync">
    <ul>
        @foreach (var r in _orders.Data ?? [])
        {
            <li>@r.Region: @r.Orders</li>
        }
    </ul>
</KlyDashboardCard>

@code {
    private readonly DashboardResource<IReadOnlyList<RegionSales>> _orders = new(
        "orders",
        _ => Task.FromResult<IReadOnlyList<RegionSales>>([new("EMEA", 21_300, 140), new("AMER", 18_900, 120)]));

    protected override Task OnInitializedAsync() => _orders.RefreshAsync();

    private DashboardDataTable BuildTable() => new(
        [new("region", "Region"), new("orders", "Orders", Numeric: true)],
        (_orders.Data ?? []).Select(r => (IReadOnlyDictionary<string, string?>)new Dictionary<string, string?>
        {
            ["region"] = r.Region,
            ["orders"] = r.Orders.ToString(CultureInfo.InvariantCulture),
        }).ToList());

    private async Task DownloadAsync(string csv) =>
        await Js.InvokeVoidAsync("klyDownload", DashboardFormat.ResolveFileName("Orders by region"), csv);
}
```

The download itself is plain browser code. Add it once, for example in `App.razor`:

```html
<script>
  window.klyDownload = (name, text) => {
    const url = URL.createObjectURL(new Blob([text], { type: "text/csv;charset=utf-8" }));
    const link = Object.assign(document.createElement("a"), { href: url, download: name });
    link.click();
    URL.revokeObjectURL(url);
  };
</script>
```

Three details of the CSV matter. The text starts with a UTF-8 byte order mark so spreadsheets read accents correctly. Headers and text cells are quoted, but cells of a `Numeric` column are written as they are, so put numbers without group separators in them: a formatted `1,234.5` in a comma-delimited file splits into two columns. And the delimiter is `;` when the culture uses a decimal comma and `,` otherwise. The card uses the Klyroo.UI format culture for that choice. If you need to control it, skip the menu button and call `DashboardFormat.ExportCsv(table, culture)` yourself.

## Refresh on a timer

Goal: keep a wall display up to date.

```razor
@implements IDisposable
@inject SalesService Sales

<KlyDashboardPage Dashboard="_dashboard" Title="Live sales">
    <KlyDashboardGrid Freshness="true">
        <KlyDashboardStat Source="_summary" Label="Orders" Value="_summary.Data?.Orders" />
    </KlyDashboardGrid>
</KlyDashboardPage>

@code {
    private readonly DashboardController _dashboard = new("live");
    private readonly CancellationTokenSource _cts = new();
    private DashboardResource<Summary> _summary = default!;

    protected override async Task OnInitializedAsync()
    {
        _summary = _dashboard.RegisterResource(
            new DashboardResource<Summary>("summary", ct => Sales.GetSummaryAsync(null, ct)));

        await _dashboard.RefreshAsync();
        _ = RefreshEvery(TimeSpan.FromSeconds(30));
    }

    private async Task RefreshEvery(TimeSpan interval)
    {
        using var timer = new PeriodicTimer(interval);
        try
        {
            while (await timer.WaitForNextTickAsync(_cts.Token))
            {
                await _dashboard.RefreshAsync();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void Dispose() => _cts.Cancel();
}
```

`DashboardController.AutoRefresh` is only a number the controller stores for you: nothing in the library reads it or schedules a refresh, so the timer is yours. A refresh of data that is already loaded keeps the old data on screen, shows a thin progress bar on the card while it runs, and `Freshness="true"` adds an `Updated ... ago` footer. A resource that failed on the previous tick goes through `Loading` again.

## Test a dashboard

Goal: test the state logic without a renderer, and the blocks with bUnit.

The test project mirrors the repository's own: the Razor SDK, an executable output for xUnit v3, and the `xunit.v3` and `bunit` packages, plus a reference to `Klyroo.Dashboard`.

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit.v3" Version="4.0.3" />
    <PackageReference Include="bunit" Version="2.15.2" />
    <PackageReference Include="Klyroo.Dashboard" Version="0.1.0-preview.1" />
  </ItemGroup>
</Project>
```

The controller and the resources need no host, so a plain xUnit test covers the stages and the failure path:

```csharp
using Bunit;
using Klyroo.Dashboard;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class SalesDashboardTests
{
    [Fact]
    public async Task Failed_query_surfaces_the_message_and_recovers_on_retry()
    {
        var fail = true;
        var controller = new DashboardController("sales");
        var revenue = controller.RegisterResource(new DashboardResource<double>(
            "revenue",
            _ => fail ? throw new InvalidOperationException("Upstream timeout") : Task.FromResult(48_500.0)));

        await controller.RefreshAsync();
        Assert.Equal(DashboardResourceState.Error, controller.State);
        Assert.Equal("Upstream timeout", revenue.Error);

        fail = false;
        await revenue.RefreshAsync();
        Assert.Equal(DashboardResourceState.Ready, revenue.State);
        Assert.Equal(48_500.0, revenue.Data);
    }

    [Fact]
    public async Task Background_resources_wait_for_essential_ones()
    {
        var order = new List<string>();
        var controller = new DashboardController("sales");
        controller.RegisterResource(new DashboardResource<int>(
            "late", _ => { order.Add("background"); return Task.FromResult(0); }, DashboardQueryStage.Background));
        controller.RegisterResource(new DashboardResource<int>(
            "first", async _ => { await Task.Delay(20); order.Add("essential"); return 0; }));

        await controller.RefreshAsync();

        Assert.Equal(["essential", "background"], order);
    }
}
```

For components, register the services on the bUnit context first, then render. The components mark their parts with `data-slot` attributes, which the library's own tests select on:

```csharp
public sealed class SalesDashboardComponentTests : BunitContext
{
    public SalesDashboardComponentTests() => Services.AddKlyrooDashboard();

    [Fact]
    public void Stat_shows_the_change_against_the_previous_period()
    {
        var cut = Render<KlyDashboardStat>(p => p
            .Add(x => x.Label, "Revenue")
            .Add(x => x.Value, 120.0)
            .Add(x => x.Compare, 100.0));

        Assert.Contains("+20%", cut.Find("[data-slot=dashboard-stat-delta]").TextContent);
    }

    [Fact]
    public async Task Page_renders_loaded_blocks()
    {
        var controller = new DashboardController("sales");
        var summary = controller.RegisterResource(new DashboardResource<double>(
            "revenue", _ => Task.FromResult(48_500.0)));
        await controller.RefreshAsync();

        var cut = Render<KlyDashboardPage>(p => p
            .Add(x => x.Dashboard, controller)
            .Add(x => x.Title, "Sales")
            .AddChildContent<KlyDashboardStat>(s => s
                .Add(x => x.Source, summary)
                .Add(x => x.Label, "Revenue")
                .Add(x => x.Value, summary.Data)));

        Assert.Equal("Sales", cut.Find("[data-slot=dashboard-title]").TextContent);
        Assert.Contains("48.5K", cut.Find("[data-slot=dashboard-stat-value]").TextContent);
    }
}
```

Pass `Now` to `KlyDashboardFeed` in tests, since its day headings and relative times otherwise depend on the clock.
