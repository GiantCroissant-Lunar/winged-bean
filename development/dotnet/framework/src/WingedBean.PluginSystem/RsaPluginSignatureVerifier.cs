using System.Security.Cryptography;

namespace WingedBean.PluginSystem;

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
