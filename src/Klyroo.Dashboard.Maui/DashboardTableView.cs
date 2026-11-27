namespace Klyroo.Dashboard.Maui;

/// <summary>The "View as table" body of a <see cref="KlyDashboardCard"/>: a <see cref="DashboardDataTable"/> as a grid of labels that scrolls sideways when it is wider than the card.</summary>
internal static class DashboardTableView
{
    /// <summary>The header cells and the rows of a table. Numbers are right-aligned, an empty cell shows the missing-value dash.</summary>
    public static View Build(DashboardDataTable table)
    {
        var grid = new Grid { ColumnSpacing = 0, RowSpacing = 0 };
        AddColumns(grid, table.Columns.Count);
        // The first row is the header, then a divider under it and between the rows.
        var headerRow = NextRow(grid);
        for (var c = 0; c < table.Columns.Count; c++)
        {
            var column = table.Columns[c];
            grid.Add(Cell(column.Label, column.Numeric, KlyTheme.TextMuted, FontAttributes.Bold), c, headerRow);
        }
        AddDivider(grid, table.Columns.Count);
        for (var r = 0; r < table.Rows.Count; r++)
        {
            var row = NextRow(grid);
            for (var c = 0; c < table.Columns.Count; c++)
            {
                var column = table.Columns[c];
                table.Rows[r].TryGetValue(column.Key, out var value);
                grid.Add(Cell(value ?? DashboardFormat.MissingValue, column.Numeric, KlyTheme.Text, FontAttributes.None), c, row);
            }
            if (r < table.Rows.Count - 1) AddDivider(grid, table.Columns.Count);
        }
        return new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = grid };
    }

    /// <summary>
    /// The columns of a table: each as wide as its content, except the first, which takes what the card has left. A table narrower than the card then fills it, as <c>min-w-full</c> does on the web,
    /// and a wider one scrolls sideways.
    /// </summary>
    public static void AddColumns(Grid grid, int count)
    {
        for (var c = 0; c < count; c++) grid.ColumnDefinitions.Add(new ColumnDefinition(c == 0 ? GridLength.Star : GridLength.Auto));
    }

    /// <summary>A cell of 12 high text with the padding of the web table (<c>px-2.5 py-1.5</c>).</summary>
    public static Label Cell(string text, bool numeric, KlyThemeColor color, FontAttributes attributes)
    {
        var label = DashboardParts.NewLabel(12, color, attributes, LineBreakMode.NoWrap);
        label.Text = text;
        label.Padding = new Thickness(10, 6);
        label.HorizontalTextAlignment = numeric ? TextAlignment.End : TextAlignment.Start;
        label.VerticalTextAlignment = TextAlignment.Center;
        return label;
    }

    /// <summary>Adds a row of automatic height and returns its index.</summary>
    public static int NextRow(Grid grid)
    {
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        return grid.RowDefinitions.Count - 1;
    }

    /// <summary>Adds a hairline row across every column.</summary>
    public static void AddDivider(Grid grid, int columns)
    {
        var row = NextRow(grid);
        var divider = DashboardParts.Divider();
        grid.Add(divider, 0, row);
        grid.SetColumnSpan(divider, Math.Max(1, columns));
    }
}
