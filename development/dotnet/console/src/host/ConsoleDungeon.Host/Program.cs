using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using WingedBean.Contracts.Core;
using WingedBean.Registry;
using WingedBean.PluginLoader;
using WingedBean.Providers.AssemblyContext;

namespace ConsoleDungeon.Host;

/// <summary>
/// Console host entry point with dynamic plugin loading.
/// Initializes Registry, loads plugins from configuration, and launches ConsoleDungeon app.
/// </summary>
public class Program
{
    public static async System.Threading.Tasks.Task Main(string[] args)
    {
        System.Console.WriteLine("========================================");
        System.Console.WriteLine("ConsoleDungeon.Host - Dynamic Plugin Mode");
        System.Console.WriteLine("========================================");
        System.Console.WriteLine();

        try
        {
            // Step 1: Create foundation services
            System.Console.WriteLine("[1/5] Initializing foundation services...");
            var registry = new ActualRegistry();
            var contextProvider = new AssemblyContextProvider();
            var pluginLoader = new ActualPluginLoader(contextProvider);

            registry.Register<IRegistry>(registry);
            registry.Register<IPluginLoader>(pluginLoader);
            System.Console.WriteLine("✓ Foundation services initialized");
            System.Console.WriteLine();

            // Step 2: Load plugin configuration
            System.Console.WriteLine("[2/5] Loading plugin configuration...");
            var config = await LoadPluginConfigurationAsync("plugins.json");
            var enabledPlugins = config.Plugins
                .Where(p => p.Enabled)
                .OrderByDescending(p => p.Priority)
                .ToList();
            System.Console.WriteLine($"✓ Found {enabledPlugins.Count} enabled plugins");
            System.Console.WriteLine();

            // Step 3: Load plugins dynamically
            System.Console.WriteLine("[3/5] Loading plugins...");
            var loadedPlugins = new Dictionary<string, ILoadedPlugin>();

            foreach (var descriptor in enabledPlugins)
            {
                if (descriptor.LoadStrategy != LoadStrategy.Eager)
                {
                    System.Console.WriteLine($"  ⊘ Skipping {descriptor.Id} (strategy: {descriptor.LoadStrategy})");
                    continue;
                }

                System.Console.WriteLine($"  → Loading: {descriptor.Id} (priority: {descriptor.Priority})");

                try
                {
                    var plugin = await pluginLoader.LoadAsync(descriptor.Path);
                    loadedPlugins[descriptor.Id] = plugin;

                    System.Console.WriteLine($"    ✓ Loaded: {plugin.Manifest.Id} v{plugin.Manifest.Version}");

                    // Auto-register services from plugin
                    await RegisterPluginServicesAsync(registry, plugin, descriptor.Priority);
                }
                catch (System.Exception ex)
                {
                    System.Console.WriteLine($"    ✗ Failed to load {descriptor.Id}: {ex.Message}");

                    // Check if plugin is critical
                    if (descriptor.Priority >= 1000)
                    {
                        System.Console.WriteLine($"    CRITICAL: Plugin {descriptor.Id} failed to load. Aborting.");
                        return;
                    }
                }
            }

            System.Console.WriteLine($"✓ {loadedPlugins.Count} plugins loaded successfully");
            System.Console.WriteLine();

            // Step 4: Verify required services
            System.Console.WriteLine("[4/5] Verifying service registry...");
            System.Console.WriteLine($"  IRegistry registered: {registry.IsRegistered<IRegistry>()}");
            System.Console.WriteLine($"  IPluginLoader registered: {registry.IsRegistered<IPluginLoader>()}");
            System.Console.WriteLine();

            // Step 5: Launch ConsoleDungeon with Registry
            System.Console.WriteLine("[5/5] Launching ConsoleDungeon...");
            System.Console.WriteLine();

            var app = new ConsoleDungeon.Program(registry);
            await app.RunAsync();
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"\n❌ FATAL ERROR: {ex.Message}");
            System.Console.WriteLine($"Stack trace:\n{ex.StackTrace}");
            System.Environment.Exit(1);
        }
    }

    private static async System.Threading.Tasks.Task<PluginConfiguration> LoadPluginConfigurationAsync(string path)
    {
        if (!System.IO.File.Exists(path))
        {
            throw new System.IO.FileNotFoundException($"Plugin configuration file not found: {path}");
        }

        var json = await System.IO.File.ReadAllTextAsync(path);
        var config = JsonSerializer.Deserialize<PluginConfiguration>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        });

        return config ?? throw new System.InvalidOperationException("Failed to parse plugin configuration");
    }

    private static System.Threading.Tasks.Task RegisterPluginServicesAsync(
        IRegistry registry,
        ILoadedPlugin plugin,
        int priority)
    {
        var services = plugin.GetServices();

        foreach (var service in services)
        {
            var serviceType = service.GetType().GetInterfaces()
                .FirstOrDefault(i => i.Namespace?.StartsWith("WingedBean.Contracts") == true);

            if (serviceType != null)
            {
                // Use reflection to call the generic Register method
                var registerMethod = typeof(IRegistry).GetMethod("Register")
                    ?.MakeGenericMethod(serviceType);
                
                registerMethod?.Invoke(registry, new object[] { service, priority });
                
                System.Console.WriteLine($"    ✓ {serviceType.Name} <- {service.GetType().Name} (priority: {priority})");
            }
        }

        return System.Threading.Tasks.Task.CompletedTask;
    }
}
