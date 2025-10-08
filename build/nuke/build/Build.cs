using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using Serilog;
using Lunar.Build.Configuration;
using Lunar.Build.Components;
using Lunar.NfunReport.MNuke.Components;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

// Enable nullable reference types
#nullable enable

/// <summary>
/// WingedBean Console Build - Integrated with Lunar Build Components
/// </summary>
partial class Build :
    INfunReportComponent,
    IBuildConfigurationComponent,
    IWrapperPathComponent
{
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution("../../development/dotnet/console/Console.sln")]
    readonly Solution? Solution;

    [GitVersion(NoFetch = true)]
    GitVersion _gitVersion = null!;

    public GitVersion GitVersion => _gitVersion;

    // IWrapperPathComponent implementation
    public string[] ProjectConfigIdentifiers => new[]
    {
        "winged-bean.console",
        "console-dungeon"
    };

    [Parameter("NUKE root directory override from wrapper script", Name = "wrapper-nuke-root")]
    readonly string? _wrapperNukeRootParam;

    [Parameter("Config path override from wrapper script", Name = "wrapper-config-path")]
    readonly string? _wrapperConfigPathParam;

    [Parameter("Script directory from wrapper script", Name = "wrapper-script-dir")]
    readonly string? _wrapperScriptDirParam;

    public string? WrapperNukeRootParam => _wrapperNukeRootParam;
    public string? WrapperConfigPathParam => _wrapperConfigPathParam;
    public string? WrapperScriptDirParam => _wrapperScriptDirParam;

    // IBuildConfigurationComponent implementation
    public string BuildConfigPath => ((IWrapperPathComponent)this).EffectiveRootDirectory / "build" / "nuke" / "build-config.json";

    AbsolutePath OutputDirectory => RootDirectory / "bin" / Configuration;
    AbsolutePath ArtifactsDirectory => RootDirectory / "_artifacts" / GitVersion.SemVer;

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            Log.Information("Cleaning solution...");
            if (Solution != null)
            {
                DotNetClean(s => s
                    .SetProject(Solution)
                    .SetConfiguration(Configuration));
            }
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            Log.Information("Restoring solution...");
            if (Solution != null)
            {
                DotNetRestore(s => s
                    .SetProjectFile(Solution));
            }
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            Log.Information("Building solution...");
            if (Solution != null)
            {
                DotNetBuild(s => s
                    .SetProjectFile(Solution)
                    .SetConfiguration(Configuration)
                    .EnableNoRestore());
            }
        });

    Target BuildAll => _ => _
        .Description("Build all projects")
        .DependsOn(Compile)
        .Executes(() =>
        {
            Log.Information("Build completed for version {Version}", GitVersion.SemVer);
            Log.Information("Artifacts directory: {Directory}", ArtifactsDirectory);
        });

    Target Test => _ => _
        .Description("Run tests with structured output")
        .DependsOn(BuildAll)
        .Executes(() =>
        {
            Log.Information("Running tests...");
            var testResultsDir = ArtifactsDirectory / "dotnet" / "test-results";
            testResultsDir.CreateOrCleanDirectory();

            if (Solution != null)
            {
                DotNetTest(s => s
                    .SetProjectFile(Solution)
                    .SetConfiguration(Configuration)
                    .SetLoggers(
                        $"trx;LogFileName={testResultsDir}/test-results.trx",
                        $"html;LogFileName={testResultsDir}/test-report.html")
                    .SetProperty("CollectCoverage", "true")
                    .SetProperty("CoverletOutputFormat", "cobertura")
                    .SetProperty("CoverletOutput", $"{testResultsDir}/coverage/")
                    .EnableNoBuild());
            }

            Log.Information("Test results saved to: {Directory}", testResultsDir);
        });

    Target CI => _ => _
        .Description("Full CI pipeline")
        .DependsOn(Clean, BuildAll, Test)
        .Executes(() =>
        {
            Log.Information("CI pipeline completed successfully");
        });
}
