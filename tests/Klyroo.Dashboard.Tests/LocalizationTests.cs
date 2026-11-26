using System.Reflection;
using Bunit;
using Klyroo.UI;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Klyroo.Dashboard.Tests;

// The built-in texts follow the locale of Klyroo.UI, one set per locale, each changeable on its own.
public sealed class LocalizationTests : BunitContext
{
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);

    public LocalizationTests()
    {
        Services.AddKlyrooDashboard();
    }

    private IRenderedComponent<CascadingValue<KlyLocale>> InLocale<TComponent>(KlyLocale locale, Action<ComponentParameterCollectionBuilder<TComponent>> parameters)
        where TComponent : IComponent =>
        Render<CascadingValue<KlyLocale>>(p => p.Add(x => x.Value, locale).AddChildContent(parameters));

    private static DashboardResource<string> Loaded()
    {
        var resource = new DashboardResource<string>("r", _ => Task.FromResult("x"));
        resource.SetData("x");
        return resource;
    }

    [Fact]
    public void The_card_menu_and_empty_state_show_the_texts_of_the_locale()
    {
        var fr = InLocale<KlyDashboardBars>(KlyLocales.Fr, p => p
            .Add(x => x.Title, "Régions").Add(x => x.Menu, true)
            .Add(x => x.Items, [new DashboardBarItem("EMEA", 3)]));
        Assert.Equal("Afficher en tableau", fr.Find("[data-slot=dashboard-card-table-toggle]").TextContent.Trim());
        Assert.Equal("Télécharger en CSV", fr.Find("[data-slot=dashboard-card-csv]").TextContent.Trim());
        Assert.Equal("Agrandir", fr.Find("[data-slot=dashboard-card-expand]").TextContent.Trim());
        fr.Find("[data-slot=dashboard-card-table-toggle]").Click();
        Assert.Equal("Masquer le tableau", fr.Find("[data-slot=dashboard-card-table-toggle]").TextContent.Trim());
        Assert.Equal("Libellé", fr.Find("[data-slot=dashboard-data-table] th").TextContent.Trim());

        var empty = InLocale<KlyDashboardBars>(KlyLocales.Fr, p => p.Add(x => x.Items, []));
        Assert.Equal("Aucune donnée pour cette période", empty.Find("[data-slot=dashboard-empty] p").TextContent.Trim());

        var en = InLocale<KlyDashboardBars>(KlyLocales.En, p => p.Add(x => x.Items, []));
        Assert.Equal("No data for this period", en.Find("[data-slot=dashboard-empty] p").TextContent.Trim());
    }

    [Fact]
    public void The_error_state_and_retry_button_are_localized_but_the_message_of_the_resource_is_kept()
    {
        var failed = new DashboardResource<string>("r", _ => Task.FromResult("x"));
        failed.SetError("boom");
        var cut = InLocale<KlyDashboardBars>(KlyLocales.Fr, p => p.Add(x => x.Source, failed));
        Assert.Equal("Impossible de charger ces données", cut.Find("[data-slot=dashboard-error] p").TextContent.Trim());
        Assert.Contains("boom", cut.Find("[data-slot=dashboard-error]").TextContent);
        Assert.Equal("Réessayer", cut.Find("[data-slot=dashboard-retry]").TextContent.Trim());
    }

    [Fact]
    public void The_page_toolbar_and_the_freshness_line_are_localized()
    {
        var dashboard = new DashboardController();
        dashboard.RegisterResource(Loaded());
        dashboard.AddFilter(new DashboardFilterControl("period", "Période", DashboardFilterKind.DateRange));
        dashboard.AddFilter(new DashboardFilterControl("region", "Région", items: [new("emea", "EMEA")], searchable: true));
        dashboard.Controls[1].Toggle("emea");

        var cut = InLocale<KlyDashboardPage>(KlyLocales.Fr, p => p.Add(x => x.Dashboard, dashboard).Add(x => x.Title, "Ventes"));

        Assert.Equal("Actualiser", cut.Find("[data-slot=dashboard-refresh]").GetAttribute("aria-label"));
        Assert.Equal("Mis à jour à l’instant", cut.Find("[data-slot=dashboard-freshness]").TextContent.Trim());
        Assert.Equal("Filtres", cut.Find("[data-slot=dashboard-filters]").GetAttribute("aria-label"));
        Assert.Equal("Période\u00a0: début", cut.Find("[data-slot=dashboard-filter-start]").GetAttribute("aria-label"));
        Assert.Equal("Période\u00a0: fin", cut.Find("[data-slot=dashboard-filter-end]").GetAttribute("aria-label"));
        Assert.Equal("Effacer Région", cut.Find("[data-slot=dashboard-filter-clear]").GetAttribute("aria-label"));
        Assert.Equal("Réinitialiser", cut.Find("[data-slot=dashboard-filters-reset]").TextContent.Trim());
    }

    [Fact]
    public void Relative_times_and_day_headings_of_a_feed_follow_the_locale()
    {
        DashboardFeedItem[] items = [new("Commande", Now.AddMinutes(-5)), new("Retour", Now.AddDays(-1))];
        var cut = InLocale<KlyDashboardFeed>(KlyLocales.Fr, p => p.Add(x => x.Items, items).Add(x => x.Now, Now));
        Assert.Equal(["il y a 5 min", "hier"], cut.FindAll("[data-slot=dashboard-feed-time]").Select(t => t.TextContent.Trim()));
        Assert.Equal(["Aujourd’hui", "Hier"], cut.FindAll("[data-slot=dashboard-feed-day]").Select(t => t.TextContent.Trim()));
    }

    [Fact]
    public void Column_headers_of_the_table_view_and_the_csv_export_are_localized()
    {
        var chart = InLocale<KlyDashboardChart>(KlyLocales.Fr, p => p
            .Add(x => x.Menu, true)
            .Add(x => x.Categories, ["Jan"])
            .Add(x => x.Series, [new DashboardChartSeries("revenue", "CA", [1])]));
        chart.Find("[data-slot=dashboard-card-table-toggle]").Click();
        Assert.Equal("Catégorie", chart.Find("[data-slot=dashboard-data-table] th").TextContent.Trim());

        var alerts = InLocale<KlyDashboardAlerts>(KlyLocales.Fr, p => p
            .Add(x => x.Menu, true)
            .Add(x => x.Items, [new DashboardAlertItem(DashboardAlertSeverity.Error, "Panne")]));
        Assert.Equal("Critique", alerts.Find("[data-slot=dashboard-alert-row] [data-slot], [data-slot=dashboard-alert-row] span").TextContent.Trim());
    }

    [Fact]
    public void Gauge_and_stat_texts_with_a_value_follow_the_locale_word_order()
    {
        var gauge = InLocale<KlyDashboardGauge>(KlyLocales.Fr, p => p.Add(x => x.Value, 40.0).Add(x => x.Target, 80.0));
        Assert.Equal("Cible : 80", gauge.Find("[data-slot=gauge-target]").TextContent.Trim());

        var stat = InLocale<KlyDashboardStat>(KlyLocales.De, p => p.Add(x => x.Label, "Umsatz").Add(x => x.Value, 5.0).Add(x => x.Compare, 4.0));
        Assert.Equal("vs. 4 im Vorzeitraum", stat.Find("[data-slot=dashboard-stat-caption]").TextContent.Trim());
    }

    [Fact]
    public void A_change_applies_to_its_locale_and_to_the_locales_that_resolve_to_it()
    {
        Services.AddKlyrooDashboard(o => o.Texts("fr", t => t with { NoData = "Rien ici" }));
        var canadian = KlyLocales.Extend(KlyLocales.Fr, code: "fr-CA", name: "Français (Canada)");

        Assert.Equal("Rien ici", InLocale<KlyDashboardBars>(KlyLocales.Fr, p => p.Add(x => x.Items, [])).Find("[data-slot=dashboard-empty] p").TextContent.Trim());
        Assert.Equal("Rien ici", InLocale<KlyDashboardBars>(canadian, p => p.Add(x => x.Items, [])).Find("[data-slot=dashboard-empty] p").TextContent.Trim());
        Assert.Equal("No data for this period", InLocale<KlyDashboardBars>(KlyLocales.En, p => p.Add(x => x.Items, [])).Find("[data-slot=dashboard-empty] p").TextContent.Trim());
    }

    [Fact]
    public void Options_registered_by_hand_are_refused_so_they_never_split_from_the_extension()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new KlyrooDashboardOptions { PersistMaxBytes = 1 });
        var error = Assert.Throws<InvalidOperationException>(() => services.AddKlyrooDashboard());
        Assert.Contains("already registered", error.Message);
    }

    [Fact]
    public void Calling_AddKlyrooDashboard_twice_configures_one_instance_that_the_container_and_the_components_share()
    {
        var services = new ServiceCollection();
        services.AddKlyrooDashboard(o => o.PersistMaxBytes = 111);
        services.AddKlyrooDashboard(o => o.PersistMaxAge = TimeSpan.FromSeconds(7));
        using var provider = services.BuildServiceProvider();

        var fromContainer = provider.GetRequiredService<KlyrooDashboardOptions>();
        Assert.Same(provider.GetRequiredService<KlyrooOptions>().Extension<KlyrooDashboardOptions>(), fromContainer);
        Assert.Equal(111, fromContainer.PersistMaxBytes);
        Assert.Equal(TimeSpan.FromSeconds(7), fromContainer.PersistMaxAge);
        Assert.Single(services, d => d.ServiceType == typeof(KlyrooDashboardOptions));
    }

    [Fact]
    public void Texts_cannot_change_once_a_component_resolved_one()
    {
        InLocale<KlyDashboardBars>(KlyLocales.En, p => p.Add(x => x.Items, []));
        var options = Services.GetRequiredService<KlyrooDashboardOptions>();
        Assert.Throws<InvalidOperationException>(() => options.Texts("en", t => t));
    }

    [Fact]
    public void The_options_in_the_container_are_the_extension_of_the_Klyroo_UI_options()
    {
        Assert.Same(Services.GetRequiredService<KlyrooOptions>().Extension<KlyrooDashboardOptions>(), Services.GetRequiredService<KlyrooDashboardOptions>());
    }

    [Fact]
    public void Every_locale_of_Klyroo_UI_has_a_complete_set()
    {
        Assert.Equal(KlyLocales.All.Select(l => l.Code).Order(StringComparer.OrdinalIgnoreCase), DashboardLocales.Tables.Keys.Order(StringComparer.OrdinalIgnoreCase));
        foreach (var (code, texts) in DashboardLocales.Tables)
        {
            foreach (var property in typeof(DashboardTexts).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = property.GetValue(texts);
                var shown = value switch
                {
                    string s => new[] { s },
                    Func<int, string> f => new[] { f(1), f(2), f(5), f(11), f(21) },
                    Func<string, string> f => new[] { f("Region") },
                    DashboardRelativeTimeTexts => ["ok"],
                    _ => throw new InvalidOperationException($"{property.Name} has an unexpected type."),
                };
                Assert.All(shown, s => Assert.False(string.IsNullOrWhiteSpace(s), $"{code}.{property.Name}"));
            }
            if (code != "en")
            {
                // A translated set does not fall back to the English text of a label.
                Assert.NotEqual(DashboardLocales.En.ViewAsTable, texts.ViewAsTable);
                Assert.NotEqual(DashboardLocales.En.NoData, texts.NoData);
                Assert.NotEqual(DashboardLocales.En.ColumnCategory, texts.ColumnCategory);
                Assert.NotEqual(DashboardLocales.En.RelativeJustNow, texts.RelativeJustNow);
            }
        }
    }

    [Fact]
    public void No_text_of_any_locale_holds_a_literal_unicode_escape()
    {
        foreach (var (code, texts) in DashboardLocales.Tables)
        {
            foreach (var property in typeof(DashboardTexts).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var shown = property.GetValue(texts) switch
                {
                    string s => new[] { s },
                    Func<int, string> f => new[] { f(1), f(5) },
                    Func<string, string> f => new[] { f("Region") },
                    _ => [],
                };
                Assert.All(shown, s => Assert.DoesNotContain("\\u", s));
            }
        }
    }

    [Fact]
    public void French_puts_a_no_break_space_before_a_colon_and_a_percent_sign()
    {
        var fr = DashboardLocales.Fr;
        Assert.Equal("base 100\u00a0%", fr.FunnelBase);
        Assert.Equal("60\u00a0% de l’étape précédente", fr.FunnelStep(60));
        Assert.Equal("Cible\u00a0: 80", fr.Target("80"));
        Assert.Equal("Période\u00a0: début", fr.FilterFrom("Période"));
        Assert.Equal("Période\u00a0: fin", fr.FilterTo("Période"));
        Assert.Equal("par rapport à 4 sur la période précédente", fr.VersusPrevious("4"));
        // Nowhere a plain space before the signs that French sets apart with a no-break one.
        foreach (var text in new[] { fr.FunnelBase, fr.Target("x"), fr.FilterFrom("x"), fr.FilterTo("x"), fr.FunnelStep(1) })
        {
            Assert.DoesNotMatch(@" [:;!?%]", text);
        }
    }

    [Fact]
    public void Relative_time_overload_uses_the_words_it_is_given_and_keeps_the_english_default()
    {
        var fr = DashboardLocales.Fr.RelativeTime;
        Assert.Equal("à l’instant", DashboardFormat.FormatRelativeTime(Now.AddSeconds(-10), Now, fr));
        Assert.Equal("il y a 3 h", DashboardFormat.FormatRelativeTime(Now.AddHours(-3), Now, fr));
        Assert.Equal("il y a 3 jours", DashboardFormat.FormatRelativeTime(Now.AddDays(-3), Now, fr));
        Assert.Equal("2026-11-01", DashboardFormat.FormatRelativeTime(new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero), Now, fr));
        Assert.Equal("3 hr ago", DashboardFormat.FormatRelativeTime(Now.AddHours(-3), Now));
        Assert.Equal("", DashboardFormat.FormatRelativeTime(null, Now, fr));
        Assert.Throws<ArgumentNullException>(() => DashboardFormat.FormatRelativeTime(Now, Now, null!));
    }

    [Theory]
    [InlineData(1, "день")]
    [InlineData(2, "дня")]
    [InlineData(5, "дней")]
    [InlineData(11, "дней")]
    [InlineData(21, "день")]
    public void Russian_days_take_their_plural_form(int n, string word) =>
        Assert.Equal($"{n} {word} назад", DashboardLocales.Ru.RelativeDays(n));

    [Theory]
    [InlineData(2, "دقيقة")]
    [InlineData(5, "دقائق")]
    [InlineData(11, "دقيقة")]
    public void Arabic_minutes_take_their_plural_form(int n, string word) =>
        Assert.Equal($"منذ {n} {word}", DashboardLocales.Ar.RelativeMinutes(n));
}
