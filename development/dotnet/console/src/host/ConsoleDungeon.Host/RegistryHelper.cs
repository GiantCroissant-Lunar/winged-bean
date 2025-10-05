using System;
using System.Linq;
using System.Reflection;
using WingedBean.Contracts.Core;
using WingedBean.Contracts.Terminal;
using WingedBean.Contracts.Game;
using WingedBean.Contracts.ECS;
using WingedBean.Contracts.UI;
using WingedBean.Contracts.Hosting;

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
        switch (contractType.FullName)
        {
            case "WingedBean.Contracts.Terminal.ITerminalApp":
                registry.Register((ITerminalApp)instance, priority);
                return;
                
            case "WingedBean.Contracts.Game.IDungeonGameService":
                registry.Register((IDungeonGameService)instance, priority);
                return;
                
            case "WingedBean.Contracts.Game.IRenderService":
                registry.Register((IRenderService)instance, priority);
                return;
                
            case "WingedBean.Contracts.ECS.IECSService":
                registry.Register((IECSService)instance, priority);
                return;
                
            case "WingedBean.Contracts.UI.IUIApp":
                registry.Register((IUIApp)instance, priority);
                return;
                
            case "WingedBean.Contracts.Hosting.IWingedBeanApp":
                registry.Register((IWingedBeanApp)instance, priority);
                return;
                
            case "WingedBean.Contracts.Hosting.IWingedBeanHost":
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
