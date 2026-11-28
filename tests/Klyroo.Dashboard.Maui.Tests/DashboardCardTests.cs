using static Klyroo.Dashboard.Maui.Tests.TestHelpers;

namespace Klyroo.Dashboard.Maui.Tests;

public class DashboardCardTests
{
    private static DashboardResource<int> Ready()
    {
        var resource = new DashboardResource<int>("ready");
        resource.SetData(1);
        return resource;
    }

    private static DashboardDataTable Table() => new(
        [new DashboardDataColumn("name", "Name"), new DashboardDataColumn("total", "Total", Numeric: true)],
        [new Dictionary<string, string?> { ["name"] = "Ada", ["total"] = "1200" }, new Dictionary<string, string?> { ["name"] = "Grace", ["total"] = null }]);

    private static void Click(Button button) => button.SendClicked();

    // --- defaults and phases ---------------------------------------------------------------------------------------------------------------

    [Fact]
    public void A_card_has_the_defaults_of_the_blazor_card()
    {
        var card = new KlyDashboardCard();

        Assert.Equal(12, card.Span);
        Assert.True(card.Card);
        Assert.True(card.ViewAsTable);
        Assert.True(card.Expandable);
        Assert.Null(card.Menu);
        Assert.Null(card.Freshness);
        Assert.False(card.IsEmpty);
        Assert.Equal(DashboardSkeletonKind.Rows, card.Skeleton);
        Assert.Equal(DashboardBlockPhase.Content, card.Phase);
        Assert.False(card.EffectiveMenu);
        Assert.False(card.EffectiveFreshness);
    }

    [Fact]
    public void Without_a_source_the_card_shows_its_body_or_the_empty_state()
    {
        var body = new Label { Text = "Hello" };
        var card = new KlyDashboardCard { Body = body };

        Assert.Same(body, PhaseOf(card));
        Assert.Equal(DashboardBlockPhase.Content, card.Phase);

        card.IsEmpty = true;
        Assert.Equal(DashboardBlockPhase.Empty, card.Phase);
        Assert.Contains("No data for this period", Texts(card));
        Assert.DoesNotContain("Hello", Texts(card));
    }

    [Fact]
    public void The_empty_state_takes_its_own_title_and_description()
    {
        var card = new KlyDashboardCard { IsEmpty = true, EmptyTitle = "Nothing yet", EmptyDescription = "Come back later" };

        Assert.Equal(["Nothing yet", "Come back later"], Texts(PhaseOf(card)));
    }

    [Fact]
    public void The_phase_follows_the_state_of_the_source()
    {
        var resource = new DashboardResource<int>("r");
        var card = new KlyDashboardCard { Source = resource };
        Assert.Equal(DashboardBlockPhase.Idle, card.Phase);

        resource.SetError("boom");
        Assert.Equal(DashboardBlockPhase.Error, card.Phase);

        resource.SetData(3);
        Assert.Equal(DashboardBlockPhase.Content, card.Phase);

        card.IsEmpty = true;
        Assert.Equal(DashboardBlockPhase.Empty, card.Phase);
    }

    [Fact]
    public async Task A_loading_source_shows_the_skeleton_and_the_thin_progress_bar()
    {
        var gate = new TaskCompletionSource<int>();
        var resource = new DashboardResource<int>("slow", _ => gate.Task);
        var card = new KlyDashboardCard { Body = new Label { Text = "Done" } };
        var load = resource.RefreshAsync();
        card.Source = resource;

        Assert.Equal(DashboardBlockPhase.Loading, card.Phase);
        Assert.True(card.Progress.IsVisible);
        Assert.NotEmpty(All<KlySkeleton>(PhaseOf(card)));
        Assert.DoesNotContain("Done", Texts(card));

        gate.SetResult(5);
        await load;
        card.Refresh();
        Assert.False(card.Progress.IsVisible);
        Assert.Equal("Done", Texts(card).Single());
    }

    [Fact]
    public void An_error_shows_the_message_and_a_retry_button_that_reloads_the_source()
    {
        var calls = 0;
        var resource = new DashboardResource<int>("flaky", _ =>
        {
            calls++;
            return Task.FromResult(7);
        });
        resource.SetError("The server said no");
        var card = new KlyDashboardCard { Source = resource, Body = new Label { Text = "Done" } };

        Assert.Equal(["This data could not be loaded", "The server said no"], Texts(PhaseOf(card)));

        var retry = All<KlyButton>(PhaseOf(card)).Single();
        Assert.Equal("Retry", retry.Text);
        Click(retry);
        Assert.Equal(1, calls);
        Assert.Equal(DashboardResourceState.Ready, resource.State);
    }

