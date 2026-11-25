# Klyroo.Dashboard API reference

> Generated from the compiled assemblies and their XML documentation. Do not edit by hand.

## Klyroo.Dashboard.Core

Static classes and extensions (1) · Classes (4) · Records (14) · Interfaces (1) · Enums (8)

### Static classes and extensions

#### `DashboardFormat`

`Klyroo.Dashboard` · service

Number, delta, relative-time, comparison-range and CSV helpers. Numbers use the invariant culture unless you pass one; the components pass the format culture of Klyroo.UI.

| Field | Type | Description |
| :--- | :--- | :--- |
| `const MissingValue = "—"` | `string` | The placeholder shown for a missing, `NaN` or infinite number: an em dash. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static ExportCsv(DashboardDataTable table, CultureInfo? culture = null)` | `string` | Serializes a table to CSV. The text starts with a UTF-8 byte order mark and uses `;` as delimiter when the culture has a comma as decimal separator, `,` otherwise. Headers and non-numeric cells are quoted. Cells of numeric columns are written as they are, so they must not contain the delimiter. |
| `static Format(double? value, DashboardFormatPreset preset = Number, CultureInfo? culture = null)` | `string` | Formats a number with a preset. A `null`, `NaN` or infinite value gives `DashboardFormat.MissingValue`. |
| `static FormatCompact(double? value, CultureInfo? culture = null)` | `string` | Formats a number with the `K`, `M` and `B` suffixes and at most one decimal. |
| `static FormatCurrency(double? value, string currencySymbol = "€", CultureInfo? culture = null)` | `string` | Formats an amount with the currency symbol in front and at most two decimals. The symbol position does not follow the culture. |
| `static FormatRelativeTime(DateTimeOffset? value, DateTimeOffset? now = null)` | `string` | Describes how long ago something happened: `just now` under 45 seconds, then minutes, hours, `yesterday`, days under a week, and an ISO date (`yyyy-MM-dd`) beyond that. The text is English. |
| `static ResolveComparisonRange((DateTimeOffset, DateTimeOffset)? range, DashboardComparisonMode comparison)` | `(DateTimeOffset, DateTimeOffset)?` | Computes the comparison range of a date range. `DashboardComparisonMode.Previous` gives the range of the same number of days that ends the day before the start. `DashboardComparisonMode.Year` shifts both bounds back one year, clamping February 29. |
| `static ResolveFileName(string? title, string extension = "csv")` | `string` | Turns a title into a file name: lowercase, accents removed, anything that is not a letter or digit collapsed into one hyphen. Falls back to `dashboard` when nothing is left. |

### Classes

#### `DashboardController`

`Klyroo.Dashboard` · class

Central controller for an analytical dashboard: coordinates filters, views, staged resources (`DashboardQueryStage.Essential`, `DashboardQueryStage.Background`, `DashboardQueryStage.Deferred`) and refresh. It has no UI dependency.

| Constructor | Description |
| :--- | :--- |
| `DashboardController(string key = "dashboard")` | Creates a controller. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `AutoRefresh` (get, set) | `int` | An auto-refresh interval for your own code to read. The controller only stores it and never schedules a refresh, and the components do not read it. |
| `Controls` | `IReadOnlyList<DashboardFilterControl>` | The filters, in the order they were added. |
| `CurrentView` (get, set) | `string?` | The key of the view being shown. Setting a key that is unknown or disabled is ignored. Setting a valid key marks the view as opened and raises `Changed`. The first enabled view added becomes current. |
| `Filtered` | `bool` | `true` when at least one filter differs from its default values. |
| `Key` | `string` | Identifier of the dashboard. |
| `Refreshing` | `bool` | `true` while any resource is loading or refreshing. |
| `Resources` | `IReadOnlyList<IDashboardSource>` | The registered resources, in the order they were registered. |
| `State` | `DashboardResourceState` | The combined state of the `DashboardQueryStage.Essential` resources, computed by `DashboardController.CombineStates`. Background and deferred resources are ignored. |
| `UpdatedAt` | `DateTimeOffset?` | The oldest `IDashboardSource.UpdatedAt` among the resources that have one, or `null` when none has loaded. |
| `Views` | `IReadOnlyList<DashboardViewMeta>` | The enabled views, in the order they were added. A new list is built on every call. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `AddFilter(DashboardFilterControl filter)` | `DashboardFilterControl` | Adds a filter and forwards its changes to `Changed`. |
| `AddView(string key, string label, string? badge = null, bool enabled = true)` | `DashboardViewMeta` | Adds a view tab. The first enabled view added becomes the current one. |
| `static CombineStates(IEnumerable<DashboardResourceState> states)` | `DashboardResourceState` | Combines several states into one. Disabled states are ignored. No state gives `DashboardResourceState.Ready`, only disabled states give `DashboardResourceState.Disabled`, then any error wins, then any loading, then all idle gives `DashboardResourceState.Idle`, otherwise `DashboardResourceState.Ready`. |
| `RefreshAsync()` | `Task` | Loads all `DashboardQueryStage.Essential` resources in parallel, waits for them, then loads the `DashboardQueryStage.Background` resources and the already active `DashboardQueryStage.Deferred` ones in parallel. |
| `RegisterResource<TData>(DashboardResource<TData> resource)` | `DashboardResource<TData>` | Registers a resource so that `DashboardController.RefreshAsync` loads it and its changes are forwarded to `Changed`. |
| `ResetFilters()` | `void` | Restores the default values of every enabled filter that is not headless. |

#### `DashboardFilterControl`

`Klyroo.Dashboard` · class

Interactive filter control state managed by `DashboardController`.

| Constructor | Description |
| :--- | :--- |
| `DashboardFilterControl(string key, string label, DashboardFilterKind kind = Options, IReadOnlyList<DashboardOption>? items = null, IReadOnlyList<string>? defaultValues = null, bool multiple = false, bool searchable = false, int? max = null, string placeholder = "All", bool headless = false, IReadOnlyList<DashboardFilterPreset>? presets = null)` | Creates a filter control. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Changed` | `bool` | `true` when the filter is enabled, not headless and its selection differs from the default values (same values in the same order). |
| `Display` | `string` | The text of the filter pill: the placeholder when nothing is selected, the labels joined by commas for one or two values, otherwise the first label followed by the number of others (for example `EMEA +2`). |
| `Enabled` (get, set) | `bool` | When `false` the filter is hidden, ignores changes and reports its default values. |
| `FilteredItems` | `IReadOnlyList<DashboardOption>` | The options whose label contains `DashboardFilterControl.Search`, ignoring case; all options when the search is blank. |
| `Headless` | `bool` | Whether the filter is driven by code: hidden from the toolbar and ignored by `DashboardFilterControl.Changed` and by `DashboardController.ResetFilters`. |
| `Items` (get, set) | `IReadOnlyList<DashboardOption>` | The selectable options. Replace the list to update the options, for example after loading them. |
| `Key` | `string` | Identifier of the filter. |
| `Kind` | `DashboardFilterKind` | What the control selects. Metadata for custom UIs. |
| `Label` | `string` | Text shown on the filter pill. |
| `Max` | `int?` | The maximum number of selected values in multiple mode, or `null` for no limit. |
| `Multiple` | `bool` | Whether several values can be selected. |
| `Open` (get, set) | `bool` | Whether the option menu is open in the toolbar. |
| `Placeholder` | `string` | Text shown when nothing is selected. |
| `Presets` | `IReadOnlyList<DashboardFilterPreset>` | Shortcuts that select a set of values in one click. |
| `Search` (get, set) | `string` | The text typed in the search box. Filters `DashboardFilterControl.FilteredItems`. |
| `Searchable` | `bool` | Whether the option list shows a search box. |
| `Value` (get, set) | `string?` | The first selected value. Setting it replaces the selection with that value; `null` or an empty string restores the default values. Ignored when `DashboardFilterControl.Enabled` is `false`. |
| `Values` | `IReadOnlyList<string>` | The selected values, or the default values when `DashboardFilterControl.Enabled` is `false`. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `ApplyPreset(DashboardFilterPreset preset)` | `void` | Replaces the selection with the values of a preset. `DashboardFilterControl.Max` is not checked. |
| `IsSelected(string value)` | `bool` | Whether `value` is selected. |
| `Reset()` | `void` | Restores the default values. |
| `Toggle(string value)` | `void` | Selects or deselects a value. In single mode the value replaces the selection and the menu closes; it cannot be deselected. In multiple mode the value is added unless `DashboardFilterControl.Max` is reached, or removed if already selected. `ChangedEvent` is raised even when nothing changed. |

