namespace Klyroo.Dashboard.Maui;

/// <summary>
/// A chart over categories: grouped bars, lines, areas, or a donut of the first series, with a legend and optional totals. The drawing is <see cref="KlyChart{TItem, TIndex}"/> of Klyroo.UI.Maui
/// (axes, gridlines, a tooltip under the finger, a description for a screen reader). A bar chart with a line series over the bars, or with <see cref="ShowLabels"/>, is a drawing of
/// its own that <see cref="KlyChart{TItem, TIndex}"/> cannot do: bars and lines from a zero baseline, with no axes or gridlines.
/// </summary>
public class KlyDashboardChart : KlyDashboardCard
{
    /// <summary>Identifies <see cref="Type"/>.</summary>
    public static readonly BindableProperty TypeProperty = Prop(nameof(Type), DashboardChartType.Bar);
    /// <summary>Identifies <see cref="Categories"/>.</summary>
    public static readonly BindableProperty CategoriesProperty = Prop<IReadOnlyList<string>?>(nameof(Categories), null);
    /// <summary>Identifies <see cref="Series"/>.</summary>
    public static readonly BindableProperty SeriesProperty = Prop<IReadOnlyList<DashboardChartSeries>?>(nameof(Series), null);
    /// <summary>Identifies <see cref="Format"/>.</summary>
    public static readonly BindableProperty FormatProperty = Prop(nameof(Format), DashboardFormatPreset.Number);
    /// <summary>Identifies <see cref="PlotHeight"/>.</summary>
    public static readonly BindableProperty PlotHeightProperty = Prop(nameof(PlotHeight), 220);
    /// <summary>Identifies <see cref="ShowLegend"/>.</summary>
    public static readonly BindableProperty ShowLegendProperty = Prop(nameof(ShowLegend), true);
    /// <summary>Identifies <see cref="ShowLabels"/>.</summary>
    public static readonly BindableProperty ShowLabelsProperty = Prop(nameof(ShowLabels), false);
    /// <summary>Identifies <see cref="Totals"/>.</summary>
    public static readonly BindableProperty TotalsProperty = Prop(nameof(Totals), DashboardChartTotals.None);
    /// <summary>Identifies <see cref="Stacked"/>.</summary>
    public static readonly BindableProperty StackedProperty = Prop(nameof(Stacked), false);
    /// <summary>Identifies <see cref="Horizontal"/>.</summary>
    public static readonly BindableProperty HorizontalProperty = Prop(nameof(Horizontal), false);

    private readonly DashboardCompactChart _compact = new();
    private EventHandler<DashboardChartSelection>? _selected;

    /// <summary>Creates a chart.</summary>
    public KlyDashboardChart()
    {
        Span = 8;
        Tabulate = BuildDataTable;
    }

    /// <summary>
    /// Raised with the category, the series key and the value when the user picks a data point. In the drawing of <see cref="KlyChart{TItem, TIndex}"/> the bars, points and slices themselves
    /// are the choice: a pick is reported for the series it landed on, a comparison series included; when the series cannot be told, or has no value in that category, it is the first series that is not
    /// a comparison and has a value, and a category with nothing to pick is ignored. The compact drawing has no selectable marks: a group of buttons under it offers one per category and series
    /// (one per slice for a donut, comparison series left out). Use it to drill down, for example by setting a filter of the <see cref="DashboardController"/>.
    /// </summary>
    public event EventHandler<DashboardChartSelection>? Selected
    {
        add
        {
            _selected += value;
            InvalidateContent();
        }
        remove
        {
            _selected -= value;
            InvalidateContent();
        }
    }

