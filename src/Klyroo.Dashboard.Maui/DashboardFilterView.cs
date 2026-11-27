namespace Klyroo.Dashboard.Maui;

/// <summary>
/// The control of one <see cref="DashboardFilterControl"/> in the toolbar of a <see cref="KlyDashboardPage"/>, chosen by its kind: a text, number or date box, two date boxes for a range, the app's own view
/// for a custom filter, or a pill that opens a list of options. It is built once and <see cref="Update"/> brings it up to date with the filter, so a box keeps its focus while the toolbar changes.
/// </summary>
internal sealed class DashboardFilterView
{
    private readonly DashboardFilterControl _filter;
    private readonly Action<DashboardFilterControl> _toggle;
    private readonly Border _pill = new() { StrokeThickness = 1, Padding = new Thickness(12, 0), HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Center };
    private readonly Label _label = DashboardParts.NewLabel(12, KlyTheme.TextMuted, lineBreak: LineBreakMode.NoWrap);
    private readonly Label _display = DashboardParts.NewLabel(12, KlyTheme.Text, FontAttributes.Bold, LineBreakMode.NoWrap);
    private readonly HorizontalStackLayout _row = new() { Spacing = 4, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Start };
    private readonly FlexLayout _presets = new() { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, VerticalOptions = LayoutOptions.Center };
    private readonly DashboardTapArea _clear;
    private readonly Entry? _entry;
    private readonly DatePicker? _start;
    private readonly DatePicker? _end;
    private readonly DashboardTapArea? _pillArea;
    private readonly View? _custom;
    private readonly Border? _menu;
    private readonly Entry? _search;
    private readonly VerticalStackLayout? _options;
    private bool _syncing;

    // The limits of a picker when no bound narrows them: the defaults of DatePicker.
    private static readonly DateTime FirstDate = new(1900, 1, 1);
    private static readonly DateTime LastDate = new(2100, 12, 31);

    public DashboardFilterView(DashboardFilterControl filter, Func<DashboardFilterControl, View>? custom, Action<DashboardFilterControl> toggle)
    {
        _filter = filter;
        _toggle = toggle;
        _label.Text = $"{filter.Label}:";
        _label.VerticalOptions = LayoutOptions.Center;
        _pill.SetRadius(DashboardParts.Pill);
        _pill.MinimumHeightRequest = 40;

        _clear = new DashboardTapArea(new KlyIcon { Name = KlyIconNames.Close, Size = KlySize.Sm, IconColor = KlyTheme.TextMuted, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center })
        {
            WidthRequest = 40,
            HeightRequest = 40,
            IsVisible = false,
        };
        _clear.Tapped += (_, _) => _filter.Reset();

        switch (filter.Kind)
        {
            case DashboardFilterKind.Custom:
                _custom = custom?.Invoke(filter);
                if (_custom is not null) _row.Add(_custom);
                break;

            case DashboardFilterKind.String:
            case DashboardFilterKind.Number:
                _entry = new Entry
                {
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    WidthRequest = filter.Kind == DashboardFilterKind.Number ? 80 : 128,
                    Keyboard = filter.Kind == DashboardFilterKind.Number ? Keyboard.Numeric : Keyboard.Default,
                    Placeholder = filter.Placeholder,
                    BackgroundColor = Colors.Transparent,
                    VerticalOptions = LayoutOptions.Center,
                };
                _entry.SetThemeColor(Entry.TextColorProperty, KlyTheme.Text);
                _entry.SetThemeColor(Entry.PlaceholderColorProperty, KlyTheme.TextDimmed);
                SemanticProperties.SetDescription(_entry, filter.Label);
                _entry.Completed += (_, _) => Commit();
                _entry.Unfocused += (_, _) => Commit();
                _pill.Content = Row(_label, _entry);
                _row.Add(_pill);
                break;

            case DashboardFilterKind.Date:
                _start = NewPicker(filter.Label);
                _start.DateSelected += (_, e) => Write(() => DashboardFilterInput.SetInputValue(_filter, e.NewDate is { } date ? DashboardFilterInput.FormatDate(date) : null));
                _pill.Content = Row(_label, _start);
                _row.Add(_pill);
                break;

            case DashboardFilterKind.DateRange:
                _start = NewPicker(string.Empty);
                _end = NewPicker(string.Empty);
                _start.DateSelected += (_, e) => Write(() => DashboardFilterInput.SetRangeBound(_filter, 0, e.NewDate is { } date ? DashboardFilterInput.FormatDate(date) : null));
                _end.DateSelected += (_, e) => Write(() => DashboardFilterInput.SetRangeBound(_filter, 1, e.NewDate is { } date ? DashboardFilterInput.FormatDate(date) : null));
                var dash = DashboardParts.NewLabel(12, KlyTheme.TextMuted, lineBreak: LineBreakMode.NoWrap);
                dash.Text = "–";
                dash.VerticalOptions = LayoutOptions.Center;
                AutomationProperties.SetIsInAccessibleTree(dash, false);
                _pill.Content = Row(_label, _start, dash, _end);
                _row.Add(_pill);
                break;

            default:
                _pill.Content = Row(_label, _display);
                _pillArea = new DashboardTapArea(_pill) { MinimumTarget = 40, HorizontalOptions = LayoutOptions.Start };
                _pillArea.Tapped += (_, _) => _toggle(_filter);
                _row.Add(_pillArea);
                (_menu, _search, _options) = BuildMenu();
                break;
        }

        if (filter.Presets.Count > 0 && filter.Kind is DashboardFilterKind.String or DashboardFilterKind.Number or DashboardFilterKind.Date or DashboardFilterKind.DateRange)
        {
            foreach (var preset in filter.Presets) _presets.Add(PresetChip(preset));
            _row.Add(_presets);
        }
        if (filter.Kind != DashboardFilterKind.Custom) _row.Add(_clear);
    }

