using System.Globalization;
using Klyroo.Dashboard;
using Xunit;

namespace Klyroo.Dashboard.Tests;

public sealed class ComprehensiveDashboardTests
{
    [Fact]
    public async Task DashboardController_executes_Essential_then_Background_and_keeps_Deferred_idle()
    {
        var order = new List<string>();
        var ctrl = new DashboardController("sales");

        var essential = ctrl.RegisterResource(new DashboardResource<int>(
            "kpi", async _ => { await Task.Delay(10); order.Add("essential"); return 100; }, DashboardQueryStage.Essential));
        var background = ctrl.RegisterResource(new DashboardResource<string>(
            "chart", _ => { order.Add("background"); return Task.FromResult("ok"); }, DashboardQueryStage.Background));
        var deferred = ctrl.RegisterResource(new DashboardResource<string>(
            "audit", _ => { order.Add("deferred"); return Task.FromResult("audit"); }, DashboardQueryStage.Deferred));

        await ctrl.RefreshAsync();

        Assert.Equal(["essential", "background"], order);
        Assert.Equal(DashboardResourceState.Ready, essential.State);
        Assert.Equal(DashboardResourceState.Ready, background.State);
        Assert.Equal(DashboardResourceState.Idle, deferred.State);

        await deferred.ActivateAsync();
        Assert.Equal(DashboardResourceState.Ready, deferred.State);
        Assert.Equal(["essential", "background", "deferred"], order);
    }

    [Fact]
    public void DashboardFilterControl_and_Views_track_Filtered_state_and_reset_cleanly()
    {
        var ctrl = new DashboardController("ops");
        ctrl.AddView("overview", "Overview", badge: "2");
        ctrl.AddView("cohorts", "Cohorts");
        Assert.Equal("overview", ctrl.CurrentView);
        ctrl.CurrentView = "cohorts";
        Assert.Equal("cohorts", ctrl.CurrentView);

        var region = ctrl.AddFilter(new DashboardFilterControl(
            "region", "Region",
            items: [new("eu", "Europe"), new("us", "US")],
            defaultValues: ["eu"],
            multiple: true));

        Assert.False(ctrl.Filtered);
        region.Toggle("us");
        Assert.True(ctrl.Filtered);
        ctrl.ResetFilters();
        Assert.False(ctrl.Filtered);
        Assert.Equal(["eu"], region.Values);
    }

    [Fact]
    public void DashboardFormat_handles_presets_NaN_leap_years_and_localized_BOM_CSV()
    {
        Assert.Equal("2.4M", DashboardFormat.Format(2_400_000, DashboardFormatPreset.Compact, CultureInfo.InvariantCulture));
        Assert.Equal("+14.2%", DashboardFormat.Format(14.2, DashboardFormatPreset.Delta, CultureInfo.InvariantCulture));
        Assert.Equal("—", DashboardFormat.Format(double.NaN, DashboardFormatPreset.Number, CultureInfo.InvariantCulture));

        var leapRange = (new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2024, 2, 29, 0, 0, 0, TimeSpan.Zero));
        var yearAgo = DashboardFormat.ResolveComparisonRange(leapRange, DashboardComparisonMode.Year);
        Assert.Equal(new DateTimeOffset(2023, 2, 28, 0, 0, 0, TimeSpan.Zero), yearAgo!.Value.End);

        var table = new DashboardDataTable(
            [new("region", "Region"), new("mrr", "MRR", Numeric: true)],
            [new Dictionary<string, string?> { ["region"] = "Europe", ["mrr"] = "148900" }]);

        var frCsv = DashboardFormat.ExportCsv(table, CultureInfo.GetCultureInfo("fr-FR"));
        Assert.StartsWith("\uFEFF", frCsv);
        Assert.Contains(";", frCsv);
        Assert.Equal("consommation-mensuelle.csv", DashboardFormat.ResolveFileName("Consommation mensuelle"));
    }
}
