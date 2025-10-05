namespace WingedBean.PluginSystem;

/// <summary>
/// Event arguments for plugin update operations
/// </summary>
public class PluginUpdateEventArgs : EventArgs
{
    public string PluginId { get; set; } = string.Empty;
    public string FromVersion { get; set; } = string.Empty;
    public string ToVersion { get; set; } = string.Empty;
    public PluginUpdateType UpdateType { get; set; }
}
