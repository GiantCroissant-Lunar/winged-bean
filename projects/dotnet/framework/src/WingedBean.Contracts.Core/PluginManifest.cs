using System.Collections.Generic;

namespace WingedBean.Contracts.Core;

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
