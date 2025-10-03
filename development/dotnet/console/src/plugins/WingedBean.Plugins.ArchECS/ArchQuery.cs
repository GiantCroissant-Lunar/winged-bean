using System;
using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using WingedBean.Contracts.ECS;

namespace WingedBean.Plugins.ArchECS;

/// <summary>
/// Arch-based implementation of <see cref="IQuery"/>.
/// Wraps Arch.Core.QueryDescription to provide platform-agnostic query operations.
/// </summary>
internal class ArchQuery : IQuery
{
    private readonly World _world;
    private readonly QueryDescription _queryDescription;
    private readonly ArchWorld _archWorld;

    public ArchQuery(World world, QueryDescription queryDescription, IWorld worldInterface)
    {
        _world = world;
        _queryDescription = queryDescription;
        _archWorld = (ArchWorld)worldInterface;
    }

    public void ForEach(Action<IEntity> action)
    {
        var query = _world.Query(in _queryDescription);
        
        foreach (var chunk in query)
        {
            var entities = chunk.Entities;
            for (int i = 0; i < chunk.Count; i++)
            {
                var handle = new EntityHandle(entities[i].Id, entities[i].WorldId);
                var entity = new ArchEntity(handle, _world);
                action(entity);
            }
        }
    }

    public IEnumerable<IEntity> GetEntities()
    {
        var query = _world.Query(in _queryDescription);
        var results = new List<IEntity>();

        foreach (var chunk in query)
        {
            var entities = chunk.Entities;
            for (int i = 0; i < chunk.Count; i++)
            {
                var handle = new EntityHandle(entities[i].Id, entities[i].WorldId);
                var entity = new ArchEntity(handle, _world);
                results.Add(entity);
            }
        }

        return results;
    }

    public int Count
    {
        get
        {
            var query = _world.Query(in _queryDescription);
            int count = 0;

            foreach (var chunk in query)
            {
                count += chunk.Count;
            }

            return count;
        }
    }
}
