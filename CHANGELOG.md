# Changelog

All notable changes to this project are documented in this file. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Filters in the URL: `DashboardController.ToQuery(prefix)` writes the filters that differ from their defaults as a deterministic, percent-encoded query string, `ApplyQuery(query, prefix)` reads one back (a value that does not fit its filter is ignored, a filter missing from the URL returns to its default, headless and disabled filters are never touched) and `MergeQuery(existing, prefix)` replaces the controller's keys in a query and keeps the others. `KlyDashboardPage.SyncUrl` (and `UrlPrefix`) applies the query on init, also in static SSR and in a prerender, writes later changes with a replacing navigation, and follows navigations to the same page. Apply the query before `DashboardPersistence.Attach` so the filter fingerprint of the prerender and of the interactive render match; see the guide.
- Drill-down: `KlyDashboardBars.OnSelect` (a `DashboardBarItem`), `KlyDashboardTable.OnRowClick` (the row dictionary) and `KlyDashboardList.OnRowClick` (a `DashboardListItem`), next to `KlyDashboardChart.OnSelect`. Each pick is a focusable `<button>`: the whole bar, the first cell of a table row, the label of a list row (a click on the row works too, and a row with an `Href` keeps its link).
- `KlyDashboardChart` is drawn by `KlyChart` of Klyroo.UI: value and category axes, gridlines, a tooltip that follows the pointer and the arrow keys, a data table for screen readers, and a scale that includes zero. New parameters: `Type="area"`, `Stacked`, `Horizontal` and `OnSelect`. `OnSelect` receives a `DashboardChartSelection` (category, series key, value) and shows a group of buttons under the drawing, so the choice works with the keyboard. A bar chart with a `line` series and a chart with `ShowLabels` keep a compact drawing of the block's own, because `KlyChart` draws one kind per chart and has no value labels.
- The built-in texts follow the Klyroo.UI locale, as in Klyroo.Display: the new `DashboardTexts` record holds every button, accessible name, empty and error state, table column header and relative time, `DashboardLocales` has one set per locale of Klyroo.UI (English and French reviewed, the other ten drafts), and `KlyrooDashboardOptions.Texts(code, change)` changes a set in `AddKlyrooDashboard`. `KlyrooDashboardOptions` is now stored as an extension of `KlyrooOptions`, and the container resolves that same instance.
- `DashboardFormat.FormatRelativeTime(value, now, DashboardRelativeTimeTexts)` writes the relative time with the words you give it. The overload without them stays English.
- Download CSV saves the file in the browser through `_content/Klyroo.Dashboard/klyroo-dashboard.js` when `KlyDashboardCard.OnCsvExported` has no handler, so the button works on the ready-made blocks.
- `KlyDashboardStat`, `KlyDashboardGauge`, `KlyDashboardFeed` and `KlyDashboardAlerts` build a table, so their menu has View as table and Download CSV.
- `KlyDashboardChart` draws every series: grouped bars, lines (`Type="line"`, or a `line` series over bars) and a donut (`Type="donut"`), in the `--klyroo-chart-*` colors, with a text alternative for screen readers.
- `KlyDashboardGrid.Columns` sets the number of grid columns, and block spans are clamped to it.
- `KlyDashboardGauge.Variant="bar"` draws a horizontal meter with a target marker. The default variant is now named `ring`; `arc` and any other value still draw the ring.
- `DashboardDetailsItem.Copy` adds a button that copies the value with `navigator.clipboard.writeText`.
- `DashboardController.AutoRefresh` refreshes the controller every given number of seconds. `DashboardController` is now `IDisposable` (it stops the timer and disposes its resources) and takes an optional `TimeProvider`.
- Loaders receive a real `CancellationToken`: a refresh cancels the load it supersedes and ignores its result, and so do disabling the resource, closing its `requires` gate, `SetData`, `SetError` and disposal. `DashboardResource<T>` is now `IDisposable`.
- The toolbar of `KlyDashboardPage` draws a control per `DashboardFilterKind`: text, number and date boxes, two date boxes for a range, built-in options for `Boolean` and `Comparison` filters (`DashboardFilterControl.BooleanOptions` and `ComparisonOptions`), and the new `CustomFilter` template for `Custom` filters.
- The option menu shows `DashboardOption.Description` and `Icon`, and preset buttons show `DashboardFilterPreset.Icon`.
- `DashboardFilterControl.SetValues` replaces the selection, and `DashboardFormat.FormatInvariant` writes a number as the exported tables carry it.
- `KlyDashboardAlerts.OnAction` handles the click on an alert action that has no `ActionHref`.
- `DashboardSkeletonKind.Paired` and `Stack` have their own skeleton shapes.
- Prerendered data is handed to the interactive render: a `DashboardResource` created with a `json` type info implements the new `IDashboardPersistentSource` (`ExportJson`, `RestoreJson`, `Restored`), the new scoped `DashboardPersistence.Attach(controller, renderMode)` writes the ready resources into the persistent component state at the end of the prerender and restores them in the interactive render, and the new `DashboardController.LoadAsync` loads a page like `RefreshAsync` but keeps restored data. The data goes through the app's `JsonTypeInfo`, never reflection, carries the filter values it was loaded with (other values refuse it), is dropped past `KlyrooDashboardOptions.PersistMaxAge` (5 minutes), and stays within `KlyrooDashboardOptions.PersistMaxBytes` (8 KB per resource, counted as sent) and the budget every Klyroo library of the page shares, `KlyrooOptions.PersistedStateMaxBytes` (28 KB). `AddKlyrooDashboard` takes an optional `Action<KlyrooDashboardOptions>`.

