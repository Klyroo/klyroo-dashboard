# Recipes & Testing — Lux.Dashboard

## Recipe 1: Leap-Year Comparison Ranges & UTF-8 BOM CSV Export

```csharp
var prevYear = DashboardFormat.ResolveComparisonRange((leapStart, leapEnd), DashboardComparisonMode.Year);
var csv = DashboardFormat.ExportCsv(table, CultureInfo.InvariantCulture);
```

## Recipe 2: Headless Unit Testing (`xUnit`)

Test all business logic in `Lux.Dashboard.Core` without spinning up a browser or Blazor renderer:

```csharp
using Xunit;

public sealed class DashboardUnitTests
{
    [Fact]
    public void Headless_core_runs_without_ui_host()
    {
        // Arrange & Act using Lux.Dashboard.Core directly
        // Assert deterministic outputs with zero JSInterop setup
    }
}
```

## Recipe 3: Component Testing (`bUnit`)

```csharp
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class DashboardComponentTests : BunitContext
{
    public DashboardComponentTests()
    {
        Services.AddLuxDashboard();
    }
}
```

> **Tip**: Always register LuxStack services on `Services` **before** calling `SetRendererInfo(...)` in bUnit tests.
