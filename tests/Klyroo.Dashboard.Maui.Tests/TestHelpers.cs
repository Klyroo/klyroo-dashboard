using System.Globalization;
using Klyroo.Dashboard.Maui;

namespace Klyroo.Dashboard.Maui.Tests;

/// <summary>Reads the views the controls build, with no device: the tree of a control, its texts and its colors.</summary>
internal static class TestHelpers
{
    public static IEnumerable<object> Walk(object root)
    {
        yield return root;
        IEnumerable<object?> kids = root switch
        {
            KlyDashboardGrid grid => grid.Inner.Children.Cast<object?>(),
            Layout layout => layout.Children.Cast<object?>(),
            Border border => [border.Content],
            ContentView content => [content.Content],
            ScrollView scroll => [scroll.Content],
            _ => [],
        };
        foreach (var kid in kids)
        {
            if (kid is null) continue;
            foreach (var descendant in Walk(kid)) yield return descendant;
        }
    }

    public static List<T> All<T>(object root) => Walk(root).OfType<T>().ToList();

    /// <summary>The texts of the labels that are visible, in tree order.</summary>
    public static List<string> Texts(object root) => All<Label>(root).Where(l => l.IsVisible && !string.IsNullOrEmpty(l.Text)).Select(l => l.Text!).ToList();

    public static Label LabelWith(object root, string text) => All<Label>(root).First(l => l.Text == text);

    /// <summary>The view a card shows for its content phase.</summary>
    public static View PhaseOf(KlyDashboardCard card) => Assert.IsAssignableFrom<View>(card.PhaseHost.Content);

    public static void Invariant() => CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

    /// <summary>The text color of a label, in the light theme (the tests run with no app: the light value).</summary>
    public static Color Light(KlyThemeColor color) => color.Light;
}
