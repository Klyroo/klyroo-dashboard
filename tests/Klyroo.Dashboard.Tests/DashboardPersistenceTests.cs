using System.Text.Json.Serialization;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Klyroo.Dashboard.Tests;

public sealed record PersistedSummary(int Orders, double Revenue);

[JsonSerializable(typeof(PersistedSummary))]
public sealed partial class PersistenceTestJson : JsonSerializerContext;

// A resource created with a JsonTypeInfo writes its loaded data as text and takes it back, DashboardController.LoadAsync keeps restored data,
// and DashboardPersistence hands that text from the prerender to the interactive render through the persistent component state.
public sealed class DashboardPersistenceTests : BunitContext
{
    private const string Restorable = """{"Orders":3,"Revenue":1}""";
    private static readonly DateTimeOffset LoadedAt = new(2026, 11, 26, 9, 30, 0, TimeSpan.Zero);

    private int _loads;

    private DashboardResource<PersistedSummary> Summary(string key = "summary", bool json = true, Func<bool>? enabled = null, Func<bool>? requires = null) =>
        new(key, _ =>
        {
            _loads++;
            return Task.FromResult(new PersistedSummary(12, 4.5));
        }, enabled: enabled, requires: requires, json: json ? PersistenceTestJson.Default.PersistedSummary : null);

    [Fact]
    public async Task A_resource_exports_its_data_only_once_loaded_and_only_with_a_type_info()
    {
        var resource = Summary();
        Assert.Null(resource.ExportJson());

        await resource.RefreshAsync();

        Assert.Equal("""{"Orders":12,"Revenue":4.5}""", resource.ExportJson());
        var plain = Summary(json: false);
        await plain.RefreshAsync();
        Assert.Null(plain.ExportJson());
    }

    [Fact]
    public async Task LoadAsync_keeps_restored_data_and_RefreshAsync_always_loads()
    {
        var controller = new DashboardController("orders");
        var resource = controller.RegisterResource(Summary());

        Assert.True(resource.RestoreJson(Restorable, LoadedAt));
        await controller.LoadAsync();

        Assert.Equal(0, _loads);
        Assert.True(resource.Restored);
        Assert.Equal(new PersistedSummary(3, 1), resource.Data);
        Assert.Equal(LoadedAt, resource.UpdatedAt);

        await controller.RefreshAsync();

        Assert.Equal(1, _loads);
        Assert.False(resource.Restored);
        Assert.Equal(12, resource.Data!.Orders);
    }

    [Fact]
    public async Task A_page_that_never_calls_LoadAsync_still_loads_on_its_first_refresh()
    {
        var controller = new DashboardController("orders");
        var resource = controller.RegisterResource(Summary());
        resource.RestoreJson(Restorable, LoadedAt);

        // A filter changed: the user expects the data of the new filter.
        await controller.RefreshAsync();

        Assert.Equal(1, _loads);
        Assert.Equal(12, resource.Data!.Orders);
    }

    [Fact]
    public async Task A_closed_gate_drops_restored_data_and_the_next_refresh_loads()
    {
        var ready = true;
        var controller = new DashboardController("orders");
        var resource = controller.RegisterResource(Summary(requires: () => ready));
        Assert.True(resource.RestoreJson(Restorable, LoadedAt));

        ready = false;
        await controller.LoadAsync();
        Assert.False(resource.Restored);
        Assert.Null(resource.Data);

        ready = true;
        await controller.RefreshAsync();
        Assert.Equal(1, _loads);
        Assert.Equal(12, resource.Data!.Orders);
    }

    [Fact]
    public async Task A_disabled_resource_drops_restored_data_and_loads_when_enabled_again()
    {
        var enabled = true;
        var resource = Summary(enabled: () => enabled);
        Assert.True(resource.RestoreJson(Restorable, LoadedAt));

        enabled = false;
        await resource.ActivateAsync();
        Assert.Equal(DashboardResourceState.Disabled, resource.State);

        enabled = true;
        await resource.ActivateAsync();
        Assert.Equal(1, _loads);
        Assert.Equal(DashboardResourceState.Ready, resource.State);
    }

