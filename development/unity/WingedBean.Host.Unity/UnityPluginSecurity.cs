using Microsoft.Extensions.Logging;
using WingedBean.Host;
using System.Reflection;

#if UNITY
using UnityEngine;
using UnityEngine.Networking;
#endif

namespace WingedBean.Host.Unity;

/// <summary>
/// Unity-specific plugin permission enforcer with Unity API access control
/// </summary>
public class UnityPluginPermissionEnforcer : IPluginPermissionEnforcer
{
    private readonly IPluginPermissionEnforcer _baseEnforcer;
    private readonly ILogger<UnityPluginPermissionEnforcer>? _logger;
    private readonly Dictionary<string, UnityPluginPermissions> _unityPermissions = new();

    /// <summary>
    /// Initialize Unity permission enforcer
    /// </summary>
    /// <param name="baseEnforcer">Base permission enforcer</param>
    /// <param name="logger">Logger instance</param>
    public UnityPluginPermissionEnforcer(IPluginPermissionEnforcer baseEnforcer, ILogger<UnityPluginPermissionEnforcer>? logger = null)
    {
        _baseEnforcer = baseEnforcer;
        _logger = logger;
    }

    /// <summary>
    /// Register permissions for a plugin with Unity-specific extensions
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <param name="permissions">Base plugin permissions</param>
    public void RegisterPermissions(string pluginId, PluginPermissions permissions)
    {
        // Register base permissions
        _baseEnforcer.RegisterPermissions(pluginId, permissions);

        // Extract Unity-specific permissions if present
        if (permissions is UnityPluginPermissions unityPermissions)
        {
            _unityPermissions[pluginId] = unityPermissions;
            _logger?.LogDebug("Registered Unity-specific permissions for plugin: {PluginId}", pluginId);
        }
    }

    /// <summary>
    /// Check if plugin has permission for Unity-specific operations
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <param name="permission">Permission to check</param>
    /// <returns>True if permission is granted</returns>
    public bool HasPermission(string pluginId, string permission)
    {
        // Check base permissions first
        if (_baseEnforcer.HasPermission(pluginId, permission))
        {
            return true;
        }

        // Check Unity-specific permissions
        if (_unityPermissions.TryGetValue(pluginId, out var unityPerms))
        {
            return HasUnityPermission(unityPerms, permission);
        }

        return false;
    }

    /// <summary>
    /// Enforce permission for Unity-specific operations
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <param name="permission">Permission to enforce</param>
    public void EnforcePermission(string pluginId, string permission)
    {
        if (!HasPermission(pluginId, permission))
        {
            _logger?.LogWarning("Permission denied for plugin {PluginId}: {Permission}", pluginId, permission);
            throw new UnauthorizedAccessException($"Plugin {pluginId} does not have permission: {permission}");
        }
    }

    /// <summary>
    /// Check Unity-specific permission
    /// </summary>
    private bool HasUnityPermission(UnityPluginPermissions permissions, string permission)
    {
        return permission switch
        {
            // Unity Object permissions
            "unity.gameobject.create" => permissions.Unity.CanCreateGameObjects,
            "unity.gameobject.destroy" => permissions.Unity.CanDestroyGameObjects,
            "unity.gameobject.find" => permissions.Unity.CanFindGameObjects,
            "unity.component.add" => permissions.Unity.CanAddComponents,
            "unity.component.remove" => permissions.Unity.CanRemoveComponents,

            // Scene permissions
            "unity.scene.load" => permissions.Unity.CanLoadScenes,
            "unity.scene.unload" => permissions.Unity.CanUnloadScenes,
            "unity.scene.create" => permissions.Unity.CanCreateScenes,

            // Asset permissions
            "unity.assets.load" => permissions.Unity.CanLoadAssets,
            "unity.assets.instantiate" => permissions.Unity.CanInstantiateAssets,
            "unity.resources.load" => permissions.Unity.CanAccessResources,

            // Input permissions
            "unity.input.access" => permissions.Unity.CanAccessInput,

            // Rendering permissions
            "unity.camera.access" => permissions.Unity.CanAccessCamera,
            "unity.rendering.modify" => permissions.Unity.CanModifyRendering,

            // Audio permissions
            "unity.audio.play" => permissions.Unity.CanPlayAudio,
            "unity.audio.record" => permissions.Unity.CanRecordAudio,

            // Animation permissions
            "unity.animation.control" => permissions.Unity.CanControlAnimations,

            // Physics permissions
            "unity.physics.modify" => permissions.Unity.CanModifyPhysics,

            // Editor permissions (only in editor builds)
            "unity.editor.access" => permissions.Unity.CanAccessEditor,

            _ => false
        };
    }

