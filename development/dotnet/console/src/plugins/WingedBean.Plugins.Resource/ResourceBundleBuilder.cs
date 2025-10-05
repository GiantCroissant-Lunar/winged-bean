using System.IO.Compression;
using System.Text.Json;

namespace WingedBean.Plugins.Resource;

/// <summary>
/// Builder for creating resource bundles (containers).
/// </summary>
public class ResourceBundleBuilder
{
    private string _bundleId;
    private string _version;
    private string? _name;
    private string? _description;
    private string? _author;
    private Dictionary<string, object>? _metadata;
    private readonly List<(string sourceFile, ResourceEntry entry)> _resources;
    private readonly JsonSerializerOptions _jsonOptions;

    public ResourceBundleBuilder(string bundleId, string? version = null)
    {
        _bundleId = bundleId;
        _version = version ?? "1.0.0";
        _resources = new List<(string, ResourceEntry)>();
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Set bundle metadata.
    /// </summary>
    public ResourceBundleBuilder WithMetadata(
        string? name = null,
        string? description = null,
        string? author = null,
        Dictionary<string, object>? metadata = null)
    {
        _name = name ?? _name;
        _description = description ?? _description;
        _author = author ?? _author;
        _metadata = metadata ?? _metadata;
        return this;
    }

    /// <summary>
    /// Add a resource to the bundle.
    /// </summary>
    public ResourceBundleBuilder AddResource(
        string sourceFile,
        string resourceId,
        string? type = null,
        string[]? tags = null,
        string[]? dependencies = null,
        Dictionary<string, object>? properties = null)
    {
        if (!File.Exists(sourceFile))
        {
            throw new FileNotFoundException($"Source file not found: {sourceFile}", sourceFile);
        }

        var fileInfo = new FileInfo(sourceFile);
        var extension = fileInfo.Extension.TrimStart('.');
        var internalPath = $"resources/{resourceId}";

        var entry = new ResourceEntry
        {
            Id = resourceId,
            Path = internalPath,
            Type = type ?? DetermineType(extension),
            Format = extension.ToUpperInvariant(),
            Size = fileInfo.Length,
            Tags = tags,
            Dependencies = dependencies,
            Properties = properties
        };

        _resources.Add((sourceFile, entry));
        return this;
    }

    /// <summary>
    /// Add multiple resources from a directory.
    /// </summary>
    public ResourceBundleBuilder AddDirectory(
        string directoryPath,
        string? resourcePrefix = null,
        bool recursive = true,
        string[]? filePatterns = null)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var patterns = filePatterns ?? new[] { "*.*" };

        foreach (var pattern in patterns)
        {
            var files = Directory.GetFiles(directoryPath, pattern, searchOption);

            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(directoryPath, file);
                var resourceId = resourcePrefix != null
                    ? $"{resourcePrefix}/{relativePath}"
                    : relativePath;

                // Normalize path separators
                resourceId = resourceId.Replace('\\', '/');

                AddResource(file, resourceId);
            }
        }

        return this;
    }

    /// <summary>
    /// Build the bundle and save to file.
    /// </summary>
    public async Task BuildAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        // Create manifest
        var manifest = new ResourceBundleManifest
        {
            Id = _bundleId,
            Version = _version,
            Name = _name,
            Description = _description,
            Author = _author,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            Resources = _resources.Select(r => r.entry).ToArray(),
            Metadata = _metadata
        };

        // Create directory if needed
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Delete existing bundle if exists
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        // Create ZIP archive
        using var archive = ZipFile.Open(outputPath, ZipArchiveMode.Create);

        // Add manifest
        var manifestEntry = archive.CreateEntry("manifest.json");
        await using (var stream = manifestEntry.Open())
        {
            await JsonSerializer.SerializeAsync(stream, manifest, _jsonOptions, cancellationToken);
        }

        // Add resources
        foreach (var (sourceFile, entry) in _resources)
        {
            var archiveEntry = archive.CreateEntryFromFile(sourceFile, entry.Path);
        }
    }

    private static string DetermineType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            "json" or "yaml" or "yml" or "toml" or "xml" => "data",
            "txt" or "md" => "text",
            "png" or "jpg" or "jpeg" or "gif" or "bmp" or "webp" => "image",
            "wav" or "mp3" or "ogg" or "flac" => "audio",
            "obj" or "fbx" or "gltf" or "glb" => "model",
            "ttf" or "otf" or "woff" or "woff2" => "font",
            "bin" or "dat" => "binary",
            _ => "unknown"
        };
    }
}
