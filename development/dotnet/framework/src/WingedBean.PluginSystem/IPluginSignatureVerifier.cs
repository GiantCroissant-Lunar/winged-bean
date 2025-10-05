namespace WingedBean.PluginSystem;

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