#### `DashboardResource<TData>`

`Klyroo.Dashboard` · class

A staged, optionally gated asynchronous data resource managed by a `DashboardController`.

| Constructor | Description |
| :--- | :--- |
| `DashboardResource<TData>(string key, Func<CancellationToken, Task<TData>>? loader = null, DashboardQueryStage stage = Essential, TData defaultValue = null, Func<bool>? enabled = null, Func<bool>? requires = null)` | Creates a resource. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Active` | `bool` | `true` once the resource has been activated, loaded or fed with `DashboardResource.SetData`. |
| `Data` | `TData` | The current data. After a failed load it keeps the last successful value. |
| `DefaultValue` | `TData` | The data used before the first load, when disabled, and when the `requires` gate is closed. |
| `Error` | `string?` | The message of the last failure, or `null`. |
| `Fetching` | `bool` | `true` while the loader is running. |
| `Key` | `string` | Identifier of the resource. |
| `Refreshing` | `bool` | `true` while data that was already loaded is being reloaded. |
| `Stage` | `DashboardQueryStage` | The stage in which the controller loads the resource. |
| `State` | `DashboardResourceState` | The lifecycle state of the resource. |
| `UpdatedAt` | `DateTimeOffset?` | When data was last set or loaded successfully (UTC). |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `ActivateAsync()` | `Task` | Marks the resource active and loads it when its state is `DashboardResourceState.Idle`. A disabled resource only switches to `DashboardResourceState.Disabled` and resets to its default value. |
| `RefreshAsync()` | `Task` | Runs the loader. A disabled resource goes to `DashboardResourceState.Disabled`. An unmet `requires` gate resets the data to the default value without calling the loader. Otherwise the state is `DashboardResourceState.Loading` on the first load, or stays `DashboardResourceState.Ready` with `DashboardResource.Refreshing` set on later ones. An exception from the loader is caught and stored in `DashboardResource.Error`. |
| `SetData(TData data)` | `void` | Sets the resource data directly and transitions to `DashboardResourceState.Ready`. |
| `SetError(string message)` | `void` | Transitions the resource to `DashboardResourceState.Error`. |

#### `DashboardViewMeta`

`Klyroo.Dashboard` · class

Metadata for a named dashboard view tab.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Badge` (get, set) | `string?` | Small text shown next to the label, such as a count. Hidden when empty or `0`. |
| `Enabled` (get, set) | `bool` | Disabled views are left out of `DashboardController.Views` and cannot become current. |
| `Key` (init) | `string` | Identifier of the view, the value of `DashboardController.CurrentView` when it is shown. |
| `Label` (init) | `string` | Text of the view tab. |
| `Opened` (get, set) | `bool` | `true` once the view has been current. Use it to render the content of a tab only after the first visit. |

### Records

#### `DashboardAlertItem`

`Klyroo.Dashboard` · record

One alert row in `KlyDashboardAlerts`.

| Constructor | Description |
| :--- | :--- |
| `DashboardAlertItem(DashboardAlertSeverity Severity, string Label, string? Description = null, string? Value = null, string? ActionLabel = null, string? ActionHref = null)` | One alert row in `KlyDashboardAlerts`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `ActionHref` (init) | `string?` | The target of the link. `#` when `null`. |
| `ActionLabel` (init) | `string?` | The text of a link on the right. |
| `Description` (init) | `string?` | A line of text under the label. |
| `Label` (init) | `string` | The title of the alert. |
| `Severity` (init) | `DashboardAlertSeverity` | The severity, which sets the badge and the order. |
| `Value` (init) | `string?` | A short value on the right, such as a count. |

#### `DashboardBarItem`

`Klyroo.Dashboard` · record

One horizontal bar entry in `KlyDashboardBars`.

| Constructor | Description |
| :--- | :--- |
| `DashboardBarItem(string Label, double Value, string? Tag = null, string? Meta = null, bool Selected = false)` | One horizontal bar entry in `KlyDashboardBars`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Label` (init) | `string` | The name of the bar. |
| `Meta` (init) | `string?` | A text shown instead of the formatted value. |
| `Selected` (init) | `bool` | Marks the row as selected (`data-selected`). |
| `Tag` (init) | `string?` | A small monospaced tag next to the label. |
| `Value` (init) | `double` | The value that sets the bar length. |

#### `DashboardChartSeries`

`Klyroo.Dashboard` · record

One series definition for `KlyDashboardChart`.

| Constructor | Description |
| :--- | :--- |
| `DashboardChartSeries(string Key, string Label, IReadOnlyList<double> Values, string Type = "bar", bool IsComparison = false)` | One series definition for `KlyDashboardChart`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `IsComparison` (init) | `bool` | Marks a comparison series: dashed in the legend and left out of the totals. |
| `Key` (init) | `string` | Identifier of the series, used as the column key in the table view and in the CSV export. |
| `Label` (init) | `string` | The name in the legend. |
| `Type` (init) | `string` | Reserved. Every series is currently drawn as bars. |
| `Values` (init) | `IReadOnlyList<double>` | One value per category. |

#### `DashboardDataColumn`

`Klyroo.Dashboard` · record

Tabular view and CSV export representation of a dashboard block.

| Constructor | Description |
| :--- | :--- |
| `DashboardDataColumn(string Key, string Label, bool Numeric = false)` | Tabular view and CSV export representation of a dashboard block. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Key` (init) | `string` | The key of the cell in each row. |
| `Label` (init) | `string` | The column header. |
| `Numeric` (init) | `bool` | Whether cells are right-aligned in the table view and left unquoted in the CSV export. |

