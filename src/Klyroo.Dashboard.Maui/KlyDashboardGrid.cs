namespace Klyroo.Dashboard.Maui;

/// <summary>
/// A grid of equal columns (12 by default, see <see cref="Columns"/>) that places blocks by their <see cref="KlyDashboardCard.Span"/> and gives them its <see cref="Menu"/> and <see cref="Freshness"/> defaults.
/// It lays out by width: below 640 units wide every block takes the full row, so three statistics of a span of 4 stack on a phone and sit side by side on a tablet.
/// Blocks go in <see cref="Children"/>; a child that is not a block takes one column, as an element of a CSS grid does (a full row below 640).
/// </summary>
[ContentProperty(nameof(Children))]
public class KlyDashboardGrid : Border
{
    /// <summary>Identifies <see cref="Columns"/>.</summary>
    public static readonly BindableProperty ColumnsProperty = BindableProperty.Create(nameof(Columns), typeof(int), typeof(KlyDashboardGrid), 12, propertyChanged: (b, _, _) => ((KlyDashboardGrid)b).Place());
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(DashboardGridVariant), typeof(KlyDashboardGrid), DashboardGridVariant.Cards, propertyChanged: (b, _, _) => ((KlyDashboardGrid)b).ApplyVariant());
    /// <summary>Identifies <see cref="Menu"/>.</summary>
    public static readonly BindableProperty MenuProperty = BindableProperty.Create(nameof(Menu), typeof(bool), typeof(KlyDashboardGrid), false, propertyChanged: (b, _, _) => ((KlyDashboardGrid)b).RefreshCards());
    /// <summary>Identifies <see cref="Freshness"/>.</summary>
    public static readonly BindableProperty FreshnessProperty = BindableProperty.Create(nameof(Freshness), typeof(bool), typeof(KlyDashboardGrid), false, propertyChanged: (b, _, _) => ((KlyDashboardGrid)b).RefreshCards());

    private readonly Grid _grid = new();
    private double _lastWidth = -1;

    /// <summary>Creates a grid.</summary>
    public KlyDashboardGrid()
    {
        HorizontalOptions = LayoutOptions.Fill;
        Padding = 0;
        Content = _grid;
        _grid.ChildAdded += (_, e) =>
        {
            if (e.Element is VisualElement added) added.PropertyChanged += OnChildChanged;
            Place();
        };
        _grid.ChildRemoved += (_, e) =>
        {
            if (e.Element is VisualElement removed) removed.PropertyChanged -= OnChildChanged;
            Place();
        };
        SizeChanged += (_, _) => Place();
        ParentChanged += (_, _) => Place();
        ApplyVariant();
    }

    /// <summary>The number of equal columns of the grid, at least 1. Defaults to 12. A block's <see cref="KlyDashboardCard.Span"/> is clamped to it, so with <c>Columns = 3</c> a block of the default span 12 fills one row and <c>Span = 1</c> takes a third.</summary>
    public int Columns { get => (int)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }

    /// <summary><see cref="DashboardGridVariant.Cards"/> (the default) draws separate cards with a gap. <see cref="DashboardGridVariant.Panels"/> joins the cards into one bordered panel with thin dividers.</summary>
    public DashboardGridVariant Variant { get => (DashboardGridVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>The default of <see cref="KlyDashboardCard.Menu"/> for the blocks inside the grid. Defaults to <see langword="false"/>.</summary>
    public bool Menu { get => (bool)GetValue(MenuProperty); set => SetValue(MenuProperty, value); }

    /// <summary>The default of <see cref="KlyDashboardCard.Freshness"/> for the blocks inside the grid. Defaults to <see langword="false"/>.</summary>
    public bool Freshness { get => (bool)GetValue(FreshnessProperty); set => SetValue(FreshnessProperty, value); }

    /// <summary>The blocks of the grid. Each one sets its width with <see cref="KlyDashboardCard.Span"/>.</summary>
    public IList<IView> Children => _grid.Children;

    /// <summary>The column count the grid lays out: <see cref="Columns"/>, at least 1.</summary>
    internal int EffectiveColumns => Math.Max(1, Columns);

    /// <summary>The grid the blocks are placed in, for the tests.</summary>
    internal Grid Inner => _grid;

    /// <summary>Places the blocks for the width of the page the grid is on.</summary>
    internal void Place() => Place(DashboardBreakpoints.ViewportWidth(this));

    /// <summary>
    /// Places the visible blocks left to right in rows of <see cref="Columns"/>, the way a CSS grid does: a block that does not fit in what is left of the row starts the next one,
    /// and nothing moves back to fill a gap. Below 640 units every block takes the whole row.
    /// </summary>
    /// <param name="viewportWidth">The width of the page, or a width that is not known yet (zero or less), which is the widest layout.</param>
    internal void Place(double viewportWidth)
    {
        _lastWidth = viewportWidth;
        var columns = EffectiveColumns;
        var stacked = DashboardBreakpoints.Of(viewportWidth) == DashboardBreakpoint.Base;

        while (_grid.ColumnDefinitions.Count < columns) _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        while (_grid.ColumnDefinitions.Count > columns) _grid.ColumnDefinitions.RemoveAt(_grid.ColumnDefinitions.Count - 1);

        int row = 0, column = 0, lastRow = -1;
        foreach (var child in _grid.Children.OfType<View>())
        {
            if (!child.IsVisible) continue;
            var span = stacked ? columns : child is KlyDashboardCard card ? Math.Clamp(card.Span, 1, columns) : 1;
            if (column + span > columns)
            {
                row++;
                column = 0;
            }
            Microsoft.Maui.Controls.Grid.SetRow(child, row);
            Microsoft.Maui.Controls.Grid.SetColumn(child, column);
            Microsoft.Maui.Controls.Grid.SetColumnSpan(child, span);
            lastRow = row;
            column += span;
            if (column >= columns)
            {
                row++;
                column = 0;
            }
        }

        var rows = Math.Max(lastRow + 1, 0);
        while (_grid.RowDefinitions.Count < rows) _grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        while (_grid.RowDefinitions.Count > rows) _grid.RowDefinitions.RemoveAt(_grid.RowDefinitions.Count - 1);

        // An empty grid draws nothing, not even the frame of the panels variant.
        IsVisible = rows > 0;
    }

    /// <summary>The width the grid was last placed for, for the tests.</summary>
    internal double LastWidth => _lastWidth;

    // A block that hides or shows itself leaves no gap, as an element that is not rendered does on the web.
    private void OnChildChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VisualElement.IsVisible)) Place();
    }

    private void ApplyVariant()
    {
        if (Variant == DashboardGridVariant.Panels)
        {
            // The cards sit on the color of the border with a one unit gap between them: the gap is the divider, and the border of the panel is the same color.
            _grid.RowSpacing = 1;
            _grid.ColumnSpacing = 1;
            StrokeThickness = 1;
            this.SetStroke(KlyTheme.Border);
            this.SetThemeColor(BackgroundColorProperty, KlyTheme.Border);
            this.SetRadius(KlyTheme.RadiusOf(0.75));
        }
        else
        {
            _grid.RowSpacing = 16;
            _grid.ColumnSpacing = 16;
            StrokeThickness = 0;
            this.SetStroke(KlyThemeColor.Transparent);
            this.SetThemeColor(BackgroundColorProperty, KlyThemeColor.Transparent);
            this.SetRadius(0);
        }
        RefreshCards();
    }

    private void RefreshCards()
    {
        foreach (var card in _grid.Children.OfType<KlyDashboardCard>()) card.Refresh();
    }
}
