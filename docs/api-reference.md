# API Reference — Lux.Dashboard

Complete public API reference extracted directly from the source code of `Lux.Dashboard.Core` and `Lux.Dashboard`.

## Razor Components

### `<LxDashboardAlerts>`
*Package*: `Lux.Dashboard` · *Source*: `src/Lux.Dashboard/LxDashboardAlerts.razor`

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Source` | `IDashboardSource?` | — |
| `Title` | `string?` | — |
| `Subtitle` | `string?` | — |
| `Items` | `IReadOnlyList<DashboardAlertItem>?` | — |
| `Order` | `string` | — |
| `Limit` | `int?` | — |
| `Span` | `int` | — |
| `Card` | `bool` | — |
| `Menu` | `bool?` | — |
| `Freshness` | `bool?` | — |

### `<LxDashboardBars>`
*Package*: `Lux.Dashboard` · *Source*: `src/Lux.Dashboard/LxDashboardBars.razor`

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Source` | `IDashboardSource?` | — |
| `Title` | `string?` | — |
| `Subtitle` | `string?` | — |
| `Items` | `IReadOnlyList<DashboardBarItem>?` | — |
| `Format` | `DashboardFormatPreset` | — |
| `Max` | `double?` | — |
| `Limit` | `int?` | — |
| `Variant` | `string` | — |
| `Emphasis` | `string` | — |
| `Span` | `int` | — |
| `Card` | `bool` | — |
| `Menu` | `bool?` | — |
| `Freshness` | `bool?` | — |

### `<LxDashboardCard>`
*Package*: `Lux.Dashboard` · *Source*: `src/Lux.Dashboard/LxDashboardCard.razor`

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Source` | `IDashboardSource?` | — |
| `Title` | `string?` | — |
| `Subtitle` | `string?` | — |
| `Span` | `int` | — |
| `Card` | `bool` | — |
| `IsEmpty` | `bool` | — |
| `EmptyTitle` | `string?` | — |
| `EmptyDescription` | `string?` | — |
| `Skeleton` | `DashboardSkeletonKind` | — |
| `Menu` | `bool?` | — |
| `Freshness` | `bool?` | — |
| `ViewAsTable` | `bool` | — |
| `Expandable` | `bool` | — |
| `Tabulate` | `Func<DashboardDataTable>?` | — |
| `Filters` | `IReadOnlyList<DashboardFilterControl>?` | — |
| `OnCsvExported` | `EventCallback<string>` | — |
| `HeaderRight` | `RenderFragment?` | — |
| `Footer` | `RenderFragment?` | — |
| `ChildContent` | `RenderFragment?` | — |

### `<LxDashboardChart>`
*Package*: `Lux.Dashboard` · *Source*: `src/Lux.Dashboard/LxDashboardChart.razor`

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Source` | `IDashboardSource?` | — |
| `Title` | `string?` | — |
| `Subtitle` | `string?` | — |
| `Type` | `string` | — |
| `Categories` | `IReadOnlyList<string>?` | — |
| `Series` | `IReadOnlyList<DashboardChartSeries>?` | — |
| `Format` | `DashboardFormatPreset` | — |
| `Height` | `int` | — |
| `ShowLegend` | `bool` | — |
| `ShowLabels` | `bool` | — |
| `Totals` | `string?` | — |
| `Span` | `int` | — |
| `Card` | `bool` | — |
| `Menu` | `bool?` | — |
| `Freshness` | `bool?` | — |

