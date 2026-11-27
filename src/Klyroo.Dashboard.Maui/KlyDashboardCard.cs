using System.Text;

namespace Klyroo.Dashboard.Maui;

/// <summary>
/// The frame shared by every block: a header with a title and a menu, a loading skeleton, an error with a retry button, an empty state, the content, chips for the active filters and a footer.
/// Put any view in <see cref="Body"/> to build a custom block, or use the ready-made ones (<see cref="KlyDashboardStat"/>, <see cref="KlyDashboardChart"/>...), which derive from it.
/// </summary>
/// <remarks>
/// The card follows <see cref="Source"/> but a source has no change event of its own: inside a <see cref="KlyDashboardPage"/> the page refreshes its cards whenever its
/// <see cref="DashboardController"/> changes, and anywhere else call <see cref="Refresh"/> after the source changes.
/// The Expand button opens the content in a <see cref="KlyModal"/>, so it shows only when the card sits inside a <see cref="KlyOverlayHost"/>.
/// </remarks>
[ContentProperty(nameof(Body))]
public class KlyDashboardCard : Border
{
    /// <summary>Identifies <see cref="Source"/>.</summary>
    public static readonly BindableProperty SourceProperty = Prop<IDashboardSource?>(nameof(Source), null);
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = Prop<string?>(nameof(Title), null);
    /// <summary>Identifies <see cref="Subtitle"/>.</summary>
    public static readonly BindableProperty SubtitleProperty = Prop<string?>(nameof(Subtitle), null);
    /// <summary>Identifies <see cref="Span"/>.</summary>
    public static readonly BindableProperty SpanProperty = BindableProperty.Create(nameof(Span), typeof(int), typeof(KlyDashboardCard), 12, propertyChanged: (b, _, _) => ((KlyDashboardCard)b).ParentGrid?.Place());
    /// <summary>Identifies <see cref="Card"/>.</summary>
    public static readonly BindableProperty CardProperty = Prop(nameof(Card), true);
    /// <summary>Identifies <see cref="IsEmpty"/>.</summary>
    public static readonly BindableProperty IsEmptyProperty = Prop(nameof(IsEmpty), false);
    /// <summary>Identifies <see cref="EmptyTitle"/>.</summary>
    public static readonly BindableProperty EmptyTitleProperty = Prop<string?>(nameof(EmptyTitle), null);
    /// <summary>Identifies <see cref="EmptyDescription"/>.</summary>
    public static readonly BindableProperty EmptyDescriptionProperty = Prop<string?>(nameof(EmptyDescription), null);
    /// <summary>Identifies <see cref="Skeleton"/>.</summary>
    public static readonly BindableProperty SkeletonProperty = Prop(nameof(Skeleton), DashboardSkeletonKind.Rows);
    /// <summary>Identifies <see cref="Menu"/>.</summary>
    public static readonly BindableProperty MenuProperty = Prop<bool?>(nameof(Menu), null);
    /// <summary>Identifies <see cref="Freshness"/>.</summary>
    public static readonly BindableProperty FreshnessProperty = Prop<bool?>(nameof(Freshness), null);
    /// <summary>Identifies <see cref="ViewAsTable"/>.</summary>
    public static readonly BindableProperty ViewAsTableProperty = Prop(nameof(ViewAsTable), true);
    /// <summary>Identifies <see cref="Expandable"/>.</summary>
    public static readonly BindableProperty ExpandableProperty = Prop(nameof(Expandable), true);
    /// <summary>Identifies <see cref="Tabulate"/>.</summary>
    public static readonly BindableProperty TabulateProperty = Prop<Func<DashboardDataTable>?>(nameof(Tabulate), null);
    /// <summary>Identifies <see cref="Filters"/>.</summary>
    public static readonly BindableProperty FiltersProperty = Prop<IReadOnlyList<DashboardFilterControl>?>(nameof(Filters), null);
    /// <summary>Identifies <see cref="HeaderRight"/>.</summary>
    public static readonly BindableProperty HeaderRightProperty = Prop<View?>(nameof(HeaderRight), null);
    /// <summary>Identifies <see cref="Footer"/>.</summary>
    public static readonly BindableProperty FooterProperty = Prop<View?>(nameof(Footer), null);
    /// <summary>Identifies <see cref="Body"/>.</summary>
    public static readonly BindableProperty BodyProperty = Prop<View?>(nameof(Body), null);

