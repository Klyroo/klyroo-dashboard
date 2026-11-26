using Bunit;
using Microsoft.AspNetCore.Components;
using Klyroo.UI;
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

    private sealed record Mixed(object? Value);

    [Fact]
    public void A_column_of_objects_with_different_runtime_types_sorts_by_its_display_text_instead_of_throwing()
    {
        Mixed[] rows = [new(42), new("beta"), new(new DateOnly(2026, 1, 5)), new("Alpha"), new(null)];
        var column = DashboardTableColumn.For<Mixed, object?>("v", "Value", m => m.Value);

        var cut = Render<KlyDashboardTable<Mixed>>(p => p
            .Add(x => x.Columns, [column])
            .Add(x => x.Rows, rows)
            .Add(x => x.Sort, new DashboardTableSort("v", false)));

        var cells = cut.FindAll("[data-slot=dashboard-table-cell]").Select(c => c.TextContent.Trim()).ToList();
        Assert.Equal(5, cells.Count);
        Assert.Equal(DashboardFormat.MissingValue, cells[4]);
        Assert.Equal(cells.Take(4).Order(StringComparer.OrdinalIgnoreCase).ToList(), cells.Take(4).ToList());
    }

    [Fact]
    public void Comparable_values_of_one_runtime_type_in_an_object_column_still_sort_by_value()
    {
        Mixed[] rows = [new(10), new(9), new(100)];
        var column = DashboardTableColumn.For<Mixed, object?>("v", "Value", m => m.Value);

        var cut = Render<KlyDashboardTable<Mixed>>(p => p
            .Add(x => x.Columns, [column])
            .Add(x => x.Rows, rows)
            .Add(x => x.Sort, new DashboardTableSort("v", false)));

        Assert.Equal(["9", "10", "100"], cut.FindAll("[data-slot=dashboard-table-cell]").Select(c => c.TextContent.Trim()).ToList());
    }

    private IRenderedComponent<CascadingValue<KlyLocale>> InLocale(KlyLocale locale, Action<ComponentParameterCollectionBuilder<KlyDashboardTable<Order>>> parameters) =>
        Render<CascadingValue<KlyLocale>>(p => p.Add(x => x.Value, locale).AddChildContent<KlyDashboardTable<Order>>(parameters));

    [Fact]
    public void Dates_and_numbers_shown_as_text_follow_the_culture_of_the_page()
    {
        var day = DashboardTableColumn.For<Order, DateOnly>("day", "Day", o => o.Day);
        var total = DashboardTableColumn.For<Order, decimal?>("total", "Total", o => o.Total, kind: DashboardTableColumnKind.Text);
        Order[] one = [new("a", new DateOnly(2026, 3, 4), 1234.5m, 1)];

        var fr = InLocale(KlyLocales.Fr, p => p.Add(x => x.Columns, [day, total]).Add(x => x.Rows, one));
        var en = InLocale(KlyLocales.En, p => p.Add(x => x.Columns, [day, total]).Add(x => x.Rows, one));

        var frCells = fr.FindAll("[data-slot=dashboard-table-cell]").Select(c => c.TextContent.Trim()).ToList();
        var enCells = en.FindAll("[data-slot=dashboard-table-cell]").Select(c => c.TextContent.Trim()).ToList();
        Assert.Equal("04/03/2026", frCells[0]);
        Assert.Equal("1234,5", frCells[1]);
        Assert.Equal("3/4/2026", enCells[0]);
        Assert.Equal("1234.5", enCells[1]);
    }

    [Fact]
    public void The_csv_export_writes_dates_in_ISO_form_and_numbers_shown_as_text_invariantly_whatever_the_culture()
    {
        var module = JSInterop.SetupModule("./_content/Klyroo.Dashboard/klyroo-dashboard.js");
        module.SetupVoid("downloadText", _ => true);
        Order[] one = [new("a", new DateOnly(2026, 3, 4), 1234.5m, 1)];
        var cut = InLocale(KlyLocales.Fr, p => p
            .Add(x => x.Title, "Orders")
            .Add(x => x.Menu, true)
            .Add(x => x.Columns, [
                DashboardTableColumn.For<Order, DateOnly>("day", "Day", o => o.Day),
                DashboardTableColumn.For<Order, decimal?>("total", "Total", o => o.Total, kind: DashboardTableColumnKind.Text)])
            .Add(x => x.Rows, one));

        cut.Find("[data-slot=dashboard-card-csv]").Click();

        var csv = (string)module.Invocations["downloadText"][0].Arguments[1]!;
        Assert.Contains("2026-03-04", csv, StringComparison.Ordinal);
        Assert.Contains("1234.5", csv, StringComparison.Ordinal);
        Assert.DoesNotContain("04/03/2026", csv, StringComparison.Ordinal);
    }
}
