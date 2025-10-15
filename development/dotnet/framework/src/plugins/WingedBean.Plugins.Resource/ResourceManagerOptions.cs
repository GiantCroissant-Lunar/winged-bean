namespace WingedBean.Plugins.Resource;

/// <summary>
/// Configuration options for ResourceManager.
/// </summary>
public sealed class ResourceManagerOptions
{
    /// <summary>
    /// How often to check for idle resources to unload.
    /// Default: 30 seconds.
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How long a resource must be idle (zero refcount) before unloading.
    /// Default: 5 minutes.
    /// </summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Enable detailed logging for debugging.
    /// Default: false.
    /// </summary>
    public bool EnableDebugLogging { get; set; } = false;

    /// <summary>
    /// Enable automatic cleanup of idle resources.
    /// Default: true.
    /// </summary>
    public bool EnableAutoCleanup { get; set; } = true;

    /// <summary>
    /// Gets the default options.
    /// </summary>
    public static ResourceManagerOptions Default => new();
}
