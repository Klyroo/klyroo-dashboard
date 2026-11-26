using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Klyroo.Dashboard;

/// <summary>Lifecycle state of a dashboard query or derived resource.</summary>
public enum DashboardResourceState
{
    /// <summary>The resource has not loaded yet, or a <c>requires</c> gate kept it from loading and no default value was supplied.</summary>
    Idle,
    /// <summary>The first load is running and no data is available yet.</summary>
    Loading,
    /// <summary>Data is available. A refresh can still be running; check <see cref="IDashboardSource.Refreshing"/>.</summary>
    Ready,
    /// <summary>The last load threw. <see cref="IDashboardSource.Error"/> holds the message and the previous data, if any, is kept.</summary>
    Error,
    /// <summary>The <c>enabled</c> predicate returned <see langword="false"/>. The data is the default value and a card bound to the resource is not rendered.</summary>
    Disabled,
}

/// <summary>Execution stage of a dashboard query.</summary>
public enum DashboardQueryStage
{
    /// <summary>Loaded first, all resources of this stage in parallel. <see cref="DashboardController.State"/> only looks at these resources.</summary>
    Essential,
    /// <summary>Loaded once every <see cref="Essential"/> resource has settled, in parallel with the active deferred resources.</summary>
    Background,
    /// <summary>Not loaded by <see cref="DashboardController.RefreshAsync"/> until the resource is activated, by <see cref="IDashboardSource.ActivateAsync"/> or by the first render of a card bound to it. After that it is refreshed with the background stage.</summary>
    Deferred,
}

/// <summary>Block rendering phase determined by <c>KlyDashboardCard</c>.</summary>
public enum DashboardBlockPhase
{
    /// <summary>The source has not loaded yet. The card shows a skeleton, or nothing when the source is disabled.</summary>
    Idle,
    /// <summary>The source is loading for the first time. The card shows a skeleton.</summary>
    Loading,
    /// <summary>The source failed. The card shows the error message and a retry button.</summary>
    Error,
    /// <summary>The source is ready, or there is no source, but the block has nothing to show.</summary>
    Empty,
    /// <summary>The block content is rendered.</summary>
    Content,
}

/// <summary>Skeleton geometry kind rendered while a <c>KlyDashboardCard</c> is loading.</summary>
public enum DashboardSkeletonKind
{
    /// <summary>Generic list rows. Rendered as three lines.</summary>
    Rows,
    /// <summary>A large value and a caption, used by <c>KlyDashboardStat</c>.</summary>
    Stat,
    /// <summary>A grid of metrics, used by <c>KlyDashboardStats</c>. Rendered as three lines.</summary>
    Stats,
    /// <summary>A gauge, used by <c>KlyDashboardGauge</c>. Rendered as three lines.</summary>
    Gauge,
    /// <summary>A definition list, used by <c>KlyDashboardDetails</c>. Rendered as three lines.</summary>
    Details,
    /// <summary>Four vertical bars of different heights.</summary>
    Bars,
    /// <summary>Paired bars: three groups of two vertical bars.</summary>
    Paired,
    /// <summary>A funnel, used by <c>KlyDashboardBars</c> with the <c>funnel</c> variant. Rendered as three lines.</summary>
    Funnel,
    /// <summary>Stacked bars: three horizontal bars split into segments.</summary>
    Stack,
    /// <summary>A table, used by <c>KlyDashboardTable</c>. Rendered as three lines.</summary>
    Table,
    /// <summary>A timeline, used by <c>KlyDashboardFeed</c>. Rendered as three lines.</summary>
    Feed,
    /// <summary>Same shape as <see cref="Bars"/>, used by <c>KlyDashboardChart</c> when <c>Type</c> is <c>line</c>.</summary>
    Lines,
    /// <summary>A donut chart, used by <c>KlyDashboardChart</c> when <c>Type</c> is <c>donut</c>. Rendered as three lines.</summary>
    Donut,
}

/// <summary>Preset number formats supported by <c>DashboardFormat</c>.</summary>
public enum DashboardFormatPreset
{
    /// <summary>Grouped number with at most one decimal, switching to compact notation from 10,000 (<c>1,234.5</c>, <c>12.6K</c>).</summary>
    Number,
    /// <summary>Rounded to the nearest integer, midpoints to even, with group separators (<c>1,235</c>).</summary>
    Integer,
    /// <summary>Grouped number with at most one decimal and no compact notation (<c>1,234.5</c>).</summary>
    Decimal,
    /// <summary>Thousands, millions and billions with at most one decimal (<c>12.6K</c>, <c>2.4M</c>, <c>1.1B</c>).</summary>
    Compact,
    /// <summary>A value already expressed in percent, with a percent sign (<c>14.2</c> gives <c>14.2%</c>).</summary>
    Percent,
    /// <summary>A fraction multiplied by 100, with a percent sign (<c>0.57</c> gives <c>57%</c>).</summary>
    Ratio,
    /// <summary>A signed percent change, with a plus sign when positive (<c>+14.2%</c>, <c>-4.5%</c>).</summary>
    Delta,
    /// <summary>A signed difference in percentage points (<c>+2.1 pts</c>, <c>+1 pt</c>).</summary>
    Points,
    /// <summary>A grouped number with a plus sign when positive (<c>+1,200</c>).</summary>
    Signed,
}

/// <summary>Comparison period mode for date ranges and KPI comparisons.</summary>
public enum DashboardComparisonMode
{
    /// <summary>No comparison.</summary>
    None,
    /// <summary>The period of the same length that ends the day before the range starts.</summary>
    Previous,
    /// <summary>The same dates one year earlier. February 29 becomes February 28 when the previous year is not a leap year.</summary>
    Year,
}

/// <summary>Filter control kind in a <c>DashboardController</c>. It picks the control drawn by the toolbar of <c>KlyDashboardPage</c>; the selection is always stored as strings, see <see cref="DashboardFilterControl"/>.</summary>
public enum DashboardFilterKind
{
    /// <summary>Free text, drawn as a text box. The text is applied when the box loses focus or the user presses Enter.</summary>
    String,
    /// <summary>A number, drawn as a number box and stored in the invariant culture (<c>1234.5</c>).</summary>
    Number,
    /// <summary>A yes or no choice, drawn as an option list. Without items it offers <see cref="DashboardFilterControl.BooleanOptions"/> (<c>true</c>, <c>false</c>).</summary>
    Boolean,
    /// <summary>A single date, drawn as a date picker and stored as <c>yyyy-MM-dd</c>.</summary>
    Date,
    /// <summary>A start and an end date, drawn as two date pickers and stored as two <c>yyyy-MM-dd</c> values, start then end. An open bound is an empty string.</summary>
    DateRange,
    /// <summary>One value out of a fixed set, drawn as an option list.</summary>
    Enum,
    /// <summary>A choice among <see cref="DashboardFilterControl.Items"/>, drawn as an option list. This is the default.</summary>
    Options,
    /// <summary>Options loaded from a remote source, drawn as an option list. Your code loads them and assigns <see cref="DashboardFilterControl.Items"/>, which renders the toolbar again.</summary>
    Remote,
    /// <summary>A <see cref="DashboardComparisonMode"/> choice, drawn as an option list. Without items it offers <see cref="DashboardFilterControl.ComparisonOptions"/> (<c>none</c>, <c>previous</c>, <c>year</c>).</summary>
    Comparison,
    /// <summary>A control drawn by the host application through the <c>CustomFilter</c> template of <c>KlyDashboardPage</c>. Without the template the toolbar leaves the filter out.</summary>
    Custom,
}

/// <summary>Alert severity level for <c>KlyDashboardAlerts</c>.</summary>
public enum DashboardAlertSeverity
{
    /// <summary>A failure. Shown with the label <c>Critical</c> and sorted first.</summary>
    Error,
    /// <summary>Something that needs attention. Shown with the label <c>Warning</c>.</summary>
    Warning,
    /// <summary>Information. Shown with the label <c>Info</c>.</summary>
    Info,
    /// <summary>A resolved condition. Shown with the label <c>Resolved</c> and sorted last.</summary>
    Success,
}

/// <summary>Common interface implemented by dashboard queries and derived resources.</summary>
public interface IDashboardSource
{
    /// <summary>Identifier of the source, unique within a controller by convention.</summary>
    string Key { get; }
    /// <summary>The stage in which <see cref="DashboardController.RefreshAsync"/> loads the source.</summary>
    DashboardQueryStage Stage { get; }
    /// <summary>The lifecycle state of the source.</summary>
    DashboardResourceState State { get; }
    /// <summary>The message of the last failure, or <see langword="null"/> when the last load succeeded.</summary>
    string? Error { get; }
    /// <summary><see langword="true"/> while data that was already loaded is being reloaded. The old data stays visible.</summary>
    bool Refreshing { get; }
    /// <summary><see langword="true"/> while any load is running, first load or refresh.</summary>
    bool Fetching { get; }
    /// <summary>When data was last loaded successfully, or <see langword="null"/> if it never was.</summary>
    DateTimeOffset? UpdatedAt { get; }
    /// <summary><see langword="true"/> once the source has been activated or loaded. Deferred sources are refreshed by the controller only when active.</summary>
    bool Active { get; }
    /// <summary>Marks the source active and loads it if it has never loaded, or if it was disabled and is enabled again.</summary>
    Task ActivateAsync();
    /// <summary>Reloads the source. A load still running from an earlier call is cancelled and its result ignored.</summary>
    Task RefreshAsync();
}

