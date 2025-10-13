using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleDungeon.Contracts;
using Microsoft.Extensions.Logging;
using Plate.CrossMilo.Contracts;
using Plate.PluginManoi.Contracts;
// WingedBean.Contracts.Gameplay is OPTIONAL - not needed for desktop-only scenarios
// using WingedBean.Contracts.Gameplay;
// Type alias for UI App service
using IUIAppService = Plate.CrossMilo.Contracts.UI.App.IService;
// using WingedBeanGameplay = WingedBean.Contracts.Gameplay.IService;
using RecorderService = Plate.CrossMilo.Contracts.Recorder.Services.IService;

namespace WingedBean.Plugins.SadConsole;

/// <summary>
/// Plugin for SadConsole desktop renderer.
/// Legacy IPlugin interface for compatibility with current plugin loader.
/// </summary>
public class SadConsolePlugin : IPlugin
{
    private SadConsoleApp? _appInstance;
    private IRegistry? _registry;

    public string Id => "winged-bean.plugins.sadconsole";
    public string Version => "1.0.0";

    public Task OnActivateAsync(IRegistry registry, CancellationToken ct = default)
    {
        // Create SadConsole app instance
        var logger = registry.GetAll<ILogger<SadConsoleApp>>().FirstOrDefault();
        // Resolve gameplay services (optional - not available on desktop-only scenarios)
        IDungeonService? dungeon = null;
        RecorderService? recorder = null;
        try { dungeon = registry.Get<IDungeonService>(); } catch { }
        try { recorder = registry.Get<RecorderService>(); } catch { }
        if (_appInstance == null)
        {
            _appInstance = new SadConsoleApp(logger, dungeon, null, null, recorder);
        }

        // Register in the registry (required for plugin system)
        registry.Register<IUIAppService>(_appInstance, priority: 100);

        return Task.CompletedTask;
    }

    // Registry injection for manifest loader path (reflection-based SetRegistry)
    public void SetRegistry(IRegistry registry)
    {
        _registry = registry;
        if (_appInstance != null) return;

        // Create SadConsole app instance IMMEDIATELY
        var logger = registry.GetAll<ILogger<SadConsoleApp>>().FirstOrDefault();
        IDungeonService? dungeon = null;
        RecorderService? recorder = null;
        try { dungeon = registry.Get<IDungeonService>(); } catch { }
        try { recorder = registry.Get<RecorderService>(); } catch { }
        _appInstance = new SadConsoleApp(logger, dungeon, null, null, recorder);

        // IMPORTANT: Also register it directly for the old loader path
        registry.Register<IUIAppService>(_appInstance, priority: 100);
    }

    public Task OnDeactivateAsync(CancellationToken ct = default)
    {
        // No cleanup needed
        return Task.CompletedTask;
    }

    public IEnumerable<object> GetServices()
    {
        // App should have been created in SetRegistry or OnActivateAsync
        if (_appInstance == null)
        {
            // Fallback: create with no dependencies
            _appInstance = new SadConsoleApp();
        }
        yield return _appInstance;
    }
}
