using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Arch.Core;
using Plate.CrossMilo.Contracts.ECS;
using Plate.CrossMilo.Contracts.ECS.Services;

namespace WingedBean.Plugins.ArchECS;

/// <summary>
/// Arch-based implementation of <see cref="IEntity"/>.
/// Wraps Arch.Core.Entity to provide platform-agnostic entity operations.
/// </summary>
internal class ArchEntity : IEntity
{
    private readonly Entity _entity;
    private readonly World _world;

    // Helper struct to construct Entity instances via Unsafe
    [StructLayout(LayoutKind.Sequential)]
    private struct EntityData
    {
        public int Id;
        public int WorldId;
        public int Version;
    }

    public ArchEntity(EntityHandle handle, World world)
    {
        _entity = ToArchEntity(handle);
        _world = world;
    }

    public int Id => _entity.Id;

    public bool IsAlive => _world.IsAlive(_entity);

    public void AddComponent<T>(T component) where T : struct
    {
        _world.Add(_entity, component);
    }

    public ref T GetComponent<T>() where T : struct
    {
        return ref _world.Get<T>(_entity);
    }

    public bool HasComponent<T>() where T : struct
    {
        return _world.Has<T>(_entity);
    }

    public void RemoveComponent<T>() where T : struct
    {
        _world.Remove<T>(_entity);
    }

    public void SetComponent<T>(T component) where T : struct
    {
        if (_world.Has<T>(_entity))
        {
            _world.Set(_entity, component);
        }
        else
        {
            _world.Add(_entity, component);
        }
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
}