    [Fact]
    public void An_error_without_a_message_shows_the_default_one_and_a_source_of_another_kind_works()
    {
        var card = new KlyDashboardCard { Source = new FakeSource { State = DashboardResourceState.Error } };

        Assert.Equal([KlyDashboardTexts.Resolve().ErrorTitle, KlyDashboardTexts.Resolve().ErrorMessage], Texts(PhaseOf(card)));
    }

    private sealed class FakeSource : IDashboardSource
    {
        public string Key => "fake";
        public DashboardQueryStage Stage => DashboardQueryStage.Essential;
        public DashboardResourceState State { get; init; }
        public string? Error => null;
        public bool Refreshing => false;
        public bool Fetching => false;
        public DateTimeOffset? UpdatedAt => null;
        public bool Active => true;
        public Task ActivateAsync() => Task.CompletedTask;
        public Task RefreshAsync() => Task.CompletedTask;
    }

    [Fact]
    public void A_disabled_source_that_has_not_loaded_draws_nothing_until_it_is_enabled()
    {
        var enabled = false;
        var resource = new DashboardResource<int>("gated", _ => Task.FromResult(9), enabled: () => enabled);
        var card = new KlyDashboardCard { Source = resource, Body = new Label { Text = "Shown" } };

        Assert.False(card.IsVisible);
        Assert.True(card.HiddenBySource);

        enabled = true;
        card.Refresh();

        Assert.Equal(DashboardResourceState.Ready, resource.State);
        Assert.True(card.IsVisible);
        Assert.False(card.HiddenBySource);
        Assert.Equal("Shown", Texts(card).Single());
    }

    [Fact]
    public void A_deferred_source_loads_on_the_first_refresh_of_its_card()
    {
        var loads = 0;
        var resource = new DashboardResource<int>("later", _ =>
        {
            loads++;
            return Task.FromResult(1);
        }, DashboardQueryStage.Deferred);
        Assert.False(resource.Active);

        var card = new KlyDashboardCard { Source = resource };

        Assert.True(resource.Active);
        Assert.Equal(1, loads);
        card.Refresh();
        Assert.Equal(1, loads);
    }

    // --- header, menu, footer --------------------------------------------------------------------------------------------------------------

    [Fact]
    public void The_header_shows_the_title_as_a_heading_and_the_subtitle()
    {
        var card = new KlyDashboardCard { Title = "Revenue", Subtitle = "Last 30 days" };

        Assert.True(card.Header.IsVisible);
        Assert.Equal("Revenue", card.TitleLabel.Text);
        Assert.Equal(SemanticHeadingLevel.Level3, SemanticProperties.GetHeadingLevel(card.TitleLabel));
        Assert.Contains("Last 30 days", Texts(card));
    }

    [Fact]
    public void A_card_with_nothing_for_a_header_has_none()
    {
        var card = new KlyDashboardCard();
        Assert.False(card.Header.IsVisible);

        card.HeaderRight = new Label { Text = "Right" };
        Assert.True(card.Header.IsVisible);
        Assert.Contains(card.HeaderRight, card.Tools.Children);
    }

    [Fact]
    public void The_menu_buttons_need_the_menu_a_table_and_the_content_phase()
    {
        var card = new KlyDashboardCard { Title = "T", Tabulate = Table, Expandable = false };
        Assert.Empty(card.Tools.Children);

        card.Menu = true;
        var (table, csv, _) = card.MenuButtons;
        Assert.Equal(["View as table", "Download CSV"], card.Tools.Children.OfType<Button>().Select(b => b.Text));
        Assert.Contains(table, card.Tools.Children);
        Assert.Contains(csv, card.Tools.Children);

        card.ViewAsTable = false;
        Assert.Equal(["Download CSV"], card.Tools.Children.OfType<Button>().Select(b => b.Text));

        card.Tabulate = null;
        Assert.Empty(card.Tools.Children);
    }

    [Fact]
    public void No_menu_buttons_while_the_card_is_not_showing_its_content()
    {
        var resource = new DashboardResource<int>("r");
        var card = new KlyDashboardCard { Source = resource, Menu = true, Tabulate = Table, Expandable = false };

        Assert.Empty(card.Tools.Children);
        resource.SetData(1);
        card.Refresh();
        Assert.NotEmpty(card.Tools.Children);
    }

    [Fact]
    public void The_table_button_swaps_the_block_for_its_data_and_back()
    {
        var body = new Label { Text = "Chart" };
        var card = new KlyDashboardCard { Title = "T", Menu = true, Tabulate = Table, Body = body, Expandable = false };
        Assert.Same(body, PhaseOf(card));

        Click(card.MenuButtons.Table);
        Assert.True(card.ShowingTable);
        Assert.Equal("Hide table", card.MenuButtons.Table.Text);
        var table = PhaseOf(card);
        Assert.Equal(["Name", "Total", "Ada", "1200", "Grace", "—"], Texts(table));

        Click(card.MenuButtons.Table);
        Assert.False(card.ShowingTable);
        Assert.Same(body, PhaseOf(card));
        Assert.Equal("View as table", card.MenuButtons.Table.Text);
    }

