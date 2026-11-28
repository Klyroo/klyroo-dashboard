namespace Klyroo.Dashboard;

/// <summary>
/// The built-in texts of the dashboard components: toolbar and card buttons, accessible names, empty and error states, column headers of the
/// table view and the CSV export, and relative times. There is one set per locale of Klyroo.UI (<see cref="DashboardLocales"/>), and a
/// component shows the set of its current locale, the one its <c>Messages</c> come from. Change a set with
/// <c>KlyrooDashboardOptions.Texts</c> in <c>AddKlyrooDashboard(o =&gt; ...)</c>.
/// </summary>
/// <remarks>
/// A text that contains a number or a name is a function, so a language can choose its own plural forms and word order.
/// Build a variant with a <c>with</c> expression. A new instance holds the English texts. Text you pass to the components
/// (titles, labels, values, filter options) is never translated.
/// </remarks>
/// <example>
/// <code>
/// builder.Services.AddKlyrooDashboard(o =&gt; o.Texts("fr", t =&gt; t with
/// {
///     Refresh = "Actualiser",
///     RelativeMinutes = n =&gt; $"il y a {n} min",
/// }));
/// </code>
/// </example>
public sealed record DashboardTexts
{
    /// <summary>Creates the texts with their English defaults.</summary>
    public DashboardTexts()
    {
    }

    // Page: header, toolbar

    /// <summary>The refresh button of the page (<c>"Refresh"</c>), also its accessible name.</summary>
    public string Refresh { get; init; } = "Refresh";

    /// <summary>The refresh button of the page while a load is running (<c>"Refreshing…"</c>).</summary>
    public string Refreshing { get; init; } = "Refreshing…";

    /// <summary>The freshness line of the page and of the cards, given the relative time (<c>"Updated 5 min ago"</c>).</summary>
    public Func<string, string> Updated { get; init; } = relative => $"Updated {relative}";

    /// <summary>The accessible name of the view tabs (<c>"Views"</c>).</summary>
    public string Views { get; init; } = "Views";

    /// <summary>The accessible name of the filter toolbar (<c>"Filters"</c>).</summary>
    public string Filters { get; init; } = "Filters";

    /// <summary>The button that restores every filter (<c>"Reset"</c>).</summary>
    public string ResetFilters { get; init; } = "Reset";

    /// <summary>The placeholder of the search box of a filter menu (<c>"Search…"</c>).</summary>
    public string SearchPlaceholder { get; init; } = "Search…";

    /// <summary>The accessible name of the search box of a filter menu, given the filter label (<c>"Search Region"</c>).</summary>
    public Func<string, string> SearchFilter { get; init; } = label => $"Search {label}";

    /// <summary>The accessible name of the button that clears a filter, given its label (<c>"Clear Region"</c>).</summary>
    public Func<string, string> ClearFilter { get; init; } = label => $"Clear {label}";

    /// <summary>The accessible name of the start box of a date range filter, given its label (<c>"Period from"</c>).</summary>
    public Func<string, string> FilterFrom { get; init; } = label => $"{label} from";

    /// <summary>The accessible name of the end box of a date range filter, given its label (<c>"Period to"</c>).</summary>
    public Func<string, string> FilterTo { get; init; } = label => $"{label} to";

    // Card

    /// <summary>The button that shows the block as a table (<c>"View as table"</c>).</summary>
    public string ViewAsTable { get; init; } = "View as table";

    /// <summary>The button that returns from the table view to the block (<c>"Hide table"</c>).</summary>
    public string HideTable { get; init; } = "Hide table";

    /// <summary>The button that saves the block as a CSV file (<c>"Download CSV"</c>).</summary>
    public string DownloadCsv { get; init; } = "Download CSV";

    /// <summary>The button that enlarges the block in a dialog (<c>"Expand"</c>).</summary>
    public string Expand { get; init; } = "Expand";

