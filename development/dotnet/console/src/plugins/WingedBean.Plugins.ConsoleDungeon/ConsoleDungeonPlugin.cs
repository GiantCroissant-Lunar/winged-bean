using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plate.CrossMilo.Contracts;
using Plate.CrossMilo.Contracts.Terminal;
using Plate.PluginManoi.Core;
using Plate.PluginManoi.Contracts;

namespace WingedBean.Plugins.ConsoleDungeon;

/// <summary>
/// Plugin activator that registers the Console Dungeon Terminal.Gui application
/// </summary>
public class ConsoleDungeonActivator : IPluginActivator
{
    public Task ActivateAsync(IServiceCollection services, IServiceProvider hostServices, CancellationToken ct = default)
    {
        var logger = hostServices.GetService<ILogger<ConsoleDungeonActivator>>();
        logger?.LogInformation("Registering ITerminalApp -> ConsoleDungeonAppRefactored (RFC-0020/0021)");

        services.AddSingleton<ITerminalApp, ConsoleDungeonAppRefactored>();
        return Task.CompletedTask;
    }

    public Task DeactivateAsync(CancellationToken ct = default)
    {
        // Nothing to clean up for this simple service
        return Task.CompletedTask;
    }
}

/// <summary>
/// Legacy plugin interface implementation for backward compatibility with the current host.
/// This bridges the gap between the new IPluginActivator pattern and the old IPlugin pattern.
/// </summary>
public class ConsoleDungeonPlugin : IPlugin
{
    private ConsoleDungeonAppRefactored? _appInstance;

    public string Id => "winged-bean.console-dungeon";
    public string Version => "1.0.0";

    public Task OnActivateAsync(IRegistry registry, CancellationToken ct = default)
    {
        // Create the terminal app instance using parameterless constructor
        _appInstance = new ConsoleDungeonAppRefactored();
        
        // Set registry for the app (RFC-0038)
        _appInstance.SetRegistry(registry);
        
        // Register the service directly with the registry (priority 51 from Plugin attribute)
        registry.Register<ITerminalApp>(_appInstance, priority: 51);
        
        return Task.CompletedTask;
    }

    public Task OnDeactivateAsync(CancellationToken ct = default)
    {
        _appInstance?.Dispose();
        return Task.CompletedTask;
    }

    public IEnumerable<object> GetServices()
    {
        if (_appInstance != null)
        {
            yield return _appInstance;
        }
    }
}
