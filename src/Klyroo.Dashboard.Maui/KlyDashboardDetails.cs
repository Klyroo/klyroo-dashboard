namespace Klyroo.Dashboard.Maui;

/// <summary>A definition list of labelled values, with optional hints, links and copy buttons. The rows sit in two columns from 640 units wide and in one below.</summary>
public class KlyDashboardDetails : KlyDashboardCard
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IReadOnlyList<DashboardDetailsItem>), typeof(KlyDashboardDetails), null, propertyChanged: (b, _, _) => ((KlyDashboardDetails)b).InvalidateContent());

    private int _columns = DashboardBreakpoints.DetailsColumns(-1);

    /// <summary>Creates the block.</summary>
    public KlyDashboardDetails()
    {
        Span = 6;
        Skeleton = DashboardSkeletonKind.Details;
        Tabulate = BuildDataTable;
        SizeChanged += (_, _) => Resize(DashboardBreakpoints.ViewportWidth(this));
    }

    /// <summary>The definition rows. Rows marked <see cref="DashboardDetailsItem.Hidden"/> are skipped, and rows marked <see cref="DashboardDetailsItem.Copy"/> get a button that copies the value to the clipboard.</summary>
    public IReadOnlyList<DashboardDetailsItem>? Items { get => (IReadOnlyList<DashboardDetailsItem>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>The rows that are not hidden.</summary>
    internal IReadOnlyList<DashboardDetailsItem> VisibleItems => Items?.Where(i => !i.Hidden).ToList() ?? [];

    /// <summary>The number of columns the rows are in now.</summary>
    internal int Columns => _columns;

    /// <summary>Lays the rows out for the width of the page: builds them again when the number of columns changes.</summary>
    internal void Resize(double viewportWidth)
    {
        var columns = DashboardBreakpoints.DetailsColumns(viewportWidth);
        if (columns == _columns) return;
        _columns = columns;
        InvalidateContent();
    }

    /// <inheritdoc />
    protected override void OnRefreshing()
    {
        var empty = VisibleItems.Count == 0;
        if (IsEmpty != empty) IsEmpty = empty;
    }

    /// <inheritdoc />
    protected override View? BuildBody()
    {
        var grid = new Grid { ColumnSpacing = 16, RowSpacing = 12 };
        var columns = Math.Max(1, _columns);
        for (var c = 0; c < columns; c++) grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        int row = 0, column = 0;
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        foreach (var item in VisibleItems)
        {
            var span = item.FullSpan ? columns : 1;
            if (column + span > columns)
            {
                row++;
                column = 0;
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }
            var view = BuildItem(Texts, item);
            grid.Add(view, column, row);
            grid.SetColumnSpan(view, span);
            column += span;
            if (column >= columns)
            {
                row++;
                column = 0;
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }
        }
        return grid;
    }

    private static View BuildItem(DashboardTexts texts, DashboardDetailsItem item)
    {
        var stack = new VerticalStackLayout { Spacing = 2 };
        var term = DashboardParts.NewLabel(12, KlyTheme.TextMuted, FontAttributes.Bold);
        term.Text = item.Label;
        stack.Add(term);

        var definition = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
        var empty = string.IsNullOrWhiteSpace(item.Value);
        var value = DashboardParts.NewLabel(14, empty ? KlyTheme.TextDimmed : !string.IsNullOrEmpty(item.Href) ? DashboardParts.PrimaryText : KlyTheme.TextHighlighted, item.Mono ? FontAttributes.None : FontAttributes.Bold);
        value.Text = empty ? DashboardFormat.MissingValue : item.Value;
        value.VerticalOptions = LayoutOptions.Center;
        if (item.Mono) value.FontFamily = DashboardParts.MonoFont();
        if (!empty && !string.IsNullOrEmpty(item.Href))
        {
            value.TextDecorations = TextDecorations.Underline;
            value.MinimumHeightRequest = 40;
            value.VerticalTextAlignment = TextAlignment.Center;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => DashboardLaunch.Open(item.Href);
            value.GestureRecognizers.Add(tap);
            SemanticProperties.SetHint(value, item.Href);
        }
        definition.Add(value);
        if (item.Copy && !empty)
        {
            var toCopy = item.Value;
            var copy = new DashboardTapArea(new KlyIcon { Name = KlyIconNames.Copy, Size = KlySize.Sm, IconColor = KlyTheme.TextMuted, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center })
            {
                Description = texts.CopyValue(item.Label),
                WidthRequest = 40,
                HeightRequest = 40,
            };
            copy.Tapped += (_, _) => DashboardLaunch.Copy(toCopy);
            definition.Add(copy);
        }
        stack.Add(definition);

        if (!string.IsNullOrEmpty(item.Hint))
        {
            var hint = DashboardParts.NewLabel(12, KlyTheme.TextMuted);
            hint.Text = item.Hint;
            stack.Add(hint);
        }
        return stack;
    }

    private DashboardDataTable BuildDataTable()
    {
        var texts = Texts;
        var columns = new List<DashboardDataColumn>
        {
            new("label", texts.ColumnLabel),
            new("value", texts.ColumnValue),
        };
        var rows = VisibleItems.Select(i => (IReadOnlyDictionary<string, string?>)new Dictionary<string, string?>
        {
            ["label"] = i.Label,
            ["value"] = i.Value,
        }).ToList();
        return new DashboardDataTable(columns, rows);
    }
}
