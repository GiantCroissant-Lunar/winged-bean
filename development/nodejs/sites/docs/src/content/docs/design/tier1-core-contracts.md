---
title: Tier 1 Core Contracts Design
description: "Documentation for Tier 1 Core Contracts Design"
---

# Tier 1 Core Contracts Design

## Overview

This document defines the **foundational Tier 1 contracts** for the WingedBean service platform. These contracts are **pure C# interfaces** with no engine-specific types, designed to be implemented across multiple platforms (Console, Unity, Godot, Web).

## Core Service Groups

### 1. WingedBean.Contracts.Core

Foundation contracts for the service platform itself.

#### IRegistry

Central service registry for managing service implementations and selection strategies.

```csharp
namespace WingedBean.Contracts.Core;

/// <summary>
/// Registry for managing service implementations and selection strategies.
/// Foundation service - manually instantiated at bootstrap, not loaded as a plugin.
/// </summary>
public interface IRegistry
{
    /// <summary>
    /// Register a service implementation with optional priority.
    /// </summary>
    /// <typeparam name="TService">Service interface type</typeparam>
    /// <param name="implementation">Service implementation instance</param>
    /// <param name="priority">Priority for selection (higher = preferred), default 0</param>
    void Register<TService>(TService implementation, int priority = 0)
        where TService : class;

    /// <summary>
    /// Register a service implementation with metadata.
    /// </summary>
    void Register<TService>(TService implementation, ServiceMetadata metadata)
        where TService : class;

    /// <summary>
    /// Get a single service implementation based on selection mode.
    /// </summary>
    /// <typeparam name="TService">Service interface type</typeparam>
    /// <param name="mode">Selection strategy (One, HighestPriority, etc.)</param>
    /// <returns>Service implementation</returns>
    /// <exception cref="ServiceNotFoundException">No implementation found</exception>
    /// <exception cref="MultipleServicesException">Multiple found when One expected</exception>
    TService Get<TService>(SelectionMode mode = SelectionMode.HighestPriority)
        where TService : class;

    /// <summary>
    /// Get all registered implementations of a service.
    /// </summary>
    /// <typeparam name="TService">Service interface type</typeparam>
    /// <returns>All registered implementations</returns>
    IEnumerable<TService> GetAll<TService>()
        where TService : class;

    /// <summary>
    /// Check if a service is registered.
    /// </summary>
    bool IsRegistered<TService>()
        where TService : class;

    /// <summary>
    /// Unregister a specific service implementation.
    /// </summary>
    bool Unregister<TService>(TService implementation)
        where TService : class;

    /// <summary>
    /// Unregister all implementations of a service.
    /// </summary>
    void UnregisterAll<TService>()
        where TService : class;

    /// <summary>
    /// Get metadata for a registered service.
    /// </summary>
    ServiceMetadata? GetMetadata<TService>(TService implementation)
        where TService : class;
}

/// <summary>
/// Selection mode for retrieving services from registry.
/// </summary>
public enum SelectionMode
{
    /// <summary>
    /// Get the single registered implementation (error if multiple exist).
    /// </summary>
    One,

    /// <summary>
    /// Get the implementation with the highest priority value.
    /// </summary>
    HighestPriority,

    /// <summary>
    /// Get all registered implementations (for fan-out scenarios).
    /// </summary>
    All
}

/// <summary>
/// Metadata associated with a registered service.
/// </summary>
public record ServiceMetadata
{
    public string? Name { get; init; }
    public int Priority { get; init; }
    public string? Version { get; init; }
    public string? Platform { get; init; }
    public IDictionary<string, object>? Properties { get; init; }
}

/// <summary>
/// Exception thrown when a requested service is not found in the registry.
/// </summary>
public class ServiceNotFoundException : Exception
{
    public Type ServiceType { get; }

    public ServiceNotFoundException(Type serviceType)
        : base($"Service {serviceType.Name} not found in registry")
    {
        ServiceType = serviceType;
    }
}

/// <summary>
/// Exception thrown when multiple services found but SelectionMode.One was specified.
/// </summary>
public class MultipleServicesException : Exception
{
    public Type ServiceType { get; }
    public int Count { get; }

    public MultipleServicesException(Type serviceType, int count)
        : base($"Multiple services ({count}) found for {serviceType.Name}, but SelectionMode.One was specified")
    {
        ServiceType = serviceType;
        Count = count;
    }
}
```

#### IPluginLoader

Orchestrates loading, unloading, and lifecycle management of plugins.

