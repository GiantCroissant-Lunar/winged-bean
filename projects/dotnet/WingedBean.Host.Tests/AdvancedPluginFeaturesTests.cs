using Xunit;
using FluentAssertions;

namespace WingedBean.Host.Tests;

public class SemanticVersionTests
{
    [Theory]
    [InlineData("1.0.0", 1, 0, 0, null, null)]
    [InlineData("1.2.3", 1, 2, 3, null, null)]
    [InlineData("1.0.0-alpha", 1, 0, 0, "alpha", null)]
    [InlineData("1.0.0-alpha.1", 1, 0, 0, "alpha.1", null)]
    [InlineData("1.0.0+build.1", 1, 0, 0, null, "build.1")]
    [InlineData("1.0.0-alpha+build.1", 1, 0, 0, "alpha", "build.1")]
    public void Parse_ValidVersions_ShouldParseCorrectly(string version, int major, int minor, int patch, string? preRelease, string? build)
    {
        // Act
        var result = SemanticVersion.Parse(version);

        // Assert
        result.Major.Should().Be(major);
        result.Minor.Should().Be(minor);
        result.Patch.Should().Be(patch);
        result.PreRelease.Should().Be(preRelease);
        result.Build.Should().Be(build);
        result.ToString().Should().Be(version);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("1.2")]
    [InlineData("1.2.3.4")]
    [InlineData("invalid")]
    [InlineData("1.2.3-")]
    [InlineData("1.2.3+")]
    public void Parse_InvalidVersions_ShouldThrowException(string version)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => SemanticVersion.Parse(version));
    }

    [Theory]
    [InlineData("1.0.0", "1.0.0", 0)]
    [InlineData("1.0.0", "1.0.1", -1)]
    [InlineData("1.0.1", "1.0.0", 1)]
    [InlineData("1.0.0", "1.1.0", -1)]
    [InlineData("1.1.0", "1.0.0", 1)]
    [InlineData("1.0.0", "2.0.0", -1)]
    [InlineData("2.0.0", "1.0.0", 1)]
    [InlineData("1.0.0-alpha", "1.0.0", -1)]
    [InlineData("1.0.0", "1.0.0-alpha", 1)]
    [InlineData("1.0.0-alpha", "1.0.0-beta", -1)]
    public void CompareTo_VersionComparisons_ShouldReturnCorrectResult(string version1, string version2, int expected)
    {
        // Arrange
        var v1 = SemanticVersion.Parse(version1);
        var v2 = SemanticVersion.Parse(version2);

        // Act
        var result = v1.CompareTo(v2);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("1.0.0", "1.0.0", true)]
    [InlineData("1.0.0", "1.0.1", false)]
    [InlineData("1.0.0-alpha", "1.0.0-alpha", true)]
    [InlineData("1.0.0-alpha", "1.0.0-beta", false)]
    public void Equals_VersionEquality_ShouldReturnCorrectResult(string version1, string version2, bool expected)
    {
        // Arrange
        var v1 = SemanticVersion.Parse(version1);
        var v2 = SemanticVersion.Parse(version2);

        // Act
        var result = v1.Equals(v2);

        // Assert
        result.Should().Be(expected);
    }
}

public class VersionRangeTests
{
    [Theory]
    [InlineData("1.0.0", VersionRange.RangeType.Exact, "1.0.0")]
    [InlineData("^1.0.0", VersionRange.RangeType.Compatible, "1.0.0")]
    [InlineData("~1.2.3", VersionRange.RangeType.Tilde, "1.2.3")]
    public void Parse_ValidRanges_ShouldParseCorrectly(string range, VersionRange.RangeType expectedType, string expectedVersion)
    {
        // Act
        var result = VersionRange.Parse(range);

        // Assert
        result.Type.Should().Be(expectedType);
        result.Version.ToString().Should().Be(expectedVersion);
    }

    [Theory]
    [InlineData("1.0.0", "1.0.0", true)]  // Exact match
    [InlineData("1.0.0", "1.0.1", false)] // Exact mismatch
    [InlineData("^1.0.0", "1.0.0", true)]  // Compatible: same version
    [InlineData("^1.0.0", "1.5.0", true)]  // Compatible: same major
    [InlineData("^1.0.0", "2.0.0", false)] // Compatible: different major
    [InlineData("~1.2.0", "1.2.0", true)]  // Tilde: same version
    [InlineData("~1.2.0", "1.2.5", true)]  // Tilde: same major.minor
    [InlineData("~1.2.0", "1.3.0", false)] // Tilde: different minor
    public void Satisfies_VersionRequirements_ShouldReturnCorrectResult(string rangeStr, string versionStr, bool expected)
    {
        // Arrange
        var range = VersionRange.Parse(rangeStr);
        var version = SemanticVersion.Parse(versionStr);

        // Act
        var result = range.Satisfies(version);

        // Assert
        result.Should().Be(expected);
    }
}

