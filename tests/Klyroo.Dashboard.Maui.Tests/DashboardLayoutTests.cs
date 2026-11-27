using System.Globalization;
using static Klyroo.Dashboard.Maui.Tests.TestHelpers;

namespace Klyroo.Dashboard.Maui.Tests;

public class DashboardLayoutTests
{
    private static KlyDashboardCard Block(int span) => new() { Span = span };

    private static (int Row, int Column, int Span) At(View view) => (Grid.GetRow(view), Grid.GetColumn(view), Grid.GetColumnSpan(view));

    // --- breakpoints -----------------------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(320, "Base")]
    [InlineData(639.9, "Base")]
    [InlineData(640, "Sm")]
    [InlineData(767, "Sm")]
    [InlineData(768, "Md")]
    [InlineData(1023, "Md")]
    [InlineData(1024, "Lg")]
    [InlineData(2000, "Lg")]
    [InlineData(-1, "Lg")]
    [InlineData(0, "Lg")]
    [InlineData(double.NaN, "Lg")]
    public void The_width_classes_are_the_breakpoints_of_the_web_theme_and_an_unknown_width_is_the_widest(double width, string expected) =>
        Assert.Equal(Enum.Parse<DashboardBreakpoint>(expected), DashboardBreakpoints.Of(width));

    [Theory]
    [InlineData(400, 1, 1, 16)]
    [InlineData(700, 2, 2, 16)]
    [InlineData(900, 2, 2, 16)]
    [InlineData(1100, 4, 2, 28)]
    [InlineData(-1, 4, 2, 28)]
    public void Stats_details_and_the_page_padding_follow_the_width(double width, int statsColumns, int detailsColumns, double padding)
    {
        Assert.Equal(statsColumns, DashboardBreakpoints.StatsColumns(width));
        Assert.Equal(detailsColumns, DashboardBreakpoints.DetailsColumns(width));
        Assert.Equal(padding, DashboardBreakpoints.PagePadding(width));
    }

    [Fact]
    public void The_viewport_is_the_width_of_the_page_the_view_is_on_else_its_own()
    {
        var card = new KlyDashboardCard();
        Assert.Equal(-1, DashboardBreakpoints.ViewportWidth(card));

        var page = new ContentPage { Content = card };
        // A page that has not been laid out has no width yet.
        Assert.Equal(-1, DashboardBreakpoints.ViewportWidth(card));
        ((IView)page).Arrange(new Rect(0, 0, 500, 800));
        Assert.Equal(500, DashboardBreakpoints.ViewportWidth(card));
    }

    // --- grid ------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void The_grid_has_twelve_columns_and_cards_spacing_by_default()
    {
        var grid = new KlyDashboardGrid();

        Assert.Equal(12, grid.Columns);
        Assert.Equal(12, grid.EffectiveColumns);
        Assert.Equal(DashboardGridVariant.Cards, grid.Variant);
        Assert.False(grid.Menu);
        Assert.False(grid.Freshness);
        Assert.Equal(16, grid.Inner.RowSpacing);
        Assert.Equal(16, grid.Inner.ColumnSpacing);
        Assert.Equal(0, grid.StrokeThickness);
    }

    [Fact]
    public void Blocks_are_placed_left_to_right_and_a_block_that_does_not_fit_starts_the_next_row()
    {
        var grid = new KlyDashboardGrid();
        var a = Block(4);
        var b = Block(4);
        var c = Block(6);
        var d = Block(12);
        foreach (var block in new[] { a, b, c, d }) grid.Children.Add(block);

        Assert.Equal((0, 0, 4), At(a));
        Assert.Equal((0, 4, 4), At(b));
        Assert.Equal((1, 0, 6), At(c));
        Assert.Equal((2, 0, 12), At(d));
        Assert.Equal(12, grid.Inner.ColumnDefinitions.Count);
        Assert.Equal(3, grid.Inner.RowDefinitions.Count);
    }

