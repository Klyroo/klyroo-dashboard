using System.Collections.Concurrent;

namespace Klyroo.Dashboard;

/// <summary>
/// Options of Klyroo.Dashboard, set with <c>services.AddKlyrooDashboard(options =&gt; ...)</c>: the hand-over of prerendered data and the built-in
/// texts of each locale. It is an extension of <see cref="KlyrooOptions"/> (<see cref="KlyrooOptions.Extension{T}"/>), so configure it in code shared by
/// the server and the WebAssembly client.
/// </summary>
public sealed class KlyrooDashboardOptions
{
    /// <summary>
    /// The largest data, in bytes as sent, that one resource hands from the prerender to the interactive render through
    /// <see cref="DashboardPersistence"/> (8 KB by default). The size counts the escaping and the two base64 encodings Blazor adds, so the JSON
    /// of the data itself is about a third of it. A resource over it is not handed over: the interactive render loads it again once. All the
    /// resources, and the other Klyroo libraries of the page (Klyroo.Table lists), also share <c>KlyrooOptions.PersistedStateMaxBytes</c> (28 KB as
    /// sent by default): the message that starts an Interactive Server circuit carries all of it, and SignalR refuses it past 32 KB.
    /// </summary>
    public int PersistMaxBytes { get; set; } = 8 * 1024;

    /// <summary>
    /// The oldest handed-over data <see cref="DashboardPersistence"/> restores (5 minutes by default), measured from when it was loaded. A prerender
    /// hands its data over within seconds, but .NET can also persist the state of an Interactive Server circuit when it pauses and give it back
    /// when the circuit resumes, possibly much later: older data is dropped and the resource loads again.
    /// </summary>
    public TimeSpan PersistMaxAge { get; set; } = TimeSpan.FromMinutes(5);

    private readonly object _gate = new();
    private readonly Dictionary<string, List<Func<DashboardTexts, DashboardTexts>>> _changes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DashboardTexts> _resolved = new(StringComparer.OrdinalIgnoreCase);
    private volatile bool _frozen;

    /// <summary>
    /// Changes the texts of a locale, like <see cref="KlyLocales.Extend"/> does for Klyroo.UI: <paramref name="change"/> receives the set the
    /// locale resolves to and returns the one to use, usually <c>t =&gt; t with { ... }</c>. <paramref name="localeCode"/> is a code of
    /// <see cref="KlyLocales.All"/> ("fr"), or the code of a locale you added ("en-AU", "tlh"): a locale without a set of its own starts from
    /// the set <see cref="KlyLocales.ResolveCode"/> picks for it, then its changes apply. The texts become read-only the first time a
    /// component resolves one.
    /// </summary>
    /// <param name="localeCode">The code of the locale, compared ignoring case.</param>
    /// <param name="change">Receives the texts of the locale and returns the texts to use.</param>
    /// <returns>The same options, for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="localeCode"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="localeCode"/> or <paramref name="change"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">A component has already resolved a text.</exception>
    public KlyrooDashboardOptions Texts(string localeCode, Func<DashboardTexts, DashboardTexts> change)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localeCode);
        ArgumentNullException.ThrowIfNull(change);
        lock (_gate)
        {
            if (_frozen)
            {
                throw new InvalidOperationException($"{nameof(KlyrooDashboardOptions)}.{nameof(Texts)} cannot change once a component has resolved a text: the result of each locale is cached. Set it in AddKlyrooDashboard, before the app starts rendering.");
            }
            if (!_changes.TryGetValue(localeCode, out var list))
            {
                _changes[localeCode] = list = [];
            }
            list.Add(change);
        }
        return this;
    }

    /// <summary>The texts for a locale code (<see cref="KlyLocale.Code"/>), cached per code. The first call makes <see cref="Texts"/> read-only.</summary>
    internal DashboardTexts ResolveTexts(string localeCode)
    {
        // Read on every render: take the lock only the first time.
        if (!_frozen)
        {
            lock (_gate) _frozen = true;
        }
        return _resolved.GetOrAdd(localeCode, static (code, self) => self.Compute(code), this);
    }

    private DashboardTexts Compute(string localeCode)
    {
        var tables = DashboardLocales.Tables;
        var tableCode = KlyLocales.ResolveCode(localeCode, tables.Keys);
        var texts = tables[tableCode];
        foreach (var key in new[] { tableCode, localeCode }.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (_changes.TryGetValue(key, out var changes))
            {
                foreach (var change in changes) texts = change(texts) ?? throw new InvalidOperationException($"A change of the texts of '{key}' returned null.");
            }
        }
        return texts;
    }
}
