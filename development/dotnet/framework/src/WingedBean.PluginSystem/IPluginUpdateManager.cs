namespace WingedBean.PluginSystem;

/// <summary>
/// Plugin update manager for handling hot updates and rollbacks
/// </summary>
public interface IPluginUpdateManager
{
    /// <summary>Check for available updates for a plugin</summary>
    Task<PluginUpdateInfo?> CheckForUpdatesAsync(string pluginId, CancellationToken ct = default);

    /// <summary>Update a plugin to a new version</summary>
    Task<bool> UpdatePluginAsync(string pluginId, string targetVersion, CancellationToken ct = default);

    /// <summary>Rollback a plugin to previous version</summary>
    Task<bool> RollbackPluginAsync(string pluginId, CancellationToken ct = default);

    /// <summary>Get update history for a plugin</summary>
    Task<IEnumerable<PluginUpdateRecord>> GetUpdateHistoryAsync(string pluginId, CancellationToken ct = default);

    /// <summary>Enable/disable automatic updates for a plugin</summary>
    Task SetAutoUpdateAsync(string pluginId, bool enabled, CancellationToken ct = default);

    /// <summary>Event triggered when plugin update is available</summary>
    event EventHandler<PluginUpdateAvailableEventArgs> UpdateAvailable;

    /// <summary>Event triggered when plugin update starts</summary>
    event EventHandler<PluginUpdateEventArgs> UpdateStarted;

    /// <summary>Event triggered when plugin update completes</summary>
    event EventHandler<PluginUpdateEventArgs> UpdateCompleted;

    /// <summary>Event triggered when plugin update fails</summary>
    event EventHandler<PluginUpdateErrorEventArgs> UpdateFailed;
}
