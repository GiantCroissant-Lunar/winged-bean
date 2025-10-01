namespace WingedBean.Contracts.Resource;

/// <summary>
/// Resource metadata (size, type, etc.).
/// </summary>
public record ResourceMetadata
{
    /// <summary>
    /// Resource unique identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable resource name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Resource type (e.g., "texture", "audio", "prefab").
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// Resource size in bytes.
    /// </summary>
    public long Size { get; init; }

    /// <summary>
    /// Resource format (e.g., "PNG", "MP3", "JSON").
    /// </summary>
    public string? Format { get; init; }

    /// <summary>
    /// Additional resource properties.
    /// </summary>
    public IDictionary<string, object>? Properties { get; init; }
}
