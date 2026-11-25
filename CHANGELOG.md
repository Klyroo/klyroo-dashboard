# Changelog

All notable changes to this project are documented in this file. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
