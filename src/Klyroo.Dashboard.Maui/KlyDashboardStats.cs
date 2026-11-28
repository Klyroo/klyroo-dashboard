namespace Klyroo.Dashboard.Maui;

/// <summary>Several metrics side by side, each with an optional delta, progress bar and status pill. They sit in four columns on a wide page, two from 640 units and one below.</summary>
public class KlyDashboardStats : KlyDashboardCard
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IReadOnlyList<DashboardStatsItem>), typeof(KlyDashboardStats), null, propertyChanged: (b, _, _) => ((KlyDashboardStats)b).InvalidateContent());
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(DashboardStatsVariant), typeof(KlyDashboardStats), DashboardStatsVariant.Plain, propertyChanged: (b, _, _) => ((KlyDashboardStats)b).InvalidateContent());

    private int _columns;

    /// <summary>Creates the block.</summary>
    public KlyDashboardStats()
    {
        Skeleton = DashboardSkeletonKind.Stats;
        Tabulate = BuildDataTable;
        _columns = DashboardBreakpoints.StatsColumns(-1);
        SizeChanged += (_, _) => Resize(DashboardBreakpoints.ViewportWidth(this));
    }

    /// <summary>The metrics, laid out in a grid of up to four columns.</summary>
    public IReadOnlyList<DashboardStatsItem>? Items { get => (IReadOnlyList<DashboardStatsItem>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary><see cref="DashboardStatsVariant.Plain"/> (the default) or <see cref="DashboardStatsVariant.Tiles"/>, which puts each metric in a bordered tile.</summary>
    public DashboardStatsVariant Variant { get => (DashboardStatsVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>The number of columns the metrics are in now.</summary>
    internal int Columns => _columns;

    /// <summary>Lays the metrics out for the width of the page: builds them again when the number of columns changes.</summary>
    internal void Resize(double viewportWidth)
    {
        var columns = DashboardBreakpoints.StatsColumns(viewportWidth);
        if (columns == _columns) return;
        _columns = columns;
        InvalidateContent();
    }

    /// <inheritdoc />
    protected override void OnRefreshing()
    {
        var empty = Items is not { Count: > 0 };
        if (IsEmpty != empty) IsEmpty = empty;
    }

    /// <inheritdoc />
    protected override View? BuildBody()
    {
        var grid = new Grid { ColumnSpacing = 16, RowSpacing = 16 };
        var columns = Math.Max(1, _columns);
        for (var c = 0; c < columns; c++) grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        var items = Items ?? [];
        for (var i = 0; i < items.Count; i++)
        {
            if (i % columns == 0) grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.Add(BuildItem(items[i]), i % columns, i / columns);
        }
        return grid;
    }

    private View BuildItem(DashboardStatsItem item)
    {
        var stack = new VerticalStackLayout { Spacing = 4 };

        var head = new Grid { ColumnSpacing = 8 };
        head.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        head.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        var label = DashboardParts.NewLabel(12, KlyTheme.TextMuted, FontAttributes.Bold);
        label.Text = item.Label;
        label.VerticalOptions = LayoutOptions.Center;
        head.Add(label, 0);
        if (item.Status is { } status)
        {
            head.Add(new KlyBadge { Text = status.Label, Color = status.Color is not null ? new KlyColor(status.Color) : KlyColor.Neutral, Variant = KlyVariant.Subtle, Size = KlySize.Sm, HorizontalOptions = LayoutOptions.End }, 1);
        }
        stack.Add(head);

        var figures = new Grid { ColumnSpacing = 8 };
        figures.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        figures.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        var value = DashboardParts.NewLabel(20, KlyTheme.TextHighlighted, FontAttributes.Bold, LineBreakMode.NoWrap);
        value.Text = DashboardFormat.Format(item.Value, item.Format, FormatCulture);
        value.VerticalOptions = LayoutOptions.Center;
        figures.Add(value, 0);
        if (item.Delta is { } delta)
        {
            var good = item.InvertDelta ? delta < 0 : delta > 0;
            var change = DashboardParts.NewLabel(12, DashboardParts.DeltaText(delta, good), FontAttributes.Bold, LineBreakMode.NoWrap);
            change.Text = DashboardFormat.Format(delta, DashboardFormatPreset.Delta, FormatCulture);
            change.VerticalOptions = LayoutOptions.Center;
            figures.Add(change, 1);
        }
        stack.Add(figures);

        if (item.Progress is { } progress) stack.Add(DashboardParts.ProgressBar(Math.Clamp(progress, 0, 100), 6, DashboardParts.PrimaryFill));
        if (!string.IsNullOrEmpty(item.Caption))
        {
            var caption = DashboardParts.NewLabel(12, KlyTheme.TextMuted);
            caption.Text = item.Caption;
            stack.Add(caption);
        }

        if (Variant != DashboardStatsVariant.Tiles) return stack;
        var tile = new Border { StrokeThickness = 1, Padding = new Thickness(12), Content = stack };
        tile.SetRadius(KlyTheme.RadiusOf(1));
        tile.SetStroke(KlyTheme.Border);
        tile.SetThemeColor(BackgroundColorProperty, KlyTheme.BackgroundMuted);
        return tile;
    }

    private DashboardDataTable BuildDataTable()
    {
        var texts = Texts;
        var columns = new List<DashboardDataColumn>
        {
            new("label", texts.ColumnLabel),
            new("value", texts.ColumnValue, Numeric: true),
            new("change", texts.ColumnChangePercent, Numeric: true),
        };
        var rows = (Items ?? []).Select(i => (IReadOnlyDictionary<string, string?>)new Dictionary<string, string?>
        {
            ["label"] = i.Label,
            ["value"] = DashboardFormat.FormatInvariant(i.Value),
            ["change"] = DashboardFormat.FormatInvariant(i.Delta),
        }).ToList();
        return new DashboardDataTable(columns, rows);
    }
}
