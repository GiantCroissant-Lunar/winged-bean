namespace WingedBean.PluginSystem;

/// <summary>
/// Plugin permissions defining what the plugin is allowed to do
/// </summary>
public class PluginPermissions
{
    /// <summary>File system access permissions</summary>
    public FileSystemPermissions FileSystem { get; set; } = new();

    /// <summary>Network access permissions</summary>
    public NetworkPermissions Network { get; set; } = new();

    /// <summary>Process execution permissions</summary>
    public ProcessPermissions Process { get; set; } = new();

    /// <summary>System API access permissions</summary>
    public SystemPermissions System { get; set; } = new();

    /// <summary>Custom permissions for specific capabilities</summary>
    public Dictionary<string, bool> Custom { get; set; } = new();
}
