using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WingedBean.Host;
using System.Reflection;

#if UNITY
using UnityEngine;
#endif

namespace WingedBean.Host.Unity;

/// <summary>
/// Unity-specific implementation of ILoadedPlugin with MonoBehaviour integration
/// </summary>
public class LoadedUnityPlugin : ILoadedPlugin
{
    private readonly IPluginActivator _activator;
    private readonly Assembly _assembly;
    private readonly HybridClrPluginLoader _loader;
    private PluginState _state;
    private readonly List<IDisposable> _disposables = new();

    /// <summary>
    /// Plugin manifest
    /// </summary>
    public PluginManifest Manifest { get; }

    /// <summary>
    /// Plugin ID
    /// </summary>
    public string Id => Manifest.Id;

    /// <summary>
    /// Plugin version
    /// </summary>
    public Version Version => new(Manifest.Version);

    /// <summary>
    /// Current plugin state
    /// </summary>
    public PluginState State => _state;

    /// <summary>
    /// Plugin services collection
    /// </summary>
    public IServiceCollection Services { get; private set; } = new ServiceCollection();

#if UNITY
    /// <summary>
    /// Unity GameObject container for plugin components
    /// </summary>
    public GameObject? GameObjectContainer { get; set; }

    /// <summary>
    /// MonoBehaviour components created by this plugin
    /// </summary>
    public List<MonoBehaviour> MonoBehaviourComponents { get; } = new();
#endif

    /// <summary>
    /// Initialize Unity plugin
    /// </summary>
    /// <param name="manifest">Plugin manifest</param>
    /// <param name="activator">Plugin activator</param>
    /// <param name="assembly">Plugin assembly</param>
    /// <param name="loader">Plugin loader reference</param>
    public LoadedUnityPlugin(PluginManifest manifest, IPluginActivator activator, Assembly assembly, HybridClrPluginLoader loader)
    {
        Manifest = manifest;
        _activator = activator;
        _assembly = assembly;
        _loader = loader;
        _state = PluginState.Loaded;
    }

