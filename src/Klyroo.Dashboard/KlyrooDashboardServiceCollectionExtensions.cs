using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Klyroo.Dashboard;

/// <summary>Registration of the Klyroo.Dashboard services.</summary>
public static class KlyrooDashboardServiceCollectionExtensions
{
    /// <summary>
    /// Registers the services the dashboard components need: <see cref="DashboardPersistence"/> (scoped), the <see cref="KlyrooDashboardOptions"/>,
    /// and Klyroo.UI through <c>AddKlyrooUi()</c>, so a second call to <c>AddKlyrooUi()</c> is not needed.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Changes the options. A second call changes the same instance.</param>
    /// <returns>The same service collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddKlyrooDashboard(this IServiceCollection services, Action<KlyrooDashboardOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddKlyrooUi();
        var options = services.FirstOrDefault(d => d.ServiceType == typeof(KlyrooDashboardOptions))?.ImplementationInstance as KlyrooDashboardOptions;
        if (options is null)
        {
            options = new KlyrooDashboardOptions();
            services.AddSingleton(options);
        }
        configure?.Invoke(options);
        services.TryAddScoped<DashboardPersistence>();
        return services;
    }
}