### Fixed

- Expand on a card opens an accessible modal dialog instead of turning the card into a `fixed` div with no focus handling: it reuses `KlyModal` of Klyroo.UI, so the page behind is inert, the focus is kept inside, Escape, a click on the backdrop and the close button close it, the focus returns to the Expand button, and the dialog is named by the title of the block. The Expand button has `aria-haspopup="dialog"`.
- Other blocks no longer misrepresent zero and negative values. `KlyDashboardBars` and the `bar` column of `KlyDashboardTable` drew every bar at least 2% wide and treated a largest value under 1 as 1, so a zero looked small but positive and a list of ratios was drawn too short; bars are now proportional to their value on a scale that includes zero, and a negative value extends to the left of a zero line. A `KlyDashboardGauge` ring at zero no longer leaves a dot from its round cap. A change of zero in a list, a table or a stats block is muted instead of red or green.
- A chart value of zero or below no longer looks positive: the old drawing clamped every bar to at least 4% of the height and drew a negative value as a zero, a missing value as a zero, and a line from a baseline that ignored the sign. Bars start at zero, negative values go below it, a missing or non-finite value is a gap, and a donut leaves out zero and negative values. The totals keep negative values.
- In a `;`-delimited CSV (decimal-comma cultures), numbers are written with a decimal comma, so the spreadsheet reads them as numbers.
- Setting `DashboardController.AutoRefresh` after the controller is disposed does nothing instead of throwing, and a refresh that throws no longer stops the timer. Intervals above `MaxAutoRefreshSeconds` (one day) are refused.
- A text, number or date filter box shows the restored default after it is cleared, and a gauge with a `NaN` value renders empty.
- `DashboardFormat.ExportCsv` wrote numeric cells unquoted even when they contained the delimiter, so a formatted `1,234.5` split into two columns. Such cells are now quoted.
- `DashboardFormat.ExportCsv` wrote cells starting with `=`, `+`, `-`, `@`, a tab or a carriage return as they were, so a spreadsheet could run them as formulas. They now get a leading apostrophe. The header line also ends with `\r\n` like the data lines, instead of the platform newline.
- Table views and CSV exports held the formatted text of numbers (`12.6K`, `+14.2%`) and every item even when `Limit` hid some. They now hold raw invariant numbers and only the rows the block shows, in `KlyDashboardTable`, `KlyDashboardList` and `KlyDashboardBars`.
- `DashboardFormat.FormatCompact` chose the suffix before rounding, so `999,999` gave `1000K`. It now gives `1M`.
- A resource disabled at first stayed `Disabled` after its `enabled` predicate turned true, because `ActivateAsync` only loaded idle resources and cards only activated Deferred ones once. `ActivateAsync` now loads it, and a card asks again on every render. A resource that stays disabled no longer raises `Changed` on every activation or refresh.
- Setting `DashboardViewMeta.Badge` or `Enabled`, or `DashboardFilterControl.Enabled` or `Items`, did not re-render `KlyDashboardPage`. They now raise the controller's `Changed`, and disabling a filter that had a selection raises its `ChangedEvent`.
- Disabling the current view left `CurrentView` on it. The next enabled view becomes current, and enabling a view when none is current selects it.
- `DashboardFilterControl.ApplyPreset` kept every preset value in single-select mode and ignored `Max`. It now keeps one value (two for a date range) and at most `Max` values.
- An alert with an `ActionLabel` and no `ActionHref` rendered a link to `#`. It now renders nothing, or a button when `OnAction` has a handler.
- `KlyDashboardStats` showed a missing value as `–` instead of `DashboardFormat.MissingValue`.

## [0.1.0-preview.1] - 2026-11-24

### Added

- `DashboardController` and `DashboardResource<T>`: queries with their own state (idle, loading, ready, error, disabled), loaded in an essential stage first, then a background stage, with deferred resources loaded on demand.
- `DashboardFilterControl` and named views: single and multiple selection with a maximum, presets, default values, reset detection, and view tabs with badges.
- Blazor components: `KlyDashboardPage`, `KlyDashboardGrid` and `KlyDashboardCard`, and ten blocks (`KlyDashboardStat`, `KlyDashboardStats`, `KlyDashboardGauge`, `KlyDashboardBars` with a funnel variant, `KlyDashboardChart`, `KlyDashboardList`, `KlyDashboardTable`, `KlyDashboardDetails`, `KlyDashboardFeed`, `KlyDashboardAlerts`), each with skeleton, error with retry and empty states.
- Comparisons: stat deltas computed from a previous value as a percent change or a difference in points, and `DashboardFormat.ResolveComparisonRange` for the previous period or the same dates one year earlier.
- `DashboardFormat`: nine number presets, compact and currency formatting, relative times and file name slugs, with an explicit culture.
- CSV export of a block through `DashboardFormat.ExportCsv`, with a UTF-8 byte order mark and a culture-dependent delimiter.
- `Klyroo.Dashboard.Core` without a UI dependency, declared trimmable and AOT compatible, and `Klyroo.Dashboard` for Blazor Web Apps and .NET MAUI Blazor Hybrid.

[Unreleased]: https://github.com/klyroo/klyroo-dashboard/compare/v0.1.0-preview.1...HEAD
[0.1.0-preview.1]: https://github.com/klyroo/klyroo-dashboard/releases/tag/v0.1.0-preview.1
