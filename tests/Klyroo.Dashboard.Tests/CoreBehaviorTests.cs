using System.Globalization;
using Xunit;

namespace Klyroo.Dashboard.Tests;

public sealed class CoreBehaviorTests
{
    [Theory]
    [InlineData(999_999, "1M")]
    [InlineData(-999_999, "-1M")]
    [InlineData(999_949, "999.9K")]
    [InlineData(999.96, "1K")]
    [InlineData(999_960_000, "1B")]
    [InlineData(12_600, "12.6K")]
    public void FormatCompact_chooses_the_suffix_after_rounding(double value, string expected)
    {
        Assert.Equal(expected, DashboardFormat.FormatCompact(value, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void FormatInvariant_writes_every_digit_in_the_invariant_culture()
    {
        Assert.Equal("12600", DashboardFormat.FormatInvariant(12_600));
        Assert.Equal("-4.25", DashboardFormat.FormatInvariant(-4.25));
        Assert.Null(DashboardFormat.FormatInvariant(null));
        Assert.Null(DashboardFormat.FormatInvariant(double.NaN));
    }

    [Fact]
    public void ExportCsv_neutralizes_formulas_and_ends_the_header_with_crlf()
    {
        var table = new DashboardDataTable(
            [new("label", "=Label"), new("val", "Value", Numeric: true)],
            [
                new Dictionary<string, string?> { ["label"] = "=SUM(A1:A9)", ["val"] = "-4.5" },
                new Dictionary<string, string?> { ["label"] = "+cmd", ["val"] = "=1+1" },
                new Dictionary<string, string?> { ["label"] = "-x", ["val"] = "12" },
                new Dictionary<string, string?> { ["label"] = "@A1", ["val"] = null },
                new Dictionary<string, string?> { ["label"] = "\tTab", ["val"] = null },
                new Dictionary<string, string?> { ["label"] = "\rCr", ["val"] = null },
            ]);

        var csv = DashboardFormat.ExportCsv(table, CultureInfo.InvariantCulture);
        var lines = csv.TrimStart('﻿').Split("\r\n");

        Assert.StartsWith("﻿\"'=Label\",\"Value\"\r\n", csv);
        Assert.Equal("\"'=SUM(A1:A9)\",-4.5", lines[1]);
        Assert.Equal("\"'+cmd\",\"'=1+1\"", lines[2]);
        Assert.Equal("\"'-x\",12", lines[3]);
        Assert.Equal("\"'@A1\",", lines[4]);
        Assert.Equal("\"'\tTab\",", lines[5]);
        Assert.Equal("\"'\rCr\",", lines[6]);
        Assert.DoesNotContain("\n", csv.Replace("\r\n", "", StringComparison.Ordinal));
    }

    [Fact]
    public void ApplyPreset_keeps_one_value_in_single_mode_two_for_a_range_and_respects_Max()
    {
        var preset = new DashboardFilterPreset("Top", ["a", "b", "c"]);

        var single = new DashboardFilterControl("s", "Single", items: [new("a", "A"), new("b", "B"), new("c", "C")]);
        single.ApplyPreset(preset);
        Assert.Equal(["a"], single.Values);

        var multi = new DashboardFilterControl("m", "Multi", multiple: true, max: 2);
        multi.ApplyPreset(preset);
        Assert.Equal(["a", "b"], multi.Values);

        var range = new DashboardFilterControl("r", "Period", DashboardFilterKind.DateRange);
        range.ApplyPreset(new DashboardFilterPreset("March", ["2026-03-01", "2026-03-31", "ignored"]));
        Assert.Equal(["2026-03-01", "2026-03-31"], range.Values);
        Assert.Equal("2026-03-01 – 2026-03-31", range.Display);
    }

    [Fact]
    public void Boolean_and_Comparison_filters_get_built_in_options()
    {
        var flag = new DashboardFilterControl("flag", "Active", DashboardFilterKind.Boolean);
        flag.Toggle("true");
        Assert.Equal("Yes", flag.Display);

        var comparison = new DashboardFilterControl("cmp", "Compare", DashboardFilterKind.Comparison, defaultValues: ["previous"]);
        Assert.Equal(["none", "previous", "year"], comparison.Items.Select(i => i.Value));
        Assert.True(Enum.TryParse<DashboardComparisonMode>(comparison.Value, ignoreCase: true, out var mode));
        Assert.Equal(DashboardComparisonMode.Previous, mode);
    }

    [Fact]
    public async Task A_resource_disabled_at_first_loads_on_activation_once_enabled()
    {
        var enabled = false;
        var calls = 0;
        var resource = new DashboardResource<int>(
            "late",
            _ => { calls++; return Task.FromResult(7); },
            DashboardQueryStage.Deferred,
            enabled: () => enabled);

        await resource.ActivateAsync();
        Assert.Equal(DashboardResourceState.Disabled, resource.State);
        Assert.Equal(0, calls);

        enabled = true;
        await resource.ActivateAsync();

        Assert.Equal(DashboardResourceState.Ready, resource.State);
        Assert.Equal(7, resource.Data);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task A_disabled_resource_does_not_raise_Changed_again_when_it_stays_disabled()
    {
        var resource = new DashboardResource<int>("off", _ => Task.FromResult(1), enabled: () => false);
        var changes = 0;
        resource.Changed += () => changes++;

        await resource.ActivateAsync();
        await resource.RefreshAsync();

        Assert.Equal(0, changes);
    }

    [Fact]
    public async Task A_newer_refresh_cancels_the_running_load_and_wins()
    {
        var first = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var tokens = new List<CancellationToken>();
        var call = 0;
        var resource = new DashboardResource<string>("orders", ct =>
        {
            tokens.Add(ct);
            return ++call == 1 ? first.Task : Task.FromResult("new");
        });

        var slow = resource.RefreshAsync();
        Assert.True(tokens[0].CanBeCanceled);
        await resource.RefreshAsync();

        Assert.True(tokens[0].IsCancellationRequested);
        Assert.Equal("new", resource.Data);

        first.SetResult("old");
        await slow;

        Assert.Equal("new", resource.Data);
        Assert.Equal(DashboardResourceState.Ready, resource.State);
        Assert.False(resource.Fetching);
    }

    [Fact]
    public async Task Disposing_the_controller_cancels_loads_and_stops_refreshing()
    {
        var pending = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken token = default;
        var controller = new DashboardController("ops");
        var resource = controller.RegisterResource(new DashboardResource<int>("slow", ct =>
        {
            token = ct;
            return pending.Task;
        }));

        var refresh = controller.RefreshAsync();
        controller.Dispose();

        Assert.True(token.IsCancellationRequested);
        pending.SetResult(5);
        await refresh;
        Assert.Equal(0, resource.Data);

        await controller.RefreshAsync();
        Assert.Equal(0, resource.Data);
    }

    [Fact]
    public void View_and_filter_setters_raise_the_controller_Changed()
    {
        var controller = new DashboardController("ops");
        var overview = controller.AddView("overview", "Overview");
        var filter = controller.AddFilter(new DashboardFilterControl("region", "Region"));
        var changes = 0;
        controller.Changed += () => changes++;

        overview.Badge = "4";
        Assert.Equal(1, changes);
        overview.Badge = "4";
        Assert.Equal(1, changes);

        filter.Items = [new("emea", "EMEA")];
        Assert.Equal(2, changes);

        filter.Enabled = false;
        Assert.Equal(3, changes);

        overview.Enabled = false;
        Assert.Equal(4, changes);
    }

    [Fact]
    public void Disabling_a_filter_with_a_selection_raises_ChangedEvent()
    {
        var filter = new DashboardFilterControl("region", "Region", items: [new("emea", "EMEA")]);
        filter.Toggle("emea");
        var selectionChanges = 0;
        filter.ChangedEvent += () => selectionChanges++;

        filter.Enabled = false;

        Assert.Equal(1, selectionChanges);
        Assert.Empty(filter.Values);
    }

    [Fact]
    public void Disabling_the_current_view_falls_back_to_the_next_enabled_one()
    {
        var controller = new DashboardController("ops");
        var overview = controller.AddView("overview", "Overview");
        var details = controller.AddView("details", "Details");
        var audit = controller.AddView("audit", "Audit", enabled: false);

        overview.Enabled = false;
        Assert.Equal("details", controller.CurrentView);
        Assert.True(details.Opened);

        details.Enabled = false;
        Assert.Null(controller.CurrentView);

        audit.Enabled = true;
        Assert.Equal("audit", controller.CurrentView);
        Assert.True(audit.Opened);
    }

    [Fact]
    public async Task AutoRefresh_refreshes_on_each_tick_and_stops_when_cleared_or_disposed()
    {
        var clock = new ManualTimeProvider();
        var controller = new DashboardController("live", clock);
        var calls = 0;
        var loaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        controller.RegisterResource(new DashboardResource<int>("orders", _ =>
        {
            Interlocked.Increment(ref calls);
            loaded.TrySetResult();
            return Task.FromResult(1);
        }));

        controller.AutoRefresh = 30;
        var timer = Assert.Single(clock.Timers);
        Assert.Equal(TimeSpan.FromSeconds(30), timer.Period);

        timer.Fire();
        await loaded.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(1, Volatile.Read(ref calls));

        controller.AutoRefresh = 0;
        await timer.Disposed.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        controller.AutoRefresh = 10;
        var second = clock.Timers[1];
        controller.Dispose();
        await second.Disposed.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Throws<ObjectDisposedException>(() => controller.AutoRefresh = 5);
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private readonly List<ManualTimer> _timers = [];

        public IReadOnlyList<ManualTimer> Timers
        {
            get
            {
                lock (_timers)
                {
                    return _timers.ToList();
                }
            }
        }

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(callback, state, period);
            lock (_timers)
            {
                _timers.Add(timer);
            }
            return timer;
        }
    }

    private sealed class ManualTimer(TimerCallback callback, object? state, TimeSpan period) : ITimer
    {
        private readonly TaskCompletionSource _disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TimeSpan Period { get; private set; } = period;
        public Task Disposed => _disposed.Task;

        public void Fire() => callback(state);

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            Period = period;
            return true;
        }

        public void Dispose() => _disposed.TrySetResult();

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