    [Fact]
    public void Nothing_moves_back_to_fill_a_gap_like_a_css_grid_with_sparse_flow()
    {
        var grid = new KlyDashboardGrid();
        var a = Block(8);
        var b = Block(8);
        var c = Block(4);
        foreach (var block in new[] { a, b, c }) grid.Children.Add(block);

        Assert.Equal((0, 0, 8), At(a));
        Assert.Equal((1, 0, 8), At(b));
        Assert.Equal((1, 8, 4), At(c));
    }

    [Fact]
    public void A_span_is_clamped_between_one_and_the_columns_of_the_grid()
    {
        var grid = new KlyDashboardGrid { Columns = 3 };
        var full = Block(12);
        var third = Block(1);
        var zero = Block(0);
        var negative = Block(-4);
        foreach (var block in new[] { full, third, zero, negative }) grid.Children.Add(block);

        Assert.Equal((0, 0, 3), At(full));
        Assert.Equal((1, 0, 1), At(third));
        Assert.Equal((1, 1, 1), At(zero));
        Assert.Equal((1, 2, 1), At(negative));
        Assert.Equal(3, grid.Inner.ColumnDefinitions.Count);
    }

    [Fact]
    public void A_grid_of_less_than_one_column_has_one()
    {
        var grid = new KlyDashboardGrid { Columns = 0 };
        var a = Block(5);
        grid.Children.Add(a);

        Assert.Equal(1, grid.EffectiveColumns);
        Assert.Equal((0, 0, 1), At(a));
    }

    [Fact]
    public void Below_640_units_every_block_takes_the_whole_row()
    {
        var grid = new KlyDashboardGrid();
        var a = Block(4);
        var b = Block(4);
        var c = Block(4);
        foreach (var block in new[] { a, b, c }) grid.Children.Add(block);

        grid.Place(500);
        Assert.Equal((0, 0, 12), At(a));
        Assert.Equal((1, 0, 12), At(b));
        Assert.Equal((2, 0, 12), At(c));
        Assert.Equal(500, grid.LastWidth);

        grid.Place(640);
        Assert.Equal((0, 0, 4), At(a));
        Assert.Equal((0, 4, 4), At(b));
        Assert.Equal((0, 8, 4), At(c));

        grid.Place(639);
        Assert.Equal((2, 0, 12), At(c));
    }

    [Fact]
    public void The_grid_lays_out_for_the_width_of_the_page_it_is_on()
    {
        var grid = new KlyDashboardGrid();
        var a = Block(6);
        var b = Block(6);
        grid.Children.Add(a);
        grid.Children.Add(b);
        var page = new ContentPage { Content = grid };

        ((IView)page).Arrange(new Rect(0, 0, 400, 800));
        grid.Place();
        Assert.Equal((1, 0, 12), At(b));

        ((IView)page).Arrange(new Rect(0, 0, 1200, 800));
        grid.Place();
        Assert.Equal((0, 6, 6), At(b));
    }

    [Fact]
    public void A_child_that_is_not_a_block_takes_a_full_row()
    {
        var grid = new KlyDashboardGrid();
        var label = new Label { Text = "Note" };
        var block = Block(6);
        grid.Children.Add(block);
        grid.Children.Add(label);

        Assert.Equal((0, 0, 6), At(block));
        Assert.Equal((1, 0, 12), At(label));
    }

    [Fact]
    public void A_hidden_block_leaves_no_gap_and_comes_back_in_its_place()
    {
        var enabled = false;
        var gated = new DashboardResource<int>("gated", _ => Task.FromResult(1), enabled: () => enabled);
        var grid = new KlyDashboardGrid();
        var a = Block(6);
        var hidden = new KlyDashboardCard { Span = 6, Source = gated };
        var c = Block(6);
        foreach (var block in new[] { a, hidden, c }) grid.Children.Add(block);

        Assert.False(hidden.IsVisible);
        Assert.Equal((0, 0, 6), At(a));
        Assert.Equal((0, 6, 6), At(c));

        enabled = true;
        hidden.Refresh();

        Assert.True(hidden.IsVisible);
        Assert.Equal((0, 6, 6), At(hidden));
        Assert.Equal((1, 0, 6), At(c));
    }