    /// <summary>
    /// Unregister permissions for a plugin
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    public void UnregisterPermissions(string pluginId)
    {
        _baseEnforcer.UnregisterPermissions(pluginId);
        _unityPermissions.Remove(pluginId);
    }
}

/// <summary>
/// Extended plugin permissions including Unity-specific permissions
/// </summary>
public class UnityPluginPermissions : PluginPermissions
{
    /// <summary>Unity-specific permissions</summary>
    public UnitySpecificPermissions Unity { get; set; } = new();
}

/// <summary>
/// Unity-specific permission settings
/// </summary>
public class UnitySpecificPermissions
{
    // GameObject and Component permissions
    public bool CanCreateGameObjects { get; set; } = true;
    public bool CanDestroyGameObjects { get; set; } = false;
    public bool CanFindGameObjects { get; set; } = true;
    public bool CanAddComponents { get; set; } = true;
    public bool CanRemoveComponents { get; set; } = false;
    public List<string> AllowedComponentTypes { get; set; } = new();

    // Scene management permissions
    public bool CanLoadScenes { get; set; } = false;
    public bool CanUnloadScenes { get; set; } = false;
    public bool CanCreateScenes { get; set; } = false;
    public List<string> AllowedScenes { get; set; } = new();

    // Asset and Resources permissions
    public bool CanLoadAssets { get; set; } = true;
    public bool CanInstantiateAssets { get; set; } = true;
    public bool CanAccessResources { get; set; } = true;
    public List<string> AllowedAssetPaths { get; set; } = new();

    // Input system permissions
    public bool CanAccessInput { get; set; } = true;
    public List<string> AllowedInputActions { get; set; } = new();

    // Rendering permissions
    public bool CanAccessCamera { get; set; } = false;
    public bool CanModifyRendering { get; set; } = false;
    public List<string> AllowedCameraTags { get; set; } = new();

    // Audio permissions
    public bool CanPlayAudio { get; set; } = true;
    public bool CanRecordAudio { get; set; } = false;

    // Animation permissions
    public bool CanControlAnimations { get; set; } = true;

    // Physics permissions
    public bool CanModifyPhysics { get; set; } = false;

    // Editor-only permissions
    public bool CanAccessEditor { get; set; } = false;
}

/// <summary>
/// Unity-specific security verifier for plugin assets and components
/// </summary>
public class UnityPluginSecurityVerifier
{
    private readonly ILogger<UnityPluginSecurityVerifier>? _logger;

