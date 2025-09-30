# Advanced Plugin Features Developer Guide

## Overview

This guide covers the advanced plugin features implemented in RFC-0003 Phase 3, including semantic versioning, security framework, hot updates, and plugin registry capabilities.

## Table of Contents

- [Semantic Versioning](#semantic-versioning)
- [Plugin Security](#plugin-security)
- [Plugin Registry](#plugin-registry)
- [Hot Updates](#hot-updates)
- [Dependency Management](#dependency-management)
- [Integration Examples](#integration-examples)

## Semantic Versioning

### SemanticVersion Class

The `SemanticVersion` class provides full semantic versioning support following the [SemVer 2.0.0](https://semver.org/) specification.

```csharp
// Parse version strings
var version = SemanticVersion.Parse("1.2.3-alpha.1+build.123");

// Access version components
Console.WriteLine($"Major: {version.Major}");      // 1
Console.WriteLine($"Minor: {version.Minor}");      // 2
Console.WriteLine($"Patch: {version.Patch}");      // 3
Console.WriteLine($"PreRelease: {version.PreRelease}"); // alpha.1
Console.WriteLine($"Build: {version.Build}");      // build.123

// Version comparison
var v1 = SemanticVersion.Parse("1.0.0");
var v2 = SemanticVersion.Parse("1.0.1");
Console.WriteLine(v1.CompareTo(v2)); // -1 (v1 < v2)
```

### Version Ranges

The `VersionRange` class supports common versioning patterns:

- **Exact**: `1.0.0` - must match exactly
- **Compatible**: `^1.0.0` - compatible within same major version
- **Tilde**: `~1.2.0` - reasonably close to the specified version

```csharp
// Create version ranges
var exactRange = VersionRange.Parse("1.0.0");
var compatibleRange = VersionRange.Parse("^1.0.0");
var tildeRange = VersionRange.Parse("~1.2.0");

// Check if version satisfies range
var version = SemanticVersion.Parse("1.5.0");
Console.WriteLine(compatibleRange.Satisfies(version)); // true
Console.WriteLine(tildeRange.Satisfies(version));      // false
```

## Plugin Security

### Digital Signatures

Plugins can be digitally signed using RSA signatures to ensure authenticity and integrity.

```csharp
// Create plugin signature
var rsa = RSA.Create(2048);
var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
var pluginContent = File.ReadAllBytes("plugin.dll");

var signature = rsa.SignData(pluginContent, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

var pluginSignature = new PluginSignature
{
    Algorithm = "RS256",
    PublicKey = publicKey,
    Data = Convert.ToBase64String(signature)
};

// Verify signature
var verifier = new DefaultPluginSignatureVerifier();
var isValid = await verifier.VerifySignatureAsync(pluginContent, pluginSignature);
```

### Permission System

Plugins operate under a granular permission system with four main categories:

#### FileSystem Permissions

```csharp
var filePermissions = new FileSystemPermissions
{
    CanRead = true,
    CanWrite = false,
    CanDelete = false,
    AllowedPaths = new List<string> { "/tmp", "/app/data" }
};
```

#### Network Permissions

```csharp
var networkPermissions = new NetworkPermissions
{
    CanHttpClient = true,
    CanListen = false,
    CanSocket = false,
    AllowedHosts = new List<string> { "api.example.com" },
    AllowedPorts = new List<int> { 80, 443 }
};
```

#### Process Permissions

```csharp
var processPermissions = new ProcessPermissions
{
    CanExecute = false,
    CanSpawn = false,
    AllowedExecutables = new List<string>()
};
```

#### System Permissions

```csharp
var systemPermissions = new SystemPermissions
{
    CanAccessEnvironment = false,
    CanAccessRegistry = false
};
```

### Plugin Manifest Security Section

```json
{
  "id": "secure-plugin",
  "version": "1.0.0",
  "security": {
    "requireSignature": true,
    "signature": {
      "algorithm": "RS256",
      "publicKey": "MIIBIjANBgkqhkiG9w0...",
      "data": "base64-signature-data"
    },
    "permissions": {
      "fileSystem": {
        "canRead": true,
        "canWrite": false,
        "allowedPaths": ["/tmp"]
      },
      "network": {
        "canHttpClient": true,
        "canListen": false,
        "allowedHosts": ["api.example.com"]
      }
    }
  }
}
```

## Plugin Registry

### FilePluginRegistry

The plugin registry provides centralized metadata storage and discovery capabilities.

```csharp
// Initialize registry
var registry = new FilePluginRegistry("registry.json");

// Register a plugin
var manifest = new PluginManifest
{
    Id = "my-plugin",
    Version = "1.0.0",
    Name = "My Plugin",
    Author = "Developer"
};
await registry.RegisterPluginAsync(manifest);

// Find plugins
var searchResults = await registry.FindPluginsAsync(new PluginSearchCriteria
{
    Author = "Developer",
    RequiredCapabilities = new List<string> { "database" },
    SupportedProfiles = new List<string> { "web" },
    MinVersion = "1.0.0"
});

// Get statistics
var stats = await registry.GetStatisticsAsync();
Console.WriteLine($"Total plugins: {stats.TotalPlugins}");
Console.WriteLine($"Unique plugins: {stats.UniquePlugins}");
```

### Search Capabilities

The registry supports advanced search and filtering:

```csharp
var criteria = new PluginSearchCriteria
{
    Author = "AuthorName",                    // Filter by author
    RequiredCapabilities = new List<string>   // Required capabilities
    { 
        "database", "caching" 
    },
    SupportedProfiles = new List<string>      // Supported execution profiles
    { 
        "web", "console" 
    },
    MinVersion = "1.0.0",                     // Minimum version
    MaxVersion = "2.0.0"                      // Maximum version
};

var results = await registry.FindPluginsAsync(criteria);
```

## Hot Updates

### PluginUpdateManager

The update manager handles plugin updates with rollback capabilities.

```csharp
var updateManager = new PluginUpdateManager(registry, loader, signatureVerifier);

// Subscribe to update events
updateManager.PluginUpdateAvailable += (sender, args) =>
{
    Console.WriteLine($"Update available for {args.PluginId}: {args.CurrentVersion} -> {args.NewVersion}");
};

updateManager.PluginUpdateCompleted += (sender, args) =>
{
    Console.WriteLine($"Updated {args.PluginId} to {args.NewVersion}");
};

updateManager.PluginUpdateFailed += (sender, args) =>
{
    Console.WriteLine($"Update failed for {args.PluginId}: {args.Error}");
};

// Check for updates
var hasUpdates = await updateManager.CheckForUpdatesAsync("my-plugin");

// Create rollback point before update
await updateManager.CreateRollbackPointAsync("my-plugin");

// Perform hot update (implementation-specific)
// ... update plugin files ...

// If something goes wrong, rollback
await updateManager.RollbackAsync("my-plugin");
```

## Dependency Management

### Enhanced Dependency Resolution

The dependency resolver supports semantic versioning and conflict detection:

```csharp
var resolver = new PluginDependencyResolver();

// Define plugins with dependencies
var pluginA = new PluginManifest
{
    Id = "plugin-a",
    Version = "1.0.0",
    Dependencies = new Dictionary<string, string>
    {
        { "plugin-b", "^1.0.0" },
        { "plugin-c", "~2.1.0" }
    }
};

var pluginB = new PluginManifest
{
    Id = "plugin-b",
    Version = "1.2.0",
    Dependencies = new Dictionary<string, string>
    {
        { "plugin-d", "^1.0.0" }
    }
};

var plugins = new[] { pluginA, pluginB, pluginC, pluginD };

// Resolve load order
var loadOrder = resolver.ResolveLoadOrder(plugins);

// Validate dependencies
var isValid = resolver.ValidateDependencies(plugins);

// Find best version for a dependency
var bestVersion = resolver.FindBestVersion(availablePlugins, "plugin-b", "^1.0.0");
```

## Integration Examples

### Complete Plugin Lifecycle

```csharp
public class PluginService
{
    private readonly IPluginRegistry _registry;
    private readonly IPluginUpdateManager _updateManager;
    private readonly IPluginPermissionEnforcer _permissionEnforcer;
    private readonly IPluginSignatureVerifier _signatureVerifier;
    private readonly PluginDependencyResolver _dependencyResolver;
    private readonly PluginLoader _loader;

    public async Task<bool> LoadPluginAsync(string pluginPath)
    {
        // 1. Load plugin manifest
        var manifest = await LoadManifestAsync(pluginPath);
        
        // 2. Verify security
        if (manifest.Security?.RequireSignature == true)
        {
            var pluginContent = await File.ReadAllBytesAsync(
                Path.Combine(pluginPath, manifest.EntryPoint));
            var isValid = await _signatureVerifier.VerifySignatureAsync(
                pluginContent, manifest.Security.Signature);
            
            if (!isValid)
            {
                throw new SecurityException("Plugin signature verification failed");
            }
        }

        // 3. Register permissions
        if (manifest.Security?.Permissions != null)
        {
            _permissionEnforcer.RegisterPermissions(manifest.Id, manifest.Security.Permissions);
        }

        // 4. Check dependencies
        var dependencies = await ResolveDependenciesAsync(manifest);
        if (!_dependencyResolver.ValidateDependencies(dependencies))
        {
            throw new InvalidOperationException("Dependency validation failed");
        }

        // 5. Register in registry
        await _registry.RegisterPluginAsync(manifest);

        // 6. Load plugin
        var plugin = await _loader.LoadPluginAsync(pluginPath);

        // 7. Check for updates
        await _updateManager.CheckForUpdatesAsync(manifest.Id);

        return true;
    }
}
```

### HostBootstrap Integration

```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        var hostVersion = SemanticVersion.Parse("2.0.0");
        var bootstrap = new HostBootstrap(hostVersion);
        
        var services = new ServiceCollection();
        services.AddLogging();
        
        // Register all advanced plugin services
        bootstrap.RegisterHostServices(services);
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Start the host with security verification
        await bootstrap.StartAsync(serviceProvider, "./plugins");
    }
}
```

## Best Practices

### Plugin Development

1. **Always sign plugins** in production environments
2. **Request minimal permissions** required for functionality
3. **Use semantic versioning** for proper dependency management
4. **Test dependency compatibility** across version ranges
5. **Handle permission denials** gracefully

### Host Integration

1. **Verify signatures** before loading plugins
2. **Implement permission enforcement** at plugin boundaries
3. **Monitor plugin updates** and test before applying
4. **Create rollback points** before major updates
5. **Use plugin registry** for discovery and management

### Security Considerations

1. **Never trust unsigned plugins** in production
2. **Regularly audit plugin permissions**
3. **Monitor network and file system access**
4. **Implement logging** for security events
5. **Keep plugin runtime sandboxed**

## Troubleshooting

### Common Issues

#### Signature Verification Failures
- Ensure public key matches the signing key
- Verify plugin content hasn't been modified
- Check signature algorithm compatibility

#### Dependency Resolution Errors
- Verify all dependencies are available
- Check version compatibility ranges
- Look for circular dependencies

#### Permission Denied Errors
- Review requested vs. granted permissions
- Check path and resource access lists
- Verify permission enforcement configuration

#### Update Failures
- Ensure rollback points are created
- Check plugin compatibility with new versions
- Verify update source integrity

For more detailed troubleshooting, enable debug logging and examine the plugin lifecycle events.
