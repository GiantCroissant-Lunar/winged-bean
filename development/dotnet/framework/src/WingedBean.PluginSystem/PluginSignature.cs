namespace WingedBean.PluginSystem;

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
