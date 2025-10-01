namespace WingedBean.Host;

/// <summary>
/// Represents the state of a plugin during its lifecycle
/// </summary>
public enum PluginState
{
    /// <summary>Plugin has been discovered but not yet loaded</summary>
    Discovered,

    /// <summary>Plugin is currently being loaded</summary>
    Loading,

    /// <summary>Plugin has been loaded but not activated</summary>
    Loaded,

    /// <summary>Plugin is currently being activated</summary>
    Activating,

    /// <summary>Plugin has been activated and is running</summary>
    Activated,

    /// <summary>Plugin is currently being deactivated</summary>
    Deactivating,

    /// <summary>Plugin has been deactivated but still loaded</summary>
    Deactivated,

    /// <summary>Plugin is currently being unloaded</summary>
    Unloading,

    /// <summary>Plugin has been unloaded and removed from memory</summary>
    Unloaded,

    /// <summary>Plugin has failed to load or activate</summary>
    Failed
}