/// <summary>
/// A source whose loaded data can be written as JSON text and read back, so that a render hands it to the next one instead of
/// loading it again: the prerender of a Blazor page to its interactive render, for example. The text is written with a
/// <see cref="JsonTypeInfo{T}"/> the app gives, never by reflection, so it works with trimming and NativeAOT.
/// </summary>
public interface IDashboardPersistentSource : IDashboardSource
{
    /// <summary>The loaded data as JSON text, or <see langword="null"/> when there is nothing to hand over: no <see cref="JsonTypeInfo{T}"/>, no successful load yet, or a load still running.</summary>
    string? ExportJson();

    /// <summary>
    /// Takes data written by <see cref="ExportJson"/> as if it had been loaded at <paramref name="updatedAt"/>, and sets <see cref="Restored"/>.
    /// Returns <see langword="false"/> and changes nothing when the source cannot take it: no <see cref="JsonTypeInfo{T}"/>, a load already
    /// started, a disabled or disposed source, a closed <c>requires</c> gate, or text that does not read as the data type.
    /// </summary>
    bool RestoreJson(string json, DateTimeOffset? updatedAt);

    /// <summary>
    /// <see langword="true"/> while the data is the one <see cref="RestoreJson"/> took back: <see cref="DashboardController.LoadAsync"/> keeps it
    /// instead of loading again. Any load, <c>SetData</c>, <c>SetError</c> or closed gate clears it.
    /// </summary>
    bool Restored { get; }
}

// The first load of a page (DashboardController.LoadAsync): a resource whose data was restored keeps it, once its gates pass.
internal interface IDashboardFirstLoad
{
    Task LoadKeepingRestoredAsync();
}

/// <summary>
/// A staged, optionally gated asynchronous data resource managed by a <c>DashboardController</c>.
/// </summary>
public sealed class DashboardResource<TData> : IDashboardPersistentSource, IDashboardFirstLoad, IDisposable
{
    private readonly Func<CancellationToken, Task<TData>>? _loader;
    private readonly Func<bool>? _enabled;
    private readonly Func<bool>? _requires;
    private readonly JsonTypeInfo<TData>? _json;
    private CancellationTokenSource? _load;
    private bool _disposed;

    /// <summary>Creates a resource.</summary>
    /// <param name="key">Identifier of the resource.</param>
    /// <param name="loader">Loads the data. Its token is cancelled when a newer load starts, when the resource becomes disabled or its <paramref name="requires"/> gate closes, when <see cref="SetData"/> or <see cref="SetError"/> replaces the result, and when the resource is disposed; the result of a cancelled load is ignored. When <see langword="null"/>, <see cref="RefreshAsync"/> only marks the resource ready, and you feed it with <see cref="SetData"/>.</param>
    /// <param name="stage">The stage in which the controller loads the resource.</param>
    /// <param name="defaultValue">The data used before the first load, when the resource is disabled, and when <paramref name="requires"/> is not met.</param>
    /// <param name="enabled">Returns <see langword="false"/> to disable the resource. Evaluated here and on every activation and refresh.</param>
    /// <param name="requires">Returns <see langword="false"/> when a precondition is missing (for example a selected customer). Evaluated on every refresh: the loader is skipped and the data resets to <paramref name="defaultValue"/>.</param>
    /// <param name="json">
    /// Writes and reads the data as JSON, usually a property of a source-generated <c>JsonSerializerContext</c> (<c>AppJson.Default.Summary</c>).
    /// With it, the data a prerender loaded can be handed to the interactive render instead of being loaded twice (see <see cref="IDashboardPersistentSource"/>);
    /// without it, every render loads the data itself.
    /// </param>
    public DashboardResource(
        string key,
        Func<CancellationToken, Task<TData>>? loader = null,
        DashboardQueryStage stage = DashboardQueryStage.Essential,
        TData? defaultValue = default,
        Func<bool>? enabled = null,
        Func<bool>? requires = null,
        JsonTypeInfo<TData>? json = null)
    {
        Key = key;
        _loader = loader;
        _json = json;
        Stage = stage;
        DefaultValue = defaultValue;
        Data = defaultValue;
        _enabled = enabled;
        _requires = requires;
        if (_enabled is not null && !_enabled())
        {
            State = DashboardResourceState.Disabled;
        }
    }

    /// <summary>Raised after every state change of the resource, on the thread that made the change.</summary>
    public event Action? Changed;

    /// <summary>Identifier of the resource.</summary>
    public string Key { get; }
    /// <summary>The stage in which the controller loads the resource.</summary>
    public DashboardQueryStage Stage { get; }
    /// <summary>The data used before the first load, when disabled, and when the <c>requires</c> gate is closed.</summary>
    public TData? DefaultValue { get; }
    /// <summary>The current data. After a failed load it keeps the last successful value.</summary>
    public TData? Data { get; private set; }
    /// <summary>The lifecycle state of the resource.</summary>
    public DashboardResourceState State { get; private set; } = DashboardResourceState.Idle;
    /// <summary>The message of the last failure, or <see langword="null"/>.</summary>
    public string? Error { get; private set; }
    /// <summary><see langword="true"/> while data that was already loaded is being reloaded.</summary>
    public bool Refreshing { get; private set; }
    /// <summary><see langword="true"/> while the loader is running.</summary>
    public bool Fetching { get; private set; }
    /// <summary>When data was last set or loaded successfully (UTC).</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }
    /// <summary><see langword="true"/> once the resource has been activated, loaded or fed with <see cref="SetData"/>.</summary>
    public bool Active { get; private set; }
    /// <inheritdoc />
    public bool Restored { get; private set; }

    /// <summary>Sets the resource data directly and transitions to <c>DashboardResourceState.Ready</c>. A load in progress is cancelled and its result ignored.</summary>
    public void SetData(TData? data)
    {
        CancelLoad();
        Restored = false;
        Data = data;
        Error = null;
        State = DashboardResourceState.Ready;
        Refreshing = false;
        Fetching = false;
        UpdatedAt = DateTimeOffset.UtcNow;
        Active = true;
        Changed?.Invoke();
    }

    /// <summary>Transitions the resource to <c>DashboardResourceState.Error</c>. A load in progress is cancelled and its result ignored.</summary>
    public void SetError(string message)
    {
        CancelLoad();
        Restored = false;
        Error = message;
        State = DashboardResourceState.Error;
        Refreshing = false;
        Fetching = false;
        Changed?.Invoke();
    }

    /// <summary>Marks the resource active and loads it when its state is <see cref="DashboardResourceState.Idle"/>, or <see cref="DashboardResourceState.Disabled"/> with an <c>enabled</c> predicate that now returns <see langword="true"/>. A resource that is still disabled stays <see cref="DashboardResourceState.Disabled"/> with its default value, and <see cref="Changed"/> is only raised when it was not disabled before.</summary>
    public async Task ActivateAsync()
    {
        if (_disposed) return;
        if (_enabled is not null && !_enabled())
        {
            MarkDisabled();
            return;
        }
        Active = true;
        if (State is DashboardResourceState.Idle or DashboardResourceState.Disabled)
        {
            await RefreshAsync();
        }
    }

    /// <summary>Runs the loader. A disabled resource goes to <see cref="DashboardResourceState.Disabled"/>. An unmet <c>requires</c> gate resets the data to the default value without calling the loader. Otherwise the state is <see cref="DashboardResourceState.Loading"/> on the first load, or stays <see cref="DashboardResourceState.Ready"/> with <see cref="Refreshing"/> set on later ones. An exception from the loader is caught and stored in <see cref="Error"/>.</summary>
    /// <remarks>A call cancels the load still running from an earlier call: the latest call wins, and the result of a superseded load is ignored even when its loader does not observe the token. After <see cref="Dispose"/> the call does nothing.</remarks>
    public Task RefreshAsync() => RefreshCoreAsync(keepRestored: false);

    Task IDashboardFirstLoad.LoadKeepingRestoredAsync() => RefreshCoreAsync(keepRestored: true);

