# Klyroo.Dashboard.Maui

Native .NET MAUI views (no WebView, no Blazor) over [Klyroo.Dashboard.Core](../Klyroo.Dashboard.Core): the dashboard page, grid and blocks of the Blazor package `Klyroo.Dashboard`, built on the native controls of `Klyroo.UI.Maui` and its design tokens (light and dark follow the theme of the app).

The state stays in `Klyroo.Dashboard.Core`: a `DashboardController` with its filters, views and `DashboardResource<T>` sources, and `DashboardFormat` for numbers, relative times and CSV. These controls are views over it. They use the same type names as the Blazor components, in the namespace `Klyroo.Dashboard.Maui`, so an app can reference both packages.

## Controls

| Control | Built on | Notes |
| :--- | :--- | :--- |
| `KlyDashboardPage` | `ContentView` | Title, refresh button, view tabs and filter controls of a `DashboardController`, and a scrolling `Body`. `Sticky`, `Scrollable`, `CustomFilter`, `Actions`. |
| `KlyDashboardGrid` | `Border` + `Grid` | `Columns` (12), `Variant` (`Cards`, `Panels`), `Menu`, `Freshness`. Lays out by width with `SizeChanged`: below 640 units every block takes the full row. |
| `KlyDashboardCard` | `Border` | The frame of every block: header, menu, skeleton, error with retry, empty state, filter chips, footer. `Body` takes any view. The other blocks derive from it. |
| `KlyDashboardStat` | `KlyDashboardCard` + `KlyBadge` | Value, delta, sparkline, goal bar, status. |
| `KlyDashboardStats` | `KlyDashboardCard` | Metrics in 4, 2 or 1 columns by width, `Plain` or `Tiles`. |
| `KlyDashboardChart` | `KlyChart` | Bar, line, area, donut. A bar chart with a line series, or with `ShowLabels`, has a compact drawing of its own (`KlyChart` draws one kind per chart). |
| `KlyDashboardBars` | `KlyDashboardCard` | Ranking bars or a funnel; the scale always includes zero. |
| `KlyDashboardGauge` | `KlyDashboardCard` | Ring or bar, with a target. |
| `KlyDashboardList`, `KlyDashboardFeed`, `KlyDashboardAlerts`, `KlyDashboardDetails` | `KlyDashboardCard` + `KlyBadge`, `KlyIcon` | Ranked rows, a timeline grouped by day, alerts by severity, a definition list with copy buttons. |
| `KlyDashboardTable<TRow>` | `KlyDashboardCard` | Typed columns of `DashboardTableColumn.For`, tap-to-sort headers, bar and delta cells. |

## Use

```csharp
using Klyroo.Dashboard;
using Klyroo.Dashboard.Maui;

var controller = new DashboardController("sales");
var revenue = controller.RegisterResource(new DashboardResource<double>("revenue", async _ => 48_500));
controller.AddFilter(new DashboardFilterControl("region", "Region", items: [new("emea", "EMEA"), new("us", "US")]));

var stat = new KlyDashboardStat { Label = "Revenue", Source = revenue, Format = DashboardFormatPreset.Compact, Span = 4 };
controller.Changed += () => MainThread.BeginInvokeOnMainThread(() => stat.Value = revenue.Data);

var page = new KlyDashboardPage
{
    Title = "Sales",
    Dashboard = controller,
    Body = new KlyDashboardGrid { Menu = true, Children = { stat } },
};
await controller.LoadAsync();
```

A block reads its data from the properties you give it, not from `Source.Data`: `Source` only drives the skeleton, the error and the empty state. Inside a `KlyDashboardPage` the page refreshes its blocks when the controller changes, so set their properties from `controller.Changed` (or bind them) as you would in Razor. A block outside a page needs `Refresh()` after its source changes.

## What the app provides

- Wrap the page content in a `KlyOverlayHost` of Klyroo.UI.Maui to get the Expand button of a block (it opens the content in a `KlyModal`). Without a host the button is not offered.
- Nothing to register in the service collection. Texts are picked from the UI culture of the app (`KlyCulture.Set` of Klyroo.UI.Maui, or `CultureInfo`); `KlyDashboardTexts.Configure("fr", t => t with { Refresh = "..." })` changes a set at startup.
- Download CSV raises `KlyDashboardCard.CsvExported` with the CSV text. Without a subscriber the card writes the file to the cache folder and offers it to the share sheet of the device.

## Differences from the Blazor controls

- `EventCallback<T>` parameters are events: `OnSelect` is `Selected` (chart, bars), `OnRowClick` is `RowClicked` (list, table), `OnAction` is `ActionInvoked`, `OnCsvExported` is `CsvExported`. A block that listens becomes tappable, as in Blazor.
- `KlyDashboardChart.Height` is `PlotHeight`, because a native view already has a `Height`. The drawing fills the width of the card instead of being capped at 640.
- `KlyDashboardPage` scrolls its body itself (`Scrollable`), and with `Sticky` the header and the toolbar stay at the top, where the web page lets the header scroll away. The option lists of the filters open under the filters, in the toolbar.
- `SyncUrl` and `UrlPrefix` (filters in the URL) have no native equivalent and are not ported; `DashboardController.ToQuery` and `ApplyQuery` stay available for a deep link of your own. `Class` and `AdditionalAttributes` are web-only.
- A child of `KlyDashboardGrid` that is not a block takes one column, as an element of a CSS grid does (a full row below 640). A block that hides itself (a disabled source) or is hidden by the app leaves no gap.
- A refresh keeps what is on screen: a block builds its content again only when a property of the block, its phase, the table view or the dialog changed, so a scroll position, a focus or an open menu of your own views in the card survives a controller change.
- A view added with `DashboardController.AddView` after the page was built shows when the page builds itself again (the controller raises no event for it, in Blazor as here).

## Build

This project is not part of `Klyroo.Dashboard.slnx`: it needs the MAUI workload. It targets `net10.0` (the unit tests run on it) and `net10.0-android`, plus iOS and Mac Catalyst outside Linux and Windows on Windows. It references `../../../klyroo-ui/src/Klyroo.UI.Maui`, so the two repositories must sit side by side.

```bash
dotnet build -f net10.0
dotnet build -f net10.0-android
dotnet test --project ../../tests/Klyroo.Dashboard.Maui.Tests
```
