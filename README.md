# Klyroo.Dashboard

*Part of [Klyroo](https://klyroo.dev): Build better. Compose freely.*

[![CI](https://github.com/klyroo/klyroo-dashboard/actions/workflows/ci.yml/badge.svg)](https://github.com/klyroo/klyroo-dashboard/actions/workflows/ci.yml) [![NuGet](https://img.shields.io/badge/NuGet-0.1.0--preview.1-004880.svg)](https://www.nuget.org/packages/Klyroo.Dashboard) [![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/) [![NativeAOT](https://img.shields.io/badge/NativeAOT-Compatible-10b981.svg)](https://klyroo.dev/why) [![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](./LICENSE)

> **Staged 3-wave analytical queries, global filters, comparison ranges & 13 KPI blocks**

Analytical dashboard orchestrator with 3-wave staged query loading (Essential -> Background -> Deferred), reactive global filter toolbar and views, leap-year-safe comparison date ranges, RFC 4180 UTF-8 BOM CSV export, and 13 ready-to-use KPI & visualization blocks.

[![MAUI Hybrid](https://img.shields.io/badge/.NET%20MAUI-Blazor%20Hybrid-8b5cf6.svg)](./docs/maui-and-aot.md)

---

## Architecture (Two-Layer Headless Split)

- **`Klyroo.Dashboard.Core`** — 100% headless C#, zero UI dependencies, `<IsAotCompatible>true</IsAotCompatible>`, `<IsTrimmable>true</IsTrimmable>`. Runs in Minimal APIs, background workers, CLI apps, and native C# .NET MAUI ViewModels.
- **`Klyroo.Dashboard`** — Multiplatform Blazor Web (Static SSR, Interactive Server, WebAssembly, Auto) & `.NET MAUI Blazor Hybrid` (`browser`, `ios`, `android`, `maccatalyst`, `windows`).

## Highlights

- **3-Wave Staged Query Orchestration** (`Staged Waves`) — DashboardController runs Essential KPIs first, Background charts second, and keeps Deferred resources idle until explicitly requested.
- **Global Filters, Presets & Views** (`Reactive Filters`) — Synchronized filters (DashboardFilterControl) with one-click Reset detection and tabbed DashboardViewMeta views with live count badges.
- **13 Analytical Visualization Blocks** (`13 Widgets`) — KlyDashboardPage, Grid, Card, Stat, Stats, SVG Gauge, Details, Bars, List, sortable Table with CSV export, Alerts, Feed, and Chart.
- **Smart Formatters & CSV Export** (`Analytics Core`) — DashboardFormat formats Compact (12.6K), Delta (+14.2%), Points (+2.1 pts), Ratio (57%), leap-year date ranges, and locale-aware UTF-8 BOM CSV.

## Prerequisites

- .NET 10 SDK or later.
- A Blazor Web App (Static SSR, Interactive Server, WebAssembly or Auto) or a .NET MAUI Blazor Hybrid app for the UI
  package; the `.Core` package runs anywhere .NET 10 runs, NativeAOT included.
- For .NET MAUI: the `maui` workload (`dotnet workload install maui`).

## Installation

```bash
dotnet add package Klyroo.Dashboard.Core   # Headless C# / NativeAOT / MAUI C#
dotnet add package Klyroo.Dashboard        # Blazor Web & .NET MAUI Blazor Hybrid
```

Register services in `Program.cs` (Blazor Web App) or `MauiProgram.cs` (.NET MAUI Blazor Hybrid):

```csharp
builder.Services.AddKlyrooDashboard();
```

## Quick Start

### Headless Core (`Klyroo.Dashboard.Core`)

```csharp
using Klyroo.Dashboard;

var controller = new DashboardController("analytics");
controller.AddView("overview", "Overview", badge: "3");
controller.AddFilter(new DashboardFilterControl("year", "Year",
    items: [new("2025", "2025"), new("2026", "2026")], defaultValues: ["2026"]));

var kpis = controller.RegisterResource(new DashboardResource<int>(
    "summary", LoadSummaryAsync, DashboardQueryStage.Essential));
var chart = controller.RegisterResource(new DashboardResource<double[]>(
    "trend", LoadTrendAsync, DashboardQueryStage.Background));

await controller.RefreshAsync(); // Runs Essential -> Background
```

### Blazor Web & .NET MAUI Hybrid (`Klyroo.Dashboard`)

```razor
<KlyDashboardPage Dashboard="@_dashboard" Title="Revenue Analytics">
    <KlyDashboardGrid Menu="true" Freshness="true">
        <KlyDashboardStats Title="Key Metrics" Items="@_statItems" />
        <KlyDashboardGauge Title="ARR Goal" Value="84" Max="100" Target="90"
                          Format="DashboardFormatPreset.Percent" />
        <KlyDashboardBars Title="Channels" Items="@_barItems"
                         Format="DashboardFormatPreset.Compact" />
    </KlyDashboardGrid>
</KlyDashboardPage>
```

## Documentation

| Guide | Description |
| :--- | :--- |
| [Online documentation](https://docs.klyroo.dev/dashboard) | Guides, live examples and API reference |
| [Getting Started](./docs/getting-started.md) | Installation, DI registration, stylesheets, and first working example |
| [Architecture](./docs/architecture.md) | Headless `.Core` vs UI split, NativeAOT guarantees, and platform matrix |
| [API Reference](./docs/api-reference.md) | Exhaustive reference of every public class, enum, property, method, and Razor parameter |
| [MAUI & NativeAOT](./docs/maui-and-aot.md) | `.NET MAUI Blazor Hybrid` (`iOS`, `Android`, `macOS`, `Windows`) and NativeAOT deployment |
| [Recipes & Testing](./docs/recipes.md) | End-to-end production patterns and `xUnit` / `bUnit` testing recipes |
| [AI Agent Skill](./skills/klyroo-dashboard/SKILL.md) | Machine-readable guidance for coding agents |

## Key Public Types

| Type | Kind | Package | Summary |
| :--- | :--- | :--- | :--- |
| `DashboardResourceState` | `enum` | `Klyroo.Dashboard.Core` | Lifecycle state of a dashboard query or derived resource. |
| `DashboardQueryStage` | `enum` | `Klyroo.Dashboard.Core` | Execution stage of a dashboard query. |
| `DashboardBlockPhase` | `enum` | `Klyroo.Dashboard.Core` | Block rendering phase determined by `KlyDashboardCard`. |
| `DashboardSkeletonKind` | `enum` | `Klyroo.Dashboard.Core` | Skeleton geometry kind rendered while a `KlyDashboardCard` is loading. |
| `DashboardFormatPreset` | `enum` | `Klyroo.Dashboard.Core` | Preset number formats supported by `DashboardFormat`. |
| `DashboardComparisonMode` | `enum` | `Klyroo.Dashboard.Core` | Comparison period mode for date ranges and KPI comparisons. |
| `DashboardFilterKind` | `enum` | `Klyroo.Dashboard.Core` | Filter control kind in a `DashboardController`. |
| `DashboardAlertSeverity` | `enum` | `Klyroo.Dashboard.Core` | Alert severity level for `KlyDashboardAlerts`. |
| `IDashboardSource` | `interface` | `Klyroo.Dashboard.Core` | Common interface implemented by dashboard queries and derived resources. |
| `DashboardResource<TData>` | `class` | `Klyroo.Dashboard.Core` | A staged, optionally gated asynchronous data resource managed by a `DashboardController`. |
| `DashboardOption` | `record` | `Klyroo.Dashboard.Core` | An option item in a `DashboardFilterControl`. |
| `DashboardFilterPreset` | `record` | `Klyroo.Dashboard.Core` | A preset value in a `DashboardFilterControl`. |
| `DashboardFilterControl` | `class` | `Klyroo.Dashboard.Core` | Interactive filter control state managed by `DashboardController`. |
| `DashboardViewMeta` | `class` | `Klyroo.Dashboard.Core` | Metadata for a named dashboard view tab. |
| `DashboardController` | `class` | `Klyroo.Dashboard.Core` | Central controller for an analytical dashboard: coordinates filters, views, staged resources (`DashboardQueryStage.Essential`, `DashboardQueryStage.Background`, `DashboardQueryStage.Deferred`), refresh and auto-refresh state. |
| `DashboardDataColumn` | `record` | `Klyroo.Dashboard.Core` | Tabular view and CSV export representation of a dashboard block. |
| `DashboardDataTable` | `record` | `Klyroo.Dashboard.Core` | Tabular representation of a dashboard block used by "View as table" and "Download CSV". |
| `DashboardFormat` | `class` | `Klyroo.Dashboard.Core` | Locale-aware number, delta, relative-time, comparison-range, and CSV formatting utilities for the Dashboard domain. |

## Razor Components

| Component | Package | Parameters | Source |
| :--- | :--- | :--- | :--- |
| `<KlyDashboardAlerts>` | `Klyroo.Dashboard` | 10 parameters | `src/Klyroo.Dashboard/KlyDashboardAlerts.razor` |
| `<KlyDashboardBars>` | `Klyroo.Dashboard` | 13 parameters | `src/Klyroo.Dashboard/KlyDashboardBars.razor` |
| `<KlyDashboardCard>` | `Klyroo.Dashboard` | 19 parameters | `src/Klyroo.Dashboard/KlyDashboardCard.razor` |
| `<KlyDashboardChart>` | `Klyroo.Dashboard` | 15 parameters | `src/Klyroo.Dashboard/KlyDashboardChart.razor` |
| `<KlyDashboardDetails>` | `Klyroo.Dashboard` | 8 parameters | `src/Klyroo.Dashboard/KlyDashboardDetails.razor` |
| `<KlyDashboardFeed>` | `Klyroo.Dashboard` | 11 parameters | `src/Klyroo.Dashboard/KlyDashboardFeed.razor` |
| `<KlyDashboardGauge>` | `Klyroo.Dashboard` | 14 parameters | `src/Klyroo.Dashboard/KlyDashboardGauge.razor` |
| `<KlyDashboardGrid>` | `Klyroo.Dashboard` | 5 parameters | `src/Klyroo.Dashboard/KlyDashboardGrid.razor` |
| `<KlyDashboardList>` | `Klyroo.Dashboard` | 10 parameters | `src/Klyroo.Dashboard/KlyDashboardList.razor` |
| `<KlyDashboardPage>` | `Klyroo.Dashboard` | 9 parameters | `src/Klyroo.Dashboard/KlyDashboardPage.razor` |
| `<KlyDashboardStat>` | `Klyroo.Dashboard` | 18 parameters | `src/Klyroo.Dashboard/KlyDashboardStat.razor` |
| `<KlyDashboardStats>` | `Klyroo.Dashboard` | 9 parameters | `src/Klyroo.Dashboard/KlyDashboardStats.razor` |
| `<KlyDashboardTable>` | `Klyroo.Dashboard` | 12 parameters | `src/Klyroo.Dashboard/KlyDashboardTable.razor` |

## Building & Testing

```bash
dotnet build
dotnet test
dotnet pack -c Release
```

## Contributing

Issues and pull requests are welcome. Read [CONTRIBUTING.md](./CONTRIBUTING.md) for the workflow (Conventional
Commits, tests, documentation), follow the [Code of Conduct](./CODE_OF_CONDUCT.md), and report vulnerabilities
privately as described in [SECURITY.md](./SECURITY.md).

## Migrating from Lux

This library was previously named `lux-dashboard`. [MIGRATION.md](./MIGRATION.md) lists every renamed package, namespace,
component, CSS token and build setting.

## License

MIT — see [LICENSE](./LICENSE). Part of the **[Klyroo](../klyroo-stack/README.md)** ecosystem.