    [Fact]
    public void An_empty_grid_draws_nothing()
    {
        var grid = new KlyDashboardGrid { Variant = DashboardGridVariant.Panels };
        grid.Place(1000);
        Assert.False(grid.IsVisible);

        var card = Block(12);
        grid.Children.Add(card);
        Assert.True(grid.IsVisible);

        grid.Children.Remove(card);
        Assert.False(grid.IsVisible);
        Assert.Empty(grid.Inner.RowDefinitions);
    }

    [Fact]
    public void Panels_join_the_cards_with_a_one_unit_divider_inside_a_border()
    {
        var grid = new KlyDashboardGrid { Variant = DashboardGridVariant.Panels };

        Assert.Equal(1, grid.Inner.RowSpacing);
        Assert.Equal(1, grid.Inner.ColumnSpacing);
        Assert.Equal(1, grid.StrokeThickness);
        Assert.Equal(KlyTheme.Radius * 0.75, ((Microsoft.Maui.Controls.Shapes.RoundRectangle)grid.StrokeShape!).CornerRadius.TopLeft);

        grid.Variant = DashboardGridVariant.Cards;
        Assert.Equal(16, grid.Inner.RowSpacing);
        Assert.Equal(0, grid.StrokeThickness);
    }

    // --- texts -----------------------------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("en", "en")]
    [InlineData("en-US", "en")]
    [InlineData("fr", "fr")]
    [InlineData("fr-CA", "fr")]
    [InlineData("pt-BR", "pt-BR")]
    [InlineData("pt-PT", "pt")]
    [InlineData("zh-CN", "zh-Hans")]
    [InlineData("zh-Hans", "zh-Hans")]
    [InlineData("sv", "en")]
    [InlineData("", "en")]
    public void A_culture_gets_the_set_of_its_code_then_of_its_parents_then_english(string name, string code) =>
        Assert.Equal(code, KlyDashboardTexts.CodeOf(CultureInfo.GetCultureInfo(name)));