    /// <summary>The button that closes the enlarged block (<c>"Collapse"</c>).</summary>
    public string Collapse { get; init; } = "Collapse";

    /// <summary>The accessible name of the button that removes a filter chip of a card, given the filter label (<c>"Remove Region"</c>).</summary>
    public Func<string, string> RemoveFilter { get; init; } = label => $"Remove {label}";

    /// <summary>The title of the message shown when a resource failed to load (<c>"This data could not be loaded"</c>).</summary>
    public string ErrorTitle { get; init; } = "This data could not be loaded";

    /// <summary>The text of the failed-load message when the resource gives no message of its own.</summary>
    public string ErrorMessage { get; init; } = "The rest of the dashboard is unaffected. Try again in a moment.";

    /// <summary>The button that loads a failed resource again (<c>"Retry"</c>).</summary>
    public string Retry { get; init; } = "Retry";

    /// <summary>The title of an empty block (<c>"No data for this period"</c>).</summary>
    public string NoData { get; init; } = "No data for this period";

    /// <summary>The accessible name of the group of buttons that select a data point of the compact chart drawing (<c>"Select a data point"</c>); the drawing of <c>KlyChart</c> makes its own bars, points and slices selectable instead.</summary>
    public string SelectPoint { get; init; } = "Select a data point";

    // Blocks

    /// <summary>The title of an empty Alerts block (<c>"Nothing needs attention"</c>).</summary>
    public string NothingNeedsAttention { get; init; } = "Nothing needs attention";

    /// <summary>The badge of an error alert (<c>"Critical"</c>).</summary>
    public string SeverityCritical { get; init; } = "Critical";

    /// <summary>The badge of a warning alert (<c>"Warning"</c>).</summary>
    public string SeverityWarning { get; init; } = "Warning";

    /// <summary>The badge of an informational alert (<c>"Info"</c>).</summary>
    public string SeverityInfo { get; init; } = "Info";

    /// <summary>The badge of a resolved alert (<c>"Resolved"</c>).</summary>
    public string SeverityResolved { get; init; } = "Resolved";

    /// <summary>The accessible name of a Gauge without a title, and its label in the table view (<c>"Gauge"</c>).</summary>
    public string Gauge { get; init; } = "Gauge";

    /// <summary>The target line of a Gauge, given the formatted target (<c>"Target: 80%"</c>).</summary>
    public Func<string, string> Target { get; init; } = value => $"Target: {value}";

    /// <summary>The goal line of a Stat, given the formatted goal (<c>"Goal 1,000"</c>).</summary>
    public Func<string, string> Goal { get; init; } = value => $"Goal {value}";

    /// <summary>The caption of a Stat compared with a previous value, given the formatted value (<c>"vs 900 previous period"</c>).</summary>
    public Func<string, string> VersusPrevious { get; init; } = value => $"vs {value} previous period";

    /// <summary>The note of the first step of a funnel (<c>"base 100%"</c>).</summary>
    public string FunnelBase { get; init; } = "base 100%";

    /// <summary>The note of a later step of a funnel, given its share of the previous step in percent (<c>"60% of the previous step"</c>).</summary>
    public Func<int, string> FunnelStep { get; init; } = percent => $"{percent}% of the previous step";

    /// <summary>The day heading of a Feed for the current day (<c>"Today"</c>).</summary>
    public string Today { get; init; } = "Today";

    /// <summary>The day heading of a Feed for the previous day (<c>"Yesterday"</c>).</summary>
    public string Yesterday { get; init; } = "Yesterday";

    /// <summary>The accessible name of the copy button of a Details row, given the row label (<c>"Copy Order id"</c>).</summary>
    public Func<string, string> CopyValue { get; init; } = label => $"Copy {label}";

    /// <summary>The tooltip of the copy button of a Details row (<c>"Copy"</c>).</summary>
    public string Copy { get; init; } = "Copy";

    // Relative times (the freshness lines and the times of a Feed)

