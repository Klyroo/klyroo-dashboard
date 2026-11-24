using Microsoft.Extensions.DependencyInjection;

namespace Lux.Dashboard;

public static class LuxDashboardServiceCollectionExtensions
{
    public static IServiceCollection AddLuxDashboard(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddLuxUi();
        return services;
    }
}
