using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Serilog;
using Lunar.Build.NuGet;
using Lunar.Build.Components;
using Lunar.Build.Configuration;

#nullable enable

namespace WingedBean.Console.MNuke
{
    /// <summary>
    /// WingedBean Framework Build - NuGet Packaging Implementation (RFC-0041)
    /// Partial class extending Build with NuGet packaging capabilities
    /// </summary>
    partial class Build : INuGetPackaging, INuGetLocalRepoSyncComponent
    {
    /// <summary>
    /// Override NuGetRepository to use GitVersion artifacts path
    /// </summary>
    public AbsolutePath NuGetRepository => GetNuGetOutputDirectoryFromConfig();

    /// <summary>
    /// Override BuiltPackagesDirectory for workspace sync
    /// </summary>
    public AbsolutePath BuiltPackagesDirectory => GetNuGetOutputDirectoryFromConfig();

    /// <summary>
    /// Complete NuGet workflow with workspace sync (RFC-0041)
    /// Skips BuildAll to avoid solution file issues - Pack builds projects directly
    /// </summary>
    public Target NuGetWorkflow => _ => _
        .Description("Complete NuGet workflow - pack and sync to workspace - RFC-0041")
        .DependsOn(((INuGetPackaging)this).Pack)
        .DependsOn(((INuGetLocalRepoSyncComponent)this).SyncNugetPackagesToLocalFeeds)
        .Executes(() => {
            Log.Information("✅ RFC-0041: NuGet workflow completed!");
            Log.Information("📦 Packages: {Path}", NuGetRepository);

            var workspaceRepo = ((IBuildConfigurationComponent)this).GetConfigurableValue(
                "globalPaths.nugetRepositoryDirectory", null, "packages/nuget-repo");
            Log.Information("📦 Workspace: {Path}", workspaceRepo);

            // List generated packages
            var packages = NuGetRepository.GlobFiles("*.nupkg");
            Log.Information("📋 Generated {Count} packages:", packages.Count);
            foreach (var package in packages)
            {
                Log.Information("  ✅ {Package}", package.Name);
            }
        });

    /// <summary>
    /// Get projects to pack - discovers from configuration
    /// </summary>
    public System.Collections.Generic.List<AbsolutePath> GetProjectsToPack()
    {
        var projectStrings = DiscoverProjectsFromConfiguration();
        var allProjects = projectStrings
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(p => (AbsolutePath)p)
            .Where(p => p.FileExists())
            .ToList();

        Log.Information("📋 RFC-0041: Projects to pack:");
        foreach (var project in allProjects)
        {
            Log.Information("  ✅ {ProjectName}", project.NameWithoutExtension);
        }

        if (!allProjects.Any())
        {
            Log.Warning("⚠️ No projects found to pack!");
        }

        return allProjects;
    }

    /// <summary>
    /// Get NuGet output directory with GitVersion support
    /// </summary>
    private AbsolutePath GetNuGetOutputDirectoryFromConfig()
    {
        try
        {
            var configValue = ((IBuildConfigurationComponent)this).GetConfigurableValue(
                "nuget.outputDirectory",
                null,
                "../../../../build/_artifacts/{GitVersion}/dotnet/packages");

            if (!string.IsNullOrEmpty(configValue) && GitVersion != null && configValue.Contains("{GitVersion}"))
            {
                configValue = configValue.Replace("{GitVersion}", GitVersion.SemVer);
                Log.Debug("📂 Substituted GitVersion in output directory: {Path}", configValue);
            }

            var root = ((IWrapperPathComponent)this).EffectiveRootDirectory;
            var resolved = root / configValue;
            Log.Information("📂 NuGet output: {Path}", resolved);
            return resolved;
        }
        catch (System.Exception ex)
        {
            Log.Warning("Failed to get NuGet output directory: {Message}", ex.Message);
            var root = ((IWrapperPathComponent)this).EffectiveRootDirectory;
            return root / "../../../../build/_artifacts/local/dotnet/packages";
        }
    }

    /// <summary>
    /// Discover projects from configuration
    /// </summary>
    private string[] DiscoverProjectsFromConfiguration()
    {
        var projects = new System.Collections.Generic.List<string>();
        try
        {
            var config = ((IBuildConfigurationComponent)this).BuildConfig;

            if (config.ProjectType == "multi-group-build")
            {
                var all = config.ProjectGroups?.SelectMany(group =>
                {
                    if (group.ExplicitProjects == null || !group.ExplicitProjects.Any())
                    {
                        Log.Debug("Group {GroupName} has no explicit projects", group.Name);
                        return System.Linq.Enumerable.Empty<string>();
                    }

                    if (!string.IsNullOrEmpty(group.SourceDirectory))
                    {
                        var effectiveRoot = ((IWrapperPathComponent)this).EffectiveRootDirectory;
                        var groupProjects = group.ExplicitProjects
                            .Select(p => (effectiveRoot / group.SourceDirectory / p).ToString())
                            .ToList();

                        Log.Debug("Group {GroupName}: {Count} projects from {SourceDir}",
                            group.Name, groupProjects.Count, group.SourceDirectory);

                        return groupProjects;
                    }

                    Log.Debug("Group {GroupName} has no source directory", group.Name);
                    return group.ExplicitProjects;
                }).ToArray() ?? System.Array.Empty<string>();

                projects.AddRange(all);
                Log.Debug("Discovered {Count} total projects from configuration", projects.Count);
            }
            else
            {
                Log.Warning("Project type is not multi-group-build: {Type}", config.ProjectType);
            }
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "Failed to discover projects from configuration: {Message}", ex.Message);
        }

        return projects.ToArray();
    }
    }
}
