namespace WingedBean.PluginSystem;

/// <summary>
/// Process execution permissions
/// </summary>
public class ProcessPermissions
{
    /// <summary>Can spawn new processes</summary>
    public bool CanSpawn { get; set; } = false;

    /// <summary>Allowed executables</summary>
    public List<string> AllowedExecutables { get; set; } = new();

    /// <summary>Can access process information</summary>
    public bool CanInspect { get; set; } = false;
}
