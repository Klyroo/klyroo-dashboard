using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Klyroo.Dashboard;

/// <summary>Registration of the Klyroo.Dashboard services.</summary>
public static class KlyrooDashboardServiceCollectionExtensions
{
    private static readonly Func<IServiceProvider, object> ExtensionFactory = sp => sp.GetRequiredService<KlyrooOptions>().Extension<KlyrooDashboardOptions>();

    /// <summary>
    /// Registers the services the dashboard components need: <see cref="DashboardPersistence"/> (scoped), the <see cref="KlyrooDashboardOptions"/>,
    /// and Klyroo.UI through <c>AddKlyrooUi()</c>, so a second call to <c>AddKlyrooUi()</c> is not needed.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Changes the options, such as the built-in texts of a locale with <see cref="KlyrooDashboardOptions.Texts"/>. The options are stored with the Klyroo.UI options, so a second call changes the same instance.</param>
    /// <returns>The same service collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">A <see cref="KlyrooDashboardOptions"/> was registered in <paramref name="services"/> by hand: configure the options here instead.</exception>
    public static IServiceCollection AddKlyrooDashboard(this IServiceCollection services, Action<KlyrooDashboardOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        // The options live in the Klyroo.UI options, where the components read them. An instance registered by hand would be a second set of options
        // that configure, texts and PersistMaxBytes never reach, so it is refused instead of silently ignored.
        if (services.Any(d => d.ServiceType == typeof(KlyrooDashboardOptions) && !ReferenceEquals(d.ImplementationFactory, ExtensionFactory)))
        {
            throw new InvalidOperationException($"{nameof(KlyrooDashboardOptions)} is already registered in the service collection. Remove that registration and configure the options with {nameof(AddKlyrooDashboard)}(options => ...): they are stored with the Klyroo.UI options, and a second instance would not receive the texts or the limits.");
        }
        services.AddKlyrooUi(ui => configure?.Invoke(ui.Extension<KlyrooDashboardOptions>()));
        // The same instance the components read through the Klyroo.UI options.
        services.TryAddSingleton(typeof(KlyrooDashboardOptions), ExtensionFactory);
        services.TryAddScoped<DashboardPersistence>();
        return services;
    }
}
