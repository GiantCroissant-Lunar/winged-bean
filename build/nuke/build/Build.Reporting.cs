using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.IO;
using Serilog;
using Lunar.Build.CodeQuality.Reporting;
using Lunar.Build.Abstractions.Services;
using Lunar.Build.Configuration;
using Lunar.Build.Components;
using Lunar.NfunReport.MNuke.Components;

#nullable enable

namespace WingedBean.Console.MNuke
{
    /// <summary>
    /// WingedBean Console Build - Component Reporting Implementation (RFC-0040)
    /// Implements INfunReportComponent interface for test reporting integration
    /// </summary>
    partial class Build
{
    /// <summary>
    /// Generate component reports (includes test metrics from CodeQuality component)
    /// RFC-0040: Automatic test metric collection from TRX files
    /// </summary>
    public Target GenerateComponentReports => _ => _
        .Description("Generate reports from build components (includes test metrics) - RFC-0040")
        .DependsOn(Test)
        .Executes(async () =>
        {
            var version = GitVersion?.SemVer ?? "local";
            var reportOutput = ((IBuildConfigurationComponent)this).GetConfigurablePath(
                "reporting.outputDirectory",
                null,
                $"../_artifacts/{version}/reports");

            Log.Information("📊 RFC-0040: Generating component reports to: {Dir}", reportOutput);

            reportOutput.CreateDirectory();
            var componentsReportOutput = reportOutput / "components";
            componentsReportOutput.CreateDirectory();

            var coordinator = new ComponentReportCoordinator(new SimpleServiceProvider());

            // Register CodeQuality provider - this automatically collects test metrics!
            var cqOutput = ((IBuildConfigurationComponent)this).GetConfigurablePath(
                "codeQuality.outputDirectory",
                null,
                $"../_artifacts/{version}/dotnet/test-results");

            var cqSolution = ((IBuildConfigurationComponent)this).GetConfigurableValue(
                "codeQuality.solutionFile",
                null,
                "../../development/dotnet/console/Console.sln");

            var codeQualityProvider = new CodeQualityReportProvider(
                outputDirectory: cqOutput,
                solutionPath: cqSolution,
                failOnIssues: false,
                coverageEnabled: true);

            coordinator.RegisterProvider(codeQualityProvider);
            Log.Information("✅ Registered CodeQuality report provider");

            // Generate reports
            var providers = await coordinator.DiscoverReportProvidersAsync();

            foreach (var provider in providers)
            {
                try
                {
                    Log.Information("📋 Generating {ComponentName} report...", provider.ComponentName);

                    var reportData = provider.GetReportData();
                    var metadata = provider.GetReportMetadata();
                    var parameters = provider.GetReportParameters();

                    var componentDir = componentsReportOutput / provider.ComponentName.ToLowerInvariant();
                    componentDir.CreateDirectory();

                    var report = new
                    {
                        Component = provider.ComponentName,
                        Metadata = metadata,
                        Data = reportData,
                        Parameters = parameters,
                        GeneratedAt = DateTime.UtcNow,
                        RFC = "RFC-0040"
                    };

                    var componentReportPath = componentDir / "component-report.json";
                    var jsonContent = System.Text.Json.JsonSerializer.Serialize(
                        report,
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                    System.IO.File.WriteAllText(componentReportPath, jsonContent);
                    Log.Information("💾 {ComponentName} report saved: {Path}",
                        provider.ComponentName, componentReportPath);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "❌ Failed to generate {ComponentName} report", provider.ComponentName);
                }
            }

            Log.Information("✅ RFC-0040: Component reports generated successfully");
            Log.Information("📂 View reports at: {Path}", componentsReportOutput);
        });

    /// <summary>
    /// Full CI pipeline with all reporting (RFC-0040)
    /// </summary>
    public Target CIWithReports => _ => _
        .Description("Complete CI workflow with component reporting - RFC-0040")
        .DependsOn(Clean)
        .DependsOn(BuildAll)
        .DependsOn(Test)
        .DependsOn(GenerateComponentReports)
        .Executes(() =>
        {
            Log.Information("✅ RFC-0040: CI pipeline with reports completed successfully");
            Log.Information("📊 Test Results: {Path}", ArtifactsDirectory / "dotnet" / "test-results");
            Log.Information("📊 Component Reports: {Path}", ArtifactsDirectory / "reports" / "components");
        });
    }
}