    /// <summary>
    /// The chart type: <see cref="DashboardChartType.Bar"/> (the default) draws one bar per series in each category, side by side; <see cref="DashboardChartType.Line"/> draws every series as a line;
    /// <see cref="DashboardChartType.Area"/> draws every series as a line with the area under it filled; <see cref="DashboardChartType.Donut"/> draws the first series that is not a comparison as a ring of
    /// slices, one per category, with the total in the middle. In a bar chart, a series whose <see cref="DashboardChartSeries.Type"/> is <see cref="DashboardChartSeriesType.Line"/> is drawn as a line over the bars.
    /// The type also picks the loading skeleton.
    /// </summary>
    public DashboardChartType Type { get => (DashboardChartType)GetValue(TypeProperty); set => SetValue(TypeProperty, value); }

    /// <summary>The labels of the horizontal axis, one per category, or the slices of a donut.</summary>
    public IReadOnlyList<string>? Categories { get => (IReadOnlyList<string>?)GetValue(CategoriesProperty); set => SetValue(CategoriesProperty, value); }

    /// <summary>The data series, all drawn (a donut draws the first one that is not a comparison) and all in the table view. Each series takes the next chart color of Klyroo.UI (<c>chart-1</c> to <c>chart-6</c>, then neutral); comparison series are muted.</summary>
    public IReadOnlyList<DashboardChartSeries>? Series { get => (IReadOnlyList<DashboardChartSeries>?)GetValue(SeriesProperty); set => SetValue(SeriesProperty, value); }

