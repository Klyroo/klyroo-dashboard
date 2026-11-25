# Klyroo.Dashboard.Core

The headless part of [Klyroo.Dashboard](https://github.com/klyroo/klyroo-dashboard): a controller that coordinates
filters, views and staged queries, plus number, date and CSV formatting. It has no UI dependency, is marked trimmable
and AOT compatible, and runs in console apps, workers, minimal APIs and MAUI view models. Use the `Klyroo.Dashboard`
package for the Blazor components.

## Install

```bash
dotnet add package Klyroo.Dashboard.Core --prerelease
```

## Use

```csharp
using Klyroo.Dashboard;

var controller = new DashboardController("sales");

var revenue = controller.RegisterResource(new DashboardResource<double>(
    "revenue", async _ => { await Task.Delay(50); return 48_500; }));

await controller.RefreshAsync();

Console.WriteLine(DashboardFormat.Format(revenue.Data, DashboardFormatPreset.Compact)); // 48.5K
```

Documentation, guides and the API reference are in the
[repository](https://github.com/klyroo/klyroo-dashboard#readme).
