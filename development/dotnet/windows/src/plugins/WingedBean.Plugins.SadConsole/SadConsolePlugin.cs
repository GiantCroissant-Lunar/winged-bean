using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleDungeon.Contracts;
using Microsoft.Extensions.Logging;
using Plate.CrossMilo.Contracts;
using Plate.CrossMilo.Contracts.Capability;
using Plate.CrossMilo.Contracts.Goap;
using Plate.CrossMilo.Contracts.UI.App;
using Plate.PluginManoi.Contracts;
using WingedBean.Contracts.Gameplay;
using IUIAppService = Plate.CrossMilo.Contracts.UI.App.IService;
using RecorderService = Plate.CrossMilo.Contracts.Recorder.Services.IService;
using WingedBeanGameplay = WingedBean.Contracts.Gameplay.IService;

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
            IGoapPlannerService? planner = null;
            try { planner = registry.Get<IGoapPlannerService>(); } catch { }
            ICapabilityEvaluator? capability = null;
            try { capability = registry.Get<ICapabilityEvaluator>(); } catch { }
            _appInstance = new SadConsoleApp(logger, dungeon, null, null, recorder, planner, capability);
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
        IGoapPlannerService? planner2 = null;
        try { planner2 = registry.Get<IGoapPlannerService>(); } catch { }
        ICapabilityEvaluator? capability2 = null;
        try { capability2 = registry.Get<ICapabilityEvaluator>(); } catch { }
        _appInstance = new SadConsoleApp(logger, dungeon, null, null, recorder, planner2, capability2);

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