    private async Task RefreshCoreAsync(bool keepRestored)
    {
        if (_disposed) return;
        if (_enabled is not null && !_enabled())
        {
            MarkDisabled();
            return;
        }

        if (_requires is not null && !_requires())
        {
            CancelLoad();
            Restored = false;
            State = DefaultValue is not null ? DashboardResourceState.Ready : DashboardResourceState.Idle;
            Data = DefaultValue;
            Refreshing = false;
            Fetching = false;
            Changed?.Invoke();
            return;
        }

        if (keepRestored && Restored)
        {
            // The first load of the page: the data came from the previous render a moment ago.
            return;
        }
        Restored = false;

        if (_loader is null)
        {
            State = DashboardResourceState.Ready;
            UpdatedAt ??= DateTimeOffset.UtcNow;
            Changed?.Invoke();
            return;
        }

        Active = true;
        var load = new CancellationTokenSource();
        // The new load becomes current before the old one is cancelled, so a continuation that runs inline in Cancel sees it is superseded.
        var previous = _load;
        _load = load;
        Cancel(previous);
        // A continuation of the old load that ran inline in Cancel may have settled the resource (SetData, SetError).
        if (!IsCurrent(load))
        {
            load.Dispose();
            return;
        }

        var wasReady = State == DashboardResourceState.Ready;
        Refreshing = wasReady;
        Fetching = true;
        if (!wasReady)
        {
            State = DashboardResourceState.Loading;
        }
        Error = null;
        Changed?.Invoke();

        try
        {
            var data = await _loader(load.Token);
            if (IsCurrent(load))
            {
                Data = data;
                State = DashboardResourceState.Ready;
                UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
        catch (Exception ex)
        {
            if (IsCurrent(load))
            {
                Error = ex.Message;
                State = DashboardResourceState.Error;
            }
        }
        finally
        {
            if (IsCurrent(load))
            {
                _load = null;
                Refreshing = false;
                Fetching = false;
                Changed?.Invoke();
            }
            load.Dispose();
        }
    }

    /// <inheritdoc />
    public string? ExportJson()
    {
        if (_json is null || _disposed || State != DashboardResourceState.Ready || Fetching) return null;
        return JsonSerializer.Serialize(Data!, _json);
    }

    /// <inheritdoc />
    public bool RestoreJson(string json, DateTimeOffset? updatedAt)
    {
        ArgumentNullException.ThrowIfNull(json);
        if (_json is null || _disposed || _load is not null || State is DashboardResourceState.Disabled or DashboardResourceState.Loading) return false;
        if ((_enabled is not null && !_enabled()) || (_requires is not null && !_requires())) return false;
        TData? data;
        try
        {
            data = JsonSerializer.Deserialize(json, _json);
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            // Written by another version of the type, or refused by a custom converter (which can throw anything): the resource loads as if nothing had been handed over.
            return false;
        }
        Data = data;
        Error = null;
        State = DashboardResourceState.Ready;
        Refreshing = false;
        Fetching = false;
        UpdatedAt = updatedAt ?? DateTimeOffset.UtcNow;
        Active = true;
        Restored = true;
        Changed?.Invoke();
        return true;
    }

    /// <summary>Cancels the load in progress and makes later calls to <see cref="RefreshAsync"/> and <see cref="ActivateAsync"/> do nothing. The state and the data are left as they are. <c>DashboardController.Dispose</c> disposes the resources registered with it.</summary>
    public void Dispose()
    {
        _disposed = true;
        CancelLoad();
    }

    private bool IsCurrent(CancellationTokenSource load) => !_disposed && ReferenceEquals(_load, load);

    private void MarkDisabled()
    {
        var wasBusy = _load is not null;
        CancelLoad();
        Restored = false;
        var changed = State != DashboardResourceState.Disabled || wasBusy;
        State = DashboardResourceState.Disabled;
        Data = DefaultValue;
        Refreshing = false;
        Fetching = false;
        if (changed)
        {
            Changed?.Invoke();
        }
    }

    private void CancelLoad()
    {
        var current = _load;
        _load = null;
        Cancel(current);
    }

    private static void Cancel(CancellationTokenSource? load)
    {
        if (load is null) return;
        try
        {
            load.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The load finished and disposed its source in the meantime.
        }
    }
}

/// <summary>An option item in a <c>DashboardFilterControl</c>.</summary>
/// <param name="Value">The value stored in the selection.</param>
/// <param name="Label">The text shown to the user.</param>
/// <param name="Description">A secondary line under the label in the option menu of the toolbar.</param>
/// <param name="Icon">The name of a Klyroo.UI icon (for example <c>globe</c> or <c>i-lucide-globe</c>) shown before the label in the option menu of the toolbar.</param>
/// <param name="Disabled">Whether the option cannot be selected.</param>
public sealed record DashboardOption(
    string Value,
    string Label,
    string? Description = null,
    string? Icon = null,
    bool Disabled = false);

/// <summary>A preset value in a <c>DashboardFilterControl</c>.</summary>
/// <param name="Label">The text of the preset button.</param>
/// <param name="Values">The values selected when the preset is applied, normalized as by <see cref="DashboardFilterControl.SetValues"/>.</param>
/// <param name="Icon">The name of a Klyroo.UI icon shown before the label of the preset button in the toolbar.</param>
public sealed record DashboardFilterPreset(
    string Label,
    IReadOnlyList<string> Values,
    string? Icon = null);

/// <summary>Interactive filter control state managed by <c>DashboardController</c>.</summary>
/// <remarks>
/// The selection is a list of strings whatever the kind. The toolbar of <c>KlyDashboardPage</c> writes them as follows:
/// <see cref="DashboardFilterKind.String"/> stores the text, <see cref="DashboardFilterKind.Number"/> an invariant-culture number
/// (<c>1234.5</c>), <see cref="DashboardFilterKind.Date"/> an ISO date (<c>yyyy-MM-dd</c>), <see cref="DashboardFilterKind.DateRange"/>
/// two ISO dates, start then end (an open bound is an empty string), <see cref="DashboardFilterKind.Boolean"/> <c>true</c> or
/// <c>false</c>, and <see cref="DashboardFilterKind.Comparison"/> <c>none</c>, <c>previous</c> or <c>year</c>, which
/// <c>Enum.TryParse&lt;DashboardComparisonMode&gt;(value, ignoreCase: true, out var mode)</c> reads back.
/// </remarks>
public sealed class DashboardFilterControl
{
    private readonly List<string> _values = [];
    private readonly List<string> _defaultValues = [];
    private IReadOnlyList<DashboardOption> _items;
    private bool _enabled = true;

    /// <summary>The options a <see cref="DashboardFilterKind.Boolean"/> filter gets when it is created without items: <c>true</c> (Yes) and <c>false</c> (No).</summary>
    public static IReadOnlyList<DashboardOption> BooleanOptions { get; } =
        [new("true", "Yes"), new("false", "No")];

    /// <summary>The options a <see cref="DashboardFilterKind.Comparison"/> filter gets when it is created without items: <c>none</c>, <c>previous</c> and <c>year</c>, the names of the <see cref="DashboardComparisonMode"/> members in lowercase.</summary>
    public static IReadOnlyList<DashboardOption> ComparisonOptions { get; } =
        [new("none", "No comparison"), new("previous", "Previous period"), new("year", "Previous year")];

    /// <summary>Creates a filter control.</summary>
    /// <param name="key">Identifier of the filter.</param>
    /// <param name="label">Text shown on the filter pill.</param>
    /// <param name="kind">What the control selects. It picks the control the toolbar of <c>KlyDashboardPage</c> draws.</param>
    /// <param name="items">The selectable options. A <see cref="DashboardFilterKind.Boolean"/> or <see cref="DashboardFilterKind.Comparison"/> filter created without items gets <see cref="BooleanOptions"/> or <see cref="ComparisonOptions"/>.</param>
    /// <param name="defaultValues">The values selected at the start and restored by <see cref="Reset"/>.</param>
    /// <param name="multiple">Whether several values can be selected.</param>
    /// <param name="searchable">Whether the option list shows a search box.</param>
    /// <param name="max">The maximum number of selected values when <paramref name="multiple"/> is set. <see cref="Toggle"/>, <see cref="ApplyPreset"/> and <see cref="SetValues"/> enforce it.</param>
    /// <param name="placeholder">Text shown when nothing is selected.</param>
    /// <param name="headless">When set, the filter is not drawn by the toolbar, never counts as changed and is not touched by <see cref="DashboardController.ResetFilters"/>. Use it for a value your code drives.</param>
    /// <param name="presets">Shortcuts that select a set of values in one click.</param>
    public DashboardFilterControl(
        string key,
        string label,
        DashboardFilterKind kind = DashboardFilterKind.Options,
        IReadOnlyList<DashboardOption>? items = null,
        IReadOnlyList<string>? defaultValues = null,
        bool multiple = false,
        bool searchable = false,
        int? max = null,
        string placeholder = "All",
        bool headless = false,
        IReadOnlyList<DashboardFilterPreset>? presets = null)
    {
        Key = key;
        Label = label;
        Kind = kind;
        _items = items is { Count: > 0 } ? items
            : kind == DashboardFilterKind.Boolean ? BooleanOptions
            : kind == DashboardFilterKind.Comparison ? ComparisonOptions
            : items ?? [];
        Multiple = multiple;
        Searchable = searchable;
        Max = max;
        Placeholder = placeholder;
        Headless = headless;
        Presets = presets ?? [];

        if (defaultValues is { Count: > 0 })
        {
            _defaultValues.AddRange(defaultValues);
            _values.AddRange(defaultValues);
        }
    }

    /// <summary>Raised after the selection changes through <see cref="Value"/>, <see cref="Toggle"/>, <see cref="ApplyPreset"/>, <see cref="SetValues"/> or <see cref="Reset"/>, and when <see cref="Enabled"/> changes the values the filter reports. Resources do not reload on their own: subscribe here to call <see cref="DashboardController.RefreshAsync"/>.</summary>
    public event Action? ChangedEvent;

    /// <summary>Raised when <see cref="Enabled"/> or <see cref="Items"/> changes without changing the reported values, so that the controller can tell the page to render again.</summary>
    internal event Action? MetaChanged;

    /// <summary>Identifier of the filter.</summary>
    public string Key { get; }
    /// <summary>Text shown on the filter pill.</summary>
    public string Label { get; }
    /// <summary>What the control selects. The toolbar of <c>KlyDashboardPage</c> draws a text box for <see cref="DashboardFilterKind.String"/>, a number box for <see cref="DashboardFilterKind.Number"/>, a date picker for <see cref="DashboardFilterKind.Date"/>, two for <see cref="DashboardFilterKind.DateRange"/>, your <c>CustomFilter</c> template for <see cref="DashboardFilterKind.Custom"/>, and the option list for the other kinds.</summary>
    public DashboardFilterKind Kind { get; }
    /// <summary>Whether several values can be selected.</summary>
    public bool Multiple { get; }
    /// <summary>Whether the option list shows a search box.</summary>
    public bool Searchable { get; }
    /// <summary>The maximum number of selected values in multiple mode, or <see langword="null"/> for no limit.</summary>
    public int? Max { get; }
    /// <summary>Text shown when nothing is selected.</summary>
    public string Placeholder { get; }
    /// <summary>Whether the filter is driven by code: hidden from the toolbar and ignored by <see cref="Changed"/> and by <see cref="DashboardController.ResetFilters"/>.</summary>
    public bool Headless { get; }
    /// <summary>When <see langword="false"/> the filter is hidden, ignores changes and reports its default values. Changing it closes the option menu and raises <see cref="ChangedEvent"/> when the reported values change; the controller's <c>Changed</c> is raised either way, so the page renders again.</summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value) return;
            var before = Values.ToList();
            _enabled = value;
            Open = false;
            if (before.SequenceEqual(Values, StringComparer.Ordinal))
            {
                MetaChanged?.Invoke();
            }
            else
            {
                ChangedEvent?.Invoke();
            }
        }
    }
    /// <summary>The selectable options. Replace the list to update the options, for example after loading them; the controller's <c>Changed</c> is raised so the page renders again. <see langword="null"/> is stored as an empty list.</summary>
    public IReadOnlyList<DashboardOption> Items
    {
        get => _items;
        set
        {
            if (ReferenceEquals(_items, value)) return;
            _items = value ?? [];
            MetaChanged?.Invoke();
        }
    }
    /// <summary>Shortcuts that select a set of values in one click.</summary>
    public IReadOnlyList<DashboardFilterPreset> Presets { get; }
    /// <summary>The text typed in the search box. Filters <see cref="FilteredItems"/>.</summary>
    public string Search { get; set; } = "";
    /// <summary>Whether the option menu is open in the toolbar.</summary>
    public bool Open { get; set; }

