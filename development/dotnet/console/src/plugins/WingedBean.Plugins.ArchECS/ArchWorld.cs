using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Arch.Core;
using Arch.Core.Extensions;
using WingedBean.Contracts.ECS;

namespace WingedBean.Plugins.ArchECS;

/// <summary>
/// Arch-based implementation of <see cref="IWorld"/>.
/// Wraps Arch.Core.World to provide platform-agnostic ECS operations.
/// </summary>
internal class ArchWorld : IWorld
{
    private readonly World _world;
    
    // Helper struct to construct Entity instances via Unsafe
    [StructLayout(LayoutKind.Sequential)]
    private struct EntityData
    {
        public int Id;
        public int WorldId;
        public int Version;
    }

    public ArchWorld()
    {
        _world = World.Create();
    }

    public EntityHandle CreateEntity()
    {
        var entity = _world.Create();
        return new EntityHandle(entity.Id, _world.Id);
    }

    public void DestroyEntity(EntityHandle entity)
    {
        var archEntity = ToArchEntity(entity);
        _world.Destroy(archEntity);
    }

    public void AttachComponent<T>(EntityHandle entity, T component) where T : struct
    {
        var archEntity = ToArchEntity(entity);
        _world.Add(archEntity, component);
    }

    public void DetachComponent<T>(EntityHandle entity) where T : struct
    {
        var archEntity = ToArchEntity(entity);
        _world.Remove<T>(archEntity);
    }

    public ref T GetComponent<T>(EntityHandle entity) where T : struct
    {
        var archEntity = ToArchEntity(entity);
        return ref _world.Get<T>(archEntity);
    }

    public bool HasComponent<T>(EntityHandle entity) where T : struct
    {
        var archEntity = ToArchEntity(entity);
        return _world.Has<T>(archEntity);
    }

    public bool IsAlive(EntityHandle entity)
    {
        var archEntity = ToArchEntity(entity);
        return _world.IsAlive(archEntity);
    }

    public IEnumerable<EntityHandle> CreateQuery<T1>() where T1 : struct
    {
        var query = _world.Query(in new QueryDescription().WithAll<T1>());
        var results = new List<EntityHandle>();

        foreach (var chunk in query)
        {
            var entities = chunk.Entities;
            for (int i = 0; i < chunk.Count; i++)
            {
                results.Add(new EntityHandle(entities[i].Id, entities[i].WorldId));
            }
        }

        return results;
    }

    public IEnumerable<EntityHandle> CreateQuery<T1, T2>()
        where T1 : struct
        where T2 : struct
    {
        var query = _world.Query(in new QueryDescription().WithAll<T1, T2>());
        var results = new List<EntityHandle>();

        foreach (var chunk in query)
        {
            var entities = chunk.Entities;
            for (int i = 0; i < chunk.Count; i++)
            {
                results.Add(new EntityHandle(entities[i].Id, entities[i].WorldId));
            }
        }

        return results;
    }

    public IEnumerable<EntityHandle> CreateQuery<T1, T2, T3>()
        where T1 : struct
        where T2 : struct
        where T3 : struct
    {
        var query = _world.Query(in new QueryDescription().WithAll<T1, T2, T3>());
        var results = new List<EntityHandle>();

        foreach (var chunk in query)
        {
            var entities = chunk.Entities;
            for (int i = 0; i < chunk.Count; i++)
            {
                results.Add(new EntityHandle(entities[i].Id, entities[i].WorldId));
            }
        }

        return results;
    }

    public int EntityCount => _world.Size;

    /// <summary>
    /// Create a query object for entities with specific components.
    /// </summary>
    /// <typeparam name="T1">First component type to query for.</typeparam>
    /// <returns>Query object for iterating over matching entities.</returns>
    public IQuery Query<T1>() where T1 : struct
    {
        var queryDescription = new QueryDescription().WithAll<T1>();
        return new ArchQuery(_world, queryDescription, this);
    }

    /// <summary>
    /// Create a query object for entities with specific components.
    /// </summary>
    /// <typeparam name="T1">First component type to query for.</typeparam>
    /// <typeparam name="T2">Second component type to query for.</typeparam>
    /// <returns>Query object for iterating over matching entities.</returns>
    public IQuery Query<T1, T2>()
        where T1 : struct
        where T2 : struct
    {
        var queryDescription = new QueryDescription().WithAll<T1, T2>();
        return new ArchQuery(_world, queryDescription, this);
    }

    /// <summary>
    /// Create a query object for entities with specific components.
    /// </summary>
    /// <typeparam name="T1">First component type to query for.</typeparam>
    /// <typeparam name="T2">Second component type to query for.</typeparam>
    /// <typeparam name="T3">Third component type to query for.</typeparam>
    /// <returns>Query object for iterating over matching entities.</returns>
    public IQuery Query<T1, T2, T3>()
        where T1 : struct
        where T2 : struct
        where T3 : struct
    {
        var queryDescription = new QueryDescription().WithAll<T1, T2, T3>();
        return new ArchQuery(_world, queryDescription, this);
    }

    private Entity ToArchEntity(EntityHandle handle)
    {
        // Create Entity using Unsafe since the constructor is internal
        // Note: We use version 1 as default since we don't track versions in EntityHandle
        // This is a known limitation of the abstraction layer
        var data = new EntityData 
        { 
            Id = handle.Id, 
            WorldId = handle.WorldId, 
            Version = 1 
        };
        return Unsafe.As<EntityData, Entity>(ref data);
    }

    internal World GetArchWorld() => _world;
}
