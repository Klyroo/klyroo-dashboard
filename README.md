# Lux.Dashboard (LuxStack)

[![CI](https://github.com/luxstack/lux-dashboard/actions/workflows/ci.yml/badge.svg)](https://github.com/luxstack/lux-dashboard/actions/workflows/ci.yml) [![NuGet](https://img.shields.io/badge/NuGet-0.1.0--preview.1-004880.svg)](https://www.nuget.org/packages/Lux.Dashboard) [![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/) [![NativeAOT](https://img.shields.io/badge/NativeAOT-Compatible-10b981.svg)](https://luxstack.dev/why) [![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](./LICENSE)

> **Staged 3-wave analytical queries, global filters, comparison ranges & 13 KPI blocks**

Analytical dashboard orchestrator with 3-wave staged query loading (Essential -> Background -> Deferred), reactive global filter toolbar and views, leap-year-safe comparison date ranges, RFC 4180 UTF-8 BOM CSV export, and 13 ready-to-use KPI & visualization blocks.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](./LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![NativeAOT](https://img.shields.io/badge/Core-NativeAOT%20Ready-10b981.svg)](./docs/maui-and-aot.md)
[![MAUI Hybrid](https://img.shields.io/badge/.NET%20MAUI-Blazor%20Hybrid-8b5cf6.svg)](./docs/maui-and-aot.md)

---

## Architecture (Two-Layer Headless Split)

- **`Lux.Dashboard.Core`** — 100% headless C#, zero UI dependencies, `<IsAotCompatible>true</IsAotCompatible>`, `<IsTrimmable>true</IsTrimmable>`. Runs in Minimal APIs, background workers, CLI apps, and native C# .NET MAUI ViewModels.
- **`Lux.Dashboard`** — Multiplatform Blazor Web (Static SSR, Interactive Server, WebAssembly, Auto) & `.NET MAUI Blazor Hybrid` (`browser`, `ios`, `android`, `maccatalyst`, `windows`).

## Highlights

- **3-Wave Staged Query Orchestration** (`Staged Waves`) — DashboardController runs Essential KPIs first, Background charts second, and keeps Deferred resources idle until explicitly requested.
- **Global Filters, Presets & Views** (`Reactive Filters`) — Synchronized filters (DashboardFilterControl) with one-click Reset detection and tabbed DashboardViewMeta views with live count badges.
- **13 Analytical Visualization Blocks** (`13 Widgets`) — LxDashboardPage, Grid, Card, Stat, Stats, SVG Gauge, Details, Bars, List, sortable Table with CSV export, Alerts, Feed, and Chart.
- **Smart Formatters & CSV Export** (`Analytics Core`) — DashboardFormat formats Compact (12.6K), Delta (+14.2%), Points (+2.1 pts), Ratio (57%), leap-year date ranges, and locale-aware UTF-8 BOM CSV.

## Installation

```bash
dotnet add package Lux.Dashboard.Core   # Headless C# / NativeAOT / MAUI C#
dotnet add package Lux.Dashboard        # Blazor Web & .NET MAUI Blazor Hybrid
```

Register services in `Program.cs` (Blazor Web App) or `MauiProgram.cs` (.NET MAUI Blazor Hybrid):

```csharp
builder.Services.AddLuxDashboard();
```

## Quick Start

### Headless Core (`Lux.Dashboard.Core`)

```csharp
using Lux.Dashboard;

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

### Blazor Web & .NET MAUI Hybrid (`Lux.Dashboard`)

```razor
<LxDashboardPage Dashboard="@_dashboard" Title="Revenue Analytics">
    <LxDashboardGrid Menu="true" Freshness="true">
        <LxDashboardStats Title="Key Metrics" Items="@_statItems" />
        <LxDashboardGauge Title="ARR Goal" Value="84" Max="100" Target="90"
                          Format="DashboardFormatPreset.Percent" />
        <LxDashboardBars Title="Channels" Items="@_barItems"
                         Format="DashboardFormatPreset.Compact" />
    </LxDashboardGrid>
</LxDashboardPage>
```

## Documentation

| Guide | Description |
| :--- | :--- |
| [Getting Started](./docs/getting-started.md) | Installation, DI registration, stylesheets, and first working example |
| [Architecture](./docs/architecture.md) | Headless `.Core` vs UI split, NativeAOT guarantees, and platform matrix |
| [API Reference](./docs/api-reference.md) | Exhaustive reference of every public class, enum, property, method, and Razor parameter |
| [MAUI & NativeAOT](./docs/maui-and-aot.md) | `.NET MAUI Blazor Hybrid` (`iOS`, `Android`, `macOS`, `Windows`) and NativeAOT deployment |
| [Recipes & Testing](./docs/recipes.md) | End-to-end production patterns and `xUnit` / `bUnit` testing recipes |
| [AI Agent Skill](./skills/lux-dashboard/SKILL.md) | Machine-readable guidance for coding agents |

## Key Public Types

| Type | Kind | Package | Summary |
| :--- | :--- | :--- | :--- |
| `DashboardResourceState` | `enum` | `Lux.Dashboard.Core` | Lifecycle state of a dashboard query or derived resource. |
| `DashboardQueryStage` | `enum` | `Lux.Dashboard.Core` | Execution stage of a dashboard query. |
| `DashboardBlockPhase` | `enum` | `Lux.Dashboard.Core` | Block rendering phase determined by `LxDashboardCard`. |
| `DashboardSkeletonKind` | `enum` | `Lux.Dashboard.Core` | Skeleton geometry kind rendered while a `LxDashboardCard` is loading. |
| `DashboardFormatPreset` | `enum` | `Lux.Dashboard.Core` | Preset number formats supported by `DashboardFormat`. |
| `DashboardComparisonMode` | `enum` | `Lux.Dashboard.Core` | Comparison period mode for date ranges and KPI comparisons. |
| `DashboardFilterKind` | `enum` | `Lux.Dashboard.Core` | Filter control kind in a `DashboardController`. |
| `DashboardAlertSeverity` | `enum` | `Lux.Dashboard.Core` | Alert severity level for `LxDashboardAlerts`. |
| `IDashboardSource` | `interface` | `Lux.Dashboard.Core` | Common interface implemented by dashboard queries and derived resources. |
| `DashboardResource<TData>` | `class` | `Lux.Dashboard.Core` | A staged, optionally gated asynchronous data resource managed by a `DashboardController`. |
| `DashboardOption` | `record` | `Lux.Dashboard.Core` | An option item in a `DashboardFilterControl`. |
| `DashboardFilterPreset` | `record` | `Lux.Dashboard.Core` | A preset value in a `DashboardFilterControl`. |
| `DashboardFilterControl` | `class` | `Lux.Dashboard.Core` | Interactive filter control state managed by `DashboardController`. |
| `DashboardViewMeta` | `class` | `Lux.Dashboard.Core` | Metadata for a named dashboard view tab. |
| `DashboardController` | `class` | `Lux.Dashboard.Core` | Central controller for an analytical dashboard: coordinates filters, views, staged resources (`DashboardQueryStage.Essential`, `DashboardQueryStage.Background`, `DashboardQueryStage.Deferred`), refresh and auto-refresh state. |
| `DashboardDataColumn` | `record` | `Lux.Dashboard.Core` | Tabular view and CSV export representation of a dashboard block. |
| `DashboardDataTable` | `record` | `Lux.Dashboard.Core` | Tabular representation of a dashboard block used by "View as table" and "Download CSV". |
| `DashboardFormat` | `class` | `Lux.Dashboard.Core` | Locale-aware number, delta, relative-time, comparison-range, and CSV formatting utilities for the Dashboard domain. |

## Razor Components

| Component | Package | Parameters | Source |
| :--- | :--- | :--- | :--- |
| `<LxDashboardAlerts>` | `Lux.Dashboard` | 10 parameters | `src/Lux.Dashboard/LxDashboardAlerts.razor` |
| `<LxDashboardBars>` | `Lux.Dashboard` | 13 parameters | `src/Lux.Dashboard/LxDashboardBars.razor` |
| `<LxDashboardCard>` | `Lux.Dashboard` | 19 parameters | `src/Lux.Dashboard/LxDashboardCard.razor` |
| `<LxDashboardChart>` | `Lux.Dashboard` | 15 parameters | `src/Lux.Dashboard/LxDashboardChart.razor` |
| `<LxDashboardDetails>` | `Lux.Dashboard` | 8 parameters | `src/Lux.Dashboard/LxDashboardDetails.razor` |
| `<LxDashboardFeed>` | `Lux.Dashboard` | 11 parameters | `src/Lux.Dashboard/LxDashboardFeed.razor` |
| `<LxDashboardGauge>` | `Lux.Dashboard` | 14 parameters | `src/Lux.Dashboard/LxDashboardGauge.razor` |
| `<LxDashboardGrid>` | `Lux.Dashboard` | 5 parameters | `src/Lux.Dashboard/LxDashboardGrid.razor` |
| `<LxDashboardList>` | `Lux.Dashboard` | 10 parameters | `src/Lux.Dashboard/LxDashboardList.razor` |
| `<LxDashboardPage>` | `Lux.Dashboard` | 9 parameters | `src/Lux.Dashboard/LxDashboardPage.razor` |
| `<LxDashboardStat>` | `Lux.Dashboard` | 18 parameters | `src/Lux.Dashboard/LxDashboardStat.razor` |
| `<LxDashboardStats>` | `Lux.Dashboard` | 9 parameters | `src/Lux.Dashboard/LxDashboardStats.razor` |
| `<LxDashboardTable>` | `Lux.Dashboard` | 12 parameters | `src/Lux.Dashboard/LxDashboardTable.razor` |

## Building & Testing

```bash
dotnet build
dotnet test
dotnet pack -c Release
```

## License

MIT — see [LICENSE](./LICENSE). Part of the **[LuxStack](../lux.stack/README.md)** ecosystem.
