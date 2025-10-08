using System;
using System.Linq;
using Plate.CrossMilo.Contracts.Hosting.Host;
using WingedBean.Hosting.Console;
using WingedBean.Hosting.Unity;
using WingedBean.Hosting.Godot;

// Type alias
using IWingedBeanHostBuilder = Plate.CrossMilo.Contracts.Hosting.Host.IServiceBuilder;

namespace WingedBean.Hosting;

/// <summary>
/// Factory for creating platform-appropriate hosts.
/// Auto-detects platform or allows explicit selection.
/// </summary>
public static class WingedBeanHost
{
    /// <summary>
    /// Create a host builder for the current platform.
    /// </summary>
    public static IWingedBeanHostBuilder CreateDefaultBuilder(string[] args)
    {
        // Auto-detect platform
        if (IsUnityRuntime())
            return new UnityWingedBeanHostBuilder();

        if (IsGodotRuntime())
            return new GodotWingedBeanHostBuilder();

        // Default to console
        return new ConsoleWingedBeanHostBuilder(args);
    }

    /// <summary>
    /// Create a console host builder explicitly.
    /// </summary>
    public static IWingedBeanHostBuilder CreateConsoleBuilder(string[] args)
        => new ConsoleWingedBeanHostBuilder(args);

    /// <summary>
    /// Create a Unity host builder explicitly.
    /// </summary>
    public static IWingedBeanHostBuilder CreateUnityBuilder()
        => new UnityWingedBeanHostBuilder();

    /// <summary>
    /// Create a Godot host builder explicitly.
    /// </summary>
    public static IWingedBeanHostBuilder CreateGodotBuilder()
        => new GodotWingedBeanHostBuilder();

    private static bool IsUnityRuntime()
        => AppDomain.CurrentDomain.GetAssemblies()
            .Any(a => a.GetName().Name == "UnityEngine");

    private static bool IsGodotRuntime()
        => AppDomain.CurrentDomain.GetAssemblies()
            .Any(a => a.GetName().Name == "GodotSharp");
}
