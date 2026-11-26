# Concepts

This page explains how Klyroo.Dashboard is put together and why. For task-oriented steps see the [guides](guides.md), for every member see the [API reference](api-reference.md).

## Two packages

The library is split in two so that the state of a dashboard can live and be tested without a UI.

```text
Klyroo.Dashboard.Core (no UI dependency)         Klyroo.Dashboard (Blazor, depends on Core and Klyroo.UI)

DashboardController  ---- you pass it --->       KlyDashboardPage     tabs, filter controls, refresh button
  |- DashboardFilterControl[]                      KlyDashboardGrid   12 columns by default, Menu and Freshness defaults
  |- DashboardViewMeta[]                             blocks           Stat, Stats, Gauge, Bars, Chart, List,
  `- IDashboardSource[]                                               Table, Details, Feed, Alerts
       `- DashboardResource<T>  -- Source -->          KlyDashboardCard   skeleton, error, empty, content
DashboardFormat (numbers, dates, CSV)
```

`Klyroo.Dashboard.Core` holds the controller, the resources, the filters, the views, the item records the blocks consume and `DashboardFormat`. It targets .NET 10 with no package dependency and declares `IsTrimmable` and `IsAotCompatible`, so a console app, a worker, a minimal API or a MAUI view model can use it.

`Klyroo.Dashboard` holds 13 Razor components: the page, the grid, the card and ten blocks. It builds on `Klyroo.UI` for the base component class (culture, `Class` and unmatched attributes), badges and skeletons.

## Resources and their lifecycle

A `DashboardResource<T>` is one query: a loader function, a stage, optional gates, and the state of the last load. It implements `IDashboardSource`, the only thing a block needs to know about it.

```text
                 RefreshAsync                loader returns
        Idle ------------------> Loading ----------------------> Ready <---+
                                    |                              |       | RefreshAsync:
                                    | loader throws                +-------+ data stays,
                                    v                                        Refreshing = true
                                  Error ---- RefreshAsync (Retry) ----> Loading

   any state ---- enabled() returns false ----> Disabled (Data = default value)
```

The state tells the story, and two flags refine it. `Fetching` is true whenever the loader runs. `Refreshing` is true only when data was already there, so a block can keep showing the old value while the new one loads. `UpdatedAt` records the last success, and `Error` holds the message of the last failure.

Two optional gates control whether the loader runs. `enabled` removes the resource: the state becomes `Disabled`, the data resets to the default value and a card bound to it is not rendered. `requires` skips one load while a precondition is missing: the data resets to the default value, and the state is `Ready` if there is a default value (always the case for value types) or `Idle` if there is none.

A resource without a loader is a plain holder: `RefreshAsync` marks it `Ready` and you feed it with `SetData` or `SetError`.

Only the latest load counts. Each call to `RefreshAsync` gives the loader a fresh `CancellationToken` and cancels the token of the load still running, whose result is then ignored even if its loader never looks at the token. Disabling the resource, closing its `requires` gate, calling `SetData` or `SetError` and disposing the resource cancel the running load the same way. A disabled resource leaves `Disabled` when its predicate returns `true` again and something activates or refreshes it; a card bound to it does that on every render.

## Stages and the controller

Every resource has a stage:

| Stage | When it loads |
| :--- | :--- |
| `Essential` | First, all together. This is the default. |
| `Background` | After every Essential resource has settled, together with the active Deferred resources. |
| `Deferred` | Not at all until activated, by `ActivateAsync` or by the first render of a card bound to it. After that it refreshes with the Background stage. |

`DashboardController.RefreshAsync` implements exactly that: one parallel wave of Essential resources, then one parallel wave of Background plus active Deferred. It never throws for a failing loader, because each resource catches its own exception. `AutoRefresh` calls it every given number of seconds, and disposing the controller stops that timer, cancels the loads in progress and makes later refreshes do nothing.

The controller also aggregates. `State` combines the Essential states only: no error means the page can be shown, an error means the headline numbers are missing. `Refreshing` is true while any resource loads, and `UpdatedAt` is the oldest successful update among the resources, which is the honest age of the dashboard. `Filtered` is true when any filter differs from its defaults.

## Filters and views

A `DashboardFilterControl` holds a selection of string values, the options it can pick from and the defaults it returns to. Single mode replaces the selection and cannot be cleared; multiple mode toggles values up to an optional maximum. Its `Kind` only decides which control the page draws (an option pill, a text, number or date box, two date boxes, or your own template); dates, numbers and booleans are stored as invariant strings, so every kind reads the same way. `Changed` means "differs from the defaults", which is what drives the reset button and the highlighted pill. A `Headless` filter holds a value that your code controls: the page does not draw it, `Changed` ignores it and `ResetFilters` leaves it alone.

