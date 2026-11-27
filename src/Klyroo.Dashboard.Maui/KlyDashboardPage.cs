namespace Klyroo.Dashboard.Maui;

/// <summary>
/// The shell of a dashboard: a header with the title and a refresh button, a toolbar with the view tabs and the filter controls of a <see cref="DashboardController"/> (option lists, text, number and date boxes,
/// or your own view, depending on the kind of the filter), and a body for the blocks. It follows the controller: when it changes the page builds its toolbar again and refreshes the blocks in its <see cref="Body"/>.
/// </summary>
/// <remarks>
/// The page scrolls its body itself (<see cref="Scrollable"/>); with <see cref="Sticky"/> the header and the toolbar stay at the top while it scrolls. The filter lists open under the filters,
/// in the toolbar. The page only draws the tabs: show the blocks of the current view yourself, from <see cref="DashboardController.CurrentView"/>.
/// </remarks>
[ContentProperty(nameof(Body))]
public class KlyDashboardPage : ContentView
{
    /// <summary>Identifies <see cref="Dashboard"/>.</summary>
    public static readonly BindableProperty DashboardProperty = BindableProperty.Create(nameof(Dashboard), typeof(DashboardController), typeof(KlyDashboardPage), null, propertyChanged: (b, o, n) => ((KlyDashboardPage)b).OnDashboardChanged((DashboardController?)o, (DashboardController?)n));
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = Prop<string?>(nameof(Title), null);
    /// <summary>Identifies <see cref="Description"/>.</summary>
    public static readonly BindableProperty DescriptionProperty = Prop<string?>(nameof(Description), null);
    /// <summary>Identifies <see cref="ShowRefresh"/>.</summary>
    public static readonly BindableProperty ShowRefreshProperty = Prop(nameof(ShowRefresh), true);
    /// <summary>Identifies <see cref="ShowTabs"/>.</summary>
    public static readonly BindableProperty ShowTabsProperty = Prop(nameof(ShowTabs), true);
    /// <summary>Identifies <see cref="ShowFilters"/>.</summary>
    public static readonly BindableProperty ShowFiltersProperty = Prop(nameof(ShowFilters), true);
    /// <summary>Identifies <see cref="CustomFilter"/>.</summary>
    public static readonly BindableProperty CustomFilterProperty = BindableProperty.Create(nameof(CustomFilter), typeof(Func<DashboardFilterControl, View>), typeof(KlyDashboardPage), null, propertyChanged: (b, _, _) => ((KlyDashboardPage)b).OnCustomFilterChanged());
    /// <summary>Identifies <see cref="Sticky"/>.</summary>
    public static readonly BindableProperty StickyProperty = BindableProperty.Create(nameof(Sticky), typeof(bool), typeof(KlyDashboardPage), true, propertyChanged: (b, _, _) => ((KlyDashboardPage)b).Compose());
    /// <summary>Identifies <see cref="Scrollable"/>.</summary>
    public static readonly BindableProperty ScrollableProperty = BindableProperty.Create(nameof(Scrollable), typeof(bool), typeof(KlyDashboardPage), true, propertyChanged: (b, _, _) => ((KlyDashboardPage)b).Compose());
    /// <summary>Identifies <see cref="Actions"/>.</summary>
    public static readonly BindableProperty ActionsProperty = Prop<View?>(nameof(Actions), null);
    /// <summary>Identifies <see cref="Body"/>.</summary>
    public static readonly BindableProperty BodyProperty = BindableProperty.Create(nameof(Body), typeof(View), typeof(KlyDashboardPage), null, propertyChanged: (b, _, v) => ((KlyDashboardPage)b).OnBodyChanged((View?)v));

