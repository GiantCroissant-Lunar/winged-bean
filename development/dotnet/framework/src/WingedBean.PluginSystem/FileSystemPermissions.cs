namespace WingedBean.PluginSystem;

/// <summary>
/// File system access permissions
/// </summary>
public class FileSystemPermissions
{
    /// <summary>Can read files</summary>
    public bool CanRead { get; set; } = true;

    /// <summary>Can write files</summary>
    public bool CanWrite { get; set; } = false;

    /// <summary>Can delete files</summary>
    public bool CanDelete { get; set; } = false;

    /// <summary>Allowed directories for access</summary>
    public List<string> AllowedPaths { get; set; } = new();

    /// <summary>Denied directories</summary>
    public List<string> DeniedPaths { get; set; } = new();
}