    /// <summary>The selected values, or the default values when <see cref="Enabled"/> is <see langword="false"/>.</summary>
    public IReadOnlyList<string> Values => Enabled ? _values : _defaultValues;
    /// <summary>The first selected value. Setting it replaces the selection with that value; <see langword="null"/> or an empty string restores the default values. Ignored when <see cref="Enabled"/> is <see langword="false"/>.</summary>
    public string? Value
    {
        get => Values.Count > 0 ? Values[0] : null;
        set
        {
            if (!Enabled) return;
            _values.Clear();
            if (!string.IsNullOrEmpty(value))
            {
                _values.Add(value);
            }
            else if (_defaultValues.Count > 0)
            {
                _values.AddRange(_defaultValues);
            }
            ChangedEvent?.Invoke();
        }
    }

    /// <summary><see langword="true"/> when the filter is enabled, not headless and its selection differs from the default values (same values in the same order).</summary>
    public bool Changed =>
        Enabled && !Headless && !_values.SequenceEqual(_defaultValues, StringComparer.Ordinal);

    /// <summary>The text of the filter pill: the placeholder when nothing is selected, <c>start – end</c> for a <see cref="DashboardFilterKind.DateRange"/> (with <c>…</c> for an open bound), the labels joined by commas for one or two values, otherwise the first label followed by the number of others (for example <c>EMEA +2</c>).</summary>
    public string Display
    {
        get
        {
            var current = Values;
            if (current.Count == 0) return Placeholder;
            if (Kind == DashboardFilterKind.DateRange)
            {
                var start = current[0];
                var end = current.Count > 1 ? current[1] : "";
                if (string.IsNullOrEmpty(start) && string.IsNullOrEmpty(end)) return Placeholder;
                return $"{(string.IsNullOrEmpty(start) ? "…" : start)} – {(string.IsNullOrEmpty(end) ? "…" : end)}";
            }
            var labels = current
                .Select(v => Items.FirstOrDefault(i => i.Value == v)?.Label ?? v)
                .ToList();
            if (labels.Count <= 2) return string.Join(", ", labels);
            return $"{labels[0]} +{labels.Count - 1}";
        }
    }

    /// <summary>The options whose label contains <see cref="Search"/>, ignoring case; all options when the search is blank.</summary>
    public IReadOnlyList<DashboardOption> FilteredItems =>
        string.IsNullOrWhiteSpace(Search)
            ? Items
            : Items.Where(i => i.Label.Contains(Search.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>Whether <paramref name="value"/> is selected.</summary>
    /// <param name="value">The option value to look for (ordinal comparison).</param>
    public bool IsSelected(string value) => Values.Contains(value, StringComparer.Ordinal);

    /// <summary>Selects or deselects a value. In single mode the value replaces the selection and the menu closes; it cannot be deselected. In multiple mode the value is added unless <see cref="Max"/> is reached, or removed if already selected. <see cref="ChangedEvent"/> is raised even when nothing changed.</summary>
    /// <param name="value">The option value.</param>
    public void Toggle(string value)
    {
        if (!Enabled) return;
        if (!Multiple)
        {
            _values.Clear();
            _values.Add(value);
            Open = false;
            ChangedEvent?.Invoke();
            return;
        }

        if (_values.Contains(value, StringComparer.Ordinal))
        {
            _values.Remove(value);
        }
        else if (Max is null || _values.Count < Max.Value)
        {
            _values.Add(value);
        }
        ChangedEvent?.Invoke();
    }

    /// <summary>Replaces the selection with the values of a preset, normalized as by <see cref="SetValues"/>: one value in single mode, two for a <see cref="DashboardFilterKind.DateRange"/>, at most <see cref="Max"/> in multiple mode.</summary>
    /// <param name="preset">The preset to apply.</param>
    /// <exception cref="ArgumentNullException"><paramref name="preset"/> is <see langword="null"/>.</exception>
    public void ApplyPreset(DashboardFilterPreset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);
        SetValues(preset.Values);
    }

    /// <summary>Replaces the selection and raises <see cref="ChangedEvent"/>. In single mode only the first value is kept, except for a <see cref="DashboardFilterKind.DateRange"/>, which keeps the first two (start and end). In multiple mode duplicates are dropped and at most <see cref="Max"/> values are kept. A list whose values are all empty clears the selection. Ignored when <see cref="Enabled"/> is <see langword="false"/>.</summary>
    /// <param name="values">The new selection, in order.</param>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
    public void SetValues(IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (!Enabled) return;
        var list = values.Select(v => v ?? "").ToList();
        if (list.All(string.IsNullOrEmpty))
        {
            list.Clear();
        }
        else if (Kind == DashboardFilterKind.DateRange)
        {
            list = list.Take(2).ToList();
        }
        else if (!Multiple)
        {
            list = list.Take(1).ToList();
        }
        else
        {
            list = list.Where(v => v.Length > 0).Distinct(StringComparer.Ordinal).ToList();
            if (Max is { } max)
            {
                list = list.Take(Math.Max(0, max)).ToList();
            }
        }
        _values.Clear();
        _values.AddRange(list);
        ChangedEvent?.Invoke();
    }

    /// <summary>Restores the default values.</summary>
    public void Reset()
    {
        if (!Enabled) return;
        _values.Clear();
        _values.AddRange(_defaultValues);
        ChangedEvent?.Invoke();
    }
}

/// <summary>Metadata for a named dashboard view tab.</summary>
public sealed class DashboardViewMeta
{
    private string? _badge;
    private bool _enabled = true;

    /// <summary>Raised when <see cref="Badge"/> or <see cref="Enabled"/> changes, so that the controller can pick another current view and tell the page to render again.</summary>
    internal event Action? MetaChanged;

