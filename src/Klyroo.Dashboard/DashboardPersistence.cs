using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Klyroo.Dashboard;

/// <summary>
/// Hands the data the prerender of a page loaded to its interactive render, so the resources do not load twice. In <c>OnInitializedAsync</c>,
/// register the resources, set the filters, call <see cref="Attach"/>, then load with <see cref="DashboardController.LoadAsync"/>: in the
/// prerender the ready resources are written into the persistent component state of the page, and in the interactive render they take that
/// data back, which <see cref="DashboardController.LoadAsync"/> keeps instead of loading again. Only resources created with a <c>json</c>
/// type info take part (see <see cref="IDashboardPersistentSource"/>).
/// </summary>
/// <remarks>
/// Registered as a scoped service by <c>AddKlyrooDashboard</c>. Where Blazor has no persistent component state (a .NET MAUI Blazor Hybrid app),
/// <see cref="Attach"/> does nothing. The data is written as text by the <c>JsonTypeInfo</c> of the app, so no reflection is involved, and it
/// is refused when the filters of the interactive render differ from those of the prerender, or when it is older than
/// <see cref="KlyrooDashboardOptions.PersistMaxAge"/> (a Server circuit that .NET resumed long after it paused).
/// </remarks>
public sealed partial class DashboardPersistence
{
    private readonly PersistentComponentState? _state;
    private readonly KlyrooDashboardOptions _options;
    private readonly ILogger<DashboardPersistence>? _logger;
    private readonly HashSet<string> _attached = new(StringComparer.Ordinal);
    private readonly KlyPersistedStateBudget _budget;

    /// <summary>Creates the service. Use the instance <c>AddKlyrooDashboard</c> registers rather than creating one.</summary>
    /// <param name="services">The services of the scope, where the persistent component state is looked up.</param>
    public DashboardPersistence(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        _state = services.GetService<PersistentComponentState>();
        // The options AddKlyrooDashboard configures: the extension of the Klyroo.UI options, the one the components read too.
        _options = services.GetService<KlyrooOptions>()?.Extension<KlyrooDashboardOptions>() ?? new KlyrooDashboardOptions();
        _logger = services.GetService<ILogger<DashboardPersistence>>();
        _budget = services.GetService<KlyPersistedStateBudget>() ?? new KlyPersistedStateBudget(services.GetService<KlyrooOptions>() ?? new KlyrooOptions());
    }

    /// <summary>
    /// Restores the resources of <paramref name="dashboard"/> that the previous render handed over, and hands them over at the end of this render.
    /// Call it after registering the resources and setting the filters: a resource registered later is handed over but never restored, and data
    /// written under other filter values is refused.
    /// </summary>
    /// <param name="dashboard">The controller of the page.</param>
    /// <param name="renderMode">
    /// The render mode the page runs in next: its <c>AssignedRenderMode</c> when the page declares <c>@rendermode</c>, or the mode of the app
    /// (<c>RenderMode.InteractiveServer</c>, ...) when the whole app is interactive and the page has none of its own. With <see langword="null"/>
    /// nothing is handed over: Blazor needs a render mode to know which runtime receives the data.
    /// </param>
    /// <returns>Stops handing the data over: dispose it with the page.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dashboard"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Another controller with the same key is attached in this scope: give each dashboard of a page its own key.</exception>
    public IDisposable Attach(DashboardController dashboard, IComponentRenderMode? renderMode)
    {
        ArgumentNullException.ThrowIfNull(dashboard);
        if (_state is null) return Detached.Instance;
        if (!_attached.Add(dashboard.Key))
        {
            throw new InvalidOperationException($"Two dashboards with the key '{dashboard.Key}' are attached to {nameof(DashboardPersistence)} in one page: give each DashboardController its own key.");
        }
        try
        {
            var filters = dashboard.FilterFingerprint();
            foreach (var source in dashboard.Resources.OfType<IDashboardPersistentSource>())
            {
                // Every entry is taken, even one that is refused, so nothing is left for another page of the circuit.
                if (TryTake(StateKey(dashboard, source)) is { } text && Parse(text) is var (updatedAt, written, json) && written == filters
                    && updatedAt is { } at && DateTimeOffset.UtcNow - at <= _options.PersistMaxAge)
                {
                    source.RestoreJson(json, updatedAt);
                }
            }
            if (renderMode is null) return new Subscription(this, dashboard.Key, null);
            var subscription = _state.RegisterOnPersisting(() =>
            {
                Persist(dashboard);
                return Task.CompletedTask;
            }, renderMode);
            return new Subscription(this, dashboard.Key, subscription);
        }
        catch
        {
            _attached.Remove(dashboard.Key);
            throw;
        }
    }

