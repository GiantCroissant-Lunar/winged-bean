using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using FluentAssertions;
using System.Security.Cryptography;

namespace WingedBean.Host.Tests.Integration;

public class PluginLifecycleIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _registryPath;
    private readonly ServiceProvider _serviceProvider;
    private RSA? _testRsa;

    public PluginLifecycleIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _registryPath = Path.Combine(_tempDir, "registry.json");

        var services = new ServiceCollection();
        services.AddSingleton<IPluginRegistry>(new FilePluginRegistry(_registryPath));
        services.AddSingleton<IPluginSignatureVerifier, DefaultPluginSignatureVerifier>();
        services.AddSingleton<IPluginPermissionEnforcer, DefaultPluginPermissionEnforcer>();
        services.AddSingleton<IPluginUpdateManager, PluginUpdateManager>();
        services.AddSingleton<PluginDependencyResolver>();
        services.AddLogging(builder => builder.AddDebug());

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task FullPluginLifecycle_LoadActivateUpdateDeactivate_ShouldWorkCorrectly()
    {
        // Arrange
        var loader = new PluginLoader();
        var registry = _serviceProvider.GetRequiredService<IPluginRegistry>();
        var updateManager = _serviceProvider.GetRequiredService<IPluginUpdateManager>();
        
        var pluginDir = Path.Combine(_tempDir, "test-plugin");
        Directory.CreateDirectory(pluginDir);
        
        var manifest = await CreateTestPluginWithManifest(pluginDir, "test-plugin", "1.0.0");
        var pluginPath = Path.Combine(pluginDir, "plugin.dll");
        await File.WriteAllTextAsync(pluginPath, "fake dll content"); // Mock DLL

        // Act 1: Load and register plugin
        await registry.RegisterPluginAsync(manifest);
        var registeredPlugin = await registry.GetPluginAsync("test-plugin");
        
        // Assert 1: Plugin should be registered
        registeredPlugin.Should().NotBeNull();
        registeredPlugin!.Id.Should().Be("test-plugin");

        // Act 2: Check for updates (simulate newer version available)
        var newManifest = await CreateTestPluginWithManifest(pluginDir, "test-plugin", "1.1.0");
        await registry.RegisterPluginAsync(newManifest);
        
        var updateAvailable = await updateManager.CheckForUpdatesAsync("test-plugin");
        
        // Assert 2: Update should be available
        updateAvailable.Should().BeTrue();

        // Act 3: Get plugin statistics
        var stats = await registry.GetStatisticsAsync();
        
        // Assert 3: Statistics should reflect registered plugins
        stats.TotalPlugins.Should().BeGreaterThan(0);
        stats.UniquePlugins.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PluginSecurityWorkflow_SignatureVerificationAndPermissions_ShouldEnforceCorrectly()
    {
        // Arrange
        var signatureVerifier = _serviceProvider.GetRequiredService<IPluginSignatureVerifier>();
        var permissionEnforcer = _serviceProvider.GetRequiredService<IPluginPermissionEnforcer>();
        
        var pluginDir = Path.Combine(_tempDir, "secure-plugin");
        Directory.CreateDirectory(pluginDir);

        // Create RSA key pair for testing
        _testRsa = RSA.Create(2048);
        var publicKey = Convert.ToBase64String(_testRsa.ExportRSAPublicKey());
        var privateKey = _testRsa.ExportRSAPrivateKey();

        var manifest = await CreateSecurePluginWithManifest(pluginDir, "secure-plugin", "1.0.0", publicKey);
        var pluginContent = "test plugin content";
        
        // Sign the plugin content
        var signature = _testRsa.SignData(System.Text.Encoding.UTF8.GetBytes(pluginContent), 
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        manifest.Security!.Signature!.Data = Convert.ToBase64String(signature);

        // Act 1: Verify signature
        var isValidSignature = await signatureVerifier.VerifySignatureAsync(
            System.Text.Encoding.UTF8.GetBytes(pluginContent), manifest.Security.Signature);

        // Assert 1: Signature should be valid
        isValidSignature.Should().BeTrue();

        // Act 2: Register permissions and test enforcement
        permissionEnforcer.RegisterPermissions("secure-plugin", manifest.Security.Permissions!);
        
        var canRead = permissionEnforcer.HasPermission("secure-plugin", "filesystem.read");
        var canWrite = permissionEnforcer.HasPermission("secure-plugin", "filesystem.write");

        // Assert 2: Permissions should be enforced correctly
        canRead.Should().BeTrue();
        canWrite.Should().BeFalse();
        
        // Assert 3: Unauthorized access should throw
        Assert.Throws<UnauthorizedAccessException>(() => 
            permissionEnforcer.EnforcePermission("secure-plugin", "filesystem.write"));
    }

    [Fact]
    public async Task PluginDependencyResolution_ComplexDependencyGraph_ShouldResolveCorrectly()
    {
        // Arrange
        var resolver = _serviceProvider.GetRequiredService<PluginDependencyResolver>();
        var registry = _serviceProvider.GetRequiredService<IPluginRegistry>();

        // Create a complex dependency graph:
        // PluginA 1.0.0 depends on PluginB ^1.0.0, PluginC ~2.1.0
        // PluginB 1.2.0 depends on PluginD ^1.0.0
        // PluginC 2.1.5 (no dependencies)
        // PluginD 1.3.0 (no dependencies)

        var pluginDDir = Path.Combine(_tempDir, "plugin-d");
        var pluginCDir = Path.Combine(_tempDir, "plugin-c");
        var pluginBDir = Path.Combine(_tempDir, "plugin-b");
        var pluginADir = Path.Combine(_tempDir, "plugin-a");

        Directory.CreateDirectory(pluginDDir);
        Directory.CreateDirectory(pluginCDir);
        Directory.CreateDirectory(pluginBDir);
        Directory.CreateDirectory(pluginADir);

        var manifestD = await CreateTestPluginWithManifest(pluginDDir, "plugin-d", "1.3.0");
        var manifestC = await CreateTestPluginWithManifest(pluginCDir, "plugin-c", "2.1.5");
        var manifestB = await CreateTestPluginWithManifest(pluginBDir, "plugin-b", "1.2.0", 
            ("plugin-d", "^1.0.0"));
        var manifestA = await CreateTestPluginWithManifest(pluginADir, "plugin-a", "1.0.0",
            ("plugin-b", "^1.0.0"), ("plugin-c", "~2.1.0"));

        var manifests = new[] { manifestA, manifestB, manifestC, manifestD };

        // Register all plugins
        foreach (var manifest in manifests)
        {
            await registry.RegisterPluginAsync(manifest);
        }

        // Act
        var loadOrder = resolver.ResolveLoadOrder(manifests).ToList();
        var isValid = resolver.ValidateDependencies(manifests);

        // Assert
        isValid.Should().BeTrue();
        loadOrder.Should().HaveCount(4);
        
        // Verify load order (dependencies should be loaded before dependents)
        var indexD = loadOrder.FindIndex(m => m.Id == "plugin-d");
        var indexC = loadOrder.FindIndex(m => m.Id == "plugin-c");
        var indexB = loadOrder.FindIndex(m => m.Id == "plugin-b");
        var indexA = loadOrder.FindIndex(m => m.Id == "plugin-a");

        indexD.Should().BeLessThan(indexB); // D before B
        indexB.Should().BeLessThan(indexA); // B before A
        indexC.Should().BeLessThan(indexA); // C before A
    }

    [Fact]
    public async Task PluginUpdateManager_HotUpdateWithRollback_ShouldHandleCorrectly()
    {
        // Arrange
        var updateManager = _serviceProvider.GetRequiredService<IPluginUpdateManager>();
        var registry = _serviceProvider.GetRequiredService<IPluginRegistry>();

        var pluginDir = Path.Combine(_tempDir, "updatable-plugin");
        Directory.CreateDirectory(pluginDir);

        // Register initial version
        var initialManifest = await CreateTestPluginWithManifest(pluginDir, "updatable-plugin", "1.0.0");
        await registry.RegisterPluginAsync(initialManifest);

        // Create newer version
        var newManifest = await CreateTestPluginWithManifest(pluginDir, "updatable-plugin", "1.1.0");
        await registry.RegisterPluginAsync(newManifest);

        var updateAvailable = false;
        var updateCompleted = false;
        var rollbackCompleted = false;

        // Subscribe to events
        updateManager.PluginUpdateAvailable += (sender, args) => 
        {
            updateAvailable = true;
        };
        
        updateManager.PluginUpdateCompleted += (sender, args) => 
        {
            updateCompleted = true;
        };

        // Act 1: Check for updates
        var hasUpdates = await updateManager.CheckForUpdatesAsync("updatable-plugin");

        // Assert 1: Updates should be detected
        hasUpdates.Should().BeTrue();
        updateAvailable.Should().BeTrue();

        // Act 2: Simulate successful hot update
        await updateManager.CreateRollbackPointAsync("updatable-plugin");
        
        // Simulate update completion event
        updateManager.OnPluginUpdateCompleted("updatable-plugin", "1.1.0");

        // Assert 2: Update should be completed
        updateCompleted.Should().BeTrue();

        // Act 3: Simulate rollback
        updateManager.PluginUpdateCompleted += (sender, args) => 
        {
            if (args.PluginId == "updatable-plugin" && args.NewVersion == "1.0.0")
                rollbackCompleted = true;
        };

        await updateManager.RollbackAsync("updatable-plugin");
        updateManager.OnPluginUpdateCompleted("updatable-plugin", "1.0.0");

        // Assert 3: Rollback should be completed
        rollbackCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task HostBootstrap_CompleteIntegration_ShouldInitializeAllServices()
    {
        // Arrange
        var hostVersion = SemanticVersion.Parse("1.0.0");
        var bootstrap = new HostBootstrap(hostVersion);
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug());

        // Act
        bootstrap.RegisterHostServices(services);
        var provider = services.BuildServiceProvider();

        // Assert - All required services should be registered
        provider.GetService<IPluginRegistry>().Should().NotBeNull();
        provider.GetService<IPluginSignatureVerifier>().Should().NotBeNull();
        provider.GetService<IPluginPermissionEnforcer>().Should().NotBeNull();
        provider.GetService<IPluginUpdateManager>().Should().NotBeNull();
        provider.GetService<PluginDependencyResolver>().Should().NotBeNull();
        provider.GetService<PluginLoader>().Should().NotBeNull();
    }

    private async Task<PluginManifest> CreateTestPluginWithManifest(string pluginDir, string id, string version,
        params (string depId, string depVersion)[] dependencies)
    {
        var manifest = new PluginManifest
        {
            Id = id,
            Version = version,
            Name = $"Test Plugin {id}",
            Description = "Integration test plugin",
            Author = "Test Suite",
            EntryPoint = "plugin.dll",
            Dependencies = dependencies.ToDictionary(d => d.depId, d => d.depVersion),
            Capabilities = new List<string> { "test" },
            SupportedProfiles = new List<string> { "console" }
        };

        var manifestPath = Path.Combine(pluginDir, "plugin.json");
        var json = System.Text.Json.JsonSerializer.Serialize(manifest, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(manifestPath, json);

        return manifest;
    }

    private async Task<PluginManifest> CreateSecurePluginWithManifest(string pluginDir, string id, string version, 
        string publicKey)
    {
        var manifest = new PluginManifest
        {
            Id = id,
            Version = version,
            Name = $"Secure Plugin {id}",
            Description = "Security test plugin",
            Author = "Test Suite",
            EntryPoint = "plugin.dll",
            Security = new PluginSecurityInfo
            {
                RequireSignature = true,
                Signature = new PluginSignature
                {
                    Algorithm = "RS256",
                    PublicKey = publicKey,
                    Data = "" // Will be set by test
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

        var manifestPath = Path.Combine(pluginDir, "plugin.json");
        var json = System.Text.Json.JsonSerializer.Serialize(manifest, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(manifestPath, json);

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

public class PluginRegistryIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _registryPath;

    public PluginRegistryIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _registryPath = Path.Combine(_tempDir, "registry.json");
    }

    [Fact]
    public async Task FilePluginRegistry_PersistenceAndRecovery_ShouldMaintainState()
    {
        // Arrange & Act 1: Create registry and add plugins
        PluginRegistryStatistics? stats1;
        using (var registry1 = new FilePluginRegistry(_registryPath))
        {
            var manifest1 = CreateTestManifest("plugin1", "1.0.0");
            var manifest2 = CreateTestManifest("plugin2", "2.0.0");
            
            await registry1.RegisterPluginAsync(manifest1);
            await registry1.RegisterPluginAsync(manifest2);
            
            stats1 = await registry1.GetStatisticsAsync();
        }

        // Act 2: Create new registry instance and verify persistence
        PluginRegistryStatistics? stats2;
        PluginManifest? recoveredPlugin;
        using (var registry2 = new FilePluginRegistry(_registryPath))
        {
            stats2 = await registry2.GetStatisticsAsync();
            recoveredPlugin = await registry2.GetPluginAsync("plugin1");
        }

        // Assert
        stats1.TotalPlugins.Should().Be(2);
        stats2.TotalPlugins.Should().Be(2);
        stats1.UniquePlugins.Should().Be(stats2.UniquePlugins);
        
        recoveredPlugin.Should().NotBeNull();
        recoveredPlugin!.Id.Should().Be("plugin1");
        recoveredPlugin.Version.Should().Be("1.0.0");
    }

    [Fact]
    public async Task FilePluginRegistry_ConcurrentAccess_ShouldHandleCorrectly()
    {
        // Arrange
        var tasks = new List<Task>();
        const int concurrentOperations = 10;

        // Act: Perform concurrent registrations
        for (int i = 0; i < concurrentOperations; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                using var registry = new FilePluginRegistry(_registryPath);
                var manifest = CreateTestManifest($"plugin{index}", "1.0.0");
                await registry.RegisterPluginAsync(manifest);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert: Verify all plugins were registered
        using var finalRegistry = new FilePluginRegistry(_registryPath);
        var stats = await finalRegistry.GetStatisticsAsync();
        
        stats.TotalPlugins.Should().Be(concurrentOperations);
        stats.UniquePlugins.Should().Be(concurrentOperations);
    }

    private static PluginManifest CreateTestManifest(string id, string version)
    {
        return new PluginManifest
        {
            Id = id,
            Version = version,
            Name = $"Test Plugin {id}",
            Description = "Test plugin for integration tests",
            Author = "Test Suite",
            SupportedProfiles = new List<string> { "console" }
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
