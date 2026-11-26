# FAQ

## Troubleshooting

**A block shows a skeleton and never fills**

The resource is still `Idle`: nothing has loaded it. Call `controller.RefreshAsync()` (or `resource.RefreshAsync()`) once the resources are registered. Two other causes keep a resource idle on purpose. A Deferred resource waits until something activates it, and a resource whose `requires` predicate returns `false` stays `Idle` when it has no default value. Pass a `defaultValue` to the constructor to make it `Ready` instead.

**A block does not appear at all**

Its source is `Disabled`: the `enabled` predicate returned `false`, and a card bound to a disabled source renders nothing. Fix the predicate. Once it returns `true`, the card loads the source on its next render, and `ActivateAsync` or `RefreshAsync` loads it too, so a resource that nothing renders or refreshes stays `Disabled`.

**Changing a filter does not change the data**

Filters and resources are independent. The loader reads `filter.Value` when it runs, but nothing runs it when the filter changes. Subscribe to `filter.ChangedEvent` and call `RefreshAsync`, as in [Reload when a filter changes](guides.md#reload-when-a-filter-changes).

**Tabs, filter pills, Refresh and the table sort do nothing**

The page is rendered with Static SSR, where `@onclick` handlers cannot run. Add an interactive render mode to the page, for example `@rendermode InteractiveServer`. See [Platforms](platforms.md#blazor-render-modes).

**The view tabs switch but the content stays the same**

`KlyDashboardPage` only draws the tabs and updates `Dashboard.CurrentView`. It does not hide or show blocks. Branch on `CurrentView` in your markup, as in [Load a block only when its tab opens](guides.md#load-a-block-only-when-its-tab-opens).

**The tabs do not show**

They only render when the controller has at least two enabled views and `ShowTabs` is true. A single view gives no tab bar.

**Download CSV does nothing in a .NET MAUI app**

The button starts a browser download, which the MAUI web view does not support. Handle `KlyDashboardCard.OnCsvExported` and save the text with the MAUI file APIs.

**The Error message shows an internal exception text**

A card shows `Source.Error`, which is the message of the exception thrown by the loader. Catch exceptions in the loader and throw one whose message is safe for users, as in [Handle failures and skip queries that cannot run](guides.md#handle-failures-and-skip-queries-that-cannot-run).

**The queries run twice on a page**

The page is prerendered and then rendered again when it becomes interactive, and each render loads the resources. Give the resources a `json` type info, attach the controller to `DashboardPersistence` and load with `LoadAsync`, as in [Hand the prerendered data to the interactive render](guides.md#hand-the-prerendered-data-to-the-interactive-render). If a resource still loads twice, its data is over `KlyrooDashboardOptions.PersistMaxBytes` or no longer fits in the budget the Klyroo libraries of the page share, `KlyrooOptions.PersistedStateMaxBytes` (a warning in the server log names it), it was registered after `Attach`, `Attach` got no render mode, a filter value differs between the two renders, or its loader was still running when the prerender ended. You can also disable prerendering for the page.

**The block is unstyled or misaligned**

Load `_content/Klyroo.Dashboard/klyroo-dashboard.css` after the Klyroo.UI stylesheets. The dashboard stylesheet only contains the utilities used by the dashboard markup and relies on the `--klyroo-*` tokens that Klyroo.UI defines. See [Getting started](getting-started.md#5-load-the-stylesheets).

**A block does not update when its resource changes**

Only `KlyDashboardPage` subscribes to the controller. A block placed outside a page re-renders when its parent does, so call `StateHasChanged` (through `InvokeAsync` when you are off the renderer thread) in the parent when `controller.Changed` or `resource.Changed` fires.

**A parameter has no visible effect**

String parameters such as `Variant`, `Type`, `Order`, `Emphasis`, `CompareMode` and the column `Type` fall back to their default when the value is unknown, so check the spelling. In a chart, a series `Type` only matters when the chart `Type` is `bar`.

**A filter does not appear in the toolbar**

The toolbar leaves out filters that are disabled or headless, and filters whose `Kind` is `Custom` when the page has no `CustomFilter` template. Give the page a `CustomFilter` to draw them, as in [Filter by text, number or date](guides.md#filter-by-text-number-or-date).

**The copy button of a details row does nothing**

It calls `navigator.clipboard.writeText`, which needs an interactive render mode and a secure context (HTTPS, or `localhost` during development). The browser can also refuse it without user permission; the button then does nothing and no error is shown.

**The automatic refresh keeps running after the user leaves the page**

`DashboardController.AutoRefresh` runs a timer until the controller is disposed. Implement `IDisposable` in the page that creates the controller and call `_dashboard.Dispose()`, as in [Refresh on a timer](guides.md#refresh-on-a-timer).

## Known limitations

- **Chart.** The chart block draws grouped bars, lines and a donut with HTML and SVG and no charting dependency. It has no axes, gridlines, zoom or interactive tooltips beyond the browser's `title` text, negative values are drawn as zero, and a donut shows only the first series that is not a comparison. `IsComparison` series are left out of the totals. For more, use a charting library inside a `KlyDashboardCard`.
- **Filters.** `Remote` filters are drawn like `Options`: the library does not fetch anything, so load the options and assign `Items` yourself, and the search box only filters the options already loaded. Text and number boxes apply their value when they lose focus or the user presses Enter.
- **Gauge.** The `ring` variant shows `Target` as text only; the `bar` variant also draws a marker.
- **Cancellation.** A refresh cancels the token of the load it supersedes and ignores its result, but a loader that does not pass the token on keeps running until it finishes.
- **Language.** The block texts come in the 12 locales of Klyroo.UI; only English and French are reviewed, the others are drafts. Text you pass in (titles, labels, filter options, the default `Yes`/`No` options of a `Boolean` filter and the options of a `Comparison` filter, which `Klyroo.Dashboard.Core` creates without a locale) is never translated, and `DashboardFormat.FormatRelativeTime` is English unless you pass `DashboardRelativeTimeTexts`.
- **Number formats.** `DashboardFormat` uses the invariant culture unless you pass one.
- **Persistence is opt-in and small.** Only resources with a `json` type info and a controller attached to `DashboardPersistence` hand their data from the prerender to the interactive render, at most 8 KB per resource by default, counted as sent, within the 28 KB per page every Klyroo library shares, because an Interactive Server circuit receives it in one SignalR message. A larger resource loads again once.
- **NativeAOT.** Only `Klyroo.Dashboard.Core` declares it. See [Platforms](platforms.md#nativeaot-and-trimming).

## Questions

**Can I use the controller without Blazor?**

Yes. `Klyroo.Dashboard.Core` has no UI dependency. Register resources, call `RefreshAsync` and read `Data`, `State` and `Error`. That is also how the library tests its own logic.

**How do I refresh automatically?**

Set `controller.AutoRefresh` to a number of seconds and dispose the controller with the page, as in [Refresh on a timer](guides.md#refresh-on-a-timer). Data already loaded stays visible during the refresh.

**Can several blocks share one resource?**

Yes. A resource is just a source and its data: give the same `Source` to several blocks and read different parts of `Data` for each.

**How do I show data without a controller?**

Leave `Source` unset. The block renders its content immediately, or its empty state when the items are empty, and you manage the data yourself.

**Does a resource keep its data when a refresh fails?**

Yes. `Data` keeps the last successful value, but the card replaces the content with the error and a Retry button, so the user does not see stale numbers next to an error.

**What does the page do with `UpdatedAt`?**

When `Description` is not set, the page header shows `Updated ... ago` from the oldest `UpdatedAt` among the resources. Blocks show their own `Updated` footer when `Freshness` is true.

**Which stylesheet mode does it support?**

The documented setup loads the precompiled `klyroo-dashboard.css` after the Klyroo.UI stylesheets. The package has no build integration for Tailwind compiled by your own app, and this documentation does not cover that case.
