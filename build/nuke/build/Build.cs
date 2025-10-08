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

    [GitVersion(NoFetch = true)]
    GitVersion _gitVersion = null!;

    public GitVersion GitVersion => _gitVersion;

    // Solution path - manually specified since attribute injection doesn't work with component interfaces
    AbsolutePath SolutionPath => RootDirectory.Parent.Parent / "development" / "dotnet" / "console" / "Console.sln";

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
            if (SolutionPath.FileExists())
            {
                DotNetClean(s => s
                    .SetProject(SolutionPath)
                    .SetConfiguration(Configuration));
            }
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            Log.Information("Restoring solution...");
            if (SolutionPath.FileExists())
            {
                DotNetRestore(s => s
                    .SetProjectFile(SolutionPath));
            }
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            Log.Information("Building solution...");
            if (SolutionPath.FileExists())
            {
                DotNetBuild(s => s
                    .SetProjectFile(SolutionPath)
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
            Log.Information("Running tests for version {Version}...", GitVersion.SemVer);
            var testResultsDir = ArtifactsDirectory / "dotnet" / "test-results";
            testResultsDir.CreateOrCleanDirectory();

            if (SolutionPath.FileExists())
            {
                Log.Information("Test results directory: {Directory}", testResultsDir);

                DotNetTest(s => s
                    .SetProjectFile(SolutionPath)
                    .SetConfiguration(Configuration)
                    .SetResultsDirectory(testResultsDir)
                    .AddLoggers(
                        $"trx;LogFileName=test-results.trx",
                        $"html;LogFileName=test-report.html")
                    .SetProperty("CollectCoverage", "true")
                    .SetProperty("CoverletOutputFormat", "cobertura")
                    .SetProperty("CoverletOutput", $"{testResultsDir}/coverage/")
                    .EnableNoBuild()
                    .EnableNoRestore());

                Log.Information("Test results saved to: {Directory}", testResultsDir);

                // Check if TRX file was created
                var trxFile = testResultsDir / "test-results.trx";
                if (trxFile.FileExists())
                {
                    Log.Information("✓ TRX file created: {File}", trxFile);
                }
                else
                {
                    Log.Warning("TRX file not found at: {File}", trxFile);
                }
            }
            else
            {
                Log.Error("Cannot run tests: Solution not found at {Path}", SolutionPath);
            }
        });

    Target CI => _ => _
        .Description("Full CI pipeline")
        .DependsOn(Clean, BuildAll, Test)
        .Executes(() =>
        {
            Log.Information("CI pipeline completed successfully");
        });
}
