using Microsoft.Extensions.Logging;
using WingedBean.Host;
using System.Collections.Concurrent;

#if UNITY
using UnityEngine;
#endif

namespace WingedBean.Host.Unity;

/// <summary>
/// Unity-specific plugin hot-reload manager with component state preservation
/// </summary>
public class UnityPluginHotReloadManager
{
    private readonly HybridClrPluginLoader _pluginLoader;
    private readonly IPluginUpdateManager _updateManager;
    private readonly ILogger<UnityPluginHotReloadManager>? _logger;
    private readonly ConcurrentDictionary<string, UnityPluginState> _pluginStates = new();

    /// <summary>
    /// Initialize Unity hot-reload manager
    /// </summary>
    /// <param name="pluginLoader">Unity plugin loader</param>
    /// <param name="updateManager">Base update manager</param>
    /// <param name="logger">Logger instance</param>
    public UnityPluginHotReloadManager(
        HybridClrPluginLoader pluginLoader,
        IPluginUpdateManager updateManager,
        ILogger<UnityPluginHotReloadManager>? logger = null)
    {
        _pluginLoader = pluginLoader;
        _updateManager = updateManager;
        _logger = logger;

        // Subscribe to update events
        _updateManager.PluginUpdateAvailable += OnUpdateAvailable;
        _updateManager.PluginUpdateCompleted += OnUpdateCompleted;
        _updateManager.PluginUpdateFailed += OnUpdateFailed;
    }

    /// <summary>
    /// Perform hot-reload of Unity plugin with state preservation
    /// </summary>
    /// <param name="pluginId">Plugin to reload</param>
    /// <param name="preserveState">Whether to preserve component state</param>
    /// <param name="ct">Cancellation token</param>
    public async Task<bool> HotReloadPluginAsync(string pluginId, bool preserveState = true, CancellationToken ct = default)
    {
        _logger?.LogInformation("Starting Unity hot-reload for plugin: {PluginId}", pluginId);

        try
        {
            // Find the plugin
            if (!_pluginLoader.LoadedPlugins.TryGetValue(pluginId, out var plugin) ||
                plugin is not LoadedUnityPlugin unityPlugin)
            {
                _logger?.LogWarning("Plugin not found or not a Unity plugin: {PluginId}", pluginId);
                return false;
            }

            // Create rollback point
            await _updateManager.CreateRollbackPointAsync(pluginId);

            // Preserve state if requested
            UnityPluginState? savedState = null;
            if (preserveState)
            {
                savedState = await PreservePluginStateAsync(unityPlugin, ct);
            }

            // Perform the hot-reload
            await _pluginLoader.ReloadPluginAsync(plugin, ct);

            // Find the new plugin instance
            if (_pluginLoader.LoadedPlugins.TryGetValue(pluginId, out var newPlugin) &&
                newPlugin is LoadedUnityPlugin newUnityPlugin)
            {
                // Restore state if we preserved it
                if (preserveState && savedState != null)
                {
                    await RestorePluginStateAsync(newUnityPlugin, savedState, ct);
                }

                _logger?.LogInformation("Successfully hot-reloaded Unity plugin: {PluginId}", pluginId);
                return true;
            }

            _logger?.LogError("Failed to find plugin after hot-reload: {PluginId}", pluginId);
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Hot-reload failed for Unity plugin: {PluginId}", pluginId);

            // Attempt rollback
            try
            {
                await _updateManager.RollbackAsync(pluginId);
                _logger?.LogInformation("Rolled back plugin after hot-reload failure: {PluginId}", pluginId);
            }
            catch (Exception rollbackEx)
            {
                _logger?.LogError(rollbackEx, "Rollback also failed for plugin: {PluginId}", pluginId);
            }

            return false;
        }
    }

