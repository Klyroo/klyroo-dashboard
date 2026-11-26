namespace Klyroo.Dashboard;

/// <summary>Page shell of a dashboard: a header with the title and a refresh button, a sticky toolbar with the view tabs and the filter controls of a <see cref="DashboardController"/> (option lists, text, number and date inputs, or your own template, depending on the filter kind), and a body for the blocks. It re-renders when the controller changes. Buttons and inputs need an interactive render mode.</summary>
public partial class KlyDashboardPage;

/// <summary>A grid of equal columns (12 by default, see <c>Columns</c>) that places blocks by their <c>Span</c>, and gives them its <c>Menu</c> and <c>Freshness</c> defaults.</summary>
public partial class KlyDashboardGrid;

/// <summary>The frame shared by every block: header with title and menu, loading skeleton, error with a retry button, empty state, content, chips for active filters and a footer. Use it directly to build a custom block.</summary>
public partial class KlyDashboardCard;

/// <summary>A single metric with an optional delta, sparkline, goal bar and status pill.</summary>
public partial class KlyDashboardStat;

/// <summary>Several metrics side by side, each with an optional delta, progress bar and status pill.</summary>
public partial class KlyDashboardStats;

/// <summary>A list of alerts ordered by severity, each with an optional value and an action link or button.</summary>
public partial class KlyDashboardAlerts;

/// <summary>Horizontal bars for a ranking, or a funnel with the conversion rate between steps.</summary>
public partial class KlyDashboardBars;

/// <summary>A chart over categories without a charting dependency: grouped bars, lines, bars with line series over them, or a donut of the first series, with a legend and optional totals.</summary>
public partial class KlyDashboardChart;

/// <summary>A definition list of labelled values, with optional hints, links and copy buttons.</summary>
public partial class KlyDashboardDetails;

/// <summary>A timeline of events with relative times, grouped by day.</summary>
public partial class KlyDashboardFeed;

/// <summary>A gauge that shows a value between a minimum and a maximum, as a ring or a horizontal bar.</summary>
public partial class KlyDashboardGauge;

/// <summary>A ranked list of rows with an optional value, percentage and delta.</summary>
public partial class KlyDashboardList;

/// <summary>A table over rows of your own type, with typed columns (text, number, delta, percent, bar) and click-to-sort headers.</summary>
public partial class KlyDashboardTable<TRow>;