#### `DashboardDataTable`

`Klyroo.Dashboard` · record

Tabular representation of a dashboard block used by "View as table" and "Download CSV".

| Constructor | Description |
| :--- | :--- |
| `DashboardDataTable(IReadOnlyList<DashboardDataColumn> Columns, IReadOnlyList<IReadOnlyDictionary<string, string?>> Rows)` | Tabular representation of a dashboard block used by "View as table" and "Download CSV". |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Columns` (init) | `IReadOnlyList<DashboardDataColumn>` | The columns, in display order. |
| `Rows` (init) | `IReadOnlyList<IReadOnlyDictionary<string, string?>>` | The rows: the formatted text of each cell by column key, `null` for an empty cell. |

#### `DashboardDetailsItem`

`Klyroo.Dashboard` · record

One definition row inside `KlyDashboardDetails`.

| Constructor | Description |
| :--- | :--- |
| `DashboardDetailsItem(string Key, string Label, string? Value, string? Hint = null, bool Mono = false, bool Copy = false, string? Href = null, bool FullSpan = false, bool Hidden = false)` | One definition row inside `KlyDashboardDetails`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Copy` (init) | `bool` | Reserved. The component does not render a copy button. |
| `FullSpan` (init) | `bool` | Whether the row spans both columns on wide screens. |
| `Hidden` (init) | `bool` | Whether the row is left out. |
| `Hint` (init) | `string?` | A line of help text under the value. |
| `Href` (init) | `string?` | When set, the value is a link. |
| `Key` (init) | `string` | Identifier of the row. |
| `Label` (init) | `string` | The term. |
| `Mono` (init) | `bool` | Whether the value uses a monospaced font. |
| `Value` (init) | `string?` | The text of the definition. An empty value is shown as the missing-value placeholder. |

#### `DashboardFeedItem`

`Klyroo.Dashboard` · record

One timeline activity event in `KlyDashboardFeed`.

| Constructor | Description |
| :--- | :--- |
| `DashboardFeedItem(string Label, DateTimeOffset Time, string? Description = null, string? AvatarText = null)` | One timeline activity event in `KlyDashboardFeed`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `AvatarText` (init) | `string?` | Short text in a circle, such as initials. |
| `Description` (init) | `string?` | A line of text under the label. |
| `Label` (init) | `string` | What happened. |
| `Time` (init) | `DateTimeOffset` | When it happened. |

#### `DashboardFilterPreset`

`Klyroo.Dashboard` · record

A preset value in a `DashboardFilterControl`.

| Constructor | Description |
| :--- | :--- |
| `DashboardFilterPreset(string Label, IReadOnlyList<string> Values, string? Icon = null)` | A preset value in a `DashboardFilterControl`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Icon` (init) | `string?` | An icon name. Not rendered by the built-in toolbar. |
| `Label` (init) | `string` | The text of the preset button. |
| `Values` (init) | `IReadOnlyList<string>` | The values selected when the preset is applied. |

#### `DashboardListItem`

`Klyroo.Dashboard` · record

One ranked row in `KlyDashboardList`.

| Constructor | Description |
| :--- | :--- |
| `DashboardListItem(string Label, string? Description = null, double? Value = null, double? Percent = null, double? Delta = null, string? LeadingText = null, string? Href = null, bool Selected = false)` | One ranked row in `KlyDashboardList`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Delta` (init) | `double?` | A percent change, green when zero or positive and red when negative. |
| `Description` (init) | `string?` | A line of text under the label. |
| `Href` (init) | `string?` | When set, the label is a link. |
| `Label` (init) | `string` | The name of the row. |
| `LeadingText` (init) | `string?` | Short text in a circle before the label, such as initials or a rank. |
| `Percent` (init) | `double?` | A percentage shown before the value. |
| `Selected` (init) | `bool` | Marks the row as selected (`data-selected`). |
| `Value` (init) | `double?` | The value, formatted with the list `Format`. |

#### `DashboardOption`

`Klyroo.Dashboard` · record

An option item in a `DashboardFilterControl`.

| Constructor | Description |
| :--- | :--- |
| `DashboardOption(string Value, string Label, string? Description = null, string? Icon = null, bool Disabled = false)` | An option item in a `DashboardFilterControl`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Description` (init) | `string?` | A secondary text. Not rendered by the built-in toolbar. |
| `Disabled` (init) | `bool` | Whether the option cannot be selected. |
| `Icon` (init) | `string?` | An icon name. Not rendered by the built-in toolbar. |
| `Label` (init) | `string` | The text shown to the user. |
| `Value` (init) | `string` | The value stored in the selection. |

#### `DashboardStatsItem`

`Klyroo.Dashboard` · record

One metric entry inside `KlyDashboardStats`.

| Constructor | Description |
| :--- | :--- |
| `DashboardStatsItem(string Key, string Label, double? Value, DashboardFormatPreset Format = Number, double? Delta = null, bool InvertDelta = false, string? Caption = null, double? Progress = null, DashboardStatus? Status = null)` | One metric entry inside `KlyDashboardStats`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Caption` (init) | `string?` | A line of text under the value. |
| `Delta` (init) | `double?` | The change in percent, shown next to the value. |
| `Format` (init) | `DashboardFormatPreset` | How the value is formatted. |
| `InvertDelta` (init) | `bool` | When set, a negative delta is shown as good news (for churn or cost). |
| `Key` (init) | `string` | Identifier of the metric. |
| `Label` (init) | `string` | The metric name. |
| `Progress` (init) | `double?` | A progress bar from 0 to 100. |
| `Status` (init) | `DashboardStatus?` | A status pill. |
| `Value` (init) | `double?` | The value, or `null` to show a dash. |

#### `DashboardStatus`

`Klyroo.Dashboard` · record

Status pill shown on a `KlyDashboardStat` or `DashboardStatsItem`.

