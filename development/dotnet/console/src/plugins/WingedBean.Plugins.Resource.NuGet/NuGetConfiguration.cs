namespace WingedBean.Plugins.Resource.NuGet;

/// <summary>
/// Configuration for NuGet package loading.
/// </summary>
public class NuGetConfiguration
{
    /// <summary>
    /// Default NuGet feed URL.
    /// </summary>
    public string DefaultFeed { get; set; } = "https://api.nuget.org/v3/index.json";
    
    /// <summary>
    /// Include prerelease versions in version resolution.
    /// </summary>
    public bool IncludePrerelease { get; set; } = false;
    
    /// <summary>
    /// Require packages to be signed.
    /// </summary>
    public bool RequireSignedPackages { get; set; } = false;
    
    /// <summary>
    /// Directory for package cache.
    /// </summary>
    public string? PackagesDirectory { get; set; }
    
    /// <summary>
    /// List of allowed package IDs (whitelist). If empty, all packages allowed.
    /// </summary>
    public List<string> AllowedPackages { get; set; } = new();
    
    /// <summary>
    /// Warn when loading unsigned or untrusted packages.
    /// </summary>
    public bool WarnOnUntrustedPackages { get; set; } = true;
    
    /// <summary>
    /// Maximum cache size in bytes (0 = unlimited).
    /// </summary>
    public long MaxCacheSizeBytes { get; set; } = 0;
    
    /// <summary>
    /// Default configuration instance.
    /// </summary>
    public static NuGetConfiguration Default => new();
}
