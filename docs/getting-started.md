# Getting Started with Lux.Dashboard

Analytical dashboard orchestrator with 3-wave staged query loading (Essential -> Background -> Deferred), reactive global filter toolbar and views, leap-year-safe comparison date ranges, RFC 4180 UTF-8 BOM CSV export, and 13 ready-to-use KPI & visualization blocks.

## 1. Choose Your Package

| Use Case | Package to Install |
| :--- | :--- |
| Headless C#, Minimal APIs, Workers, CLI, or native C# .NET MAUI ViewModels | `Lux.Dashboard.Core` |
| Blazor Web App (Server, WebAssembly, Auto, Static SSR) or `.NET MAUI Blazor Hybrid` | `Lux.Dashboard` |

```bash
dotnet add package Lux.Dashboard.Core
dotnet add package Lux.Dashboard
```

## 2. Register Services

Call `builder.Services.AddLuxDashboard();` in `Program.cs` (both Server and Client projects when using Blazor Interactive Auto) or `MauiProgram.cs` (after `AddMauiBlazorWebView()`):

```csharp
builder.Services.AddLuxDashboard();
```

## 3. Add Namespace Imports

In `_Imports.razor`:

```razor
@using Blux.UI
@using Blux.UI.Tools

```

## 4. First Example

### Headless C# (`Lux.Dashboard.Core`)

```csharp
using Blux.UI.Tools;

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

### Blazor / MAUI Hybrid (`Lux.Dashboard`)

```razor
<BxDashboardPage Dashboard="@_dashboard" Title="Revenue Analytics">
    <BxDashboardGrid Menu="true" Freshness="true">
        <BxDashboardStats Title="Key Metrics" Items="@_statItems" />
        <BxDashboardGauge Title="ARR Goal" Value="84" Max="100" Target="90"
                          Format="DashboardFormatPreset.Percent" />
        <BxDashboardBars Title="Channels" Items="@_barItems"
                         Format="DashboardFormatPreset.Compact" />
    </BxDashboardGrid>
</BxDashboardPage>
```

## Next Steps

- [Architecture & Two-Layer Design](./architecture.md)
- [Complete API Reference](./api-reference.md)
- [.NET MAUI & NativeAOT Guide](./maui-and-aot.md)
- [Recipes & Testing](./recipes.md)
