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
internal class ArchQuery(World world, QueryDescription queryDescription, IWorld worldInterface)
    : IQuery
{
    private readonly ArchWorld _archWorld = (ArchWorld)worldInterface;

    public void ForEach(Action<IEntity> action)
    {
        var query = world.Query(in queryDescription);

        foreach (var chunk in query)
        {
            var entities = chunk.Entities;
            for (var i = 0; i < chunk.Count; i++)
            {
                var handle = new EntityHandle(entities[i].Id, entities[i].WorldId);
                var entity = new ArchEntity(handle, world);
                action(entity);
            }
        }
    }

    public IEnumerable<IEntity> GetEntities()
    {
        var query = world.Query(in queryDescription);
        var results = new List<IEntity>();

        foreach (var chunk in query)
        {
            var entities = chunk.Entities;
            for (var i = 0; i < chunk.Count; i++)
            {
                var handle = new EntityHandle(entities[i].Id, entities[i].WorldId);
                var entity = new ArchEntity(handle, world);
                results.Add(entity);
            }
        }

        return results;
    }

    public int Count
    {
        get
        {
            var query = world.Query(in queryDescription);
            var count = 0;

            foreach (var chunk in query)
            {
                count += chunk.Count;
            }

            return count;
        }
    }
}
