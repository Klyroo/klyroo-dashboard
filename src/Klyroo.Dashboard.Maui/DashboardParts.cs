using System.Globalization;
using Microsoft.Maui.Controls.Shapes;

namespace Klyroo.Dashboard.Maui;

/// <summary>Small building blocks shared by the dashboard views: themed colors and radius, labels, dividers, taps, and the few helpers the Klyroo.UI.Maui internals do not share.</summary>
internal static class DashboardParts
{
    /// <summary>Binds a color property to the light and dark values: it follows the app theme without any code of the control.</summary>
    public static void SetThemeColor(this BindableObject target, BindableProperty property, KlyThemeColor color) =>
        target.SetAppThemeColor(property, color.Light, color.Dark);

    /// <summary>Binds the border brush to the light and dark values.</summary>
    public static void SetStroke(this Border border, KlyThemeColor color) =>
        border.SetAppTheme<Brush>(Border.StrokeProperty, new SolidColorBrush(color.Light), new SolidColorBrush(color.Dark));

    /// <summary>Rounds the corners of a border.</summary>
    public static void SetRadius(this Border border, double radius) =>
        border.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(radius) };

    /// <summary>The corner radius of a fraction of the base radius (<c>0.75</c> is <c>--klyroo-radius-md</c>, <c>1</c> is <c>--klyroo-radius</c>).</summary>
    public static double RadiusOf(double fraction) => KlyTheme.Radius * fraction;

    /// <summary>The radius of a pill.</summary>
    public const double Pill = 9999;

    /// <summary>The fill of the primary color (<c>--klyroo-primary</c>).</summary>
    public static KlyThemeColor PrimaryFill => KlyTheme.Resolve(KlyColor.Primary).Fill;

    /// <summary>The text color of the primary color (<c>--klyroo-primary-text</c>).</summary>
    public static KlyThemeColor PrimaryText => KlyTheme.Resolve(KlyColor.Primary).Text;

    /// <summary>The text color of a semantic color (<c>--klyroo-success-text</c>).</summary>
    public static KlyThemeColor TextOf(KlyColor color) => KlyTheme.Resolve(color).Text;

    /// <summary>The fill of a semantic color.</summary>
    public static KlyThemeColor FillOf(KlyColor color) => KlyTheme.Resolve(color).Fill;

    /// <summary>A label with a size and a color that follows the app theme.</summary>
    public static Label NewLabel(double size, KlyThemeColor color, FontAttributes attributes = FontAttributes.None, LineBreakMode lineBreak = LineBreakMode.WordWrap)
    {
        var label = new Label { FontSize = size, FontAttributes = attributes, LineBreakMode = lineBreak };
        label.SetThemeColor(Label.TextColorProperty, color);
        return label;
    }

    /// <summary>Sets a text and hides the label when there is none.</summary>
    public static void Show(Label label, string? text)
    {
        label.Text = text;
        label.IsVisible = !string.IsNullOrEmpty(text);
    }

    /// <summary>A line of one device-independent unit: the dividers of the lists and tables.</summary>
    public static Border Divider(KlyThemeColor? color = null)
    {
        var line = new Border { StrokeThickness = 0, HeightRequest = 1, HorizontalOptions = LayoutOptions.Fill, Padding = 0 };
        line.SetThemeColor(VisualElement.BackgroundColorProperty, color ?? KlyTheme.Border);
        AutomationProperties.SetIsInAccessibleTree(line, false);
        return line;
    }

    /// <summary>A borderless native button that looks like a text action ("Retry", a link).</summary>
    public static Button TextButton(string? text, KlyThemeColor color, double fontSize, Action onClick, FontAttributes attributes = FontAttributes.None)
    {
        var button = new Button
        {
            Text = text,
            FontSize = fontSize,
            FontAttributes = attributes,
            Padding = new Thickness(0),
            MinimumHeightRequest = 40,
            BackgroundColor = Colors.Transparent,
            BorderWidth = 0,
            TextTransform = TextTransform.None,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Center,
        };
        button.SetThemeColor(Button.TextColorProperty, color);
        button.Clicked += (_, _) => onClick();
        return button;
    }

    /// <summary>Takes a view out of the view that holds it, so that it can be put in another one.</summary>
    public static void Detach(View? view)
    {
        switch (view?.Parent)
        {
            case Layout layout:
                layout.Remove(view);
                break;
            case Border border:
                border.Content = null;
                break;
            case ContentView contentView:
                contentView.Content = null;
                break;
            case ScrollView scroll:
                scroll.Content = null;
                break;
        }
    }

    /// <summary>
    /// Makes the children of a layout the given views in the given order, touching only what differs: a view that is already in its place is not taken out and put back,
    /// which would close a menu it holds, take the focus of a box in it, or reset its scroll position.
    /// </summary>
    public static void Sync(Layout layout, IReadOnlyList<View> wanted)
    {
        foreach (var child in layout.Children.OfType<View>().Where(c => !wanted.Contains(c)).ToList()) layout.Remove(child);
        for (var i = 0; i < wanted.Count; i++)
        {
            var view = wanted[i];
            if (i < layout.Children.Count && ReferenceEquals(layout.Children[i], view)) continue;
            if (ReferenceEquals(view.Parent, layout)) layout.Remove(view);
            else Detach(view);
            layout.Insert(i, view);
        }
    }

    /// <summary>A monospace font family of the platform. The default one when the platform cannot be told.</summary>
    public static string MonoFont()
    {
        try
        {
            var platform = DeviceInfo.Platform;
            if (platform == DevicePlatform.iOS || platform == DevicePlatform.MacCatalyst) return "Menlo";
            if (platform == DevicePlatform.WinUI) return "Consolas";
        }
        catch (Exception)
        {
            // Not running on a platform (unit tests): the generic family name.
        }
        return "monospace";
    }

    /// <summary>Runs <paramref name="action"/> on the UI thread of <paramref name="owner"/> (next turn of its loop), or at once when it has no dispatcher (a unit test).</summary>
    public static void Post(Element owner, Action action)
    {
        IDispatcher? dispatcher;
        try
        {
            dispatcher = owner.Dispatcher;
        }
        catch (InvalidOperationException)
        {
            // No app and no dispatcher on this thread (a unit test): there is no UI thread to go back to.
            dispatcher = null;
        }
        if (dispatcher is not null) dispatcher.Dispatch(action);
        else action();
    }

    /// <summary>The culture that formats numbers and dates.</summary>
    public static CultureInfo FormatCulture => CultureInfo.CurrentCulture;

    /// <summary>The share of a track a value fills, as a two-column grid of star widths: <paramref name="percent"/> of the width is the first column.</summary>
    public static Grid Share(double percent, View filled)
    {
        var clamped = Math.Clamp(percent, 0, 100);
        var grid = new Grid { ColumnSpacing = 0, HorizontalOptions = LayoutOptions.Fill };
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(Math.Max(clamped, 0.0001), GridUnitType.Star)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(Math.Max(100 - clamped, 0.0001), GridUnitType.Star)));
        filled.IsVisible = clamped > 0;
        grid.Add(filled, 0);
        return grid;
    }

    /// <summary>
    /// A horizontal progress bar of <paramref name="height"/>: a track in the muted background and a fill of <paramref name="percent"/> of it, with rounded ends.
    /// </summary>
    public static View ProgressBar(double percent, double height, KlyThemeColor fill)
    {
        var bar = new Border { StrokeThickness = 0, HeightRequest = height, Padding = 0, HorizontalOptions = LayoutOptions.Fill };
        bar.SetRadius(Pill);
        bar.SetThemeColor(VisualElement.BackgroundColorProperty, KlyTheme.BackgroundMuted);
        var filled = new Border { StrokeThickness = 0, HeightRequest = height, Padding = 0 };
        filled.SetThemeColor(VisualElement.BackgroundColorProperty, fill);
        bar.Content = Share(percent, filled);
        AutomationProperties.SetIsInAccessibleTree(bar, false);
        return bar;
    }

    /// <summary>The percent text of a progress, <c>"42%"</c>, rounded.</summary>
    public static string PercentText(double percent) => Math.Round(percent).ToString(CultureInfo.CurrentCulture) + "%";

    /// <summary>A pill with a background that holds a few views: the delta of a metric.</summary>
    public static Border Capsule(KlyThemeColor background, params View[] content)
    {
        var pill = new Border { StrokeThickness = 0, Padding = new Thickness(8, 2), HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Center };
        pill.SetRadius(Pill);
        pill.SetThemeColor(VisualElement.BackgroundColorProperty, background);
        var row = new HorizontalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center };
        foreach (var view in content) row.Add(view);
        pill.Content = row;
        return pill;
    }

    /// <summary>The heading level of a screen reader for a level from 1 to 6.</summary>
    public static SemanticHeadingLevel HeadingLevel(int level) => Math.Clamp(level, 1, 6) switch
    {
        1 => SemanticHeadingLevel.Level1,
        2 => SemanticHeadingLevel.Level2,
        3 => SemanticHeadingLevel.Level3,
        4 => SemanticHeadingLevel.Level4,
        5 => SemanticHeadingLevel.Level5,
        _ => SemanticHeadingLevel.Level6,
    };

    /// <summary>The kind of tone a change of a metric has: neutral when it is zero, good or bad otherwise.</summary>
    public static (KlyThemeColor Background, KlyThemeColor Text) DeltaTone(double delta, bool good)
    {
        if (DashboardDelta.IsZero(delta)) return (KlyTheme.BackgroundMuted, KlyTheme.TextMuted);
        var color = good ? KlyColor.Success : KlyColor.Error;
        return (FillOf(color).WithAlpha(0.15f), TextOf(color));
    }

    /// <summary>The text color of a change shown without a pill: muted for zero, success or error otherwise.</summary>
    public static KlyThemeColor DeltaText(double delta, bool good) =>
        DashboardDelta.IsZero(delta) ? KlyTheme.TextMuted : TextOf(good ? KlyColor.Success : KlyColor.Error);

    /// <summary>A number with at most one decimal in invariant culture (<c>"12.5"</c>), as the web formatters write a width.</summary>
    public static string Invariant(double value) => (value == 0 ? 0 : value).ToString("0.##", CultureInfo.InvariantCulture);
}