```csharp
namespace WingedBean.Contracts.Core;

/// <summary>
/// Orchestrates plugin loading, unloading, and lifecycle management.
/// Foundation service - manually instantiated at bootstrap, delegates to platform-specific providers.
/// </summary>
public interface IPluginLoader
{
    /// <summary>
    /// Load a plugin from a specified path.
    /// </summary>
    /// <param name="pluginPath">Path to plugin assembly</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Loaded plugin handle</returns>
    Task<ILoadedPlugin> LoadAsync(string pluginPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Load a plugin with explicit metadata.
    /// </summary>
    Task<ILoadedPlugin> LoadAsync(PluginManifest manifest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unload a previously loaded plugin.
    /// </summary>
    /// <param name="plugin">Plugin to unload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UnloadAsync(ILoadedPlugin plugin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reload a plugin (unload + load).
    /// </summary>
    Task<ILoadedPlugin> ReloadAsync(ILoadedPlugin plugin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all currently loaded plugins.
    /// </summary>
    IEnumerable<ILoadedPlugin> GetLoadedPlugins();

    /// <summary>
    /// Check if a plugin is loaded.
    /// </summary>
    bool IsLoaded(string pluginId);

    /// <summary>
    /// Get a loaded plugin by ID.
    /// </summary>
    ILoadedPlugin? GetPlugin(string pluginId);
}

/// <summary>
/// Represents a loaded plugin with lifecycle management.
/// </summary>
public interface ILoadedPlugin
{
    /// <summary>
    /// Unique plugin identifier.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Plugin version.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Plugin manifest metadata.
    /// </summary>
    PluginManifest Manifest { get; }

    /// <summary>
    /// Current state of the plugin.
    /// </summary>
    PluginState State { get; }

    /// <summary>
    /// Get a service provided by this plugin.
    /// </summary>
    TService? GetService<TService>() where TService : class;

    /// <summary>
    /// Get all services provided by this plugin.
    /// </summary>
    IEnumerable<object> GetServices();

    /// <summary>
    /// Activate the plugin (register services, initialize resources).
    /// </summary>
    Task ActivateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivate the plugin (cleanup, prepare for unload).
    /// </summary>
    Task DeactivateAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Plugin lifecycle states.
/// </summary>
public enum PluginState
{
    Discovered,
    Loading,
    Loaded,
    Activating,
    Activated,
    Deactivating,
    Deactivated,
    Unloading,
    Unloaded,
    Failed
}

/// <summary>
/// Plugin manifest describing metadata and dependencies.
/// </summary>
public record PluginManifest
{
    public required string Id { get; init; }
    public required string Version { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Author { get; init; }
    public string[]? ProvidesServices { get; init; }
    public PluginDependency[]? Dependencies { get; init; }
    public int Priority { get; init; }
    public LoadStrategy LoadStrategy { get; init; } = LoadStrategy.Lazy;
    public IDictionary<string, string>? EntryPoints { get; init; }
    public IDictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Plugin dependency specification.
/// </summary>
public record PluginDependency
{
    public required string PluginId { get; init; }
    public string? VersionRange { get; init; }
    public bool Optional { get; init; }
}

/// <summary>
/// Plugin loading strategy.
/// </summary>
public enum LoadStrategy
{
    /// <summary>
    /// Load during bootstrap (immediately).
    /// </summary>
    Eager,

    /// <summary>
    /// Load on first use (deferred).
    /// </summary>
    Lazy,

    /// <summary>
    /// Load on explicit request only.
    /// </summary>
    Explicit
}
```

#### IPlugin

Base interface for plugin implementations (optional marker interface).

```csharp
namespace WingedBean.Contracts.Core;

/// <summary>
/// Optional marker interface for plugins.
/// Plugins can implement this for lifecycle hooks, or just provide services directly.
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Plugin unique identifier.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Plugin version.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Called when the plugin is activated.
    /// </summary>
    Task OnActivateAsync(IRegistry registry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when the plugin is deactivated.
    /// </summary>
    Task OnDeactivateAsync(CancellationToken cancellationToken = default);
}
```

### 2. WingedBean.Contracts.Config

Configuration service contracts.

#### IConfigService

Configuration service interface (inspired by Microsoft.Extensions.Configuration).

