using Microsoft.Extensions.DependencyInjection;
using Plate.Goap.Adapters;

namespace WingedBean.Plugins.ConsoleDungeon;

public static class GoapIntegration
{
    public static IServiceCollection AddGoap(this IServiceCollection services)
    {
        // Time provider for timers and disablers
        services.AddSingleton<ITimeProvider, StopwatchTimeProvider>();
        return services;
    }
}