| Constructor | Description |
| :--- | :--- |
| `DashboardStatus(string Label, string? Color = null)` | Status pill shown on a `KlyDashboardStat` or `DashboardStatsItem`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Color` (init) | `string?` | A semantic color name such as `success`, `warning`, `error`, `info` or `primary`; neutral when `null`. It must not be empty. |
| `Label` (init) | `string` | The text of the pill. |

#### `DashboardTableColumn`

`Klyroo.Dashboard` · record

Column definition for `KlyDashboardTable`.

| Constructor | Description |
| :--- | :--- |
| `DashboardTableColumn(string Key, string Label, string Type = "text", DashboardFormatPreset Format = Number, bool InvertDelta = false, bool Sortable = true)` | Column definition for `KlyDashboardTable`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Format` (init) | `DashboardFormatPreset` | The number format of `number` and `bar` cells. `delta` and `percent` cells use their own format. |
| `InvertDelta` (init) | `bool` | When set, a negative `delta` is shown as good news. |
| `Key` (init) | `string` | The key of the cell in each row. |
| `Label` (init) | `string` | The column header. |
| `Sortable` (init) | `bool` | Whether the header is a sort button. |
| `Type` (init) | `string` | One of `text`, `number`, `delta`, `percent` or `bar`. Any other value is treated as `text`. |

#### `DashboardTableSort`

`Klyroo.Dashboard` · record

Active sort state of `KlyDashboardTable`.

| Constructor | Description |
| :--- | :--- |
| `DashboardTableSort(string Key, bool Descending)` | Active sort state of `KlyDashboardTable`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Descending` (init) | `bool` | Whether the order is descending. |
| `Key` (init) | `string` | The key of the sorted column. |

### Interfaces

#### `IDashboardSource`

`Klyroo.Dashboard` · interface

Common interface implemented by dashboard queries and derived resources.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Active` | `bool` | `true` once the source has been activated or loaded. Deferred sources are refreshed by the controller only when active. |
| `Error` | `string?` | The message of the last failure, or `null` when the last load succeeded. |
| `Fetching` | `bool` | `true` while any load is running, first load or refresh. |
| `Key` | `string` | Identifier of the source, unique within a controller by convention. |
| `Refreshing` | `bool` | `true` while data that was already loaded is being reloaded. The old data stays visible. |
| `Stage` | `DashboardQueryStage` | The stage in which `DashboardController.RefreshAsync` loads the source. |
| `State` | `DashboardResourceState` | The lifecycle state of the source. |
| `UpdatedAt` | `DateTimeOffset?` | When data was last loaded successfully, or `null` if it never was. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `ActivateAsync()` | `Task` | Marks the source active and loads it if it has never loaded. |
| `RefreshAsync()` | `Task` | Reloads the source. |

### Enums

#### `DashboardAlertSeverity`

`Klyroo.Dashboard` · enum

Alert severity level for `KlyDashboardAlerts`.

| Value | Description |
| :--- | :--- |
| `Error` | A failure. Shown with the label `Critical` and sorted first. |
| `Warning` | Something that needs attention. Shown with the label `Warning`. |
| `Info` | Information. Shown with the label `Info`. |
| `Success` | A resolved condition. Shown with the label `Resolved` and sorted last. |

#### `DashboardBlockPhase`

`Klyroo.Dashboard` · enum

Block rendering phase determined by `KlyDashboardCard`.

| Value | Description |
| :--- | :--- |
| `Idle` | The source has not loaded yet. The card shows a skeleton, or nothing when the source is disabled. |
| `Loading` | The source is loading for the first time. The card shows a skeleton. |
| `Error` | The source failed. The card shows the error message and a retry button. |
| `Empty` | The source is ready, or there is no source, but the block has nothing to show. |
| `Content` | The block content is rendered. |

#### `DashboardComparisonMode`

`Klyroo.Dashboard` · enum

Comparison period mode for date ranges and KPI comparisons.

| Value | Description |
| :--- | :--- |
| `None` | No comparison. |
| `Previous` | The period of the same length that ends the day before the range starts. |
| `Year` | The same dates one year earlier. February 29 becomes February 28 when the previous year is not a leap year. |

#### `DashboardFilterKind`

`Klyroo.Dashboard` · enum

Filter control kind in a `DashboardController`.

| Value | Description |
| :--- | :--- |
| `String` | Free text. Metadata for custom UIs: the toolbar of `KlyDashboardPage` draws every visible filter the same way and does not read the kind. |
| `Number` | A number. Metadata for custom UIs: the toolbar of `KlyDashboardPage` draws every visible filter the same way and does not read the kind. |
| `Boolean` | A yes or no choice. Metadata for custom UIs: the toolbar of `KlyDashboardPage` draws every visible filter the same way and does not read the kind. |
| `Date` | A single date. Metadata for custom UIs: the toolbar of `KlyDashboardPage` draws every visible filter the same way and does not read the kind. |
| `DateRange` | A start and an end date. Metadata for custom UIs: the toolbar of `KlyDashboardPage` draws every visible filter the same way and does not read the kind. |
| `Enum` | One value out of a fixed set. Metadata for custom UIs: the toolbar of `KlyDashboardPage` draws every visible filter the same way and does not read the kind. |
| `Options` | A choice among `DashboardFilterControl.Items`. This is the default. Metadata for custom UIs: the toolbar of `KlyDashboardPage` draws every visible filter the same way and does not read the kind. |
| `Remote` | Options loaded from a remote source. Metadata for custom UIs: the toolbar of `KlyDashboardPage` draws every visible filter the same way and does not read the kind. |
| `Comparison` | A `DashboardComparisonMode` choice. Metadata for custom UIs: the toolbar of `KlyDashboardPage` draws every visible filter the same way and does not read the kind. |
| `Custom` | A control drawn by the host application. Metadata for custom UIs: the toolbar of `KlyDashboardPage` draws every visible filter the same way and does not read the kind. |

#### `DashboardFormatPreset`

`Klyroo.Dashboard` · enum

Preset number formats supported by `DashboardFormat`.

| Value | Description |
| :--- | :--- |
| `Number` | Grouped number with at most one decimal, switching to compact notation from 10,000 (`1,234.5`, `12.6K`). |
| `Integer` | Rounded to the nearest integer, midpoints to even, with group separators (`1,235`). |
| `Decimal` | Grouped number with at most one decimal and no compact notation (`1,234.5`). |
| `Compact` | Thousands, millions and billions with at most one decimal (`12.6K`, `2.4M`, `1.1B`). |
| `Percent` | A value already expressed in percent, with a percent sign (`14.2` gives `14.2%`). |
| `Ratio` | A fraction multiplied by 100, with a percent sign (`0.57` gives `57%`). |
| `Delta` | A signed percent change, with a plus sign when positive (`+14.2%`, `-4.5%`). |
| `Points` | A signed difference in percentage points (`+2.1 pts`, `+1 pt`). |
| `Signed` | A grouped number with a plus sign when positive (`+1,200`). |

#### `DashboardQueryStage`

`Klyroo.Dashboard` · enum

Execution stage of a dashboard query.

| Value | Description |
| :--- | :--- |
| `Essential` | Loaded first, all resources of this stage in parallel. `DashboardController.State` only looks at these resources. |
| `Background` | Loaded once every `DashboardQueryStage.Essential` resource has settled, in parallel with the active deferred resources. |
| `Deferred` | Not loaded by `DashboardController.RefreshAsync` until the resource is activated, by `IDashboardSource.ActivateAsync` or by the first render of a card bound to it. After that it is refreshed with the background stage. |

