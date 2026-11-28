namespace Klyroo.Dashboard.Maui;

/// <summary>
/// A tappable area made of a real native <see cref="Button"/> (ripple, focus, keyboard and screen reader behavior of the platform) laid over a content that does not take touches.
/// It is how a row or a tab with several texts becomes one button.
/// </summary>
internal sealed class DashboardTapArea : Grid
{
    private readonly KlyButton _button = new() { Block = true, Variant = ButtonVariant.Ghost, Color = KlyColor.Neutral, Padding = new Thickness(0), MinimumHeightRequest = 40 };
    private readonly View _content;

    public DashboardTapArea(View content)
    {
        _content = content;
        content.InputTransparent = true;
        Children.Add(content);
        Children.Add(_button);
        _button.Clicked += (_, _) => Tapped?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Raised when the area is pressed.</summary>
    public event EventHandler? Tapped;

    /// <summary>What a screen reader says for the area.</summary>
    public string? Description
    {
        get => SemanticProperties.GetDescription(_button);
        set => SemanticProperties.SetDescription(_button, value);
    }

    /// <summary>What a screen reader adds after the description.</summary>
    public string? Hint
    {
        get => SemanticProperties.GetHint(_button);
        set => SemanticProperties.SetHint(_button, value);
    }

    /// <summary>The smallest height of the touch target.</summary>
    public double MinimumTarget
    {
        get => _button.MinimumHeightRequest;
        set => _button.MinimumHeightRequest = value;
    }

    /// <summary>The native button over the content.</summary>
    public KlyButton Button => _button;

    /// <summary>Presses the area, as a tap does.</summary>
    public void SendTap() => _button.SendClicked();
}
