namespace Klyroo.Dashboard.Maui;

/// <summary>
/// A table over rows of your own type, with typed columns (text, number, delta, percent, bar) and tap-to-sort headers. It scrolls sideways when it is wider than the card.
/// Its columns are the ones of <see cref="DashboardTableColumn.For{TRow, TValue}"/>, the same as the Blazor table.
/// </summary>
/// <typeparam name="TRow">The row type.</typeparam>
public class KlyDashboardTable<TRow> : KlyDashboardCard
{
    /// <summary>Identifies <see cref="Columns"/>.</summary>
    public static readonly BindableProperty ColumnsProperty = Prop<IReadOnlyList<DashboardTableColumn<TRow>>?>(nameof(Columns), null);
    /// <summary>Identifies <see cref="Rows"/>.</summary>
    public static readonly BindableProperty RowsProperty = Prop<IReadOnlyList<TRow>?>(nameof(Rows), null);
    /// <summary>Identifies <see cref="Sort"/>.</summary>
    public static readonly BindableProperty SortProperty = Prop<DashboardTableSort?>(nameof(Sort), null);
    /// <summary>Identifies <see cref="Limit"/>.</summary>
    public static readonly BindableProperty LimitProperty = Prop<int?>(nameof(Limit), null);

    private EventHandler<TRow>? _rowClicked;

    /// <summary>Creates the block.</summary>
    public KlyDashboardTable()
    {
        Skeleton = DashboardSkeletonKind.Table;
        ViewAsTable = false;
        Tabulate = BuildDataTable;
    }

    /// <summary>Raised when the sort changes, with <see langword="null"/> when the sort is cleared.</summary>
    public event EventHandler<DashboardTableSort?>? SortChanged;

    /// <summary>
    /// Raised with the row (the item of <see cref="Rows"/>) when the user picks it. The whole row is then a button, so the choice works with a screen reader and a keyboard.
    /// Use it to drill down, for example by setting a filter of the <see cref="DashboardController"/>.
    /// </summary>
    public event EventHandler<TRow>? RowClicked
    {
        add
        {
            _rowClicked += value;
            InvalidateContent();
        }
        remove
        {
            _rowClicked -= value;
            InvalidateContent();
        }
    }

    /// <summary>The column definitions, in display order.</summary>
    public IReadOnlyList<DashboardTableColumn<TRow>>? Columns { get => (IReadOnlyList<DashboardTableColumn<TRow>>?)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }

    /// <summary>The rows. Each column reads its value from the row with its own delegate (<see cref="DashboardTableColumn.For{TRow, TValue}"/>), and a <see langword="null"/> value shows the missing-value placeholder.</summary>
    public IReadOnlyList<TRow>? Rows { get => (IReadOnlyList<TRow>?)GetValue(RowsProperty); set => SetValue(RowsProperty, value); }

    /// <summary>The active sort. The table updates it itself when a header is tapped, and <see cref="SortChanged"/> reports the change. Numeric columns sort descending first, then ascending, then unsorted. Empty cells always come last.</summary>
    public DashboardTableSort? Sort { get => (DashboardTableSort?)GetValue(SortProperty); set => SetValue(SortProperty, value); }

    /// <summary>The maximum number of rows shown, applied after sorting. The CSV export holds the same rows.</summary>
    public int? Limit { get => (int?)GetValue(LimitProperty); set => SetValue(LimitProperty, value); }

    /// <summary>The rows in the order of <see cref="Sort"/>. Rows without a value come last, whatever the direction, and the sort is stable.</summary>
    internal IReadOnlyList<TRow> SortedRows
    {
        get
        {
            if (Rows is not { Count: > 0 } rows) return [];
            if (Sort is not { } sort || Columns is null) return rows;
            var column = Columns.FirstOrDefault(c => c.Key == sort.Key);
            if (column is null) return rows;

            var comparer = Comparer<TRow>.Create((a, b) => sort.Descending ? -column.Compare(a, b) : column.Compare(a, b));
            var sorted = rows.Where(r => !column.IsMissing(r)).Order(comparer).ToList();
            sorted.AddRange(rows.Where(column.IsMissing));
            return sorted;
        }
    }

    /// <summary>The rows shown: the sorted rows, cut at <see cref="Limit"/>.</summary>
    internal IReadOnlyList<TRow> SortedAndLimitedRows => Limit is > 0 ? SortedRows.Take(Limit.Value).ToList() : SortedRows;

    /// <summary>Moves the sort on from a header: a new column sorts in its natural order (descending for numbers), the same column again reverses it, and once more clears it.</summary>
    internal void CycleSort(DashboardTableColumn<TRow> column)
    {
        var naturalDescending = column.IsNumeric;
        if (Sort?.Key != column.Key) Sort = new DashboardTableSort(column.Key, naturalDescending);
        else if (Sort.Descending == naturalDescending) Sort = new DashboardTableSort(column.Key, !naturalDescending);
        else Sort = null;
        SortChanged?.Invoke(this, Sort);
    }

    /// <inheritdoc />
    protected override void OnRefreshing()
    {
        var empty = Rows is not { Count: > 0 };
        if (IsEmpty != empty) IsEmpty = empty;
    }