#### `DashboardResourceState`

`Klyroo.Dashboard` · enum

Lifecycle state of a dashboard query or derived resource.

| Value | Description |
| :--- | :--- |
| `Idle` | The resource has not loaded yet, or a `requires` gate kept it from loading and no default value was supplied. |
| `Loading` | The first load is running and no data is available yet. |
| `Ready` | Data is available. A refresh can still be running; check `IDashboardSource.Refreshing`. |
| `Error` | The last load threw. `IDashboardSource.Error` holds the message and the previous data, if any, is kept. |
| `Disabled` | The `enabled` predicate returned `false`. The data is the default value and a card bound to the resource is not rendered. |

#### `DashboardSkeletonKind`

`Klyroo.Dashboard` · enum

Skeleton geometry kind rendered while a `KlyDashboardCard` is loading.

| Value | Description |
| :--- | :--- |
| `Rows` | Generic list rows. Rendered as three lines. |
| `Stat` | A large value and a caption, used by `KlyDashboardStat`. |
| `Stats` | A grid of metrics, used by `KlyDashboardStats`. Rendered as three lines. |
| `Gauge` | A gauge, used by `KlyDashboardGauge`. Rendered as three lines. |
| `Details` | A definition list, used by `KlyDashboardDetails`. Rendered as three lines. |
| `Bars` | Four vertical bars of different heights. |
| `Paired` | Paired bars. Rendered as three lines. |
| `Funnel` | A funnel, used by `KlyDashboardBars` with the `funnel` variant. Rendered as three lines. |
| `Stack` | Stacked bars. Rendered as three lines. |
| `Table` | A table, used by `KlyDashboardTable`. Rendered as three lines. |
| `Feed` | A timeline, used by `KlyDashboardFeed`. Rendered as three lines. |
| `Lines` | Same shape as `DashboardSkeletonKind.Bars`, used by `KlyDashboardChart` when `Type` is `line`. |
| `Donut` | A donut chart, used by `KlyDashboardChart` when `Type` is `donut`. Rendered as three lines. |

## Klyroo.Dashboard

Components (13) · Static classes and extensions (1)

### Components

#### `KlyDashboardAlerts`

`Klyroo.Dashboard` · component · inherits `KlyComponentBase`

A list of alerts ordered by severity, each with an optional value and action link.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Card` | `bool` | Whether the block draws its own surface (border, background and shadow; only the background in a `panels` grid). Defaults to `true`. |
| `Freshness` | `bool?` | Shows an `Updated` footer built from `Source.UpdatedAt`. When `null`, the value of the enclosing `KlyDashboardGrid.Freshness` applies, or `false` outside a grid. |
| `Items` | `IReadOnlyList<DashboardAlertItem>?` | The alerts to display. |
| `Limit` | `int?` | The maximum number of alerts shown, applied after ordering. |
| `Menu` | `bool?` | Shows the View as table, Download CSV and Expand buttons in the header once the content is displayed. The CSV text only reaches your code through `KlyDashboardCard.OnCsvExported`, which the ready-made blocks do not expose, so Download CSV does nothing on them. When `null`, the value of the enclosing `KlyDashboardGrid.Menu` applies, or `false` outside a grid. |
| `Order` | `string` | `severity` (the default) puts errors first and successes last. Any other value keeps the order of `KlyDashboardAlerts.Items`. |
| `Source` | `IDashboardSource?` | The resource whose state drives the block: a skeleton while it loads, an error message with a retry button, then the content. The block does not read `Data` from it, so pass the data through the other parameters. Without a source the content renders immediately. |
| `Span` | `int` | Width of the block in columns of the 12-column grid, clamped between 1 and 12. Defaults to 6. |
| `Subtitle` | `string?` | A secondary line under the title. |
| `Title` | `string?` | The heading of the block. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyDashboardBars`

`Klyroo.Dashboard` · component · inherits `KlyComponentBase`

Horizontal bars for a ranking, or a funnel with the conversion rate between steps.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Card` | `bool` | Whether the block draws its own surface (border, background and shadow; only the background in a `panels` grid). Defaults to `true`. |
| `Emphasis` | `string` | `all` (the default) colors every bar. `first` colors the first bar and mutes the others. |
| `Format` | `DashboardFormatPreset` | How the values are formatted, unless an item supplies its own `Meta` text. |
| `Freshness` | `bool?` | Shows an `Updated` footer built from `Source.UpdatedAt`. When `null`, the value of the enclosing `KlyDashboardGrid.Freshness` applies, or `false` outside a grid. |
| `Items` | `IReadOnlyList<DashboardBarItem>?` | The bars, in display order. |
| `Limit` | `int?` | The maximum number of bars shown. |
| `Max` | `double?` | The value that fills a whole bar. Defaults to the largest item. Ignored by the funnel variant. |
| `Menu` | `bool?` | Shows the View as table, Download CSV and Expand buttons in the header once the content is displayed. The CSV text only reaches your code through `KlyDashboardCard.OnCsvExported`, which the ready-made blocks do not expose, so Download CSV does nothing on them. When `null`, the value of the enclosing `KlyDashboardGrid.Menu` applies, or `false` outside a grid. |
| `Source` | `IDashboardSource?` | The resource whose state drives the block: a skeleton while it loads, an error message with a retry button, then the content. The block does not read `Data` from it, so pass the data through the other parameters. Without a source the content renders immediately. |
| `Span` | `int` | Width of the block in columns of the 12-column grid, clamped between 1 and 12. Defaults to 6. |
| `Subtitle` | `string?` | A secondary line under the title. |
| `Title` | `string?` | The heading of the block. |
| `Variant` | `string` | `bars` (the default), or `funnel`: every bar is scaled to the first one and shows its share of the previous step. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyDashboardCard`

`Klyroo.Dashboard` · component · inherits `KlyComponentBase`