    /// <summary>Identifier of the view, the value of <see cref="DashboardController.CurrentView"/> when it is shown.</summary>
    public required string Key { get; init; }
    /// <summary>Text of the view tab.</summary>
    public required string Label { get; init; }
    /// <summary>Small text shown next to the label, such as a count. Hidden when empty or <c>0</c>. Setting it raises the controller's <c>Changed</c>, so the page renders again.</summary>
    public string? Badge
    {
        get => _badge;
        set
        {
            if (string.Equals(_badge, value, StringComparison.Ordinal)) return;
            _badge = value;
            MetaChanged?.Invoke();
        }
    }
    /// <summary>Disabled views are left out of <see cref="DashboardController.Views"/> and cannot become current. Disabling the current view makes the next enabled view current (wrapping to the first), and enabling a view when none is current makes the first enabled view current. Setting it raises the controller's <c>Changed</c>.</summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value) return;
            _enabled = value;
            MetaChanged?.Invoke();
        }
    }
    /// <summary><see langword="true"/> once the view has been current. Use it to render the content of a tab only after the first visit.</summary>
    public bool Opened { get; set; }
}

/// <summary>
/// Central controller for an analytical dashboard: coordinates filters, views, staged resources
/// (<c>DashboardQueryStage.Essential</c>, <c>DashboardQueryStage.Background</c>, <c>DashboardQueryStage.Deferred</c>)
/// and refresh. It has no UI dependency. Dispose it with the page that owns it: that stops <see cref="AutoRefresh"/> and cancels the loads in progress.
/// </summary>
public sealed class DashboardController : IDisposable
{
    private readonly List<DashboardFilterControl> _controls = [];
    private readonly List<DashboardViewMeta> _views = [];
    private readonly List<IDashboardSource> _resources = [];
    private readonly TimeProvider _timeProvider;
    private string? _currentView;
    private int _autoRefresh;
    private CancellationTokenSource? _autoRefreshLoop;
    private bool _disposed;

    /// <summary>Creates a controller.</summary>
    /// <param name="key">Identifier of the dashboard.</param>
    /// <param name="timeProvider">The clock of the <see cref="AutoRefresh"/> timer. <see cref="TimeProvider.System"/> when <see langword="null"/>; pass a fake one in tests.</param>
    public DashboardController(string key = "dashboard", TimeProvider? timeProvider = null)
    {
        Key = key;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Raised when a filter, a view or the current view, or a registered resource changes, and at the end of <see cref="RefreshAsync"/> and <see cref="ResetFilters"/>. It can be raised from any thread, and often several times for one change.</summary>
    public event Action? Changed;

    /// <summary>Identifier of the dashboard.</summary>
    public string Key { get; }
    /// <summary>The filters, in the order they were added.</summary>
    public IReadOnlyList<DashboardFilterControl> Controls => _controls;
    /// <summary>The enabled views, in the order they were added. A new list is built on every call.</summary>
    public IReadOnlyList<DashboardViewMeta> Views => _views.Where(v => v.Enabled).ToList();
    /// <summary>The registered resources, in the order they were registered.</summary>
    public IReadOnlyList<IDashboardSource> Resources => _resources;

    /// <summary>Seconds between two automatic calls to <see cref="RefreshAsync"/>, or 0 (the default) for none. A positive value starts a timer and 0 or a negative value stops it. A tick waits for the refresh it started to finish, so refreshes started by the timer never overlap. The timer resumes on the synchronization context that set the value, if any, and stops when the controller is disposed. Setting it on a disposed controller does nothing. A refresh that throws does not stop the timer.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is larger than <see cref="MaxAutoRefreshSeconds"/>.</exception>
    public int AutoRefresh
    {
        get => _autoRefresh;
        set
        {
            var seconds = Math.Max(0, value);
            // A page can set it after it was disposed, for example when the user leaves during the first load: do nothing.
            if (_disposed) return;
            ArgumentOutOfRangeException.ThrowIfGreaterThan(seconds, MaxAutoRefreshSeconds, nameof(value));
            if (seconds == _autoRefresh && _autoRefreshLoop is not null) return;
            _autoRefresh = seconds;
            StopAutoRefresh();
            if (seconds > 0)
            {
                var loop = new CancellationTokenSource();
                _autoRefreshLoop = loop;
                _ = RunAutoRefreshAsync(TimeSpan.FromSeconds(seconds), loop);
            }
        }
    }

    /// <summary>The key of the view being shown. Setting a key that is unknown or disabled is ignored. Setting a valid key marks the view as opened and raises <see cref="Changed"/>. The first enabled view added becomes current, and when the current view is disabled the next enabled one takes its place.</summary>
    public string? CurrentView
    {
        get => _currentView;
        set
        {
            var target = _views.FirstOrDefault(v => v.Key == value && v.Enabled);
            if (target is null) return;
            _currentView = target.Key;
            target.Opened = true;
            Changed?.Invoke();
        }
    }

    /// <summary><see langword="true"/> when at least one filter differs from its default values.</summary>
    public bool Filtered => _controls.Any(c => c.Changed);
    /// <summary><see langword="true"/> while any resource is loading or refreshing.</summary>
    public bool Refreshing => _resources.Any(r => r.Refreshing || r.Fetching);

    /// <summary>The oldest <see cref="IDashboardSource.UpdatedAt"/> among the resources that have one, or <see langword="null"/> when none has loaded.</summary>
    public DateTimeOffset? UpdatedAt
    {
        get
        {
            var dates = _resources.Where(r => r.UpdatedAt.HasValue).Select(r => r.UpdatedAt!.Value).ToList();
            return dates.Count > 0 ? dates.Min() : null;
        }
    }

    /// <summary>The combined state of the <see cref="DashboardQueryStage.Essential"/> resources, computed by <see cref="CombineStates"/>. Background and deferred resources are ignored.</summary>
    public DashboardResourceState State =>
        CombineStates(_resources.Where(r => r.Stage == DashboardQueryStage.Essential).Select(r => r.State));

    /// <summary>Adds a filter and forwards its changes, including changes of <see cref="DashboardFilterControl.Enabled"/> and <see cref="DashboardFilterControl.Items"/>, to <see cref="Changed"/>.</summary>
    /// <param name="filter">The filter to add.</param>
    /// <returns>The same filter.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="filter"/> is <see langword="null"/>.</exception>
    public DashboardFilterControl AddFilter(DashboardFilterControl filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        filter.ChangedEvent += () => Changed?.Invoke();
        filter.MetaChanged += () => Changed?.Invoke();
        _controls.Add(filter);
        return filter;
    }

    /// <summary>Adds a view tab. The first enabled view added becomes the current one. Later changes of its <c>Badge</c> and <c>Enabled</c> raise <see cref="Changed"/>.</summary>
    /// <param name="key">Identifier of the view.</param>
    /// <param name="label">Text of the tab.</param>
    /// <param name="badge">Small text shown next to the label.</param>
    /// <param name="enabled">Whether the view is available.</param>
    /// <returns>The view, which you can update later (badge, enabled).</returns>
    public DashboardViewMeta AddView(string key, string label, string? badge = null, bool enabled = true)
    {
        var meta = new DashboardViewMeta { Key = key, Label = label, Badge = badge, Enabled = enabled };
        meta.MetaChanged += OnViewChanged;
        _views.Add(meta);
        if (_currentView is null && enabled)
        {
            _currentView = key;
            meta.Opened = true;
        }
        return meta;
    }

    /// <summary>Registers a resource so that <see cref="RefreshAsync"/> loads it, its changes are forwarded to <see cref="Changed"/> and <see cref="Dispose"/> disposes it.</summary>
    /// <typeparam name="TData">The type of the resource data.</typeparam>
    /// <param name="resource">The resource to register.</param>
    /// <returns>The same resource.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="resource"/> is <see langword="null"/>.</exception>
    public DashboardResource<TData> RegisterResource<TData>(DashboardResource<TData> resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        resource.Changed += () => Changed?.Invoke();
        _resources.Add(resource);
        return resource;
    }

    /// <summary>Restores the default values of every enabled filter that is not headless.</summary>
    public void ResetFilters()
    {
        foreach (var control in _controls)
        {
            if (control.Enabled && !control.Headless)
            {
                control.Reset();
            }
        }
        Changed?.Invoke();
    }

    /// <summary>Loads all <see cref="DashboardQueryStage.Essential"/> resources in parallel, waits for them, then loads the <see cref="DashboardQueryStage.Background"/> resources and the already active <see cref="DashboardQueryStage.Deferred"/> ones in parallel. A resource still loading from an earlier call is cancelled and loaded again. After <see cref="Dispose"/> it does nothing.</summary>
    /// <returns>A task that completes when both stages have settled. Loader failures do not throw: they end up in <see cref="IDashboardSource.Error"/>.</returns>
    public Task RefreshAsync() => LoadStagesAsync(keepRestored: false);

    /// <summary>
    /// The first load of a page: like <see cref="RefreshAsync"/>, except that a resource whose data was handed over by an earlier render
    /// (<see cref="IDashboardPersistentSource.Restored"/>) keeps it instead of loading again. Call it once where the page loads its data,
    /// and <see cref="RefreshAsync"/> for every later reload. Without restored data, it is the same as <see cref="RefreshAsync"/>.
    /// </summary>
    /// <returns>A task that completes when both stages have settled.</returns>
    public Task LoadAsync() => LoadStagesAsync(keepRestored: true);

    private async Task LoadStagesAsync(bool keepRestored)
    {
        if (_disposed) return;
        Task Load(IDashboardSource r) => keepRestored && r is IDashboardFirstLoad first ? first.LoadKeepingRestoredAsync() : r.RefreshAsync();
        var essentials = _resources.Where(r => r.Stage == DashboardQueryStage.Essential).ToList();
        await Task.WhenAll(essentials.Select(Load));
        var backgrounds = _resources.Where(r => r.Stage == DashboardQueryStage.Background).ToList();
        var activeDeferred = _resources.Where(r => r.Stage == DashboardQueryStage.Deferred && r.Active).ToList();
        await Task.WhenAll(backgrounds.Concat(activeDeferred).Select(Load));
        if (_disposed) return;
        Changed?.Invoke();
    }

    /// <summary>
    /// A text that changes when a filter value changes: the keys and values of the filters. <c>DashboardPersistence</c> stores it with the
    /// handed-over data and refuses the data when the next render starts from other filter values.
    /// </summary>
    public string FilterFingerprint()
    {
        var text = new StringBuilder();
        foreach (var control in _controls)
        {
            text.Append(control.Key.Length).Append(':').Append(control.Key).Append('=');
            // Each value is written with its length, so no value can be mistaken for two.
            foreach (var value in control.Values) text.Append(value.Length).Append(':').Append(value);
            text.Append(';');
        }
        return text.ToString();
    }

    /// <summary>Stops the <see cref="AutoRefresh"/> timer and disposes the registered resources that are disposable, which cancels their loads. Later calls to <see cref="RefreshAsync"/> do nothing.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopAutoRefresh();
        _autoRefresh = 0;
        foreach (var resource in _resources)
        {
            (resource as IDisposable)?.Dispose();
        }
    }

