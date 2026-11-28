using System.Globalization;

namespace Klyroo.Dashboard.Maui;

/// <summary>A gauge that shows a value between a minimum and a maximum, as a ring or a horizontal bar.</summary>
public class KlyDashboardGauge : KlyDashboardCard
{
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = Prop<double?>(nameof(Value), null);
    /// <summary>Identifies <see cref="Min"/>.</summary>
    public static readonly BindableProperty MinProperty = Prop(nameof(Min), 0d);
    /// <summary>Identifies <see cref="Max"/>.</summary>
    public static readonly BindableProperty MaxProperty = Prop(nameof(Max), 100d);
    /// <summary>Identifies <see cref="Target"/>.</summary>
    public static readonly BindableProperty TargetProperty = Prop<double?>(nameof(Target), null);
    /// <summary>Identifies <see cref="Format"/>.</summary>
    public static readonly BindableProperty FormatProperty = Prop(nameof(Format), DashboardFormatPreset.Number);
    /// <summary>Identifies <see cref="Caption"/>.</summary>
    public static readonly BindableProperty CaptionProperty = Prop<string?>(nameof(Caption), null);
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = Prop(nameof(Variant), DashboardGaugeVariant.Ring);

    /// <summary>Creates a gauge.</summary>
    public KlyDashboardGauge()
    {
        Span = 4;
        Skeleton = DashboardSkeletonKind.Gauge;
        Tabulate = BuildDataTable;
    }

