namespace WingedBean.PluginSystem;

/// <summary>
/// Event arguments for plugin update errors
/// </summary>
public class PluginUpdateErrorEventArgs : PluginUpdateEventArgs
{
    public Exception Exception { get; set; } = new();
    public string ErrorMessage { get; set; } = string.Empty;
}