    /// <summary>The largest <see cref="AutoRefresh"/> interval, one day in seconds.</summary>
    public const int MaxAutoRefreshSeconds = 86_400;

    private async Task RunAutoRefreshAsync(TimeSpan interval, CancellationTokenSource loop)
    {
        var cancellationToken = loop.Token;
        using var timer = new PeriodicTimer(interval, _timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                try
                {
                    await RefreshAsync();
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // A failing source or Changed subscriber must not stop the timer; the next tick tries again.
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // AutoRefresh was changed or the controller was disposed.
        }
        finally
        {
            if (ReferenceEquals(_autoRefreshLoop, loop)) _autoRefreshLoop = null;
            loop.Dispose();
        }
    }

    private void StopAutoRefresh()
    {
        var loop = _autoRefreshLoop;
        _autoRefreshLoop = null;
        if (loop is null) return;
        try
        {
            loop.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The loop already ended.
        }
    }

    private void OnViewChanged()
    {
        var current = _views.FirstOrDefault(v => v.Key == _currentView);
        if (current is not { Enabled: true })
        {
            DashboardViewMeta? next;
            if (current is null)
            {
                next = _views.FirstOrDefault(v => v.Enabled);
            }
            else
            {
                var index = _views.IndexOf(current);
                next = _views.Skip(index + 1).Concat(_views.Take(index)).FirstOrDefault(v => v.Enabled);
            }
            _currentView = next?.Key;
            if (next is not null)
            {
                next.Opened = true;
            }
        }
        Changed?.Invoke();
    }

    /// <summary>Combines several states into one. Disabled states are ignored. No state gives <see cref="DashboardResourceState.Ready"/>, only disabled states give <see cref="DashboardResourceState.Disabled"/>, then any error wins, then any loading, then all idle gives <see cref="DashboardResourceState.Idle"/>, otherwise <see cref="DashboardResourceState.Ready"/>.</summary>
    /// <param name="states">The states to combine.</param>
    /// <returns>The combined state.</returns>
    public static DashboardResourceState CombineStates(IEnumerable<DashboardResourceState> states)
    {
        var list = states.ToList();
        if (list.Count == 0) return DashboardResourceState.Ready;
        var active = list.Where(s => s != DashboardResourceState.Disabled).ToList();
        if (active.Count == 0) return DashboardResourceState.Disabled;
        if (active.Contains(DashboardResourceState.Error)) return DashboardResourceState.Error;
        if (active.Contains(DashboardResourceState.Loading)) return DashboardResourceState.Loading;
        if (active.All(s => s == DashboardResourceState.Idle)) return DashboardResourceState.Idle;
        return DashboardResourceState.Ready;
    }
}

/// <summary>Tabular view and CSV export representation of a dashboard block.</summary>
/// <param name="Key">The key of the cell in each row.</param>
/// <param name="Label">The column header.</param>
/// <param name="Numeric">Whether cells hold numbers: right-aligned in the table view, and written unquoted by the CSV export when they read as a number. The ready-made blocks write them with <see cref="DashboardFormat.FormatInvariant"/>.</param>
public sealed record DashboardDataColumn(string Key, string Label, bool Numeric = false);

/// <summary>Tabular representation of a dashboard block used by "View as table" and "Download CSV".</summary>
/// <param name="Columns">The columns, in display order.</param>
/// <param name="Rows">The rows: the text of each cell by column key, <see langword="null"/> for an empty cell. The ready-made blocks put raw invariant-culture numbers in numeric columns (<c>12600</c>, not <c>12.6K</c>) and the rows they show, after their <c>Limit</c>.</param>
public sealed record DashboardDataTable(
    IReadOnlyList<DashboardDataColumn> Columns,
    IReadOnlyList<IReadOnlyDictionary<string, string?>> Rows);

/// <summary>The words <see cref="DashboardFormat.FormatRelativeTime(DateTimeOffset?, DateTimeOffset?, DashboardRelativeTimeTexts)"/> uses. A new instance holds the English words.</summary>
public sealed record DashboardRelativeTimeTexts
{
    /// <summary>The English words, the ones <see cref="DashboardFormat.FormatRelativeTime(DateTimeOffset?, DateTimeOffset?)"/> uses.</summary>
    public static DashboardRelativeTimeTexts English { get; } = new();

    /// <summary>A moment under 45 seconds ago.</summary>
    public string JustNow { get; init; } = "just now";

    /// <summary>A moment less than 45 minutes ago, given the number of minutes (at least 1).</summary>
    public Func<int, string> MinutesAgo { get; init; } = n => $"{n} min ago";

    /// <summary>A moment less than 22 hours ago, given the number of hours (at least 1).</summary>
    public Func<int, string> HoursAgo { get; init; } = n => $"{n} hr ago";

    /// <summary>A moment about a day ago.</summary>
    public string Yesterday { get; init; } = "yesterday";

    /// <summary>A moment less than a week ago, given the number of days.</summary>
    public Func<int, string> DaysAgo { get; init; } = n => $"{n} days ago";
}

/// <summary>Number, delta, relative-time, comparison-range and CSV helpers. Numbers use the invariant culture unless you pass one; the components pass the format culture of Klyroo.UI.</summary>
public static class DashboardFormat
{
    /// <summary>The placeholder shown for a missing, <c>NaN</c> or infinite number: an em dash.</summary>
    public const string MissingValue = "—";

    /// <summary>Formats a number with a preset. A <see langword="null"/>, <c>NaN</c> or infinite value gives <see cref="MissingValue"/>.</summary>
    /// <param name="value">The number to format.</param>
    /// <param name="preset">The format to apply.</param>
    /// <param name="culture">The culture for separators. The invariant culture when <see langword="null"/>, not the current culture.</param>
    /// <returns>The formatted text.</returns>
    public static string Format(double? value, DashboardFormatPreset preset = DashboardFormatPreset.Number, CultureInfo? culture = null)
    {
        if (value is null || double.IsNaN(value.Value) || double.IsInfinity(value.Value))
        {
            return MissingValue;
        }
        var v = value.Value;
        var c = culture ?? CultureInfo.InvariantCulture;
        return preset switch
        {
            DashboardFormatPreset.Integer => Math.Round(v).ToString("#,0", c),
            DashboardFormatPreset.Decimal => v.ToString("#,0.#", c),
            DashboardFormatPreset.Compact => FormatCompact(v, c),
            DashboardFormatPreset.Percent => v.ToString("0.#", c) + "%",
            DashboardFormatPreset.Ratio => (v * 100.0).ToString("0.#", c) + "%",
            DashboardFormatPreset.Delta => (v > 0 ? "+" : "") + v.ToString("0.#", c) + "%",
            DashboardFormatPreset.Points => (v > 0 ? "+" : "") + v.ToString("0.#", c) + (Math.Abs(Math.Abs(v) - 1.0) < 0.0001 ? " pt" : " pts"),
            DashboardFormatPreset.Signed => (v > 0 ? "+" : "") + v.ToString("#,0.#", c),
            _ => Math.Abs(v) >= 10_000 ? FormatCompact(v, c) : v.ToString("#,0.#", c),
        };
    }