public class PluginDependencyResolverTests
{
    [Fact]
    public void ResolveLoadOrder_NoDependencies_ShouldReturnOriginalOrder()
    {
        // Arrange
        var resolver = new PluginDependencyResolver();
        var manifests = new[]
        {
            CreateManifest("plugin1", "1.0.0"),
            CreateManifest("plugin2", "1.0.0"),
            CreateManifest("plugin3", "1.0.0")
        };

        // Act
        var result = resolver.ResolveLoadOrder(manifests);

        // Assert
        result.Should().HaveCount(3);
        result.Select(m => m.Id).Should().Contain("plugin1", "plugin2", "plugin3");
    }

    [Fact]
    public void ResolveLoadOrder_WithDependencies_ShouldReturnCorrectOrder()
    {
        // Arrange
        var resolver = new PluginDependencyResolver();
        var manifests = new[]
        {
            CreateManifest("plugin1", "1.0.0", ("plugin2", "^1.0.0")),
            CreateManifest("plugin2", "1.0.0", ("plugin3", "^1.0.0")),
            CreateManifest("plugin3", "1.0.0")
        };

        // Act
        var result = resolver.ResolveLoadOrder(manifests).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Id.Should().Be("plugin3"); // No dependencies
        result[1].Id.Should().Be("plugin2"); // Depends on plugin3
        result[2].Id.Should().Be("plugin1"); // Depends on plugin2
    }

    [Fact]
    public void ResolveLoadOrder_CircularDependencies_ShouldThrowException()
    {
        // Arrange
        var resolver = new PluginDependencyResolver();
        var manifests = new[]
        {
            CreateManifest("plugin1", "1.0.0", ("plugin2", "^1.0.0")),
            CreateManifest("plugin2", "1.0.0", ("plugin1", "^1.0.0"))
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => resolver.ResolveLoadOrder(manifests).ToList());
    }

    [Fact]
    public void ValidateDependencies_IncompatibleVersions_ShouldReturnFalse()
    {
        // Arrange
        var resolver = new PluginDependencyResolver();
        var manifests = new[]
        {
            CreateManifest("plugin1", "1.0.0", ("plugin2", "^2.0.0")), // Requires v2
            CreateManifest("plugin2", "1.0.0") // Available v1
        };

        // Act
        var result = resolver.ValidateDependencies(manifests);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void FindBestVersion_MultipleVersions_ShouldReturnLatestCompatible()
    {
        // Arrange
        var resolver = new PluginDependencyResolver();
        var manifests = new[]
        {
            CreateManifest("plugin1", "1.0.0"),
            CreateManifest("plugin1", "1.1.0"),
            CreateManifest("plugin1", "1.2.0"),
            CreateManifest("plugin1", "2.0.0")
        };

        // Act
        var result = resolver.FindBestVersion(manifests, "plugin1", "^1.0.0");

        // Assert
        result.Should().NotBeNull();
        result!.Version.Should().Be("1.2.0"); // Latest 1.x version
    }

    private static PluginManifest CreateManifest(string id, string version, params (string depId, string depVersion)[] dependencies)
    {
        var manifest = new PluginManifest
        {
            Id = id,
            Version = version,
            Name = $"Test Plugin {id}",
            Description = "Test plugin",
            Dependencies = dependencies.ToDictionary(d => d.depId, d => d.depVersion)
        };
        return manifest;
    }
}

public class PluginRegistryTests
{
    private readonly string _tempRegistryPath;

    public PluginRegistryTests()
    {
        _tempRegistryPath = Path.GetTempFileName();
        File.Delete(_tempRegistryPath); // Start with non-existent file
    }

    [Fact]
    public async Task RegisterPluginAsync_NewPlugin_ShouldAddToRegistry()
    {
        // Arrange
        using var registry = new FilePluginRegistry(_tempRegistryPath);
        var manifest = CreateTestManifest("test-plugin", "1.0.0");

        // Act
        await registry.RegisterPluginAsync(manifest);
        var result = await registry.GetPluginAsync("test-plugin");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("test-plugin");
        result.Version.Should().Be("1.0.0");
    }

