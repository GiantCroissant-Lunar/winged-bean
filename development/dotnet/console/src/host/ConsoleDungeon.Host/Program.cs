using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using WingedBean.Contracts;
using WingedBean.Contracts.Core;
using WingedBean.Contracts.Game;
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
            var exeDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? System.Environment.CurrentDirectory;
            var configPath = System.IO.Path.Combine(exeDirectory, "plugins.json");
            var config = await LoadPluginConfigurationAsync(configPath);
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
                    // Resolve plugin path relative to executable directory
                    var pluginPath = System.IO.Path.IsPathRooted(descriptor.Path) 
                        ? descriptor.Path 
                        : System.IO.Path.Combine(exeDirectory, descriptor.Path);
                    var plugin = await pluginLoader.LoadAsync(pluginPath);
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
            VerifyRequiredServices(registry);
            System.Console.WriteLine("✓ All required services registered");
            System.Console.WriteLine();

            // Step 5: Launch Terminal App from plugins (RFC-0017)
            System.Console.WriteLine("[5/5] Launching Terminal App from plugins...");
            System.Console.WriteLine();

            if (!registry.IsRegistered<ITerminalApp>())
            {
                throw new System.InvalidOperationException("No ITerminalApp registered. Ensure a UI plugin (e.g., ConsoleDungeonApp) is loaded.");
            }

            var terminalApp = registry.Get<ITerminalApp>();
            var appConfig = new TerminalAppConfig
            {
                Name = "Console Dungeon",
                Cols = 80,
                Rows = 24
            };

            // Provide registry and gameplay service to UI (RFC-0018)
            appConfig.Parameters["registry"] = registry;
            
            if (registry.IsRegistered<IDungeonGameService>())
            {
                var gameService = registry.Get<IDungeonGameService>();
                appConfig.Parameters["gameService"] = gameService;
            }
            await terminalApp.StartAsync(appConfig);
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

    private static async System.Threading.Tasks.Task RegisterPluginServicesAsync(
        IRegistry registry,
        ILoadedPlugin plugin,
        int priority)
    {
        // Set registry on plugin BEFORE activation (required for OnActivateAsync)
        // Use reflection to call SetRegistry if it exists (LoadedPluginWrapper has this method)
        var setRegistryMethod = plugin.GetType().GetMethod("SetRegistry", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (setRegistryMethod != null)
        {
            setRegistryMethod.Invoke(plugin, new object[] { registry });
        }
        
        // Activate plugin (if it implements IPlugin)
        await plugin.ActivateAsync();

        // Get all services from plugin
        var services = plugin.GetServices();

        foreach (var service in services)
        {
            // Find the contract interface (in WingedBean.Contracts.* namespace)
            var serviceType = service.GetType().GetInterfaces()
                .FirstOrDefault(i => i.Namespace?.StartsWith("WingedBean.Contracts") == true);

            if (serviceType != null)
            {
                // Use reflection to call the generic Register method
                // We need to specify parameter types to disambiguate overloads
                var registerMethod = typeof(IRegistry).GetMethods()
                    .Where(m => m.Name == "Register" && m.IsGenericMethod)
                    .Where(m => m.GetParameters().Length == 2)
                    .Where(m => m.GetParameters()[0].ParameterType.IsGenericParameter)
                    .Where(m => m.GetParameters()[1].ParameterType == typeof(int))
                    .FirstOrDefault()
                    ?.MakeGenericMethod(serviceType);
                
                registerMethod?.Invoke(registry, new object[] { service, priority });
                
                System.Console.WriteLine($"      → Registered: {serviceType.Name} (priority: {priority})");
            }
            else
            {
                System.Console.WriteLine($"      ⚠ Warning: Service {service.GetType().Name} has no contract interface");
            }
        }
    }

    private static void VerifyRequiredServices(IRegistry registry)
    {
        // Verify foundation services
        if (!registry.IsRegistered<IRegistry>())
        {
            throw new System.InvalidOperationException("IRegistry is not registered");
        }
        if (!registry.IsRegistered<IPluginLoader>())
        {
            throw new System.InvalidOperationException("IPluginLoader is not registered");
        }

        System.Console.WriteLine("  ✓ IRegistry registered");
        System.Console.WriteLine("  ✓ IPluginLoader registered");
    }
}