    /// <summary>A moment under 45 seconds ago (<c>"just now"</c>).</summary>
    public string RelativeJustNow { get; init; } = "just now";

    /// <summary>A moment less than 45 minutes ago, given the minutes (<c>"5 min ago"</c>).</summary>
    public Func<int, string> RelativeMinutes { get; init; } = n => $"{n} min ago";

    /// <summary>A moment less than 22 hours ago, given the hours (<c>"3 hr ago"</c>).</summary>
    public Func<int, string> RelativeHours { get; init; } = n => $"{n} hr ago";

    /// <summary>A moment about a day ago (<c>"yesterday"</c>).</summary>
    public string RelativeYesterday { get; init; } = "yesterday";

    /// <summary>A moment less than a week ago, given the days (<c>"3 days ago"</c>).</summary>
    public Func<int, string> RelativeDays { get; init; } = n => $"{n} days ago";

    // Column headers of the table view and the CSV export

    /// <summary>The first column of a chart table (<c>"Category"</c>).</summary>
    public string ColumnCategory { get; init; } = "Category";

    /// <summary>The name column of the table view of Bars, List, Stat, Stats and Gauge (<c>"Label"</c>).</summary>
    public string ColumnLabel { get; init; } = "Label";

    /// <summary>The value column (<c>"Value"</c>).</summary>
    public string ColumnValue { get; init; } = "Value";

    /// <summary>The percent change column (<c>"Change (%)"</c>).</summary>
    public string ColumnChangePercent { get; init; } = "Change (%)";

    /// <summary>The change column of a Stat whose change is neither a percent nor points (<c>"Change"</c>).</summary>
    public string ColumnChange { get; init; } = "Change";

    /// <summary>The change column of a Stat compared in points (<c>"Change (pts)"</c>).</summary>
    public string ColumnChangePoints { get; init; } = "Change (pts)";

    /// <summary>The previous value column of a Stat (<c>"Previous"</c>).</summary>
    public string ColumnPrevious { get; init; } = "Previous";

    /// <summary>The goal column of a Stat (<c>"Goal"</c>).</summary>
    public string ColumnGoal { get; init; } = "Goal";

    /// <summary>The minimum column of a Gauge (<c>"Minimum"</c>).</summary>
    public string ColumnMinimum { get; init; } = "Minimum";

    /// <summary>The maximum column of a Gauge (<c>"Maximum"</c>).</summary>
    public string ColumnMaximum { get; init; } = "Maximum";

    /// <summary>The target column of a Gauge (<c>"Target"</c>).</summary>
    public string ColumnTarget { get; init; } = "Target";

    /// <summary>The percent column of a List (<c>"Percent (%)"</c>).</summary>
    public string ColumnPercent { get; init; } = "Percent (%)";

    /// <summary>The description column of List, Feed and Alerts (<c>"Description"</c>).</summary>
    public string ColumnDescription { get; init; } = "Description";

    /// <summary>The time column of a Feed (<c>"Time"</c>).</summary>
    public string ColumnTime { get; init; } = "Time";

    /// <summary>The event column of a Feed (<c>"Event"</c>).</summary>
    public string ColumnEvent { get; init; } = "Event";

    /// <summary>The severity column of Alerts (<c>"Severity"</c>).</summary>
    public string ColumnSeverity { get; init; } = "Severity";

    /// <summary>The alert column of Alerts (<c>"Alert"</c>).</summary>
    public string ColumnAlert { get; init; } = "Alert";

    /// <summary>The relative times as the <c>DashboardFormat.FormatRelativeTime</c> overload takes them.</summary>
    public DashboardRelativeTimeTexts RelativeTime => new()
    {
        JustNow = RelativeJustNow,
        MinutesAgo = RelativeMinutes,
        HoursAgo = RelativeHours,
        Yesterday = RelativeYesterday,
        DaysAgo = RelativeDays,
    };
}
