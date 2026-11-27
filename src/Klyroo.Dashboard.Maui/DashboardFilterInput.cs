using System.Globalization;

namespace Klyroo.Dashboard.Maui;

/// <summary>What the boxes of a filter write into a <see cref="DashboardFilterControl"/>, which stores every selection as strings.</summary>
internal static class DashboardFilterInput
{
    /// <summary>The format of a stored date.</summary>
    public const string DateFormat = "yyyy-MM-dd";

    /// <summary>Whether a text is an ISO date (<c>yyyy-MM-dd</c>).</summary>
    public static bool IsIsoDate(string? text) =>
        DateOnly.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    /// <summary>The date of a stored text, or <see langword="null"/> when it is empty or not an ISO date.</summary>
    public static DateTime? ParseDate(string? text) =>
        DateOnly.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date.ToDateTime(TimeOnly.MinValue) : null;

    /// <summary>The stored text of a date.</summary>
    public static string FormatDate(DateTime date) => date.ToString(DateFormat, CultureInfo.InvariantCulture);

    /// <summary>
    /// Writes the text of a box into a text, number or date filter. An empty text clears the filter. A number is read in the culture of the device, then in the invariant one,
    /// and stored invariantly (<c>1234.5</c>); text that is not a number is ignored. A date is stored only when it is an ISO date.
    /// </summary>
    public static void SetInputValue(DashboardFilterControl filter, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            filter.Value = null;
            return;
        }
        switch (filter.Kind)
        {
            case DashboardFilterKind.Number:
                if (TryParseNumber(text, out var number)) filter.Value = number.ToString("R", CultureInfo.InvariantCulture);
                break;
            case DashboardFilterKind.Date:
                if (IsIsoDate(text)) filter.Value = text;
                break;
            default:
                filter.Value = text;
                break;
        }
    }

    /// <summary>Reads a number the user typed: in the culture of the device, then in the invariant culture. NaN and infinity are not numbers.</summary>
    public static bool TryParseNumber(string text, out double number) =>
        (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out number) || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number)) && double.IsFinite(number);

    /// <summary>One bound of a date range: <paramref name="index"/> 0 is the start, 1 the end. An open bound is an empty string.</summary>
    public static string RangeBound(DashboardFilterControl filter, int index) => filter.Values.Count > index ? filter.Values[index] : "";

    /// <summary>Changes one bound of a date range and keeps the other. A value that is not an ISO date opens the bound.</summary>
    public static void SetRangeBound(DashboardFilterControl filter, int index, string? value)
    {
        var bound = IsIsoDate(value) ? value! : "";
        var start = index == 0 ? bound : RangeBound(filter, 0);
        var end = index == 1 ? bound : RangeBound(filter, 1);
        filter.SetValues([start, end]);
    }
}
