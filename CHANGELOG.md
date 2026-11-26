# Changelog

All notable changes to this project are documented in this file. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

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

### Fixed

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
