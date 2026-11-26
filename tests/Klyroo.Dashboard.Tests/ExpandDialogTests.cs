using Bunit;
using Klyroo.UI;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Klyroo.Dashboard.Tests;

// Expand opens the block in the modal of Klyroo.UI: a native dialog, so the page is inert, the focus is kept inside,
// Escape and the backdrop close it, and the focus returns to the button.
public sealed class ExpandDialogTests : BunitContext
{
    public ExpandDialogTests()
    {
        Services.AddKlyrooDashboard();
    }

    private IRenderedComponent<KlyDashboardBars> Bars(Action<ComponentParameterCollectionBuilder<KlyDashboardBars>>? more = null) =>
        Render<KlyDashboardBars>(p =>
        {
            p.Add(x => x.Title, "Regions").Add(x => x.Subtitle, "Last 30 days").Add(x => x.Menu, true)
             .Add(x => x.Items, [new DashboardBarItem("EMEA", 3)]);
            more?.Invoke(p);
        });

    [Fact]
    public void The_dialog_is_closed_and_empty_until_Expand_is_clicked()
    {
        var cut = Bars();
        var modal = cut.Find("kly-modal");

        Assert.False(modal.HasAttribute("open"));
        Assert.Empty(cut.FindAll("dialog [data-slot=dashboard-bars]"));
        Assert.Single(cut.FindAll("[data-slot=dashboard-bars]"));
        var expand = cut.Find("[data-slot=dashboard-card-expand]");
        Assert.Equal("dialog", expand.GetAttribute("aria-haspopup"));
        Assert.Equal("false", expand.GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Expand_opens_a_named_modal_dialog_with_the_content_drawn_once()
    {
        var cut = Bars();
        cut.Find("[data-slot=dashboard-card-expand]").Click();

        Assert.True(cut.Find("kly-modal").HasAttribute("open"));
        Assert.Equal("true", cut.Find("[data-slot=dashboard-card-expand]").GetAttribute("aria-expanded"));
        // The content is in the dialog, not in the card behind it: one copy only.
        Assert.Single(cut.FindAll("[data-slot=dashboard-bars]"));
        Assert.Single(cut.FindAll("dialog [data-slot=dashboard-bars]"));
        Assert.Empty(cut.FindAll("[data-slot=dashboard-card-body] [data-slot=dashboard-bars]"));

        var dialog = cut.Find("dialog");
        var labelledBy = dialog.GetAttribute("aria-labelledby")!;
        Assert.Equal("Regions", cut.Find($"#{labelledBy}").TextContent);
        Assert.Contains("Last 30 days", dialog.TextContent);
        // Escape, the close button and the backdrop are the behavior of the modal: the close button is there and localized.
        Assert.Equal(KlyLocales.En.Messages.Close, cut.Find("dialog [data-kly-close]").GetAttribute("aria-label"));
    }

    [Fact]
    public void A_dialog_without_a_title_is_still_named()
    {
        var cut = Render<KlyDashboardBars>(p => p
            .Add(x => x.Menu, true)
            .Add(x => x.Items, [new DashboardBarItem("EMEA", 3)]));
        Assert.Equal("Expand", cut.Find("dialog").GetAttribute("aria-label"));
    }

    [Fact]
    public void Closing_the_dialog_by_any_way_puts_the_content_back_in_the_card()
    {
        var cut = Bars();
        cut.Find("[data-slot=dashboard-card-expand]").Click();

        // The element reports every close (Escape, the close button, a click on the backdrop) with the same event.
        cut.Find("kly-modal").TriggerEvent("onklytoggle", new KlyToggleEventArgs { Open = false, Sequence = 2 });

        Assert.False(cut.Find("kly-modal").HasAttribute("open"));
        Assert.Equal("false", cut.Find("[data-slot=dashboard-card-expand]").GetAttribute("aria-expanded"));
        Assert.Single(cut.FindAll("[data-slot=dashboard-card-body] [data-slot=dashboard-bars]"));
        Assert.Empty(cut.FindAll("dialog [data-slot=dashboard-bars]"));
    }

    [Fact]
    public void The_Collapse_button_behind_the_dialog_closes_it_too()
    {
        var cut = Bars();
        cut.Find("[data-slot=dashboard-card-expand]").Click();
        Assert.Equal("Collapse", cut.Find("[data-slot=dashboard-card-expand]").TextContent.Trim());

        cut.Find("[data-slot=dashboard-card-expand]").Click();
        Assert.False(cut.Find("kly-modal").HasAttribute("open"));
    }

    [Fact]
    public void The_card_no_longer_becomes_a_fixed_div()
    {
        var cut = Bars();
        cut.Find("[data-slot=dashboard-card-expand]").Click();
        Assert.DoesNotContain("fixed", cut.Find("[data-slot=dashboard-card]").ClassName);
    }

    [Fact]
    public void There_is_no_dialog_without_the_menu_or_when_the_block_is_not_expandable()
    {
        Assert.Empty(Render<KlyDashboardBars>(p => p.Add(x => x.Items, [new DashboardBarItem("A", 1)])).FindAll("kly-modal"));
        var stat = Render<KlyDashboardStat>(p => p.Add(x => x.Label, "Revenue").Add(x => x.Value, 5.0).Add(x => x.Menu, true));
        Assert.Empty(stat.FindAll("kly-modal"));
        Assert.Empty(stat.FindAll("[data-slot=dashboard-card-expand]"));
        var card = Render<KlyDashboardCard>(p => p.Add(x => x.Menu, true).Add(x => x.Expandable, false).AddChildContent("<p>x</p>"));
        Assert.Empty(card.FindAll("kly-modal"));
    }

    [Fact]
    public void An_empty_block_has_no_Expand_button()
    {
        var cut = Render<KlyDashboardBars>(p => p.Add(x => x.Title, "Regions").Add(x => x.Menu, true).Add(x => x.Items, []));
        Assert.Empty(cut.FindAll("[data-slot=dashboard-card-expand]"));
        Assert.False(cut.Find("kly-modal").HasAttribute("open"));
    }
}
