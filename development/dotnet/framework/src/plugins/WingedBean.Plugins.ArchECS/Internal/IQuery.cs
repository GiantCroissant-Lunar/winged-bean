using System;
using System.Collections.Generic;

namespace WingedBean.Plugins.ArchECS.Internal;

/// <summary>
/// Represents a query for entities with specific components.
/// Provides efficient iteration over matching entities.
/// Internal to ArchECS plugin - not part of public API.
/// </summary>
public interface IQuery
{
    /// <summary>
    /// Execute an action for each entity matching the query.
    /// </summary>
    /// <param name="action">Action to execute for each entity</param>
    void ForEach(Action<IEntity> action);

    /// <summary>
    /// Get all entities matching this query.
    /// </summary>
    /// <returns>Enumerable collection of matching entities</returns>
    IEnumerable<IEntity> GetEntities();

    /// <summary>
    /// Get the count of entities matching this query.
    /// </summary>
    int Count { get; }
}