    /// <summary>
    /// Initialize Unity security verifier
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public UnityPluginSecurityVerifier(ILogger<UnityPluginSecurityVerifier>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Verify Unity plugin security constraints
    /// </summary>
    /// <param name="manifest">Plugin manifest</param>
    /// <param name="assemblyPath">Path to plugin assembly</param>
    /// <returns>True if plugin passes Unity security checks</returns>
    public async Task<bool> VerifyUnityPluginSecurityAsync(PluginManifest manifest, string assemblyPath)
    {
        try
        {
            _logger?.LogDebug("Verifying Unity security for plugin: {PluginId}", manifest.Id);

            // Verify Unity version compatibility
            if (!VerifyUnityVersionCompatibility(manifest))
            {
                _logger?.LogWarning("Unity version compatibility check failed for plugin: {PluginId}", manifest.Id);
                return false;
            }

            // Verify required Unity packages
            if (!await VerifyUnityPackageDependencies(manifest))
            {
                _logger?.LogWarning("Unity package dependency check failed for plugin: {PluginId}", manifest.Id);
                return false;
            }

            // Verify assembly security (check for dangerous API usage)
            if (!VerifyAssemblySecurity(assemblyPath))
            {
                _logger?.LogWarning("Assembly security check failed for plugin: {PluginId}", manifest.Id);
                return false;
            }

            // Verify asset dependencies
            if (!VerifyAssetDependencies(manifest))
            {
                _logger?.LogWarning("Asset dependency check failed for plugin: {PluginId}", manifest.Id);
                return false;
            }

            _logger?.LogDebug("Unity security verification passed for plugin: {PluginId}", manifest.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unity security verification failed for plugin: {PluginId}", manifest.Id);
            return false;
        }
    }

    /// <summary>
    /// Verify Unity version compatibility
    /// </summary>
    private bool VerifyUnityVersionCompatibility(PluginManifest manifest)
    {
        if (manifest.Unity == null) return true;

#if UNITY
        var currentVersion = Application.unityVersion;

        // Check minimum Unity version
        if (!string.IsNullOrEmpty(manifest.Unity.MinUnityVersion))
        {
            if (CompareUnityVersions(currentVersion, manifest.Unity.MinUnityVersion) < 0)
            {
                _logger?.LogWarning("Plugin requires Unity {MinVersion} but current is {CurrentVersion}",
                    manifest.Unity.MinUnityVersion, currentVersion);
                return false;
            }
        }

        // Check maximum Unity version
        if (!string.IsNullOrEmpty(manifest.Unity.MaxUnityVersion))
        {
            if (CompareUnityVersions(currentVersion, manifest.Unity.MaxUnityVersion) > 0)
            {
                _logger?.LogWarning("Plugin supports Unity up to {MaxVersion} but current is {CurrentVersion}",
                    manifest.Unity.MaxUnityVersion, currentVersion);
                return false;
            }
        }
#endif

        return true;
    }

    /// <summary>
    /// Verify Unity package dependencies
    /// </summary>
    private async Task<bool> VerifyUnityPackageDependencies(PluginManifest manifest)
    {
        if (manifest.Unity?.RequiredPackages == null || manifest.Unity.RequiredPackages.Count == 0)
        {
            return true;
        }

        // In a real implementation, this would check Unity's Package Manager
        // For now, we'll assume packages are available
        _logger?.LogDebug("Checking {PackageCount} Unity package dependencies",
            manifest.Unity.RequiredPackages.Count);

        foreach (var package in manifest.Unity.RequiredPackages)
        {
            if (!package.Optional)
            {
                // TODO: Implement actual package verification with Unity Package Manager API
                _logger?.LogDebug("Required package: {PackageName} v{Version}", package.Name, package.Version);
            }
        }

        return true;
    }

    /// <summary>
    /// Verify assembly doesn't use dangerous Unity APIs
    /// </summary>
    private bool VerifyAssemblySecurity(string assemblyPath)
    {
        try
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            var dangerousApis = new[]
            {
                "UnityEditor.EditorApplication.Exit",
                "UnityEngine.Application.Quit",
                "System.Diagnostics.Process.Start",
                "System.IO.File.Delete",
                "UnityEngine.Object.DestroyImmediate"
            };

            foreach (var type in assembly.GetTypes())
            {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                {
                    // Check if method calls dangerous APIs
                    // This is a simplified check - in reality, you'd need IL analysis
                    var methodBody = method.GetMethodBody();
                    if (methodBody != null)
                    {
                        // TODO: Implement proper IL analysis for dangerous API detection
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to verify assembly security: {AssemblyPath}", assemblyPath);
            return false;
        }
    }

    /// <summary>
    /// Verify asset dependencies exist and are accessible
    /// </summary>
    private bool VerifyAssetDependencies(PluginManifest manifest)
    {
        if (manifest.Unity?.AssetDependencies == null || manifest.Unity.AssetDependencies.Count == 0)
        {
            return true;
        }

        foreach (var asset in manifest.Unity.AssetDependencies)
        {
            if (asset.Required && !File.Exists(asset.Path))
            {
                _logger?.LogWarning("Required asset not found: {AssetPath}", asset.Path);
                return false;
            }
        }

        return true;
    }

#if UNITY
    /// <summary>
    /// Compare Unity version strings
    /// </summary>
    private int CompareUnityVersions(string version1, string version2)
    {
        // Simplified version comparison - in reality, you'd parse Unity's version format properly
        try
        {
            var v1Parts = version1.Split('.').Take(3).Select(int.Parse).ToArray();
            var v2Parts = version2.Split('.').Take(3).Select(int.Parse).ToArray();

            for (int i = 0; i < 3; i++)
            {
                var v1Part = i < v1Parts.Length ? v1Parts[i] : 0;
                var v2Part = i < v2Parts.Length ? v2Parts[i] : 0;

                if (v1Part != v2Part)
                {
                    return v1Part.CompareTo(v2Part);
                }
            }

            return 0;
        }
        catch
        {
            // Fallback to string comparison
            return string.Compare(version1, version2, StringComparison.OrdinalIgnoreCase);
        }
    }
#endif
}