    /// <summary>What goes in the row of the toolbar.</summary>
    public View Pill => _row;

    /// <summary>The panel of an option list, or <see langword="null"/> for the other kinds.</summary>
    public View? Menu => _menu;

    /// <summary>Whether the panel is wanted now: the filter is open and has options or presets to show.</summary>
    public bool MenuOpen => _menu is not null && _filter.Open && (_filter.Items.Count > 0 || _filter.Presets.Count > 0);

    /// <summary>The text box of a text or number filter.</summary>
    public Entry? Entry => _entry;

    /// <summary>The date boxes of a date filter (the start) and of a range (both).</summary>
    public (DatePicker? Start, DatePicker? End) Pickers => (_start, _end);

    /// <summary>The search box of an option list.</summary>
    public Entry? Search => _search;

    /// <summary>The rows of the option list.</summary>
    public VerticalStackLayout? Options => _options;

    /// <summary>The pill, whose look follows <see cref="DashboardFilterControl.Changed"/>.</summary>
    public Border PillFrame => _pill;

    /// <summary>The button that clears the filter.</summary>
    public DashboardTapArea Clear => _clear;

    /// <summary>The presets shown beside a text, number or date box.</summary>
    public FlexLayout Presets => _presets;

    /// <summary>The label of the selection of an option pill.</summary>
    public Label Display => _display;

    /// <summary>The button of an option pill, which opens its list.</summary>
    public DashboardTapArea? PillButton => _pillArea;

