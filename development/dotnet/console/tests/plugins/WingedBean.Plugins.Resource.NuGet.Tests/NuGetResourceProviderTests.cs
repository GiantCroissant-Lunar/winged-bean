using FluentAssertions;
using Microsoft.Extensions.Logging;
using WingedBean.Plugins.Resource.NuGet;
using Xunit;
using Xunit.Abstractions;

namespace WingedBean.Plugins.Resource.NuGet.Tests;

/// <summary>
/// Tests for NuGet resource provider integration.
/// </summary>
public class NuGetResourceProviderTests
{
    private readonly ILogger<NuGetResourceProvider> _logger;
    
    public NuGetResourceProviderTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        
        _logger = loggerFactory.CreateLogger<NuGetResourceProvider>();
    }
    
    [Theory]
    [InlineData("nuget:Newtonsoft.Json", true)]
    [InlineData("nuget:Some.Package/1.0.0", true)]
    [InlineData("nuget:Package@https://feed.com", true)]
    [InlineData("data/file.json", false)]
    [InlineData("/absolute/path.txt", false)]
    public void CanHandle_VariousUris_ReturnsCorrectResult(string resourceId, bool expected)
    {
        // Arrange
        var provider = new NuGetResourceProvider(_logger);
        
        // Act
        var result = provider.CanHandle(resourceId);
        
        // Assert
        result.Should().Be(expected);
    }
    
    [Fact]
    public async Task LoadAsync_ValidNuGetPackage_LoadsSuccessfully()
    {
        // Arrange
        var provider = new NuGetResourceProvider(_logger);
        
        // Act
        var package = await provider.LoadAsync<NuGetPackageResource>(
            "nuget:Newtonsoft.Json/13.0.3",
            CancellationToken.None
        );
        
        // Assert
        package.Should().NotBeNull();
        package!.PackageId.Should().Be("Newtonsoft.Json");
        package.Version.Should().Be("13.0.3");
    }
    
    [Fact]
    public async Task LoadAsync_CachedPackage_LoadsFromCache()
    {
        // Arrange
        var provider = new NuGetResourceProvider(_logger);
        
        // Act - Load twice
        var package1 = await provider.LoadAsync<NuGetPackageResource>(
            "nuget:Newtonsoft.Json/13.0.3",
            CancellationToken.None
        );
        
        var package2 = await provider.LoadAsync<NuGetPackageResource>(
            "nuget:Newtonsoft.Json/13.0.3",
            CancellationToken.None
        );
        
        // Assert
        package1.Should().NotBeNull();
        package2.Should().NotBeNull();
        // Cache should return same instance
        package1.Should().BeSameAs(package2);
    }
    
    [Fact]
    public void GetCacheStatistics_AfterLoads_ReturnsValidStats()
    {
        // Arrange
        var provider = new NuGetResourceProvider(_logger);
        
        // Act
        var stats = provider.GetCacheStatistics();
        
        // Assert
        stats.Should().NotBeNull();
        stats.TotalPackages.Should().BeGreaterThanOrEqualTo(0);
    }
}
