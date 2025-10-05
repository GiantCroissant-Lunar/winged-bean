namespace WingedBean.PluginSystem;

/// <summary>
/// Network access permissions
/// </summary>
public class NetworkPermissions
{
    /// <summary>Can make outbound HTTP requests</summary>
    public bool CanHttpClient { get; set; } = true;

    /// <summary>Can create server sockets</summary>
    public bool CanListen { get; set; } = false;

    /// <summary>Allowed hosts for outbound connections</summary>
    public List<string> AllowedHosts { get; set; } = new();

    /// <summary>Allowed ports for connections</summary>
    public List<int> AllowedPorts { get; set; } = new();
}
