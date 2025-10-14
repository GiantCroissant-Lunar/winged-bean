using System;
using System.Collections.Generic;

namespace WingedBean.Plugins.Diagnostics;

/// <summary>
/// Interface for the diagnostics service (Tier 3).
/// Provides diagnostics capabilities and manages backend providers (Tier 4).
/// </summary>
public interface IDiagnosticsService
{
    /// <summary>
    /// Gets whether diagnostics is currently enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Enables or disables diagnostics.
    /// </summary>
    void SetEnabled(bool enabled);

    /// <summary>
    /// Registers a diagnostics backend provider (Tier 4).
    /// </summary>
    /// <param name="name">Unique name for the backend (e.g., "sentry", "opentelemetry")</param>
    /// <param name="backend">The backend implementation</param>
    void RegisterBackend(string name, IDiagnosticsBackend backend);

    /// <summary>
    /// Unregisters a diagnostics backend provider.
    /// </summary>
    /// <param name="name">Name of the backend to unregister</param>
    /// <returns>True if the backend was found and removed</returns>
    bool UnregisterBackend(string name);

    /// <summary>
    /// Gets all registered backend names.
    /// </summary>
    IEnumerable<string> GetRegisteredBackends();

    /// <summary>
    /// Gets a specific backend by name.
    /// </summary>
    /// <param name="name">Name of the backend</param>
    /// <returns>The backend, or null if not found</returns>
    IDiagnosticsBackend? GetBackend(string name);
}
