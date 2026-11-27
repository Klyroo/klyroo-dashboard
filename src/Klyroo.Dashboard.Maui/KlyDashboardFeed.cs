using System.Globalization;

namespace Klyroo.Dashboard.Maui;

/// <summary>A timeline of events with relative times, grouped by day.</summary>
public class KlyDashboardFeed : KlyDashboardCard
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = Prop<IReadOnlyList<DashboardFeedItem>?>(nameof(Items), null);
    /// <summary>Identifies <see cref="GroupByDay"/>.</summary>
    public static readonly BindableProperty GroupByDayProperty = Prop(nameof(GroupByDay), true);
    /// <summary>Identifies <see cref="Now"/>.</summary>
    public static readonly BindableProperty NowProperty = Prop<DateTimeOffset?>(nameof(Now), null);
    /// <summary>Identifies <see cref="Limit"/>.</summary>
    public static readonly BindableProperty LimitProperty = Prop<int?>(nameof(Limit), null);

    /// <summary>Creates the block.</summary>
    public KlyDashboardFeed()
    {
        Span = 6;
        Skeleton = DashboardSkeletonKind.Feed;
        Tabulate = BuildDataTable;
    }

    /// <summary>The events, in display order. They are not sorted.</summary>
    public IReadOnlyList<DashboardFeedItem>? Items { get => (IReadOnlyList<DashboardFeedItem>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>Inserts a heading (<c>Today</c>, <c>Yesterday</c> or an ISO date) when the day changes. Defaults to <see langword="true"/>.</summary>
    public bool GroupByDay { get => (bool)GetValue(GroupByDayProperty); set => SetValue(GroupByDayProperty, value); }

    /// <summary>The reference moment for the day headings and the relative times. The current UTC time when <see langword="null"/>; set it to get deterministic output in tests.</summary>
    public DateTimeOffset? Now { get => (DateTimeOffset?)GetValue(NowProperty); set => SetValue(NowProperty, value); }

    /// <summary>The maximum number of events shown. The table view and the CSV export hold the same events.</summary>
    public int? Limit { get => (int?)GetValue(LimitProperty); set => SetValue(LimitProperty, value); }

    /// <summary>The events shown: the first <see cref="Limit"/> items.</summary>
    internal IReadOnlyList<DashboardFeedItem> Visible => Items is { Count: > 0 } items ? (Limit is > 0 ? items.Take(Limit.Value).ToList() : items) : [];

    /// <inheritdoc />
    protected override void OnRefreshing()
    {
        var empty = Items is not { Count: > 0 };
        if (IsEmpty != empty) IsEmpty = empty;
    }

    /// <inheritdoc />
    protected override View? BuildBody()
    {
        var texts = Texts;
        var stack = new VerticalStackLayout { Spacing = 12 };
        string? lastDay = null;
        foreach (var item in Visible)
        {
            if (GroupByDay)
            {
                var heading = DayHeading(texts, item.Time);
                if (heading != lastDay)
                {
                    lastDay = heading;
                    var day = DashboardParts.NewLabel(11, KlyTheme.TextMuted, FontAttributes.Bold, LineBreakMode.NoWrap);
                    day.Text = heading;
                    day.TextTransform = TextTransform.Uppercase;
                    day.CharacterSpacing = 0.8;
                    SemanticProperties.SetHeadingLevel(day, SemanticHeadingLevel.Level4);
                    stack.Add(day);
                }
            }
            stack.Add(BuildItem(texts, item));
        }
        return stack;
    }

    private View BuildItem(DashboardTexts texts, DashboardFeedItem item)
    {
        var row = new Grid { ColumnSpacing = 10 };
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        var avatar = new Border { WidthRequest = 24, HeightRequest = 24, StrokeThickness = 0, Padding = 0, VerticalOptions = LayoutOptions.Start, Margin = new Thickness(0, 2, 0, 0) };
        avatar.SetRadius(DashboardParts.Pill);
        avatar.SetThemeColor(BackgroundColorProperty, KlyTheme.BackgroundMuted);
        var initials = DashboardParts.NewLabel(10, KlyTheme.TextHighlighted, FontAttributes.Bold, LineBreakMode.NoWrap);
        initials.Text = item.AvatarText ?? "•";
        initials.HorizontalOptions = LayoutOptions.Center;
        initials.VerticalOptions = LayoutOptions.Center;
        avatar.Content = initials;
        AutomationProperties.SetIsInAccessibleTree(avatar, false);
        row.Add(avatar, 0);

        var body = new VerticalStackLayout { Spacing = 2 };
        var head = new Grid { ColumnSpacing = 8 };
        head.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        head.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        var label = DashboardParts.NewLabel(12, KlyTheme.TextHighlighted, FontAttributes.Bold);
        label.Text = item.Label;
        var time = DashboardParts.NewLabel(11, KlyTheme.TextMuted, lineBreak: LineBreakMode.NoWrap);
        time.Text = DashboardFormat.FormatRelativeTime(item.Time, Now, texts.RelativeTime);
        time.VerticalOptions = LayoutOptions.Start;
        SemanticProperties.SetDescription(time, $"{time.Text}, {item.Time:u}");
        head.Add(label, 0);
        head.Add(time, 1);
        body.Add(head);
        if (!string.IsNullOrEmpty(item.Description))
        {
            var description = DashboardParts.NewLabel(12, KlyTheme.TextMuted);
            description.Text = item.Description;
            body.Add(description);
        }
        row.Add(body, 1);
        return row;
    }

    private string DayHeading(DashboardTexts texts, DateTimeOffset time)
    {
        var now = (Now ?? DateTimeOffset.UtcNow).UtcDateTime.Date;
        var day = time.UtcDateTime.Date;
        if (day == now) return texts.Today;
        if (day == now.AddDays(-1)) return texts.Yesterday;
        return day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private DashboardDataTable BuildDataTable()
    {
        var texts = Texts;
        var columns = new List<DashboardDataColumn>
        {
            new("time", texts.ColumnTime),
            new("label", texts.ColumnEvent),
            new("description", texts.ColumnDescription),
        };
        var rows = Visible.Select(i => (IReadOnlyDictionary<string, string?>)new Dictionary<string, string?>
        {
            ["time"] = i.Time.ToString("O", CultureInfo.InvariantCulture),
            ["label"] = i.Label,
            ["description"] = i.Description,
        }).ToList();
        return new DashboardDataTable(columns, rows);
    }

    private static BindableProperty Prop<T>(string name, T? defaultValue) =>
        BindableProperty.Create(name, typeof(T), typeof(KlyDashboardFeed), defaultValue, propertyChanged: (b, _, _) => ((KlyDashboardFeed)b).InvalidateContent());
}
