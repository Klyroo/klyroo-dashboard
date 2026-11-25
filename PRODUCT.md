# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

- **Primary**: .NET 10 / Blazor and `.NET MAUI Blazor Hybrid` developers building analytical dashboards, executive KPI workspaces, revenue/ops monitoring views, and filter-synchronized reporting screens.

## Product Purpose

`klyroo-dashboard` provides a headless NativeAOT 3-wave analytical query orchestrator (`Klyroo.Dashboard.Core`) paired with 13 KPI and visualization blocks (`Klyroo.Dashboard`) for Blazor Web and `.NET MAUI Blazor Hybrid`.

Success means a dashboard loads critical KPIs first (`Essential`), hydrates secondary visualizations asynchronously (`Background`), defers heavy resources until requested (`Deferred`), keeps global filters and views synchronized across widgets and `Klyroo.Agent`, and exports clean RFC 4180 UTF-8 BOM CSV files with zero bespoke state plumbing.

## Positioning

Unlike charting packs that only draw static SVGs and leave loading orchestration, filter synchronization, comparison date math, and CSV export to every application team, `Klyroo.Dashboard` unifies a 3-wave staged `DashboardController`, leap-year-safe date comparison ranges, compact/delta/ratio formatters, and 13 composable analytical blocks in a NativeAOT headless split under MIT.

## Operating Context

- **Packages**: `Klyroo.Dashboard.Core` (`DashboardController`, `DashboardFilterControl`, `DashboardResource<T>`, `DashboardQueryStage`, `DashboardFormat`, CSV export) and `Klyroo.Dashboard` (13 widgets: `KlyDashboardPage`, `Grid`, `Card`, `Stat`, `Stats`, `Gauge`, `Details`, `Bars`, `List`, `Table`, `Alerts`, `Feed`, `Chart`, plus `klyroo-dashboard.css`).
- **Integration**: Built on `Klyroo.UI` primitives and natively registrable in `Klyroo.Agent` (`AgentBridgeService.RegisterDashboard`).
- **Showcase**: Powers the `Overview` workspace in `Klyroo.Stack.Site` `/showcase` (Executive Metrics, Annual Target Attainment gauge, ARR by Region bars, Operational Alerts, and global Region Filter).

## Capabilities and Constraints

- `Klyroo.Dashboard.Core` is 100% headless with `<IsAotCompatible>true</IsAotCompatible>` and `<IsTrimmable>true</IsTrimmable>`.
- 3-wave staged execution (`Essential`, `Background`, `Deferred`) with cancellation and per-resource state tracking.
- `KlyDashboardCard` defaults to 12-column spans (`Span` parameter, `1–12`); callers placing cards inside custom Tailwind grids pass `Span="1"` for responsive column alignment.
- `DashboardStatsItem.Progress` uses a `0–100` percentage scale.
- Ships precompiled zero-friction CSS (`klyroo-dashboard.css`) alongside native Tailwind CSS v4 support.
- 100% MIT open-source license; strict `Klyroo.Dashboard*` / `KlyDashboard*` / `--klyroo-*` naming.

## Brand Commitments

- **Name**: `Klyroo.Dashboard` (`klyroo-dashboard`), part of the **Klyroo** ecosystem.
- **Voice**: Analytical, concise, and operationally grounded.

## Evidence on Hand

- Passing unit and bUnit test suite (`tests/Klyroo.Dashboard.Tests`) covering 3-wave orchestration, filters, leap-year date ranges, formatters, CSV export, and widgets, plus NativeAOT publish verification in CI.
- Live interactive dashboard in `Klyroo.Stack.Site` `/showcase` and dedicated documentation at `/dashboard`.
- Verified precompiled standalone CSS (`klyroo-dashboard.css`).
- **Explicit absences (do not fabricate)**: No NuGet download stats, customer logos, or third-party testimonials exist yet.

## Product Principles

1. **Progressive disclosure of data**: Load essential metrics first so operators see signal immediately instead of waiting on the slowest query.
2. **One filter source of truth**: Filters, views, resets, and agent commands mutate `DashboardController` once and propagate cleanly to every widget.
3. **Readable numbers by default**: Tabular numerals, consistent compact/delta/percent formatting, and clear positive/negative semantics prevent misreading.
4. **Honest states**: Skeletons, freshness indicators, empty states, and tabular export are first-class features of every dashboard card.

## Accessibility & Inclusion

- Durable standard: **WCAG 2.1 AA** across all 13 dashboard widgets: accessible `role="progressbar"` with `aria-valuenow`/`aria-valuemin`/`aria-valuemax`, text labels alongside color-coded deltas and alert severities, tabular export alternatives for visual charts, and keyboard-accessible filter/view controls.