    private readonly Grid _root = new();
    private readonly VerticalStackLayout _headerBox = new() { Spacing = 0 };
    private readonly Grid _header = new() { ColumnSpacing = 16 };
    private readonly Label _title = DashboardParts.NewLabel(20, KlyTheme.TextHighlighted, FontAttributes.Bold, LineBreakMode.TailTruncation);
    private readonly Label _description = DashboardParts.NewLabel(12, KlyTheme.TextMuted);
    private readonly HorizontalStackLayout _actions = new() { Spacing = 8, VerticalOptions = LayoutOptions.Start, HorizontalOptions = LayoutOptions.End };
    private readonly KlyButton _refresh = new() { Color = KlyColor.Neutral, Variant = ButtonVariant.Outline, Size = KlySize.Sm, MinimumHeightRequest = 40 };
    private readonly VerticalStackLayout _toolbarStack = new() { Spacing = 8 };
    private readonly Border _toolbar = new() { StrokeThickness = 0, Padding = 0 };
    private readonly ScrollView _tabsScroll = new() { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never };
    private readonly HorizontalStackLayout _tabs = new() { Spacing = 8 };
    private readonly FlexLayout _filters = new() { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, AlignItems = Microsoft.Maui.Layouts.FlexAlignItems.Center };
    private readonly VerticalStackLayout _menus = new() { Spacing = 0 };
    private readonly KlyButton _resetFilters = new() { Color = KlyColor.Primary, Variant = ButtonVariant.Link, Size = KlySize.Sm, MinimumHeightRequest = 40 };
    private readonly ContentView _bodyHost = new();
    private readonly ScrollView _scroll = new();
    private readonly VerticalStackLayout _flow = new() { Spacing = 0 };
    private readonly Dictionary<DashboardFilterControl, DashboardFilterView> _filterViews = new();
    private readonly List<(DashboardViewMeta View, DashboardTapArea Area)> _tabViews = [];
    private DashboardController? _subscribed;
    private ControllerRelay? _relay;
    private int _queued;

    /// <summary>Creates a page.</summary>
    public KlyDashboardPage()
    {
        this.SetThemeColor(BackgroundColorProperty, KlyTheme.BackgroundMuted);
        SemanticProperties.SetHeadingLevel(_title, SemanticHeadingLevel.Level1);

        var texts = new VerticalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Start };
        texts.Add(_title);
        texts.Add(_description);
        _header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        _header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        _header.Add(texts, 0);
        _header.Add(_actions, 1);
        _header.SetThemeColor(BackgroundColorProperty, KlyTheme.Background);
        _headerBox.Add(_header);
        _headerBox.Add(DashboardParts.Divider());

        _tabsScroll.Content = _tabs;
        _toolbarStack.Add(_tabsScroll);
        _toolbarStack.Add(_filters);
        _toolbarStack.Add(_menus);
        var toolbar = new VerticalStackLayout { Spacing = 0 };
        toolbar.Add(_toolbarStack);
        toolbar.Add(DashboardParts.Divider());
        _toolbar.Content = toolbar;
        _toolbar.SetThemeColor(BackgroundColorProperty, KlyTheme.Background.WithAlpha(0.95f));

        _refresh.Clicked += async (_, _) => await RefreshAsync();
        _resetFilters.Clicked += (_, _) => _subscribed?.ResetFilters();
        Content = _root;
        Compose();
        ApplyPadding(-1);
        Rebuild();

