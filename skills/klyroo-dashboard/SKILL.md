---
name: klyroo-dashboard
description: Klyroo Analytical Dashboard controller, staged queries, filters, formatters, and KPI blocks for .NET, Blazor, and MAUI.
---

# Klyroo.Dashboard Skill

- Use `Klyroo.Dashboard.Core` (`DashboardController`, `DashboardResource<T>`, `DashboardFilterControl`, `DashboardFormat`) for headless analytical state and formatting.
- Use `Klyroo.Dashboard` (`AddKlyrooDashboard()`, `KlyDashboardPage`, `KlyDashboardGrid`, `KlyDashboardCard`, `KlyDashboardStat`, `KlyDashboardTable`, `KlyDashboardChart`) for Blazor and MAUI Hybrid UIs.
- To avoid loading twice on a prerendered page, pass `json: AppJson.Default.MyData` to `DashboardResource<T>` call `DashboardPersistence.Attach(controller, AssignedRenderMode)` after registering the resources and setting the filters, then `await controller.LoadAsync()` (not `RefreshAsync`) for the first load.
