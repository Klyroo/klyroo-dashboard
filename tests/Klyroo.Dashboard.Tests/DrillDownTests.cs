using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Klyroo.Dashboard.Tests;

// Drill-down: a block hands the picked bar, row or point to the app, through controls the keyboard reaches.
public sealed class DrillDownTests : BunitContext
{
    public DrillDownTests()
    {
        Services.AddKlyrooDashboard();
    }

    [Fact]
    public void Bars_without_OnSelect_are_plain_rows()
    {
        var cut = Render<KlyDashboardBars>(p => p.Add(x => x.Items, [new DashboardBarItem("EMEA", 3)]));
        Assert.Equal("DIV", cut.Find("[data-slot=dashboard-bar-row]").TagName);
        Assert.Empty(cut.FindAll("button[data-slot=dashboard-bar-row]"));
    }

    [Fact]
    public void Each_bar_is_a_button_that_reports_its_item_and_exposes_its_selected_state()
    {
        var picked = new List<DashboardBarItem>();
        var emea = new DashboardBarItem("EMEA", 3, Selected: true);
        var amer = new DashboardBarItem("AMER", 2);
        var cut = Render<KlyDashboardBars>(p => p
            .Add(x => x.Items, [emea, amer])
            .Add(x => x.OnSelect, (DashboardBarItem i) => picked.Add(i)));

        var rows = cut.FindAll("button[data-slot=dashboard-bar-row]");
        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.Equal("button", r.GetAttribute("type")));
        Assert.Equal("true", rows[0].GetAttribute("aria-pressed"));
        Assert.Equal("false", rows[1].GetAttribute("aria-pressed"));
        // Still the same content and fill inside the button.
        Assert.Equal("EMEA", rows[0].QuerySelector(".truncate")!.TextContent);
        Assert.NotNull(rows[0].QuerySelector("[data-slot=dashboard-bar-fill]"));

        rows[1].Click();
        Assert.Same(amer, Assert.Single(picked));
    }

    [Fact]
    public void A_limited_bars_block_only_has_buttons_for_the_bars_it_shows()
    {
        var cut = Render<KlyDashboardBars>(p => p
            .Add(x => x.Limit, 1)
            .Add(x => x.Items, [new DashboardBarItem("A", 3), new DashboardBarItem("B", 2)])
            .Add(x => x.OnSelect, (DashboardBarItem _) => { }));
        Assert.Single(cut.FindAll("button[data-slot=dashboard-bar-row]"));
    }

    [Fact]
    public void A_table_without_OnRowClick_has_no_buttons_in_its_rows()
    {
        var cut = Render<KlyDashboardTable<RegionRow>>(p => p
            .Add(x => x.Columns, [DashboardTableColumn.For<RegionRow, string>("name", "Name", r => r.Name)])
            .Add(x => x.Rows, [new RegionRow("EMEA", 0)]));
        Assert.Empty(cut.FindAll("[data-slot=dashboard-table-row-button]"));
    }

    [Fact]
    public void A_table_row_is_picked_with_the_button_of_its_first_cell_or_a_click_on_the_row_once()
    {
        var picked = new List<RegionRow>();
        var emea = new RegionRow("EMEA", 10.0);
        var amer = new RegionRow("AMER", 20.0);
        var cut = Render<KlyDashboardTable<RegionRow>>(p => p
            .Add(x => x.Columns, [
                DashboardTableColumn.For<RegionRow, string>("name", "Name", r => r.Name, sortable: false),
                DashboardTableColumn.For<RegionRow, double>("revenue", "Revenue", r => r.Revenue, sortable: false)])
            .Add(x => x.Rows, [emea, amer])
            .Add(x => x.OnRowClick, (RegionRow r) => picked.Add(r)));

        var buttons = cut.FindAll("[data-slot=dashboard-table-row-button]");
        // One button per row, in the first cell only.
        Assert.Equal(2, buttons.Count);
        Assert.Equal("button", buttons[0].GetAttribute("type"));
        Assert.Equal("EMEA", buttons[0].TextContent.Trim());

        buttons[1].Click();
        Assert.Same(amer, Assert.Single(picked));

        cut.FindAll("[data-slot=dashboard-table-row]")[0].Click();
        Assert.Equal(2, picked.Count);
        Assert.Same(emea, picked[1]);
    }

    [Fact]
    public void A_sorted_table_reports_the_row_that_was_clicked_not_its_position()
    {
        var picked = new List<RegionRow>();
        var cut = Render<KlyDashboardTable<RegionRow>>(p => p
            .Add(x => x.Columns, [
                DashboardTableColumn.For<RegionRow, string>("name", "Name", r => r.Name),
                DashboardTableColumn.For<RegionRow, double>("revenue", "Revenue", r => r.Revenue)])
            .Add(x => x.Rows, [new RegionRow("EMEA", 10.0), new RegionRow("AMER", 20.0)])
            .Add(x => x.OnRowClick, (RegionRow r) => picked.Add(r)));
        cut.FindAll("[data-slot=dashboard-table-sort]")[1].Click();

        cut.FindAll("[data-slot=dashboard-table-row-button]")[0].Click();
        Assert.Equal("AMER", Assert.Single(picked).Name);
    }

    [Fact]
    public void A_list_row_without_a_link_is_picked_with_its_label_button_or_a_click_on_the_row_once()
    {
        var picked = new List<DashboardListItem>();
        var anna = new DashboardListItem("Anna", Selected: true);
        var bob = new DashboardListItem("Bob");
        var linked = new DashboardListItem("Carol", Href: "/people/carol");
        var cut = Render<KlyDashboardList>(p => p
            .Add(x => x.Items, [anna, bob, linked])
            .Add(x => x.OnRowClick, (DashboardListItem i) => picked.Add(i)));

        var buttons = cut.FindAll("[data-slot=dashboard-list-row-button]");
        Assert.Equal(2, buttons.Count);
        Assert.Equal("true", buttons[0].GetAttribute("aria-pressed"));
        Assert.Equal("false", buttons[1].GetAttribute("aria-pressed"));

        buttons[1].Click();
        Assert.Same(bob, Assert.Single(picked));
        cut.FindAll("[data-slot=dashboard-list-row]")[0].Click();
        Assert.Same(anna, picked[1]);

        // A row with a link keeps the link and raises nothing.
        Assert.Equal("/people/carol", cut.Find("a").GetAttribute("href"));
        Assert.Throws<MissingEventHandlerException>(() => cut.FindAll("[data-slot=dashboard-list-row]")[2].Click());
        Assert.Equal(2, picked.Count);
    }

    [Fact]
    public void A_list_without_OnRowClick_has_no_buttons()
    {
        var cut = Render<KlyDashboardList>(p => p.Add(x => x.Items, [new DashboardListItem("Anna")]));
        Assert.Empty(cut.FindAll("[data-slot=dashboard-list-row-button]"));
    }
}