    /// <summary>Formats a number with the <c>K</c>, <c>M</c> and <c>B</c> suffixes and at most one decimal. The suffix is chosen after rounding, so <c>999,999</c> gives <c>1M</c>, not <c>1000K</c>.</summary>
    /// <param name="value">The number to format.</param>
    /// <param name="culture">The culture for separators. The invariant culture when <see langword="null"/>.</param>
    /// <returns>The formatted text, or <see cref="MissingValue"/>.</returns>
    public static string FormatCompact(double? value, CultureInfo? culture = null)
    {
        if (value is null || double.IsNaN(value.Value) || double.IsInfinity(value.Value)) return MissingValue;
        var c = culture ?? CultureInfo.InvariantCulture;
        string[] suffixes = ["", "K", "M", "B"];
        var tier = 0;
        var scaled = value.Value;
        while (tier < suffixes.Length - 1 && Math.Abs(scaled) >= 1_000)
        {
            scaled /= 1_000;
            tier++;
        }
        var rounded = Math.Round(scaled, 1, MidpointRounding.AwayFromZero);
        if (tier < suffixes.Length - 1 && Math.Abs(rounded) >= 1_000)
        {
            rounded = Math.Round(rounded / 1_000, 1, MidpointRounding.AwayFromZero);
            tier++;
        }
        return rounded.ToString("0.#", c) + suffixes[tier];
    }

    /// <summary>Writes a number as the cells of a <see cref="DashboardDataTable"/> carry it: in the invariant culture, without grouping and with every significant digit (<c>12600</c>, <c>-4.25</c>), so that it keeps full precision. <see cref="ExportCsv"/> writes it with a decimal comma for the cultures that use one.</summary>
    /// <param name="value">The number to write.</param>
    /// <returns>The text, or <see langword="null"/> for a <see langword="null"/>, <c>NaN</c> or infinite value (an empty cell).</returns>
    public static string? FormatInvariant(double? value)
    {
        if (value is null || double.IsNaN(value.Value) || double.IsInfinity(value.Value)) return null;
        return value.Value.ToString("R", CultureInfo.InvariantCulture);
    }

    /// <summary>Formats an amount with the currency symbol in front and at most two decimals. The symbol position does not follow the culture.</summary>
    /// <param name="value">The amount.</param>
    /// <param name="currencySymbol">The text put before the amount.</param>
    /// <param name="culture">The culture for separators. The invariant culture when <see langword="null"/>.</param>
    /// <returns>The formatted text, or <see cref="MissingValue"/>.</returns>
    public static string FormatCurrency(double? value, string currencySymbol = "€", CultureInfo? culture = null)
    {
        if (value is null || double.IsNaN(value.Value) || double.IsInfinity(value.Value)) return MissingValue;
        var c = culture ?? CultureInfo.InvariantCulture;
        return $"{currencySymbol}{value.Value.ToString("#,0.##", c)}";
    }

    /// <summary>Describes how long ago something happened: <c>just now</c> under 45 seconds, then minutes, hours, <c>yesterday</c>, days under a week, and an ISO date (<c>yyyy-MM-dd</c>) beyond that. The text is English: pass <see cref="DashboardRelativeTimeTexts"/> to the overload for another language.</summary>
    /// <param name="value">The moment to describe.</param>
    /// <param name="now">The reference moment. The current UTC time when <see langword="null"/>.</param>
    /// <returns>The text, or an empty string when <paramref name="value"/> is <see langword="null"/>.</returns>
    public static string FormatRelativeTime(DateTimeOffset? value, DateTimeOffset? now = null) =>
        FormatRelativeTime(value, now, DashboardRelativeTimeTexts.English);

    /// <summary>Describes how long ago something happened like <see cref="FormatRelativeTime(DateTimeOffset?, DateTimeOffset?)"/>, with the wording of <paramref name="texts"/>. Past a week the text is still an ISO date.</summary>
    /// <param name="value">The moment to describe.</param>
    /// <param name="now">The reference moment. The current UTC time when <see langword="null"/>.</param>
    /// <param name="texts">The words to use, for example those of another language.</param>
    /// <returns>The text, or an empty string when <paramref name="value"/> is <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="texts"/> is <see langword="null"/>.</exception>
    public static string FormatRelativeTime(DateTimeOffset? value, DateTimeOffset? now, DashboardRelativeTimeTexts texts)
    {
        ArgumentNullException.ThrowIfNull(texts);
        if (value is null) return "";
        var reference = now ?? DateTimeOffset.UtcNow;
        var diff = reference - value.Value;
        if (diff.TotalSeconds < 45) return texts.JustNow;
        if (diff.TotalMinutes < 45) return texts.MinutesAgo(Math.Max(1, (int)Math.Round(diff.TotalMinutes)));
        if (diff.TotalHours < 22) return texts.HoursAgo(Math.Max(1, (int)Math.Round(diff.TotalHours)));
        if (diff.TotalDays < 1.5) return texts.Yesterday;
        if (diff.TotalDays < 7) return texts.DaysAgo((int)Math.Round(diff.TotalDays));
        return value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    /// <summary>Computes the comparison range of a date range. <see cref="DashboardComparisonMode.Previous"/> gives the range of the same number of days that ends the day before the start. <see cref="DashboardComparisonMode.Year"/> shifts both bounds back one year, clamping February 29.</summary>
    /// <param name="range">The current range, inclusive of both days.</param>
    /// <param name="comparison">The comparison mode.</param>
    /// <returns>The comparison range, or <see langword="null"/> when <paramref name="range"/> is <see langword="null"/> or the mode is <see cref="DashboardComparisonMode.None"/>.</returns>
    public static (DateTimeOffset Start, DateTimeOffset End)? ResolveComparisonRange(
        (DateTimeOffset Start, DateTimeOffset End)? range,
        DashboardComparisonMode comparison)
    {
        if (range is null || comparison == DashboardComparisonMode.None) return null;
        var (start, end) = range.Value;
        if (comparison == DashboardComparisonMode.Year)
        {
            return (ShiftYearBack(start), ShiftYearBack(end));
        }

        var days = Math.Max(1, (int)Math.Round((end.UtcDateTime.Date - start.UtcDateTime.Date).TotalDays) + 1);
        return (start.AddDays(-days), start.AddDays(-1));
    }

    private static DateTimeOffset ShiftYearBack(DateTimeOffset dt)
    {
        var year = dt.Year - 1;
        var day = Math.Min(dt.Day, DateTime.DaysInMonth(year, dt.Month));
        return new DateTimeOffset(year, dt.Month, day, dt.Hour, dt.Minute, dt.Second, dt.Offset);
    }

    /// <summary>Turns a title into a file name: lowercase, accents removed, anything that is not a letter or digit collapsed into one hyphen. Falls back to <c>dashboard</c> when nothing is left.</summary>
    /// <param name="title">The block title.</param>
    /// <param name="extension">The extension, without the dot.</param>
    /// <returns>The file name, for example <c>monthly-revenue.csv</c>.</returns>
    public static string ResolveFileName(string? title, string extension = "csv")
    {
        if (string.IsNullOrWhiteSpace(title)) return $"dashboard.{extension}";
        var formD = title.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        var prevHyphen = false;
        foreach (var ch in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
                prevHyphen = false;
            }
            else if (!prevHyphen && sb.Length > 0)
            {
                sb.Append('-');
                prevHyphen = true;
            }
        }
        var slug = sb.ToString().Trim('-');
        return string.IsNullOrEmpty(slug) ? $"dashboard.{extension}" : $"{slug}.{extension}";
    }

    /// <summary>Serializes a table to CSV. The text starts with a UTF-8 byte order mark, uses <c>;</c> as delimiter when the culture has a comma as decimal separator and <c>,</c> otherwise, and ends every line, the header included, with <c>\r\n</c>. Headers and text cells are quoted. A cell of a numeric column that reads as a number is written bare, with a decimal comma instead of the invariant point when the delimiter is <c>;</c>, unless it contains the delimiter, a quote or a line break, in which case it is quoted too. Any other cell that starts with <c>=</c>, <c>+</c>, <c>-</c>, <c>@</c>, a tab or a carriage return gets a leading apostrophe, so that a spreadsheet does not run it as a formula.</summary>
    /// <param name="table">The table to export.</param>
    /// <param name="culture">The culture that picks the delimiter and, with the invariant culture, decides whether a numeric cell reads as a number. The invariant culture when <see langword="null"/>.</param>
    /// <returns>The CSV text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="table"/> is <see langword="null"/>.</exception>
    public static string ExportCsv(DashboardDataTable table, CultureInfo? culture = null)
    {
        ArgumentNullException.ThrowIfNull(table);
        var c = culture ?? CultureInfo.InvariantCulture;
        var delimiter = c.NumberFormat.NumberDecimalSeparator == "," ? ';' : ',';
        var sb = new StringBuilder();
        sb.Append('\uFEFF');

        sb.Append(string.Join(delimiter, table.Columns.Select(col => QuoteCsv(NeutralizeFormula(col.Label))))).Append("\r\n");
        foreach (var row in table.Rows)
        {
            var cells = new List<string>(table.Columns.Count);
            foreach (var col in table.Columns)
            {
                row.TryGetValue(col.Key, out var val);
                if (val is null)
                {
                    cells.Add("");
                }
                else if (col.Numeric && IsNumber(val, c))
                {
                    // A spreadsheet that reads ';' files (decimal-comma cultures) reads "4,25" as a number and "4.25" as text.
                    var number = delimiter == ';' && InvariantPlainNumber(val) ? val.Replace('.', ',') : val;
                    cells.Add(NeedsQuoting(number, delimiter) ? QuoteCsv(number) : number);
                }
                else
                {
                    cells.Add(QuoteCsv(NeutralizeFormula(val)));
                }
            }
            sb.Append(string.Join(delimiter, cells)).Append("\r\n");
        }
        return sb.ToString();
    }

