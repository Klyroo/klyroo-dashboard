namespace Klyroo.Dashboard.Maui;

/// <summary>A ranked list of rows with an optional value, percentage and delta.</summary>
public class KlyDashboardList : KlyDashboardCard
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = Prop<IReadOnlyList<DashboardListItem>?>(nameof(Items), null);
    /// <summary>Identifies <see cref="Format"/>.</summary>
    public static readonly BindableProperty FormatProperty = Prop(nameof(Format), DashboardFormatPreset.Number);
    /// <summary>Identifies <see cref="Limit"/>.</summary>
    public static readonly BindableProperty LimitProperty = Prop<int?>(nameof(Limit), null);

    private EventHandler<DashboardListItem>? _rowClicked;

    /// <summary>Creates the block.</summary>
    public KlyDashboardList()
    {
        Span = 6;
        Skeleton = DashboardSkeletonKind.Rows;
        Tabulate = BuildDataTable;
    }

    /// <summary>
    /// Raised with the row when the user picks it: the whole row is then a button, so the choice works with a screen reader and a keyboard, and the row whose <see cref="DashboardListItem.Selected"/> is set is highlighted.
    /// A row with an <see cref="DashboardListItem.Href"/> keeps its link and does not raise this event. Use it to drill down, for example by setting a filter of the <see cref="DashboardController"/>.
    /// </summary>
    public event EventHandler<DashboardListItem>? RowClicked
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

    /// <summary>The rows, in display order.</summary>
    public IReadOnlyList<DashboardListItem>? Items { get => (IReadOnlyList<DashboardListItem>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>How the row values are formatted.</summary>
    public DashboardFormatPreset Format { get => (DashboardFormatPreset)GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    /// <summary>The maximum number of rows shown. The table view and the CSV export hold the same rows.</summary>
    public int? Limit { get => (int?)GetValue(LimitProperty); set => SetValue(LimitProperty, value); }

    /// <summary>The rows shown: the first <see cref="Limit"/> items.</summary>
    internal IReadOnlyList<DashboardListItem> Visible => Items is { Count: > 0 } items ? (Limit is > 0 ? items.Take(Limit.Value).ToList() : items) : [];

    /// <inheritdoc />
    protected override void OnRefreshing()
    {
        var empty = Items is not { Count: > 0 };
        if (IsEmpty != empty) IsEmpty = empty;
    }

    /// <inheritdoc />
    protected override View? BuildBody()
    {
        var stack = new VerticalStackLayout { Spacing = 0 };
        var visible = Visible;
        for (var i = 0; i < visible.Count; i++)
        {
            if (i > 0) stack.Add(DashboardParts.Divider());
            stack.Add(BuildRow(visible[i], i == 0, i == visible.Count - 1));
        }
        return stack;
    }

    private View BuildRow(DashboardListItem item, bool first, bool last)
    {
        var clickable = _rowClicked is not null && string.IsNullOrEmpty(item.Href);
        var row = new Grid { ColumnSpacing = 12, Padding = new Thickness(0, first ? 0 : 8, 0, last ? 0 : 8) };
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        var left = new HorizontalStackLayout { Spacing = 10, VerticalOptions = LayoutOptions.Center };
        if (!string.IsNullOrEmpty(item.LeadingText))
        {
            var circle = new Border { WidthRequest = 28, HeightRequest = 28, StrokeThickness = 0, Padding = 0, VerticalOptions = LayoutOptions.Center };
            circle.SetRadius(DashboardParts.Pill);
            circle.SetThemeColor(BackgroundColorProperty, KlyTheme.BackgroundMuted);
            var initials = DashboardParts.NewLabel(12, KlyTheme.TextHighlighted, FontAttributes.Bold, LineBreakMode.NoWrap);
            initials.Text = item.LeadingText;
            initials.HorizontalOptions = LayoutOptions.Center;
            initials.VerticalOptions = LayoutOptions.Center;
            circle.Content = initials;
            left.Add(circle);
        }
        var texts = new VerticalStackLayout { Spacing = 0, VerticalOptions = LayoutOptions.Center };
        var label = DashboardParts.NewLabel(12, KlyTheme.TextHighlighted, FontAttributes.Bold, LineBreakMode.TailTruncation);
        label.Text = item.Label;
        if (!string.IsNullOrEmpty(item.Href))
        {
            // A link keeps its own tap: the label opens the address.
            label.TextDecorations = TextDecorations.Underline;
            label.MinimumHeightRequest = 40;
            label.VerticalTextAlignment = TextAlignment.Center;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => DashboardLaunch.Open(item.Href);
            label.GestureRecognizers.Add(tap);
            SemanticProperties.SetHint(label, item.Href);
        }
        texts.Add(label);
        if (!string.IsNullOrEmpty(item.Description))
        {
            var description = DashboardParts.NewLabel(11, KlyTheme.TextMuted, lineBreak: LineBreakMode.TailTruncation);
            description.Text = item.Description;
            texts.Add(description);
        }
        left.Add(texts);
        row.Add(left, 0);

        var right = new HorizontalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };
        if (item.Percent is { } percent)
        {
            var text = DashboardParts.NewLabel(12, KlyTheme.TextMuted, lineBreak: LineBreakMode.NoWrap);
            text.Text = DashboardFormat.Format(percent, DashboardFormatPreset.Percent, FormatCulture);
            right.Add(text);
        }
        if (item.Delta is { } delta)
        {
            var text = DashboardParts.NewLabel(12, DashboardParts.DeltaText(delta, delta > 0), FontAttributes.Bold, LineBreakMode.NoWrap);
            text.Text = DashboardFormat.Format(delta, DashboardFormatPreset.Delta, FormatCulture);
            right.Add(text);
        }
        if (item.Value.HasValue)
        {
            var text = DashboardParts.NewLabel(12, KlyTheme.TextHighlighted, FontAttributes.Bold, LineBreakMode.NoWrap);
            text.Text = DashboardFormat.Format(item.Value, Format, FormatCulture);
            right.Add(text);
        }
        row.Add(right, 1);

        var frame = new Border { StrokeThickness = 0, Padding = 0, Content = row };
        frame.SetThemeColor(BackgroundColorProperty, item.Selected ? KlyTheme.BackgroundMuted.WithAlpha(0.5f) : KlyThemeColor.Transparent);
        if (!clickable) return frame;

        var area = new DashboardTapArea(frame) { Description = SummaryOf(item), MinimumTarget = 40 };
        area.Tapped += (_, _) => _rowClicked?.Invoke(this, item);
        return area;
    }

    private string SummaryOf(DashboardListItem item)
    {
        var parts = new List<string> { item.Label };
        if (!string.IsNullOrEmpty(item.Description)) parts.Add(item.Description);
        if (item.Percent is { } percent) parts.Add(DashboardFormat.Format(percent, DashboardFormatPreset.Percent, FormatCulture));
        if (item.Delta is { } delta) parts.Add(DashboardFormat.Format(delta, DashboardFormatPreset.Delta, FormatCulture));
        if (item.Value.HasValue) parts.Add(DashboardFormat.Format(item.Value, Format, FormatCulture));
        return string.Join(", ", parts);
    }

    private DashboardDataTable BuildDataTable()
    {
        var texts = Texts;
        var visible = Visible;
        var columns = new List<DashboardDataColumn> { new("label", texts.ColumnLabel) };
        if (visible.Any(i => !string.IsNullOrEmpty(i.Description))) columns.Add(new("description", texts.ColumnDescription));
        if (visible.Any(i => i.Percent.HasValue)) columns.Add(new("percent", texts.ColumnPercent, Numeric: true));
        if (visible.Any(i => i.Delta.HasValue)) columns.Add(new("change", texts.ColumnChangePercent, Numeric: true));
        columns.Add(new("value", texts.ColumnValue, Numeric: true));
        var rows = visible.Select(i => (IReadOnlyDictionary<string, string?>)new Dictionary<string, string?>
        {
            ["label"] = i.Label,
            ["description"] = i.Description,
            ["percent"] = DashboardFormat.FormatInvariant(i.Percent),
            ["change"] = DashboardFormat.FormatInvariant(i.Delta),
            ["value"] = DashboardFormat.FormatInvariant(i.Value),
        }).ToList();
        return new DashboardDataTable(columns, rows);
    }

    private static BindableProperty Prop<T>(string name, T? defaultValue) =>
        BindableProperty.Create(name, typeof(T), typeof(KlyDashboardList), defaultValue, propertyChanged: (b, _, _) => ((KlyDashboardList)b).InvalidateContent());
}