    [Fact]
    public void The_table_view_right_aligns_numbers()
    {
        var card = new KlyDashboardCard { Menu = true, Tabulate = Table, Expandable = false };
        Click(card.MenuButtons.Table);

        var labels = All<Label>(PhaseOf(card));
        Assert.Equal(TextAlignment.Start, labels.First(l => l.Text == "Name").HorizontalTextAlignment);
        Assert.Equal(TextAlignment.End, labels.First(l => l.Text == "Total").HorizontalTextAlignment);
        Assert.Equal(TextAlignment.End, labels.First(l => l.Text == "1200").HorizontalTextAlignment);
    }

    [Fact]
    public async Task Download_csv_raises_the_event_with_the_csv_of_the_table()
    {
        var card = new KlyDashboardCard { Menu = true, Tabulate = Table, Expandable = false, Title = "Sales" };
        var received = new List<string>();
        card.CsvExported += (_, csv) => received.Add(csv);

        await card.ExportCsvAsync();

        Assert.Equal([DashboardFormat.ExportCsv(Table(), System.Globalization.CultureInfo.CurrentCulture)], received);
    }

    [Fact]
    public async Task Download_csv_without_a_listener_does_not_fail_where_there_is_no_share_sheet()
    {
        var card = new KlyDashboardCard { Menu = true, Tabulate = Table, Expandable = false, Title = "Sales" };

        await card.ExportCsvAsync();
        await new KlyDashboardCard().ExportCsvAsync();
    }

    [Fact]
    public void The_menu_default_comes_from_the_grid_and_the_card_can_override_it()
    {
        var grid = new KlyDashboardGrid { Menu = true, Freshness = true };
        var inherits = new KlyDashboardCard { Tabulate = Table, Expandable = false };
        var overrides = new KlyDashboardCard { Tabulate = Table, Expandable = false, Menu = false, Freshness = false };
        grid.Children.Add(inherits);
        grid.Children.Add(overrides);

        Assert.True(inherits.EffectiveMenu);
        Assert.True(inherits.EffectiveFreshness);
        Assert.NotEmpty(inherits.Tools.Children);
        Assert.False(overrides.EffectiveMenu);
        Assert.False(overrides.EffectiveFreshness);
        Assert.Empty(overrides.Tools.Children);

        grid.Menu = false;
        Assert.Empty(inherits.Tools.Children);
    }

    [Fact]
    public void Freshness_shows_when_the_source_loaded_and_the_card_asks_for_it()
    {
        var resource = Ready();
        var card = new KlyDashboardCard { Source = resource, Freshness = true };

        Assert.True(card.FooterBox.IsVisible);
        Assert.True(card.FreshnessLabel.IsVisible);
        Assert.Equal("Updated just now", card.FreshnessLabel.Text);

        card.Freshness = false;
        Assert.False(card.FooterBox.IsVisible);
    }

    [Fact]
    public void The_footer_view_is_shown_with_or_without_freshness()
    {
        var footer = new Label { Text = "Source: CRM" };
        var card = new KlyDashboardCard { Footer = footer };

        Assert.True(card.FooterBox.IsVisible);
        Assert.False(card.FreshnessLabel.IsVisible);
        Assert.Contains("Source: CRM", Texts(card));
    }

    [Fact]
    public void Active_filters_show_as_chips_that_remove_them()
    {
        var region = new DashboardFilterControl("region", "Region", items: [new DashboardOption("emea", "EMEA"), new DashboardOption("us", "US")]);
        var untouched = new DashboardFilterControl("segment", "Segment");
        var card = new KlyDashboardCard { Filters = [region, untouched] };
        Assert.False(card.Chips.IsVisible);

        region.Value = "emea";
        card.Refresh();

        Assert.True(card.Chips.IsVisible);
        Assert.Equal(["Region: EMEA"], Texts(card.Chips));
        var remove = All<DashboardTapArea>(card.Chips).Single();
        Assert.Equal("Remove Region", remove.Description);

        remove.SendTap();
        Assert.False(region.Changed);
        card.Refresh();
        Assert.False(card.Chips.IsVisible);
    }

    // --- surface ---------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void A_card_has_a_border_and_the_medium_radius_and_can_go_without_a_surface()
    {
        var card = new KlyDashboardCard();
        Assert.Equal(1, card.StrokeThickness);
        Assert.Equal(KlyTheme.Radius * 0.75, ((Microsoft.Maui.Controls.Shapes.RoundRectangle)card.StrokeShape!).CornerRadius.TopLeft);

        card.Card = false;
        Assert.Equal(0, card.StrokeThickness);
    }

