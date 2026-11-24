using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tools;

public static class LuxDashboardServiceCollectionExtensions
{
    public static IServiceCollection AddLuxDashboard(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddBluxUi();
        return services;
    }
}
