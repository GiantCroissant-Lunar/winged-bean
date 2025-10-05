using System.Text;
using System.Text.Json;

namespace WingedBean.PluginSystem;

/// <summary>
/// Plugin security context and verification
/// </summary>
public class PluginSecurity
{
    /// <summary>Digital signature information</summary>
    public PluginSignature? Signature { get; set; }

    /// <summary>Plugin permissions</summary>
    public PluginPermissions Permissions { get; set; } = new();

    /// <summary>Security policy level</summary>
    public SecurityLevel SecurityLevel { get; set; } = SecurityLevel.Restricted;

    /// <summary>Trusted publisher information</summary>
    public string? TrustedPublisher { get; set; }
}
