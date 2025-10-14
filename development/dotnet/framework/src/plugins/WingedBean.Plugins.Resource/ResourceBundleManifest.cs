namespace WingedBean.Plugins.Resource;

/// <summary>
/// Manifest for resource bundles (containers).
/// Defines metadata and contents of a resource bundle.
/// </summary>
public class ResourceBundleManifest
{
    /// <summary>
    /// Bundle identifier (usually the bundle filename without extension).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Bundle version (semantic versioning recommended).
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Bundle name (human-readable).
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Bundle description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Bundle author/creator.
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// Bundle creation timestamp (ISO 8601).
    /// </summary>
    public string? CreatedAt { get; init; }

    /// <summary>
    /// List of resources contained in the bundle.
    /// </summary>
    public ResourceEntry[]? Resources { get; init; }

    /// <summary>
    /// Additional metadata properties.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Represents a single resource entry in a bundle manifest.
/// </summary>
public class ResourceEntry
{
    /// <summary>
    /// Resource identifier (path within bundle or logical ID).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Physical path within the bundle archive.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Resource type (e.g., "texture", "audio", "data", "prefab").
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// Resource format (e.g., "JSON", "PNG", "MP3").
    /// </summary>
    public string? Format { get; init; }

    /// <summary>
    /// Uncompressed size in bytes.
    /// </summary>
    public long? Size { get; init; }

    /// <summary>
    /// Content hash (for integrity verification).
    /// </summary>
    public string? Hash { get; init; }

    /// <summary>
    /// Tags for categorization and searching.
    /// </summary>
    public string[]? Tags { get; init; }

    /// <summary>
    /// Dependencies on other resources (resource IDs).
    /// </summary>
    public string[]? Dependencies { get; init; }

    /// <summary>
    /// Additional properties.
    /// </summary>
    public Dictionary<string, object>? Properties { get; init; }
}