The frame shared by every block: header with title and menu, loading skeleton, error with a retry button, empty state, content, chips for active filters and a footer. Use it directly to build a custom block.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Card` | `bool` | Whether the block draws its own surface (border, background and shadow; only the background in a `panels` grid). Defaults to `true`. |
| `ChildContent` | `RenderFragment?` | The content of the block, rendered once the source is ready and not empty. |
| `EmptyDescription` | `string?` | A line of text under the title of the empty state. |
| `EmptyTitle` | `string?` | The title of the empty state. Defaults to `No data for this period`. |
| `Expandable` | `bool` | Shows the Expand button in the menu, which enlarges the card over the page. Defaults to `true`. |
| `Filters` | `IReadOnlyList<DashboardFilterControl>?` | Filters whose non-default selection is shown as removable chips under the header. |
| `Footer` | `RenderFragment?` | Content placed in the footer, before the freshness text. |
| `Freshness` | `bool?` | Shows an `Updated` footer built from `Source.UpdatedAt`. When `null`, the value of the enclosing `KlyDashboardGrid.Freshness` applies, or `false` outside a grid. |
| `HeaderRight` | `RenderFragment?` | Content placed in the header, before the menu buttons. |
| `IsEmpty` | `bool` | Shows the empty state instead of the content once the source is ready. |
| `Menu` | `bool?` | Shows the View as table, Download CSV and Expand buttons in the header once the content is displayed. The CSV text only reaches your code through `KlyDashboardCard.OnCsvExported`, which the ready-made blocks do not expose, so Download CSV does nothing on them. When `null`, the value of the enclosing `KlyDashboardGrid.Menu` applies, or `false` outside a grid. |
| `OnCsvExported` | `EventCallback<string>` | Receives the CSV text when the user clicks Download CSV. The card does not start a download: save or offer the text in your handler. Name the file with `DashboardFormat.ResolveFileName`. |
| `Skeleton` | `DashboardSkeletonKind` | The shape of the loading skeleton. Only `Stat`, `Bars` and `Lines` have their own shape, the other kinds render three lines. |
| `Source` | `IDashboardSource?` | The resource whose state drives the block: a skeleton while it loads, an error message with a retry button, then the content. The block does not read `Data` from it, so pass the data through the other parameters. Without a source the content renders immediately. |
| `Span` | `int` | Width of the block in columns of the 12-column grid, clamped between 1 and 12. Defaults to 12. |
| `Subtitle` | `string?` | A secondary line under the title. |
| `Tabulate` | `Func<DashboardDataTable>?` | Returns the content of the block as a table. It enables the View as table and Download CSV buttons. |
| `Title` | `string?` | The heading of the block. |
| `ViewAsTable` | `bool` | Shows the View as table button in the menu. It needs `KlyDashboardCard.Tabulate`. Defaults to `true`. |

Also accepts the parameters of `KlyComponentBase`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Phase` | `DashboardBlockPhase` | The phase the card is in, derived from the state of `KlyDashboardCard.Source`: `Idle` and `Loading` show the skeleton, `Error` the message, otherwise `Empty` or `Content`. Without a source it is `Content`, or `Empty` when `KlyDashboardCard.IsEmpty` is set. |

#### `KlyDashboardChart`

`Klyroo.Dashboard` · component · inherits `KlyComponentBase`

A bar chart over categories, with a legend and optional totals. Only the first series is drawn.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Card` | `bool` | Whether the block draws its own surface (border, background and shadow; only the background in a `panels` grid). Defaults to `true`. |
| `Categories` | `IReadOnlyList<string>?` | The labels of the horizontal axis, one per bar. |
| `Format` | `DashboardFormatPreset` | How the values of labels, tooltips, totals and the table view are formatted. |
| `Freshness` | `bool?` | Shows an `Updated` footer built from `Source.UpdatedAt`. When `null`, the value of the enclosing `KlyDashboardGrid.Freshness` applies, or `false` outside a grid. |
| `Height` | `int` | The height of the plot area in pixels. Defaults to 220. |
| `Menu` | `bool?` | Shows the View as table, Download CSV and Expand buttons in the header once the content is displayed. The CSV text only reaches your code through `KlyDashboardCard.OnCsvExported`, which the ready-made blocks do not expose, so Download CSV does nothing on them. When `null`, the value of the enclosing `KlyDashboardGrid.Menu` applies, or `false` outside a grid. |
| `Series` | `IReadOnlyList<DashboardChartSeries>?` | The data series. Only the first series is drawn; the others appear in the legend, in the totals and in the table view. |
| `ShowLabels` | `bool` | Shows the formatted value above each bar. |
| `ShowLegend` | `bool` | Shows the series legend. Defaults to `true`. |
| `Source` | `IDashboardSource?` | The resource whose state drives the block: a skeleton while it loads, an error message with a retry button, then the content. The block does not read `Data` from it, so pass the data through the other parameters. Without a source the content renders immediately. |
| `Span` | `int` | Width of the block in columns of the 12-column grid, clamped between 1 and 12. Defaults to 8. |
| `Subtitle` | `string?` | A secondary line under the title. |
| `Title` | `string?` | The heading of the block. |
| `Totals` | `string?` | `average` shows the average of each series, any other non-empty value shows the sum. Comparison series are left out. Nothing is shown when empty. |
| `Type` | `string` | The chart type: `bar` (the default), `line` or `donut`. Every type is currently drawn as vertical bars; the value only picks the loading skeleton. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyDashboardDetails`

`Klyroo.Dashboard` · component · inherits `KlyComponentBase`

A definition list of labelled values, with optional hints and links.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Card` | `bool` | Whether the block draws its own surface (border, background and shadow; only the background in a `panels` grid). Defaults to `true`. |
| `Freshness` | `bool?` | Shows an `Updated` footer built from `Source.UpdatedAt`. When `null`, the value of the enclosing `KlyDashboardGrid.Freshness` applies, or `false` outside a grid. |
| `Items` | `IReadOnlyList<DashboardDetailsItem>?` | The definition rows. Rows marked `Hidden` are skipped. |
| `Menu` | `bool?` | Shows the View as table, Download CSV and Expand buttons in the header once the content is displayed. The CSV text only reaches your code through `KlyDashboardCard.OnCsvExported`, which the ready-made blocks do not expose, so Download CSV does nothing on them. When `null`, the value of the enclosing `KlyDashboardGrid.Menu` applies, or `false` outside a grid. |
| `Source` | `IDashboardSource?` | The resource whose state drives the block: a skeleton while it loads, an error message with a retry button, then the content. The block does not read `Data` from it, so pass the data through the other parameters. Without a source the content renders immediately. |
| `Span` | `int` | Width of the block in columns of the 12-column grid, clamped between 1 and 12. Defaults to 6. |
| `Subtitle` | `string?` | A secondary line under the title. |
| `Title` | `string?` | The heading of the block. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyDashboardFeed`

`Klyroo.Dashboard` · component · inherits `KlyComponentBase`

A timeline of events with relative times, grouped by day.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Card` | `bool` | Whether the block draws its own surface (border, background and shadow; only the background in a `panels` grid). Defaults to `true`. |
| `Freshness` | `bool?` | Shows an `Updated` footer built from `Source.UpdatedAt`. When `null`, the value of the enclosing `KlyDashboardGrid.Freshness` applies, or `false` outside a grid. |
| `GroupByDay` | `bool` | Inserts a heading (`Today`, `Yesterday` or an ISO date) when the day changes. Defaults to `true`. |
| `Items` | `IReadOnlyList<DashboardFeedItem>?` | The events, in display order. They are not sorted. |
| `Limit` | `int?` | The maximum number of events shown. |
| `Menu` | `bool?` | Shows the View as table, Download CSV and Expand buttons in the header once the content is displayed. The CSV text only reaches your code through `KlyDashboardCard.OnCsvExported`, which the ready-made blocks do not expose, so Download CSV does nothing on them. When `null`, the value of the enclosing `KlyDashboardGrid.Menu` applies, or `false` outside a grid. |
| `Now` | `DateTimeOffset?` | The reference moment for the day headings and the relative times. The current UTC time when `null`; set it to get deterministic output in tests. |
| `Source` | `IDashboardSource?` | The resource whose state drives the block: a skeleton while it loads, an error message with a retry button, then the content. The block does not read `Data` from it, so pass the data through the other parameters. Without a source the content renders immediately. |
| `Span` | `int` | Width of the block in columns of the 12-column grid, clamped between 1 and 12. Defaults to 6. |
| `Subtitle` | `string?` | A secondary line under the title. |
| `Title` | `string?` | The heading of the block. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyDashboardGauge`