    [Fact]
    public void The_texts_are_the_ones_of_the_ui_culture_and_the_sets_are_the_blazor_ones()
    {
        Assert.Equal("Refresh", KlyDashboardTexts.Resolve(CultureInfo.GetCultureInfo("en-GB")).Refresh);
        Assert.Equal(DashboardLocales.Fr.Refresh, KlyDashboardTexts.Resolve(CultureInfo.GetCultureInfo("fr-FR")).Refresh);

        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de");
            Assert.Equal(DashboardLocales.De.Refresh, KlyDashboardTexts.Resolve().Refresh);
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    [Fact]
    public void A_set_can_be_changed_by_the_code_of_its_locale_and_the_change_applies_to_the_cultures_that_resolve_to_it()
    {
        KlyDashboardTexts.Configure("sw", t => t with { Refresh = "Onyesha upya" });

        var swahili = CultureInfo.GetCultureInfo("sw");
        Assert.Equal("Onyesha upya", KlyDashboardTexts.Resolve(swahili).Refresh);
        // A culture the change does not name keeps the set it had.
        Assert.Equal("Refresh", KlyDashboardTexts.Resolve(CultureInfo.GetCultureInfo("en")).Refresh);

        KlyDashboardTexts.Configure("it", t => t with { Refresh = "Ricarica" });
        Assert.Equal("Ricarica", KlyDashboardTexts.Resolve(CultureInfo.GetCultureInfo("it-CH")).Refresh);
        KlyDashboardTexts.Configure("it", t => t with { Refresh = DashboardLocales.It.Refresh });
        Assert.Equal(DashboardLocales.It.Refresh, KlyDashboardTexts.Resolve(CultureInfo.GetCultureInfo("it")).Refresh);
    }

    [Fact]
    public void Changing_the_texts_needs_a_code_and_a_change()
    {
        Assert.Throws<ArgumentException>(() => KlyDashboardTexts.Configure(" ", t => t));
        Assert.Throws<ArgumentNullException>(() => KlyDashboardTexts.Configure("fr", null!));
        Assert.Throws<ArgumentNullException>(() => KlyDashboardTexts.Configure(null!, t => t));
    }

    [Fact]
    public void A_card_speaks_the_language_of_the_app()
    {
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr");
            var card = new KlyDashboardCard { IsEmpty = true };

            Assert.Equal([DashboardLocales.Fr.NoData], Texts(PhaseOf(card)));
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    // --- small drawings --------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void The_sparkline_spans_the_width_and_keeps_its_margins()
    {
        var points = DashboardSparkline.Project([10, 20, 15], 100, 30);

        Assert.Equal([0f, 50f, 100f], points.Select(p => p.X));
        // The lowest point is 2 units above the bottom, the highest 4 below the top.
        Assert.Equal(28f, points[0].Y, 3);
        Assert.Equal(4f, points[1].Y, 3);
        Assert.Equal(16f, points[2].Y, 3);
        Assert.Empty(DashboardSparkline.Project([1], 100, 30));
    }

    [Fact]
    public void A_flat_sparkline_is_a_straight_line_not_a_division_by_zero()
    {
        var points = DashboardSparkline.Project([5, 5, 5], 100, 30);

        Assert.All(points, p => Assert.Equal(28f, p.Y, 3));
    }

    [Fact]
    public void The_ring_arc_starts_at_the_top_and_goes_clockwise()
    {
        var quarter = DashboardGaugeRing.ArcPath(50, 50, 40, 0.25);

        Assert.Equal(50f, quarter.FirstPoint.X, 3);
        Assert.Equal(10f, quarter.FirstPoint.Y, 3);
        Assert.Equal(90f, quarter.LastPoint.X, 3);
        Assert.Equal(50f, quarter.LastPoint.Y, 3);

        var half = DashboardGaugeRing.ArcPath(50, 50, 40, 0.5);
        Assert.Equal(50f, half.LastPoint.X, 3);
        Assert.Equal(90f, half.LastPoint.Y, 3);
    }

    [Fact]
    public void A_share_of_the_ring_is_clamped()
    {
        var ring = new DashboardGaugeRing { Share = 7 };
        Assert.Equal(1, ring.Share);
        ring.Share = -3;
        Assert.Equal(0, ring.Share);
    }

    [Fact]
    public void The_bar_of_a_value_in_a_category_is_centered_in_its_column_and_never_wider_than_48()
    {
        float Y(double v) => (float)(100 - v);

        var rect = DashboardCompactChart.BarRect(40, category: 1, count: 4, barIndex: 0, barCount: 1, width: 400, Y, zeroY: 100)!.Value;
        Assert.Equal(100 + (100 - 48) / 2f, rect.X);
        Assert.Equal(48, rect.Width);
        Assert.Equal(60, rect.Y);
        Assert.Equal(40, rect.Height);

        var negative = DashboardCompactChart.BarRect(-20, 0, 4, 0, 1, 400, Y, 100)!.Value;
        Assert.Equal(100, negative.Y);
        Assert.Equal(20, negative.Height);

        Assert.Null(DashboardCompactChart.BarRect(null, 0, 4, 0, 1, 400, Y, 100));
    }

    [Fact]
    public void Two_bars_of_a_category_sit_side_by_side_with_a_gap()
    {
        float Y(double v) => (float)(100 - v);

        var first = DashboardCompactChart.BarRect(10, 0, 2, 0, 2, 200, Y, 100)!.Value;
        var second = DashboardCompactChart.BarRect(10, 0, 2, 1, 2, 200, Y, 100)!.Value;

        Assert.Equal(first.Right + 2, second.Left);
        Assert.Equal(first.Width, second.Width);
    }

    [Fact]
    public void Chart_colors_follow_the_tokens_and_fall_back_to_the_semantic_colors()
    {
        Assert.Equal(["chart-1", "chart-2", "chart-3", "chart-4", "chart-5", "chart-6", "neutral"], DashboardChartColors.Palette);
        Assert.Equal("chart-1", DashboardChartColors.ColorOf(7).Value);
        Assert.Equal("neutral", DashboardChartColors.ColorOf(6).Value);
        Assert.Equal(KlyTheme.Resolve(KlyColor.Primary), DashboardChartColors.Resolve("chart-1"));
        Assert.Equal(KlyTheme.Resolve(KlyColor.Warning), DashboardChartColors.Resolve("chart-6"));
        Assert.Throws<InvalidOperationException>(() => DashboardChartColors.Resolve("not-registered-anywhere"));
    }

    [Fact]
    public void Tones_of_a_change_are_muted_for_zero_and_success_or_error_otherwise()
    {
        Assert.Equal(KlyTheme.TextMuted, DashboardParts.DeltaText(0, true));
        Assert.Equal(KlyTheme.TextMuted, DashboardParts.DeltaText(0.00001, false));
        Assert.Equal(DashboardParts.TextOf(KlyColor.Success), DashboardParts.DeltaText(5, true));
        Assert.Equal(DashboardParts.TextOf(KlyColor.Error), DashboardParts.DeltaText(-5, false));
        Assert.Equal(KlyTheme.BackgroundMuted, DashboardParts.DeltaTone(0, true).Background);
        Assert.Equal(DashboardParts.FillOf(KlyColor.Success).WithAlpha(0.15f), DashboardParts.DeltaTone(2, true).Background);
        Assert.Equal(DashboardParts.TextOf(KlyColor.Error), DashboardParts.DeltaTone(2, false).Text);
    }

    [Fact]
    public void A_progress_bar_has_a_filled_share_of_its_track()
    {
        var bar = Assert.IsType<Border>(DashboardParts.ProgressBar(25, 6, KlyTheme.Border));
        var grid = Assert.IsType<Grid>(bar.Content);

        Assert.Equal(6, bar.HeightRequest);
        Assert.Equal(25, grid.ColumnDefinitions[0].Width.Value);
        Assert.Equal(75, grid.ColumnDefinitions[1].Width.Value);
        Assert.True(((View)grid.Children[0]).IsVisible);

        var empty = Assert.IsType<Grid>(Assert.IsType<Border>(DashboardParts.ProgressBar(0, 6, KlyTheme.Border)).Content);
        Assert.False(((View)empty.Children[0]).IsVisible);
        Assert.Equal("43%", DashboardParts.PercentText(42.6));
    }

    [Fact]
    public void An_address_is_opened_only_when_a_device_can_open_it()
    {
        Assert.True(DashboardLaunch.TryParse("https://klyroo.dev/docs", out var uri));
        Assert.Equal("klyroo.dev", uri!.Host);
        Assert.True(DashboardLaunch.TryParse("mailto:ada@example.com", out _));
        Assert.False(DashboardLaunch.TryParse("/docs", out _));
        Assert.False(DashboardLaunch.TryParse(null, out _));
        Assert.False(DashboardLaunch.TryParse("", out _));

        var opened = new List<Uri>();
        DashboardLaunch.Open("https://klyroo.dev", u =>
        {
            opened.Add(u);
            return Task.CompletedTask;
        });
        DashboardLaunch.Open("/relative", u =>
        {
            opened.Add(u);
            return Task.CompletedTask;
        });
        Assert.Equal(["https://klyroo.dev/"], opened.Select(u => u.AbsoluteUri));
    }

    [Fact]
    public void A_failing_opener_or_clipboard_is_swallowed()
    {
        DashboardLaunch.Open("https://klyroo.dev", _ => throw new InvalidOperationException("no browser"));
        DashboardLaunch.Copy("text", _ => throw new InvalidOperationException("no clipboard"));
        DashboardLaunch.Copy("", _ => throw new InvalidOperationException("never called"));

        var copied = new List<string>();
        DashboardLaunch.Copy("hello", t =>
        {
            copied.Add(t);
            return Task.CompletedTask;
        });
        Assert.Equal(["hello"], copied);
    }
}