    /// <inheritdoc />
    protected override View? BuildBody()
    {
        var grid = new Grid { ColumnSpacing = 0, RowSpacing = 0 };
        var columns = Columns ?? [];
        DashboardTableView.AddColumns(grid, columns.Count);

        var headerRow = DashboardTableView.NextRow(grid);
        for (var c = 0; c < columns.Count; c++) grid.Add(BuildHeader(columns[c]), c, headerRow);
        DashboardTableView.AddDivider(grid, columns.Count);

        var rows = SortedAndLimitedRows;
        for (var r = 0; r < rows.Count; r++)
        {
            var rowIndex = DashboardTableView.NextRow(grid);
            var row = rows[r];
            for (var c = 0; c < columns.Count; c++)
            {
                var cell = BuildCell(columns[c], row);
                cell.InputTransparent = _rowClicked is not null;
                grid.Add(cell, c, rowIndex);
            }
            if (_rowClicked is not null && columns.Count > 0)
            {
                // The whole row is one native button laid over its cells; it is named by the first cell.
                var button = new KlyButton { Block = true, Variant = ButtonVariant.Ghost, Color = KlyColor.Neutral, Padding = new Thickness(0), MinimumHeightRequest = 40, Text = string.Empty };
                SemanticProperties.SetDescription(button, columns[0].IsMissing(row) ? DashboardFormat.MissingValue : columns[0].Display(row, FormatCulture) ?? string.Empty);
                var current = row;
                button.Clicked += (_, _) => _rowClicked?.Invoke(this, current);
                grid.Add(button, 0, rowIndex);
                grid.SetColumnSpan(button, columns.Count);
            }
            if (r < rows.Count - 1) DashboardTableView.AddDivider(grid, columns.Count);
        }
        return new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = grid };
    }

    private View BuildHeader(DashboardTableColumn<TRow> column)
    {
        if (!column.Sortable) return DashboardTableView.Cell(column.Label, column.IsNumeric, KlyTheme.TextMuted, FontAttributes.Bold);
        var arrow = Sort?.Key == column.Key ? (Sort.Descending ? " ↓" : " ↑") : string.Empty;
        var button = new KlyButton
        {
            Text = column.Label + arrow,
            Color = KlyColor.Neutral,
            Variant = ButtonVariant.Ghost,
            Size = KlySize.Xs,
            MinimumHeightRequest = 40,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = column.IsNumeric ? LayoutOptions.End : LayoutOptions.Start,
        };
        button.Clicked += (_, _) => CycleSort(column);
        return button;
    }

    private View BuildCell(DashboardTableColumn<TRow> column, TRow row)
    {
        var numeric = column.IsNumeric;
        if (column.IsMissing(row)) return DashboardTableView.Cell(DashboardFormat.MissingValue, numeric, KlyTheme.TextDimmed, FontAttributes.None);
        if (column.Number(row) is { } number)
        {
            switch (column.Kind)
            {
                case DashboardTableColumnKind.Delta:
                {
                    var good = column.InvertDelta ? number < 0 : number > 0;
                    return DashboardTableView.Cell(DashboardFormat.Format(number, DashboardFormatPreset.Delta, FormatCulture), numeric, DashboardParts.DeltaText(number, good), FontAttributes.Bold);
                }
                case DashboardTableColumnKind.Percent:
                    return DashboardTableView.Cell(DashboardFormat.Format(number, DashboardFormatPreset.Percent, FormatCulture), numeric, KlyTheme.Text, FontAttributes.None);
                case DashboardTableColumnKind.Bar:
                    return BuildBarCell(column, number);
                case DashboardTableColumnKind.Number:
                    return DashboardTableView.Cell(DashboardFormat.Format(number, column.Format, FormatCulture), numeric, KlyTheme.Text, FontAttributes.None);
            }
        }
        return DashboardTableView.Cell(column.Display(row, FormatCulture) ?? DashboardFormat.MissingValue, numeric, KlyTheme.Text, FontAttributes.None);
    }

    // A number with a bar of 64 by 6: the scale includes zero, so a zero has no bar and a negative value extends to the left of a zero line.
    private View BuildBarCell(DashboardTableColumn<TRow> column, double number)
    {
        var (lo, hi) = ColumnScale(column);
        var (left, width) = DashboardBarGeometry.Segment(number, lo, hi);
        var cell = new HorizontalStackLayout { Spacing = 8, Padding = new Thickness(10, 6), HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Center };
        var text = DashboardParts.NewLabel(12, KlyTheme.Text, lineBreak: LineBreakMode.NoWrap);
        text.Text = DashboardFormat.Format(number, column.Format, FormatCulture);
        text.VerticalOptions = LayoutOptions.Center;
        cell.Add(text);
        cell.Add(new DashboardBarView(6)
        {
            WidthRequest = 64,
            Left = left,
            Length = width,
            ZeroPercent = lo < 0 ? DashboardBarGeometry.Zero(lo, hi) : null,
        });
        return cell;
    }

    private (double Lo, double Hi) ColumnScale(DashboardTableColumn<TRow> column)
    {
        var values = new List<double>();
        foreach (var row in Rows ?? [])
        {
            if (column.Number(row) is { } value) values.Add(value);
        }
        return DashboardBarGeometry.Domain(values);
    }

    private DashboardDataTable BuildDataTable()
    {
        var columns = (Columns ?? []).Select(c => new DashboardDataColumn(c.Key, c.Label, c.IsNumeric)).ToList();
        var rows = SortedAndLimitedRows.Select(r =>
        {
            var cells = new Dictionary<string, string?>();
            foreach (var column in Columns ?? [])
            {
                cells[column.Key] = column.Number(r) is { } d && column.IsNumeric ? DashboardFormat.FormatInvariant(d) : column.DisplayInvariant(r);
            }
            return (IReadOnlyDictionary<string, string?>)cells;
        }).ToList();
        return new DashboardDataTable(columns, rows);
    }

    private static BindableProperty Prop<T>(string name, T? defaultValue) =>
        BindableProperty.Create(name, typeof(T), typeof(KlyDashboardTable<TRow>), defaultValue, propertyChanged: (b, _, _) => ((KlyDashboardTable<TRow>)b).InvalidateContent());
}
