namespace WingedBean.PluginSystem;

/// <summary>
/// Plugin update record for tracking update history
/// </summary>
public class PluginUpdateRecord
{
    /// <summary>Plugin ID</summary>
    public string PluginId { get; set; } = string.Empty;

    /// <summary>Previous version</summary>
    public string FromVersion { get; set; } = string.Empty;

    /// <summary>New version</summary>
    public string ToVersion { get; set; } = string.Empty;

    /// <summary>Update timestamp</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Update success status</summary>
    public bool Success { get; set; }

    /// <summary>Error message if update failed</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Update duration</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>Update type</summary>
    public PluginUpdateType UpdateType { get; set; }

    /// <summary>Rollback information</summary>
    public string? RollbackPath { get; set; }
}