    [Fact]
    public async Task A_resource_refuses_text_it_cannot_take_and_then_loads_normally()
    {
        var resource = Summary();
        Assert.False(resource.RestoreJson("not json", LoadedAt));
        Assert.False(Summary(json: false).RestoreJson(Restorable, LoadedAt));
        Assert.False(Summary(enabled: () => false).RestoreJson(Restorable, LoadedAt));
        Assert.False(Summary(requires: () => false).RestoreJson(Restorable, LoadedAt));

        await resource.RefreshAsync();

        Assert.Equal(1, _loads);
        Assert.Equal(DashboardResourceState.Ready, resource.State);
    }

    [Fact]
    public async Task The_prerender_hands_the_loaded_data_to_the_interactive_render()
    {
        var state = this.AddBunitPersistentComponentState();
        Services.AddKlyrooDashboard();

        // The prerender loads, then writes what it loaded when the page persists its state.
        var prerender = new DashboardController("orders");
        prerender.RegisterResource(Summary());
        using (Services.GetRequiredService<DashboardPersistence>().Attach(prerender, RenderMode.InteractiveServer))
        {
            await prerender.LoadAsync();
            state.TriggerOnPersisting();
        }
        Assert.Equal(1, _loads);
        Assert.True(state.TryTake<string>("kly-dash:orders:summary", out var text));
        Assert.EndsWith("""{"Orders":12,"Revenue":4.5}""", text);

        // The interactive render takes it back and does not load again.
        state.Persist("kly-dash:orders:summary", text);
        var interactive = new DashboardController("orders");
        var summary = interactive.RegisterResource(Summary());
        using var attached = Services.GetRequiredService<DashboardPersistence>().Attach(interactive, RenderMode.InteractiveServer);
        await interactive.LoadAsync();

        Assert.Equal(1, _loads);
        Assert.Equal(new PersistedSummary(12, 4.5), summary.Data);
        Assert.NotNull(summary.UpdatedAt);
    }

    [Fact]
    public async Task Data_written_under_other_filter_values_is_refused()
    {
        var state = this.AddBunitPersistentComponentState();
        Services.AddKlyrooDashboard();
        var persistence = Services.GetRequiredService<DashboardPersistence>();

        var prerender = new DashboardController("orders");
        prerender.AddFilter(new DashboardFilterControl("region", "Region", items: [new("eu", "Europe"), new("us", "US")]));
        prerender.RegisterResource(Summary());
        using (persistence.Attach(prerender, RenderMode.InteractiveServer))
        {
            await prerender.LoadAsync();
            state.TriggerOnPersisting();
        }
        Assert.True(state.TryTake<string>("kly-dash:orders:summary", out var text));
        state.Persist("kly-dash:orders:summary", text);

        // The interactive render starts from a filter the prerender did not have (read from browser storage, say).
        var interactive = new DashboardController("orders");
        interactive.AddFilter(new DashboardFilterControl("region", "Region", items: [new("eu", "Europe"), new("us", "US")])).Toggle("us");
        var summary = interactive.RegisterResource(Summary());
        using var attached = persistence.Attach(interactive, RenderMode.InteractiveServer);
        await interactive.LoadAsync();

        Assert.False(summary.Restored);
        Assert.Equal(2, _loads);
    }

    [Fact]
    public async Task Data_older_than_the_maximum_age_is_refused()
    {
        var state = this.AddBunitPersistentComponentState();
        Services.AddKlyrooDashboard();
        var filters = new DashboardController("orders").FilterFingerprint();
        state.Persist("kly-dash:orders:summary", DateTimeOffset.UtcNow.AddHours(-2).ToString("O") + "\n" + filters.Length + ":" + filters + Restorable);
        var controller = new DashboardController("orders");
        var summary = controller.RegisterResource(Summary());

        using var attached = Services.GetRequiredService<DashboardPersistence>().Attach(controller, RenderMode.InteractiveServer);
        await controller.LoadAsync();

        Assert.False(summary.Restored);
        Assert.Equal(1, _loads);
    }

