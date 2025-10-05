namespace WingedBean.PluginSystem;

/// <summary>
/// Plugin permission enforcement service
/// </summary>
public interface IPluginPermissionEnforcer
{
    /// <summary>Check if plugin has permission for an operation</summary>
    bool HasPermission(string pluginId, string operation, object? context = null);

    /// <summary>Enforce permission check, throw if denied</summary>
    void EnforcePermission(string pluginId, string operation, object? context = null);

    /// <summary>Register plugin permissions</summary>
    void RegisterPermissions(string pluginId, PluginPermissions permissions);
}
