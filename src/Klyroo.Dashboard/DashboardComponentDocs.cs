namespace Klyroo.Dashboard;

/// <summary>Page shell of a dashboard: a header with the title and a refresh button, a sticky toolbar with the view tabs and the filter pills of a <see cref="DashboardController"/>, and a body for the blocks. It re-renders when the controller changes. Buttons need an interactive render mode.</summary>
public partial class KlyDashboardPage;

/// <summary>A 12-column grid that places blocks by their <c>Span</c>, and gives them its <c>Menu</c> and <c>Freshness</c> defaults.</summary>
public partial class KlyDashboardGrid;

/// <summary>The frame shared by every block: header with title and menu, loading skeleton, error with a retry button, empty state, content, chips for active filters and a footer. Use it directly to build a custom block.</summary>
public partial class KlyDashboardCard;

/// <summary>A single metric with an optional delta, sparkline, goal bar and status pill.</summary>
public partial class KlyDashboardStat;

/// <summary>Several metrics side by side, each with an optional delta, progress bar and status pill.</summary>
public partial class KlyDashboardStats;

/// <summary>A list of alerts ordered by severity, each with an optional value and action link.</summary>
public partial class KlyDashboardAlerts;

/// <summary>Horizontal bars for a ranking, or a funnel with the conversion rate between steps.</summary>
public partial class KlyDashboardBars;

/// <summary>A bar chart over categories, with a legend and optional totals. Only the first series is drawn.</summary>
public partial class KlyDashboardChart;

/// <summary>A definition list of labelled values, with optional hints and links.</summary>
public partial class KlyDashboardDetails;

/// <summary>A timeline of events with relative times, grouped by day.</summary>
public partial class KlyDashboardFeed;

/// <summary>A ring gauge that shows a value between a minimum and a maximum.</summary>
public partial class KlyDashboardGauge;

/// <summary>A ranked list of rows with an optional value, percentage and delta.</summary>
public partial class KlyDashboardList;

/// <summary>A table with typed columns (text, number, delta, percent, bar) and click-to-sort headers.</summary>
public partial class KlyDashboardTable;
