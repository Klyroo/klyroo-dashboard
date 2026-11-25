# Getting started

In this tutorial you build a small orders dashboard. You start with the headless controller in a console app to see how resources and filters behave, then render the same idea in a Blazor page.

You need the .NET 10 SDK. For the Blazor part, use a Blazor Web App with interactivity enabled (Interactive Server, WebAssembly or Auto).

## 1. Install the packages

For the headless part, in any .NET 10 project:

```bash
dotnet add package Klyroo.Dashboard.Core --prerelease
```

For the Blazor part, in your Blazor Web App:

```bash
dotnet add package Klyroo.Dashboard --prerelease
```

`Klyroo.Dashboard` brings `Klyroo.Dashboard.Core` and `Klyroo.UI` with it.

## 2. Build a headless dashboard

A `DashboardController` owns the filters, the views and the resources of one dashboard. A `DashboardResource<T>` wraps one query. Put this in a console app:

```csharp
using Klyroo.Dashboard;

var controller = new DashboardController("orders");

var status = controller.AddFilter(new DashboardFilterControl(
    "status", "Status",
    items: [new("open", "Open"), new("shipped", "Shipped")],
    defaultValues: ["open"]));

var orders = controller.RegisterResource(new DashboardResource<int>(
    "orders", _ => CountOrdersAsync(status.Value)));

await controller.RefreshAsync();
Console.WriteLine($"{orders.State}: {orders.Data}");   // Ready: 12

status.Toggle("shipped");          // the selection changed...
await controller.RefreshAsync();   // ...but resources only reload when you ask
Console.WriteLine($"{orders.State}: {orders.Data}");   // Ready: 30

static Task<int> CountOrdersAsync(string? status) =>
    Task.FromResult(status == "open" ? 12 : 30);
```

Two things to notice. The loader reads the filter through a closure, so the controller never needs to know what a filter means for a query. And changing a filter does not reload anything by itself: the next step shows the Blazor equivalent, and the [guides](guides.md#reload-when-a-filter-changes) show how to reload automatically.

## 3. Register the services

In `Program.cs` of the Blazor Web App, and in the `.Client` project when you use WebAssembly or Auto:

```csharp
using Klyroo.Dashboard;

builder.Services.AddKlyrooDashboard();
```

In a .NET MAUI Blazor Hybrid app, call it in `MauiProgram.cs`. `AddKlyrooDashboard()` calls `AddKlyrooUi()`, so you do not register Klyroo.UI separately.

## 4. Add the imports

In `_Imports.razor`:

```razor
@using Klyroo.UI
@using Klyroo.Dashboard
```

## 5. Load the stylesheets

In the `<head>` of `App.razor`, load the Klyroo.UI styles and then the dashboard stylesheet. The dashboard stylesheet contains the utilities used by the dashboard markup and relies on the tokens defined by Klyroo.UI, so the order matters:

```razor
<link rel="stylesheet" href="@Assets["_content/Klyroo.UI/klyroo.preflight.css"]" />
<link rel="stylesheet" href="@Assets["_content/Klyroo.UI/klyroo.css"]" />
<link rel="stylesheet" href="@Assets["_content/Klyroo.Dashboard/klyroo-dashboard.css"]" />
```

In a MAUI Blazor Hybrid app, use the same `_content/...` paths in `wwwroot/index.html` without `@Assets[...]`.

## 6. Render the dashboard

Create `Components/Pages/Orders.razor`. `SalesService`, `Summary` and `RegionSales` stand for your own data access and records:

```razor
@page "/orders"
@rendermode InteractiveServer
@inject SalesService Sales

<KlyDashboardPage Dashboard="_dashboard" Title="Orders">
    <KlyDashboardGrid>
        <KlyDashboardStat Source="_summary" Label="Orders" Span="4"
                          Value="_summary.Data?.Orders" Format="DashboardFormatPreset.Integer" />
        <KlyDashboardBars Source="_regions" Title="Revenue by region" Span="8"
                          Items="_regions.Data?.Select(r => new DashboardBarItem(r.Region, r.Revenue)).ToList()"
                          Format="DashboardFormatPreset.Compact" />
    </KlyDashboardGrid>
</KlyDashboardPage>

@code {
    private readonly DashboardController _dashboard = new("orders");
    private DashboardResource<Summary> _summary = default!;
    private DashboardResource<IReadOnlyList<RegionSales>> _regions = default!;

    protected override async Task OnInitializedAsync()
    {
        _summary = _dashboard.RegisterResource(
            new DashboardResource<Summary>("summary", ct => Sales.GetSummaryAsync(null, ct)));
        _regions = _dashboard.RegisterResource(
            new DashboardResource<IReadOnlyList<RegionSales>>(
                "regions", ct => Sales.GetRegionsAsync(null, ct), DashboardQueryStage.Background));

        await _dashboard.RefreshAsync();
    }
}
```

The `@rendermode` line assumes the `@using static Microsoft.AspNetCore.Components.Web.RenderMode` that the Blazor Web App template puts in `_Imports.razor`. Run the app and open `/orders`. The two blocks first show skeletons. The stat appears when the essential query finishes, then the bars appear when the background query finishes. If a query throws, only its block turns into an error message with a Retry button.

Three details of the page are worth knowing:

- The `Source` parameter only drives the state of the block (skeleton, error, empty, content). The block never reads `Data` from the source, so you pass `_summary.Data?.Orders` yourself.
- `KlyDashboardPage` subscribes to the controller and re-renders when any resource changes. A block rendered outside a page does not update on its own.
- The page draws the view tabs and the filter pills, but you render the content of each view yourself (see [Load a block only when its tab opens](guides.md#load-a-block-only-when-its-tab-opens)).

## Next steps

- [Guides](guides.md): filters that reload data, lazy tabs, comparisons, CSV export and tests.
- [Concepts](concepts.md): how the controller, resources and blocks fit together.
- [Platforms](platforms.md): render modes, .NET MAUI Blazor Hybrid and NativeAOT.
- [FAQ](faq.md): skeletons that never go away, CSV that does nothing, and other known issues.