A filter does not know about queries. Your loaders read `filter.Value` or `filter.Values` through closures, and you decide when to reload (see [Reload when a filter changes](guides.md#reload-when-a-filter-changes)). The controller forwards filter changes to its `Changed` event, which re-renders the page.

Views are named tabs. The controller tracks the current view and whether each one has been opened, and moves to the next enabled view when the current one is disabled. The page draws the tabs and sets `CurrentView`; what each view contains is your markup. Changing a view's `Badge` or `Enabled`, or a filter's `Enabled` or `Items`, raises the controller's `Changed`, so the page follows.

## Blocks and the card

Every block is a `KlyDashboardCard` with a particular content. The card derives a phase from the state of its `Source`:

| Source state | Card phase | What the user sees |
| :--- | :--- | :--- |
| Idle, Loading | Idle, Loading | A skeleton. |
| Error | Error | The error message and a Retry button that refreshes the source. |
| Ready, nothing to show | Empty | An empty state. |
| Ready | Content | The block. |
| Disabled | Idle | Nothing: the card is not rendered. |

Without a `Source` the card shows its content (or the empty state) immediately. This makes every block usable with plain data and no controller.

The source drives the state only. A block does not read `Data` from it: you pass the values through the block parameters. That costs a little repetition (`Source="_summary" Value="_summary.Data?.Orders"`) and buys flexibility: one source can feed several blocks, and a block can show data from anywhere.

Blocks sit on a grid of 12 columns by default (`KlyDashboardGrid.Columns`) and take a `Span`, clamped between 1 and the column count. The grid's `Variant` is `cards` (separate cards) or `panels` (one bordered panel with dividers). The grid also supplies the defaults of `Menu` (table, CSV and expand buttons) and `Freshness` (an `Updated ... ago` footer) to the blocks inside it.

Several parameters are strings rather than enums: `Variant`, `Type`, `Order`, `Emphasis`, `CompareMode`, `Totals` and the column `Type`. This keeps markup short, but an unknown value is silently treated as the default, so check spelling when something does not change.

## Re-rendering and threads

Only `KlyDashboardPage` subscribes to the controller. When a filter, a view or a resource changes it calls `StateHasChanged` through `InvokeAsync`, and the page's content is built again with the current values. Blocks used without a page do not update by themselves: call `StateHasChanged` from your component when the source changes.

The controller and the resources raise their events on the thread that made the change. In Blazor that is usually the renderer's context after an `await`, but a refresh started from a timer or another thread raises them there, which is why the page marshals with `InvokeAsync`. Nothing in the controller is thread-safe: keep one controller per page instance, not one per application. A controller registered as a singleton would share filters and data between users.

## Formatting and culture

`DashboardFormat` is static. Its methods default to the invariant culture, not the current one, so `Format(1234.5)` is `1,234.5` everywhere unless you pass a culture. The blocks pass the format culture of Klyroo.UI (the user's regional culture when it matches the active locale), so numbers inside blocks follow the user. Labels such as Refresh, View as table, Retry and the relative times (`5 min ago`) are English only.

## Error handling

A loader exception is caught by the resource, stored as `Error` (its message) and shown by the card. Nothing is logged and nothing is retried automatically. The message is shown to the user as is, so wrap infrastructure exceptions in a message that is safe to display. An exception thrown by a load that was superseded or cancelled is ignored, like its result: the latest load decides the state.

## Design decisions and trade-offs

- **State in Core, rendering in Blazor.** The lifecycle rules are plain C# that run in milliseconds in a unit test. The cost is that blocks and sources are two objects wired by hand.
- **Two stages and an opt-in third.** Most dashboards need "headline first, details after". A full dependency graph between queries would be more precise and a lot more to configure, so ordering is by stage only.
- **Latest load wins, no de-duplication.** A new refresh cancels the one running for the same resource rather than waiting for it or sharing its result, so the data always matches the latest filters, at the cost of restarting queries the user changed their mind about.
- **Charts without a charting library.** The chart block hands its series to `KlyChart` of Klyroo.UI, which draws bars, lines, areas and a donut as SVG on the server in the theme's chart colors, with axes and gridlines, a tooltip that follows the pointer and the arrow keys, a scale that always includes zero (a zero value has no bar, a negative one goes below the axis) and a data table for screen readers. There is no charting dependency and no JavaScript beyond the small custom element of Klyroo.UI. A bar chart that mixes in a `line` series, and a chart with value labels, use a compact drawing of the block's own, without axes.
- **Prerendered data is handed over on request.** A prerendered interactive page loads its data twice unless its resources have a `json` type info and the page attaches its controller to `DashboardPersistence` and loads with `LoadAsync`: the prerender then hands what it loaded to the interactive render, which keeps it instead of loading again. The data is written with the `JsonTypeInfo` of the app, never by reflection, and only up to a size limit (see [Platforms](platforms.md#blazor-render-modes)).
