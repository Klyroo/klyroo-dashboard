namespace Klyroo.Dashboard.Maui;

/// <summary>Horizontal bars for a ranking, or a funnel with the conversion rate between steps.</summary>
public class KlyDashboardBars : KlyDashboardCard
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = Prop<IReadOnlyList<DashboardBarItem>?>(nameof(Items), null);
    /// <summary>Identifies <see cref="Format"/>.</summary>
    public static readonly BindableProperty FormatProperty = Prop(nameof(Format), DashboardFormatPreset.Number);
    /// <summary>Identifies <see cref="Max"/>.</summary>
    public static readonly BindableProperty MaxProperty = Prop<double?>(nameof(Max), null);
    /// <summary>Identifies <see cref="Limit"/>.</summary>
    public static readonly BindableProperty LimitProperty = Prop<int?>(nameof(Limit), null);
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = Prop(nameof(Variant), DashboardBarsVariant.Bars);
    /// <summary>Identifies <see cref="Emphasis"/>.</summary>
    public static readonly BindableProperty EmphasisProperty = Prop(nameof(Emphasis), DashboardBarsEmphasis.All);

    private EventHandler<DashboardBarItem>? _selected;

    /// <summary>Creates the block.</summary>
    public KlyDashboardBars()
    {
        Span = 6;
        Tabulate = BuildDataTable;
    }

    /// <summary>
    /// Raised with the bar when the user picks it. Each bar is then a button, so the choice works with a screen reader and a keyboard, and the bar whose <see cref="DashboardBarItem.Selected"/> is set is highlighted.
    /// Use it to drill down, for example by setting a filter of the <see cref="DashboardController"/>.
    /// </summary>
    public event EventHandler<DashboardBarItem>? Selected
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

    /// <summary>The bars, in display order.</summary>
    public IReadOnlyList<DashboardBarItem>? Items { get => (IReadOnlyList<DashboardBarItem>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>How the values are formatted, unless an item supplies its own <c>Meta</c> text.</summary>
    public DashboardFormatPreset Format { get => (DashboardFormatPreset)GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    /// <summary>The value that fills a whole bar. Defaults to the largest item. Ignored by the funnel variant. The scale always includes zero: a zero value has no bar and a negative value extends to the left of a zero line.</summary>
    public double? Max { get => (double?)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }

    /// <summary>The maximum number of bars shown. The table view and the CSV export hold the same bars.</summary>
    public int? Limit { get => (int?)GetValue(LimitProperty); set => SetValue(LimitProperty, value); }

    /// <summary><see cref="DashboardBarsVariant.Bars"/> (the default), or <see cref="DashboardBarsVariant.Funnel"/>: every bar is scaled to the first one and shows its share of the previous step.</summary>
    public DashboardBarsVariant Variant { get => (DashboardBarsVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary><see cref="DashboardBarsEmphasis.All"/> (the default) colors every bar. <see cref="DashboardBarsEmphasis.First"/> colors the first bar and mutes the others.</summary>
    public DashboardBarsEmphasis Emphasis { get => (DashboardBarsEmphasis)GetValue(EmphasisProperty); set => SetValue(EmphasisProperty, value); }

    private bool IsFunnel => Variant == DashboardBarsVariant.Funnel;

    /// <summary>The bars shown: the first <see cref="Limit"/> items.</summary>
    internal IReadOnlyList<DashboardBarItem> Visible => Items is { Count: > 0 } items ? (Limit is > 0 ? items.Take(Limit.Value).ToList() : items) : [];

    /// <inheritdoc />
    protected override void OnRefreshing()
    {
        var empty = Items is not { Count: > 0 };
        if (IsEmpty != empty) IsEmpty = empty;
        var skeleton = IsFunnel ? DashboardSkeletonKind.Funnel : DashboardSkeletonKind.Bars;
        if (Skeleton != skeleton) Skeleton = skeleton;
    }

    /// <inheritdoc />
    protected override View? BuildBody()
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        if (Items is not { Count: > 0 } items) return stack;
        var visible = Visible;
        // The scale always includes zero: a zero has no bar, a negative value extends to the left of the zero line, and no bar has a minimum width.
        var (lo, hi) = DashboardBarGeometry.Domain(items.Select(i => i.Value), IsFunnel ? items[0].Value : Max);
        for (var i = 0; i < visible.Count; i++)
        {
            var item = visible[i];
            var funnelNote = IsFunnel
                ? (i == 0 ? Texts.FunnelBase : (items[i - 1].Value > 0 ? Texts.FunnelStep((int)Math.Round(item.Value / items[i - 1].Value * 100)) : DashboardFormat.MissingValue))
                : null;
            stack.Add(BuildRow(item, i, lo, hi, funnelNote));
        }
        return stack;
    }

    private View BuildRow(DashboardBarItem item, int index, double lo, double hi, string? funnelNote)
    {
        var (left, width) = DashboardBarGeometry.Segment(item.Value, lo, hi);
        var content = new VerticalStackLayout { Spacing = 4 };

        var head = new Grid { ColumnSpacing = 8 };
        head.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        head.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        var name = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
        var label = DashboardParts.NewLabel(12, KlyTheme.TextHighlighted, FontAttributes.Bold, LineBreakMode.TailTruncation);
        label.Text = item.Label;
        label.VerticalOptions = LayoutOptions.Center;
        name.Add(label);
        if (!string.IsNullOrEmpty(item.Tag))
        {
            var tag = new Border { StrokeThickness = 0, Padding = new Thickness(6, 2), VerticalOptions = LayoutOptions.Center };
            tag.SetRadius(KlyTheme.RadiusOf(1));
            tag.SetThemeColor(BackgroundColorProperty, KlyTheme.BackgroundMuted);
            var text = DashboardParts.NewLabel(10, KlyTheme.TextMuted, lineBreak: LineBreakMode.NoWrap);
            text.Text = item.Tag;
            text.FontFamily = DashboardParts.MonoFont();
            tag.Content = text;
            name.Add(tag);
        }
        head.Add(name, 0);

        var figures = new HorizontalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };
        if (funnelNote is not null)
        {
            var note = DashboardParts.NewLabel(11, KlyTheme.TextMuted, lineBreak: LineBreakMode.NoWrap);
            note.Text = funnelNote;
            note.VerticalOptions = LayoutOptions.Center;
            figures.Add(note);
        }
        var value = DashboardParts.NewLabel(12, KlyTheme.TextHighlighted, FontAttributes.Bold, LineBreakMode.NoWrap);
        value.Text = item.Meta ?? DashboardFormat.Format(item.Value, Format, FormatCulture);
        value.VerticalOptions = LayoutOptions.Center;
        figures.Add(value);
        head.Add(figures, 1);
        content.Add(head);

        var muted = Emphasis == DashboardBarsEmphasis.First && index > 0;
        content.Add(new DashboardBarView(8)
        {
            Left = left,
            Length = width,
            ZeroPercent = lo < 0 ? DashboardBarGeometry.Zero(lo, hi) : null,
            Fill = muted ? KlyTheme.TextMuted.WithAlpha(0.5f) : DashboardParts.PrimaryFill,
        });

        if (_selected is null) return Wrap(content, item);

        // The row is a button when something listens, so a screen reader and a keyboard reach it.
        var area = new DashboardTapArea(Wrap(content, item))
        {
            Description = $"{item.Label}, {value.Text}",
            MinimumTarget = 40,
        };
        area.Tapped += (_, _) => _selected?.Invoke(this, item);
        return area;
    }

    private static View Wrap(View content, DashboardBarItem item)
    {
        // The highlight of the selected bar, which the web theme draws from data-selected.
        var frame = new Border { StrokeThickness = 0, Padding = new Thickness(6, 4), Content = content };
        frame.SetRadius(KlyTheme.RadiusOf(1));
        frame.SetThemeColor(BackgroundColorProperty, item.Selected ? KlyTheme.BackgroundMuted.WithAlpha(0.5f) : KlyThemeColor.Transparent);
        return frame;
    }

    private DashboardDataTable BuildDataTable()
    {
        var texts = Texts;
        var columns = new List<DashboardDataColumn>
        {
            new("label", texts.ColumnLabel),
            new("value", texts.ColumnValue, Numeric: true),
        };
        var rows = Visible.Select(i => (IReadOnlyDictionary<string, string?>)new Dictionary<string, string?>
        {
            ["label"] = i.Label,
            ["value"] = DashboardFormat.FormatInvariant(i.Value),
        }).ToList();
        return new DashboardDataTable(columns, rows);
    }

    private static BindableProperty Prop<T>(string name, T? defaultValue) =>
        BindableProperty.Create(name, typeof(T), typeof(KlyDashboardBars), defaultValue, propertyChanged: (b, _, _) => ((KlyDashboardBars)b).InvalidateContent());
}