    [Fact]
    public void In_a_panels_grid_a_card_keeps_its_background_only()
    {
        var grid = new KlyDashboardGrid { Variant = DashboardGridVariant.Panels };
        var card = new KlyDashboardCard();
        grid.Children.Add(card);

        Assert.Equal(0, card.StrokeThickness);
        Assert.Equal(0, ((Microsoft.Maui.Controls.Shapes.RoundRectangle)card.StrokeShape!).CornerRadius.TopLeft);

        grid.Variant = DashboardGridVariant.Cards;
        Assert.Equal(1, card.StrokeThickness);
    }

    // --- skeletons -------------------------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(DashboardSkeletonKind.Stat, 2)]
    [InlineData(DashboardSkeletonKind.Bars, 4)]
    [InlineData(DashboardSkeletonKind.Lines, 4)]
    [InlineData(DashboardSkeletonKind.Paired, 6)]
    [InlineData(DashboardSkeletonKind.Stack, 9)]
    [InlineData(DashboardSkeletonKind.Rows, 3)]
    [InlineData(DashboardSkeletonKind.Stats, 3)]
    [InlineData(DashboardSkeletonKind.Gauge, 3)]
    [InlineData(DashboardSkeletonKind.Details, 3)]
    [InlineData(DashboardSkeletonKind.Funnel, 3)]
    [InlineData(DashboardSkeletonKind.Table, 3)]
    [InlineData(DashboardSkeletonKind.Feed, 3)]
    [InlineData(DashboardSkeletonKind.Donut, 3)]
    public void Each_skeleton_kind_has_its_shape_and_is_hidden_from_assistive_technology(DashboardSkeletonKind kind, int skeletons)
    {
        var view = DashboardSkeletons.Build(kind);

        Assert.Equal(skeletons, All<KlySkeleton>(view).Count);
        Assert.False(AutomationProperties.GetIsInAccessibleTree(view));
    }

    [Fact]
    public void Bar_skeletons_stand_on_a_common_baseline_with_their_own_heights()
    {
        var bars = All<KlySkeleton>(DashboardSkeletons.Build(DashboardSkeletonKind.Bars));

        Assert.Equal([64.0, 96.0, 128.0 * 2 / 3, 128.0], bars.Select(b => b.HeightRequest));
        Assert.All(bars, b => Assert.Equal(LayoutOptions.End, b.VerticalOptions));
    }

    // --- expand ----------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Expand_is_offered_only_inside_an_overlay_host()
    {
        var card = new KlyDashboardCard { Title = "T", Menu = true, Tabulate = Table };
        Assert.DoesNotContain(card.MenuButtons.Expand, card.Tools.Children);

        var host = new KlyOverlayHost { Content = card };
        card.Refresh();

        Assert.Contains(card.MenuButtons.Expand, card.Tools.Children);
        Assert.Equal("Expand", card.MenuButtons.Expand.Text);
        Assert.NotNull(host);
    }

    [Fact]
    public void Expand_moves_the_content_into_a_dialog_and_closing_it_brings_it_back()
    {
        var body = new Label { Text = "Chart" };
        var card = new KlyDashboardCard { Title = "Revenue", Subtitle = "30 days", Menu = true, Body = body };
        var host = new KlyOverlayHost { Content = card };
        card.Refresh();

        Click(card.MenuButtons.Expand);

        Assert.True(card.Expanded);
        Assert.True(card.Modal.IsOpen);
        Assert.Equal("Revenue", card.Modal.Title);
        Assert.Equal("30 days", card.Modal.Description);
        Assert.Equal(KlySize.Xl, card.Modal.Size);
        Assert.Same(body, card.Modal.Body);
        Assert.NotSame(body, card.PhaseHost.Content);
        Assert.Equal("Collapse", card.MenuButtons.Expand.Text);
        Assert.Single(host.Layers);

        card.Modal.IsOpen = false;

        Assert.False(card.Expanded);
        Assert.Same(body, card.PhaseHost.Content);
        Assert.Null(card.Modal.Body);
        Assert.Equal("Expand", card.MenuButtons.Expand.Text);
    }

    [Fact]
    public void The_collapse_button_closes_the_dialog()
    {
        var card = new KlyDashboardCard { Menu = true, Body = new Label { Text = "x" } };
        _ = new KlyOverlayHost { Content = card };
        card.Refresh();

        Click(card.MenuButtons.Expand);
        Click(card.MenuButtons.Expand);

        Assert.False(card.Expanded);
        Assert.False(card.Modal.IsOpen);
    }
}