    [Fact]
    public async Task A_converter_that_throws_never_escapes_and_spares_the_other_resources()
    {
        var state = this.AddBunitPersistentComponentState();
        Services.AddKlyrooDashboard();
        var options = new System.Text.Json.JsonSerializerOptions { TypeInfoResolver = PersistenceTestJson.Default, Converters = { new ThrowingConverter() } };
        var broken = new DashboardResource<PersistedSummary>("broken", _ => Task.FromResult(new PersistedSummary(1, 1)),
            json: (System.Text.Json.Serialization.Metadata.JsonTypeInfo<PersistedSummary>)options.GetTypeInfo(typeof(PersistedSummary)));
        var controller = new DashboardController("orders");
        controller.RegisterResource(broken);
        controller.RegisterResource(Summary());
        using (Services.GetRequiredService<DashboardPersistence>().Attach(controller, RenderMode.InteractiveServer))
        {
            await controller.LoadAsync();
            state.TriggerOnPersisting();
        }

        Assert.False(state.TryTake<string>("kly-dash:orders:broken", out _));
        Assert.True(state.TryTake<string>("kly-dash:orders:summary", out _));
        Assert.False(broken.RestoreJson(Restorable, LoadedAt));
    }

    private sealed class ThrowingConverter : JsonConverter<PersistedSummary>
    {
        public override PersistedSummary Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options) => throw new FormatException("read");

        public override void Write(System.Text.Json.Utf8JsonWriter writer, PersistedSummary value, System.Text.Json.JsonSerializerOptions options) => throw new FormatException("write");
    }

    [Fact]
    public async Task Without_a_render_mode_nothing_is_handed_over()
    {
        var state = this.AddBunitPersistentComponentState();
        Services.AddKlyrooDashboard();
        var controller = new DashboardController("orders");
        controller.RegisterResource(Summary());
        using var attached = Services.GetRequiredService<DashboardPersistence>().Attach(controller, null);
        await controller.LoadAsync();

        state.TriggerOnPersisting();

        Assert.False(state.TryTake<string>("kly-dash:orders:summary", out _));
    }

    [Fact]
    public async Task The_limit_counts_the_size_as_sent_not_the_raw_text()
    {
        var state = this.AddBunitPersistentComponentState();
        // The raw text is about 80 bytes; escaped and base64-encoded twice it is well over 120.
        Services.AddKlyrooDashboard(o => o.PersistMaxBytes = 120);
        var controller = new DashboardController("orders");
        controller.RegisterResource(Summary());
        using var attached = Services.GetRequiredService<DashboardPersistence>().Attach(controller, RenderMode.InteractiveServer);
        await controller.LoadAsync();

        state.TriggerOnPersisting();

        Assert.False(state.TryTake<string>("kly-dash:orders:summary", out _));
    }

    [Fact]
    public void Two_dashboards_with_one_key_cannot_be_attached_at_once()
    {
        this.AddBunitPersistentComponentState();
        Services.AddKlyrooDashboard();
        var persistence = Services.GetRequiredService<DashboardPersistence>();

        var first = persistence.Attach(new DashboardController("orders"), RenderMode.InteractiveServer);
        Assert.Throws<InvalidOperationException>(() => persistence.Attach(new DashboardController("orders"), RenderMode.InteractiveServer));

        // Once the first page is gone, the key is free again.
        first.Dispose();
        persistence.Attach(new DashboardController("orders"), RenderMode.InteractiveServer).Dispose();
    }

    [Fact]
    public async Task Without_persistent_state_attaching_does_nothing()
    {
        var persistence = new DashboardPersistence(new ServiceCollection().BuildServiceProvider());
        var controller = new DashboardController("orders");
        var summary = controller.RegisterResource(Summary());

        using (persistence.Attach(controller, RenderMode.InteractiveServer))
        {
            await controller.LoadAsync();
        }

        Assert.Equal(1, _loads);
        Assert.Equal(12, summary.Data!.Orders);
    }
}