    private readonly Grid _root = new();
    private readonly Grid _stack = new() { RowSpacing = 0 };
    private readonly BoxView _progress = new() { HeightRequest = 2, IsVisible = false, HorizontalOptions = LayoutOptions.Fill };
    private readonly Grid _header = new() { ColumnSpacing = 8, Padding = new Thickness(16, 16, 16, 0) };
    private readonly Label _title = DashboardParts.NewLabel(14, KlyTheme.TextHighlighted, FontAttributes.Bold, LineBreakMode.TailTruncation);
    private readonly Label _subtitle = DashboardParts.NewLabel(12, KlyTheme.TextMuted);
    private readonly FlexLayout _tools = new() { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, JustifyContent = Microsoft.Maui.Layouts.FlexJustify.End, AlignItems = Microsoft.Maui.Layouts.FlexAlignItems.Center, VerticalOptions = LayoutOptions.Start };
    private readonly KlyButton _tableButton = ToolButton();
    private readonly KlyButton _csvButton = ToolButton();
    private readonly KlyButton _expandButton = ToolButton();
    private readonly FlexLayout _chips = new() { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, Padding = new Thickness(16, 8, 16, 0), IsVisible = false };
    private readonly Grid _content = new() { RowSpacing = 0, Padding = new Thickness(16) };
    private readonly ContentView _phaseHost = new();
    private readonly VerticalStackLayout _footerBox = new() { Spacing = 8, IsVisible = false, Margin = new Thickness(0, 12, 0, 0) };
    private readonly Grid _footerRow = new() { ColumnSpacing = 8 };
    private readonly Label _freshness = DashboardParts.NewLabel(12, KlyTheme.TextMuted, lineBreak: LineBreakMode.NoWrap);
    // The dialog of Expand sits in the card only to find the overlay host; it has no size and takes no touch, so it never covers the card.
    private readonly KlyModal _modal = new() { Size = KlySize.Xl, InputTransparent = true, HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Start };
    private EventHandler<string>? _csvExported;
    private bool _showingTable;
    private bool _expanded;
    private bool _building;
    private bool _rebuildRequested;
    private bool _hiddenBySource;
    private bool _visibleBeforeHide = true;
    private bool _bodyDirty = true;
    private BodyKey? _bodyKey;
    private string? _chipsKey;
    private (bool Card, bool Panels)? _surfaceKey;

    // What the body of the card was built for: when none of it changed and no property did, the body stays as it is, with its scroll position, focus and open tooltips.
    private sealed record BodyKey(DashboardBlockPhase Phase, bool ShowingTable, bool Expanded, string? Error);

    /// <summary>Creates a card.</summary>
    public KlyDashboardCard()
    {
        HorizontalOptions = LayoutOptions.Fill;
        StrokeThickness = 1;
        Padding = 0;
        _progress.SetThemeColor(BoxView.ColorProperty, DashboardParts.PrimaryFill);
        SemanticProperties.SetHeadingLevel(_title, SemanticHeadingLevel.Level3);

        var texts = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Start };
        texts.Add(_title);
        texts.Add(_subtitle);
        _header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        _header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        _header.Add(texts, 0);
        _header.Add(_tools, 1);

        _footerRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        _footerRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        _freshness.HorizontalOptions = LayoutOptions.End;
        _freshness.VerticalOptions = LayoutOptions.Center;
        _footerRow.Add(_freshness, 1);
        _footerBox.Add(DashboardParts.Divider());
        _footerBox.Add(_footerRow);

        // The body is the phase on top and the footer at the bottom of the card, whatever the height the row of the grid gives it.
        _content.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        _content.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        _content.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        _content.Add(_phaseHost, 0, 0);
        _content.Add(_footerBox, 0, 2);
        foreach (var height in new[] { GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Star }) _stack.RowDefinitions.Add(new RowDefinition(height));
        _stack.Add(_progress, 0, 0);
        _stack.Add(_header, 0, 1);
        _stack.Add(_chips, 0, 2);
        _stack.Add(_content, 0, 3);
        _root.Add(_stack);
        _root.Add(_modal);
        Content = _root;

