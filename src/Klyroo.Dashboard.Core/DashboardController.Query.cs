using System.Globalization;

namespace Klyroo.Dashboard;

public sealed partial class DashboardController
{
    private const int MaxQueryValueLength = 512;

    /// <summary>
    /// Writes the filters that differ from their default values as a URL query string, without the leading <c>?</c>: <c>region=EMEA&amp;region=APAC&amp;period=2026-01-01&amp;period=2026-01-31</c>.
    /// Each value is its own <c>key=value</c> pair, in the order of the filters and of their values, and every key and value is percent-encoded, so the same
    /// filters always give the same text. Filters at their default values, disabled filters and <see cref="DashboardFilterControl.Headless"/> filters are left out;
    /// a filter whose selection was cleared is written as <c>key=</c>. The method changes nothing, uses no reflection and no clock.
    /// </summary>
    /// <param name="prefix">Put before every key (<c>sales.</c>), so two dashboards can share one query string. <see langword="null"/> for none.</param>
    /// <returns>The query string, empty when every filter is at its default.</returns>
    public string ToQuery(string? prefix = null) =>
        string.Join('&', QueryPairs(prefix ?? "").Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

    /// <summary>
    /// Sets the filters from a URL query string, the inverse of <see cref="ToQuery"/>. The query describes every filter that is not headless and enabled: one
    /// that is not in it returns to its default values, so applying the query of a URL always gives the state of that URL. A value that does not fit its filter
    /// (a number that is not a number, a date that is not <c>yyyy-MM-dd</c>, a <see cref="DashboardFilterKind.Boolean"/> that is not <c>true</c> or <c>false</c>, a
    /// <see cref="DashboardFilterKind.Comparison"/> that is not a <see cref="DashboardComparisonMode"/> name, a text longer than 512 characters, an option that is not
    /// among the loaded <see cref="DashboardFilterControl.Items"/> of an <see cref="DashboardFilterKind.Options"/> or <see cref="DashboardFilterKind.Enum"/>
    /// filter) is ignored, so a hand-edited URL cannot put an unexpected value in a filter. A filter is only touched when its selection changes, so
    /// nothing is raised for a query that matches the current state, and applying the same query twice is the same as once. The method
    /// does not reload any resource: call <see cref="RefreshAsync"/> or <see cref="LoadAsync"/> afterwards, or let the subscribers of
    /// <see cref="DashboardFilterControl.ChangedEvent"/> do it. It uses no reflection.
    /// </summary>
    /// <param name="query">The query string with or without the leading <c>?</c>, and with or without a <c>#fragment</c>. <see langword="null"/> or empty applies the defaults.</param>
    /// <param name="prefix">The prefix <see cref="ToQuery"/> was given. <see langword="null"/> for none.</param>
    /// <returns><see langword="true"/> when at least one filter changed.</returns>
    public bool ApplyQuery(string? query, string? prefix = null)
    {
        var fromUrl = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var segment in QuerySegments(query))
        {
            var (name, value) = SplitSegment(segment);
            if (!fromUrl.TryGetValue(name, out var list))
            {
                fromUrl[name] = list = [];
            }
            list.Add(value);
        }

        var changed = false;
        foreach (var control in _controls)
        {
            if (control.Headless || !control.Enabled) continue;
            List<string>? wanted = null;
            if (fromUrl.TryGetValue((prefix ?? "") + control.Key, out var given))
            {
                wanted = Normalize(control, given);
            }
            if (wanted is null)
            {
                // Not in the URL, or not a value this filter takes: the default selection.
                if (control.Changed)
                {
                    control.Reset();
                    changed = true;
                }
            }
            else if (!control.Values.SequenceEqual(wanted, StringComparer.Ordinal))
            {
                control.SetValues(wanted);
                changed = true;
            }
        }
        return changed;
    }

