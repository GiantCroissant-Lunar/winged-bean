using System;
using System.Linq;
using System.Reflection;
using ConsoleDungeon.Contracts;
using HostLand.Abstractions;
using Plate.CrossMilo.Contracts;
using Plate.CrossMilo.Contracts.Hosting.App;
using Plate.PluginManoi.Contracts;
using WingedBean.Plugins.ArchECS;
// Note: IDungeonService replaces Game.Dungeon.IService, RenderService removed
// Note: ECS is now internal to ArchECS plugin, registered as ArchECSService

// Type aliases for the new IService pattern
using ITerminalApp = Plate.CrossMilo.Contracts.Terminal.ITerminalApp;
using IUIApp = Plate.CrossMilo.Contracts.UI.App.IService;
using IWingedBeanApp = Plate.CrossMilo.Contracts.Hosting.App.IService;

namespace Dungeon.Host.Console;
public static class RegistryHelper
{
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

            // WingedBean gameplay contracts
            case "WingedBean.Contracts.Gameplay.IService":
                registry.Register((WingedBean.Contracts.Gameplay.IService)instance, priority);
                return;

            case "WingedBean.Contracts.Gameplay.IInventoryService":
                registry.Register((WingedBean.Contracts.Gameplay.IInventoryService)instance, priority);
                return;

            case "WingedBean.Contracts.Gameplay.ICombatService":
                registry.Register((WingedBean.Contracts.Gameplay.ICombatService)instance, priority);
                return;

            case "WingedBean.Contracts.Gameplay.IQuestService":
                registry.Register((WingedBean.Contracts.Gameplay.IQuestService)instance, priority);
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

            // ECS service is now internal to ArchECS plugin
            case "WingedBean.Plugins.ArchECS.ArchECSService":
            case "Plate.CrossMilo.Contracts.ECS.Services.IService":  // Legacy - map to ArchECSService
            case "WingedBean.Contracts.ECS.IECSService":  // Legacy
                registry.Register((ArchECSService)instance, priority);
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

            case "HostLand.Abstractions.IHost":
            case "WingedBean.Hosting.Abstractions.IWingedBeanHost":  // Legacy
            case "Plate.CrossMilo.Contracts.Hosting.Host.IService":  // Legacy
            case "WingedBean.Contracts.Hosting.IWingedBeanHost":  // Legacy
                registry.Register((IHost)instance, priority);
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
