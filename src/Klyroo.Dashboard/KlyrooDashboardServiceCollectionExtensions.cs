using Microsoft.Extensions.DependencyInjection;

namespace Klyroo.Dashboard;

public static class KlyrooDashboardServiceCollectionExtensions
{
    public static IServiceCollection AddKlyrooDashboard(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddKlyrooUi();
        return services;
    }
}