```csharp
namespace WingedBean.Contracts.Config;

/// <summary>
/// Configuration service for accessing application settings.
/// Inspired by Microsoft.Extensions.Configuration but platform-agnostic.
/// </summary>
public interface IConfigService
{
    /// <summary>
    /// Get a configuration value by key.
    /// </summary>
    /// <param name="key">Configuration key (colon-separated for nested, e.g., "Plugins:Load")</param>
    /// <returns>Configuration value as string, or null if not found</returns>
    string? Get(string key);

    /// <summary>
    /// Get a strongly-typed configuration value.
    /// </summary>
    /// <typeparam name="T">Target type</typeparam>
    /// <param name="key">Configuration key</param>
    /// <returns>Parsed value, or default(T) if not found</returns>
    T? Get<T>(string key);

    /// <summary>
    /// Get a configuration section.
    /// </summary>
    /// <param name="key">Section key</param>
    /// <returns>Configuration section</returns>
    IConfigSection GetSection(string key);

    /// <summary>
    /// Set a configuration value (if implementation supports writes).
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="value">Value to set</param>
    void Set(string key, string value);

    /// <summary>
    /// Check if a key exists in configuration.
    /// </summary>
    bool Exists(string key);

    /// <summary>
    /// Reload configuration from source (if supported).
    /// </summary>
    Task ReloadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when configuration changes (if supported by implementation).
    /// </summary>
    event EventHandler<ConfigChangedEventArgs>? ConfigChanged;
}

/// <summary>
/// Represents a section of configuration.
/// </summary>
public interface IConfigSection
{
    /// <summary>
    /// Section key/path.
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Section value (if it's a leaf node).
    /// </summary>
    string? Value { get; }

    /// <summary>
    /// Get a child section.
    /// </summary>
    IConfigSection GetSection(string key);

    /// <summary>
    /// Get all child sections.
    /// </summary>
    IEnumerable<IConfigSection> GetChildren();

    /// <summary>
    /// Bind this section to an object instance.
    /// </summary>
    void Bind(object instance);

    /// <summary>
    /// Get this section as a strongly-typed object.
    /// </summary>
    T? Get<T>();
}

/// <summary>
/// Event args for configuration changes.
/// </summary>
public class ConfigChangedEventArgs : EventArgs
{
    public required string Key { get; init; }
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
}

/// <summary>
/// Proxy service for IConfigService (partial class, source gen fills in methods).
/// </summary>
[RealizeService(typeof(IConfigService))]
[SelectionStrategy(SelectionMode.HighestPriority)]
public partial class ProxyService : IConfigService
{
    private readonly IRegistry _registry;

    public ProxyService(IRegistry registry)
    {
        _registry = registry;
    }

    // Source gen will fill in:
    // - All interface methods (Get, Set, GetSection, etc.)
    // - Delegation to _registry.Get<IConfigService>(SelectionMode.HighestPriority)
    // - Event forwarding for ConfigChanged
}
```

### 3. WingedBean.Contracts.Audio

Audio service contracts (example domain service).

```csharp
namespace WingedBean.Contracts.Audio;

/// <summary>
/// Audio service for playing sounds and music.
/// Platform implementations: NAudio (Console), Unity AudioSource (Unity), etc.
/// </summary>
public interface IAudioService
{
    /// <summary>
    /// Play an audio clip by name/path.
    /// </summary>
    void Play(string clipId, AudioPlayOptions? options = null);

    /// <summary>
    /// Stop a playing audio clip.
    /// </summary>
    void Stop(string clipId);

    /// <summary>
    /// Stop all playing audio.
    /// </summary>
    void StopAll();

    /// <summary>
    /// Pause a playing audio clip.
    /// </summary>
    void Pause(string clipId);

    /// <summary>
    /// Resume a paused audio clip.
    /// </summary>
    void Resume(string clipId);

    /// <summary>
    /// Master volume (0.0 to 1.0).
    /// </summary>
    float Volume { get; set; }

    /// <summary>
    /// Check if an audio clip is currently playing.
    /// </summary>
    bool IsPlaying(string clipId);

    /// <summary>
    /// Load an audio clip (preload for faster playback).
    /// </summary>
    Task<bool> LoadAsync(string clipId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unload an audio clip (free memory).
    /// </summary>
    void Unload(string clipId);
}

/// <summary>
/// Options for playing audio clips.
/// </summary>
public record AudioPlayOptions
{
    public float Volume { get; init; } = 1.0f;
    public bool Loop { get; init; } = false;
    public float Pitch { get; init; } = 1.0f;
    public float FadeInDuration { get; init; } = 0f;
    public string? MixerGroup { get; init; }
}

/// <summary>
/// Proxy service for IAudioService.
/// </summary>
[RealizeService(typeof(IAudioService))]
[SelectionStrategy(SelectionMode.HighestPriority)]
public partial class ProxyService : IAudioService
{
    private readonly IRegistry _registry;

    public ProxyService(IRegistry registry)
    {
        _registry = registry;
    }

    // Source gen fills in all methods
}
```

### 4. WingedBean.Contracts.Resource

Resource loading service contracts (example domain service).

