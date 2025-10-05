namespace WingedBean.PluginSystem;

/// <summary>
/// Search criteria for finding plugins
/// </summary>
public class PluginSearchCriteria
{
    /// <summary>Plugin ID pattern (supports wildcards)</summary>
    public string? IdPattern { get; set; }

    /// <summary>Name search term</summary>
    public string? NameContains { get; set; }

    /// <summary>Description search term</summary>
    public string? DescriptionContains { get; set; }

    /// <summary>Author name</summary>
    public string? Author { get; set; }

    /// <summary>Required capabilities</summary>
    public List<string> RequiredCapabilities { get; set; } = new();

    /// <summary>Supported profiles</summary>
    public List<string> SupportedProfiles { get; set; } = new();

    /// <summary>Minimum version</summary>
    public string? MinVersion { get; set; }

    /// <summary>Maximum version</summary>
    public string? MaxVersion { get; set; }

    /// <summary>Security level requirement</summary>
    public SecurityLevel? SecurityLevel { get; set; }

    /// <summary>Only signed plugins</summary>
    public bool OnlySigned { get; set; } = false;

    /// <summary>Load strategy</summary>
    public string? LoadStrategy { get; set; }

    /// <summary>Tags to include</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>Maximum results to return</summary>
    public int? MaxResults { get; set; }

    /// <summary>Sort order</summary>
    public PluginSortOrder SortOrder { get; set; } = PluginSortOrder.Name;
}