    /// <summary>
    /// Preserve the current state of Unity plugin components
    /// </summary>
    private async Task<UnityPluginState> PreservePluginStateAsync(LoadedUnityPlugin plugin, CancellationToken ct)
    {
        _logger?.LogDebug("Preserving state for Unity plugin: {PluginId}", plugin.Id);

        var state = new UnityPluginState
        {
            PluginId = plugin.Id,
            WasActivated = plugin.State == PluginState.Activated,
            PreservationTime = DateTimeOffset.UtcNow
        };

#if UNITY
        // Preserve MonoBehaviour component states
        foreach (var component in plugin.MonoBehaviourComponents)
        {
            if (component != null)
            {
                var componentState = await PreserveComponentStateAsync(component, ct);
                if (componentState != null)
                {
                    state.ComponentStates.Add(componentState);
                }
            }
        }

        // Preserve GameObject state
        if (plugin.GameObjectContainer != null)
        {
            state.GameObjectState = PreserveGameObjectState(plugin.GameObjectContainer);
        }
#endif

        // Store the state
        _pluginStates[plugin.Id] = state;

        _logger?.LogDebug("Preserved state for {ComponentCount} components in plugin: {PluginId}",
            state.ComponentStates.Count, plugin.Id);

        return state;
    }

    /// <summary>
    /// Restore the preserved state to a reloaded Unity plugin
    /// </summary>
    private async Task RestorePluginStateAsync(LoadedUnityPlugin plugin, UnityPluginState savedState, CancellationToken ct)
    {
        _logger?.LogDebug("Restoring state for Unity plugin: {PluginId}", plugin.Id);

        try
        {
#if UNITY
            // Restore GameObject state
            if (plugin.GameObjectContainer != null && savedState.GameObjectState != null)
            {
                RestoreGameObjectState(plugin.GameObjectContainer, savedState.GameObjectState);
            }

            // Restore component states
            foreach (var componentState in savedState.ComponentStates)
            {
                await RestoreComponentStateAsync(plugin, componentState, ct);
            }
#endif

            _logger?.LogDebug("Successfully restored state for Unity plugin: {PluginId}", plugin.Id);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to fully restore state for Unity plugin: {PluginId}", plugin.Id);
            // Continue anyway - partial restoration is better than none
        }
    }

#if UNITY
    /// <summary>
    /// Preserve the state of a MonoBehaviour component
    /// </summary>
    private async Task<ComponentState?> PreserveComponentStateAsync(MonoBehaviour component, CancellationToken ct)
    {
        try
        {
            var componentState = new ComponentState
            {
                TypeName = component.GetType().FullName ?? "Unknown",
                IsEnabled = component.enabled
            };

            // If component implements IStatefulComponent, use its state preservation
            if (component is IStatefulComponent statefulComponent)
            {
                componentState.CustomState = await statefulComponent.SaveStateAsync(ct);
            }
            else
            {
                // Use reflection to preserve public fields and properties
                componentState.SerializedFields = SerializeComponentFields(component);
            }

            return componentState;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to preserve state for component: {ComponentType}", component.GetType().Name);
            return null;
        }
    }

    /// <summary>
    /// Restore state to a MonoBehaviour component
    /// </summary>
    private async Task RestoreComponentStateAsync(LoadedUnityPlugin plugin, ComponentState componentState, CancellationToken ct)
    {
        try
        {
            // Find matching component in the reloaded plugin
            var component = plugin.MonoBehaviourComponents
                .FirstOrDefault(c => c.GetType().FullName == componentState.TypeName);

            if (component == null)
            {
                _logger?.LogWarning("Could not find component to restore state: {ComponentType}", componentState.TypeName);
                return;
            }

            // Restore enabled state
            component.enabled = componentState.IsEnabled;

            // Restore component-specific state
            if (component is IStatefulComponent statefulComponent && componentState.CustomState != null)
            {
                await statefulComponent.RestoreStateAsync(componentState.CustomState, ct);
            }
            else if (componentState.SerializedFields != null)
            {
                DeserializeComponentFields(component, componentState.SerializedFields);
            }

            _logger?.LogDebug("Restored state for component: {ComponentType}", componentState.TypeName);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to restore state for component: {ComponentType}", componentState.TypeName);
        }
    }

    /// <summary>
    /// Preserve GameObject state (transform, active state, etc.)
    /// </summary>
    private GameObjectState PreserveGameObjectState(GameObject gameObject)
    {
        return new GameObjectState
        {
            Name = gameObject.name,
            IsActive = gameObject.activeInHierarchy,
            Position = gameObject.transform.position,
            Rotation = gameObject.transform.rotation,
            Scale = gameObject.transform.localScale,
            Tag = gameObject.tag,
            Layer = gameObject.layer
        };
    }

