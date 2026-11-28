namespace Klyroo.Dashboard.Maui;

/// <summary>The loading placeholders of a <see cref="KlyDashboardCard"/>, one shape per <see cref="DashboardSkeletonKind"/>, made of <see cref="KlySkeleton"/>.</summary>
internal static class DashboardSkeletons
{
    /// <summary>The height of the bar skeletons: <c>h-32</c>.</summary>
    public const double BarsHeight = 128;

    /// <summary>The placeholder of a kind. <c>Stat</c>, <c>Bars</c>, <c>Lines</c>, <c>Paired</c> and <c>Stack</c> have their own shape, the other kinds are three lines.</summary>
    public static View Build(DashboardSkeletonKind kind)
    {
        var body = new VerticalStackLayout { Spacing = 10 };
        switch (kind)
        {
            case DashboardSkeletonKind.Stat:
                body.Add(new KlySkeleton { HeightRequest = 32, WidthRequest = 112, HorizontalOptions = LayoutOptions.Start });
                body.Add(new KlySkeleton { HeightRequest = 16, WidthRequest = 80, HorizontalOptions = LayoutOptions.Start });
                break;
            case DashboardSkeletonKind.Bars:
            case DashboardSkeletonKind.Lines:
                body.Add(Columns(8, [0.5, 0.75, 2.0 / 3.0, 1.0]));
                break;
            case DashboardSkeletonKind.Paired:
            {
                var groups = new Grid { ColumnSpacing = 16, HeightRequest = BarsHeight };
                double[][] heights = [[2.0 / 3.0, 0.5], [1.0, 0.75], [0.5, 2.0 / 3.0]];
                for (var i = 0; i < heights.Length; i++)
                {
                    groups.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                    groups.Add(Columns(4, heights[i]), i);
                }
                body.Add(groups);
                break;
            }
            case DashboardSkeletonKind.Stack:
                body.Add(Row(0.5, 0.25, 1.0 / 6.0));
                body.Add(Row(0.4, 1.0 / 3.0, 0.2));
                body.Add(Row(1.0 / 3.0, 1.0 / 3.0, 0.25));
                break;
            default:
                body.Add(Row(0.75));
                body.Add(Row(1));
                body.Add(Row(2.0 / 3.0));
                break;
        }
        AutomationProperties.SetIsInAccessibleTree(body, false);
        return body;
    }

    // Vertical bars along the bottom of a box of BarsHeight, each as high as its fraction.
    private static Grid Columns(double spacing, double[] heights)
    {
        var grid = new Grid { ColumnSpacing = spacing, HeightRequest = BarsHeight };
        for (var i = 0; i < heights.Length; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            grid.Add(new KlySkeleton { HeightRequest = BarsHeight * heights[i], VerticalOptions = LayoutOptions.End }, i);
        }
        return grid;
    }

    // A line of skeletons that take the given fractions of the width, side by side with a hairline between them; the rest stays empty.
    private static Grid Row(params double[] fractions)
    {
        const double height = 16;
        var grid = new Grid { ColumnSpacing = 1, HeightRequest = height };
        var used = 0.0;
        for (var i = 0; i < fractions.Length; i++)
        {
            used += fractions[i];
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(fractions[i], GridUnitType.Star)));
            grid.Add(new KlySkeleton { HeightRequest = height }, i);
        }
        if (used < 0.9999) grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1 - used, GridUnitType.Star)));
        return grid;
    }
}