    /// <summary>
    /// Activate the plugin with Unity-specific features
    /// </summary>
    /// <param name="hostServices">Host services provider</param>
    /// <param name="ct">Cancellation token</param>
    public async Task ActivateAsync(IServiceProvider? hostServices, CancellationToken ct = default)
    {
        if (_state != PluginState.Loaded && _state != PluginState.Deactivated)
        {
            throw new InvalidOperationException($"Cannot activate plugin in state: {_state}");
        }

        try
        {
            SetState(PluginState.Activating);

            // Create fresh service collection for this activation
            Services = new ServiceCollection();

            // Register Unity-specific services
            RegisterUnityServices();

            // Activate plugin through standard interface
            await _activator.ActivateAsync(Services, hostServices ?? EmptyServiceProvider.Instance, ct);

            // Handle Unity-specific MonoBehaviour components
#if UNITY
            await ActivateUnityComponentsAsync(ct);
#endif

            SetState(PluginState.Activated);
        }
        catch (Exception ex)
        {
            SetState(PluginState.Failed);
            throw new InvalidOperationException($"Failed to activate Unity plugin {Id}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deactivate the plugin
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    public async Task DeactivateAsync(CancellationToken ct = default)
    {
        if (_state != PluginState.Activated)
        {
            return; // Already deactivated or not activated
        }

        try
        {
            SetState(PluginState.Deactivating);

#if UNITY
            // Deactivate Unity components first
            await DeactivateUnityComponentsAsync(ct);
#endif

            // Deactivate plugin through standard interface
            await _activator.DeactivateAsync(ct);

            // Dispose any registered disposables
            foreach (var disposable in _disposables.ToArray())
            {
                try
                {
                    disposable.Dispose();
                }
                catch
                {
                    // Log but don't fail deactivation
                }
            }
            _disposables.Clear();

            SetState(PluginState.Deactivated);
        }
        catch (Exception ex)
        {
            SetState(PluginState.Failed);
            throw new InvalidOperationException($"Failed to deactivate Unity plugin {Id}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Register Unity-specific services
    /// </summary>
    private void RegisterUnityServices()
    {
#if UNITY
        // Register Unity-specific services that plugins might need
        Services.AddSingleton<IUnityPluginServices>(new UnityPluginServices(this));
        
        // Register GameObject container if available
        if (GameObjectContainer != null)
        {
            Services.AddSingleton(GameObjectContainer);
        }
#endif
    }

#if UNITY
    /// <summary>
    /// Activate Unity-specific MonoBehaviour components
    /// </summary>
    private async Task ActivateUnityComponentsAsync(CancellationToken ct)
    {
        if (GameObjectContainer == null) return;

        try
        {
            // Find MonoBehaviour types in plugin assembly
            var monoBehaviourTypes = _assembly.GetTypes()
                .Where(t => typeof(MonoBehaviour).IsAssignableFrom(t) && !t.IsAbstract)
                .ToList();

            foreach (var componentType in monoBehaviourTypes)
            {
                // Add component to GameObject
                var component = GameObjectContainer.AddComponent(componentType) as MonoBehaviour;
                if (component != null)
                {
                    MonoBehaviourComponents.Add(component);
                    
                    // Initialize component if it implements initialization interface
                    if (component is IPluginComponent pluginComponent)
                    {
                        await pluginComponent.InitializeAsync(ct);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to activate Unity components for plugin {Id}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deactivate Unity-specific MonoBehaviour components
    /// </summary>
    private async Task DeactivateUnityComponentsAsync(CancellationToken ct)
    {
        try
        {
            // Cleanup components in reverse order
            for (int i = MonoBehaviourComponents.Count - 1; i >= 0; i--)
            {
                var component = MonoBehaviourComponents[i];
                if (component != null)
                {
                    // Cleanup component if it implements cleanup interface
                    if (component is IPluginComponent pluginComponent)
                    {
                        await pluginComponent.CleanupAsync(ct);
                    }

                    // Destroy the component
                    UnityEngine.Object.DestroyImmediate(component);
                }
            }

            MonoBehaviourComponents.Clear();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to deactivate Unity components for plugin {Id}: {ex.Message}", ex);
        }
    }
#endif

    /// <summary>
    /// Set plugin state (internal use)
    /// </summary>
    internal void SetState(PluginState state)
    {
        _state = state;
    }

    /// <summary>
    /// Register a disposable resource
    /// </summary>
    public void RegisterDisposable(IDisposable disposable)
    {
        _disposables.Add(disposable);
    }

    /// <summary>
    /// Get plugin assembly
    /// </summary>
    public Assembly GetAssembly() => _assembly;

    /// <summary>
    /// Dispose plugin resources
    /// </summary>
    public void Dispose()
    {
        if (_state == PluginState.Activated)
        {
            try
            {
                DeactivateAsync().GetAwaiter().GetResult();
            }
            catch
            {
                // Best effort cleanup
            }
        }

        foreach (var disposable in _disposables)
        {
            try
            {
                disposable.Dispose();
            }
            catch
            {
                // Best effort cleanup
            }
        }

        _disposables.Clear();
    }
}

/// <summary>
/// Empty service provider for fallback scenarios
/// </summary>
internal class EmptyServiceProvider : IServiceProvider
{
    public static readonly EmptyServiceProvider Instance = new();

    public object? GetService(Type serviceType) => null;
}

#if UNITY
/// <summary>
/// Interface for Unity plugin components that need initialization/cleanup
/// </summary>
public interface IPluginComponent
{
    Task InitializeAsync(CancellationToken ct = default);
    Task CleanupAsync(CancellationToken ct = default);
}

/// <summary>
/// Unity-specific services for plugins
/// </summary>
public interface IUnityPluginServices
{
    GameObject? GetPluginGameObject();
    T? AddComponent<T>() where T : MonoBehaviour;
    void RemoveComponent<T>() where T : MonoBehaviour;
    T? FindComponent<T>() where T : MonoBehaviour;
}

/// <summary>
/// Implementation of Unity plugin services
/// </summary>
public class UnityPluginServices : IUnityPluginServices
{
    private readonly LoadedUnityPlugin _plugin;

    public UnityPluginServices(LoadedUnityPlugin plugin)
    {
        _plugin = plugin;
    }

    public GameObject? GetPluginGameObject() => _plugin.GameObjectContainer;

    public T? AddComponent<T>() where T : MonoBehaviour
    {
        if (_plugin.GameObjectContainer == null) return null;
        
        var component = _plugin.GameObjectContainer.AddComponent<T>();
        if (component != null)
        {
            _plugin.MonoBehaviourComponents.Add(component);
        }
        return component;
    }

    public void RemoveComponent<T>() where T : MonoBehaviour
    {
        if (_plugin.GameObjectContainer == null) return;

        var component = _plugin.GameObjectContainer.GetComponent<T>();
        if (component != null)
        {
            _plugin.MonoBehaviourComponents.Remove(component);
            UnityEngine.Object.DestroyImmediate(component);
        }
    }

    public T? FindComponent<T>() where T : MonoBehaviour
    {
        return _plugin.GameObjectContainer?.GetComponent<T>();
    }
}
#endif
