using System.Reflection;

namespace WingedBean.Plugins.Resource.NuGet;

/// <summary>
/// Represents a loaded NuGet package as a resource.
/// Provides access to package assemblies, dependencies, and metadata.
/// </summary>
public class NuGetPackageResource
{
    internal List<Assembly>? _assemblies;
    internal List<PackageDependency>? _dependencies;
    
    /// <summary>
    /// Package identifier (e.g., "Newtonsoft.Json").
    /// </summary>
    public required string PackageId { get; init; }
    
    /// <summary>
    /// Package version (e.g., "13.0.3").
    /// </summary>
    public required string Version { get; init; }
    
    /// <summary>
    /// Local installation path.
    /// </summary>
    public required string InstallPath { get; init; }
    
    /// <summary>
    /// Package metadata from .nuspec.
    /// </summary>
    public required PackageMetadata Metadata { get; init; }
    
    /// <summary>
    /// Get all assemblies in this package for the current target framework.
    /// </summary>
    public IEnumerable<Assembly> GetAssemblies()
    {
        return _assemblies ?? Enumerable.Empty<Assembly>();
    }
    
    /// <summary>
    /// Get a specific assembly by name (without .dll extension).
    /// </summary>
    /// <param name="assemblyName">Assembly name (e.g., "Newtonsoft.Json")</param>
    public Assembly? GetAssembly(string assemblyName)
    {
        var normalizedName = assemblyName.Replace(".dll", "");
        return _assemblies?.FirstOrDefault(a => 
            a.GetName().Name?.Equals(normalizedName, StringComparison.OrdinalIgnoreCase) == true
        );
    }
    
    /// <summary>
    /// Load a type from this package.
    /// </summary>
    /// <typeparam name="T">Expected base type for validation</typeparam>
    /// <param name="typeName">Full type name (e.g., "Newtonsoft.Json.JsonSerializer")</param>
    public T? LoadType<T>(string typeName) where T : class
    {
        if (_assemblies == null) return null;
        
        foreach (var assembly in _assemblies)
        {
            var type = assembly.GetType(typeName);
            if (type != null)
            {
                // Validate type is assignable to T
                if (typeof(T).IsAssignableFrom(type))
                {
                    return Activator.CreateInstance(type) as T;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Get type from this package without instantiating.
    /// </summary>
    /// <param name="typeName">Full type name</param>
    public Type? GetType(string typeName)
    {
        if (_assemblies == null) return null;
        
        foreach (var assembly in _assemblies)
        {
            var type = assembly.GetType(typeName);
            if (type != null) return type;
        }
        
        return null;
    }
    
    /// <summary>
    /// Get all dependencies of this package.
    /// </summary>
    public IEnumerable<PackageDependency> GetDependencies()
    {
        return _dependencies ?? Enumerable.Empty<PackageDependency>();
    }
    
    /// <summary>
    /// Get content files from the package (readme, icon, etc.).
    /// </summary>
    public IEnumerable<string> GetContentFiles()
    {
        if (!Directory.Exists(InstallPath)) return Enumerable.Empty<string>();
        
        var contentDir = Path.Combine(InstallPath, "content");
        if (!Directory.Exists(contentDir)) return Enumerable.Empty<string>();
        
        return Directory.GetFiles(contentDir, "*", SearchOption.AllDirectories);
    }
}
