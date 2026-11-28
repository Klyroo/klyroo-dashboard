namespace Klyroo.Dashboard;

/// <summary>
/// The built-in texts of Klyroo.Dashboard, one set per locale of <c>KlyLocales.All</c> and keyed by the same codes. A component picks the
/// set of its current <c>KlyLocale.Code</c> with <c>KlyLocales.ResolveCode</c>, so "pt-PT" gets "pt" and an unknown language gets
/// English. Change a set with <c>KlyrooDashboardOptions.Texts</c>.
/// </summary>
public static partial class DashboardLocales
{
    private static IReadOnlyDictionary<string, DashboardTexts>? _tables;

    /// <summary>Every set by locale code: the codes of <c>KlyLocales.All</c>.</summary>
    public static IReadOnlyDictionary<string, DashboardTexts> Tables => _tables ??= new Dictionary<string, DashboardTexts>(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = En,
        ["fr"] = Fr,
        ["de"] = De,
        ["es"] = Es,
        ["it"] = It,
        ["pt"] = Pt,
        ["pt-BR"] = PtBr,
        ["nl"] = Nl,
        ["ja"] = Ja,
        ["zh-Hans"] = ZhCn,
        ["ar"] = Ar,
        ["ru"] = Ru,
    };

    /// <summary>The codes whose texts were reviewed by a native speaker. The other sets are drafts waiting for review.</summary>
    public static IReadOnlyList<string> ReviewedCodes { get; } = ["en", "fr"];

    /// <summary>English.</summary>
    public static DashboardTexts En { get; } = new();

    // Russian plural forms: one (1, 21, 31...), few (2-4, 22-24...), many (the rest, 11-14 included).
    internal static string RussianPlural(int n, string one, string few, string many)
    {
        var mod10 = Math.Abs(n) % 10;
        var mod100 = Math.Abs(n) % 100;
        if (mod10 == 1 && mod100 != 11) return one;
        return mod10 is >= 2 and <= 4 && mod100 is < 12 or > 14 ? few : many;
    }

    // Arabic plural forms after a number: the singular for 1, 2 and 11-99 (the counted noun stays singular), the plural for 0 and 3-10.
    internal static string ArabicPlural(int n, string singular, string plural)
    {
        var mod100 = Math.Abs(n) % 100;
        return mod100 is >= 3 and <= 10 || n == 0 ? plural : singular;
    }
}
