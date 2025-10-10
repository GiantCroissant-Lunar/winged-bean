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
using Lunar.Build.NuGet;
using Lunar.NfunReport.MNuke.Components;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

// Enable nullable reference types
#nullable enable

namespace WingedBean.Console.MNuke
{
    /// <summary>
    /// WingedBean Unified Build - Integrated with Lunar Build Components (RFC-0040, RFC-0041)
    /// Split into partial classes for better separation of concerns:
    /// - Build.cs: Main class, interface declarations, and core build targets
    /// - Build.Configuration.cs: IBuildConfigurationComponent implementation details
    /// - Build.WrapperPath.cs: IWrapperPathComponent implementation details
    /// - Build.Reporting.cs: INfunReportComponent implementation (RFC-0040)
    /// - Build.NuGetPackaging.cs: NuGet packaging for framework (RFC-0041)
    ///
    /// Supports building both Console and Framework projects via the --project parameter:
    /// - --project console (default): Build ConsoleDungeon (RFC-0040)
    /// - --project framework: Build and pack Framework NuGet packages (RFC-0041)
    ///
    /// Note: INfunReportComponent is an abstract class, not an interface, so it cannot be
    /// declared in the base class list alongside NukeBuild. It's implemented separately
    /// in Build.Reporting.cs partial class.
    /// </summary>
    partial class Build : NukeBuild,
        IBuildConfigurationComponent,
        IWrapperPathComponent
    {
        /// <summary>
        /// Single entry point for the build system - RFC-0040 Console Build / RFC-0041 Framework Build
        /// Default target is CIWithReports for complete build+test+reporting workflow
        /// </summary>
        public static int Main() => Execute<Build>(x => x.CIWithReports);

        [Parameter("Project to build: 'console' (default) or 'framework'")]
        readonly string Project = "console";

        [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
        readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

        [GitVersion(NoFetch = true)]
        GitVersion _gitVersion = null!;

        public GitVersion GitVersion => _gitVersion;

        // Solution path - switches based on Project parameter
        AbsolutePath SolutionPath => Project?.ToLower() == "framework"
            ? RootDirectory.Parent.Parent / "development" / "dotnet" / "framework" / "Framework.sln"
            : RootDirectory.Parent.Parent / "development" / "dotnet" / "console" / "Console.sln";

    AbsolutePath OutputDirectory => RootDirectory / "bin" / Configuration;
    AbsolutePath ArtifactsDirectory => RootDirectory / "_artifacts" / GitVersion.SemVer;

    Target Clean => _ => _
        .Before(RestoreSolution)
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

    Target RestoreSolution => _ => _
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
        .DependsOn(RestoreSolution)
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
        .Description("Full CI pipeline (without reports - use CIWithReports for RFC-0040)")
        .DependsOn(Clean, BuildAll, Test)
        .Executes(() =>
        {
            Log.Information("CI pipeline completed successfully");
            Log.Information("💡 Tip: Use 'CIWithReports' target for full RFC-0040 reporting");
        });
    }

    /// <summary>
    /// Simple service provider for component coordination
    /// </summary>
    public class SimpleServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
