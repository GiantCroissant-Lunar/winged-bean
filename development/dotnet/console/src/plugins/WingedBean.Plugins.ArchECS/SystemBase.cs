using System.Collections.Generic;
using Plate.CrossMilo.Contracts.ECS;
using Plate.CrossMilo.Contracts.ECS.Services;

namespace WingedBean.Plugins.ArchECS;

/// <summary>
/// Base class for all game systems in the Arch ECS implementation.
/// Provides common functionality including world access and query caching.
/// </summary>
public abstract class SystemBase : IECSSystem
{
    private readonly Dictionary<string, IQuery> _cachedQueries = new();

    /// <summary>
    /// Gets the ECS world this system operates on.
    /// </summary>
    protected IWorld World { get; private set; } = null!;

    /// <summary>
    /// Initializes the system with the specified world.
    /// This method must be called before Execute.
    /// </summary>
    /// <param name="world">The ECS world this system will operate on.</param>
    public void Initialize(IWorld world)
    {
        World = world ?? throw new System.ArgumentNullException(nameof(world));
    }

    /// <summary>
    /// Execute system logic for this frame.
    /// </summary>
    /// <param name="ecs">The ECS service providing access to entities and components.</param>
    /// <param name="world">The world this system should operate on.</param>
    /// <param name="deltaTime">Time elapsed since the last frame in seconds.</param>
    public void Execute(IService ecs, IWorld world, float deltaTime)
    {
        // Update world reference if provided
        if (world != null)
        {
            World = world;
        }

        if (World == null)
        {
            throw new System.InvalidOperationException(
                "System not initialized. Call Initialize(world) before Execute or provide world parameter.");
        }

        OnUpdate(deltaTime);
    }

    /// <summary>
    /// Called each frame to update system logic.
    /// Override this method to implement system-specific behavior.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last frame in seconds.</param>
    protected abstract void OnUpdate(float deltaTime);

    /// <summary>
    /// Get a cached query for entities with the specified component.
    /// Queries are cached to avoid recreation overhead.
    /// </summary>
    /// <typeparam name="T1">First component type to query for.</typeparam>
    /// <returns>Query object for iterating over matching entities.</returns>
    protected IQuery GetQuery<T1>() where T1 : struct
    {
        var key = typeof(T1).FullName!;
        if (!_cachedQueries.TryGetValue(key, out var query))
        {
            if (World is ArchWorld archWorld)
            {
                query = archWorld.Query<T1>();
                _cachedQueries[key] = query;
            }
            else
            {
                throw new System.InvalidOperationException("World must be an ArchWorld instance");
            }
        }
        return query;
    }

    /// <summary>
    /// Get a cached query for entities with the specified components.
    /// Queries are cached to avoid recreation overhead.
    /// </summary>
    /// <typeparam name="T1">First component type to query for.</typeparam>
    /// <typeparam name="T2">Second component type to query for.</typeparam>
    /// <returns>Query object for iterating over matching entities.</returns>
    protected IQuery GetQuery<T1, T2>()
        where T1 : struct
        where T2 : struct
    {
        var key = $"{typeof(T1).FullName}+{typeof(T2).FullName}";
        if (!_cachedQueries.TryGetValue(key, out var query))
        {
            if (World is ArchWorld archWorld)
            {
                query = archWorld.Query<T1, T2>();
                _cachedQueries[key] = query;
            }
            else
            {
                throw new System.InvalidOperationException("World must be an ArchWorld instance");
            }
        }
        return query;
    }

    /// <summary>
    /// Get a cached query for entities with the specified components.
    /// Queries are cached to avoid recreation overhead.
    /// </summary>
    /// <typeparam name="T1">First component type to query for.</typeparam>
    /// <typeparam name="T2">Second component type to query for.</typeparam>
    /// <typeparam name="T3">Third component type to query for.</typeparam>
    /// <returns>Query object for iterating over matching entities.</returns>
    protected IQuery GetQuery<T1, T2, T3>()
        where T1 : struct
        where T2 : struct
        where T3 : struct
    {
        var key = $"{typeof(T1).FullName}+{typeof(T2).FullName}+{typeof(T3).FullName}";
        if (!_cachedQueries.TryGetValue(key, out var query))
        {
            if (World is ArchWorld archWorld)
            {
                query = archWorld.Query<T1, T2, T3>();
                _cachedQueries[key] = query;
            }
            else
            {
                throw new System.InvalidOperationException("World must be an ArchWorld instance");
            }
        }
        return query;
    }
}