    [Fact]
    public async Task RegisterPluginAsync_MultipleVersions_ShouldKeepLatestFirst()
    {
        // Arrange
        using var registry = new FilePluginRegistry(_tempRegistryPath);
        var manifest1 = CreateTestManifest("test-plugin", "1.0.0");
        var manifest2 = CreateTestManifest("test-plugin", "1.1.0");
        var manifest3 = CreateTestManifest("test-plugin", "1.0.5");

        // Act
        await registry.RegisterPluginAsync(manifest1);
        await registry.RegisterPluginAsync(manifest2);
        await registry.RegisterPluginAsync(manifest3);

        var latest = await registry.GetPluginAsync("test-plugin");
        var versions = await registry.GetPluginVersionsAsync("test-plugin");

        // Assert
        latest!.Version.Should().Be("1.1.0"); // Latest version
        versions.Should().HaveCount(3);
        versions.First().Version.Should().Be("1.1.0"); // Sorted descending
    }

    [Fact]
    public async Task FindPluginsAsync_WithCriteria_ShouldFilterCorrectly()
    {
        // Arrange
        using var registry = new FilePluginRegistry(_tempRegistryPath);
        await registry.RegisterPluginAsync(CreateTestManifest("plugin1", "1.0.0", author: "TestAuthor", capabilities: new[] { "pty" }));
        await registry.RegisterPluginAsync(CreateTestManifest("plugin2", "2.0.0", author: "OtherAuthor", capabilities: new[] { "recording" }));

        var criteria = new PluginSearchCriteria
        {
            Author = "TestAuthor",
            RequiredCapabilities = new List<string> { "pty" }
        };

        // Act
        var results = await registry.FindPluginsAsync(criteria);

        // Assert
        results.Should().HaveCount(1);
        results.First().Id.Should().Be("plugin1");
    }

    [Fact]
    public async Task GetStatisticsAsync_MultiplePlugins_ShouldReturnCorrectStats()
    {
        // Arrange
        using var registry = new FilePluginRegistry(_tempRegistryPath);
        await registry.RegisterPluginAsync(CreateTestManifest("plugin1", "1.0.0", profiles: new[] { "console" }));
        await registry.RegisterPluginAsync(CreateTestManifest("plugin2", "1.0.0", profiles: new[] { "console", "unity" }));

        // Act
        var stats = await registry.GetStatisticsAsync();

        // Assert
        stats.TotalPlugins.Should().Be(2);
        stats.UniquePlugins.Should().Be(2);
        stats.PluginsByProfile["console"].Should().Be(2);
        stats.PluginsByProfile["unity"].Should().Be(1);
    }

    private static PluginManifest CreateTestManifest(string id, string version, string author = "TestAuthor",
        string[]? capabilities = null, string[]? profiles = null)
    {
        return new PluginManifest
        {
            Id = id,
            Version = version,
            Name = $"Test Plugin {id}",
            Description = "Test plugin",
            Author = author,
            Capabilities = capabilities?.ToList() ?? new List<string>(),
            SupportedProfiles = profiles?.ToList() ?? new List<string> { "console" }
        };
    }

    public void Dispose()
    {
        if (File.Exists(_tempRegistryPath))
            File.Delete(_tempRegistryPath);
    }
}

public class PluginSecurityTests
{
    [Fact]
    public void DefaultPluginPermissionEnforcer_RegisterPermissions_ShouldAllowAccess()
    {
        // Arrange
        var enforcer = new DefaultPluginPermissionEnforcer();
        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions { CanRead = true, CanWrite = false },
            Network = new NetworkPermissions { CanHttpClient = true, CanListen = false }
        };

        // Act
        enforcer.RegisterPermissions("test-plugin", permissions);

        // Assert
        enforcer.HasPermission("test-plugin", "filesystem.read").Should().BeTrue();
        enforcer.HasPermission("test-plugin", "filesystem.write").Should().BeFalse();
        enforcer.HasPermission("test-plugin", "network.http").Should().BeTrue();
        enforcer.HasPermission("test-plugin", "network.listen").Should().BeFalse();
    }

    [Fact]
    public void DefaultPluginPermissionEnforcer_EnforcePermission_ShouldThrowWhenDenied()
    {
        // Arrange
        var enforcer = new DefaultPluginPermissionEnforcer();
        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions { CanWrite = false }
        };
        enforcer.RegisterPermissions("test-plugin", permissions);

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(() => 
            enforcer.EnforcePermission("test-plugin", "filesystem.write"));
    }

    [Fact]
    public void DefaultPluginPermissionEnforcer_UnregisteredPlugin_ShouldDenyAccess()
    {
        // Arrange
        var enforcer = new DefaultPluginPermissionEnforcer();

        // Act & Assert
        enforcer.HasPermission("unknown-plugin", "filesystem.read").Should().BeFalse();
    }
}
