# Getting Started with Klyroo.Dashboard

Analytical dashboard orchestrator with 3-wave staged query loading (Essential -> Background -> Deferred), reactive global filter toolbar and views, leap-year-safe comparison date ranges, RFC 4180 UTF-8 BOM CSV export, and 13 ready-to-use KPI & visualization blocks.

## 1. Choose Your Package

| Use Case | Package to Install |
| :--- | :--- |
| Headless C#, Minimal APIs, Workers, CLI, or native C# .NET MAUI ViewModels | `Klyroo.Dashboard.Core` |
| Blazor Web App (Server, WebAssembly, Auto, Static SSR) or `.NET MAUI Blazor Hybrid` | `Klyroo.Dashboard` |

```bash
dotnet add package Klyroo.Dashboard.Core
dotnet add package Klyroo.Dashboard
```

## 2. Register Services

Call `builder.Services.AddKlyrooDashboard();` in `Program.cs` (both Server and Client projects when using Blazor Interactive Auto) or `MauiProgram.cs` (after `AddMauiBlazorWebView()`):

```csharp
builder.Services.AddKlyrooDashboard();
```

## 3. Add Namespace Imports

In `_Imports.razor`:

```razor
@using Klyroo.UI
@using Klyroo.Dashboard

```

## 4. First Example

### Headless C# (`Klyroo.Dashboard.Core`)

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

### Blazor / MAUI Hybrid (`Klyroo.Dashboard`)

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

## Next Steps

- [Architecture & Two-Layer Design](./architecture.md)
- [Complete API Reference](./api-reference.md)
- [.NET MAUI & NativeAOT Guide](./maui-and-aot.md)
- [Recipes & Testing](./recipes.md)
