namespace WingedBean.Plugins.Resource;

/// <summary>
/// Represents the current state of a resource.
/// </summary>
public enum ResourceLoadState
{
    /// <summary>
    /// Resource is not loaded and no load operation is in progress.
    /// </summary>
    Unloaded,

    /// <summary>
    /// Resource is currently being loaded.
    /// Subsequent load requests will wait for this operation to complete.
    /// </summary>
    Loading,

    /// <summary>
    /// Resource is fully loaded and available.
    /// </summary>
    Loaded,

    /// <summary>
    /// Resource loading failed.
    /// GetAsync will return null.
    /// </summary>
    Failed
}
