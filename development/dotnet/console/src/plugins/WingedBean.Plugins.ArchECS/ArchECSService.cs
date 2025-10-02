using System.Collections.Generic;
using WingedBean.Contracts.Core;
using WingedBean.Contracts.ECS;

namespace WingedBean.Plugins.ArchECS;

/// <summary>
/// Arch-based implementation of <see cref="IECSService"/>.
/// Provides high-performance Entity Component System functionality using Arch ECS library.
/// </summary>
[Plugin(
    Name = "Arch.ECS",
    Provides = new[] { typeof(IECSService) },
    Priority = 100
)]
public class ArchECSService : IECSService
{
    private readonly Dictionary<int, ArchWorld> _worlds = new();
    private int _nextWorldId = 0;

    public IWorld CreateWorld()
    {
        var world = new ArchWorld();
        var worldId = _nextWorldId++;
        _worlds[worldId] = world;
        return world;
    }

    public void DestroyWorld(IWorld world)
    {
        if (world is not ArchWorld archWorld)
        {
            throw new ArgumentException("World is not an ArchWorld instance", nameof(world));
        }

        var worldId = archWorld.GetArchWorld().Id;
        if (_worlds.ContainsKey(worldId))
        {
            _worlds.Remove(worldId);
        }

        // Arch World cleanup happens when it goes out of scope
        // The world will be disposed by the garbage collector
    }

    public IWorld? GetWorld(int worldId)
    {
        return _worlds.TryGetValue(worldId, out var world) ? world : null;
    }
}
