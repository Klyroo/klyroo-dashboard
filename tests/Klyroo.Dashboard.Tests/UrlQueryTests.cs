using Xunit;

namespace Klyroo.Dashboard.Tests;

// ToQuery, ApplyQuery and MergeQuery put the filters in a URL and read them back: pure, deterministic, and strict about what a URL can set.
public sealed class UrlQueryTests
{
    private static DashboardController Sales(out Dictionary<string, DashboardFilterControl> filters)
    {
        var dashboard = new DashboardController("sales");
        filters = new Dictionary<string, DashboardFilterControl>
        {
            ["region"] = dashboard.AddFilter(new DashboardFilterControl("region", "Region", items: [new("EMEA", "EMEA"), new("APAC", "APAC"), new("AMER", "AMER")], multiple: true, max: 2)),
            ["segment"] = dashboard.AddFilter(new DashboardFilterControl("segment", "Segment", items: [new("pro", "Pro"), new("free", "Free")], defaultValues: ["pro"])),
            ["q"] = dashboard.AddFilter(new DashboardFilterControl("q", "Search", DashboardFilterKind.String)),
            ["min"] = dashboard.AddFilter(new DashboardFilterControl("min", "Minimum", DashboardFilterKind.Number)),
            ["day"] = dashboard.AddFilter(new DashboardFilterControl("day", "Day", DashboardFilterKind.Date)),
            ["period"] = dashboard.AddFilter(new DashboardFilterControl("period", "Period", DashboardFilterKind.DateRange)),
            ["paid"] = dashboard.AddFilter(new DashboardFilterControl("paid", "Paid", DashboardFilterKind.Boolean)),
            ["compare"] = dashboard.AddFilter(new DashboardFilterControl("compare", "Compare", DashboardFilterKind.Comparison)),
            ["tenant"] = dashboard.AddFilter(new DashboardFilterControl("tenant", "Tenant", headless: true)),
        };
        return dashboard;
    }

    [Fact]
    public void A_dashboard_at_its_defaults_has_an_empty_query()
    {
        Assert.Equal("", Sales(out _).ToQuery());
    }

    [Fact]
    public void Only_filters_that_differ_from_their_default_are_written_in_the_order_of_the_filters()
    {
        var dashboard = Sales(out var f);
        f["q"].Value = "acme";
        f["region"].SetValues(["APAC", "EMEA"]);
        f["segment"].Value = "free";

        Assert.Equal("region=APAC&region=EMEA&segment=free&q=acme", dashboard.ToQuery());
    }

    [Fact]
    public void A_filter_that_returns_to_its_default_leaves_the_query()
    {
        var dashboard = Sales(out var f);
        f["segment"].Value = "free";
        f["segment"].Value = "pro";
        Assert.Equal("", dashboard.ToQuery());
    }

    [Fact]
    public void A_cleared_filter_with_a_default_is_written_as_an_empty_value()
    {
        var dashboard = Sales(out var f);
        f["segment"].SetValues([]);
        Assert.Equal("segment=", dashboard.ToQuery());

        var other = Sales(out var g);
        other.ApplyQuery("segment=");
        Assert.Empty(g["segment"].Values);
        Assert.Equal("segment=", other.ToQuery());
    }

    [Fact]
    public void Headless_and_disabled_filters_are_not_written()
    {
        var dashboard = Sales(out var f);
        f["tenant"].Value = "acme";
        f["q"].Value = "x";
        f["q"].Enabled = false;
        Assert.Equal("", dashboard.ToQuery());
    }

    [Fact]
    public void Keys_and_values_are_percent_encoded_and_the_prefix_goes_before_every_key()
    {
        var dashboard = Sales(out var f);
        f["q"].Value = "R&D = 50% café #1";

        var query = dashboard.ToQuery("sales.");
        Assert.Equal("sales.q=R%26D%20%3D%2050%25%20caf%C3%A9%20%231", query);

        var other = Sales(out var g);
        other.ApplyQuery(query, "sales.");
        Assert.Equal("R&D = 50% café #1", g["q"].Value);
        // The same state always gives the same text.
        Assert.Equal(query, other.ToQuery("sales."));
    }

    [Fact]
    public void A_date_range_with_an_open_bound_keeps_its_two_positions()
    {
        var dashboard = Sales(out var f);
        f["period"].SetValues(["", "2026-01-31"]);
        Assert.Equal("period=&period=2026-01-31", dashboard.ToQuery());

        var other = Sales(out var g);
        other.ApplyQuery("?period=&period=2026-01-31");
        Assert.Equal(["", "2026-01-31"], g["period"].Values);
    }

