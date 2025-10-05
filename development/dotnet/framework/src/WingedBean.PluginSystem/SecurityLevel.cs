namespace WingedBean.PluginSystem;

/// <summary>
/// Security enforcement levels
/// </summary>
public enum SecurityLevel
{
    /// <summary>Minimal restrictions, full system access</summary>
    Unrestricted,

    /// <summary>Standard restrictions, safe for most plugins</summary>
    Standard,

    /// <summary>High restrictions, sandboxed execution</summary>
    Restricted,

    /// <summary>Maximum restrictions, very limited access</summary>
    Isolated
}