    /// <summary>Brings the control up to date with the filter.</summary>
    public void Update()
    {
        var changed = _filter.Changed;
        if (_filter.Kind != DashboardFilterKind.Custom)
        {
            if (changed)
            {
                _pill.SetStroke(DashboardParts.PrimaryText);
                _pill.SetThemeColor(VisualElement.BackgroundColorProperty, DashboardParts.PrimaryFill.WithAlpha(0.10f));
            }
            else
            {
                _pill.SetStroke(KlyTheme.Border);
                _pill.SetThemeColor(VisualElement.BackgroundColorProperty, KlyTheme.Background);
            }
            _display.SetThemeColor(Label.TextColorProperty, changed ? KlyTheme.TextHighlighted : KlyTheme.Text);
        }
        _clear.IsVisible = changed && _filter.Kind != DashboardFilterKind.Custom;

        _syncing = true;
        try
        {
            if (_entry is not null && _entry.Text != (_filter.Value ?? string.Empty) && !_entry.IsFocused) _entry.Text = _filter.Value ?? string.Empty;
            if (_filter.Kind == DashboardFilterKind.Date && _start is not null) _start.Date = DashboardFilterInput.ParseDate(_filter.Value);
            if (_filter.Kind == DashboardFilterKind.DateRange && _start is not null && _end is not null)
            {
                // A picker pulls its date back inside its limits, so the limits of the last range open up before the new dates go in, and close on the new range after.
                _start.MaximumDate = LastDate;
                _end.MinimumDate = FirstDate;
                _start.Date = DashboardFilterInput.ParseDate(DashboardFilterInput.RangeBound(_filter, 0));
                _end.Date = DashboardFilterInput.ParseDate(DashboardFilterInput.RangeBound(_filter, 1));
                _start.MaximumDate = _end.Date ?? LastDate;
                _end.MinimumDate = _start.Date ?? FirstDate;
            }
        }
        finally
        {
            _syncing = false;
        }

        if (_pillArea is not null)
        {
            _display.Text = _filter.Display;
            _pillArea.Description = $"{_filter.Label}: {_filter.Display}";
            // The rows of a closed list are not on screen: they are built when it opens.
            if (_filter.Open) RebuildOptions();
            if (_menu is not null) _menu.IsVisible = MenuOpen;
        }
    }

    /// <summary>Sets the accessible names that need the texts of the current culture.</summary>
    public void Localize(DashboardTexts texts)
    {
        _clear.Description = texts.ClearFilter(_filter.Label);
        if (_start is not null && _filter.Kind == DashboardFilterKind.DateRange) SemanticProperties.SetDescription(_start, texts.FilterFrom(_filter.Label));
        if (_end is not null) SemanticProperties.SetDescription(_end, texts.FilterTo(_filter.Label));
        if (_search is not null)
        {
            _search.Placeholder = texts.SearchPlaceholder;
            SemanticProperties.SetDescription(_search, texts.SearchFilter(_filter.Label));
        }
    }

    // A text or number box applies when it loses the focus or the user confirms, like the change event of a web input.
    private void Commit()
    {
        if (_syncing || _entry is null) return;
        if (_entry.Text == (_filter.Value ?? string.Empty)) return;
        DashboardFilterInput.SetInputValue(_filter, _entry.Text);
        // A text that was refused (not a number) goes back to the stored value.
        if (_entry.Text != (_filter.Value ?? string.Empty) && !string.IsNullOrWhiteSpace(_entry.Text)) _entry.Text = _filter.Value ?? string.Empty;
    }

    private void Write(Action write)
    {
        if (!_syncing) write();
    }

    private static DatePicker NewPicker(string description)
    {
        var picker = new DatePicker { FontSize = 12, FontAttributes = FontAttributes.Bold, Format = "d", Date = null, VerticalOptions = LayoutOptions.Center, MinimumHeightRequest = 40 };
        picker.SetThemeColor(DatePicker.TextColorProperty, KlyTheme.Text);
        if (description.Length > 0) SemanticProperties.SetDescription(picker, description);
        return picker;
    }

    private static HorizontalStackLayout Row(params View[] views)
    {
        var row = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
        foreach (var view in views) row.Add(view);
        return row;
    }

