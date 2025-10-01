using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WingedBean.Host;

/// <summary>
/// Plugin signature for verifying plugin integrity and authenticity
/// </summary>
public class PluginSignature
{
    /// <summary>Digital signature of the plugin manifest and assemblies</summary>
    public string Signature { get; set; } = string.Empty;

    /// <summary>Public key used for signature verification</summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>Signature algorithm used</summary>
    public string Algorithm { get; set; } = "RSA-SHA256";

    /// <summary>Timestamp when the plugin was signed</summary>
    public DateTimeOffset SignedAt { get; set; }

    /// <summary>Certificate chain for verification</summary>
    public List<string> CertificateChain { get; set; } = new();
}

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

/// <summary>
/// Plugin signature verification service
/// </summary>
public interface IPluginSignatureVerifier
{
    /// <summary>Verify a plugin's digital signature</summary>
    Task<bool> VerifySignatureAsync(PluginManifest manifest, string pluginPath, CancellationToken ct = default);

    /// <summary>Generate signature for a plugin (for plugin authors)</summary>
    Task<PluginSignature> SignPluginAsync(PluginManifest manifest, string pluginPath, string privateKeyPath, CancellationToken ct = default);
}

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

/// <summary>
/// Default implementation of plugin signature verification
/// </summary>
public class RsaPluginSignatureVerifier : IPluginSignatureVerifier
{
    public async Task<bool> VerifySignatureAsync(PluginManifest manifest, string pluginPath, CancellationToken ct = default)
    {
        try
        {
            var security = manifest.Security;
            if (security?.Signature == null)
                return false; // Unsigned plugins not allowed in secure mode

            var publicKeyPem = security.Signature.PublicKey;
            var signature = Convert.FromBase64String(security.Signature.Signature);

            // Calculate hash of plugin files
            var hash = await CalculatePluginHashAsync(pluginPath, ct);

            // Verify signature
            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);

            return rsa.VerifyData(hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false; // Any exception means verification failed
        }
    }

    public async Task<PluginSignature> SignPluginAsync(PluginManifest manifest, string pluginPath, string privateKeyPath, CancellationToken ct = default)
    {
        var privateKeyPem = await File.ReadAllTextAsync(privateKeyPath, ct);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);

        // Calculate hash of plugin files
        var hash = await CalculatePluginHashAsync(pluginPath, ct);

        // Sign the hash
        var signature = rsa.SignData(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return new PluginSignature
        {
            Signature = Convert.ToBase64String(signature),
            PublicKey = rsa.ExportRSAPublicKeyPem(),
            Algorithm = "RSA-SHA256",
            SignedAt = DateTimeOffset.UtcNow
        };
    }

    private async Task<byte[]> CalculatePluginHashAsync(string pluginPath, CancellationToken ct)
    {
        using var sha256 = SHA256.Create();

        // Include all plugin files in hash calculation
        var files = Directory.GetFiles(pluginPath, "*", SearchOption.AllDirectories)
            .OrderBy(f => f)
            .ToList();

        foreach (var file in files)
        {
            var fileBytes = await File.ReadAllBytesAsync(file, ct);
            sha256.TransformBlock(fileBytes, 0, fileBytes.Length, null, 0);
        }

        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return sha256.Hash ?? Array.Empty<byte>();
    }
}

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
