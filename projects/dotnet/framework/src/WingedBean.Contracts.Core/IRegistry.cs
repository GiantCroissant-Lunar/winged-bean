using System.Collections.Generic;

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
