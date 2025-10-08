using System.Reflection;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace WingedBean.Plugins.Resource.NuGet;

/// <summary>
/// Low-level NuGet package loader using NuGet.Protocol.
/// Handles package discovery, download, and extraction.
/// </summary>
public class NuGetPackageLoader
{
    private readonly Microsoft.Extensions.Logging.ILogger _logger;
    private readonly SourceCacheContext _cacheContext;
    private readonly NuGetConfiguration _config;
    private readonly string _packagesDirectory;
    
    public NuGetPackageLoader(
        Microsoft.Extensions.Logging.ILogger logger,
        NuGetConfiguration? config = null)
    {
        _logger = logger;
        _config = config ?? NuGetConfiguration.Default;
        _cacheContext = new SourceCacheContext();
        
        // Default: ~/.wingedbean/packages
        _packagesDirectory = _config.PackagesDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".wingedbean",
            "packages"
        );
        
        Directory.CreateDirectory(_packagesDirectory);
    }
    
    /// <summary>
    /// Load a NuGet package (download if needed).
    /// </summary>
    /// <param name="packageId">Package identifier</param>
    /// <param name="version">Specific version (null for latest)</param>
    /// <param name="feedUrl">Feed URL (null for default)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<NuGetPackageResource> LoadPackageAsync(
        string packageId,
        string? version = null,
        string? feedUrl = null,
        CancellationToken cancellationToken = default)
    {
        var nugetLogger = new NuGetLoggerAdapter(_logger);
        
        // Determine package source
        var sourceUrl = feedUrl ?? _config.DefaultFeed;
        _logger.LogDebug("Loading package '{PackageId}' from feed: {FeedUrl}", packageId, sourceUrl);
        
        var sourceRepository = Repository.Factory.GetCoreV3(sourceUrl, FeedType.HttpV3);
        
        // Find package metadata
        var metadataResource = await sourceRepository.GetResourceAsync<PackageMetadataResource>(cancellationToken);
        var metadata = await metadataResource.GetMetadataAsync(
            packageId,
            includePrerelease: _config.IncludePrerelease,
            includeUnlisted: false,
            _cacheContext,
            nugetLogger,
            cancellationToken
        );
        
        // Resolve version
        IPackageSearchMetadata? packageMetadata;
        if (version == null)
        {
            packageMetadata = metadata.OrderByDescending(m => m.Identity.Version).FirstOrDefault();
            if (packageMetadata != null)
            {
                _logger.LogInformation(
                    "Resolved latest version of '{PackageId}': {Version}",
                    packageId,
                    packageMetadata.Identity.Version
                );
            }
        }
        else
        {
            packageMetadata = metadata.FirstOrDefault(m => m.Identity.Version.ToString() == version);
        }
        
        if (packageMetadata == null)
        {
            throw new PackageNotFoundException(
                $"Package '{packageId}' version '{version ?? "latest"}' not found in feed '{sourceUrl}'"
            );
        }
        
        var packageIdentity = packageMetadata.Identity;
        
        // Check if already installed
        var installPath = GetPackageInstallPath(packageIdentity);
        if (!Directory.Exists(installPath) || !Directory.GetFiles(installPath).Any())
        {
            _logger.LogInformation(
                "Downloading package '{PackageId}' version '{Version}'...",
                packageIdentity.Id,
                packageIdentity.Version
            );
            
            // Download and extract package
            await DownloadPackageAsync(
                sourceRepository,
                packageIdentity,
                installPath,
                nugetLogger,
                cancellationToken
            );
        }
        else
        {
            _logger.LogDebug(
                "Package '{PackageId}' version '{Version}' already cached at: {Path}",
                packageIdentity.Id,
                packageIdentity.Version,
                installPath
            );
        }
        
        // Load package from disk
        return await LoadPackageFromDiskAsync(installPath, packageIdentity, packageMetadata);
    }
    
    private async Task DownloadPackageAsync(
        SourceRepository sourceRepository,
        PackageIdentity packageIdentity,
        string installPath,
        global::NuGet.Common.ILogger nugetLogger,
        CancellationToken cancellationToken)
    {
        var downloadResource = await sourceRepository.GetResourceAsync<DownloadResource>(cancellationToken);
        
        var downloadContext = new PackageDownloadContext(_cacheContext);
        
        using var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
            packageIdentity,
            downloadContext,
            SettingsUtility.GetGlobalPackagesFolder(Settings.LoadDefaultSettings(null)),
            nugetLogger,
            cancellationToken
        );
        
        if (downloadResult.Status != DownloadResourceResultStatus.Available)
        {
            throw new PackageNotFoundException(
                $"Package '{packageIdentity.Id}' version '{packageIdentity.Version}' could not be downloaded. Status: {downloadResult.Status}"
            );
        }
        
        // Create install directory
        Directory.CreateDirectory(installPath);
        
        // Extract package
        var packagePathResolver = new PackagePathResolver(Path.GetDirectoryName(installPath)!);
        var extractionContext = new PackageExtractionContext(
            PackageSaveMode.Defaultv3,
            XmlDocFileSaveMode.None,
            null, // clientPolicyContext
            nugetLogger
        );
        
        await PackageExtractor.ExtractPackageAsync(
            downloadResult.PackageSource,
            downloadResult.PackageStream,
            packagePathResolver,
            extractionContext,
            cancellationToken
        );
        
        _logger.LogInformation(
            "Package '{PackageId}' version '{Version}' extracted to: {Path}",
            packageIdentity.Id,
            packageIdentity.Version,
            installPath
        );
    }
    
    private async Task<NuGetPackageResource> LoadPackageFromDiskAsync(
        string installPath,
        PackageIdentity identity,
        IPackageSearchMetadata metadata)
    {
        // Find .nuspec file
        var nuspecPath = Directory.GetFiles(installPath, "*.nuspec", SearchOption.TopDirectoryOnly)
            .FirstOrDefault();
        
        if (nuspecPath == null)
        {
            throw new InvalidOperationException($"No .nuspec file found in {installPath}");
        }
        
        // Parse package metadata
        PackageMetadata packageMetadata;
        List<PackageDependency> dependencies;
        
        using (var nuspecStream = File.OpenRead(nuspecPath))
        {
            var nuspecReader = new NuspecReader(nuspecStream);
            var nuspecMetadata = nuspecReader.GetMetadata().ToDictionary(
                kvp => kvp.Key, 
                kvp => kvp.Value,
                StringComparer.OrdinalIgnoreCase
            );
            
            packageMetadata = new PackageMetadata
            {
                Title = nuspecMetadata.GetValueOrDefault("title") ?? identity.Id,
                Description = nuspecMetadata.GetValueOrDefault("description") ?? string.Empty,
                Authors = nuspecMetadata.GetValueOrDefault("authors") ?? string.Empty,
                LicenseUrl = nuspecMetadata.GetValueOrDefault("licenseUrl"),
                ProjectUrl = nuspecMetadata.GetValueOrDefault("projectUrl"),
                Tags = (nuspecMetadata.GetValueOrDefault("tags") ?? string.Empty)
                    .Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
            };
            
            // Get dependencies
            dependencies = nuspecReader.GetDependencyGroups()
                .SelectMany(g => g.Packages)
                .Select(d => new PackageDependency
                {
                    PackageId = d.Id,
                    VersionRange = d.VersionRange.ToString()
                })
                .ToList();
        }
        
        // Find assemblies for current framework
        var targetFramework = NuGetFramework.Parse("net8.0");
        var assemblies = LoadAssembliesForFramework(installPath, targetFramework);
        
        _logger.LogInformation(
            "Loaded package '{PackageId}' version '{Version}' with {AssemblyCount} assemblies",
            identity.Id,
            identity.Version,
            assemblies.Count
        );
        
        return new NuGetPackageResource
        {
            PackageId = identity.Id,
            Version = identity.Version.ToString(),
            InstallPath = installPath,
            Metadata = packageMetadata,
            _assemblies = assemblies,
            _dependencies = dependencies
        };
    }
    
    private List<Assembly> LoadAssembliesForFramework(string installPath, NuGetFramework targetFramework)
    {
        var assemblies = new List<Assembly>();
        var libPath = Path.Combine(installPath, "lib");
        
        if (!Directory.Exists(libPath))
        {
            _logger.LogWarning("No 'lib' directory found in package at: {Path}", installPath);
            return assemblies;
        }
        
        // Find best matching framework folder
        var frameworkFolders = Directory.GetDirectories(libPath)
            .Select(d => new
            {
                Path = d,
                Framework = NuGetFramework.ParseFolder(Path.GetFileName(d))
            })
            .Where(f => f.Framework.Framework != NuGetFramework.UnsupportedFramework.Framework)
            .ToList();
        
        if (!frameworkFolders.Any())
        {
            _logger.LogWarning("No valid framework folders found in lib directory");
            return assemblies;
        }
        
        // Find most compatible framework
        var reducer = new FrameworkReducer();
        var nearestFramework = reducer.GetNearest(targetFramework, frameworkFolders.Select(f => f.Framework));
        
        var selectedFolder = frameworkFolders.FirstOrDefault(f => f.Framework == nearestFramework);
        
        if (selectedFolder == null)
        {
            _logger.LogWarning(
                "No compatible framework found. Target: {TargetFramework}, Available: {Available}",
                targetFramework,
                string.Join(", ", frameworkFolders.Select(f => f.Framework.ToString()))
            );
            return assemblies;
        }
        
        _logger.LogDebug(
            "Selected framework folder: {Framework} (target: {TargetFramework})",
            selectedFolder.Framework,
            targetFramework
        );
        
        // Load all DLLs from selected framework folder
        var dllFiles = Directory.GetFiles(selectedFolder.Path, "*.dll");
        
        foreach (var dllFile in dllFiles)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dllFile);
                assemblies.Add(assembly);
                
                _logger.LogDebug("Loaded assembly: {Assembly}", assembly.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to load assembly from: {DllFile}",
                    dllFile
                );
            }
        }
        
        return assemblies;
    }
    
    private string GetPackageInstallPath(PackageIdentity identity)
    {
        return Path.Combine(
            _packagesDirectory,
            identity.Id.ToLowerInvariant(),
            identity.Version.ToString()
        );
    }
    
    /// <summary>
    /// Clear all cached packages.
    /// </summary>
    public void ClearCache()
    {
        if (Directory.Exists(_packagesDirectory))
        {
            Directory.Delete(_packagesDirectory, recursive: true);
            Directory.CreateDirectory(_packagesDirectory);
            
            _logger.LogInformation("NuGet package cache cleared: {Path}", _packagesDirectory);
        }
    }
    
    /// <summary>
    /// Clear a specific package from cache.
    /// </summary>
    public void ClearPackage(string packageId)
    {
        var packageDir = Path.Combine(_packagesDirectory, packageId.ToLowerInvariant());
        
        if (Directory.Exists(packageDir))
        {
            Directory.Delete(packageDir, recursive: true);
            
            _logger.LogInformation("Cleared package '{PackageId}' from cache", packageId);
        }
    }
}

