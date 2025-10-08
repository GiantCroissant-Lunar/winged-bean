using FluentAssertions;
using Microsoft.Extensions.Logging;
using WingedBean.Plugins.Resource.NuGet;
using Xunit;
using Xunit.Abstractions;

namespace WingedBean.Plugins.Resource.NuGet.Tests;

/// <summary>
/// Integration tests for NuGet package loading.
/// These tests download real packages from NuGet.org - expect network delays.
/// </summary>
public class NuGetPackageLoaderTests
{
    private readonly ILogger<NuGetPackageLoader> _logger;
    
    public NuGetPackageLoaderTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        
        _logger = loggerFactory.CreateLogger<NuGetPackageLoader>();
    }
    
    [Fact]
    public async Task LoadPackageAsync_NewtonoftJson_LoadsSuccessfully()
    {
        // Arrange
        var loader = new NuGetPackageLoader(_logger);
        
        // Act
        var package = await loader.LoadPackageAsync(
            "Newtonsoft.Json",
            "13.0.3",
            null,
            CancellationToken.None
        );
        
        // Assert
        package.Should().NotBeNull();
        package.PackageId.Should().Be("Newtonsoft.Json");
        package.Version.Should().Be("13.0.3");
        package.GetAssemblies().Should().NotBeEmpty();
        
        var jsonAssembly = package.GetAssembly("Newtonsoft.Json");
        jsonAssembly.Should().NotBeNull();
        jsonAssembly!.FullName.Should().Contain("Newtonsoft.Json");
    }
    
    [Fact]
    public async Task LoadPackageAsync_LatestVersion_ResolvesSuccessfully()
    {
        // Arrange
        var loader = new NuGetPackageLoader(_logger);
        
        // Act
        var package = await loader.LoadPackageAsync(
            "Newtonsoft.Json",
            null, // Latest version
            null,
            CancellationToken.None
        );
        
        // Assert
        package.Should().NotBeNull();
        package.PackageId.Should().Be("Newtonsoft.Json");
        package.Version.Should().NotBeNullOrEmpty();
        
        // Latest version should be >= 13.0.3
        var version = new Version(package.Version.Split('-')[0]); // Handle prerelease
        version.Should().BeGreaterThanOrEqualTo(new Version("13.0.0"));
    }
    
    [Fact]
    public async Task LoadPackageAsync_InvalidPackage_ThrowsException()
    {
        // Arrange
        var loader = new NuGetPackageLoader(_logger);
        
        // Act & Assert
        await Assert.ThrowsAsync<PackageNotFoundException>(() =>
            loader.LoadPackageAsync(
                "NonExistent.Package.That.Does.Not.Exist",
                "1.0.0",
                null,
                CancellationToken.None
            )
        );
    }
    
    [Fact]
    public async Task LoadPackageAsync_CachedPackage_LoadsFromDisk()
    {
        // Arrange
        var loader = new NuGetPackageLoader(_logger);
        
        // Act - Load twice
        var package1 = await loader.LoadPackageAsync(
            "Newtonsoft.Json",
            "13.0.3",
            null,
            CancellationToken.None
        );
        
        var package2 = await loader.LoadPackageAsync(
            "Newtonsoft.Json",
            "13.0.3",
            null,
            CancellationToken.None
        );
        
        // Assert
        package1.InstallPath.Should().Be(package2.InstallPath);
        Directory.Exists(package1.InstallPath).Should().BeTrue();
    }
    
    [Fact]
    public async Task LoadPackageAsync_WithMetadata_ContainsPackageInfo()
    {
        // Arrange
        var loader = new NuGetPackageLoader(_logger);
        
        // Act
        var package = await loader.LoadPackageAsync(
            "Newtonsoft.Json",
            "13.0.3",
            null,
            CancellationToken.None
        );
        
        // Assert
        package.Metadata.Should().NotBeNull();
        package.Metadata.Title.Should().NotBeNullOrEmpty();
        package.Metadata.Description.Should().NotBeNullOrEmpty();
        package.Metadata.Authors.Should().Contain("James Newton-King");
    }
}
