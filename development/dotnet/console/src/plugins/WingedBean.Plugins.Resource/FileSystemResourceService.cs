using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WingedBean.Contracts.Resource;

namespace WingedBean.Plugins.Resource;

/// <summary>
/// File system-based resource service for Console profile.
/// Prioritizes loading from resource bundles (ZIP containers) with fallback to individual files.
/// </summary>
public class FileSystemResourceService : IResourceService
{
    private readonly ILogger<FileSystemResourceService> _logger;
    private readonly ResourceCache _cache;
    private readonly string _basePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly Dictionary<string, ResourceBundle> _bundles;
    private readonly object _bundleLock = new();

    public FileSystemResourceService(
        ILogger<FileSystemResourceService> logger,
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

    /// <inheritdoc/>
    public async Task<TResource?> LoadAsync<TResource>(
        string resourceId, 
        CancellationToken cancellationToken = default)
        where TResource : class
    {
        // Check cache first
        if (_cache.TryGet<TResource>(resourceId, out var cached))
        {
            _logger.LogDebug("Resource '{ResourceId}' loaded from cache", resourceId);
            return cached;
        }

        // Try to load from bundles first (priority)
        var resource = await LoadFromBundlesAsync<TResource>(resourceId, cancellationToken);
        
        // Fall back to individual file if not found in bundles
        if (resource == null)
        {
            resource = await LoadFromFileAsync<TResource>(resourceId, cancellationToken);
        }

        // Cache if loaded successfully
        if (resource != null)
        {
            _cache.Set(resourceId, resource);
            _logger.LogInformation(
                "Resource '{ResourceId}' loaded successfully", 
                resourceId
            );
        }

        return resource;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TResource>> LoadAllAsync<TResource>(
        string pattern, 
        CancellationToken cancellationToken = default)
        where TResource : class
    {
        var searchPath = ResolveResourcePath(pattern);
        var directory = Path.GetDirectoryName(searchPath) ?? _basePath;
        var searchPattern = Path.GetFileName(searchPath);

        if (!Directory.Exists(directory))
        {
            _logger.LogWarning(
                "Directory not found for pattern '{Pattern}': {Directory}", 
                pattern, 
                directory
            );
            return Enumerable.Empty<TResource>();
        }

        var files = Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories);
        var resources = new List<TResource>();

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var resourceId = GetResourceIdFromPath(file);
            var resource = await LoadAsync<TResource>(resourceId, cancellationToken);
            
            if (resource != null)
            {
                resources.Add(resource);
            }
        }

        _logger.LogInformation(
            "Loaded {Count} resources matching pattern '{Pattern}'", 
            resources.Count, 
            pattern
        );

        return resources;
    }

    /// <inheritdoc/>
    public void Unload(string resourceId)
    {
        if (_cache.Remove(resourceId))
        {
            _logger.LogDebug("Resource '{ResourceId}' unloaded from cache", resourceId);
        }
    }

    /// <inheritdoc/>
    public void UnloadAll<TResource>() where TResource : class
    {
        var count = _cache.RemoveAll<TResource>();
        _logger.LogInformation(
            "Unloaded {Count} resources of type {Type}", 
            count, 
            typeof(TResource).Name
        );
    }

    /// <inheritdoc/>
    public bool IsLoaded(string resourceId)
    {
        return _cache.Contains(resourceId);
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task PreloadAsync(
        IEnumerable<string> resourceIds, 
        CancellationToken cancellationToken = default)
    {
        var tasks = resourceIds.Select(id => 
            LoadAsync<object>(id, cancellationToken)
        );

        await Task.WhenAll(tasks);

        _logger.LogInformation(
            "Preloaded {Count} resources", 
            resourceIds.Count()
        );
    }

    // Private helper methods

    /// <summary>
    /// Discover and load resource bundles from the base path.
    /// </summary>
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

    /// <summary>
    /// Load a bundle from a file path.
    /// </summary>
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

    /// <summary>
    /// Try to load a resource from all loaded bundles.
    /// </summary>
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

    /// <summary>
    /// Load a resource from an individual file (fallback).
    /// </summary>
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

    private string GetResourceIdFromPath(string filePath)
    {
        // Convert absolute path back to resource ID
        if (filePath.StartsWith(_basePath))
        {
            return filePath.Substring(_basePath.Length).TrimStart(
                Path.DirectorySeparatorChar, 
                Path.AltDirectorySeparatorChar
            );
        }

        return filePath;
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