/// <summary>
/// Adapter to bridge Microsoft.Extensions.Logging.ILogger to NuGet.Common.ILogger.
/// </summary>
internal class NuGetLoggerAdapter : global::NuGet.Common.ILogger
{
    private readonly Microsoft.Extensions.Logging.ILogger _logger;
    
    public NuGetLoggerAdapter(Microsoft.Extensions.Logging.ILogger logger)
    {
        _logger = logger;
    }
    
    public void Log(global::NuGet.Common.LogLevel level, string data)
    {
        _logger.Log(ConvertLogLevel(level), data);
    }
    
    public void Log(ILogMessage message)
    {
        _logger.Log(ConvertLogLevel(message.Level), message.Message);
    }
    
    public Task LogAsync(global::NuGet.Common.LogLevel level, string data)
    {
        Log(level, data);
        return Task.CompletedTask;
    }
    
    public Task LogAsync(ILogMessage message)
    {
        Log(message);
        return Task.CompletedTask;
    }
    
    public void LogDebug(string data) => Log(global::NuGet.Common.LogLevel.Debug, data);
    public void LogVerbose(string data) => Log(global::NuGet.Common.LogLevel.Verbose, data);
    public void LogInformation(string data) => Log(global::NuGet.Common.LogLevel.Information, data);
    public void LogMinimal(string data) => Log(global::NuGet.Common.LogLevel.Minimal, data);
    public void LogWarning(string data) => Log(global::NuGet.Common.LogLevel.Warning, data);
    public void LogError(string data) => Log(global::NuGet.Common.LogLevel.Error, data);
    public void LogInformationSummary(string data) => Log(global::NuGet.Common.LogLevel.Information, data);
    
    private Microsoft.Extensions.Logging.LogLevel ConvertLogLevel(global::NuGet.Common.LogLevel level)
    {
        return level switch
        {
            global::NuGet.Common.LogLevel.Debug => Microsoft.Extensions.Logging.LogLevel.Debug,
            global::NuGet.Common.LogLevel.Verbose => Microsoft.Extensions.Logging.LogLevel.Trace,
            global::NuGet.Common.LogLevel.Information => Microsoft.Extensions.Logging.LogLevel.Information,
            global::NuGet.Common.LogLevel.Minimal => Microsoft.Extensions.Logging.LogLevel.Information,
            global::NuGet.Common.LogLevel.Warning => Microsoft.Extensions.Logging.LogLevel.Warning,
            global::NuGet.Common.LogLevel.Error => Microsoft.Extensions.Logging.LogLevel.Error,
            _ => Microsoft.Extensions.Logging.LogLevel.Information
        };
    }
}
