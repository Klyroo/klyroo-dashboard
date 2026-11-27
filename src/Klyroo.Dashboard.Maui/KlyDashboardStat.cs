namespace Klyroo.Dashboard.Maui;

/// <summary>A single metric with an optional delta, sparkline, goal bar and status pill.</summary>
public class KlyDashboardStat : KlyDashboardCard
{
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = Prop<double?>(nameof(Value), null);
    /// <summary>Identifies <see cref="TextValue"/>.</summary>
    public static readonly BindableProperty TextValueProperty = Prop<string?>(nameof(TextValue), null);
    /// <summary>Identifies <see cref="Format"/>.</summary>
    public static readonly BindableProperty FormatProperty = Prop(nameof(Format), DashboardFormatPreset.Number);
    /// <summary>Identifies <see cref="Delta"/>.</summary>
    public static readonly BindableProperty DeltaProperty = Prop<double?>(nameof(Delta), null);
    /// <summary>Identifies <see cref="DeltaFormat"/>.</summary>
    public static readonly BindableProperty DeltaFormatProperty = Prop<DashboardFormatPreset?>(nameof(DeltaFormat), null);
    /// <summary>Identifies <see cref="InvertDelta"/>.</summary>
    public static readonly BindableProperty InvertDeltaProperty = Prop(nameof(InvertDelta), false);
    /// <summary>Identifies <see cref="Compare"/>.</summary>
    public static readonly BindableProperty CompareProperty = Prop<double?>(nameof(Compare), null);
    /// <summary>Identifies <see cref="CompareMode"/>.</summary>
    public static readonly BindableProperty CompareModeProperty = Prop(nameof(CompareMode), DashboardCompareMode.Change);
    /// <summary>Identifies <see cref="Caption"/>.</summary>
    public static readonly BindableProperty CaptionProperty = Prop<string?>(nameof(Caption), null);
    /// <summary>Identifies <see cref="Trend"/>.</summary>
    public static readonly BindableProperty TrendProperty = Prop<IReadOnlyList<double>?>(nameof(Trend), null);
    /// <summary>Identifies <see cref="Goal"/>.</summary>
    public static readonly BindableProperty GoalProperty = Prop<double?>(nameof(Goal), null);
    /// <summary>Identifies <see cref="Status"/>.</summary>
    public static readonly BindableProperty StatusProperty = Prop<DashboardStatus?>(nameof(Status), null);

    private readonly KlyBadge _status = new() { Variant = KlyVariant.Subtle, Size = KlySize.Sm, VerticalOptions = LayoutOptions.Center };

    /// <summary>Creates a stat.</summary>
    public KlyDashboardStat()
    {
        Span = 4;
        Skeleton = DashboardSkeletonKind.Stat;
        Expandable = false;
        Tabulate = BuildDataTable;
    }

    /// <summary>The name of the metric, shown as the title of the card. Required.</summary>
    public string Label { get => Title ?? string.Empty; set => Title = value; }