    /// <summary>
    /// Restore GameObject state
    /// </summary>
    private void RestoreGameObjectState(GameObject gameObject, GameObjectState state)
    {
        gameObject.name = state.Name;
        gameObject.SetActive(state.IsActive);
        gameObject.transform.position = state.Position;
        gameObject.transform.rotation = state.Rotation;
        gameObject.transform.localScale = state.Scale;
        gameObject.tag = state.Tag;
        gameObject.layer = state.Layer;
    }

    /// <summary>
    /// Serialize component fields using reflection
    /// </summary>
    private Dictionary<string, object?> SerializeComponentFields(MonoBehaviour component)
    {
        var fields = new Dictionary<string, object?>();
        var type = component.GetType();

        // Get serializable fields (public or with SerializeField attribute)
        var fieldInfos = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        foreach (var field in fieldInfos)
        {
            if (IsSerializableType(field.FieldType))
            {
                try
                {
                    fields[field.Name] = field.GetValue(component);
                }
                catch
                {
                    // Skip fields that can't be accessed
                }
            }
        }

        return fields;
    }

    /// <summary>
    /// Deserialize component fields using reflection
    /// </summary>
    private void DeserializeComponentFields(MonoBehaviour component, Dictionary<string, object?> fields)
    {
        var type = component.GetType();

        foreach (var kvp in fields)
        {
            try
            {
                var field = type.GetField(kvp.Key, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (field != null && IsSerializableType(field.FieldType))
                {
                    field.SetValue(component, kvp.Value);
                }
            }
            catch
            {
                // Skip fields that can't be set
            }
        }
    }

    /// <summary>
    /// Check if a type can be safely serialized for state preservation
    /// </summary>
    private bool IsSerializableType(Type type)
    {
        return type.IsPrimitive ||
               type == typeof(string) ||
               type == typeof(Vector3) ||
               type == typeof(Vector2) ||
               type == typeof(Quaternion) ||
               type == typeof(Color) ||
               type.IsEnum;
    }
#endif

    /// <summary>
    /// Handle update available event
    /// </summary>
    private void OnUpdateAvailable(object? sender, PluginUpdateEventArgs e)
    {
        _logger?.LogInformation("Unity plugin update available: {PluginId} {CurrentVersion} -> {NewVersion}",
            e.PluginId, e.CurrentVersion, e.NewVersion);

        // Could trigger automatic hot-reload here if configured
    }

    /// <summary>
    /// Handle update completed event
    /// </summary>
    private void OnUpdateCompleted(object? sender, PluginUpdateEventArgs e)
    {
        _logger?.LogInformation("Unity plugin update completed: {PluginId} -> {NewVersion}",
            e.PluginId, e.NewVersion);

        // Clean up old state
        _pluginStates.TryRemove(e.PluginId, out _);
    }

    /// <summary>
    /// Handle update failed event
    /// </summary>
    private void OnUpdateFailed(object? sender, PluginUpdateEventArgs e)
    {
        _logger?.LogError("Unity plugin update failed: {PluginId} - {Error}",
            e.PluginId, e.Error);
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        // Unsubscribe from events
        _updateManager.PluginUpdateAvailable -= OnUpdateAvailable;
        _updateManager.PluginUpdateCompleted -= OnUpdateCompleted;
        _updateManager.PluginUpdateFailed -= OnUpdateFailed;

        _pluginStates.Clear();
    }
}

/// <summary>
/// Preserved state of a Unity plugin
/// </summary>
public class UnityPluginState
{
    public string PluginId { get; set; } = string.Empty;
    public bool WasActivated { get; set; }
    public DateTimeOffset PreservationTime { get; set; }
    public List<ComponentState> ComponentStates { get; set; } = new();

#if UNITY
    public GameObjectState? GameObjectState { get; set; }
#endif
}

/// <summary>
/// Preserved state of a MonoBehaviour component
/// </summary>
public class ComponentState
{
    public string TypeName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public Dictionary<string, object?>? SerializedFields { get; set; }
    public object? CustomState { get; set; }
}

#if UNITY
/// <summary>
/// Preserved state of a GameObject
/// </summary>
public class GameObjectState
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
    public Vector3 Scale { get; set; }
    public string Tag { get; set; } = "Untagged";
    public int Layer { get; set; }
}

/// <summary>
/// Interface for components that want custom state preservation
/// </summary>
public interface IStatefulComponent
{
    Task<object?> SaveStateAsync(CancellationToken ct = default);
    Task RestoreStateAsync(object? state, CancellationToken ct = default);
}
#endif