```csharp
namespace WingedBean.Contracts.Resource;

/// <summary>
/// Resource loading service for assets, data files, etc.
/// Platform implementations: File system (Console), Addressables (Unity), YooAsset (Unity alt), etc.
/// </summary>
public interface IResourceService
{
    /// <summary>
    /// Load a resource asynchronously.
    /// </summary>
    Task<TResource?> LoadAsync<TResource>(string resourceId, CancellationToken cancellationToken = default)
        where TResource : class;

    /// <summary>
    /// Load multiple resources asynchronously.
    /// </summary>
    Task<IEnumerable<TResource>> LoadAllAsync<TResource>(string pattern, CancellationToken cancellationToken = default)
        where TResource : class;

    /// <summary>
    /// Unload a resource (free memory).
    /// </summary>
    void Unload(string resourceId);

    /// <summary>
    /// Unload all resources of a specific type.
    /// </summary>
    void UnloadAll<TResource>() where TResource : class;

    /// <summary>
    /// Check if a resource is loaded.
    /// </summary>
    bool IsLoaded(string resourceId);

    /// <summary>
    /// Get resource metadata without loading the full resource.
    /// </summary>
    Task<ResourceMetadata?> GetMetadataAsync(string resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Preload resources (load into memory without instantiating).
    /// </summary>
    Task PreloadAsync(IEnumerable<string> resourceIds, CancellationToken cancellationToken = default);
}

/// <summary>
/// Resource metadata (size, type, etc.).
/// </summary>
public record ResourceMetadata
{
    public required string Id { get; init; }
    public string? Name { get; init; }
    public string? Type { get; init; }
    public long Size { get; init; }
    public string? Format { get; init; }
    public IDictionary<string, object>? Properties { get; init; }
}

/// <summary>
/// Proxy service for IResourceService.
/// </summary>
[RealizeService(typeof(IResourceService))]
[SelectionStrategy(SelectionMode.HighestPriority)]
public partial class ProxyService : IResourceService
{
    private readonly IRegistry _registry;

    public ProxyService(IRegistry registry)
    {
        _registry = registry;
    }

    // Source gen fills in all methods
}
```

## Source Code Generation Attributes

These attributes are used to drive source code generation for proxy services.

```csharp
namespace WingedBean.Contracts.Core;

/// <summary>
/// Marks a partial class as a proxy service that realizes a specific interface.
/// Source generator will implement all interface methods by delegating to the registry.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RealizeServiceAttribute : Attribute
{
    public Type ServiceType { get; }

    public RealizeServiceAttribute(Type serviceType)
    {
        ServiceType = serviceType;
    }
}

/// <summary>
/// Specifies the selection strategy for retrieving service implementations from the registry.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SelectionStrategyAttribute : Attribute
{
    public SelectionMode Mode { get; }

    public SelectionStrategyAttribute(SelectionMode mode)
    {
        Mode = mode;
    }
}

/// <summary>
/// Marks a class as a plugin with metadata.
/// Source generator can read this to generate registration code.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class PluginAttribute : Attribute
{
    public string? Name { get; set; }
    public Type[]? Provides { get; set; }
    public Type[]? Dependencies { get; set; }
    public int Priority { get; set; }
}
```

## Project Structure

These contracts will be organized into separate projects:

```
framework/src/
├── WingedBean.Contracts.Core/
│   ├── IRegistry.cs
│   ├── IPluginLoader.cs
│   ├── IPlugin.cs
│   ├── SelectionMode.cs
│   ├── ServiceMetadata.cs
│   ├── PluginManifest.cs
│   ├── Exceptions.cs
│   └── Attributes.cs
│
├── WingedBean.Contracts.Config/
│   ├── IConfigService.cs
│   ├── IConfigSection.cs
│   ├── ConfigChangedEventArgs.cs
│   └── ProxyService.cs (partial)
│
├── WingedBean.Contracts.Audio/
│   ├── IAudioService.cs
│   ├── AudioPlayOptions.cs
│   └── ProxyService.cs (partial)
│
└── WingedBean.Contracts.Resource/
    ├── IResourceService.cs
    ├── ResourceMetadata.cs
    └── ProxyService.cs (partial)
```

## Implementation Notes

### Tier 1 Purity

- **No engine-specific types**: All contracts are pure C# with no Unity/Godot/etc. types
- **No implementation logic**: Tier 1 is interfaces only (except partial proxy classes)
- **No external dependencies**: Minimal dependencies, only .NET BCL

### Proxy Pattern

- **Partial classes**: Proxy services are partial, source gen completes them
- **Registry injection**: All proxies take `IRegistry` in constructor
- **Transparent delegation**: Proxies delegate to actual implementations via registry
- **Selection strategies**: Each proxy specifies how to select from multiple implementations

### Bootstrap Considerations

- **Registry and PluginLoader**: Foundation services, manually instantiated
- **Config**: First plugin loaded (bootstrap plugin)
- **Other services**: Loaded via plugin system, registered with registry

## Next Steps

1. **Implement these contracts** in actual C# projects
2. **Create source generators** (`WingedBean.Contracts.SourceGen`)
3. **Implement Tier 2 Registry** (`WingedBean.Registry`)
4. **Create Tier 3 plugin implementations** for each service
5. **Test end-to-end** with console bootstrap

---

**Status**: Design Complete - Ready for Implementation
**Author**: Ray Wang (with Claude AI assistance)
**Date**: 2025-09-30