    /// <summary>The current value.</summary>
    public double? Value { get => (double?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>The value at which the gauge is empty. Defaults to 0.</summary>
    public double Min { get => (double)GetValue(MinProperty); set => SetValue(MinProperty, value); }

    /// <summary>The value at which the gauge is full. Defaults to 100.</summary>
    public double Max { get => (double)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }

    /// <summary>A target shown as text under the gauge, and as a marker on the bar of the <see cref="DashboardGaugeVariant.Bar"/> variant.</summary>
    public double? Target { get => (double?)GetValue(TargetProperty); set => SetValue(TargetProperty, value); }

    /// <summary>How the value and the target are formatted.</summary>
    public DashboardFormatPreset Format { get => (DashboardFormatPreset)GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    /// <summary>A line of text under the value inside the ring, or next to the value in the <see cref="DashboardGaugeVariant.Bar"/> variant.</summary>
    public string? Caption { get => (string?)GetValue(CaptionProperty); set => SetValue(CaptionProperty, value); }

    /// <summary><see cref="DashboardGaugeVariant.Ring"/> (the default) draws a circular gauge with the value in the middle. <see cref="DashboardGaugeVariant.Bar"/> draws a horizontal meter with the value above it, the minimum and maximum under it and a marker at <see cref="Target"/>.</summary>
    public DashboardGaugeVariant Variant { get => (DashboardGaugeVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>The filled fraction of the gauge, from 0 to 1: <c>(Value - Min) / (Max - Min)</c>, clamped. 0 when <see cref="Value"/> is <see langword="null"/>, NaN or infinite, or <see cref="Max"/> is not above <see cref="Min"/>.</summary>
    public double Share
    {
        get
        {
            var span = Max - Min;
            if (span <= 0 || Value is not { } value || !double.IsFinite(value)) return 0;
            return Math.Clamp((value - Min) / span, 0, 1);
        }
    }

    /// <summary>The place of the target on the gauge, from 0 to 1, or <see langword="null"/> without a target.</summary>
    internal double? TargetShare
    {
        get
        {
            var span = Max - Min;
            if (span <= 0 || Target is not { } target || double.IsNaN(target)) return null;
            return Math.Clamp((target - Min) / span, 0, 1);
        }
    }

    // NaN or infinity reads as no value, so nothing shows "NaN".
    private double? FiniteValue => Value is { } v && double.IsFinite(v) ? v : null;

    /// <summary>The value as shown.</summary>
    internal string FormattedValue => DashboardFormat.Format(FiniteValue, Format, FormatCulture);

    private bool IsBar => Variant == DashboardGaugeVariant.Bar;

    /// <inheritdoc />
    protected override View? BuildBody()
    {
        var texts = Texts;
        var stack = new VerticalStackLayout { Spacing = 8, Padding = new Thickness(0, 8), HorizontalOptions = LayoutOptions.Fill };
        var name = Title ?? texts.Gauge;
        stack.Add(IsBar ? BuildBar() : BuildRing());
        if (Target.HasValue)
        {
            var target = DashboardParts.NewLabel(12, KlyTheme.TextMuted);
            target.Text = texts.Target(DashboardFormat.Format(Target, Format, FormatCulture));
            target.HorizontalTextAlignment = IsBar ? TextAlignment.Start : TextAlignment.Center;
            stack.Add(target);
        }
        // The role of a meter: the name and the value, with the range for a screen reader.
        SemanticProperties.SetDescription(stack, $"{name}: {FormattedValue}");
        SemanticProperties.SetHint(stack, $"{Min.ToString("0.##", CultureInfo.InvariantCulture)} - {Max.ToString("0.##", CultureInfo.InvariantCulture)}");
        return stack;
    }

    private View BuildRing()
    {
        var grid = new Grid { WidthRequest = 140, HeightRequest = 140, HorizontalOptions = LayoutOptions.Center };
        grid.Add(new DashboardGaugeRing { Share = Share });
        var center = new VerticalStackLayout { Spacing = 0, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, InputTransparent = true };
        var value = DashboardParts.NewLabel(20, KlyTheme.TextHighlighted, FontAttributes.Bold, LineBreakMode.NoWrap);
        value.Text = FormattedValue;
        value.HorizontalTextAlignment = TextAlignment.Center;
        center.Add(value);
        if (!string.IsNullOrEmpty(Caption))
        {
            var caption = DashboardParts.NewLabel(12, KlyTheme.TextMuted);
            caption.Text = Caption;
            caption.HorizontalTextAlignment = TextAlignment.Center;
            center.Add(caption);
        }
        grid.Add(center);
        return grid;
    }

    private View BuildBar()
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        var head = new Grid { ColumnSpacing = 8 };
        head.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        head.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        var value = DashboardParts.NewLabel(20, KlyTheme.TextHighlighted, FontAttributes.Bold, LineBreakMode.NoWrap);
        value.Text = FormattedValue;
        head.Add(value, 0);
        if (!string.IsNullOrEmpty(Caption))
        {
            var caption = DashboardParts.NewLabel(12, KlyTheme.TextMuted);
            caption.Text = Caption;
            caption.HorizontalTextAlignment = TextAlignment.End;
            caption.VerticalOptions = LayoutOptions.End;
            head.Add(caption, 1);
        }
        stack.Add(head);
        stack.Add(new DashboardGaugeBar { Share = Share, TargetShare = TargetShare });

        var scale = new Grid();
        scale.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        scale.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        var min = DashboardParts.NewLabel(11, KlyTheme.TextMuted, lineBreak: LineBreakMode.NoWrap);
        min.Text = DashboardFormat.Format(Min, Format, FormatCulture);
        var max = DashboardParts.NewLabel(11, KlyTheme.TextMuted, lineBreak: LineBreakMode.NoWrap);
        max.Text = DashboardFormat.Format(Max, Format, FormatCulture);
        max.HorizontalTextAlignment = TextAlignment.End;
        scale.Add(min, 0);
        scale.Add(max, 1);
        stack.Add(scale);
        return stack;
    }

    private DashboardDataTable BuildDataTable()
    {
        var texts = Texts;
        var columns = new List<DashboardDataColumn>
        {
            new("label", texts.ColumnLabel),
            new("value", texts.ColumnValue, Numeric: true),
            new("min", texts.ColumnMinimum, Numeric: true),
            new("max", texts.ColumnMaximum, Numeric: true),
        };
        var row = new Dictionary<string, string?>
        {
            ["label"] = Title ?? texts.Gauge,
            ["value"] = DashboardFormat.FormatInvariant(FiniteValue),
            ["min"] = DashboardFormat.FormatInvariant(Min),
            ["max"] = DashboardFormat.FormatInvariant(Max),
        };
        if (Target.HasValue)
        {
            columns.Add(new("target", texts.ColumnTarget, Numeric: true));
            row["target"] = DashboardFormat.FormatInvariant(Target);
        }
        return new DashboardDataTable(columns, [row]);
    }

    private static BindableProperty Prop<T>(string name, T? defaultValue) =>
        BindableProperty.Create(name, typeof(T), typeof(KlyDashboardGauge), defaultValue, propertyChanged: (b, _, _) => ((KlyDashboardGauge)b).InvalidateContent());
}