    [Fact]
    public void Every_kind_round_trips_through_a_url()
    {
        var source = Sales(out var f);
        f["region"].SetValues(["AMER", "EMEA"]);
        f["segment"].Value = "free";
        f["q"].Value = "acme corp";
        f["min"].Value = "1234.5";
        f["day"].Value = "2026-03-01";
        f["period"].SetValues(["2026-01-01", "2026-01-31"]);
        f["paid"].Value = "true";
        f["compare"].Value = "year";

        var target = Sales(out var g);
        Assert.True(target.ApplyQuery(source.ToQuery()));

        foreach (var key in f.Keys.Where(k => k != "tenant"))
        {
            Assert.Equal(f[key].Values, g[key].Values);
        }
        Assert.Equal(source.ToQuery(), target.ToQuery());
        Assert.Equal(source.FilterFingerprint(), target.FilterFingerprint());
    }

    [Fact]
    public void Applying_the_url_before_loading_gives_the_same_fingerprint_as_the_prerender_had()
    {
        // DashboardPersistence refuses data written under other filter values: a render that starts from the URL must match it.
        var prerender = Sales(out var f);
        f["region"].SetValues(["EMEA"]);
        f["period"].SetValues(["2026-01-01", "2026-01-31"]);

        var interactive = Sales(out _);
        interactive.ApplyQuery("?" + prerender.ToQuery());

        Assert.Equal(prerender.FilterFingerprint(), interactive.FilterFingerprint());
        Assert.NotEqual(Sales(out _).FilterFingerprint(), interactive.FilterFingerprint());
    }

    [Fact]
    public void A_filter_missing_from_the_url_goes_back_to_its_default_and_headless_ones_are_left_alone()
    {
        var dashboard = Sales(out var f);
        f["q"].Value = "old";
        f["segment"].Value = "free";
        f["tenant"].Value = "acme";

        Assert.True(dashboard.ApplyQuery("region=EMEA"));

        Assert.Equal(["EMEA"], f["region"].Values);
        Assert.Null(f["q"].Value);
        Assert.Equal(["pro"], f["segment"].Values);
        Assert.Equal("acme", f["tenant"].Value);
    }

    [Fact]
    public void A_url_cannot_set_a_headless_or_a_disabled_filter()
    {
        var dashboard = Sales(out var f);
        f["q"].Enabled = false;
        Assert.False(dashboard.ApplyQuery("tenant=evil&q=evil"));
        Assert.Null(f["tenant"].Value);
        Assert.Null(f["q"].Value);
    }

    [Theory]
    [InlineData("min=abc")]
    [InlineData("min=NaN")]
    [InlineData("min=Infinity")]
    [InlineData("day=2026-13-01")]
    [InlineData("day=01/03/2026")]
    [InlineData("period=2026-01-01&period=tomorrow")]
    [InlineData("paid=maybe")]
    [InlineData("compare=weekly")]
    [InlineData("region=MARS")]
    [InlineData("segment=enterprise")]
    public void A_value_that_does_not_fit_its_filter_is_ignored(string query)
    {
        var dashboard = Sales(out var f);
        Assert.False(dashboard.ApplyQuery(query));
        Assert.Equal("", dashboard.ToQuery());
        Assert.Equal(["pro"], f["segment"].Values);
    }

    [Fact]
    public void A_text_that_is_too_long_is_ignored()
    {
        var dashboard = Sales(out var f);
        Assert.False(dashboard.ApplyQuery("q=" + new string('a', 513)));
        Assert.Null(f["q"].Value);
        Assert.True(dashboard.ApplyQuery("q=" + new string('a', 512)));
    }

    [Fact]
    public void Known_options_filter_the_values_but_unloaded_options_accept_them()
    {
        var dashboard = Sales(out var f);
        dashboard.ApplyQuery("region=EMEA&region=MARS&region=APAC");
        // MARS is not an option; Max is 2.
        Assert.Equal(["EMEA", "APAC"], f["region"].Values);

        var remote = new DashboardController();
        var city = remote.AddFilter(new DashboardFilterControl("city", "City", DashboardFilterKind.Remote));
        remote.ApplyQuery("city=Paris");
        Assert.Equal(["Paris"], city.Values);
    }

    [Fact]
    public void A_single_value_filter_takes_the_first_value_and_a_multiple_one_drops_duplicates_and_respects_Max()
    {
        var dashboard = Sales(out var f);
        dashboard.ApplyQuery("segment=free&segment=pro&region=EMEA&region=EMEA&region=APAC&region=AMER");
        Assert.Equal(["free"], f["segment"].Values);
        Assert.Equal(["EMEA", "APAC"], f["region"].Values);
    }

    [Fact]
    public void Booleans_and_comparisons_are_read_ignoring_case_and_numbers_are_stored_the_way_the_page_stores_them()
    {
        var dashboard = Sales(out var f);
        dashboard.ApplyQuery("paid=TRUE&compare=Previous&min=1e3");
        Assert.Equal(["true"], f["paid"].Values);
        Assert.Equal(["previous"], f["compare"].Values);
        Assert.Equal(["1000"], f["min"].Values);
    }

