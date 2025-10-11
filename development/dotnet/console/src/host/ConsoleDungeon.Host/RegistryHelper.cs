using System;
using System.Linq;
using System.Reflection;
using Plate.PluginManoi.Contracts;
using Plate.CrossMilo.Contracts;
using ConsoleDungeon.Contracts;
using Plate.CrossMilo.Contracts.ECS.Services;
using Plate.CrossMilo.Contracts.Hosting.App;
using Plate.CrossMilo.Contracts.Hosting.Host;
// Note: IDungeonService replaces Game.Dungeon.IService, RenderService removed

// Type aliases for the new IService pattern
using ITerminalApp = Plate.CrossMilo.Contracts.Terminal.ITerminalApp;
using IECSService = Plate.CrossMilo.Contracts.ECS.Services.IService;
using IUIApp = Plate.CrossMilo.Contracts.UI.App.IService;
using IWingedBeanApp = Plate.CrossMilo.Contracts.Hosting.App.IService;
using IWingedBeanHost = Plate.CrossMilo.Contracts.Hosting.Host.IService;

namespace ConsoleDungeon.Host;

/// <summary>
/// Helper class for type-safe registry operations without reflection.
/// Provides manual dispatch for known contract types (RFC-0038 Phase 2).
/// This will be replaced by source generators in future phases.
/// </summary>
public static class RegistryHelper
{
    /// <summary>
    /// Registers an instance with the registry for a given contract type.
    /// Uses type-safe dispatch for known contracts, falls back to reflection for unknown types.
    /// </summary>
    /// <param name="registry">The registry to register with.</param>
    /// <param name="contractType">The contract interface type.</param>
    /// <param name="instance">The implementation instance.</param>
    /// <param name="priority">Registration priority.</param>
    public static void RegisterDynamic(
        this IRegistry registry,
        Type contractType,
        object instance,
        int priority)
    {
        // Fast path: Known contract types (no reflection)
        // All contracts have been migrated to Plate.CrossMilo.Contracts.*
        switch (contractType.FullName)
        {
            case "Plate.CrossMilo.Contracts.Terminal.ITerminalApp":
            case "Plate.CrossMilo.Contracts.TerminalUI.ITerminalApp":  // Legacy
            case "WingedBean.Contracts.Terminal.IService":  // Legacy
            case "WingedBean.Contracts.Terminal.ITerminalApp":  // Legacy
                registry.Register((ITerminalApp)instance, priority);
                return;
                
            case "ConsoleDungeon.Contracts.IDungeonService":
            case "Plate.CrossMilo.Contracts.Game.Dungeon.IService":  // Legacy
            case "WingedBean.Contracts.Game.IDungeonGameService":  // Legacy
                registry.Register((IDungeonService)instance, priority);
                return;
                
            // RenderService removed - no longer supported
            // case "Plate.CrossMilo.Contracts.Game.Render.IService":
            // case "WingedBean.Contracts.Game.IRenderService":  // Legacy
            //     registry.Register((IRenderService)instance, priority);
            //     return;
                
            case "Plate.CrossMilo.Contracts.ECS.Services.IService":
            case "WingedBean.Contracts.ECS.IECSService":  // Legacy
                registry.Register((IECSService)instance, priority);
                return;
                
            case "Plate.CrossMilo.Contracts.UI.App.IService":
            case "WingedBean.Contracts.UI.IService":  // Legacy
            case "WingedBean.Contracts.UI.IUIApp":  // Legacy
                registry.Register((IUIApp)instance, priority);
                return;
                
            case "Plate.CrossMilo.Contracts.Hosting.App.IService":
            case "WingedBean.Contracts.Hosting.IWingedBeanApp":  // Legacy
                registry.Register((IWingedBeanApp)instance, priority);
                return;
                
            case "Plate.CrossMilo.Contracts.Hosting.Host.IService":
            case "WingedBean.Contracts.Hosting.IWingedBeanHost":  // Legacy
                registry.Register((IWingedBeanHost)instance, priority);
                return;
                
            // Note: Scene and Input contracts are not referenced by host
            // They will be handled by reflection fallback if needed
                
            default:
                // Slow path: Unknown type - use reflection (backward compatibility)
                RegisterViaReflection(registry, contractType, instance, priority);
                return;
        }
    }
    
    private static void RegisterViaReflection(
        IRegistry registry,
        Type contractType,
        object instance,
        int priority)
    {
        // Fallback to reflection for types not in the switch above
        var registerMethod = typeof(IRegistry).GetMethods()
            .Where(m => m.Name == "Register" && m.IsGenericMethod)
            .Where(m => m.GetParameters().Length == 2)
            .Where(m => m.GetParameters()[0].ParameterType.IsGenericParameter)
            .FirstOrDefault();
            
        if (registerMethod != null)
        {
            var genericMethod = registerMethod.MakeGenericMethod(contractType);
            genericMethod.Invoke(registry, new object[] { instance, priority });
        }
    }
}
