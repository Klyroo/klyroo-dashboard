using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Klyroo.Dashboard.Tests;

public sealed record Order(string Customer, DateOnly Day, decimal? Total, int Lines);

// The table reads its columns from the row type: no dictionary, no key looked up by name.
public sealed class TypedTableTests : BunitContext
{
    private static readonly Order[] Orders =
    [
        new("beta", new DateOnly(2026, 3, 10), 20m, 2),
        new("Alpha", new DateOnly(2026, 1, 5), null, 7),
        new("gamma", new DateOnly(2026, 2, 1), 5.5m, 1),
    ];

    public TypedTableTests()
    {
        Services.AddKlyrooDashboard();
    }

    private IRenderedComponent<KlyDashboardTable<Order>> Table(DashboardTableSort? sort = null, params DashboardTableColumn<Order>[] columns) =>
        Render<KlyDashboardTable<Order>>(p => p
            .Add(x => x.Columns, columns)
            .Add(x => x.Rows, Orders)
            .Add(x => x.Sort, sort));

    private static IReadOnlyList<string> FirstCells(IRenderedComponent<KlyDashboardTable<Order>> cut) =>
        cut.FindAll("[data-slot=dashboard-table-row]").Select(r => r.QuerySelector("[data-slot=dashboard-table-cell]")!.TextContent.Trim()).ToList();

    [Fact]
    public void Blazor_infers_the_row_type_from_the_rows_and_the_columns()
    {
        var cut = Render<KlyDashboardTable<Order>>(p => p.Add(x => x.Rows, Orders));
        Assert.NotNull(cut);
    }

    [Fact]
    public void A_text_column_sorts_ignoring_case_and_a_date_column_sorts_by_value()
    {
        var customer = DashboardTableColumn.For<Order, string>("customer", "Customer", o => o.Customer);
        var day = DashboardTableColumn.For<Order, DateOnly>("day", "Day", o => o.Day);

        Assert.Equal(["Alpha", "beta", "gamma"], FirstCells(Table(new DashboardTableSort("customer", false), customer, day)));
        Assert.Equal(["Alpha", "gamma", "beta"], FirstCells(Table(new DashboardTableSort("day", false), customer, day)));
        Assert.Equal(["beta", "gamma", "Alpha"], FirstCells(Table(new DashboardTableSort("day", true), customer, day)));
    }

    [Fact]
    public void A_number_column_sorts_by_value_and_rows_without_a_value_come_last_in_both_directions()
    {
        var customer = DashboardTableColumn.For<Order, string>("customer", "Customer", o => o.Customer);
        var total = DashboardTableColumn.For<Order, decimal?>("total", "Total", o => o.Total);

        Assert.Equal(["gamma", "beta", "Alpha"], FirstCells(Table(new DashboardTableSort("total", false), customer, total)));
        Assert.Equal(["beta", "gamma", "Alpha"], FirstCells(Table(new DashboardTableSort("total", true), customer, total)));
    }

    [Fact]
    public void A_missing_value_shows_the_placeholder_and_a_number_is_formatted_by_the_column()
    {
        var cut = Table(null,
            DashboardTableColumn.For<Order, string>("customer", "Customer", o => o.Customer),
            DashboardTableColumn.For<Order, decimal?>("total", "Total", o => o.Total, DashboardFormatPreset.Integer));

        var totals = cut.FindAll("[data-slot=dashboard-table-row]").Select(r => r.QuerySelectorAll("[data-slot=dashboard-table-cell]")[1].TextContent.Trim()).ToList();
        Assert.Equal(DashboardFormat.MissingValue, totals[1]);
        Assert.Equal(DashboardFormat.Format(20, DashboardFormatPreset.Integer, null), totals[0]);
        Assert.Equal("number", cut.FindAll("[data-slot=dashboard-table-cell]")[1].GetAttribute("data-type"));
    }

    [Fact]
    public void The_kind_follows_the_type_unless_it_is_given_and_a_number_kind_needs_a_number_type()
    {
        Assert.Equal(DashboardTableColumnKind.Text, DashboardTableColumn.For<Order, string>("a", "A", o => o.Customer).Kind);
        Assert.Equal(DashboardTableColumnKind.Text, DashboardTableColumn.For<Order, DateOnly>("a", "A", o => o.Day).Kind);
        Assert.Equal(DashboardTableColumnKind.Number, DashboardTableColumn.For<Order, int>("a", "A", o => o.Lines).Kind);
        Assert.Equal(DashboardTableColumnKind.Number, DashboardTableColumn.For<Order, decimal?>("a", "A", o => o.Total).Kind);
        Assert.Equal(DashboardTableColumnKind.Text, DashboardTableColumn.For<Order, int>("a", "A", o => o.Lines, kind: DashboardTableColumnKind.Text).Kind);

        var error = Assert.Throws<ArgumentException>(() => DashboardTableColumn.For<Order, string>("a", "A", o => o.Customer, kind: DashboardTableColumnKind.Delta));
        Assert.Contains("\"a\"", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_csv_export_holds_raw_numbers_and_the_text_of_the_other_columns()
    {
        var module = JSInterop.SetupModule("./_content/Klyroo.Dashboard/klyroo-dashboard.js");
        module.SetupVoid("downloadText", _ => true);
        var cut = Render<KlyDashboardTable<Order>>(p => p
            .Add(x => x.Title, "Orders")
            .Add(x => x.Menu, true)
            .Add(x => x.Columns, [
                DashboardTableColumn.For<Order, string>("customer", "Customer", o => o.Customer),
                DashboardTableColumn.For<Order, decimal?>("total", "Total", o => o.Total),
                DashboardTableColumn.For<Order, int>("lines", "Lines", o => o.Lines)])
            .Add(x => x.Rows, Orders));

        cut.Find("[data-slot=dashboard-card-csv]").Click();

        var csv = (string)module.Invocations["downloadText"][0].Arguments[1]!;
        Assert.Contains("\"beta\",20,2\r\n", csv);
        Assert.Contains("\"Alpha\",,7\r\n", csv);
    }
}
