# Guides

Task-oriented recipes. They assume the setup from [Getting started](getting-started.md). `SalesService`, `Summary` and `RegionSales` stand for your own data access and records.

- [Load data in two stages](#load-data-in-two-stages)
- [Reload when a filter changes](#reload-when-a-filter-changes)
- [Filter by text, number or date](#filter-by-text-number-or-date)
- [Load a block only when its tab opens](#load-a-block-only-when-its-tab-opens)
- [Handle failures and skip queries that cannot run](#handle-failures-and-skip-queries-that-cannot-run)
- [Compare with the previous period](#compare-with-the-previous-period)
- [Show a sortable table](#show-a-sortable-table)
- [Export a block to CSV](#export-a-block-to-csv)
- [Refresh on a timer](#refresh-on-a-timer)
- [Hand the prerendered data to the interactive render](#hand-the-prerendered-data-to-the-interactive-render)
- [Drill down from a block](#drill-down-from-a-block)
- [Translate or change the built-in texts](#translate-or-change-the-built-in-texts)
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

A refresh cancels the load still running for the same resource: the token the earlier loader received is cancelled and its result is ignored, so when the user changes the filter twice quickly the data of the latest selection wins. Pass the token on to your data access, as `GetSummaryAsync(region.Value, ct)` does here, so the superseded query stops too. `ResetFilters()` raises `ChangedEvent` once for every enabled, non-headless filter, so it can start several refreshes at once, and each one cancels the one before.

## Filter by text, number or date

Goal: let the user type a customer name, set a minimum amount or pick a period.

```razor
<KlyDashboardPage Dashboard="_dashboard" Title="Orders">
    <CustomFilter Context="filter">
        <button type="button" @onclick="@(() => filter.Value = "me")">Only mine</button>
    </CustomFilter>
    <ChildContent>
        @* blocks *@
    </ChildContent>
</KlyDashboardPage>

@code {
    private readonly DashboardController _dashboard = new("orders");

    protected override void OnInitialized()
    {
        var customer = _dashboard.AddFilter(new DashboardFilterControl("customer", "Customer", DashboardFilterKind.String));
        var minimum = _dashboard.AddFilter(new DashboardFilterControl("min", "Min amount", DashboardFilterKind.Number));
        var period = _dashboard.AddFilter(new DashboardFilterControl(
            "period", "Period", DashboardFilterKind.DateRange,
            presets: [new("Q1", ["2026-01-01", "2026-03-31"], Icon: "calendar")]));
        var compare = _dashboard.AddFilter(new DashboardFilterControl(
            "compare", "Compare", DashboardFilterKind.Comparison, defaultValues: ["previous"]));
        _dashboard.AddFilter(new DashboardFilterControl("owner", "Owner", DashboardFilterKind.Custom));
    }
}
```

The toolbar picks the control from `Kind`: a text box for `String`, a number box for `Number`, a date picker for `Date`, two date pickers for `DateRange`, your `CustomFilter` template for `Custom`, and the option pill for `Options`, `Enum`, `Remote`, `Boolean` and `Comparison`. A `Boolean` or `Comparison` filter created without items gets built-in options (`true` and `false`, or `none`, `previous` and `year`). Presets show next to the inputs, or in the option menu for the option kinds, with their icon when they have one.

Every kind keeps its selection as strings, so loaders read it the same way: `customer.Value` is the text, `minimum.Value` an invariant-culture number such as `1234.5`, `period.Values` the start and end as `yyyy-MM-dd` (an open bound is an empty string), and `compare.Value` a name that `Enum.TryParse<DashboardComparisonMode>(compare.Value, ignoreCase: true, out var mode)` reads back. Text and number boxes apply their value when they lose focus or the user presses Enter, and clearing a box restores the default values. Change a selection from code with `Value`, `SetValues` or `Reset`; `SetValues` keeps one value for a single-select filter, two for a date range, and at most `Max` in multiple mode. A `Custom` filter is left out of the toolbar when the page has no `CustomFilter` template.

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

A `Disabled` resource does not load, and a card bound to it renders nothing. The predicate is evaluated when the resource is created and on each activation and refresh. Once it returns `true`, the next `RefreshAsync` or `ActivateAsync` loads the resource, and a card bound to it calls `ActivateAsync` on every render, so the block appears as soon as the page renders again.

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

Every block with `Menu="true"` has a Download CSV button that saves its table as a file named after the title, for example `orders-by-region.csv`. The ready-made blocks build the table themselves, with the rows they show (after `Limit`) and raw numbers rather than the formatted text: `12600` and not `12.6K`, `14.2` for a delta of `+14.2%`. A custom block built on `KlyDashboardCard` gives the table through `Tabulate`:

```razor
@using System.Globalization

<KlyDashboardCard Title="Orders by region" Menu="true" Source="_orders"
                  IsEmpty="@(_orders.Data is not { Count: > 0 })"
                  Tabulate="BuildTable">
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
}
```

To save the file another way, for example in a .NET MAUI app whose web view cannot download, handle `OnCsvExported` on the card: it receives the CSV text and the card then starts no download.

`DashboardFormat.FormatInvariant` writes a number the way the ready-made blocks do, which is the safest choice for a numeric column.

Four details of the CSV matter. The text starts with a UTF-8 byte order mark so spreadsheets read accents correctly, and every line, the header included, ends with `\r\n`. Headers and text cells are quoted, and cells of a `Numeric` column that read as a number are written bare so spreadsheets read them as numbers, unless they contain the delimiter, as a formatted `1,234.5` does in a comma-delimited file, in which case they are quoted. Any other cell that starts with `=`, `+`, `-`, `@`, a tab or a carriage return gets a leading apostrophe, so a spreadsheet shows it as text instead of running it as a formula. And the delimiter is `;` when the culture uses a decimal comma, with numbers written with that comma (`4,25`), and `,` with a decimal point otherwise. The card uses the Klyroo.UI format culture for that choice. If you need to control it, skip the menu button and call `DashboardFormat.ExportCsv(table, culture)` yourself.

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
    private DashboardResource<Summary> _summary = default!;

    protected override async Task OnInitializedAsync()
    {
        _summary = _dashboard.RegisterResource(
            new DashboardResource<Summary>("summary", ct => Sales.GetSummaryAsync(null, ct)));

        await _dashboard.RefreshAsync();
        _dashboard.AutoRefresh = 30;
    }

    public void Dispose() => _dashboard.Dispose();
}
```

`AutoRefresh` is a number of seconds: a positive value starts a timer that calls `RefreshAsync`, and `0` stops it. A tick waits for its refresh to finish, so the timer never stacks refreshes, and it resumes on the synchronization context that set the value, so in Blazor the refresh runs on the renderer like a click would. Disposing the controller stops the timer and cancels the loads in progress, which is why the page implements `IDisposable`: without it the timer keeps the controller alive after the user leaves, and a prerendered page would leave one timer behind on the server.

A refresh of data that is already loaded keeps the old data on screen, shows a thin progress bar on the card while it runs, and `Freshness="true"` adds an `Updated ... ago` footer. A resource that failed on the previous tick goes through `Loading` again. In tests, pass a fake `TimeProvider` to the `DashboardController` constructor to drive the timer.

## Hand the prerendered data to the interactive render

Goal: a prerendered interactive page that loads its data once, not once for the prerender and again when it becomes interactive.

Give each resource to hand over a `JsonTypeInfo` from a source-generated `JsonSerializerContext`, attach the controller to `DashboardPersistence` after registering the resources and setting the filters, then load with `LoadAsync` instead of `RefreshAsync`:

```csharp
using System.Text.Json.Serialization;

[JsonSerializable(typeof(Summary))]
public sealed partial class SalesJson : JsonSerializerContext;
```

```razor
@page "/orders"
@rendermode InteractiveServer
@implements IDisposable
@inject SalesService Sales
@inject DashboardPersistence Persistence

<KlyDashboardPage Dashboard="_dashboard" Title="Orders">
    <KlyDashboardGrid>
        <KlyDashboardStat Source="_summary" Label="Orders" Span="4" Value="_summary.Data?.Orders" />
    </KlyDashboardGrid>
</KlyDashboardPage>

@code {
    private readonly DashboardController _dashboard = new("orders");
    private DashboardResource<Summary> _summary = default!;
    private IDisposable? _persistence;

    protected override async Task OnInitializedAsync()
    {
        _summary = _dashboard.RegisterResource(new DashboardResource<Summary>(
            "summary", ct => Sales.GetSummaryAsync(null, ct), json: SalesJson.Default.Summary));
        _persistence = Persistence.Attach(_dashboard, AssignedRenderMode);
        await _dashboard.LoadAsync();
    }

    public void Dispose()
    {
        _persistence?.Dispose();
        _dashboard.Dispose();
    }
}
```

In the prerender, `LoadAsync` loads the summary and `DashboardPersistence` writes it into the persistent component state of the page when the prerender ends. In the interactive render, `Attach` gives it back to the resource, which is ready at once with the time it was loaded (`Restored` is true), and `LoadAsync` keeps it instead of loading again. `RefreshAsync`, the refresh button, a filter change and `AutoRefresh` always load.

A few rules:

- Register the resources and set the filters before `Attach`. A resource registered later is handed over but not restored, and the data is refused when the filter values differ from those of the prerender (a filter read from browser storage only in the interactive render, say): that resource loads again.
- Pass the render mode the page runs in next. `AssignedRenderMode` is it when the page declares `@rendermode`; when the whole app is interactive (`<Routes @rendermode="InteractiveServer" />`) the page has none of its own, so pass the app's, for example `RenderMode.InteractiveServer`. With `null` nothing is handed over.
- Only a resource that is ready when the prerender ends is handed over. A background resource still loading then, or one in error, loads again in the interactive render, and so does a resource whose `requires` gate is closed there.
- The data is small on purpose, counted as sent: 8 KB per resource (`AddKlyrooDashboard(o => o.PersistMaxBytes = ...)`), within a budget of 28 KB per page that every Klyroo library of the page shares, Klyroo.Table lists included (`AddKlyrooUi(o => o.PersistedStateMaxBytes = ...)`). An Interactive Server circuit receives everything persisted in one SignalR message, refused past 32 KB by default, which fails the circuit, and Blazor escapes the text and base64-encodes it twice on the way, so the JSON of the data itself is about a third of these sizes. A resource over its limit, or one that no longer fits in the shared budget, loads again once, and a warning in the log names it.
- Data older than `PersistMaxAge` (5 minutes by default, from when it was loaded) is not restored. A prerender hands its data over within seconds, but .NET can also save the state of an Interactive Server circuit when it pauses and give it back when it resumes, possibly much later.
- A resource whose custom JSON converter throws is simply not handed over: the error is logged and the other resources, and the other libraries of the page, keep their state.
- Each dashboard of a page needs its own controller key. Two controllers with one key attached at once throw.
- A .NET MAUI Blazor Hybrid app has no prerender: `Attach` does nothing there, and `LoadAsync` is `RefreshAsync`.

`IDashboardPersistentSource` (`ExportJson`, `RestoreJson`) is the contract behind it, in `Klyroo.Dashboard.Core`, so any other host can hand data between two runs the same way.

## Drill down from a block

Goal: let the user pick a bar, a row or a chart point and filter the rest of the page by it.

`KlyDashboardBars.OnSelect`, `KlyDashboardTable.OnRowClick`, `KlyDashboardList.OnRowClick` and `KlyDashboardChart.OnSelect` hand the picked item to your code. The blocks do not change a filter themselves: you decide what a pick means, usually by setting a filter of the controller.

```razor
<KlyDashboardBars Title="Revenue by region" Items="_regions" OnSelect="PickRegion" />
<KlyDashboardChart Title="Revenue" Categories="_months" Series="_series" OnSelect="PickPoint" />
<KlyDashboardTable Columns="_columns" Rows="_rows" OnRowClick="PickRow" />

@code {
    private void PickRegion(DashboardBarItem bar) => _dashboard.Controls.First(c => c.Key == "region").Value = bar.Label;

    private void PickPoint(DashboardChartSelection point) => _dashboard.Controls.First(c => c.Key == "month").Value = point.Category;

    private void PickRow(IReadOnlyDictionary<string, object?> row) => Nav.NavigateTo($"/regions/{row["id"]}");
}
```

Every pick control is a real `<button>`, so it takes the focus, works with Enter and Space, and has an accessible name:

- A bar of `KlyDashboardBars` becomes a button as a whole, with `aria-pressed` following `DashboardBarItem.Selected`.
- In `KlyDashboardTable`, the first cell of each row holds a button, and a click anywhere on the row does the same for the pointer. The row passed to `OnRowClick` is the dictionary from `Rows`, whatever the sort.
- In `KlyDashboardList`, the label of a row becomes a button, and a click on the row does the same. A row with an `Href` keeps its link and raises nothing.
- `KlyDashboardChart` shows a group of buttons under the drawing, one per category and series (one per slice for a donut, comparison series left out). `KlyChart` of Klyroo.UI has no selection event, so the bars and slices themselves are not clickable, but its tooltip and arrow keys still explore them.

## Translate or change the built-in texts

Goal: show the buttons, accessible names, empty and error states, column headers and relative times in the language of the user.

Every text of the blocks (Refresh, View as table, Download CSV, Expand, Retry, `No data for this period`, the alert badges, the `Category` column of a chart table, `Updated 5 min ago` and so on) comes from a `DashboardTexts` set chosen by the locale of Klyroo.UI: the one of an enclosing `KlyApp` or `KlyLocale` cascading value, then `KlyrooOptions.Locale`, then the current UI culture. There is one set per locale of `KlyLocales.All` in `DashboardLocales.Tables`. English and French are reviewed (`DashboardLocales.ReviewedCodes`); the other ten are drafts that wait for a native speaker.

Change a set in `AddKlyrooDashboard`, in code shared by the server and the WebAssembly client. `Texts` receives the set the locale resolves to, so `fr-CA` starts from `fr`:

```csharp
builder.Services.AddKlyrooDashboard(o => o
    .Texts("fr", t => t with
    {
        Refresh = "Rafraîchir",
        RelativeMinutes = n => $"il y a {n} minutes",
    })
    .Texts("en", t => t with { NoData = "Nothing to show" }));
```

A text that holds a number or a name is a function (`RelativeMinutes`, `ClearFilter`, `Updated`), so a language chooses its own plural forms and word order. The sets become read-only the first time a component renders, and `Texts` then throws `InvalidOperationException`.

Text you pass to the blocks (titles, labels, filter options) is never translated. `DashboardFormat.FormatRelativeTime` is English unless you pass the words: `DashboardFormat.FormatRelativeTime(at, now, DashboardLocales.Fr.RelativeTime)`. The options `DashboardFilterControl` creates for a `Boolean` or `Comparison` filter (`Yes`, `No`, `No comparison`, ...) are built in `Klyroo.Dashboard.Core`, which has no locale: pass your own `items` to translate them.

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
using Klyroo.Dashboard;

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
