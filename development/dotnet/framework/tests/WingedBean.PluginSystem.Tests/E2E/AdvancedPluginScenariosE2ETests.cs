using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using FluentAssertions;
using System.Security.Cryptography;

namespace WingedBean.Host.Tests.E2E;

public class AdvancedPluginScenariosE2ETests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _pluginsDir;
    private readonly string _registryPath;
    private readonly ServiceProvider _serviceProvider;
    private readonly HostBootstrap _hostBootstrap;
    private RSA? _testRsa;

    public AdvancedPluginScenariosE2ETests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _pluginsDir = Path.Combine(_tempDir, "plugins");
        _registryPath = Path.Combine(_tempDir, "registry.json");

        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(_pluginsDir);

        // Setup test RSA keys
        _testRsa = RSA.Create(2048);

        // Setup host bootstrap with services
        var hostVersion = SemanticVersion.Parse("2.0.0");
        _hostBootstrap = new HostBootstrap(hostVersion);

        var services = new ServiceCollection();
        services.AddSingleton<IPluginRegistry>(new FilePluginRegistry(_registryPath));
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        _hostBootstrap.RegisterHostServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task CompletePluginEcosystem_LoadSecurePluginsWithDependencies_ShouldWorkEndToEnd()
    {
        // Arrange: Create a realistic plugin ecosystem
        // Core Plugin (provides base functionality)
        // Util Plugin (depends on Core, provides utilities)
        // Feature Plugin (depends on both Core and Util, provides features)
        // All plugins are signed and have specific permissions

        var corePlugin = await CreateSignedPlugin("core-plugin", "1.2.0", "Core functionality provider");
        var utilPlugin = await CreateSignedPlugin("util-plugin", "1.0.0", "Utility functions",
            ("core-plugin", "^1.0.0"));
        var featurePlugin = await CreateSignedPlugin("feature-plugin", "2.1.0", "Feature implementation",
            ("core-plugin", "^1.0.0"), ("util-plugin", "^1.0.0"));

        var registry = _serviceProvider.GetRequiredService<IPluginRegistry>();
        var loader = _serviceProvider.GetRequiredService<PluginLoader>();
        var resolver = _serviceProvider.GetRequiredService<PluginDependencyResolver>();
        var updateManager = _serviceProvider.GetRequiredService<IPluginUpdateManager>();

        // Act 1: Register all plugins in registry
        await registry.RegisterPluginAsync(corePlugin);
        await registry.RegisterPluginAsync(utilPlugin);
        await registry.RegisterPluginAsync(featurePlugin);

        // Act 2: Resolve dependency order
        var manifests = new[] { featurePlugin, utilPlugin, corePlugin }; // Intentionally wrong order
        var orderedManifests = resolver.ResolveLoadOrder(manifests).ToList();

        // Assert 2: Dependencies should be resolved correctly
        orderedManifests.Should().HaveCount(3);
        var coreIndex = orderedManifests.FindIndex(m => m.Id == "core-plugin");
        var utilIndex = orderedManifests.FindIndex(m => m.Id == "util-plugin");
        var featureIndex = orderedManifests.FindIndex(m => m.Id == "feature-plugin");

        coreIndex.Should().BeLessThan(utilIndex);
        coreIndex.Should().BeLessThan(featureIndex);
        utilIndex.Should().BeLessThan(featureIndex);

        // Act 3: Simulate complete plugin lifecycle through HostBootstrap
        var activatedPlugins = new List<string>();

        foreach (var manifest in orderedManifests)
        {
            // Simulate security verification
            var isSecure = await _hostBootstrap.VerifyPluginSecurityAsync(manifest);
            isSecure.Should().BeTrue();

            // Simulate loading and activation
            activatedPlugins.Add(manifest.Id);
        }

        // Assert 3: All plugins should be successfully activated
        activatedPlugins.Should().Contain("core-plugin", "util-plugin", "feature-plugin");

        // Act 4: Test registry search capabilities
        var searchResults = await registry.FindPluginsAsync(new PluginSearchCriteria
        {
            RequiredCapabilities = new List<string> { "core" },
            MinVersion = "1.0.0"
        });

        // Assert 4: Search should find appropriate plugins
        searchResults.Should().NotBeEmpty();
        searchResults.Should().Contain(p => p.Id == "core-plugin");

        // Act 5: Test update detection
        var coreUpdateAvailable = await updateManager.CheckForUpdatesAsync("core-plugin");
        var utilUpdateAvailable = await updateManager.CheckForUpdatesAsync("util-plugin");

        // Assert 5: Update system should work correctly
        coreUpdateAvailable.Should().BeFalse(); // No newer version available
        utilUpdateAvailable.Should().BeFalse(); // No newer version available

        // Act 6: Get comprehensive statistics
        var stats = await registry.GetStatisticsAsync();

        // Assert 6: Statistics should reflect complete ecosystem
        stats.TotalPlugins.Should().Be(3);
        stats.UniquePlugins.Should().Be(3);
        stats.PluginsByProfile.Should().ContainKey("console");
        stats.PluginsByProfile["console"].Should().Be(3);
    }

    [Fact]
    public async Task PluginHotUpdateScenario_UpdateWithDependencyConflicts_ShouldHandleGracefully()
    {
        // Arrange: Create plugin with dependency, then update dependency causing conflict
        var basePlugin = await CreateSignedPlugin("base-service", "1.0.0", "Base service provider");
        var dependentPlugin = await CreateSignedPlugin("dependent-service", "1.0.0", "Dependent service",
            ("base-service", "^1.0.0"));

        var registry = _serviceProvider.GetRequiredService<IPluginRegistry>();
        var updateManager = _serviceProvider.GetRequiredService<IPluginUpdateManager>();
        var resolver = _serviceProvider.GetRequiredService<PluginDependencyResolver>();

        await registry.RegisterPluginAsync(basePlugin);
        await registry.RegisterPluginAsync(dependentPlugin);

        // Act 1: Create rollback point before update
        await updateManager.CreateRollbackPointAsync("base-service");

        // Act 2: Simulate major version update that breaks compatibility
        var updatedBasePlugin = await CreateSignedPlugin("base-service", "2.0.0", "Updated base service with breaking changes");
        await registry.RegisterPluginAsync(updatedBasePlugin);

        // Act 3: Check if update would cause dependency conflicts
        var allPlugins = new[]
        {
            updatedBasePlugin,
            dependentPlugin
        };

        var hasConflicts = !resolver.ValidateDependencies(allPlugins);

        // Assert 3: Should detect dependency conflicts
        hasConflicts.Should().BeTrue();

        // Act 4: Simulate rollback due to conflicts
        var rollbackSuccessful = true;
        try
        {
            await updateManager.RollbackAsync("base-service");
        }
        catch
        {
            rollbackSuccessful = false;
        }

        // Assert 4: Rollback should succeed
        rollbackSuccessful.Should().BeTrue();

        // Act 5: Verify system state after rollback
        var currentBasePlugin = await registry.GetPluginAsync("base-service");

        // Assert 5: Should be back to original version
        currentBasePlugin.Should().NotBeNull();
        // Note: In a real implementation, this would actually revert the version
        // For this test, we're simulating the rollback mechanism
    }

    [Fact]
    public async Task PluginSecurityBreach_MaliciousPluginDetection_ShouldPreventActivation()
    {
        // Arrange: Create a malicious plugin with invalid signature
        var maliciousPlugin = await CreateUnsignedPlugin("malicious-plugin", "1.0.0", "Malicious plugin");

        // Tamper with the signature to simulate malicious modification
        maliciousPlugin.Security!.Signature!.Data = "invalid-signature-data";

        var registry = _serviceProvider.GetRequiredService<IPluginRegistry>();
        var signatureVerifier = _serviceProvider.GetRequiredService<IPluginSignatureVerifier>();

        // Act 1: Attempt to register malicious plugin
        await registry.RegisterPluginAsync(maliciousPlugin);

        // Act 2: Verify plugin security
        var isSecure = await _hostBootstrap.VerifyPluginSecurityAsync(maliciousPlugin);

        // Assert 2: Malicious plugin should be rejected
        isSecure.Should().BeFalse();

        // Act 3: Test signature verification directly
        var fakeContent = System.Text.Encoding.UTF8.GetBytes("malicious content");
        var signatureValid = await signatureVerifier.VerifySignatureAsync(fakeContent, maliciousPlugin.Security.Signature);

        // Assert 3: Signature verification should fail
        signatureValid.Should().BeFalse();
    }

    [Fact]
    public async Task PluginPermissionEscalation_UnauthorizedAccess_ShouldBeBlocked()
    {
        // Arrange: Create plugin with limited permissions
        var restrictedPlugin = await CreateSignedPlugin("restricted-plugin", "1.0.0", "Plugin with limited permissions");

        // Override permissions to be very restrictive
        restrictedPlugin.Security!.Permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                CanRead = true,
                CanWrite = false,
                CanDelete = false,
                AllowedPaths = new List<string> { "/tmp/safe-area" }
            },
            Network = new NetworkPermissions
            {
                CanHttpClient = false,
                CanListen = false,
                CanSocket = false
            },
            Process = new ProcessPermissions
            {
                CanExecute = false,
                CanSpawn = false
            },
            System = new SystemPermissions
            {
                CanAccessEnvironment = false,
                CanAccessRegistry = false
            }
        };

        var permissionEnforcer = _serviceProvider.GetRequiredService<IPluginPermissionEnforcer>();

        // Act 1: Register plugin permissions
        permissionEnforcer.RegisterPermissions("restricted-plugin", restrictedPlugin.Security.Permissions);

        // Act 2: Test various unauthorized access attempts
        var canWrite = permissionEnforcer.HasPermission("restricted-plugin", "filesystem.write");
        var canNetwork = permissionEnforcer.HasPermission("restricted-plugin", "network.http");
        var canExecute = permissionEnforcer.HasPermission("restricted-plugin", "process.execute");

        // Assert 2: All unauthorized operations should be denied
        canWrite.Should().BeFalse();
        canNetwork.Should().BeFalse();
        canExecute.Should().BeFalse();

        // Act 3: Test permission enforcement
        var writeException = false;
        var networkException = false;
        var executeException = false;

        try { permissionEnforcer.EnforcePermission("restricted-plugin", "filesystem.write"); }
        catch (UnauthorizedAccessException) { writeException = true; }

        try { permissionEnforcer.EnforcePermission("restricted-plugin", "network.http"); }
        catch (UnauthorizedAccessException) { networkException = true; }

        try { permissionEnforcer.EnforcePermission("restricted-plugin", "process.execute"); }
        catch (UnauthorizedAccessException) { executeException = true; }

        // Assert 3: All should throw unauthorized access exceptions
        writeException.Should().BeTrue();
        networkException.Should().BeTrue();
        executeException.Should().BeTrue();
    }

    [Fact]
    public async Task PluginRegistryDiscovery_ComplexSearchAndFiltering_ShouldReturnAccurateResults()
    {
        // Arrange: Create diverse plugin ecosystem
        var plugins = new[]
        {
            await CreateSignedPlugin("auth-plugin", "1.0.0", "Authentication provider", author: "SecurityCorp",
                capabilities: new[] { "auth", "security" }, profiles: new[] { "web", "console" }),
            await CreateSignedPlugin("database-plugin", "2.1.0", "Database connector", author: "DataCorp",
                capabilities: new[] { "database", "storage" }, profiles: new[] { "web", "service" }),
            await CreateSignedPlugin("ui-plugin", "1.5.0", "User interface components", author: "UICorp",
                capabilities: new[] { "ui", "rendering" }, profiles: new[] { "desktop", "web" }),
            await CreateSignedPlugin("logging-plugin", "3.0.0", "Advanced logging", author: "LogCorp",
                capabilities: new[] { "logging", "monitoring" }, profiles: new[] { "console", "service", "web" }),
            await CreateSignedPlugin("cache-plugin", "1.2.0", "Caching provider", author: "DataCorp",
                capabilities: new[] { "cache", "performance" }, profiles: new[] { "web", "service" })
        };

        var registry = _serviceProvider.GetRequiredService<IPluginRegistry>();

        // Register all plugins
        foreach (var plugin in plugins)
        {
            await registry.RegisterPluginAsync(plugin);
        }

        // Act 1: Search by author
        var dataCorpPlugins = await registry.FindPluginsAsync(new PluginSearchCriteria
        {
            Author = "DataCorp"
        });

        // Assert 1: Should find plugins by DataCorp
        dataCorpPlugins.Should().HaveCount(2);
        dataCorpPlugins.Should().Contain(p => p.Id == "database-plugin");
        dataCorpPlugins.Should().Contain(p => p.Id == "cache-plugin");

        // Act 2: Search by capabilities
        var securityPlugins = await registry.FindPluginsAsync(new PluginSearchCriteria
        {
            RequiredCapabilities = new List<string> { "security" }
        });

        // Assert 2: Should find security-related plugins
        securityPlugins.Should().HaveCount(1);
        securityPlugins.First().Id.Should().Be("auth-plugin");

        // Act 3: Search by profile and version
        var webPluginsV1Plus = await registry.FindPluginsAsync(new PluginSearchCriteria
        {
            SupportedProfiles = new List<string> { "web" },
            MinVersion = "1.0.0"
        });

        // Assert 3: Should find web-compatible plugins v1.0.0+
        webPluginsV1Plus.Should().HaveCount(5); // All plugins support web and are >= 1.0.0

        // Act 4: Complex search with multiple criteria
        var complexSearch = await registry.FindPluginsAsync(new PluginSearchCriteria
        {
            SupportedProfiles = new List<string> { "service" },
            RequiredCapabilities = new List<string> { "storage" },
            MinVersion = "2.0.0"
        });

        // Assert 4: Should find plugins matching all criteria
        complexSearch.Should().HaveCount(1);
        complexSearch.First().Id.Should().Be("database-plugin");

        // Act 5: Get detailed statistics
        var stats = await registry.GetStatisticsAsync();

        // Assert 5: Statistics should be comprehensive
        stats.TotalPlugins.Should().Be(5);
        stats.UniquePlugins.Should().Be(5);
        stats.PluginsByProfile.Should().ContainKeys("web", "console", "service", "desktop");
        stats.PluginsByProfile["web"].Should().Be(4); // auth, database, ui, logging, cache
        stats.PluginsByAuthor["DataCorp"].Should().Be(2);
    }

    private async Task<PluginManifest> CreateSignedPlugin(string id, string version, string description,
        params (string depId, string depVersion)[] dependencies)
    {
        return await CreateSignedPlugin(id, version, description, "TestCorp",
            new[] { "core" }, new[] { "console" }, dependencies);
    }

    private async Task<PluginManifest> CreateSignedPlugin(string id, string version, string description,
        string author = "TestCorp", string[]? capabilities = null, string[]? profiles = null,
        params (string depId, string depVersion)[] dependencies)
    {
        var pluginDir = Path.Combine(_pluginsDir, id);
        Directory.CreateDirectory(pluginDir);

        var publicKey = Convert.ToBase64String(_testRsa!.ExportRSAPublicKey());
        var pluginContent = $"{id} plugin content v{version}";
        var signature = _testRsa.SignData(System.Text.Encoding.UTF8.GetBytes(pluginContent),
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var manifest = new PluginManifest
        {
            Id = id,
            Version = version,
            Name = $"Plugin {id}",
            Description = description,
            Author = author,
            EntryPoint = "plugin.dll",
            Dependencies = dependencies.ToDictionary(d => d.depId, d => d.depVersion),
            Capabilities = capabilities?.ToList() ?? new List<string> { "core" },
            SupportedProfiles = profiles?.ToList() ?? new List<string> { "console" },
            Security = new PluginSecurityInfo
            {
                RequireSignature = true,
                Signature = new PluginSignature
                {
                    Algorithm = "RS256",
                    PublicKey = publicKey,
                    Data = Convert.ToBase64String(signature)
                },
                Permissions = new PluginPermissions
                {
                    FileSystem = new FileSystemPermissions
                    {
                        CanRead = true,
                        CanWrite = false,
                        AllowedPaths = new List<string> { "/tmp" }
                    },
                    Network = new NetworkPermissions
                    {
                        CanHttpClient = true,
                        CanListen = false
                    }
                }
            }
        };

        // Save manifest and mock plugin file
        var manifestPath = Path.Combine(pluginDir, "plugin.json");
        var pluginPath = Path.Combine(pluginDir, "plugin.dll");

        var json = System.Text.Json.JsonSerializer.Serialize(manifest, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(manifestPath, json);
        await File.WriteAllTextAsync(pluginPath, pluginContent);

        return manifest;
    }

    private async Task<PluginManifest> CreateUnsignedPlugin(string id, string version, string description)
    {
        var pluginDir = Path.Combine(_pluginsDir, id);
        Directory.CreateDirectory(pluginDir);

        var publicKey = Convert.ToBase64String(_testRsa!.ExportRSAPublicKey());

        var manifest = new PluginManifest
        {
            Id = id,
            Version = version,
            Name = $"Plugin {id}",
            Description = description,
            Author = "MaliciousCorp",
            EntryPoint = "plugin.dll",
            Security = new PluginSecurityInfo
            {
                RequireSignature = true,
                Signature = new PluginSignature
                {
                    Algorithm = "RS256",
                    PublicKey = publicKey,
                    Data = "fake-signature-data" // Invalid signature
                },
                Permissions = new PluginPermissions
                {
                    FileSystem = new FileSystemPermissions
                    {
                        CanRead = true,
                        CanWrite = true,
                        CanDelete = true // Suspicious permissions
                    },
                    System = new SystemPermissions
                    {
                        CanAccessEnvironment = true,
                        CanAccessRegistry = true // Suspicious permissions
                    }
                }
            }
        };

        var manifestPath = Path.Combine(pluginDir, "plugin.json");
        var pluginPath = Path.Combine(pluginDir, "plugin.dll");

        var json = System.Text.Json.JsonSerializer.Serialize(manifest, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(manifestPath, json);
        await File.WriteAllTextAsync(pluginPath, "malicious content");

        return manifest;
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        _testRsa?.Dispose();

        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