    /// <summary>How the values of labels, tooltips, totals and the table view are formatted.</summary>
    public DashboardFormatPreset Format { get => (DashboardFormatPreset)GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    /// <summary>The height of the drawing in device units (<c>Height</c> in the Blazor component, which a native view already has). A donut is as wide as it is high, between 96 and 240. Defaults to 220.</summary>
    public int PlotHeight { get => (int)GetValue(PlotHeightProperty); set => SetValue(PlotHeightProperty, value); }

    /// <summary>Shows the legend: the series, or the slices with their value for a donut. Defaults to <see langword="true"/>.</summary>
    public bool ShowLegend { get => (bool)GetValue(ShowLegendProperty); set => SetValue(ShowLegendProperty, value); }

    /// <summary>Shows the formatted value above each bar (below a negative one). Lines, areas and donuts show their values in the tooltip. It switches the chart to the compact drawing.</summary>
    public bool ShowLabels { get => (bool)GetValue(ShowLabelsProperty); set => SetValue(ShowLabelsProperty, value); }

    /// <summary>The totals row: <see cref="DashboardChartTotals.Average"/> shows the average of each series, <see cref="DashboardChartTotals.Sum"/> the sum. Comparison series are left out. <see cref="DashboardChartTotals.None"/>, the default, shows nothing.</summary>
    public DashboardChartTotals Totals { get => (DashboardChartTotals)GetValue(TotalsProperty); set => SetValue(TotalsProperty, value); }

    /// <summary>Stacks the series of a <c>bar</c> or <c>area</c> chart instead of drawing them side by side or over one another. Comparison series are stacked too, so leave them out of a stacked chart.</summary>
    public bool Stacked { get => (bool)GetValue(StackedProperty); set => SetValue(StackedProperty, value); }

    /// <summary>Draws the bars of a <c>bar</c> chart along the horizontal axis, with the categories on the vertical one.</summary>
    public bool Horizontal { get => (bool)GetValue(HorizontalProperty); set => SetValue(HorizontalProperty, value); }

    /// <summary>Whether the compact drawing is used: bars with a line series over them, or bars with their values.</summary>
    internal bool UsesCompactDrawing =>
        Type == DashboardChartType.Bar && Series is { Count: > 0 } series && (ShowLabels || (series.Any(IsLineSeries) && series.Any(s => !IsLineSeries(s))));

    /// <summary>The skeleton that matches the type.</summary>
    internal DashboardSkeletonKind SkeletonOf => Type == DashboardChartType.Donut ? DashboardSkeletonKind.Donut : Type is DashboardChartType.Line or DashboardChartType.Area ? DashboardSkeletonKind.Lines : DashboardSkeletonKind.Bars;

    /// <summary>The compact drawing, for the tests.</summary>
    internal DashboardCompactChart Compact => _compact;

    private static bool IsLineSeries(DashboardChartSeries series) => series.Type == DashboardChartSeriesType.Line;

    private bool DrawsAsLine(DashboardChartSeries series) => Type == DashboardChartType.Line || IsLineSeries(series);

    private static double? ValueOrNull(DashboardChartSeries series, int index) => DashboardCompactChart.ValueOrNull(series, index);

    private string FormatValue(double value) => DashboardFormat.Format(value, Format, FormatCulture);

    /// <inheritdoc />
    protected override void OnRefreshing()
    {
        var empty = Categories is not { Count: > 0 } || Series is not { Count: > 0 };
        if (IsEmpty != empty) IsEmpty = empty;
        if (Skeleton != SkeletonOf) Skeleton = SkeletonOf;
    }

    /// <inheritdoc />
    protected override View? BuildBody()
    {
        var stack = new VerticalStackLayout { Spacing = 12 };
        if (Categories is not { Count: > 0 } categories || Series is not { Count: > 0 } series) return stack;

        if (UsesCompactDrawing) BuildCompact(stack, categories, series);
        else if (BuildChart(categories, series) is { } chart) stack.Add(chart);

        if (_selected is not null && UsesCompactDrawing) stack.Add(BuildSelection(categories, series));
        if (Totals != DashboardChartTotals.None) stack.Add(BuildTotals(series));
        return stack;
    }

    private View? BuildChart(IReadOnlyList<string> categories, IReadOnlyList<DashboardChartSeries> series)
    {
        var donut = Type == DashboardChartType.Donut;
        var shown = donut ? series.Where(s => !s.IsComparison).Take(1).ToList() : series.ToList();
        if (shown.Count == 0) return null;
        // A bar chart whose series are all lines is a line chart.
        var kind = Type switch
        {
            DashboardChartType.Line => KlyChartType.Line,
            DashboardChartType.Area => KlyChartType.Area,
            DashboardChartType.Donut => KlyChartType.Donut,
            _ => shown.All(IsLineSeries) ? KlyChartType.Line : KlyChartType.Bar,
        };
        var rows = Enumerable.Range(0, categories.Count).ToList();
        var kinds = shown.Select(s => new KlyChartCategory<int>(s.Key, i => ValueOrNull(s, i)) { Label = s.Label }).ToList();
        // A comparison series is muted, the others keep the color of their position in Series. A donut colors its slices.
        var colors = donut
            ? categories.Select((_, i) => DashboardChartColors.ColorOf(i)).ToList()
            : shown.Select(s => s.IsComparison ? KlyColor.Neutral : DashboardChartColors.ColorOf(series.ToList().IndexOf(s))).ToList();
        var size = donut ? Math.Clamp(PlotHeight, 96, 240) : PlotHeight;

        var chart = new KlyChart<int, string>
        {
            Data = rows,
            Index = i => categories[i],
            Categories = kinds,
            Type = kind,
            Horizontal = Horizontal && kind == KlyChartType.Bar,
            Stacked = Stacked && kind is KlyChartType.Bar or KlyChartType.Area,
            PlotHeight = size,
            // The canvas draws at its real size: it fills the card, where the web drawing has to be capped to keep its text in proportion.
            PlotWidth = donut ? size : 4096,
            Legend = ShowLegend,
            Format = FormatValue,
            Culture = FormatCulture,
            Colors = colors,
            AccessibleName = Title,
        };
        if (donut) chart.Center = BuildDonutCenter(categories, shown[0]);
        chart.Selected += (_, e) => Pick(e, categories, series, shown);
        return chart;
    }

    private View BuildDonutCenter(IReadOnlyList<string> categories, DashboardChartSeries first)
    {
        var total = categories.Select((_, i) => Math.Max(0, ValueOrNull(first, i) ?? 0)).Sum();
        var center = new VerticalStackLayout { Spacing = 0, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        var value = DashboardParts.NewLabel(20, KlyTheme.TextHighlighted, FontAttributes.Bold, LineBreakMode.NoWrap);
        value.Text = FormatValue(total);
        value.HorizontalTextAlignment = TextAlignment.Center;
        var name = DashboardParts.NewLabel(11, KlyTheme.TextMuted, lineBreak: LineBreakMode.TailTruncation);
        name.Text = first.Label;
        name.HorizontalTextAlignment = TextAlignment.Center;
        name.Padding = new Thickness(16, 0);
        center.Add(value);
        center.Add(name);
        return center;
    }

    // What KlyChart reports is a row and the series the pick landed on, and that series is what is reported, a comparison series included (its own key and value).
    // Only when the series cannot be told (the keyboard) or has no value in the row, the first non-comparison series with a value stands for the category;
    // a row with nothing to pick is ignored.
    internal void Pick(KlyChartSelectEventArgs<int, string> e, IReadOnlyList<string> categories, IReadOnlyList<DashboardChartSeries> series, IReadOnlyList<DashboardChartSeries> shown)
    {
        if (e.Row < 0 || e.Row >= categories.Count) return;
        var picked = e.SeriesIndex >= 0 && e.SeriesIndex < shown.Count ? shown[e.SeriesIndex] : null;
        if (picked is null || ValueOrNull(picked, e.Row) is null)
        {
            picked = series.FirstOrDefault(s => !s.IsComparison && ValueOrNull(s, e.Row) is not null);
        }
        if (picked is not null && ValueOrNull(picked, e.Row) is { } value)
        {
            _selected?.Invoke(this, new DashboardChartSelection(categories[e.Row], picked.Key, value));
        }
    }

    private void BuildCompact(VerticalStackLayout stack, IReadOnlyList<string> categories, IReadOnlyList<DashboardChartSeries> series)
    {
        var indexed = series.Select((s, i) => new DashboardCompactSeries(s, DashboardChartColors.ColorOf(i), DrawsAsLine(s))).ToList();
        if (ShowLegend) stack.Add(BuildCompactLegend(indexed));
        _compact.Set(categories, indexed, ShowLabels, FormatValue, PlotHeight);
        DashboardParts.Detach(_compact);
        SemanticProperties.SetDescription(_compact, Describe(categories, series));
        stack.Add(_compact);
    }

    private View BuildCompactLegend(IReadOnlyList<DashboardCompactSeries> indexed)
    {
        var legend = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
        foreach (var item in indexed)
        {
            var row = new HorizontalStackLayout { Spacing = 6, Margin = new Thickness(0, 0, 12, 4), VerticalOptions = LayoutOptions.Center };
            View swatch;
            if (item.Series.IsComparison)
            {
                var outline = new Border { StrokeThickness = item.IsLine ? 2 : 1, StrokeDashArray = [2, 2], Padding = 0, WidthRequest = item.IsLine ? 12 : 10, HeightRequest = item.IsLine ? 2 : 10, VerticalOptions = LayoutOptions.Center };
                outline.SetStroke(KlyTheme.TextMuted);
                outline.SetRadius(item.IsLine ? 0 : DashboardParts.Pill);
                swatch = outline;
            }
            else
            {
                var fill = new Border { StrokeThickness = 0, Padding = 0, WidthRequest = item.IsLine ? 12 : 10, HeightRequest = item.IsLine ? 2 : 10, VerticalOptions = LayoutOptions.Center };
                fill.SetRadius(item.IsLine ? 0 : DashboardParts.Pill);
                fill.SetThemeColor(BackgroundColorProperty, DashboardChartColors.Resolve(item.Color).Text);
                swatch = fill;
            }
            AutomationProperties.SetIsInAccessibleTree(swatch, false);
            var label = DashboardParts.NewLabel(12, KlyTheme.TextMuted, lineBreak: LineBreakMode.NoWrap);
            label.Text = item.Series.Label;
            label.VerticalOptions = LayoutOptions.Center;
            row.Add(swatch);
            row.Add(label);
            legend.Add(row);
        }
        return legend;
    }

    // One button per data point of the compact drawing: the keyboard and a screen reader reach what a touch reaches in a drawing.
    private View BuildSelection(IReadOnlyList<string> categories, IReadOnlyList<DashboardChartSeries> series)
    {
        var donut = Type == DashboardChartType.Donut;
        var shown = series.Where(s => !s.IsComparison).Take(donut ? 1 : int.MaxValue).ToList();
        var group = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
        for (var i = 0; i < categories.Count; i++)
        {
            foreach (var s in shown)
            {
                if (ValueOrNull(s, i) is not { } value) continue;
                var category = categories[i];
                var key = s.Key;
                var text = shown.Count > 1 ? $"{category} · {s.Label}" : category;
                var button = new KlyButton { Text = $"{text}  {FormatValue(value)}", Color = KlyColor.Neutral, Variant = ButtonVariant.Soft, Size = KlySize.Xs, MinimumHeightRequest = 40, Margin = new Thickness(0, 0, 6, 6) };
                button.Clicked += (_, _) => _selected?.Invoke(this, new DashboardChartSelection(category, key, value));
                group.Add(button);
            }
        }
        return group;
    }

    private View BuildTotals(IReadOnlyList<DashboardChartSeries> series)
    {
        var box = new VerticalStackLayout { Spacing = 8 };
        box.Add(DashboardParts.Divider());
        var row = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
        foreach (var s in series.Where(s => !s.IsComparison))
        {
            // A NaN or infinite value is a gap: it is in neither the sum nor the average.
            var finite = s.Values.Where(double.IsFinite).ToList();
            double? total = Totals == DashboardChartTotals.Average
                ? (finite.Count > 0 ? finite.Average() : null)
                : finite.Sum();
            var item = new HorizontalStackLayout { Spacing = 6, Margin = new Thickness(0, 0, 16, 4) };
            var name = DashboardParts.NewLabel(12, KlyTheme.TextMuted, lineBreak: LineBreakMode.NoWrap);
            name.Text = $"{s.Label}:";
            var value = DashboardParts.NewLabel(12, KlyTheme.TextHighlighted, FontAttributes.Bold, LineBreakMode.NoWrap);
            value.Text = DashboardFormat.Format(total, Format, FormatCulture);
            item.Add(name);
            item.Add(value);
            row.Add(item);
        }
        box.Add(row);
        return box;
    }

    private string Describe(IReadOnlyList<string> categories, IEnumerable<DashboardChartSeries> series) =>
        string.Join("; ", series.Select(s =>
            $"{s.Label}: " + string.Join(", ", categories.Select((c, i) => (Category: c, Value: ValueOrNull(s, i)))
                .Where(p => p.Value is not null).Select(p => $"{p.Category} {FormatValue(p.Value!.Value)}"))));

    private DashboardDataTable BuildDataTable()
    {
        var texts = Texts;
        var columns = new List<DashboardDataColumn> { new("category", texts.ColumnCategory) };
        foreach (var s in Series ?? []) columns.Add(new DashboardDataColumn(s.Key, s.Label, Numeric: true));

        var rows = new List<IReadOnlyDictionary<string, string?>>();
        var categories = Categories ?? [];
        for (var i = 0; i < categories.Count; i++)
        {
            var row = new Dictionary<string, string?> { ["category"] = categories[i] };
            foreach (var s in Series ?? [])
            {
                var value = i < s.Values.Count ? (double?)s.Values[i] : null;
                row[s.Key] = DashboardFormat.FormatInvariant(value);
            }
            rows.Add(row);
        }
        return new DashboardDataTable(columns, rows);
    }

    private static BindableProperty Prop<T>(string name, T? defaultValue) =>
        BindableProperty.Create(name, typeof(T), typeof(KlyDashboardChart), defaultValue, propertyChanged: (b, _, _) => ((KlyDashboardChart)b).InvalidateContent());
}
