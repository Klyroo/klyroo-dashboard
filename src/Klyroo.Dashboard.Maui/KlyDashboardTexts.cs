using System.Collections.Concurrent;
using System.Globalization;

namespace Klyroo.Dashboard.Maui;

/// <summary>
/// The built-in texts of the dashboard views (buttons, accessible names, empty and error states, relative times, column headers of the table view): one set per locale,
/// picked by the UI culture of the app (<see cref="CultureInfo.CurrentUICulture"/>, which <c>KlyCulture.Set</c> of Klyroo.UI.Maui changes). The sets are the same as the ones of the Blazor
/// package (<see cref="DashboardLocales"/>). Change one with <see cref="Configure"/> at startup.
/// </summary>
public static class KlyDashboardTexts
{
    private static readonly object Gate = new();
    private static readonly Dictionary<string, List<Func<DashboardTexts, DashboardTexts>>> Changes = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, DashboardTexts> Resolved = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The texts for a culture: the set of its code ("pt-BR"), else of its parent languages ("zh-CN" gets "zh-Hans", "pt-PT" gets "pt"), else English.</summary>
    /// <param name="culture">The culture. The UI culture of the current thread when <see langword="null"/>.</param>
    /// <returns>The texts, with the changes made by <see cref="Configure"/>.</returns>
    public static DashboardTexts Resolve(CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        return Resolved.GetOrAdd(culture.Name, static (_, c) => Compute(c), culture);
    }

    /// <summary>
    /// Changes the texts of a locale: <paramref name="change"/> receives the set the locale resolves to and returns the one to use, usually <c>t =&gt; t with { ... }</c>.
    /// Call it at startup, before the first view shows.
    /// </summary>
    /// <param name="localeCode">A code of <see cref="DashboardLocales.Tables"/> ("fr"), or the code of a culture without a set of its own ("en-AU").</param>
    /// <param name="change">Returns the texts to use.</param>
    /// <exception cref="ArgumentException"><paramref name="localeCode"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="localeCode"/> or <paramref name="change"/> is <see langword="null"/>.</exception>
    public static void Configure(string localeCode, Func<DashboardTexts, DashboardTexts> change)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localeCode);
        ArgumentNullException.ThrowIfNull(change);
        lock (Gate)
        {
            if (!Changes.TryGetValue(localeCode, out var list)) Changes[localeCode] = list = [];
            list.Add(change);
            Resolved.Clear();
        }
    }

    /// <summary>The code of the set a culture gets: its own code or the first of its parents that has a set, else <c>en</c>.</summary>
    internal static string CodeOf(CultureInfo culture)
    {
        var tables = DashboardLocales.Tables;
        for (var current = culture; current.Name.Length > 0; current = current.Parent)
        {
            foreach (var key in tables.Keys)
            {
                if (string.Equals(key, current.Name, StringComparison.OrdinalIgnoreCase)) return key;
            }
        }
        return "en";
    }

    private static DashboardTexts Compute(CultureInfo culture)
    {
        var code = CodeOf(culture);
        var texts = DashboardLocales.Tables[code];
        lock (Gate)
        {
            foreach (var key in new[] { code, culture.Name }.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!Changes.TryGetValue(key, out var changes)) continue;
                foreach (var change in changes) texts = change(texts) ?? throw new InvalidOperationException($"A change of the texts of '{key}' returned null.");
            }
        }
        return texts;
    }
}
