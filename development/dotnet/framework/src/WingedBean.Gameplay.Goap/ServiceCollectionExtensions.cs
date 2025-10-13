using Microsoft.Extensions.DependencyInjection;

namespace WingedBean.Gameplay.Goap;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWingedBeanGoap(this IServiceCollection services)
    {
        // Register planner service (depends on ITimeProvider provided elsewhere)
        services.AddSingleton<IGoapPlannerService, GoapPlannerService>();
        return services;
    }
}
