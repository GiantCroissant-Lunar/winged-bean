namespace WingedBean.PluginSystem;

/// <summary>
/// System API access permissions
/// </summary>
public class SystemPermissions
{
    /// <summary>Can access environment variables</summary>
    public bool CanAccessEnvironment { get; set; } = true;

    /// <summary>Can access system information</summary>
    public bool CanAccessSystemInfo { get; set; } = true;

    /// <summary>Can modify system settings</summary>
    public bool CanModifySystem { get; set; } = false;
}
