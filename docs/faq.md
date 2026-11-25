# FAQ

## Troubleshooting

**A block shows a skeleton and never fills**

The resource is still `Idle`: nothing has loaded it. Call `controller.RefreshAsync()` (or `resource.RefreshAsync()`) once the resources are registered. Two other causes keep a resource idle on purpose. A Deferred resource waits until something activates it, and a resource whose `requires` predicate returns `false` stays `Idle` when it has no default value. Pass a `defaultValue` to the constructor to make it `Ready` instead.

**A block does not appear at all**

Its source is `Disabled`: the `enabled` predicate returned `false`, and a card bound to a disabled source renders nothing. Fix the predicate. A resource that becomes enabled later stays `Disabled` until the next `RefreshAsync`.

**Changing a filter does not change the data**

Filters and resources are independent. The loader reads `filter.Value` when it runs, but nothing runs it when the filter changes. Subscribe to `filter.ChangedEvent` and call `RefreshAsync`, as in [Reload when a filter changes](guides.md#reload-when-a-filter-changes).

**Tabs, filter pills, Refresh and the table sort do nothing**

The page is rendered with Static SSR, where `@onclick` handlers cannot run. Add an interactive render mode to the page, for example `@rendermode InteractiveServer`. See [Platforms](platforms.md#blazor-render-modes).

**The view tabs switch but the content stays the same**

`KlyDashboardPage` only draws the tabs and updates `Dashboard.CurrentView`. It does not hide or show blocks. Branch on `CurrentView` in your markup, as in [Load a block only when its tab opens](guides.md#load-a-block-only-when-its-tab-opens).

**The tabs do not show**

They only render when the controller has at least two enabled views and `ShowTabs` is true. A single view gives no tab bar.

**Download CSV does nothing**

The button passes the CSV text to `KlyDashboardCard.OnCsvExported`, and only `KlyDashboardCard` has that parameter. The ready-made blocks (`KlyDashboardStats`, `KlyDashboardTable` and the others) do not expose it, so their button has no handler. Build the block on `KlyDashboardCard` as shown in [Export a block to CSV](guides.md#export-a-block-to-csv). Even then the card does not start a download: your handler receives the text and saves it.

**Numbers shift into the wrong column in an exported CSV**

`DashboardFormat.ExportCsv` quotes headers and text cells but writes the cells of `Numeric` columns as they are. A value formatted with group separators, such as `1,234.5`, contains the delimiter when the delimiter is a comma. Put unformatted numbers in `Numeric` columns, or leave the column non-numeric so it is quoted.

**The Error message shows an internal exception text**

A card shows `Source.Error`, which is the message of the exception thrown by the loader. Catch exceptions in the loader and throw one whose message is safe for users, as in [Handle failures and skip queries that cannot run](guides.md#handle-failures-and-skip-queries-that-cannot-run).

**The queries run twice on a page**

The page is prerendered and then rendered again when it becomes interactive, and the resources do not use persistent component state. Either accept two loads, cache in your data layer, or disable prerendering for the page. See [Platforms](platforms.md#blazor-render-modes).

**The block is unstyled or misaligned**

Load `_content/Klyroo.Dashboard/klyroo-dashboard.css` after the Klyroo.UI stylesheets. The dashboard stylesheet only contains the utilities used by the dashboard markup and relies on the `--klyroo-*` tokens that Klyroo.UI defines. See [Getting started](getting-started.md#5-load-the-stylesheets).

**A block does not update when its resource changes**

Only `KlyDashboardPage` subscribes to the controller. A block placed outside a page re-renders when its parent does, so call `StateHasChanged` (through `InvokeAsync` when you are off the renderer thread) in the parent when `controller.Changed` or `resource.Changed` fires.

**A parameter has no visible effect**

A few parameters are not read by the components yet. See the list under Known limitations.

## Known limitations

- **Chart.** `KlyDashboardChart` draws vertical bars for the first series only. `Type` (`bar`, `line`, `donut`) only chooses the loading skeleton, `DashboardChartSeries.Type` is not read, and series after the first appear in the legend, the totals and the table view but are not drawn. `IsComparison` only changes the legend marker and leaves the series out of the totals.
- **Reserved members.** These exist but nothing reads them: `KlyDashboardGrid.Columns` (the grid is always 12 columns), `KlyDashboardGauge.Variant`, `DashboardDetailsItem.Copy` (no copy button), `DashboardController.AutoRefresh`, `DashboardOption.Description` and `Icon`, `DashboardFilterPreset.Icon`, and `DashboardFilterControl.Kind` (the toolbar draws every filter the same way, as an option list).
- **Filters.** The toolbar draws every visible filter as a pill with a list of options and presets. There is no date, range, text or number input, even though `DashboardFilterKind` names them.
- **Charts and gauges.** The gauge is a ring, and `Target` is shown as text only.
- **Cancellation.** A loader always receives `CancellationToken.None`. Refreshes are not cancelled or serialized, and the last one to finish wins.
- **Language.** The texts of the blocks and `FormatRelativeTime` are English.
- **Number formats.** `DashboardFormat` uses the invariant culture unless you pass one. `FormatCompact(999_999)` gives `1000K`, because the value is rounded after the suffix is chosen.
- **Disabled to enabled.** `ActivateAsync` only loads a resource that is `Idle`, so a resource that was `Disabled` needs `RefreshAsync` once its predicate turns true.
- **CSV.** Numeric cells are not quoted, and the header line ends with the platform newline while data lines end with `\r\n`.
- **NativeAOT.** Only `Klyroo.Dashboard.Core` declares it. See [Platforms](platforms.md#nativeaot-and-trimming).

## Questions

**Can I use the controller without Blazor?**

Yes. `Klyroo.Dashboard.Core` has no UI dependency. Register resources, call `RefreshAsync` and read `Data`, `State` and `Error`. That is also how the library tests its own logic.

**How do I refresh automatically?**

Run a timer yourself and call `controller.RefreshAsync()`, as in [Refresh on a timer](guides.md#refresh-on-a-timer). Data already loaded stays visible during the refresh.

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
