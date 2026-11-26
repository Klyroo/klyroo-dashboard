using System.Globalization;
using Bunit;
using Klyroo.Dashboard;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Klyroo.Dashboard.Tests;

public sealed class DashboardUiAndEdgeCaseTests : BunitContext
{
    public DashboardUiAndEdgeCaseTests()
    {
        Services.AddKlyrooDashboard();
    }

    [Theory]
    [InlineData(500, DashboardFormatPreset.Compact, "500")]
    [InlineData(1_500, DashboardFormatPreset.Compact, "1.5K")]
    [InlineData(2_400_000, DashboardFormatPreset.Compact, "2.4M")]
    [InlineData(-4.5, DashboardFormatPreset.Delta, "-4.5%")]
    [InlineData(0.0, DashboardFormatPreset.Delta, "0%")]
    [InlineData(-1.0, DashboardFormatPreset.Points, "-1 pt")]
    [InlineData(3.5, DashboardFormatPreset.Points, "+3.5 pts")]
    [InlineData(0.84, DashboardFormatPreset.Ratio, "84%")]
    public void DashboardFormat_formats_positive_negative_zero_and_compact_magnitudes(
        double value, DashboardFormatPreset preset, string expected)
    {
        Assert.Equal(expected, DashboardFormat.Format(value, preset, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void DashboardFormat_ExportCsv_escapes_quotes_delimiters_and_newlines_per_culture()
    {
        var table = new DashboardDataTable(
            [new("label", "Label"), new("val", "Value", Numeric: true)],
            [
                new Dictionary<string, string?> { ["label"] = "Acme, \"Inc\"", ["val"] = "1200" },
                new Dictionary<string, string?> { ["label"] = "Line1\nLine2", ["val"] = "300" }
            ]);

        var enCsv = DashboardFormat.ExportCsv(table, CultureInfo.GetCultureInfo("en-US"));
        Assert.StartsWith("\uFEFF\"Label\",\"Value\"", enCsv);
        Assert.Contains("\"Acme, \"\"Inc\"\"\",1200", enCsv);

        var frCsv = DashboardFormat.ExportCsv(table, CultureInfo.GetCultureInfo("fr-FR"));
        Assert.StartsWith("\uFEFF\"Label\";\"Value\"", frCsv);
        Assert.Contains(";1200", frCsv);

        Assert.Equal("rapport-chiffre-d-affaires-2026.csv", DashboardFormat.ResolveFileName("Rapport Chiffre d'affaires 2026!"));
    }

    [Fact]
    public void DashboardFormat_ExportCsv_quotes_numeric_cells_that_contain_the_delimiter()
    {
        var table = new DashboardDataTable(
            [new("label", "Label"), new("val", "Value", Numeric: true)],
            [new Dictionary<string, string?> { ["label"] = "Revenue", ["val"] = "1,234.5" }]);

        var enCsv = DashboardFormat.ExportCsv(table, CultureInfo.GetCultureInfo("en-US"));
        Assert.EndsWith("\"Revenue\",\"1,234.5\"\r\n", enCsv);

        var frCsv = DashboardFormat.ExportCsv(table, CultureInfo.GetCultureInfo("fr-FR"));
        Assert.EndsWith("\"Revenue\";1,234.5\r\n", frCsv);
    }

    [Fact]
    public void DashboardFormat_ResolveComparisonRange_handles_None_Previous_and_Year()
    {
        var range = (new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 3, 31, 0, 0, 0, TimeSpan.Zero));

        Assert.Null(DashboardFormat.ResolveComparisonRange(range, DashboardComparisonMode.None));

        var prev = DashboardFormat.ResolveComparisonRange(range, DashboardComparisonMode.Previous);
        Assert.NotNull(prev);
        Assert.Equal(new DateTimeOffset(2026, 2, 28, 0, 0, 0, TimeSpan.Zero), prev.Value.End);

        var year = DashboardFormat.ResolveComparisonRange(range, DashboardComparisonMode.Year);
        Assert.NotNull(year);
        Assert.Equal(2025, year.Value.Start.Year);
        Assert.Equal(31, year.Value.End.Day);
    }

    [Fact]
    public async Task DashboardResource_handles_errors_and_recovers_on_retry_with_LastUpdated()
    {
        var shouldFail = true;
        var ctrl = new DashboardController("resilience");
        var res = ctrl.RegisterResource(new DashboardResource<int>(
            "orders",
            _ => shouldFail ? throw new InvalidOperationException("Upstream timeout") : Task.FromResult(99),
            DashboardQueryStage.Essential));

        await ctrl.RefreshAsync();
        Assert.Equal(DashboardResourceState.Error, res.State);
        Assert.Contains("Upstream timeout", res.Error);

        shouldFail = false;
        await res.RefreshAsync();
        Assert.Equal(DashboardResourceState.Ready, res.State);
        Assert.Equal(99, res.Data);
        Assert.NotNull(res.UpdatedAt);
    }

    [Fact]
    public void DashboardFilterControl_single_and_multi_select_and_Disabled_options_behave_cleanly()
    {
        var single = new DashboardFilterControl(
            "env", "Environment",
            items: [new("prod", "Prod"), new("staging", "Staging")],
            defaultValues: ["prod"],
            multiple: false);

        Assert.Equal("prod", single.Value);
        Assert.False(single.Changed);

        single.Toggle("staging");
        Assert.Equal("staging", single.Value);
        Assert.True(single.Changed);

        single.Reset();
        Assert.Equal("prod", single.Value);
        Assert.False(single.Changed);

        var multi = new DashboardFilterControl(
            "regions", "Regions",
            items: [new("eu", "EU"), new("us", "US"), new("apac", "APAC")],
            defaultValues: ["eu"],
            multiple: true);

        multi.Toggle("us");
        Assert.Equal(["eu", "us"], multi.Values);
        multi.Toggle("eu");
        Assert.Equal(["us"], multi.Values);
        multi.Reset();
        Assert.Equal(["eu"], multi.Values);
    }

    [Fact]
    public void KlyDashboardPage_switches_views_via_tab_clicks()
    {
        var ctrl = new DashboardController("tabs");
        ctrl.AddView("overview", "Overview");
        ctrl.AddView("retention", "Retention", badge: "New");

        var cut = Render<KlyDashboardPage>(p => p
            .Add(x => x.Dashboard, ctrl)
            .Add(x => x.Title, "Growth"));

        var tabs = cut.FindAll("[data-slot=dashboard-view-tab]");
        Assert.Equal(2, tabs.Count);
        Assert.Equal("overview", ctrl.CurrentView);

        tabs[1].Click();
        Assert.Equal("retention", ctrl.CurrentView);
    }

    [Fact]
    public void KlyDashboardList_LxDashboardDetails_and_LxDashboardStat_negative_delta_render_properly()
    {
        var list = Render<KlyDashboardList>(p => p
            .Add(x => x.Title, "Top Accounts")
            .Add(x => x.Limit, 2)
            .Add(x => x.Format, DashboardFormatPreset.Compact)
            .Add(x => x.Items, [
                new DashboardListItem("Acme", Value: 120_000),
                new DashboardListItem("Beta", Value: 85_000),
                new DashboardListItem("Gamma", Value: 40_000)
            ]));
        Assert.Contains("Acme", list.Markup);
        Assert.Contains("Beta", list.Markup);
        Assert.DoesNotContain("Gamma", list.Markup);

        var details = Render<KlyDashboardDetails>(p => p
            .Add(x => x.Title, "Cluster Metadata")
            .Add(x => x.Items, [
                new DashboardDetailsItem("region", "Region", "eu-west-1"),
                new DashboardDetailsItem("version", "Version", "0.1.0-preview.1")
            ]));
        Assert.Contains("eu-west-1", details.Markup);
        Assert.Contains("0.1.0-preview.1", details.Markup);

        var stat = Render<KlyDashboardStat>(p => p
            .Add(x => x.Label, "Churn")
            .Add(x => x.Value, 80)
            .Add(x => x.Compare, 100));
        Assert.Contains("-20%", stat.Find("[data-slot=dashboard-stat-delta]").TextContent);
    }
}