`Klyroo.Dashboard` · component · inherits `KlyComponentBase`

A ring gauge that shows a value between a minimum and a maximum.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Caption` | `string?` | A line of text under the value, inside the ring. |
| `Card` | `bool` | Whether the block draws its own surface (border, background and shadow; only the background in a `panels` grid). Defaults to `true`. |
| `Format` | `DashboardFormatPreset` | How the value and the target are formatted. |
| `Freshness` | `bool?` | Shows an `Updated` footer built from `Source.UpdatedAt`. When `null`, the value of the enclosing `KlyDashboardGrid.Freshness` applies, or `false` outside a grid. |
| `Max` | `double` | The value at which the gauge is full. Defaults to 100. |
| `Menu` | `bool?` | Shows the View as table, Download CSV and Expand buttons in the header once the content is displayed. The CSV text only reaches your code through `KlyDashboardCard.OnCsvExported`, which the ready-made blocks do not expose, so Download CSV does nothing on them. When `null`, the value of the enclosing `KlyDashboardGrid.Menu` applies, or `false` outside a grid. |
| `Min` | `double` | The value at which the gauge is empty. Defaults to 0. |
| `Source` | `IDashboardSource?` | The resource whose state drives the block: a skeleton while it loads, an error message with a retry button, then the content. The block does not read `Data` from it, so pass the data through the other parameters. Without a source the content renders immediately. |
| `Span` | `int` | Width of the block in columns of the 12-column grid, clamped between 1 and 12. Defaults to 4. |
| `Subtitle` | `string?` | A secondary line under the title. |
| `Target` | `double?` | A target shown as text under the gauge. It does not move a marker on the ring. |
| `Title` | `string?` | The heading of the block. |
| `Value` | `double?` | The current value. |
| `Variant` | `string` | Reserved. The gauge is always drawn as a ring; the value is only exposed as `data-variant`. |

Also accepts the parameters of `KlyComponentBase`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Share` | `double` | The filled fraction of the ring, from 0 to 1: `(Value - Min) / (Max - Min)`, clamped. 0 when `KlyDashboardGauge.Value` is `null` or `KlyDashboardGauge.Max` is not above `KlyDashboardGauge.Min`. |

#### `KlyDashboardGrid`

`Klyroo.Dashboard` · component · inherits `KlyComponentBase`

