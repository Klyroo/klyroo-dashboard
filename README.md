# Klyroo.Dashboard

Analytical dashboards for .NET developers: a headless controller for filters, views and staged queries, and Blazor blocks that render them.

[![CI](https://github.com/klyroo/klyroo-dashboard/actions/workflows/ci.yml/badge.svg)](https://github.com/klyroo/klyroo-dashboard/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/vpre/Klyroo.Dashboard.svg)](https://www.nuget.org/packages/Klyroo.Dashboard)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/klyroo/klyroo-dashboard/blob/main/LICENSE)

A dashboard usually fires a dozen queries and one slow or failing query should not blank the page. Klyroo.Dashboard
tracks each query as a resource with its own state, loads the important ones first, and gives every block its own
loading, error and empty states. The state lives in `Klyroo.Dashboard.Core`, which has no UI dependency, so you can
test it without a renderer. The Blazor components run in a Blazor Web App and in .NET MAUI Blazor Hybrid.

## Features

- **Staged loading**: a resource is Essential (loaded first, in parallel), Background (loaded once the essential ones settle) or Deferred (loaded only when something activates it). A new load cancels the one it supersedes, and `AutoRefresh` reloads on a timer.
- **Per-block states**: every block shows a skeleton while loading, the error message with a retry button when its query fails, and an empty state, so one failing query leaves the rest of the page intact.
- **Filters and views**: `DashboardFilterControl` supports single and multiple selection with a maximum, presets, default values and reset detection, drawn as option lists, text, number and date inputs or your own template, and views appear as tabs with optional badges.
- **Ten blocks on a grid of 12 columns by default**: stat, stats, gauge (ring or bar), bars (with a funnel variant), chart (bars, lines, areas or donut, drawn by the `KlyChart` of Klyroo.UI), list, table, details, feed and alerts, wrapped by a page, a grid and a card.
- **Comparisons**: a stat computes its delta from a previous value, and `DashboardFormat.ResolveComparisonRange` gives the previous period or the same dates one year earlier.
- **Number formatting**: nine presets (compact, percent, ratio, delta, points and more), relative times and currency, with an explicit culture.
- **CSV export**: every block with a menu downloads what it shows, with raw numbers; `DashboardFormat.ExportCsv` writes a UTF-8 byte order mark, picks the delimiter from the culture and neutralizes spreadsheet formulas.
- **Prerender without a second load**: resources created with a `JsonTypeInfo` hand the data the prerender loaded to the interactive render through `DashboardPersistence`, without reflection.
- **Headless core**: `Klyroo.Dashboard.Core` marks itself trimmable and AOT compatible.

## Packages

| Package | Description |
| :--- | :--- |
| [`Klyroo.Dashboard.Core`](https://www.nuget.org/packages/Klyroo.Dashboard.Core) | Controller, resources, filters, views and formatting. No UI dependency. |
| [`Klyroo.Dashboard`](https://www.nuget.org/packages/Klyroo.Dashboard) | Blazor components for Blazor Web Apps and .NET MAUI Blazor Hybrid. Depends on `Klyroo.Dashboard.Core` and `Klyroo.UI`. |

## Prerequisites

To use the library in an app:

- **The .NET 10 SDK**, and an app that targets `net10.0`. The packages are prereleases, so every `dotnet add package` command needs `--prerelease`.
- **`Klyroo.Dashboard.Core`** alone (controller, resources, formatting, CSV) runs in any .NET 10 app.
- **A Blazor app** for `Klyroo.Dashboard`: a Blazor Web App or a .NET MAUI Blazor Hybrid app. Tabs, filters, refresh, sorting and the card menu need an interactive render mode (Interactive Server, WebAssembly or Auto). In a WebAssembly or Auto app, register the services in the server project and in the `.Client` project.
- **Klyroo.UI set up in the app.** `Klyroo.Dashboard` brings the Klyroo.UI package, and `AddKlyrooDashboard()` registers its services, but the layout still needs the Klyroo.UI stylesheets in `<head>` and `<KlyApp>` around the body, as described in the [Klyroo.UI installation](https://github.com/klyroo/klyroo-ui#installation).
- **A browser with the native popover API**, which Klyroo.UI uses: Chrome and Edge 114, Safari 17 or Firefox 125, or later. In a .NET MAUI app, that means iOS or Mac Catalyst 17, or an Android System WebView 114 or later.
- **Network access on the first build** only if your app self-hosts web fonts (`KlyrooFont`), uses icons outside the built-in set, or compiles Tailwind (`KlyrooTailwind`): the build downloads them once and caches them under `obj/klyroo`. Node.js is not needed.

To build this repository you need the .NET 10 SDK (a newer SDK works too, see `global.json`) and a clone of `klyroo-ui` in the same parent folder: some projects reference it by relative path (`../klyroo-ui/...`). See [Contributing](https://github.com/klyroo/klyroo-dashboard/blob/main/CONTRIBUTING.md).

## Installation

```bash
dotnet add package Klyroo.Dashboard --prerelease
```

For headless use only, install `Klyroo.Dashboard.Core` instead.

Register the services in `Program.cs` of a Blazor Web App (and of the `.Client` project when you use WebAssembly or Auto), or in `MauiProgram.cs`:

```csharp
using Klyroo.Dashboard;

builder.Services.AddKlyrooDashboard();
```

Add the namespaces to `_Imports.razor`:

```razor
@using Klyroo.UI
@using Klyroo.Dashboard
```

Load the Klyroo.UI styles and the dashboard stylesheet in the `<head>` of `App.razor`. The dashboard stylesheet only contains the utilities the dashboard markup uses and reads the tokens that Klyroo.UI defines, so it comes after them:

```razor
<link rel="stylesheet" href="@Assets["_content/Klyroo.UI/klyroo.preflight.css"]" />
<link rel="stylesheet" href="@Assets["_content/Klyroo.UI/klyroo.css"]" />
<link rel="stylesheet" href="@Assets["_content/Klyroo.Dashboard/klyroo-dashboard.css"]" />
```

## Quick start

Headless first. Two resources, one per stage, loaded by a single call:

```csharp
using Klyroo.Dashboard;

var controller = new DashboardController("sales");

var revenue = controller.RegisterResource(new DashboardResource<double>(
    "revenue", async _ => { await Task.Delay(50); return 48_500; }));

var regions = controller.RegisterResource(new DashboardResource<string[]>(
    "regions", _ => Task.FromResult(new[] { "EMEA", "AMER" }), DashboardQueryStage.Background));

await controller.RefreshAsync();

Console.WriteLine(DashboardFormat.Format(revenue.Data, DashboardFormatPreset.Compact)); // 48.5K
Console.WriteLine(string.Join(", ", regions.Data!));                                      // EMEA, AMER
```

Then the same idea in Blazor. `SalesService`, `Summary` and `RegionSales` stand for your own data access and records:

```razor
@inject SalesService Sales

<KlyDashboardPage Dashboard="_dashboard" Title="Sales" Description="Revenue and orders by region">
    <KlyDashboardGrid Freshness="true">
        <KlyDashboardStat Source="_summary" Label="Revenue" Span="4"
                          Value="_summary.Data?.Revenue" Compare="_summary.Data?.PreviousRevenue"
                          Format="DashboardFormatPreset.Compact" />
        <KlyDashboardStat Source="_summary" Label="Orders" Span="4"
                          Value="_summary.Data?.Orders" Format="DashboardFormatPreset.Integer" />
        <KlyDashboardGauge Source="_summary" Title="Quota" Span="4" Value="84" Target="90"
                           Format="DashboardFormatPreset.Percent" />
        <KlyDashboardBars Source="_regions" Title="Revenue by region" Span="12"
                          Items="_regions.Data?.Select(r => new DashboardBarItem(r.Region, r.Revenue)).ToList()"
                          Format="DashboardFormatPreset.Compact" />
    </KlyDashboardGrid>
</KlyDashboardPage>

@code {
    private readonly DashboardController _dashboard = new("sales");
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

A block does not read the data of its `Source`: the source only drives the loading, error and empty states, and you pass the values through the block parameters.

## Documentation

| | |
| :--- | :--- |
| [Getting started](https://github.com/klyroo/klyroo-dashboard/blob/main/docs/getting-started.md) | Step-by-step first integration |
| [Guides](https://github.com/klyroo/klyroo-dashboard/blob/main/docs/guides.md) | Task-oriented recipes |
| [Concepts](https://github.com/klyroo/klyroo-dashboard/blob/main/docs/concepts.md) | How the library works |
| [Platforms](https://github.com/klyroo/klyroo-dashboard/blob/main/docs/platforms.md) | Render modes, .NET MAUI, NativeAOT |
| [API reference](https://github.com/klyroo/klyroo-dashboard/blob/main/docs/api-reference.md) | Every public type and member |
| [FAQ](https://github.com/klyroo/klyroo-dashboard/blob/main/docs/faq.md) | Troubleshooting and limitations |

## Compatibility

- .NET 10 SDK.
- `Klyroo.Dashboard.Core` runs in any .NET 10 app (minimal APIs, workers, console apps, MAUI view models) and declares `IsTrimmable` and `IsAotCompatible`.
- `Klyroo.Dashboard` needs a Blazor Web App or a .NET MAUI Blazor Hybrid app. It declares `IsTrimmable` but not `IsAotCompatible`, and declares the supported platforms `browser`, `ios`, `android`, `maccatalyst` and `windows`.
- Static SSR renders the blocks, but tabs, filter controls, the refresh button, the table sort and the copy and CSV buttons need an interactive render mode (Interactive Server, WebAssembly or Auto).
- The block texts (Refresh, View as table, Retry and so on) follow the Klyroo.UI locale in the 12 languages of Klyroo.UI, English and French reviewed and the others drafts. Change them with `KlyrooDashboardOptions.Texts` (see [Guides](docs/guides.md#translate-or-change-the-built-in-texts)).

## Klyroo ecosystem

Klyroo is a family of independent .NET libraries that compose. Each one has its own repository, versions and releases.

| Library | Description |
| :--- | :--- |
| [Klyroo.UI](https://github.com/klyroo/klyroo-ui) | Accessible Blazor components for the web and .NET MAUI Blazor Hybrid, styled with Tailwind CSS v4 |
| [Klyroo.Query](https://github.com/klyroo/klyroo-query) | Typed query model, URL state sync, and IQueryable / EF Core execution |
| [Klyroo.Table](https://github.com/klyroo/klyroo-table) | Data lists with table and card-grid layouts, filters, selection and actions |
| [Klyroo.Form](https://github.com/klyroo/klyroo-form) | Schema-driven forms, validation and wizards |
| **[Klyroo.Dashboard](https://github.com/klyroo/klyroo-dashboard)** | Dashboard controller, filters and Blazor blocks: stats, gauges, charts, tables and feeds |
| [Klyroo.Display](https://github.com/klyroo/klyroo-display) | Read-only display blocks, media player controls and file preview |
| [Klyroo.Spreadsheet](https://github.com/klyroo/klyroo-spreadsheet) | Import wizard for CSV, TSV and pasted data, with column and value mapping |
| [Klyroo.Agent](https://github.com/klyroo/klyroo-agent) | Bridge that lets AI agents read and act on Klyroo forms, dashboards and imports |
| [Klyroo.Stack](https://github.com/klyroo/klyroo-stack) | Meta-package, MCP documentation server and `dotnet new` templates |

## Contributing

Contributions are welcome. Read [CONTRIBUTING.md](https://github.com/klyroo/klyroo-dashboard/blob/main/CONTRIBUTING.md) before opening a pull request, and follow the
[code of conduct](https://github.com/klyroo/klyroo-dashboard/blob/main/CODE_OF_CONDUCT.md). Report security issues privately as described in [SECURITY.md](https://github.com/klyroo/klyroo-dashboard/blob/main/SECURITY.md).

## License

[MIT](https://github.com/klyroo/klyroo-dashboard/blob/main/LICENSE)
