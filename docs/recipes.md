# Recipes & Testing — Klyroo.Dashboard

## Recipe 1: Leap-Year Comparison Ranges & UTF-8 BOM CSV Export

```csharp
var prevYear = DashboardFormat.ResolveComparisonRange((leapStart, leapEnd), DashboardComparisonMode.Year);
var csv = DashboardFormat.ExportCsv(table, CultureInfo.InvariantCulture);
```

## Recipe 2: Headless Unit Testing (`xUnit`)

Test all business logic in `Klyroo.Dashboard.Core` without spinning up a browser or Blazor renderer:

```csharp
using Xunit;

public sealed class DashboardUnitTests
{
    [Fact]
    public void Headless_core_runs_without_ui_host()
    {
        // Arrange & Act using Klyroo.Dashboard.Core directly
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
        Services.AddKlyrooDashboard();
    }
}
```

> **Tip**: Always register Klyroo services on `Services` **before** calling `SetRendererInfo(...)` in bUnit tests.
