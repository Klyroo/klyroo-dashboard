namespace Klyroo.Dashboard.Maui;

/// <summary>The width classes of the web theme the dashboard uses: below <c>sm</c>, then <c>sm</c>, <c>md</c> and <c>lg</c> and up.</summary>
internal enum DashboardBreakpoint
{
    /// <summary>Under 640: a phone in portrait.</summary>
    Base,
    /// <summary>From 640.</summary>
    Sm,
    /// <summary>From 768.</summary>
    Md,
    /// <summary>From 1024.</summary>
    Lg,
}

/// <summary>The breakpoints of the web theme in device-independent units, and the layout choices the dashboard derives from them.</summary>
internal static class DashboardBreakpoints
{
    public const double Sm = 640;
    public const double Md = 768;
    public const double Lg = 1024;

    /// <summary>The class of a width. A width that is not known yet (a view that has not been laid out) is the widest class, the layout the web gives before it measures.</summary>
    public static DashboardBreakpoint Of(double width)
    {
        if (!(width > 0)) return DashboardBreakpoint.Lg;
        return width >= Lg ? DashboardBreakpoint.Lg : width >= Md ? DashboardBreakpoint.Md : width >= Sm ? DashboardBreakpoint.Sm : DashboardBreakpoint.Base;
    }

    /// <summary>
    /// The width the breakpoints of the web apply to: the viewport. The width of the page the element is on, else the width of the element. The Tailwind classes of the Blazor
    /// components read the width of the window, not the one of their container, and a native page has the same relation to its window.
    /// </summary>
    public static double ViewportWidth(Element element)
    {
        for (var current = element; current is not null; current = current.Parent)
        {
            if (current is Page { Width: > 0 } page) return page.Width;
        }
        return element is VisualElement { Width: > 0 } visual ? visual.Width : -1;
    }

    /// <summary>The padding of the page and of its header: <c>p-4 lg:p-7</c>.</summary>
    public static double PagePadding(double width) => Of(width) == DashboardBreakpoint.Lg ? 28 : 16;

    /// <summary>The columns of the metrics of <c>KlyDashboardStats</c>: <c>grid-cols-1 sm:grid-cols-2 lg:grid-cols-4</c>.</summary>
    public static int StatsColumns(double width) => Of(width) switch
    {
        DashboardBreakpoint.Lg => 4,
        DashboardBreakpoint.Base => 1,
        _ => 2,
    };

    /// <summary>The columns of the rows of <c>KlyDashboardDetails</c>: <c>grid-cols-1 sm:grid-cols-2</c>.</summary>
    public static int DetailsColumns(double width) => Of(width) == DashboardBreakpoint.Base ? 1 : 2;
}
