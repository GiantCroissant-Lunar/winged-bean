using UnityEngine;
using UnityEngine.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WingedBean.Host;
using WingedBean.Host.Unity;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace WingedBean.Unity.Demo
{
    /// <summary>
    /// Unity demo showcasing the WingedBean plugin system with hot-reload capabilities
    /// </summary>
    public class PluginSystemDemo : MonoBehaviour
    {
        [Header("UI Components")]
        public Button loadPluginButton;
        public Button reloadPluginButton;
        public Button unloadPluginButton;
        public Text statusText;
        public Text pluginListText;
        public InputField pluginPathInput;
        
        [Header("Plugin Settings")]
        public string pluginsDirectory = "Assets/Plugins";
        public bool enableHotReload = true;
        public bool preserveStateOnReload = true;

        private HybridClrPluginLoader _pluginLoader;
        private UnityPluginHotReloadManager _hotReloadManager;
        private IPluginUpdateManager _updateManager;
        private IPluginRegistry _registry;
        private IServiceProvider _serviceProvider;
        private ILogger<PluginSystemDemo> _logger;

        /// <summary>
        /// Initialize the plugin system demo
        /// </summary>
        private async void Start()
        {
            try
            {
                UpdateStatus("Initializing plugin system...");

                // Setup logging
                var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddDebug();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
                _logger = loggerFactory.CreateLogger<PluginSystemDemo>();

                // Initialize plugin system components
                await InitializePluginSystemAsync();

                // Setup UI event handlers
                SetupUIHandlers();

                // Load initial plugins
                await LoadInitialPluginsAsync();

                UpdateStatus("Plugin system ready!");
                _logger.LogInformation("WingedBean Unity Plugin System Demo initialized successfully");
            }
            catch (System.Exception ex)
            {
                UpdateStatus($"Initialization failed: {ex.Message}");
                _logger?.LogError(ex, "Failed to initialize plugin system demo");
            }
        }

        /// <summary>
        /// Initialize the plugin system components
        /// </summary>
        private async Task InitializePluginSystemAsync()
        {
            // Create service collection
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Information));

            // Initialize plugin loader
            _pluginLoader = new HybridClrPluginLoader(_logger);

            // Initialize plugin registry
            var registryPath = Path.Combine(Application.persistentDataPath, "plugin-registry.json");
            _registry = new FilePluginRegistry(registryPath);

            // Initialize update manager
            _updateManager = new PluginUpdateManager(_registry, _pluginLoader, null);

            // Initialize hot-reload manager
            _hotReloadManager = new UnityPluginHotReloadManager(_pluginLoader, _updateManager, null);

            // Register services
            services.AddSingleton(_pluginLoader);
            services.AddSingleton(_registry);
            services.AddSingleton(_updateManager);
            services.AddSingleton(_hotReloadManager);

            // Build service provider
            _serviceProvider = services.BuildServiceProvider();

            _logger.LogInformation("Plugin system components initialized");
        }

        /// <summary>
        /// Setup UI event handlers
        /// </summary>
        private void SetupUIHandlers()
        {
            if (loadPluginButton != null)
            {
                loadPluginButton.onClick.AddListener(() => LoadPluginAsync());
            }

            if (reloadPluginButton != null)
            {
                reloadPluginButton.onClick.AddListener(() => ReloadPluginAsync());
            }

            if (unloadPluginButton != null)
            {
                unloadPluginButton.onClick.AddListener(() => UnloadPluginAsync());
            }

            // Set default plugin path
            if (pluginPathInput != null)
            {
                pluginPathInput.text = Path.Combine(Application.streamingAssetsPath, "Plugins");
            }
        }

        /// <summary>
        /// Load initial plugins from the plugins directory
        /// </summary>
        private async Task LoadInitialPluginsAsync()
        {
            try
            {
                var pluginDir = Path.Combine(Application.streamingAssetsPath, pluginsDirectory);
                if (!Directory.Exists(pluginDir))
                {
                    Directory.CreateDirectory(pluginDir);
                    _logger.LogInformation("Created plugins directory: {PluginDir}", pluginDir);
                }

                // Discover and load plugins
                var discovery = new PluginDiscovery(pluginDir);
                var manifests = await discovery.DiscoverPluginsAsync();

                _logger.LogInformation("Discovered {PluginCount} plugins", manifests.Count());

                foreach (var manifest in manifests)
                {
                    try
                    {
                        var plugin = await _pluginLoader.LoadPluginAsync(manifest);
                        await _registry.RegisterPluginAsync(manifest);
                        
                        _logger.LogInformation("Loaded plugin: {PluginId} v{Version}", manifest.Id, manifest.Version);
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError(ex, "Failed to load plugin: {PluginId}", manifest.Id);
                    }
                }

                UpdatePluginList();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to load initial plugins");
                UpdateStatus($"Failed to load initial plugins: {ex.Message}");
            }
        }

        /// <summary>
        /// Load a plugin from the specified path
        /// </summary>
        private async void LoadPluginAsync()
        {
            try
            {
                var pluginPath = pluginPathInput?.text ?? "";
                if (string.IsNullOrEmpty(pluginPath))
                {
                    UpdateStatus("Please specify a plugin path");
                    return;
                }

                UpdateStatus($"Loading plugin from: {pluginPath}");

                // Look for plugin manifest
                var manifestPath = Path.Combine(pluginPath, "plugin.json");
                if (!File.Exists(manifestPath))
                {
                    UpdateStatus($"Plugin manifest not found: {manifestPath}");
                    return;
                }

                // Load and parse manifest
                var manifestJson = await File.ReadAllTextAsync(manifestPath);
                var manifest = System.Text.Json.JsonSerializer.Deserialize<PluginManifest>(manifestJson);
                
                if (manifest == null)
                {
                    UpdateStatus("Failed to parse plugin manifest");
                    return;
                }

                // Load the plugin
                var plugin = await _pluginLoader.LoadPluginAsync(manifest);
                await _registry.RegisterPluginAsync(manifest);

                // Activate the plugin
                await plugin.ActivateAsync(_serviceProvider);

                UpdateStatus($"Successfully loaded and activated plugin: {manifest.Name}");
                UpdatePluginList();

                _logger.LogInformation("Manually loaded plugin: {PluginId} v{Version}", manifest.Id, manifest.Version);
            }
            catch (System.Exception ex)
            {
                UpdateStatus($"Failed to load plugin: {ex.Message}");
                _logger?.LogError(ex, "Failed to manually load plugin");
            }
        }

        /// <summary>
        /// Hot-reload the selected plugin
        /// </summary>
        private async void ReloadPluginAsync()
        {
            try
            {
                if (_pluginLoader.LoadedPlugins.Count == 0)
                {
                    UpdateStatus("No plugins loaded to reload");
                    return;
                }

                // For demo purposes, reload the first plugin
                var firstPlugin = _pluginLoader.LoadedPlugins.Values.First();
                
                UpdateStatus($"Hot-reloading plugin: {firstPlugin.Id}");

                var success = await _hotReloadManager.HotReloadPluginAsync(
                    firstPlugin.Id, 
                    preserveStateOnReload);

                if (success)
                {
                    UpdateStatus($"Successfully hot-reloaded plugin: {firstPlugin.Id}");
                    _logger.LogInformation("Hot-reloaded plugin: {PluginId}", firstPlugin.Id);
                }
                else
                {
                    UpdateStatus($"Failed to hot-reload plugin: {firstPlugin.Id}");
                }

                UpdatePluginList();
            }
            catch (System.Exception ex)
            {
                UpdateStatus($"Hot-reload failed: {ex.Message}");
                _logger?.LogError(ex, "Hot-reload failed");
            }
        }

        /// <summary>
        /// Unload the selected plugin
        /// </summary>
        private async void UnloadPluginAsync()
        {
            try
            {
                if (_pluginLoader.LoadedPlugins.Count == 0)
                {
                    UpdateStatus("No plugins loaded to unload");
                    return;
                }

                // For demo purposes, unload the first plugin
                var firstPlugin = _pluginLoader.LoadedPlugins.Values.First();
                
                UpdateStatus($"Unloading plugin: {firstPlugin.Id}");

                await _pluginLoader.UnloadPluginAsync(firstPlugin);

                UpdateStatus($"Successfully unloaded plugin: {firstPlugin.Id}");
                UpdatePluginList();

                _logger.LogInformation("Unloaded plugin: {PluginId}", firstPlugin.Id);
            }
            catch (System.Exception ex)
            {
                UpdateStatus($"Failed to unload plugin: {ex.Message}");
                _logger?.LogError(ex, "Failed to unload plugin");
            }
        }

        /// <summary>
        /// Update the plugin list display
        /// </summary>
        private void UpdatePluginList()
        {
            if (pluginListText == null) return;

            var pluginInfo = new List<string>();
            
            foreach (var plugin in _pluginLoader.LoadedPlugins.Values)
            {
                var info = $"• {plugin.Id} v{plugin.Version} ({plugin.State})";
                
                if (plugin is LoadedUnityPlugin unityPlugin)
                {
                    info += $" [Components: {unityPlugin.MonoBehaviourComponents.Count}]";
                }
                
                pluginInfo.Add(info);
            }

            pluginListText.text = pluginInfo.Count > 0 
                ? string.Join("\n", pluginInfo)
                : "No plugins loaded";
        }

        /// <summary>
        /// Update the status display
        /// </summary>
        private void UpdateStatus(string status)
        {
            if (statusText != null)
            {
                statusText.text = $"[{System.DateTime.Now:HH:mm:ss}] {status}";
            }

            Debug.Log($"PluginSystemDemo: {status}");
        }

        /// <summary>
        /// Unity Update loop - check for plugin updates
        /// </summary>
        private void Update()
        {
            // Check for plugin updates periodically
            if (enableHotReload && Time.frameCount % 300 == 0) // Every 5 seconds at 60fps
            {
                CheckForUpdatesAsync();
            }
        }

        /// <summary>
        /// Check for plugin updates
        /// </summary>
        private async void CheckForUpdatesAsync()
        {
            try
            {
                foreach (var plugin in _pluginLoader.LoadedPlugins.Values)
                {
                    var hasUpdates = await _updateManager.CheckForUpdatesAsync(plugin.Id);
                    if (hasUpdates)
                    {
                        _logger.LogInformation("Update available for plugin: {PluginId}", plugin.Id);
                        
                        if (enableHotReload)
                        {
                            // Auto-reload if enabled
                            await _hotReloadManager.HotReloadPluginAsync(plugin.Id, preserveStateOnReload);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                _logger?.LogError(ex, "Error checking for plugin updates");
            }
        }

        /// <summary>
        /// Cleanup on destroy
        /// </summary>
        private void OnDestroy()
        {
            try
            {
                _hotReloadManager?.Dispose();
                _pluginLoader?.Dispose();
                _serviceProvider?.Dispose();

                _logger?.LogInformation("Plugin system demo cleaned up");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error during cleanup: {ex.Message}");
            }
        }

        /// <summary>
        /// GUI for runtime plugin management (for testing without UI components)
        /// </summary>
        private void OnGUI()
        {
            if (loadPluginButton == null) // Show GUI only if UI components are not assigned
            {
                GUILayout.BeginArea(new Rect(10, 10, 300, 200));
                GUILayout.Label("WingedBean Plugin System Demo", GUI.skin.box);
                
                if (GUILayout.Button("Load Plugin"))
                {
                    LoadPluginAsync();
                }
                
                if (GUILayout.Button("Hot Reload Plugin"))
                {
                    ReloadPluginAsync();
                }
                
                if (GUILayout.Button("Unload Plugin"))
                {
                    UnloadPluginAsync();
                }

                GUILayout.Label($"Loaded Plugins: {_pluginLoader?.LoadedPlugins.Count ?? 0}");
                GUILayout.EndArea();
            }
        }
    }
}
