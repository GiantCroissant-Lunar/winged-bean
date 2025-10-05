namespace WingedBean.PluginSystem;

/// <summary>
/// Default implementation of plugin permission enforcement
/// </summary>
public class DefaultPluginPermissionEnforcer : IPluginPermissionEnforcer
{
    private readonly Dictionary<string, PluginPermissions> _pluginPermissions = new();

    public bool HasPermission(string pluginId, string operation, object? context = null)
    {
        if (!_pluginPermissions.TryGetValue(pluginId, out var permissions))
            return false; // No permissions registered = no access

        return operation switch
        {
            "filesystem.read" => permissions.FileSystem.CanRead,
            "filesystem.write" => permissions.FileSystem.CanWrite,
            "filesystem.delete" => permissions.FileSystem.CanDelete,
            "network.http" => permissions.Network.CanHttpClient,
            "network.listen" => permissions.Network.CanListen,
            "process.spawn" => permissions.Process.CanSpawn,
            "process.inspect" => permissions.Process.CanInspect,
            "system.environment" => permissions.System.CanAccessEnvironment,
            "system.info" => permissions.System.CanAccessSystemInfo,
            "system.modify" => permissions.System.CanModifySystem,
            _ => permissions.Custom.GetValueOrDefault(operation, false)
        };
    }

    public void EnforcePermission(string pluginId, string operation, object? context = null)
    {
        if (!HasPermission(pluginId, operation, context))
            throw new UnauthorizedAccessException($"Plugin {pluginId} does not have permission for operation: {operation}");
    }

    public void RegisterPermissions(string pluginId, PluginPermissions permissions)
    {
        _pluginPermissions[pluginId] = permissions;
    }
}