        Loaded += (_, _) =>
        {
            if (Dashboard is { } dashboard && !ReferenceEquals(_subscribed, dashboard)) Subscribe(dashboard);
            Rebuild();
        };
        Unloaded += (_, _) => Unsubscribe();
        SizeChanged += (_, _) => ApplyPadding(DashboardBreakpoints.ViewportWidth(this));
    }

    /// <summary>The controller shown by the page: it supplies the refresh button, the view tabs and the filters, and the page follows it. Without it only the title and the body are shown.</summary>
    public DashboardController? Dashboard { get => (DashboardController?)GetValue(DashboardProperty); set => SetValue(DashboardProperty, value); }

    /// <summary>The page heading.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Text under the heading. When empty, the page shows an <c>Updated</c> line from <see cref="DashboardController.UpdatedAt"/> once data has loaded.</summary>
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>Shows the refresh button, which calls <see cref="DashboardController.RefreshAsync"/>. Defaults to <see langword="true"/>.</summary>
    public bool ShowRefresh { get => (bool)GetValue(ShowRefreshProperty); set => SetValue(ShowRefreshProperty, value); }

    /// <summary>Shows the view tabs when the controller has at least two enabled views. Defaults to <see langword="true"/>.</summary>
    public bool ShowTabs { get => (bool)GetValue(ShowTabsProperty); set => SetValue(ShowTabsProperty, value); }

    /// <summary>Shows a control for each enabled filter that is not headless, chosen by its <see cref="DashboardFilterControl.Kind"/>: a text, number or date box, two date boxes for a range, your <see cref="CustomFilter"/>, or a pill with an option list. Defaults to <see langword="true"/>.</summary>
    public bool ShowFilters { get => (bool)GetValue(ShowFiltersProperty); set => SetValue(ShowFiltersProperty, value); }

    /// <summary>Builds the control of each enabled filter whose kind is <see cref="DashboardFilterKind.Custom"/>. It receives the filter: show its selection from <see cref="DashboardFilterControl.Values"/> and change it with <see cref="DashboardFilterControl.Value"/>, <see cref="DashboardFilterControl.SetValues"/> or <see cref="DashboardFilterControl.Reset"/>. Without it the toolbar leaves those filters out.</summary>
    public Func<DashboardFilterControl, View>? CustomFilter { get => (Func<DashboardFilterControl, View>?)GetValue(CustomFilterProperty); set => SetValue(CustomFilterProperty, value); }

    /// <summary>Keeps the header and the toolbar of tabs and filters at the top of the page while the body scrolls. Without it they scroll with the body. Defaults to <see langword="true"/>.</summary>
    public bool Sticky { get => (bool)GetValue(StickyProperty); set => SetValue(StickyProperty, value); }

    /// <summary>Makes the page scroll its content. Turn it off when the page sits in a <see cref="ScrollView"/> of your own, which a page that scrolls cannot be nested in. Defaults to <see langword="true"/>.</summary>
    public bool Scrollable { get => (bool)GetValue(ScrollableProperty); set => SetValue(ScrollableProperty, value); }

    /// <summary>A view placed before the refresh button in the header, for example your own buttons.</summary>
    public View? Actions { get => (View?)GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }

    /// <summary>The body of the page, usually a <see cref="KlyDashboardGrid"/>.</summary>
    public View? Body { get => (View?)GetValue(BodyProperty); set => SetValue(BodyProperty, value); }

    /// <summary>The controls of the filters that are shown: enabled, not headless, and for a custom filter only when <see cref="CustomFilter"/> is set.</summary>
    internal IReadOnlyList<DashboardFilterControl> VisibleControls =>
        Dashboard?.Controls.Where(c => c.Enabled && !c.Headless && (c.Kind != DashboardFilterKind.Custom || CustomFilter is not null)).ToList() ?? [];

    /// <summary>The header, for the tests.</summary>
    internal Grid Header => _header;

    /// <summary>The header with the line under it, for the tests.</summary>
    internal VerticalStackLayout HeaderBox => _headerBox;

    /// <summary>The toolbar, for the tests.</summary>
    internal Border Toolbar => _toolbar;

    /// <summary>The refresh button, for the tests.</summary>
    internal KlyButton RefreshButton => _refresh;

    /// <summary>The title label, for the tests.</summary>
    internal Label TitleLabel => _title;

    /// <summary>The line under the title, for the tests.</summary>
    internal Label DescriptionLabel => _description;

    /// <summary>The tabs of the views, for the tests.</summary>
    internal IReadOnlyList<(DashboardViewMeta View, DashboardTapArea Area)> TabViews => _tabViews;

    /// <summary>The row of the filters, for the tests.</summary>
    internal FlexLayout FilterRow => _filters;

    /// <summary>The place of the open filter lists, for the tests.</summary>
    internal VerticalStackLayout MenuHost => _menus;

    /// <summary>The control of a filter, for the tests.</summary>
    internal DashboardFilterView? ViewOf(DashboardFilterControl filter) => _filterViews.GetValueOrDefault(filter);

    /// <summary>The button that restores every filter, for the tests.</summary>
    internal KlyButton ResetButton => _resetFilters;

    /// <summary>The scrolling area, for the tests.</summary>
    internal ScrollView Scroll => _scroll;

    /// <summary>The root of the page, for the tests.</summary>
    internal Grid Root => _root;

    /// <summary>The place of the body, for the tests.</summary>
    internal ContentView BodyHost => _bodyHost;

    private static BindableProperty Prop<T>(string name, T? defaultValue) =>
        BindableProperty.Create(name, typeof(T), typeof(KlyDashboardPage), defaultValue, propertyChanged: (b, _, _) => ((KlyDashboardPage)b).Rebuild());

    private static DashboardTexts Texts => KlyDashboardTexts.Resolve();

    private void OnCustomFilterChanged()
    {
        // The views of the custom filters were made by the old template.
        foreach (var key in _filterViews.Keys.Where(k => k.Kind == DashboardFilterKind.Custom).ToList()) _filterViews.Remove(key);
        Rebuild();
    }

    private void OnBodyChanged(View? body)
    {
        if (body is not null) DashboardParts.Detach(body);
        _bodyHost.Content = body;
    }

    private void OnDashboardChanged(DashboardController? oldValue, DashboardController? newValue)
    {
        if (oldValue is not null && ReferenceEquals(_subscribed, oldValue)) Unsubscribe();
        if (newValue is not null) Subscribe(newValue);
        Rebuild();
        NotifyCards();
    }

    private void Subscribe(DashboardController dashboard)
    {
        Unsubscribe();
        _subscribed = dashboard;
        _relay = new ControllerRelay(this, dashboard);
        dashboard.Changed += _relay.Invoke;
    }

    private void Unsubscribe()
    {
        if (_subscribed is null) return;
        if (_relay is not null) _subscribed.Changed -= _relay.Invoke;
        _relay = null;
        _subscribed = null;
    }

    // The controller lives as long as the app wants and the page only as long as it is on screen: the controller reaches the page through a weak reference, so a page that was built and
    // never shown (and so never unloaded) is not kept alive by it. The relay takes itself off the controller once the page is gone.
    private sealed class ControllerRelay(KlyDashboardPage page, DashboardController controller)
    {
        private readonly WeakReference<KlyDashboardPage> _page = new(page);

        public void Invoke()
        {
            if (_page.TryGetTarget(out var target)) target.OnControllerChanged();
            else controller.Changed -= Invoke;
        }
    }

    // The controller raises Changed often, several times for one change and from any thread: the page builds itself once on the UI thread.
    private void OnControllerChanged()
    {
        if (Interlocked.Exchange(ref _queued, 1) == 1) return;
        DashboardParts.Post(this, () =>
        {
            Volatile.Write(ref _queued, 0);
            Rebuild();
            NotifyCards();
        });
    }

    private async Task RefreshAsync()
    {
        if (Dashboard is not { } dashboard) return;
        try
        {
            await dashboard.RefreshAsync();
        }
        catch (Exception)
        {
            // The failure of a load is the state of its resource (Error), which its block shows; nothing else can fail in a refresh.
        }
    }

    /// <summary>How many blocks the body holds, for the tests.</summary>
    internal int CardsCount() => CardsIn(Body).Count();

    /// <summary>Refreshes every block of the body, as the web page does when it renders its content again.</summary>
    internal void NotifyCards()
    {
        foreach (var card in CardsIn(Body)) card.Refresh();
    }

    private static IEnumerable<KlyDashboardCard> CardsIn(Element? root)
    {
        switch (root)
        {
            case KlyDashboardCard card:
                yield return card;
                break;
            case KlyDashboardGrid grid:
                foreach (var child in grid.Children.OfType<Element>())
                {
                    foreach (var card in CardsIn(child)) yield return card;
                }
                break;
            case Layout layout:
                foreach (var child in layout.Children.OfType<Element>())
                {
                    foreach (var card in CardsIn(child)) yield return card;
                }
                break;
            case Border { Content: { } content }:
                foreach (var card in CardsIn(content)) yield return card;
                break;
            case ContentView { Content: { } content }:
                foreach (var card in CardsIn(content)) yield return card;
                break;
            case ScrollView { Content: { } content }:
                foreach (var card in CardsIn(content)) yield return card;
                break;
        }
    }

    private void ApplyPadding(double viewportWidth)
    {
        var padding = DashboardBreakpoints.PagePadding(viewportWidth);
        _header.Padding = new Thickness(padding, 16);
        _toolbarStack.Padding = new Thickness(padding, 10);
        _bodyHost.Padding = new Thickness(padding);
    }

    /// <summary>The padding of the sides for a width: 16, or 28 from 1024 up.</summary>
    internal double SidePadding => _header.Padding.Left;

    /// <summary>Sets the padding for the width of the page, as a size change does.</summary>
    internal void Resize(double viewportWidth) => ApplyPadding(viewportWidth);

    // The header, the toolbar and the body, in the arrangement the options ask for.
    private void Compose()
    {
        foreach (var view in new View[] { _headerBox, _toolbar, _scroll, _bodyHost, _flow }) DashboardParts.Detach(view);
        _root.Children.Clear();
        _root.RowDefinitions.Clear();
        _flow.Children.Clear();
        _scroll.Content = null;

        if (Scrollable && !Sticky)
        {
            // Everything scrolls: the header and the toolbar go with the body.
            _root.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            _flow.Add(_headerBox);
            _flow.Add(_toolbar);
            _flow.Add(_bodyHost);
            _scroll.Content = _flow;
            _root.Add(_scroll, 0, 0);
            return;
        }

        _root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        _root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        _root.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        _root.Add(_headerBox, 0, 0);
        _root.Add(_toolbar, 0, 1);
        if (Scrollable)
        {
            _scroll.Content = _bodyHost;
            _root.Add(_scroll, 0, 2);
        }
        else
        {
            _root.Add(_bodyHost, 0, 2);
        }
    }

    /// <summary>Builds the header, the tabs and the filters from the properties and the state of the controller.</summary>
    internal void Rebuild()
    {
        var texts = Texts;
        var dashboard = Dashboard;

        DashboardParts.Show(_title, Title);
        if (!string.IsNullOrEmpty(Description)) DashboardParts.Show(_description, Description);
        else if (dashboard?.UpdatedAt is { } updatedAt) DashboardParts.Show(_description, texts.Updated(DashboardFormat.FormatRelativeTime(updatedAt, null, texts.RelativeTime)));
        else DashboardParts.Show(_description, null);

        var actions = new List<View>();
        if (Actions is { } custom) actions.Add(custom);
        if (ShowRefresh && dashboard is not null)
        {
            _refresh.Text = dashboard.Refreshing ? texts.Refreshing : texts.Refresh;
            _refresh.IsEnabled = !dashboard.Refreshing;
            SemanticProperties.SetDescription(_refresh, texts.Refresh);
            actions.Add(_refresh);
        }
        // The views of the app (a menu, a box) stay where they are while the page builds itself again: taking one out and putting it back would close the menu or take the focus.
        DashboardParts.Sync(_actions, actions);
        _headerBox.IsVisible = !string.IsNullOrEmpty(Title) || _description.IsVisible || actions.Count > 0;

        var views = dashboard?.Views ?? [];
        var showTabs = dashboard is not null && ShowTabs && views.Count >= 2;
        var controls = VisibleControls;
        var showFilters = dashboard is not null && ShowFilters && controls.Count > 0;
        _toolbar.IsVisible = showTabs || showFilters;
        BuildTabs(texts, dashboard, views, showTabs);
        BuildFilters(texts, dashboard, controls, showFilters);
    }

    private void BuildTabs(DashboardTexts texts, DashboardController? dashboard, IReadOnlyList<DashboardViewMeta> views, bool show)
    {
        _tabs.Children.Clear();
        _tabViews.Clear();
        _tabsScroll.IsVisible = show;
        if (!show || dashboard is null) return;
        SemanticProperties.SetDescription(_tabsScroll, texts.Views);
        foreach (var view in views)
        {
            var active = string.Equals(dashboard.CurrentView, view.Key, StringComparison.Ordinal);
            var tab = new Border { StrokeThickness = 0, Padding = new Thickness(12, 0), VerticalOptions = LayoutOptions.Center };
            tab.SetRadius(DashboardParts.RadiusOf(1));
            tab.SetThemeColor(BackgroundColorProperty, active ? KlyTheme.BackgroundMuted : KlyThemeColor.Transparent);
            var row = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
            var label = DashboardParts.NewLabel(12, active ? KlyTheme.TextHighlighted : KlyTheme.TextMuted, FontAttributes.Bold, LineBreakMode.NoWrap);
            label.Text = view.Label;
            label.VerticalOptions = LayoutOptions.Center;
            row.Add(label);
            var hasBadge = !string.IsNullOrEmpty(view.Badge) && view.Badge != "0";
            if (hasBadge) row.Add(new KlyBadge { Text = view.Badge, Color = KlyColor.Primary, Variant = KlyVariant.Soft, Size = KlySize.Xs });
            tab.Content = row;
            var area = new DashboardTapArea(tab) { Description = hasBadge ? $"{view.Label} {view.Badge}" : view.Label, MinimumTarget = 40 };
            var key = view.Key;
            area.Tapped += (_, _) => dashboard.CurrentView = key;
            _tabs.Add(area);
            _tabViews.Add((view, area));
        }
    }

    private void BuildFilters(DashboardTexts texts, DashboardController? dashboard, IReadOnlyList<DashboardFilterControl> controls, bool show)
    {
        _filters.IsVisible = show;
        if (!show || dashboard is null)
        {
            _filters.Children.Clear();
            _menus.Children.Clear();
            return;
        }

        foreach (var gone in _filterViews.Keys.Where(k => !controls.Contains(k)).ToList()) _filterViews.Remove(gone);
        var wanted = new List<View>();
        var menus = new List<View>();
        foreach (var filter in controls)
        {
            if (!_filterViews.TryGetValue(filter, out var view)) _filterViews[filter] = view = new DashboardFilterView(filter, CustomFilter, ToggleFilterMenu);
            view.Localize(texts);
            view.Update();
            view.Pill.Margin = new Thickness(0, 0, 8, 4);
            wanted.Add(view.Pill);
            if (view.MenuOpen && view.Menu is { } menu) menus.Add(menu);
        }
        if (dashboard.Filtered)
        {
            _resetFilters.Text = texts.ResetFilters;
            wanted.Add(_resetFilters);
        }

        // The same views in the same order stay where they are: moving a text box would take its focus, and an open list with a search box would lose it.
        DashboardParts.Sync(_filters, wanted);
        DashboardParts.Sync(_menus, menus);
    }

    private void ToggleFilterMenu(DashboardFilterControl target)
    {
        if (Dashboard is not { } dashboard) return;
        foreach (var control in dashboard.Controls)
        {
            if (!ReferenceEquals(control, target)) control.Open = false;
        }
        target.Open = !target.Open;
        Rebuild();
    }
}