    /// <summary>
    /// Replaces the filters of this controller in an existing query string with <see cref="ToQuery"/> and keeps every other pair as it was, so a page can write
    /// its filters into a URL that also carries a search term or a page number. Headless filters are not the controller's: a pair with the key of one is kept.
    /// </summary>
    /// <param name="existingQuery">The query string of the URL with or without the leading <c>?</c> and with or without a <c>#fragment</c>. <see langword="null"/> for none.</param>
    /// <param name="prefix">The prefix <see cref="ToQuery"/> is given. <see langword="null"/> for none.</param>
    /// <returns>The new query string without the leading <c>?</c>, empty when nothing remains.</returns>
    public string MergeQuery(string? existingQuery, string? prefix = null)
    {
        var owned = _controls.Where(c => !c.Headless).Select(c => (prefix ?? "") + c.Key).ToHashSet(StringComparer.Ordinal);
        var parts = QuerySegments(existingQuery).Where(s => !owned.Contains(SplitSegment(s).Name)).ToList();
        var own = ToQuery(prefix);
        if (own.Length > 0) parts.Add(own);
        return string.Join('&', parts);
    }

    private IEnumerable<(string Key, string Value)> QueryPairs(string prefix)
    {
        foreach (var control in _controls)
        {
            // Changed is false for a disabled or headless filter.
            if (!control.Changed) continue;
            var values = control.Values;
            if (values.Count == 0)
            {
                yield return (prefix + control.Key, "");
                continue;
            }
            foreach (var value in values)
            {
                yield return (prefix + control.Key, value);
            }
        }
    }

    private static IEnumerable<string> QuerySegments(string? query)
    {
        if (string.IsNullOrEmpty(query)) yield break;
        var text = query.AsSpan();
        var hash = text.IndexOf('#');
        if (hash >= 0) text = text[..hash];
        if (text.Length > 0 && text[0] == '?') text = text[1..];
        foreach (var part in text.ToString().Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            yield return part;
        }
    }

    private static (string Name, string Value) SplitSegment(string segment)
    {
        var equals = segment.IndexOf('=');
        return equals < 0 ? (Decode(segment), "") : (Decode(segment[..equals]), Decode(segment[(equals + 1)..]));
    }

    // A form writes a space as "+", and Uri.EscapeDataString as "%20": both are read as a space.
    private static string Decode(string text) => Uri.UnescapeDataString(text.Replace('+', ' '));

    // The selection a filter takes from the values of a URL, as SetValues would store it, or null when they do not fit the filter.
    private static List<string>? Normalize(DashboardFilterControl control, List<string> given)
    {
        if (given.Any(v => v.Length > MaxQueryValueLength)) return null;
        if (given.All(v => v.Length == 0)) return [];

        switch (control.Kind)
        {
            case DashboardFilterKind.Number:
                return double.TryParse(given[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number)
                    ? [number.ToString("R", CultureInfo.InvariantCulture)]
                    : null;
            case DashboardFilterKind.Date:
                return IsIsoDate(given[0]) ? [given[0]] : null;
            case DashboardFilterKind.DateRange:
                var bounds = new[] { given[0], given.Count > 1 ? given[1] : "" };
                return bounds.All(b => b.Length == 0 || IsIsoDate(b)) ? [bounds[0], bounds[1]] : null;
            case DashboardFilterKind.Boolean:
                var flag = given[0].ToLowerInvariant();
                return flag is "true" or "false" ? [flag] : null;
            case DashboardFilterKind.Comparison:
                var mode = given[0].ToLowerInvariant();
                return mode is "none" or "previous" or "year" ? [mode] : null;
            case DashboardFilterKind.String:
            case DashboardFilterKind.Custom:
            case DashboardFilterKind.Remote:
                return Limit(control, given.Where(v => v.Length > 0));
            default:
                // Options and Enum: when the options are known, only they are accepted.
                var known = control.Items.Count > 0;
                return Limit(control, given.Where(v => v.Length > 0 && (!known || control.Items.Any(i => string.Equals(i.Value, v, StringComparison.Ordinal)))));
        }
    }

    private static List<string>? Limit(DashboardFilterControl control, IEnumerable<string> values)
    {
        var list = control.Multiple ? values.Distinct(StringComparer.Ordinal).ToList() : values.Take(1).ToList();
        if (control.Multiple && control.Max is { } max) list = list.Take(Math.Max(0, max)).ToList();
        return list.Count == 0 ? null : list;
    }

    private static bool IsIsoDate(string text) =>
        DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}