    private static string StateKey(DashboardController dashboard, IDashboardSource source) => "kly-dash:" + dashboard.Key + ":" + source.Key;

    private void Persist(DashboardController dashboard)
    {
        var filters = dashboard.FilterFingerprint();
        var written = new HashSet<string>(StringComparer.Ordinal);
        foreach (var source in dashboard.Resources.OfType<IDashboardPersistentSource>())
        {
            var key = StateKey(dashboard, source);
            // Two resources under one key would overwrite each other's data: the first one wins.
            if (!written.Add(key)) continue;
            try
            {
                if (source.ExportJson() is not { } json) continue;
                var text = Format(source.UpdatedAt ?? DateTimeOffset.UtcNow, filters, json);
                var bytes = KlyPersistedStateBudget.SentBytes(key, text);
                // Within the limit of one resource, then within what the Klyroo libraries of the page have left of their shared budget.
                if (bytes > _options.PersistMaxBytes || !_budget.TryReserve(bytes))
                {
                    if (_logger is not null) LogTooLarge(_logger, dashboard.Key, source.Key, bytes);
                    continue;
                }
                Write(key, text);
            }
            catch (Exception e) when (e is not OutOfMemoryException)
            {
                // Blazor drops the state of the whole page when a persisting callback throws, so nothing escapes here (a custom converter can throw anything).
                // The data of this resource is not written; the others still are, and the interactive render loads this one again.
                if (_logger is not null) LogNotWritten(_logger, dashboard.Key, source.Key, e);
            }
        }
    }

    // "2026-11-26T10:00:00.0000000+00:00\n42:<filters>{...}": when the data was loaded, the filters it was loaded with (prefixed by their length), then the data.
    private static string Format(DateTimeOffset updatedAt, string filters, string json) =>
        updatedAt.ToString("O", CultureInfo.InvariantCulture) + "\n" + filters.Length.ToString(CultureInfo.InvariantCulture) + ":" + filters + json;

    private static (DateTimeOffset? UpdatedAt, string Filters, string Json)? Parse(string text)
    {
        var newline = text.IndexOf('\n');
        if (newline < 0) return null;
        var colon = text.IndexOf(':', newline + 1);
        if (colon < 0 || !int.TryParse(text.AsSpan(newline + 1, colon - newline - 1), NumberStyles.None, CultureInfo.InvariantCulture, out var length)) return null;
        if (length > text.Length - colon - 1) return null;
        DateTimeOffset? updatedAt = DateTimeOffset.TryParseExact(text[..newline], "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var at) ? at : null;
        return (updatedAt, text.Substring(colon + 1, length), text[(colon + 1 + length)..]);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Only strings go through the reflection-based serializer of PersistentComponentState, and string has a built-in converter; the data itself is read with the JsonTypeInfo of the app.")]
    private string? TryTake(string key) => _state!.TryTakeFromJson<string>(key, out var text) ? text : null;

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Only strings go through the reflection-based serializer of PersistentComponentState, and string has a built-in converter; the data itself is written with the JsonTypeInfo of the app.")]
    private void Write(string key, string text) => _state!.PersistAsJson(key, text);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The data of resource '{Resource}' of dashboard '{Dashboard}' (about {Bytes} bytes once sent) is over KlyrooDashboardOptions.PersistMaxBytes or what is left of the budget the Klyroo libraries of the page share (KlyrooOptions.PersistedStateMaxBytes): the interactive render loads it again.")]
    private static partial void LogTooLarge(ILogger logger, string dashboard, string resource, long bytes);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The data of resource '{Resource}' of dashboard '{Dashboard}' could not be written: the interactive render loads it again.")]
    private static partial void LogNotWritten(ILogger logger, string dashboard, string resource, Exception exception);

    private sealed class Subscription(DashboardPersistence owner, string key, PersistingComponentStateSubscription? inner) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            inner?.Dispose();
            // The page is gone: the key is free for the next one in this scope (a circuit navigating to the same page again).
            owner._attached.Remove(key);
        }
    }

    private sealed class Detached : IDisposable
    {
        public static readonly Detached Instance = new();

        public void Dispose()
        {
        }
    }
}
