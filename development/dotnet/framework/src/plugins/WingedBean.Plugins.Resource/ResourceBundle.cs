using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace WingedBean.Plugins.Resource;

/// <summary>
/// Represents a loaded resource bundle (container).
/// Provides access to bundled resources with lazy loading support.
/// </summary>
public class ResourceBundle : IDisposable
{
    private readonly string _bundlePath;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private ZipArchive? _archive;
    private bool _disposed;

    /// <summary>
    /// Bundle manifest.
    /// </summary>
    public ResourceBundleManifest Manifest { get; }

    /// <summary>
    /// Bundle ID.
    /// </summary>
    public string Id => Manifest.Id;

    /// <summary>
    /// Whether the bundle is currently open.
    /// </summary>
    public bool IsOpen => _archive != null;

    public ResourceBundle(
        string bundlePath,
        ResourceBundleManifest manifest,
        ILogger logger,
        JsonSerializerOptions jsonOptions)
    {
        _bundlePath = bundlePath;
        Manifest = manifest;
        _logger = logger;
        _jsonOptions = jsonOptions;
    }

    /// <summary>
    /// Open the bundle archive for reading.
    /// </summary>
    public void Open()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ResourceBundle));
        }

        if (_archive != null)
        {
            return; // Already open
        }

        try
        {
            _archive = ZipFile.OpenRead(_bundlePath);
            _logger.LogDebug("Opened resource bundle: {BundleId}", Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open resource bundle: {BundlePath}", _bundlePath);
            throw;
        }
    }

    /// <summary>
    /// Load a resource from the bundle by ID.
    /// </summary>
    public async Task<TResource?> LoadResourceAsync<TResource>(
        string resourceId,
        CancellationToken cancellationToken = default)
        where TResource : class
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ResourceBundle));
        }

        if (_archive == null)
        {
            Open();
        }

        // Find resource entry in manifest
        var entry = Manifest.Resources?.FirstOrDefault(r => r.Id == resourceId);
        if (entry == null)
        {
            _logger.LogWarning(
                "Resource '{ResourceId}' not found in bundle '{BundleId}'",
                resourceId,
                Id
            );
            return null;
        }

        // Find entry in archive
        var archiveEntry = _archive!.GetEntry(entry.Path);
        if (archiveEntry == null)
        {
            _logger.LogWarning(
                "Archive entry '{Path}' not found for resource '{ResourceId}' in bundle '{BundleId}'",
                entry.Path,
                resourceId,
                Id
            );
            return null;
        }

        try
        {
            await using var stream = archiveEntry.Open();
            return await LoadResourceFromStreamAsync<TResource>(
                stream,
                entry,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to load resource '{ResourceId}' from bundle '{BundleId}'",
                resourceId,
                Id
            );
            return null;
        }
    }

    /// <summary>
    /// Get all resource IDs in the bundle.
    /// </summary>
    public IEnumerable<string> GetResourceIds()
    {
        return Manifest.Resources?.Select(r => r.Id) ?? Enumerable.Empty<string>();
    }

    /// <summary>
    /// Check if the bundle contains a resource.
    /// </summary>
    public bool ContainsResource(string resourceId)
    {
        return Manifest.Resources?.Any(r => r.Id == resourceId) ?? false;
    }

    /// <summary>
    /// Get resource entry metadata.
    /// </summary>
    public ResourceEntry? GetResourceEntry(string resourceId)
    {
        return Manifest.Resources?.FirstOrDefault(r => r.Id == resourceId);
    }

    /// <summary>
    /// Close the bundle archive.
    /// </summary>
    public void Close()
    {
        if (_archive != null)
        {
            _archive.Dispose();
            _archive = null;
            _logger.LogDebug("Closed resource bundle: {BundleId}", Id);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Close();
            _disposed = true;
        }
    }

    private async Task<TResource?> LoadResourceFromStreamAsync<TResource>(
        Stream stream,
        ResourceEntry entry,
        CancellationToken cancellationToken)
        where TResource : class
    {
        var format = entry.Format?.ToLowerInvariant() ?? "unknown";

        return format switch
        {
            "json" => await LoadJsonFromStreamAsync<TResource>(stream, cancellationToken),
            "txt" or "text" => await LoadTextFromStreamAsync<TResource>(stream, cancellationToken),
            "bin" or "binary" => await LoadBinaryFromStreamAsync<TResource>(stream, cancellationToken),
            _ => await TryLoadFromStreamAsync<TResource>(stream, cancellationToken)
        };
    }

    private async Task<TResource?> LoadJsonFromStreamAsync<TResource>(
        Stream stream,
        CancellationToken cancellationToken)
        where TResource : class
    {
        if (typeof(TResource) == typeof(string))
        {
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync(cancellationToken);
            return content as TResource;
        }

        return await JsonSerializer.DeserializeAsync<TResource>(
            stream,
            _jsonOptions,
            cancellationToken
        );
    }

    private async Task<TResource?> LoadTextFromStreamAsync<TResource>(
        Stream stream,
        CancellationToken cancellationToken)
        where TResource : class
    {
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync(cancellationToken);

        if (typeof(TResource) == typeof(string))
        {
            return content as TResource;
        }

        // Try JSON deserialization
        try
        {
            return JsonSerializer.Deserialize<TResource>(content, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private async Task<TResource?> LoadBinaryFromStreamAsync<TResource>(
        Stream stream,
        CancellationToken cancellationToken)
        where TResource : class
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        var bytes = memoryStream.ToArray();

        if (typeof(TResource) == typeof(byte[]))
        {
            return bytes as TResource;
        }

        return null;
    }

    private async Task<TResource?> TryLoadFromStreamAsync<TResource>(
        Stream stream,
        CancellationToken cancellationToken)
        where TResource : class
    {
        try
        {
            return await LoadTextFromStreamAsync<TResource>(stream, cancellationToken);
        }
        catch
        {
            // Reset stream if possible
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            try
            {
                return await LoadBinaryFromStreamAsync<TResource>(stream, cancellationToken);
            }
            catch
            {
                return null;
            }
        }
    }
}