A 12-column grid that places blocks by their `Span`, and gives them its `Menu` and `Freshness` defaults.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | The blocks of the grid. Each one sets its width with `Span`. |
| `Columns` | `int` | Reserved. The grid is always 12 columns wide and this value is not read. |
| `Freshness` | `bool` | The default of `Freshness` for the blocks inside the grid. Defaults to `false`. |
| `Menu` | `bool` | The default of `Menu` for the blocks inside the grid. Defaults to `false`. |
| `Variant` | `string` | `cards` (the default) draws separate cards with a gap. `panels` joins the cards into one bordered panel with thin dividers. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyDashboardList`

`Klyroo.Dashboard` · component · inherits `KlyComponentBase`

A ranked list of rows with an optional value, percentage and delta.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Card` | `bool` | Whether the block draws its own surface (border, background and shadow; only the background in a `panels` grid). Defaults to `true`. |
| `Format` | `DashboardFormatPreset` | How the row values are formatted. |
| `Freshness` | `bool?` | Shows an `Updated` footer built from `Source.UpdatedAt`. When `null`, the value of the enclosing `KlyDashboardGrid.Freshness` applies, or `false` outside a grid. |
| `Items` | `IReadOnlyList<DashboardListItem>?` | The rows, in display order. |
| `Limit` | `int?` | The maximum number of rows shown. |
| `Menu` | `bool?` | Shows the View as table, Download CSV and Expand buttons in the header once the content is displayed. The CSV text only reaches your code through `KlyDashboardCard.OnCsvExported`, which the ready-made blocks do not expose, so Download CSV does nothing on them. When `null`, the value of the enclosing `KlyDashboardGrid.Menu` applies, or `false` outside a grid. |
| `Source` | `IDashboardSource?` | The resource whose state drives the block: a skeleton while it loads, an error message with a retry button, then the content. The block does not read `Data` from it, so pass the data through the other parameters. Without a source the content renders immediately. |
| `Span` | `int` | Width of the block in columns of the 12-column grid, clamped between 1 and 12. Defaults to 6. |
| `Subtitle` | `string?` | A secondary line under the title. |
| `Title` | `string?` | The heading of the block. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyDashboardPage`

`Klyroo.Dashboard` · component · inherits `KlyComponentBase`

Page shell of a dashboard: a header with the title and a refresh button, a sticky toolbar with the view tabs and the filter pills of a `DashboardController`, and a body for the blocks. It re-renders when the controller changes. Buttons need an interactive render mode.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Actions` | `RenderFragment?` | Content placed before the refresh button, for example your own buttons. |
| `ChildContent` | `RenderFragment?` | The body of the page, usually a `KlyDashboardGrid`. The page only draws the tabs: render the content of the current view yourself from `DashboardController.CurrentView`. |
| `Dashboard` | `DashboardController?` | The controller shown by the page: it supplies the refresh button, the view tabs and the filter pills, and the page re-renders when the controller changes. Without it only the title and the body are rendered. |
| `Description` | `string?` | Text under the heading. When empty, the page shows an `Updated` line from `Dashboard.UpdatedAt` once data has loaded. |
| `ShowFilters` | `bool` | Shows a pill for each enabled filter that is not headless. Defaults to `true`. |
| `ShowRefresh` | `bool` | Shows the refresh button, which calls `DashboardController.RefreshAsync`. Defaults to `true`. |
| `ShowTabs` | `bool` | Shows the view tabs when the controller has at least two enabled views. Defaults to `true`. |
| `Sticky` | `bool` | Keeps the toolbar of tabs and filters at the top of the page while the body scrolls. Defaults to `true`. |
| `Title` | `string?` | The page heading. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyDashboardStat`

`Klyroo.Dashboard` · component · inherits `KlyComponentBase`

A single metric with an optional delta, sparkline, goal bar and status pill.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Caption` | `string?` | A line of text under the value. Replaces the default `vs ... previous period` caption. |
| `Card` | `bool` | Whether the block draws its own surface (border, background and shadow; only the background in a `panels` grid). Defaults to `true`. |
| `Compare` | `double?` | The previous value. When `KlyDashboardStat.Delta` is not set, the delta is computed from it, and the default caption names it. |
| `CompareMode` | `string` | `change` (the default) computes the percent change from `KlyDashboardStat.Compare` and shows nothing when `KlyDashboardStat.Compare` is zero. `difference` uses the raw difference, multiplied by 100 for the `Ratio` format. |
| `Delta` | `double?` | The change to display, in percent. It takes precedence over `KlyDashboardStat.Compare`. |
| `DeltaFormat` | `DashboardFormatPreset?` | How the delta is formatted. Defaults to `Delta`, or to `Points` or `Signed` when `KlyDashboardStat.CompareMode` is `difference`. |
| `Format` | `DashboardFormatPreset` | How `KlyDashboardStat.Value`, `KlyDashboardStat.Compare` and `KlyDashboardStat.Goal` are formatted. |
| `Freshness` | `bool?` | Shows an `Updated` footer built from `Source.UpdatedAt`. When `null`, the value of the enclosing `KlyDashboardGrid.Freshness` applies, or `false` outside a grid. |
| `Goal` | `double?` | A target that adds a progress bar and a percentage. It must be greater than 0. |
| `InvertDelta` | `bool` | When set, a negative delta is shown as good news (for churn, cost or latency). |
| `Label` | `string` | The name of the metric, shown as the card title. Required. |
| `Menu` | `bool?` | Shows the View as table, Download CSV and Expand buttons in the header once the content is displayed. The CSV text only reaches your code through `KlyDashboardCard.OnCsvExported`, which the ready-made blocks do not expose, so Download CSV does nothing on them. When `null`, the value of the enclosing `KlyDashboardGrid.Menu` applies, or `false` outside a grid. |
| `Source` | `IDashboardSource?` | The resource whose state drives the block: a skeleton while it loads, an error message with a retry button, then the content. The block does not read `Data` from it, so pass the data through the other parameters. Without a source the content renders immediately. |
| `Span` | `int` | Width of the block in columns of the 12-column grid, clamped between 1 and 12. Defaults to 4. |
| `Status` | `DashboardStatus?` | A status pill in the header. |
| `TextValue` | `string?` | Text shown instead of the formatted `KlyDashboardStat.Value`, for values that are not numbers. |
| `Trend` | `IReadOnlyList<double>?` | Points of a sparkline. It needs at least two values. |
| `Value` | `double?` | The value, formatted with `KlyDashboardStat.Format`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyDashboardStats`

`Klyroo.Dashboard` · component · inherits `KlyComponentBase`

Several metrics side by side, each with an optional delta, progress bar and status pill.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Card` | `bool` | Whether the block draws its own surface (border, background and shadow; only the background in a `panels` grid). Defaults to `true`. |
| `Freshness` | `bool?` | Shows an `Updated` footer built from `Source.UpdatedAt`. When `null`, the value of the enclosing `KlyDashboardGrid.Freshness` applies, or `false` outside a grid. |
| `Items` | `IReadOnlyList<DashboardStatsItem>?` | The metrics, laid out in a grid of up to four columns. |
| `Menu` | `bool?` | Shows the View as table, Download CSV and Expand buttons in the header once the content is displayed. The CSV text only reaches your code through `KlyDashboardCard.OnCsvExported`, which the ready-made blocks do not expose, so Download CSV does nothing on them. When `null`, the value of the enclosing `KlyDashboardGrid.Menu` applies, or `false` outside a grid. |
| `Source` | `IDashboardSource?` | The resource whose state drives the block: a skeleton while it loads, an error message with a retry button, then the content. The block does not read `Data` from it, so pass the data through the other parameters. Without a source the content renders immediately. |
| `Span` | `int` | Width of the block in columns of the 12-column grid, clamped between 1 and 12. Defaults to 12. |
| `Subtitle` | `string?` | A secondary line under the title. |
| `Title` | `string?` | The heading of the block. |
| `Variant` | `string` | `plain` (the default) or `tiles`, which puts each metric in a bordered tile. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyDashboardTable`

`Klyroo.Dashboard` · component · inherits `KlyComponentBase`

A table with typed columns (text, number, delta, percent, bar) and click-to-sort headers.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Card` | `bool` | Whether the block draws its own surface (border, background and shadow; only the background in a `panels` grid). Defaults to `true`. |
| `Columns` | `IReadOnlyList<DashboardTableColumn>?` | The column definitions, in display order. |
| `Freshness` | `bool?` | Shows an `Updated` footer built from `Source.UpdatedAt`. When `null`, the value of the enclosing `KlyDashboardGrid.Freshness` applies, or `false` outside a grid. |
| `Limit` | `int?` | The maximum number of rows shown, applied after sorting. |
| `Menu` | `bool?` | Shows the View as table, Download CSV and Expand buttons in the header once the content is displayed. The CSV text only reaches your code through `KlyDashboardCard.OnCsvExported`, which the ready-made blocks do not expose, so Download CSV does nothing on them. When `null`, the value of the enclosing `KlyDashboardGrid.Menu` applies, or `false` outside a grid. |
| `Rows` | `IReadOnlyList<IReadOnlyDictionary<string, object?>>?` | The rows: one dictionary per row, holding the raw value of each column by key. Missing and `null` cells show the missing-value placeholder. |
| `Sort` | `DashboardTableSort?` | The active sort. The table updates it itself when a header is clicked, and `KlyDashboardTable.SortChanged` reports the change. Numeric columns sort descending first, then ascending, then unsorted. Empty cells always come last. |
| `SortChanged` | `EventCallback<DashboardTableSort?>` | Raised when the sort changes, with `null` when the sort is cleared. |
| `Source` | `IDashboardSource?` | The resource whose state drives the block: a skeleton while it loads, an error message with a retry button, then the content. The block does not read `Data` from it, so pass the data through the other parameters. Without a source the content renders immediately. |
| `Span` | `int` | Width of the block in columns of the 12-column grid, clamped between 1 and 12. Defaults to 12. |
| `Subtitle` | `string?` | A secondary line under the title. |
| `Title` | `string?` | The heading of the block. |

Also accepts the parameters of `KlyComponentBase`.

### Static classes and extensions

#### `KlyrooDashboardServiceCollectionExtensions`

`Klyroo.Dashboard` · service

Registration of the Klyroo.Dashboard services.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static AddKlyrooDashboard(IServiceCollection services)` | `IServiceCollection` | Registers the services the dashboard components need. The dashboard has none of its own: this calls `AddKlyrooUi()` from Klyroo.UI, so a second call to `AddKlyrooUi()` is not needed. |
