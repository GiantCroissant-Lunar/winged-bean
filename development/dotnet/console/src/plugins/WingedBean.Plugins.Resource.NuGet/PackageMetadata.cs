namespace WingedBean.Plugins.Resource.NuGet;

/// <summary>
/// Metadata for a NuGet package.
/// </summary>
public class PackageMetadata
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Authors { get; init; } = string.Empty;
    public string? LicenseUrl { get; init; }
    public string? ProjectUrl { get; init; }
    public IEnumerable<string> Tags { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Represents a package dependency.
/// </summary>
public class PackageDependency
{
    public string PackageId { get; init; } = string.Empty;
    public string VersionRange { get; init; } = string.Empty;
}

/// <summary>
/// Exception thrown when a package is not found.
/// </summary>
public class PackageNotFoundException : Exception
{
    public PackageNotFoundException(string message) : base(message)
    {
    }
    
    public PackageNotFoundException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
