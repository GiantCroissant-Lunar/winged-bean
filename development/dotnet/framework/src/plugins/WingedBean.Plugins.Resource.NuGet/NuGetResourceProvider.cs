using Microsoft.Extensions.Logging;
using Plate.CrossMilo.Contracts.Resource;
using WingedBean.Plugins.Resource.Providers;

namespace WingedBean.Plugins.Resource.NuGet;

/// <summary>
/// Resource provider for NuGet packages (Console profile only).
/// Handles "nuget:" scheme URIs.
/// </summary>
public class NuGetResourceProvider : IResourceProvider
{
    private readonly NuGetPackageLoader _loader;
    private readonly NuGetPackageCache _cache;
    private readonly ILogger<NuGetResourceProvider> _logger;
    
    public NuGetResourceProvider(
        ILogger<NuGetResourceProvider> logger,
        NuGetConfiguration? config = null)
    {
        _logger = logger;
        _loader = new NuGetPackageLoader(logger, config);
        _cache = new NuGetPackageCache();
    }
    
    /// <summary>
    /// Check if this provider can handle the resource URI.
    /// </summary>
    public bool CanHandle(string resourceId)
    {
        return resourceId.StartsWith("nuget:", StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Load a NuGet package as a resource.
    /// </summary>
    public async Task<TResource?> LoadAsync<TResource>(
        string resourceId,
        CancellationToken cancellationToken = default)
        where TResource : class
    {
        // Parse: "nuget:PackageId/Version@Feed"
        var (packageId, version, feed) = ParseNuGetUri(resourceId);
        
        _logger.LogDebug(
            "Loading NuGet resource: PackageId='{PackageId}', Version='{Version}', Feed='{Feed}'",
            packageId,
            version ?? "latest",
            feed ?? "default"
        );
        
        // Check cache
        if (_cache.TryGet(packageId, version, out var cached))
        {
            _logger.LogDebug(
                "NuGet package '{PackageId}' version '{Version}' loaded from cache",
                packageId,
                cached!.Version
            );
            return ConvertResource<TResource>(cached);
        }
        
        // Download and load package
        _logger.LogInformation(
            "Downloading NuGet package '{PackageId}' version '{Version}'...",
            packageId,
            version ?? "latest"
        );
        
        var package = await _loader.LoadPackageAsync(
            packageId,
            version,
            feed,
            cancellationToken
        );
        
        // Cache for future use
        _cache.Set(package);
        
        _logger.LogInformation(
            "NuGet package '{PackageId}' version '{Version}' loaded successfully",
            package.PackageId,
            package.Version
        );
        
        return ConvertResource<TResource>(package);
    }
    
    /// <summary>
    /// Get metadata for a NuGet package.
    /// </summary>
    public async Task<ResourceMetadata?> GetMetadataAsync(
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        var (packageId, version, feed) = ParseNuGetUri(resourceId);
        
        // Check if already loaded
        if (_cache.TryGet(packageId, version, out var package))
        {
            return CreateMetadata(package!);
        }
        
        // Would need to load package to get metadata
        // For now, return null to indicate we need to load first
        return null;
    }
    
    /// <summary>
    /// Parse NuGet URI format: "nuget:PackageId[/Version][@FeedUrl]"
    /// </summary>
    private (string PackageId, string? Version, string? Feed) ParseNuGetUri(string uri)
    {
        if (!uri.StartsWith("nuget:", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Invalid NuGet URI: {uri}. Expected format: nuget:PackageId[/Version][@Feed]");
        }
        
        // Remove "nuget:" prefix
        var remainder = uri.Substring("nuget:".Length);
        
        // Split by '@' to separate package part from feed
        var parts = remainder.Split('@');
        var packagePart = parts[0];
        var feed = parts.Length > 1 ? parts[1] : null;
        
        // Split package part by '/' to separate id from version
        var versionParts = packagePart.Split('/');
        var packageId = versionParts[0];
        var version = versionParts.Length > 1 ? versionParts[1] : null;
        
        if (string.IsNullOrWhiteSpace(packageId))
        {
            throw new ArgumentException($"Invalid NuGet URI: {uri}. Package ID cannot be empty.");
        }
        
        return (packageId, version, feed);
    }
    
    /// <summary>
    /// Convert NuGetPackageResource to requested resource type.
    /// </summary>
    private TResource? ConvertResource<TResource>(NuGetPackageResource package)
        where TResource : class
    {
        if (typeof(TResource) == typeof(NuGetPackageResource))
        {
            return package as TResource;
        }
        
        if (typeof(TResource) == typeof(System.Reflection.Assembly))
        {
            // Return primary assembly
            return package.GetAssemblies().FirstOrDefault() as TResource;
        }
        
        // For other types, return null (caller should request NuGetPackageResource)
        return null;
    }
    
    /// <summary>
    /// Create ResourceMetadata from NuGetPackageResource.
    /// </summary>
    private ResourceMetadata CreateMetadata(NuGetPackageResource package)
    {
        return new ResourceMetadata
        {
            Id = $"nuget:{package.PackageId}/{package.Version}",
            Name = package.Metadata.Title,
            Type = "nuget-package",
            Size = GetPackageSize(package.InstallPath),
            Format = "NUPKG",
            Properties = new Dictionary<string, object>
            {
                ["PackageId"] = package.PackageId,
                ["Version"] = package.Version,
                ["InstallPath"] = package.InstallPath,
                ["Description"] = package.Metadata.Description,
                ["Authors"] = package.Metadata.Authors,
                ["AssemblyCount"] = package.GetAssemblies().Count(),
                ["DependencyCount"] = package.GetDependencies().Count()
            }
        };
    }
    
    private long GetPackageSize(string installPath)
    {
        if (!Directory.Exists(installPath))
        {
            return 0;
        }
        
        try
        {
            return Directory.GetFiles(installPath, "*", SearchOption.AllDirectories)
                .Sum(f => new FileInfo(f).Length);
        }
        catch
        {
            return 0;
        }
    }
    
    /// <summary>
    /// Get cache statistics.
    /// </summary>
    public CacheStatistics GetCacheStatistics()
    {
        return _cache.GetStatistics();
    }
    
    /// <summary>
    /// Clear package cache.
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
        _loader.ClearCache();
    }
}
