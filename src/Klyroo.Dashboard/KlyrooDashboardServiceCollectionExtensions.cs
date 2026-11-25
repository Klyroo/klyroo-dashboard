using Microsoft.Extensions.DependencyInjection;

namespace Klyroo.Dashboard;

/// <summary>Registration of the Klyroo.Dashboard services.</summary>
public static class KlyrooDashboardServiceCollectionExtensions
{
    /// <summary>
    /// Registers the services the dashboard components need. The dashboard has none of its own: this calls
    /// <c>AddKlyrooUi()</c> from Klyroo.UI, so a second call to <c>AddKlyrooUi()</c> is not needed.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddKlyrooDashboard(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddKlyrooUi();
        return services;
    }
}
