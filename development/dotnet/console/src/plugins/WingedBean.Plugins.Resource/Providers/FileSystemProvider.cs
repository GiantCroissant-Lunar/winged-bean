using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Plate.CrossMilo.Contracts.Resource;
using Plate.CrossMilo.Contracts.Resource.Services;

namespace WingedBean.Plugins.Resource.Providers;

/// <summary>
/// Resource provider for file system resources (files and bundles).
/// Handles file paths, bundle URIs, and directory scanning.
/// </summary>
public class FileSystemProvider : IResourceProvider
{
    private readonly ILogger _logger;
    private readonly ResourceCache _cache;
    private readonly string _basePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly Dictionary<string, ResourceBundle> _bundles;
    private readonly object _bundleLock = new();
    
    public FileSystemProvider(
        ILogger logger,
        string? basePath = null)
    {
        _logger = logger;
        _cache = new ResourceCache();
        _bundles = new Dictionary<string, ResourceBundle>();
        _basePath = basePath ?? Path.Combine(
            AppContext.BaseDirectory,
            "resources"
        );
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        
        EnsureBasePathExists();
        DiscoverBundles();
    }
    
    /// <summary>
    /// Check if this provider can handle the resource URI.
    /// File system provider handles anything that doesn't have a scheme prefix.
    /// </summary>
    public bool CanHandle(string resourceId)
    {
        // Handle file paths without scheme (default provider)
        // Don't handle URIs with schemes (nuget:, http:, etc.)
        return !resourceId.Contains(":", StringComparison.Ordinal) || 
               Path.IsPathRooted(resourceId);
    }
    
    /// <summary>
    /// Load a resource from file system or bundle.
    /// </summary>
    public async Task<TResource?> LoadAsync<TResource>(
        string resourceId,
        CancellationToken cancellationToken = default)
        where TResource : class
    {
        // Try bundles first (priority)
        var resource = await LoadFromBundlesAsync<TResource>(resourceId, cancellationToken);
        
        // Fall back to individual file
        if (resource == null)
        {
            resource = await LoadFromFileAsync<TResource>(resourceId, cancellationToken);
        }
        
        return resource;
    }
    
    /// <summary>
    /// Get metadata for a file system resource.
    /// </summary>
    public async Task<ResourceMetadata?> GetMetadataAsync(
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        var path = ResolveResourcePath(resourceId);
        if (!File.Exists(path))
        {
            return null;
        }
        
        await Task.CompletedTask; // Suppress async warning
        var fileInfo = new FileInfo(path);
        var extension = fileInfo.Extension.TrimStart('.');
        
        return new ResourceMetadata
        {
            Id = resourceId,
            Name = Path.GetFileNameWithoutExtension(fileInfo.Name),
            Type = DetermineResourceType(extension),
            Size = fileInfo.Length,
            Format = extension.ToUpperInvariant(),
            Properties = new Dictionary<string, object>
            {
                ["FullPath"] = fileInfo.FullName,
                ["LastModified"] = fileInfo.LastWriteTimeUtc,
                ["IsReadOnly"] = fileInfo.IsReadOnly
            }
        };
    }
    
    // Private helper methods
    