    private static bool InvariantPlainNumber(string text) =>
        text.Length > 0 && text.AsSpan().TrimStart('-').Length > 0
        && text.AsSpan().TrimStart('-').IndexOfAnyExcept("0123456789.") < 0
        && text.Count(ch => ch == '.') <= 1;

    private static bool IsNumber(string text, CultureInfo culture) =>
        double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _)
        || double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, culture, out _);

    private static string NeutralizeFormula(string text) =>
        text.Length > 0 && text[0] is '=' or '+' or '-' or '@' or '\t' or '\r' ? "'" + text : text;

    private static bool NeedsQuoting(string text, char delimiter) =>
        text.Contains(delimiter) || text.AsSpan().IndexOfAny('"', '\r', '\n') >= 0;

    private static string QuoteCsv(string text) =>
        $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}

/// <summary>Status pill shown on a <c>KlyDashboardStat</c> or <c>DashboardStatsItem</c>.</summary>
/// <param name="Label">The text of the pill.</param>
/// <param name="Color">A semantic color name such as <c>success</c>, <c>warning</c>, <c>error</c>, <c>info</c> or <c>primary</c>; neutral when <see langword="null"/>. It must not be empty.</param>
public sealed record DashboardStatus(string Label, string? Color = null);

/// <summary>One metric entry inside <c>KlyDashboardStats</c>.</summary>
/// <param name="Key">Identifier of the metric.</param>
/// <param name="Label">The metric name.</param>
/// <param name="Value">The value, or <see langword="null"/> to show a dash.</param>
/// <param name="Format">How the value is formatted.</param>
/// <param name="Delta">The change in percent, shown next to the value.</param>
/// <param name="InvertDelta">When set, a negative delta is shown as good news (for churn or cost).</param>
/// <param name="Caption">A line of text under the value.</param>
/// <param name="Progress">A progress bar from 0 to 100.</param>
/// <param name="Status">A status pill.</param>
public sealed record DashboardStatsItem(
    string Key,
    string Label,
    double? Value,
    DashboardFormatPreset Format = DashboardFormatPreset.Number,
    double? Delta = null,
    bool InvertDelta = false,
    string? Caption = null,
    double? Progress = null,
    DashboardStatus? Status = null);

/// <summary>One definition row inside <c>KlyDashboardDetails</c>.</summary>
/// <param name="Key">Identifier of the row.</param>
/// <param name="Label">The term.</param>
/// <param name="Value">The text of the definition. An empty value is shown as the missing-value placeholder.</param>
/// <param name="Hint">A line of help text under the value.</param>
/// <param name="Mono">Whether the value uses a monospaced font.</param>
/// <param name="Copy">Whether a button after the value copies it to the clipboard. It needs an interactive render mode.</param>
/// <param name="Href">When set, the value is a link.</param>
/// <param name="FullSpan">Whether the row spans both columns on wide screens.</param>
/// <param name="Hidden">Whether the row is left out.</param>
public sealed record DashboardDetailsItem(
    string Key,
    string Label,
    string? Value,
    string? Hint = null,
    bool Mono = false,
    bool Copy = false,
    string? Href = null,
    bool FullSpan = false,
    bool Hidden = false);

/// <summary>One horizontal bar entry in <c>KlyDashboardBars</c>.</summary>
/// <param name="Label">The name of the bar.</param>
/// <param name="Value">The value that sets the bar length.</param>
/// <param name="Tag">A small monospaced tag next to the label.</param>
/// <param name="Meta">A text shown instead of the formatted value.</param>
/// <param name="Selected">Marks the row as selected (<c>data-selected</c>).</param>
public sealed record DashboardBarItem(
    string Label,
    double Value,
    string? Tag = null,
    string? Meta = null,
    bool Selected = false);

/// <summary>One ranked row in <c>KlyDashboardList</c>.</summary>
/// <param name="Label">The name of the row.</param>
/// <param name="Description">A line of text under the label.</param>
/// <param name="Value">The value, formatted with the list <c>Format</c>.</param>
/// <param name="Percent">A percentage shown before the value.</param>
/// <param name="Delta">A percent change, green when zero or positive and red when negative.</param>
/// <param name="LeadingText">Short text in a circle before the label, such as initials or a rank.</param>
/// <param name="Href">When set, the label is a link.</param>
/// <param name="Selected">Marks the row as selected (<c>data-selected</c>).</param>
public sealed record DashboardListItem(
    string Label,
    string? Description = null,
    double? Value = null,
    double? Percent = null,
    double? Delta = null,
    string? LeadingText = null,
    string? Href = null,
    bool Selected = false);

/// <summary>Column definition for <c>KlyDashboardTable</c>.</summary>
/// <param name="Key">The key of the cell in each row.</param>
/// <param name="Label">The column header.</param>
/// <param name="Type">One of <c>text</c>, <c>number</c>, <c>delta</c>, <c>percent</c> or <c>bar</c>. Any other value is treated as <c>text</c>.</param>
/// <param name="Format">The number format of <c>number</c> and <c>bar</c> cells. <c>delta</c> and <c>percent</c> cells use their own format.</param>
/// <param name="InvertDelta">When set, a negative <c>delta</c> is shown as good news.</param>
/// <param name="Sortable">Whether the header is a sort button.</param>
public sealed record DashboardTableColumn(
    string Key,
    string Label,
    string Type = "text",
    DashboardFormatPreset Format = DashboardFormatPreset.Number,
    bool InvertDelta = false,
    bool Sortable = true);

/// <summary>Active sort state of <c>KlyDashboardTable</c>.</summary>
/// <param name="Key">The key of the sorted column.</param>
/// <param name="Descending">Whether the order is descending.</param>
public sealed record DashboardTableSort(string Key, bool Descending);

/// <summary>One alert row in <c>KlyDashboardAlerts</c>.</summary>
/// <param name="Severity">The severity, which sets the badge and the order.</param>
/// <param name="Label">The title of the alert.</param>
/// <param name="Description">A line of text under the label.</param>
/// <param name="Value">A short value on the right, such as a count.</param>
/// <param name="ActionLabel">The text of the action on the right: a link when <paramref name="ActionHref"/> is set, a button when <c>KlyDashboardAlerts.OnAction</c> has a handler, otherwise nothing is rendered.</param>
/// <param name="ActionHref">The target of the action link.</param>
public sealed record DashboardAlertItem(
    DashboardAlertSeverity Severity,
    string Label,
    string? Description = null,
    string? Value = null,
    string? ActionLabel = null,
    string? ActionHref = null);

/// <summary>One timeline activity event in <c>KlyDashboardFeed</c>.</summary>
/// <param name="Label">What happened.</param>
/// <param name="Time">When it happened.</param>
/// <param name="Description">A line of text under the label.</param>
/// <param name="AvatarText">Short text in a circle, such as initials.</param>
public sealed record DashboardFeedItem(
    string Label,
    DateTimeOffset Time,
    string? Description = null,
    string? AvatarText = null);

/// <summary>One series definition for <c>KlyDashboardChart</c>.</summary>
/// <param name="Key">Identifier of the series, used as the column key in the table view and in the CSV export.</param>
/// <param name="Label">The name in the legend.</param>
/// <param name="Values">One value per category.</param>
/// <param name="Type"><c>bar</c> (the default) or <c>line</c>. In a chart whose <c>Type</c> is <c>bar</c>, a <c>line</c> series is drawn as a line over the bars. A <c>line</c> chart draws every series as a line and a <c>donut</c> chart draws the first series that is not a comparison, so this value only matters in bar charts.</param>
/// <param name="IsComparison">Marks a comparison series: drawn in a muted color (dashed when it is a line), dashed in the legend, and left out of the totals and of a donut.</param>
public sealed record DashboardChartSeries(
    string Key,
    string Label,
    IReadOnlyList<double> Values,
    string Type = "bar",
    bool IsComparison = false);
