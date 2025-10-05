using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using WingedBean.Contracts.Terminal;

namespace WingedBean.Hosting;

/// <summary>
/// Adapter that wraps a legacy ITerminalApp (pre-IHostedService)
/// to work with .NET Generic Host.
/// </summary>
public class LegacyTerminalAppAdapter : IHostedService
{
    private readonly ITerminalApp _terminalApp;
    private readonly TerminalAppConfig _config;

    public LegacyTerminalAppAdapter(ITerminalApp terminalApp, IOptions<TerminalAppConfig> config)
    {
        _terminalApp = terminalApp;
        _config = config.Value;
    }

    private static ITerminalApp UnwrapTerminalApp(ITerminalApp app)
    {
        try
        {
            // Handle LazyTerminalAppResolver (internal type in host Program)
            var getApp = app.GetType().GetMethod(
                "GetApp",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (getApp != null)
            {
                var inner = getApp.Invoke(app, Array.Empty<object?>()) as ITerminalApp;
                return inner ?? app;
            }
        }
        catch
        {
            // ignore and fall back to original app
        }
        return app;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try { System.Console.WriteLine("[LegacyTerminalAppAdapter] StartAsync invoked"); } catch { }
        var target = UnwrapTerminalApp(_terminalApp);

        // Attempt to find legacy StartAsync(config, ct) across AssemblyLoadContexts
        // by matching parameter FullName instead of exact Type identity.
        var methods = target.GetType().GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        
        // Debug: log all public methods
        try
        {
            var allMethods = methods.Where(m => m.Name.Contains("Start")).ToList();
            System.Console.WriteLine($"[LegacyTerminalAppAdapter] Found {allMethods.Count} Start* methods:");
            foreach (var m in allMethods)
            {
                var ps = m.GetParameters();
                var sig = string.Join(", ", ps.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                System.Console.WriteLine($"  - {m.Name}({sig})");
            }
        }
        catch { }
        
        // Look for legacy StartWithConfigAsync(TerminalAppConfig, CancellationToken) method
        var legacy = methods.FirstOrDefault(m =>
        {
            if (m.Name != "StartWithConfigAsync") return false;
            var ps = m.GetParameters();
            if (ps.Length != 2) return false;
            return ps[0].ParameterType.FullName == "WingedBean.Contracts.Terminal.TerminalAppConfig" &&
                   ps[1].ParameterType.FullName == typeof(CancellationToken).FullName;
        });

        if (legacy != null)
        {
            try { System.Console.WriteLine("[LegacyTerminalAppAdapter] Found legacy StartWithConfigAsync(config, ct); creating config instance in target ALC"); } catch { }
            // Create a TerminalAppConfig instance in the target's AssemblyLoadContext and copy properties
            var cfgType = legacy.GetParameters()[0].ParameterType;
            try
            {
                var cfg = Activator.CreateInstance(cfgType)!;
                // Copy a minimal set of fields/properties by name
                void CopyProp(string name, object? value)
                {
                    var p = cfgType.GetProperty(name);
                    if (p != null && p.CanWrite)
                        p.SetValue(cfg, value);
                    else
                    {
                        var f = cfgType.GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        if (f != null) f.SetValue(cfg, value);
                    }
                }
                try
                {
                    CopyProp("Name", _config?.Name);
                    CopyProp("Cols", _config?.Cols ?? 80);
                    CopyProp("Rows", _config?.Rows ?? 24);
                    CopyProp("WorkingDirectory", _config?.WorkingDirectory);
                    // For dictionaries, we’ll pass null if we can’t easily bridge ALC
                    // The app handles null/empty safely.
                }
                catch { }

                try { System.Console.WriteLine("[LegacyTerminalAppAdapter] Invoking legacy StartWithConfigAsync(config, ct) on underlying app"); } catch { }
                var task = (Task)legacy.Invoke(target, new object[] { cfg, cancellationToken })!;
                return task;
            }
            catch (Exception ex)
            {
                try { System.Console.WriteLine($"[LegacyTerminalAppAdapter] Failed to invoke legacy StartAsync: {ex.Message}"); } catch { }
                // fall through to StartAsync(ct)
            }
        }

        // Fallback to new signature
        try { System.Console.WriteLine("[LegacyTerminalAppAdapter] Invoking StartAsync(ct) on underlying app"); } catch { }
        return target.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        var target = UnwrapTerminalApp(_terminalApp);
        return target.StopAsync(cancellationToken);
    }
}
