using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleDungeon.Contracts;
using Microsoft.Extensions.Logging;
using Plate.CrossMilo.Contracts;
using Plate.PluginManoi.Contracts;
// Type alias for UI App service
using IUIAppService = Plate.CrossMilo.Contracts.UI.App.IService;
using WingedBeanGameplay = WingedBean.Contracts.Gameplay.IService;

namespace WingedBean.Plugins.SadConsole;

/// <summary>
/// Plugin for SadConsole desktop renderer.
/// Legacy IPlugin interface for compatibility with current plugin loader.
/// </summary>
public class SadConsolePlugin : IPlugin
{
    private SadConsoleApp? _appInstance;

    public string Id => "winged-bean.plugins.sadconsole";
    public string Version => "1.0.0";

    public Task OnActivateAsync(IRegistry registry, CancellationToken ct = default)
    {
        // Create SadConsole app instance
        var logger = registry.GetAll<ILogger<SadConsoleApp>>().FirstOrDefault();
        // Resolve gameplay services (optional)
        IDungeonService? dungeon = null;
        WingedBeanGameplay? gameplay = null;
        try { dungeon = registry.Get<IDungeonService>(); } catch { }
        try { gameplay = registry.Get<WingedBeanGameplay>(); } catch { }

        _appInstance = new SadConsoleApp(logger, dungeon, gameplay);

        // Register in the registry (required for plugin system)
        registry.Register<IUIAppService>(_appInstance, priority: 100);

        return Task.CompletedTask;
    }

    public Task OnDeactivateAsync(CancellationToken ct = default)
    {
        // No cleanup needed
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