    private View PresetChip(DashboardFilterPreset preset)
    {
        var content = new Border { StrokeThickness = 0, Padding = new Thickness(8, 2), VerticalOptions = LayoutOptions.Center };
        content.SetRadius(DashboardParts.RadiusOf(1));
        content.SetThemeColor(VisualElement.BackgroundColorProperty, KlyTheme.BackgroundMuted);
        var row = new HorizontalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center };
        if (!string.IsNullOrEmpty(preset.Icon)) row.Add(new KlyIcon { Name = preset.Icon, Size = KlySize.Sm, Scale = 0.75, IconColor = KlyTheme.Text, VerticalOptions = LayoutOptions.Center });
        var text = DashboardParts.NewLabel(11, KlyTheme.Text, FontAttributes.Bold, LineBreakMode.NoWrap);
        text.Text = preset.Label;
        text.VerticalOptions = LayoutOptions.Center;
        row.Add(text);
        content.Content = row;
        var area = new DashboardTapArea(content) { Description = preset.Label, MinimumTarget = 40, Margin = new Thickness(0, 0, 4, 0) };
        area.Tapped += (_, _) => _filter.ApplyPreset(preset);
        return area;
    }

    private (Border Menu, Entry? Search, VerticalStackLayout Options) BuildMenu()
    {
        var menu = new Border { StrokeThickness = 1, Padding = new Thickness(8), WidthRequest = 240, HorizontalOptions = LayoutOptions.Start, IsVisible = false };
        menu.SetRadius(DashboardParts.RadiusOf(0.75));
        menu.SetStroke(KlyTheme.Border);
        menu.SetThemeColor(VisualElement.BackgroundColorProperty, KlyTheme.BackgroundElevated);
        var stack = new VerticalStackLayout { Spacing = 6 };

        Entry? search = null;
        if (_filter.Searchable)
        {
            search = new Entry { FontSize = 12, Placeholder = "Search…", MinimumHeightRequest = 40, ReturnType = ReturnType.Search };
            search.SetThemeColor(Entry.TextColorProperty, KlyTheme.Text);
            search.SetThemeColor(Entry.PlaceholderColorProperty, KlyTheme.TextDimmed);
            search.TextChanged += (_, e) =>
            {
                if (_syncing) return;
                _filter.Search = e.NewTextValue ?? string.Empty;
                RebuildOptions();
            };
            stack.Add(search);
        }
        if (_filter.Presets.Count > 0)
        {
            var presets = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
            foreach (var preset in _filter.Presets) presets.Add(PresetChip(preset));
            stack.Add(presets);
            stack.Add(DashboardParts.Divider());
        }
        var options = new VerticalStackLayout { Spacing = 2 };
        stack.Add(new ScrollView { MaximumHeightRequest = 208, Content = options });
        menu.Content = stack;
        return (menu, search, options);
    }

    private void RebuildOptions()
    {
        if (_options is null) return;
        _options.Children.Clear();
        foreach (var option in _filter.FilteredItems)
        {
            var selected = _filter.IsSelected(option.Value);
            var maxReached = _filter.Multiple && !selected && _filter.Max is { } max && _filter.Values.Count >= max;
            var row = new Grid { ColumnSpacing = 8, Padding = new Thickness(8, 4) };
            row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            var left = new HorizontalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };
            if (!string.IsNullOrEmpty(option.Icon)) left.Add(new KlyIcon { Name = option.Icon, Size = KlySize.Sm, IconColor = KlyTheme.TextMuted, VerticalOptions = LayoutOptions.Center });
            var texts = new VerticalStackLayout { Spacing = 0, VerticalOptions = LayoutOptions.Center };
            var label = DashboardParts.NewLabel(12, selected ? KlyTheme.TextHighlighted : KlyTheme.Text, selected ? FontAttributes.Bold : FontAttributes.None, LineBreakMode.TailTruncation);
            label.Text = option.Label;
            texts.Add(label);
            if (!string.IsNullOrEmpty(option.Description))
            {
                var description = DashboardParts.NewLabel(11, KlyTheme.TextMuted, lineBreak: LineBreakMode.TailTruncation);
                description.Text = option.Description;
                texts.Add(description);
            }
            left.Add(texts);
            row.Add(left, 0);
            if (selected) row.Add(new KlyIcon { Name = KlyIconNames.Check, Size = KlySize.Sm, IconColor = KlyTheme.TextHighlighted, VerticalOptions = LayoutOptions.Center }, 1);

            var frame = new Border { StrokeThickness = 0, Padding = 0, Content = row };
            frame.SetRadius(DashboardParts.RadiusOf(1));
            frame.SetThemeColor(VisualElement.BackgroundColorProperty, selected ? KlyTheme.BackgroundMuted : KlyThemeColor.Transparent);
            var area = new DashboardTapArea(frame) { Description = option.Label, MinimumTarget = 40, IsEnabled = !(option.Disabled || maxReached) };
            area.Opacity = area.IsEnabled ? 1 : 0.4;
            var value = option.Value;
            area.Tapped += (_, _) => _filter.Toggle(value);
            _options.Add(area);
        }
    }
}
