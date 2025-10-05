namespace WingedBean.PluginSystem;

/// <summary>
/// Event arguments for plugin update availability
/// </summary>
public class PluginUpdateAvailableEventArgs : EventArgs
{
    public string PluginId { get; set; } = string.Empty;
    public string CurrentVersion { get; set; } = string.Empty;
    public string AvailableVersion { get; set; } = string.Empty;
    public bool IsAutoUpdateEnabled { get; set; }
    public PluginUpdateInfo UpdateInfo { get; set; } = new();
}