    private void EnsureBasePathExists()
    {
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
            _logger.LogInformation(
                "Created resource base directory: {Path}",
                _basePath
            );
        }
    }
    
    private void DiscoverBundles()
    {
        if (!Directory.Exists(_basePath))
        {
            return;
        }
        
        // Find all .wbundle files (WingedBean bundle format)
        var bundleFiles = Directory.GetFiles(_basePath, "*.wbundle", SearchOption.AllDirectories);
        
        foreach (var bundleFile in bundleFiles)
        {
            try
            {
                LoadBundle(bundleFile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to load bundle: {BundlePath}",
                    bundleFile
                );
            }
        }
        
        _logger.LogInformation(
            "Discovered {Count} resource bundle(s)",
            _bundles.Count
        );
    }
    
    private void LoadBundle(string bundlePath)
    {
        // Read manifest from bundle (manifest.json at root)
        using var archive = ZipFile.OpenRead(bundlePath);
        var manifestEntry = archive.GetEntry("manifest.json");
        
        if (manifestEntry == null)
        {
            _logger.LogWarning(
                "Bundle '{BundlePath}' missing manifest.json, skipping",
                bundlePath
            );
            return;
        }
        
        using var stream = manifestEntry.Open();
        var manifest = JsonSerializer.Deserialize<ResourceBundleManifest>(stream, _jsonOptions);
        
        if (manifest == null)
        {
            _logger.LogWarning(
                "Failed to deserialize manifest for bundle: {BundlePath}",
                bundlePath
            );
            return;
        }
        
        var bundle = new ResourceBundle(bundlePath, manifest, _logger, _jsonOptions);
        
        lock (_bundleLock)
        {
            _bundles[manifest.Id] = bundle;
        }
        
        _logger.LogInformation(
            "Loaded bundle '{BundleId}' with {ResourceCount} resource(s)",
            manifest.Id,
            manifest.Resources?.Length ?? 0
        );
    }
    
    private async Task<TResource?> LoadFromBundlesAsync<TResource>(
        string resourceId,
        CancellationToken cancellationToken)
        where TResource : class
    {
        ResourceBundle[] bundles;
        lock (_bundleLock)
        {
            bundles = _bundles.Values.ToArray();
        }
        
        // Try each bundle until we find the resource
        foreach (var bundle in bundles)
        {
            if (bundle.ContainsResource(resourceId))
            {
                _logger.LogDebug(
                    "Found resource '{ResourceId}' in bundle '{BundleId}'",
                    resourceId,
                    bundle.Id
                );
                
                return await bundle.LoadResourceAsync<TResource>(resourceId, cancellationToken);
            }
        }
        
        return null;
    }
    
    private async Task<TResource?> LoadFromFileAsync<TResource>(
        string resourceId,
        CancellationToken cancellationToken)
        where TResource : class
    {
        var path = ResolveResourcePath(resourceId);
        
        if (!File.Exists(path))
        {
            _logger.LogDebug(
                "Resource '{ResourceId}' not found at path: {Path}",
                resourceId,
                path
            );
            return null;
        }
        
        try
        {
            _logger.LogDebug(
                "Loading resource '{ResourceId}' from individual file",
                resourceId
            );
            
            return await LoadResourceFromFileAsync<TResource>(path, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to load resource '{ResourceId}' from {Path}",
                resourceId,
                path
            );
            return null;
        }
    }
    
    private string ResolveResourcePath(string resourceId)
    {
        // Handle absolute paths
        if (Path.IsPathRooted(resourceId))
        {
            return resourceId;
        }
        
        // Handle relative paths
        return Path.Combine(_basePath, resourceId);
    }
    
    private async Task<TResource?> LoadResourceFromFileAsync<TResource>(
        string path,
        CancellationToken cancellationToken)
        where TResource : class
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        
        // Handle different resource types based on extension
        return extension switch
        {
            ".json" => await LoadJsonResourceAsync<TResource>(path, cancellationToken),
            ".txt" => await LoadTextResourceAsync<TResource>(path, cancellationToken),
            ".bin" => await LoadBinaryResourceAsync<TResource>(path, cancellationToken),
            _ => await LoadDefaultResourceAsync<TResource>(path, cancellationToken)
        };
    }
    
    private async Task<TResource?> LoadJsonResourceAsync<TResource>(
        string path,
        CancellationToken cancellationToken)
        where TResource : class
    {
        await using var stream = File.OpenRead(path);
        
        // If TResource is string, return raw JSON
        if (typeof(TResource) == typeof(string))
        {
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync(cancellationToken);
            return content as TResource;
        }
        
        // Otherwise deserialize to TResource
        return await JsonSerializer.DeserializeAsync<TResource>(
            stream,
            _jsonOptions,
            cancellationToken
        );
    }
    
    private async Task<TResource?> LoadTextResourceAsync<TResource>(
        string path,
        CancellationToken cancellationToken)
        where TResource : class
    {
        var content = await File.ReadAllTextAsync(path, cancellationToken);
        
        if (typeof(TResource) == typeof(string))
        {
            return content as TResource;
        }
        
        // Try to deserialize as JSON if TResource is not string
        try
        {
            return JsonSerializer.Deserialize<TResource>(content, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }
    
    private async Task<TResource?> LoadBinaryResourceAsync<TResource>(
        string path,
        CancellationToken cancellationToken)
        where TResource : class
    {
        var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        
        if (typeof(TResource) == typeof(byte[]))
        {
            return bytes as TResource;
        }
        
        return null;
    }
    
    private async Task<TResource?> LoadDefaultResourceAsync<TResource>(
        string path,
        CancellationToken cancellationToken)
        where TResource : class
    {
        // Default: try as text first, then binary
        try
        {
            return await LoadTextResourceAsync<TResource>(path, cancellationToken);
        }
        catch
        {
            return await LoadBinaryResourceAsync<TResource>(path, cancellationToken);
        }
    }
    
    private static string DetermineResourceType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            "json" => "data",
            "txt" => "text",
            "md" => "markdown",
            "yaml" or "yml" => "data",
            "png" or "jpg" or "jpeg" or "gif" or "bmp" => "image",
            "wav" or "mp3" or "ogg" => "audio",
            "bin" or "dat" => "binary",
            _ => "unknown"
        };
    }
}