    /// <summary>The value, formatted with <see cref="Format"/>.</summary>
    public double? Value { get => (double?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Text shown instead of the formatted <see cref="Value"/>, for values that are not numbers.</summary>
    public string? TextValue { get => (string?)GetValue(TextValueProperty); set => SetValue(TextValueProperty, value); }

    /// <summary>How <see cref="Value"/>, <see cref="Compare"/> and <see cref="Goal"/> are formatted.</summary>
    public DashboardFormatPreset Format { get => (DashboardFormatPreset)GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    /// <summary>The change to display, in percent. It takes precedence over <see cref="Compare"/>.</summary>
    public double? Delta { get => (double?)GetValue(DeltaProperty); set => SetValue(DeltaProperty, value); }

    /// <summary>How the delta is formatted. Defaults to <c>Delta</c>, or to <c>Points</c> or <c>Signed</c> when <see cref="CompareMode"/> is <see cref="DashboardCompareMode.Difference"/>.</summary>
    public DashboardFormatPreset? DeltaFormat { get => (DashboardFormatPreset?)GetValue(DeltaFormatProperty); set => SetValue(DeltaFormatProperty, value); }

    /// <summary>When set, a negative delta is shown as good news (for churn, cost or latency).</summary>
    public bool InvertDelta { get => (bool)GetValue(InvertDeltaProperty); set => SetValue(InvertDeltaProperty, value); }

    /// <summary>The previous value. When <see cref="Delta"/> is not set, the delta is computed from it, and the default caption names it.</summary>
    public double? Compare { get => (double?)GetValue(CompareProperty); set => SetValue(CompareProperty, value); }

    /// <summary><see cref="DashboardCompareMode.Change"/> (the default) computes the percent change from <see cref="Compare"/> and shows nothing when <see cref="Compare"/> is zero. <see cref="DashboardCompareMode.Difference"/> uses the raw difference, multiplied by 100 for the <c>Ratio</c> format.</summary>
    public DashboardCompareMode CompareMode { get => (DashboardCompareMode)GetValue(CompareModeProperty); set => SetValue(CompareModeProperty, value); }

    /// <summary>A line of text under the value. Replaces the default <c>vs ... previous period</c> caption.</summary>
    public string? Caption { get => (string?)GetValue(CaptionProperty); set => SetValue(CaptionProperty, value); }

    /// <summary>Points of a sparkline. It needs at least two values.</summary>
    public IReadOnlyList<double>? Trend { get => (IReadOnlyList<double>?)GetValue(TrendProperty); set => SetValue(TrendProperty, value); }

    /// <summary>A target that adds a progress bar and a percentage. It must be greater than 0.</summary>
    public double? Goal { get => (double?)GetValue(GoalProperty); set => SetValue(GoalProperty, value); }

    /// <summary>A status pill in the header.</summary>
    public DashboardStatus? Status { get => (DashboardStatus?)GetValue(StatusProperty); set => SetValue(StatusProperty, value); }

    /// <summary>The delta shown: <see cref="Delta"/>, else the change from <see cref="Compare"/>, else <see langword="null"/>.</summary>
    internal double? EffectiveDelta
    {
        get
        {
            if (Delta.HasValue) return Delta;
            if (Value is { } current && Compare is { } previous)
            {
                if (CompareMode == DashboardCompareMode.Difference)
                {
                    var scale = Format == DashboardFormatPreset.Ratio ? 100.0 : 1.0;
                    return (current - previous) * scale;
                }
                if (Math.Abs(previous) > 0.000001) return (current - previous) / Math.Abs(previous) * 100.0;
            }
            return null;
        }
    }

    /// <summary>How the delta is formatted: <see cref="DeltaFormat"/>, or the default of the compare mode.</summary>
    internal DashboardFormatPreset EffectiveDeltaFormat =>
        DeltaFormat ?? (CompareMode == DashboardCompareMode.Difference
            ? (Format is DashboardFormatPreset.Percent or DashboardFormatPreset.Ratio ? DashboardFormatPreset.Points : DashboardFormatPreset.Signed)
            : DashboardFormatPreset.Delta);

    /// <summary>The caption: <see cref="Caption"/>, else the previous value when there is one.</summary>
    internal string? EffectiveCaption =>
        !string.IsNullOrEmpty(Caption) ? Caption
        : Compare.HasValue ? Texts.VersusPrevious(DashboardFormat.Format(Compare, Format, FormatCulture))
        : null;

    /// <summary>The status pill of the header, for the tests.</summary>
    internal KlyBadge StatusBadge => _status;

    /// <inheritdoc />
    protected override void OnRefreshing()
    {
        if (Status is { } status)
        {
            _status.Text = status.Label;
            _status.Color = status.Color is not null ? new KlyColor(status.Color) : KlyColor.Neutral;
        }
        var wanted = Status is null ? null : _status;
        if (!ReferenceEquals(HeaderRight, wanted)) HeaderRight = wanted;
    }

    /// <inheritdoc />
    protected override View? BuildBody()
    {
        var texts = Texts;
        var stack = new VerticalStackLayout { Spacing = 8 };

        var row = new Grid { ColumnSpacing = 8 };
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        var value = DashboardParts.NewLabel(24, KlyTheme.TextHighlighted, FontAttributes.Bold, LineBreakMode.NoWrap);
        value.Text = TextValue ?? DashboardFormat.Format(Value, Format, FormatCulture);
        value.VerticalOptions = LayoutOptions.Center;
        row.Add(value, 0);
        if (EffectiveDelta is { } delta && !double.IsNaN(delta)) row.Add(DeltaPill(delta), 1);
        stack.Add(row);

        if (Trend is { Count: >= 2 } trend) stack.Add(new DashboardSparkline { Points = trend });

        if (Goal is > 0 && Value is { } current)
        {
            var percent = Math.Clamp(current / Goal.Value * 100.0, 0, 100);
            var goal = new VerticalStackLayout { Spacing = 4, Padding = new Thickness(0, 4, 0, 0) };
            var labels = new Grid { ColumnSpacing = 8 };
            labels.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            labels.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            var name = DashboardParts.NewLabel(12, KlyTheme.TextMuted);
            name.Text = texts.Goal(DashboardFormat.Format(Goal, Format, FormatCulture));
            var share = DashboardParts.NewLabel(12, KlyTheme.TextMuted, FontAttributes.Bold, LineBreakMode.NoWrap);
            share.Text = DashboardParts.PercentText(percent);
            labels.Add(name, 0);
            labels.Add(share, 1);
            goal.Add(labels);
            goal.Add(DashboardParts.ProgressBar(percent, 6, percent >= 100 ? DashboardParts.FillOf(KlyColor.Success) : DashboardParts.PrimaryFill));
            stack.Add(goal);
        }

        if (!string.IsNullOrEmpty(EffectiveCaption))
        {
            var caption = DashboardParts.NewLabel(12, KlyTheme.TextMuted);
            caption.Text = EffectiveCaption;
            stack.Add(caption);
        }
        return stack;
    }

    private View DeltaPill(double delta)
    {
        var good = InvertDelta ? delta < 0 : delta > 0;
        var (background, text) = DashboardParts.DeltaTone(delta, good);
        var arrow = DashboardParts.NewLabel(12, text, FontAttributes.Bold, LineBreakMode.NoWrap);
        arrow.Text = delta > 0 ? "↑" : delta < 0 ? "↓" : "–";
        AutomationProperties.SetIsInAccessibleTree(arrow, false);
        var number = DashboardParts.NewLabel(12, text, FontAttributes.Bold, LineBreakMode.NoWrap);
        number.Text = DashboardFormat.Format(delta, EffectiveDeltaFormat, FormatCulture);
        return DashboardParts.Capsule(background, arrow, number);
    }

    private DashboardDataTable BuildDataTable()
    {
        var texts = Texts;
        var columns = new List<DashboardDataColumn>
        {
            new("label", texts.ColumnLabel),
            new("value", texts.ColumnValue, Numeric: true),
        };
        var row = new Dictionary<string, string?>
        {
            ["label"] = Label,
            ["value"] = TextValue ?? DashboardFormat.FormatInvariant(Value),
        };
        if (Compare.HasValue)
        {
            columns.Add(new("previous", texts.ColumnPrevious, Numeric: true));
            row["previous"] = DashboardFormat.FormatInvariant(Compare);
        }
        if (EffectiveDelta is { } delta && !double.IsNaN(delta))
        {
            columns.Add(new("change", ChangeLabel(texts), Numeric: true));
            row["change"] = DashboardFormat.FormatInvariant(delta);
        }
        if (Goal is > 0)
        {
            columns.Add(new("goal", texts.ColumnGoal, Numeric: true));
            row["goal"] = DashboardFormat.FormatInvariant(Goal);
        }
        return new DashboardDataTable(columns, [row]);
    }

    private string ChangeLabel(DashboardTexts texts) => EffectiveDeltaFormat switch
    {
        DashboardFormatPreset.Delta or DashboardFormatPreset.Percent => texts.ColumnChangePercent,
        DashboardFormatPreset.Points => texts.ColumnChangePoints,
        _ => texts.ColumnChange,
    };

    private static BindableProperty Prop<T>(string name, T? defaultValue) =>
        BindableProperty.Create(name, typeof(T), typeof(KlyDashboardStat), defaultValue, propertyChanged: (b, _, _) => ((KlyDashboardStat)b).InvalidateContent());
}