        _tableButton.Clicked += (_, _) =>
        {
            _showingTable = !_showingTable;
            Refresh();
        };
        _csvButton.Clicked += async (_, _) => await ExportCsvAsync();
        _expandButton.Clicked += (_, _) => Expand();
        _modal.Closed += (_, _) =>
        {
            if (!_expanded) return;
            _expanded = false;
            Refresh();
        };
        ParentChanged += (_, _) => Refresh();
        Loaded += (_, _) => Refresh();

        Refresh();
    }

    /// <summary>Raised with the CSV text when the user taps Download CSV. With a subscriber the card does not offer the file to the share sheet of the device itself: save it yourself. Name the file with <see cref="DashboardFormat.ResolveFileName"/>.</summary>
    public event EventHandler<string>? CsvExported
    {
        add => _csvExported += value;
        remove => _csvExported -= value;
    }

    /// <summary>The resource whose state drives the block: a skeleton while it loads, an error message with a retry button, then the content. The block does not read <c>Data</c> from it, so pass the data through the other properties. Without a source the content shows at once. The card activates a deferred source on its first refresh, and asks a disabled one again on every refresh, which loads it once its <c>enabled</c> predicate returns <see langword="true"/>.</summary>
    public IDashboardSource? Source { get => (IDashboardSource?)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }

    /// <summary>The heading of the block.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>A secondary line under the title.</summary>
    public string? Subtitle { get => (string?)GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }

    /// <summary>Width of the block in columns of the enclosing <see cref="KlyDashboardGrid"/>, clamped between 1 and its <see cref="KlyDashboardGrid.Columns"/> (12 outside a grid). Below 640 units wide every block takes the full row. Defaults to 12.</summary>
    public int Span { get => (int)GetValue(SpanProperty); set => SetValue(SpanProperty, value); }

    /// <summary>Whether the block draws its own surface (border, background and rounded corners; only the background in a <see cref="DashboardGridVariant.Panels"/> grid). Defaults to <see langword="true"/>.</summary>
    public bool Card { get => (bool)GetValue(CardProperty); set => SetValue(CardProperty, value); }

    /// <summary>Shows the empty state instead of the content once the source is ready.</summary>
    public bool IsEmpty { get => (bool)GetValue(IsEmptyProperty); set => SetValue(IsEmptyProperty, value); }

    /// <summary>The title of the empty state. Defaults to the <c>NoData</c> text of the locale (<c>No data for this period</c> in English).</summary>
    public string? EmptyTitle { get => (string?)GetValue(EmptyTitleProperty); set => SetValue(EmptyTitleProperty, value); }

    /// <summary>A line of text under the title of the empty state.</summary>
    public string? EmptyDescription { get => (string?)GetValue(EmptyDescriptionProperty); set => SetValue(EmptyDescriptionProperty, value); }

    /// <summary>The shape of the loading skeleton. <c>Stat</c>, <c>Bars</c>, <c>Lines</c>, <c>Paired</c> and <c>Stack</c> have their own shape, the other kinds show three lines.</summary>
    public DashboardSkeletonKind Skeleton { get => (DashboardSkeletonKind)GetValue(SkeletonProperty); set => SetValue(SkeletonProperty, value); }

    /// <summary>Shows the View as table, Download CSV and Expand buttons in the header once the content is displayed. When <see langword="null"/>, the <see cref="KlyDashboardGrid.Menu"/> of the enclosing grid applies, or <see langword="false"/> outside a grid.</summary>
    public bool? Menu { get => (bool?)GetValue(MenuProperty); set => SetValue(MenuProperty, value); }

    /// <summary>Shows an <c>Updated</c> footer built from <see cref="IDashboardSource.UpdatedAt"/>. When <see langword="null"/>, the <see cref="KlyDashboardGrid.Freshness"/> of the enclosing grid applies, or <see langword="false"/> outside a grid.</summary>
    public bool? Freshness { get => (bool?)GetValue(FreshnessProperty); set => SetValue(FreshnessProperty, value); }

    /// <summary>Shows the View as table button in the menu. It needs <see cref="Tabulate"/>. Defaults to <see langword="true"/>.</summary>
    public bool ViewAsTable { get => (bool)GetValue(ViewAsTableProperty); set => SetValue(ViewAsTableProperty, value); }

    /// <summary>Shows the Expand button in the menu, which opens the content in a <see cref="KlyModal"/>. It needs a <see cref="KlyOverlayHost"/> above the card. Defaults to <see langword="true"/>.</summary>
    public bool Expandable { get => (bool)GetValue(ExpandableProperty); set => SetValue(ExpandableProperty, value); }

    /// <summary>Returns the content of the block as a table. It enables the View as table and Download CSV buttons.</summary>
    public Func<DashboardDataTable>? Tabulate { get => (Func<DashboardDataTable>?)GetValue(TabulateProperty); set => SetValue(TabulateProperty, value); }

    /// <summary>Filters whose non-default selection is shown as removable chips under the header.</summary>
    public IReadOnlyList<DashboardFilterControl>? Filters { get => (IReadOnlyList<DashboardFilterControl>?)GetValue(FiltersProperty); set => SetValue(FiltersProperty, value); }

    /// <summary>A view placed in the header, before the menu buttons.</summary>
    public View? HeaderRight { get => (View?)GetValue(HeaderRightProperty); set => SetValue(HeaderRightProperty, value); }

    /// <summary>A view placed in the footer, before the freshness text.</summary>
    public View? Footer { get => (View?)GetValue(FooterProperty); set => SetValue(FooterProperty, value); }

    /// <summary>The content of the block, shown once the source is ready and not empty.</summary>
    public View? Body { get => (View?)GetValue(BodyProperty); set => SetValue(BodyProperty, value); }

    /// <summary>
    /// The phase the card is in, derived from the state of <see cref="Source"/>: <c>Idle</c> and <c>Loading</c> show the skeleton, <c>Error</c> the message, otherwise <c>Empty</c> or <c>Content</c>.
    /// Without a source it is <c>Content</c>, or <c>Empty</c> when <see cref="IsEmpty"/> is set.
    /// </summary>
    public DashboardBlockPhase Phase
    {
        get
        {
            if (Source is not { } source) return IsEmpty ? DashboardBlockPhase.Empty : DashboardBlockPhase.Content;
            return source.State switch
            {
                DashboardResourceState.Disabled or DashboardResourceState.Idle => DashboardBlockPhase.Idle,
                DashboardResourceState.Loading => DashboardBlockPhase.Loading,
                DashboardResourceState.Error => DashboardBlockPhase.Error,
                _ => IsEmpty ? DashboardBlockPhase.Empty : DashboardBlockPhase.Content,
            };
        }
    }

    /// <summary>The texts of the current culture.</summary>
    protected static DashboardTexts Texts => KlyDashboardTexts.Resolve();

    /// <summary>The culture that formats numbers and dates.</summary>
    protected static System.Globalization.CultureInfo FormatCulture => DashboardParts.FormatCulture;

    /// <summary>The <see cref="Menu"/> in effect: the property, else the one of the enclosing grid.</summary>
    internal bool EffectiveMenu => Menu ?? ParentGrid?.Menu ?? false;

    /// <summary>The <see cref="Freshness"/> in effect: the property, else the one of the enclosing grid.</summary>
    internal bool EffectiveFreshness => Freshness ?? ParentGrid?.Freshness ?? false;

    /// <summary>The nearest <see cref="KlyDashboardGrid"/> above the card.</summary>
    internal KlyDashboardGrid? ParentGrid
    {
        get
        {
            for (var current = Parent; current is not null; current = current.Parent)
            {
                if (current is KlyDashboardGrid grid) return grid;
            }
            return null;
        }
    }

    /// <summary>Whether the content is shown as a table instead of the block, for the tests.</summary>
    internal bool ShowingTable => _showingTable;

    /// <summary>Whether the content is open in the dialog, for the tests.</summary>
    internal bool Expanded => _expanded;

    /// <summary>The menu buttons, for the tests: the table toggle, Download CSV and Expand.</summary>
    internal (KlyButton Table, KlyButton Csv, KlyButton Expand) MenuButtons => (_tableButton, _csvButton, _expandButton);

    /// <summary>The dialog of the Expand button, for the tests.</summary>
    internal KlyModal Modal => _modal;

    /// <summary>The header title label, for the tests.</summary>
    internal Label TitleLabel => _title;

    /// <summary>The chips of the active filters, for the tests.</summary>
    internal FlexLayout Chips => _chips;

    /// <summary>The place of the content of the phase, for the tests.</summary>
    internal ContentView PhaseHost => _phaseHost;

    /// <summary>The footer, for the tests.</summary>
    internal VerticalStackLayout FooterBox => _footerBox;

    /// <summary>The thin bar shown while the source fetches, for the tests.</summary>
    internal BoxView Progress => _progress;

    /// <summary>The freshness text, for the tests.</summary>
    internal Label FreshnessLabel => _freshness;

    /// <summary>The tools of the header, for the tests.</summary>
    internal FlexLayout Tools => _tools;

    /// <summary>The header, for the tests.</summary>
    internal Grid Header => _header;

    /// <summary>Whether the card is hidden because its source is disabled.</summary>
    internal bool HiddenBySource => _hiddenBySource;

    /// <summary>
    /// Brings the card up to date with the state of <see cref="Source"/>: the phase, the progress bar, the chips, the footer, and the content when it has to change (a new phase, the table view, the dialog,
    /// or a property that changed since). The content that is on screen is kept otherwise, with its scroll position and focus. A card inside a <see cref="KlyDashboardPage"/> is refreshed by the page
    /// when the controller changes; elsewhere call it when the source changed. Refreshing a card also activates a deferred source and asks a disabled one again.
    /// </summary>
    public void Refresh()
    {
        if (_building)
        {
            _rebuildRequested = true;
            return;
        }
        _building = true;
        try
        {
            do
            {
                _rebuildRequested = false;
                Rebuild();
            }
            while (_rebuildRequested);
        }
        finally
        {
            _building = false;
        }
    }

    /// <summary>Builds the content of the block again at the next refresh, which this runs now: a property of the block that the content shows has changed.</summary>
    protected void InvalidateContent()
    {
        _bodyDirty = true;
        Refresh();
    }

    /// <summary>The content of the block for the phase <c>Content</c>. The ready-made blocks build theirs here; a card shows <see cref="Body"/>.</summary>
    /// <returns>The view, or <see langword="null"/> for none.</returns>
    protected virtual View? BuildBody() => Body;

    /// <summary>Called by <see cref="Refresh"/> before the card is built, for a block that keeps part of its header (a status badge) in sync with its properties.</summary>
    protected virtual void OnRefreshing()
    {
    }

    private static BindableProperty Prop<T>(string name, T? defaultValue) =>
        BindableProperty.Create(name, typeof(T), typeof(KlyDashboardCard), defaultValue, propertyChanged: (b, _, _) => ((KlyDashboardCard)b).InvalidateContent());

    private static KlyButton ToolButton()
    {
        var button = new KlyButton { Variant = ButtonVariant.Ghost, Color = KlyColor.Neutral, Size = KlySize.Xs, MinimumHeightRequest = 40, VerticalOptions = LayoutOptions.Center };
        return button;
    }

    private void Rebuild()
    {
        OnRefreshing();
        ActivateIfNeeded();

        var texts = Texts;
        var source = Source;
        var phase = Phase;

        // A source that is disabled and has not loaded draws nothing, not even the frame; the grid closes the gap.
        var hidden = phase == DashboardBlockPhase.Idle && source?.State == DashboardResourceState.Disabled;
        if (hidden != _hiddenBySource)
        {
            _hiddenBySource = hidden;
            // Back from hiding, the card is as visible as the app had it.
            if (hidden) _visibleBeforeHide = IsVisible;
            IsVisible = hidden ? false : _visibleBeforeHide;
        }

        // The dialog only stays open while the card can open one: the menu is on and there is a host.
        if (_expanded && !(EffectiveMenu && Expandable && CanExpand)) _expanded = false;

        ApplySurface();
        _progress.IsVisible = source?.Fetching == true;

        DashboardParts.Show(_title, Title);
        DashboardParts.Show(_subtitle, Subtitle);
        BuildTools(texts, phase);
        _header.IsVisible = !string.IsNullOrEmpty(Title) || !string.IsNullOrEmpty(Subtitle) || HeaderRight is not null || EffectiveMenu;
        BuildChips(texts);
        BuildFooter(texts, phase, source);

        // The content is built again when a property changed or the phase, the table view or the dialog did, and kept otherwise: it holds a scroll position, a focus and tooltips.
        var key = new BodyKey(phase, _showingTable, _expanded, source?.Error);
        if (!_bodyDirty && key == _bodyKey) return;
        _bodyDirty = false;
        _bodyKey = key;
        PlaceBody(texts, BuildPhaseBody(texts, phase));
    }

    private void ActivateIfNeeded()
    {
        // A deferred source loads on the first refresh of its card; a disabled one is asked again, so it loads as soon as its enabled predicate turns true.
        if (Source is { } source && (source is { Stage: DashboardQueryStage.Deferred, Active: false } || source.State == DashboardResourceState.Disabled))
        {
            _ = ActivateAsync(source);
        }
    }

    private async Task ActivateAsync(IDashboardSource source)
    {
        var before = StateOf(source);
        try
        {
            await source.ActivateAsync();
        }
        catch (Exception)
        {
            // A loader's failure is the state of the source (Error); anything else has nowhere to go from a refresh.
        }
        // A card outside a page has nobody to tell it the load ended. A source that did nothing (still disabled) changes nothing, so this cannot go round again.
        if (StateOf(source) != before) DashboardParts.Post(this, Refresh);
    }

    private static (DashboardResourceState State, bool Active, bool Fetching, DateTimeOffset? UpdatedAt, string? Error) StateOf(IDashboardSource source) =>
        (source.State, source.Active, source.Fetching, source.UpdatedAt, source.Error);

    private void ApplySurface()
    {
        var panels = ParentGrid?.Variant == DashboardGridVariant.Panels;
        var key = (Card, panels);
        if (_surfaceKey == key) return;
        _surfaceKey = key;
        if (!Card)
        {
            this.SetThemeColor(BackgroundColorProperty, KlyThemeColor.Transparent);
            this.SetStroke(KlyThemeColor.Transparent);
            StrokeThickness = 0;
            this.SetRadius(0);
        }
        else if (panels)
        {
            this.SetThemeColor(BackgroundColorProperty, KlyTheme.Background);
            this.SetStroke(KlyThemeColor.Transparent);
            StrokeThickness = 0;
            this.SetRadius(0);
        }
        else
        {
            this.SetThemeColor(BackgroundColorProperty, KlyTheme.Background);
            this.SetStroke(KlyTheme.Border);
            StrokeThickness = 1;
            this.SetRadius(DashboardParts.RadiusOf(0.75));
        }
    }

    private void BuildTools(DashboardTexts texts, DashboardBlockPhase phase)
    {
        var wanted = new List<View>();
        if (HeaderRight is { } right) wanted.Add(right);
        if (EffectiveMenu && phase == DashboardBlockPhase.Content)
        {
            if (ViewAsTable && Tabulate is not null)
            {
                _tableButton.Text = _showingTable ? texts.HideTable : texts.ViewAsTable;
                wanted.Add(_tableButton);
            }
            if (Tabulate is not null)
            {
                _csvButton.Text = texts.DownloadCsv;
                wanted.Add(_csvButton);
            }
            if (Expandable && CanExpand)
            {
                _expandButton.Text = _expanded ? texts.Collapse : texts.Expand;
                SemanticProperties.SetHint(_expandButton, Title);
                wanted.Add(_expandButton);
            }
        }
        // The views of the app stay where they are while the card is refreshed: taking one out and putting it back would close a menu it holds or take the focus of a box.
        DashboardParts.Sync(_tools, wanted);
    }

    private bool CanExpand => KlyOverlayHost.TryFind(this, out _);

    private void BuildChips(DashboardTexts texts)
    {
        var active = Filters?.Where(f => f.Changed && f.Enabled).ToList() ?? [];
        _chips.IsVisible = active.Count > 0;
        var key = string.Join('\u001f', active.Select(f => $"{f.Key}\u001e{f.Label}: {f.Display}"));
        if (key == _chipsKey) return;
        _chipsKey = key;
        _chips.Children.Clear();
        foreach (var filter in active)
        {
            var chip = new Border { StrokeThickness = 0, Padding = new Thickness(10, 0, 0, 0), Margin = new Thickness(0, 0, 6, 6), HorizontalOptions = LayoutOptions.Start };
            chip.SetRadius(DashboardParts.Pill);
            chip.SetThemeColor(BackgroundColorProperty, KlyTheme.BackgroundMuted);
            var row = new HorizontalStackLayout { Spacing = 0, VerticalOptions = LayoutOptions.Center };
            var text = DashboardParts.NewLabel(12, KlyTheme.Text, FontAttributes.Bold, LineBreakMode.NoWrap);
            text.Text = $"{filter.Label}: {filter.Display}";
            text.VerticalOptions = LayoutOptions.Center;
            var remove = new DashboardTapArea(new KlyIcon { Name = KlyIconNames.Close, Size = KlySize.Sm, IconColor = KlyTheme.TextMuted, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center })
            {
                Description = texts.RemoveFilter(filter.Label),
                WidthRequest = 40,
                HeightRequest = 40,
            };
            var current = filter;
            remove.Tapped += (_, _) => current.Reset();
            row.Add(text);
            row.Add(remove);
            chip.Content = row;
            _chips.Add(chip);
        }
    }

    private View BuildPhaseBody(DashboardTexts texts, DashboardBlockPhase phase)
    {
        switch (phase)
        {
            case DashboardBlockPhase.Idle:
            case DashboardBlockPhase.Loading:
                return DashboardSkeletons.Build(Skeleton);
            case DashboardBlockPhase.Error:
                return BuildError(texts);
            case DashboardBlockPhase.Empty:
                return BuildEmpty(texts);
            default:
                if (_showingTable && Tabulate is { } tabulate) return DashboardTableView.Build(tabulate());
                return BuildBody() ?? new BoxView { HeightRequest = 0, Color = Colors.Transparent };
        }
    }

    private View BuildError(DashboardTexts texts)
    {
        var box = new Border { StrokeThickness = 1, Padding = new Thickness(12), HorizontalOptions = LayoutOptions.Fill };
        box.SetRadius(DashboardParts.RadiusOf(1));
        var error = DashboardParts.FillOf(KlyColor.Error);
        box.SetThemeColor(BackgroundColorProperty, error.WithAlpha(0.10f));
        box.SetStroke(error.WithAlpha(0.30f));
        var stack = new VerticalStackLayout { Spacing = 8 };
        var title = DashboardParts.NewLabel(12, DashboardParts.TextOf(KlyColor.Error), FontAttributes.Bold);
        title.Text = texts.ErrorTitle;
        var message = DashboardParts.NewLabel(12, KlyTheme.TextMuted);
        message.Text = Source?.Error ?? texts.ErrorMessage;
        stack.Add(title);
        stack.Add(message);
        if (Source is { } source)
        {
            var retry = new KlyButton { Text = texts.Retry, Color = KlyColor.Neutral, Variant = ButtonVariant.Outline, Size = KlySize.Sm, MinimumHeightRequest = 40 };
            retry.Clicked += async (_, _) =>
            {
                try
                {
                    await source.RefreshAsync();
                }
                catch (Exception)
                {
                    // The failure of the load is the state of the source: the card shows it again.
                }
            };
            stack.Add(retry);
        }
        box.Content = stack;
        SemanticProperties.SetDescription(box, $"{texts.ErrorTitle}. {message.Text}");
        return box;
    }

    private View BuildEmpty(DashboardTexts texts)
    {
        var stack = new VerticalStackLayout { Spacing = 4, Padding = new Thickness(0, 24), HorizontalOptions = LayoutOptions.Fill };
        var title = DashboardParts.NewLabel(12, KlyTheme.TextHighlighted, FontAttributes.Bold);
        title.Text = EmptyTitle ?? texts.NoData;
        title.HorizontalTextAlignment = TextAlignment.Center;
        stack.Add(title);
        if (!string.IsNullOrEmpty(EmptyDescription))
        {
            var description = DashboardParts.NewLabel(12, KlyTheme.TextMuted);
            description.Text = EmptyDescription;
            description.HorizontalTextAlignment = TextAlignment.Center;
            stack.Add(description);
        }
        return stack;
    }

    private void BuildFooter(DashboardTexts texts, DashboardBlockPhase phase, IDashboardSource? source)
    {
        // The footer row holds the app's footer view in the first column and the freshness in the second.
        foreach (var child in _footerRow.Children.OfType<View>().Where(v => !ReferenceEquals(v, _freshness) && !ReferenceEquals(v, Footer)).ToList()) _footerRow.Remove(child);
        if (Footer is { } footer && !ReferenceEquals(footer.Parent, _footerRow))
        {
            DashboardParts.Detach(footer);
            _footerRow.Add(footer, 0);
        }
        var updatedAt = EffectiveFreshness && phase == DashboardBlockPhase.Content ? source?.UpdatedAt : null;
        _freshness.IsVisible = updatedAt is not null;
        _freshness.Text = updatedAt is { } at ? texts.Updated(DashboardFormat.FormatRelativeTime(at, null, texts.RelativeTime)) : null;
        if (updatedAt is { } moment) SemanticProperties.SetDescription(_freshness, moment.ToString("u"));
        _footerBox.IsVisible = Footer is not null || updatedAt is not null;
    }

    private void PlaceBody(DashboardTexts texts, View body)
    {
        if (_expanded)
        {
            // The content is in the dialog while it is open: it is drawn in one place only.
            _phaseHost.Content = new BoxView { HeightRequest = 96, Color = Colors.Transparent };
            _modal.Title = Title;
            _modal.Description = Subtitle;
            _modal.AccessibleName = texts.Expand;
            DashboardParts.Detach(body);
            _modal.Body = body;
            if (!_modal.IsOpen)
            {
                try
                {
                    _modal.IsOpen = true;
                }
                catch (InvalidOperationException)
                {
                    // No host: the card cannot open a dialog, so it stays as it was.
                    _expanded = false;
                    _bodyKey = _bodyKey! with { Expanded = false };
                }
            }
            if (_expanded) return;
        }

        if (_modal.IsOpen) _modal.IsOpen = false;
        _modal.Body = null;
        if (ReferenceEquals(_phaseHost.Content, body)) return;
        DashboardParts.Detach(body);
        _phaseHost.Content = body;
    }

    private void Expand()
    {
        if (!CanExpand) return;
        _expanded = !_expanded;
        Refresh();
    }

    /// <summary>Saves the table of the block as CSV: raises <see cref="CsvExported"/> when something listens, otherwise offers the file to the share sheet of the device.</summary>
    internal async Task ExportCsvAsync()
    {
        if (Tabulate is not { } tabulate) return;
        var csv = DashboardFormat.ExportCsv(tabulate(), FormatCulture);
        if (_csvExported is { } handler)
        {
            handler(this, csv);
            return;
        }
        await ShareAsync(csv);
    }

    private async Task ShareAsync(string csv)
    {
        try
        {
            var name = DashboardFormat.ResolveFileName(Title);
            var path = Path.Combine(FileSystem.CacheDirectory, name);
            // The CSV text already starts with the byte order mark Excel looks for: the encoding must not add a second one.
            await File.WriteAllTextAsync(path, csv, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            await Share.Default.RequestAsync(new ShareFileRequest(Title ?? name, new ShareFile(path)));
        }
        catch (Exception)
        {
            // No cache folder or share sheet (no platform), or the user closed it: the table stays on screen, and CsvExported is the way to save it yourself.
        }
    }
}