### `<LxDashboardDetails>`
*Package*: `Lux.Dashboard` · *Source*: `src/Lux.Dashboard/LxDashboardDetails.razor`

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Source` | `IDashboardSource?` | — |
| `Title` | `string?` | — |
| `Subtitle` | `string?` | — |
| `Items` | `IReadOnlyList<DashboardDetailsItem>?` | — |
| `Span` | `int` | — |
| `Card` | `bool` | — |
| `Menu` | `bool?` | — |
| `Freshness` | `bool?` | — |

### `<LxDashboardFeed>`
*Package*: `Lux.Dashboard` · *Source*: `src/Lux.Dashboard/LxDashboardFeed.razor`

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Source` | `IDashboardSource?` | — |
| `Title` | `string?` | — |
| `Subtitle` | `string?` | — |
| `Items` | `IReadOnlyList<DashboardFeedItem>?` | — |
| `GroupByDay` | `bool` | — |
| `Now` | `DateTimeOffset?` | — |
| `Limit` | `int?` | — |
| `Span` | `int` | — |
| `Card` | `bool` | — |
| `Menu` | `bool?` | — |
| `Freshness` | `bool?` | — |

### `<LxDashboardGauge>`
*Package*: `Lux.Dashboard` · *Source*: `src/Lux.Dashboard/LxDashboardGauge.razor`

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Source` | `IDashboardSource?` | — |
| `Title` | `string?` | — |
| `Subtitle` | `string?` | — |
| `Value` | `double?` | — |
| `Min` | `double` | — |
| `Max` | `double` | — |
| `Target` | `double?` | — |
| `Format` | `DashboardFormatPreset` | — |
| `Caption` | `string?` | — |
| `Variant` | `string` | — |
| `Span` | `int` | — |
| `Card` | `bool` | — |
| `Menu` | `bool?` | — |
| `Freshness` | `bool?` | — |

### `<LxDashboardGrid>`
*Package*: `Lux.Dashboard` · *Source*: `src/Lux.Dashboard/LxDashboardGrid.razor`

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Columns` | `int` | — |
| `Variant` | `string` | — |
| `Menu` | `bool` | — |
| `Freshness` | `bool` | — |
| `ChildContent` | `RenderFragment?` | — |

### `<LxDashboardList>`
*Package*: `Lux.Dashboard` · *Source*: `src/Lux.Dashboard/LxDashboardList.razor`

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Source` | `IDashboardSource?` | — |
| `Title` | `string?` | — |
| `Subtitle` | `string?` | — |
| `Items` | `IReadOnlyList<DashboardListItem>?` | — |
| `Format` | `DashboardFormatPreset` | — |
| `Limit` | `int?` | — |
| `Span` | `int` | — |
| `Card` | `bool` | — |
| `Menu` | `bool?` | — |
| `Freshness` | `bool?` | — |

### `<LxDashboardPage>`
*Package*: `Lux.Dashboard` · *Source*: `src/Lux.Dashboard/LxDashboardPage.razor`

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Dashboard` | `DashboardController?` | — |
| `Title` | `string?` | — |
| `Description` | `string?` | — |
| `ShowRefresh` | `bool` | — |
| `ShowTabs` | `bool` | — |
| `ShowFilters` | `bool` | — |
| `Sticky` | `bool` | — |
| `Actions` | `RenderFragment?` | — |
| `ChildContent` | `RenderFragment?` | — |

### `<LxDashboardStat>`
*Package*: `Lux.Dashboard` · *Source*: `src/Lux.Dashboard/LxDashboardStat.razor`

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Source` | `IDashboardSource?` | — |
| `Label` | `string` | — |
| `Value` | `double?` | — |
| `TextValue` | `string?` | — |
| `Format` | `DashboardFormatPreset` | — |
| `Delta` | `double?` | — |
| `DeltaFormat` | `DashboardFormatPreset?` | — |
| `InvertDelta` | `bool` | — |
| `Compare` | `double?` | — |
| `CompareMode` | `string` | — |
| `Caption` | `string?` | — |
| `Trend` | `IReadOnlyList<double>?` | — |
| `Goal` | `double?` | — |
| `Status` | `DashboardStatus?` | — |
| `Span` | `int` | — |
| `Card` | `bool` | — |
| `Menu` | `bool?` | — |
| `Freshness` | `bool?` | — |

### `<LxDashboardStats>`
*Package*: `Lux.Dashboard` · *Source*: `src/Lux.Dashboard/LxDashboardStats.razor`

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Source` | `IDashboardSource?` | — |
| `Title` | `string?` | — |
| `Subtitle` | `string?` | — |
| `Items` | `IReadOnlyList<DashboardStatsItem>?` | — |
| `Variant` | `string` | — |
| `Span` | `int` | — |
| `Card` | `bool` | — |
| `Menu` | `bool?` | — |
| `Freshness` | `bool?` | — |

