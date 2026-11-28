namespace Klyroo.Dashboard.Maui;

/// <summary>A list of alerts ordered by severity, each with an optional value and an action link or button.</summary>
public class KlyDashboardAlerts : KlyDashboardCard
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = Prop<IReadOnlyList<DashboardAlertItem>?>(nameof(Items), null);
    /// <summary>Identifies <see cref="Order"/>.</summary>
    public static readonly BindableProperty OrderProperty = Prop(nameof(Order), DashboardAlertOrder.Severity);
    /// <summary>Identifies <see cref="Limit"/>.</summary>
    public static readonly BindableProperty LimitProperty = Prop<int?>(nameof(Limit), null);

    private EventHandler<DashboardAlertItem>? _action;

    /// <summary>Creates the block.</summary>
    public KlyDashboardAlerts()
    {
        Span = 6;
        Skeleton = DashboardSkeletonKind.Rows;
        Tabulate = BuildDataTable;
    }

    /// <summary>
    /// Raised with the alert when the user taps its action. An alert with an <see cref="DashboardAlertItem.ActionLabel"/> and no <see cref="DashboardAlertItem.ActionHref"/> shows its action only when something listens;
    /// without a subscriber the action is left out. An alert with an <see cref="DashboardAlertItem.ActionHref"/> always shows a link.
    /// </summary>
    public event EventHandler<DashboardAlertItem>? ActionInvoked
    {
        add
        {
            _action += value;
            InvalidateContent();
        }
        remove
        {
            _action -= value;
            InvalidateContent();
        }
    }

    /// <summary>The alerts to display.</summary>
    public IReadOnlyList<DashboardAlertItem>? Items { get => (IReadOnlyList<DashboardAlertItem>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary><see cref="DashboardAlertOrder.Severity"/> (the default) puts errors first and successes last. <see cref="DashboardAlertOrder.AsGiven"/> keeps the order of <see cref="Items"/>.</summary>
    public DashboardAlertOrder Order { get => (DashboardAlertOrder)GetValue(OrderProperty); set => SetValue(OrderProperty, value); }

    /// <summary>The maximum number of alerts shown, applied after ordering. The table view and the CSV export hold the same alerts.</summary>
    public int? Limit { get => (int?)GetValue(LimitProperty); set => SetValue(LimitProperty, value); }

    /// <summary>The alerts shown, in order.</summary>
    internal IReadOnlyList<DashboardAlertItem> OrderedItems
    {
        get
        {
            if (Items is not { Count: > 0 } items) return [];
            var sorted = Order == DashboardAlertOrder.Severity ? items.OrderBy(i => (int)i.Severity).ToList() : items.ToList();
            return Limit is > 0 ? sorted.Take(Limit.Value).ToList() : sorted;
        }
    }

    private static KlyColor ColorOf(DashboardAlertSeverity severity) => severity switch
    {
        DashboardAlertSeverity.Error => KlyColor.Error,
        DashboardAlertSeverity.Warning => KlyColor.Warning,
        DashboardAlertSeverity.Success => KlyColor.Success,
        _ => KlyColor.Info,
    };

    private static string LabelOf(DashboardTexts texts, DashboardAlertSeverity severity) => severity switch
    {
        DashboardAlertSeverity.Error => texts.SeverityCritical,
        DashboardAlertSeverity.Warning => texts.SeverityWarning,
        DashboardAlertSeverity.Success => texts.SeverityResolved,
        _ => texts.SeverityInfo,
    };

    /// <inheritdoc />
    protected override void OnRefreshing()
    {
        var empty = Items is not { Count: > 0 };
        if (IsEmpty != empty) IsEmpty = empty;
        var title = Texts.NothingNeedsAttention;
        if (EmptyTitle != title) EmptyTitle = title;
    }

    /// <inheritdoc />
    protected override View? BuildBody()
    {
        var texts = Texts;
        var stack = new VerticalStackLayout { Spacing = 0 };
        var items = OrderedItems;
        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0) stack.Add(DashboardParts.Divider());
            stack.Add(BuildRow(texts, items[i], i == 0, i == items.Count - 1));
        }
        return stack;
    }

    private View BuildRow(DashboardTexts texts, DashboardAlertItem item, bool first, bool last)
    {
        var row = new Grid { ColumnSpacing = 12, Padding = new Thickness(0, first ? 0 : 8, 0, last ? 0 : 8) };
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        var left = new HorizontalStackLayout { Spacing = 10, VerticalOptions = LayoutOptions.Center };
        left.Add(new KlyBadge { Text = LabelOf(texts, item.Severity), Color = ColorOf(item.Severity), Variant = KlyVariant.Subtle, Size = KlySize.Sm });
        var names = new VerticalStackLayout { Spacing = 0, VerticalOptions = LayoutOptions.Center };
        var label = DashboardParts.NewLabel(12, KlyTheme.TextHighlighted, FontAttributes.Bold, LineBreakMode.TailTruncation);
        label.Text = item.Label;
        names.Add(label);
        if (!string.IsNullOrEmpty(item.Description))
        {
            var description = DashboardParts.NewLabel(11, KlyTheme.TextMuted, lineBreak: LineBreakMode.TailTruncation);
            description.Text = item.Description;
            names.Add(description);
        }
        left.Add(names);
        row.Add(left, 0);

        var right = new HorizontalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };
        if (!string.IsNullOrEmpty(item.Value))
        {
            var value = DashboardParts.NewLabel(12, KlyTheme.TextHighlighted, FontAttributes.Bold, LineBreakMode.NoWrap);
            value.Text = item.Value;
            value.VerticalOptions = LayoutOptions.Center;
            right.Add(value);
        }
        if (!string.IsNullOrEmpty(item.ActionLabel))
        {
            if (!string.IsNullOrEmpty(item.ActionHref))
            {
                var href = item.ActionHref;
                right.Add(DashboardParts.TextButton(item.ActionLabel, DashboardParts.PrimaryText, 12, () => DashboardLaunch.Open(href), FontAttributes.Bold));
            }
            else if (_action is not null)
            {
                right.Add(DashboardParts.TextButton(item.ActionLabel, DashboardParts.PrimaryText, 12, () => _action?.Invoke(this, item), FontAttributes.Bold));
            }
        }
        row.Add(right, 1);
        return row;
    }

    private DashboardDataTable BuildDataTable()
    {
        var texts = Texts;
        var columns = new List<DashboardDataColumn>
        {
            new("severity", texts.ColumnSeverity),
            new("label", texts.ColumnAlert),
            new("description", texts.ColumnDescription),
            new("value", texts.ColumnValue),
        };
        var rows = OrderedItems.Select(i => (IReadOnlyDictionary<string, string?>)new Dictionary<string, string?>
        {
            ["severity"] = LabelOf(texts, i.Severity),
            ["label"] = i.Label,
            ["description"] = i.Description,
            ["value"] = i.Value,
        }).ToList();
        return new DashboardDataTable(columns, rows);
    }

    private static BindableProperty Prop<T>(string name, T? defaultValue) =>
        BindableProperty.Create(name, typeof(T), typeof(KlyDashboardAlerts), defaultValue, propertyChanged: (b, _, _) => ((KlyDashboardAlerts)b).InvalidateContent());
}