    [Fact]
    public void A_query_is_read_with_or_without_the_question_mark_a_fragment_and_plus_signs()
    {
        var dashboard = Sales(out var f);
        Assert.True(dashboard.ApplyQuery("?q=acme+corp&utm=x#section"));
        Assert.Equal("acme corp", f["q"].Value);
        Assert.False(dashboard.ApplyQuery("q=acme%20corp"));
        Assert.True(dashboard.ApplyQuery(null));
        Assert.Null(f["q"].Value);
        Assert.False(dashboard.ApplyQuery(""));
    }

    [Fact]
    public void Applying_a_query_that_matches_the_state_changes_nothing_and_raises_nothing()
    {
        var dashboard = Sales(out var f);
        dashboard.ApplyQuery("region=EMEA&q=acme");
        var raised = 0;
        dashboard.Changed += () => raised++;

        Assert.False(dashboard.ApplyQuery("region=EMEA&q=acme"));
        Assert.False(dashboard.ApplyQuery("q=acme&region=EMEA"));
        Assert.Equal(0, raised);

        // Extra values a single filter drops do not make it change on every application.
        dashboard.ApplyQuery("q=acme&q=other&region=EMEA");
        Assert.Equal(0, raised);
        Assert.Equal("acme", f["q"].Value);
    }

    [Fact]
    public void ApplyQuery_does_not_reload_resources_but_raises_the_change_events_of_the_filters_it_changed()
    {
        var dashboard = Sales(out var f);
        var loads = 0;
        dashboard.RegisterResource(new DashboardResource<int>("r", _ => { loads++; return Task.FromResult(1); }));
        var changed = 0;
        f["region"].ChangedEvent += () => changed++;

        dashboard.ApplyQuery("region=EMEA");

        Assert.Equal(0, loads);
        Assert.Equal(1, changed);
    }

    [Fact]
    public void MergeQuery_replaces_the_filters_and_keeps_every_other_pair_as_it_was()
    {
        var dashboard = Sales(out var f);
        f["region"].SetValues(["APAC"]);

        Assert.Equal("page=2&tenant=acme&region=APAC", dashboard.MergeQuery("?region=EMEA&page=2&q=old&tenant=acme#top"));
        Assert.Equal("region=APAC", dashboard.MergeQuery(null));

        f["region"].Reset();
        Assert.Equal("page=2", dashboard.MergeQuery("page=2&region=EMEA"));
        Assert.Equal("", dashboard.MergeQuery("region=EMEA"));
    }

    [Fact]
    public void A_disabled_filter_keeps_its_pair_in_the_url_and_takes_it_when_it_is_enabled_again()
    {
        var dashboard = Sales(out var f);
        f["q"].Enabled = false;
        f["region"].SetValues(["APAC"]);

        // The pair of the disabled filter is not the controller's to remove.
        Assert.Equal("q=acme&region=APAC", dashboard.MergeQuery("q=acme&region=EMEA"));

        Assert.False(dashboard.ApplyQuery("q=acme&region=APAC"));
        Assert.Null(f["q"].Value);

        f["q"].Enabled = true;
        Assert.Equal("acme", f["q"].Value);
        Assert.Equal("region=APAC&q=acme", dashboard.ToQuery());
    }

    [Fact]
    public void A_value_the_url_no_longer_has_is_not_applied_when_the_filter_is_enabled()
    {
        var dashboard = Sales(out var f);
        f["q"].Enabled = false;
        dashboard.ApplyQuery("q=acme");
        dashboard.ApplyQuery("region=EMEA");
        f["q"].Enabled = true;
        Assert.Null(f["q"].Value);

        // An invalid value in the URL is not remembered either.
        f["min"].Enabled = false;
        dashboard.ApplyQuery("min=abc");
        f["min"].Enabled = true;
        Assert.Null(f["min"].Value);
    }

    [Fact]
    public void QueryMatches_compares_decoded_pairs_of_the_controller_ignoring_order_and_encoding()
    {
        var dashboard = Sales(out var f);
        f["region"].SetValues(["APAC", "EMEA"]);
        f["q"].Value = "a b";

        Assert.True(dashboard.QueryMatches("?q=a+b&region=APAC&region=EMEA&page=2#x"));
        Assert.True(dashboard.QueryMatches("region=APAC&q=a%20b&region=EMEA"));
        // The order of the values of one filter is part of its selection; a missing, extra or other pair is not a match.
        Assert.False(dashboard.QueryMatches("region=EMEA&region=APAC&q=a%20b"));
        Assert.False(dashboard.QueryMatches("region=APAC&region=EMEA"));
        Assert.False(dashboard.QueryMatches("region=APAC&region=EMEA&q=a%20b&segment=free"));
        Assert.True(Sales(out _).QueryMatches("page=2"));
    }

    [Fact]
    public void MergeQuery_with_a_prefix_only_touches_the_keys_of_that_prefix()
    {
        var dashboard = Sales(out var f);
        f["q"].Value = "x";
        Assert.Equal("q=keep&sales.q=x", dashboard.MergeQuery("q=keep&sales.q=old", "sales."));
    }
}