### `<LxDashboardTable>`
*Package*: `Lux.Dashboard` · *Source*: `src/Lux.Dashboard/LxDashboardTable.razor`

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Source` | `IDashboardSource?` | — |
| `Title` | `string?` | — |
| `Subtitle` | `string?` | — |
| `Columns` | `IReadOnlyList<DashboardTableColumn>?` | — |
| `Rows` | `IReadOnlyList<IReadOnlyDictionary<string, object?>>?` | — |
| `Sort` | `DashboardTableSort?` | — |
| `SortChanged` | `EventCallback<DashboardTableSort?>` | — |
| `Limit` | `int?` | — |
| `Span` | `int` | — |
| `Card` | `bool` | — |
| `Menu` | `bool?` | — |
| `Freshness` | `bool?` | — |

## C# Types, Controllers, Models & Enums

### `enum DashboardResourceState`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

Lifecycle state of a dashboard query or derived resource.

| Member | Type | Description |
| :--- | :--- | :--- |
| `Idle` | `enum member` | — |
| `Loading` | `enum member` | — |
| `Ready` | `enum member` | — |
| `Error` | `enum member` | — |
| `Disabled` | `enum member` | — |

### `enum DashboardQueryStage`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

Execution stage of a dashboard query.

| Member | Type | Description |
| :--- | :--- | :--- |
| `Essential` | `enum member` | — |
| `Background` | `enum member` | — |
| `Deferred` | `enum member` | — |

### `enum DashboardBlockPhase`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

Block rendering phase determined by `LxDashboardCard`.

| Member | Type | Description |
| :--- | :--- | :--- |
| `Idle` | `enum member` | — |
| `Loading` | `enum member` | — |
| `Error` | `enum member` | — |
| `Empty` | `enum member` | — |
| `Content` | `enum member` | — |

### `enum DashboardSkeletonKind`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

Skeleton geometry kind rendered while a `LxDashboardCard` is loading.

| Member | Type | Description |
| :--- | :--- | :--- |
| `Rows` | `enum member` | — |
| `Stat` | `enum member` | — |
| `Stats` | `enum member` | — |
| `Gauge` | `enum member` | — |
| `Details` | `enum member` | — |
| `Bars` | `enum member` | — |
| `Paired` | `enum member` | — |
| `Funnel` | `enum member` | — |
| `Stack` | `enum member` | — |
| `Table` | `enum member` | — |
| `Feed` | `enum member` | — |
| `Lines` | `enum member` | — |
| `Donut` | `enum member` | — |

### `enum DashboardFormatPreset`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

Preset number formats supported by `DashboardFormat`.

| Member | Type | Description |
| :--- | :--- | :--- |
| `Number` | `enum member` | — |
| `Integer` | `enum member` | — |
| `Decimal` | `enum member` | — |
| `Compact` | `enum member` | — |
| `Percent` | `enum member` | — |
| `Ratio` | `enum member` | — |
| `Delta` | `enum member` | — |
| `Points` | `enum member` | — |
| `Signed` | `enum member` | — |

### `enum DashboardComparisonMode`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

Comparison period mode for date ranges and KPI comparisons.

| Member | Type | Description |
| :--- | :--- | :--- |
| `None` | `enum member` | — |
| `Previous` | `enum member` | — |
| `Year` | `enum member` | — |

### `enum DashboardFilterKind`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

Filter control kind in a `DashboardController`.

| Member | Type | Description |
| :--- | :--- | :--- |
| `String` | `enum member` | — |
| `Number` | `enum member` | — |
| `Boolean` | `enum member` | — |
| `Date` | `enum member` | — |
| `DateRange` | `enum member` | — |
| `Enum` | `enum member` | — |
| `Options` | `enum member` | — |
| `Remote` | `enum member` | — |
| `Comparison` | `enum member` | — |
| `Custom` | `enum member` | — |

### `enum DashboardAlertSeverity`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

Alert severity level for `LxDashboardAlerts`.

| Member | Type | Description |
| :--- | :--- | :--- |
| `Error` | `enum member` | — |
| `Warning` | `enum member` | — |
| `Info` | `enum member` | — |
| `Success` | `enum member` | — |

### `interface IDashboardSource`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

Common interface implemented by dashboard queries and derived resources.

### `class DashboardResource<TData>`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

A staged, optionally gated asynchronous data resource managed by a `DashboardController`.

| Member | Type | Description |
| :--- | :--- | :--- |
| `Key` | `string` | — |
| `Stage` | `DashboardQueryStage` | — |
| `DefaultValue` | `TData?` | — |
| `Data` | `TData?` | — |
| `State` | `DashboardResourceState` | — |
| `Error` | `string?` | — |
| `Refreshing` | `bool` | — |
| `Fetching` | `bool` | — |
| `UpdatedAt` | `DateTimeOffset?` | — |
| `Active` | `bool` | — |
| `SetData(TData? data)` | `void` | Sets the resource data directly and transitions to `DashboardResourceState.Ready`. |
| `SetError(string message)` | `void` | Transitions the resource to `DashboardResourceState.Error`. |
| `ActivateAsync()` | `Task` | — |
| `RefreshAsync()` | `Task` | — |

### `record DashboardOption`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

An option item in a `DashboardFilterControl`.

### `record DashboardFilterPreset`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

A preset value in a `DashboardFilterControl`.

### `class DashboardFilterControl`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

Interactive filter control state managed by `DashboardController`.

| Member | Type | Description |
| :--- | :--- | :--- |
| `Key` | `string` | — |
| `Label` | `string` | — |
| `Kind` | `DashboardFilterKind` | — |
| `Multiple` | `bool` | — |
| `Searchable` | `bool` | — |
| `Max` | `int?` | — |
| `Placeholder` | `string` | — |
| `Headless` | `bool` | — |
| `Enabled` | `bool` | — |
| `Items` | `IReadOnlyList<DashboardOption>` | — |
| `Presets` | `IReadOnlyList<DashboardFilterPreset>` | — |
| `Search` | `string` | — |
| `Open` | `bool` | — |
| `Values` | `IReadOnlyList<string>` | — |
| `Value` | `string?` | — |
| `Changed` | `bool` | — |
| `Display` | `string` | — |
| `FilteredItems` | `IReadOnlyList<DashboardOption>` | — |
| `IsSelected(string value)` | `bool` | — |
| `Toggle(string value)` | `void` | — |
| `ApplyPreset(DashboardFilterPreset preset)` | `void` | — |
| `Reset()` | `void` | — |

### `class DashboardViewMeta`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

Metadata for a named dashboard view tab.

| Member | Type | Description |
| :--- | :--- | :--- |
| `Key` | `string` | — |
| `Label` | `string` | — |
| `Badge` | `string?` | — |
| `Enabled` | `bool` | — |
| `Opened` | `bool` | — |

### `class DashboardController`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

Central controller for an analytical dashboard: coordinates filters, views, staged resources (`DashboardQueryStage.Essential`, `DashboardQueryStage.Background`, `DashboardQueryStage.Deferred`), refresh and auto-refresh state.

| Member | Type | Description |
| :--- | :--- | :--- |
| `Key` | `string` | — |
| `Controls` | `IReadOnlyList<DashboardFilterControl>` | — |
| `Views` | `IReadOnlyList<DashboardViewMeta>` | — |
| `Resources` | `IReadOnlyList<IDashboardSource>` | — |
| `AutoRefresh` | `int` | — |
| `CurrentView` | `string?` | — |
| `Filtered` | `bool` | — |
| `Refreshing` | `bool` | — |
| `UpdatedAt` | `DateTimeOffset?` | — |
| `State` | `DashboardResourceState` | — |
| `AddFilter(DashboardFilterControl filter)` | `DashboardFilterControl` | — |
| `AddView(string key, string label, string? badge = null, bool enabled = true)` | `DashboardViewMeta` | — |
| `RegisterResource<TData>(DashboardResource<TData> resource)` | `DashboardResource<TData>` | — |
| `ResetFilters()` | `void` | — |
| `RefreshAsync()` | `Task` | Runs staged queries in order: all `DashboardQueryStage.Essential` resources first, then `DashboardQueryStage.Background` resources once essential ones settle, plus any already active `DashboardQueryStage.Deferred` resources. |
| `CombineStates(IEnumerable<DashboardResourceState> states)` | `DashboardResourceState` | Combines multiple resource states according to the dashboard state precedence rules. |

### `record DashboardDataColumn`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

Tabular view and CSV export representation of a dashboard block.

### `record DashboardDataTable`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

Tabular representation of a dashboard block used by "View as table" and "Download CSV".

### `class DashboardFormat`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

Locale-aware number, delta, relative-time, comparison-range, and CSV formatting utilities for the Dashboard domain.

| Member | Type | Description |
| :--- | :--- | :--- |
| `Format(double? value, DashboardFormatPreset preset = DashboardFormatPreset.Number, CultureInfo? culture = null)` | `string` | — |
| `FormatCompact(double? value, CultureInfo? culture = null)` | `string` | — |
| `FormatCurrency(double? value, string currencySymbol = "€", CultureInfo? culture = null)` | `string` | — |
| `FormatRelativeTime(DateTimeOffset? value, DateTimeOffset? now = null)` | `string` | — |
| `ResolveComparisonRange(
        (DateTimeOffset Start, DateTimeOffset End)` | `(DateTimeOffset Start, DateTimeOffset End)?` | Computes the comparison date range for `DashboardComparisonMode.Previous` or `DashboardComparisonMode.Year`. |
| `ResolveFileName(string? title, string extension = "csv")` | `string` | Normalizes a block title into a slug suitable for a CSV download filename. |
| `ExportCsv(DashboardDataTable table, CultureInfo? culture = null)` | `string` | Serializes a `DashboardDataTable` to CSV with a UTF-8 BOM and locale-appropriate delimiter. |

### `record DashboardStatus`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

Status pill shown on a `LxDashboardStat` or `DashboardStatsItem`.

### `record DashboardStatsItem`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

One metric entry inside `LxDashboardStats`.

### `record DashboardDetailsItem`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

One definition row inside `LxDashboardDetails`.

### `record DashboardBarItem`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

One horizontal bar entry in `LxDashboardBars`.

### `record DashboardListItem`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

One ranked row in `LxDashboardList`.

### `record DashboardTableColumn`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

Column definition for `LxDashboardTable`.

### `record DashboardTableSort`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

Active sort state of `LxDashboardTable`.

### `record DashboardAlertItem`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

One alert row in `LxDashboardAlerts`.

### `record DashboardFeedItem`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

One timeline activity event in `LxDashboardFeed`.

### `record DashboardChartSeries`
*Package*: `Lux.Dashboard.Core` · *Source*: `src/Lux.Dashboard.Core/DashboardModels.cs`

One series definition for `LxDashboardChart`.

### `class LuxDashboardServiceCollectionExtensions`
*Package*: `Lux.Dashboard` · *Source*: `src/Lux.Dashboard/LuxDashboardServiceCollectionExtensions.cs`

| Member | Type | Description |
| :--- | :--- | :--- |
| `AddLuxDashboard(this IServiceCollection services)` | `IServiceCollection` | — |
