using WingedBean.Contracts.ECS;

namespace WingedBean.Plugins.ArchECS;

/// <summary>
/// Arch-based implementation of <see cref="IEntity"/>.
/// Wraps an EntityHandle and provides component operations through the world.
/// </summary>
internal class ArchEntity : IEntity
{
    private readonly EntityHandle _handle;
    private readonly IWorld _world;

    public ArchEntity(EntityHandle handle, IWorld world)
    {
        _handle = handle;
        _world = world;
    }

    public int Id => _handle.Id;

    public bool IsAlive => _world.IsAlive(_handle);

    public void AddComponent<T>(T component) where T : struct
    {
        _world.AttachComponent(_handle, component);
    }

    public ref T GetComponent<T>() where T : struct
    {
        return ref _world.GetComponent<T>(_handle);
    }

    public bool HasComponent<T>() where T : struct
    {
        return _world.HasComponent<T>(_handle);
    }

    public void RemoveComponent<T>() where T : struct
    {
        _world.DetachComponent<T>(_handle);
    }

    public void SetComponent<T>(T component) where T : struct
    {
        if (HasComponent<T>())
        {
            ref var existing = ref GetComponent<T>();
            existing = component;
        }
        else
        {
            AddComponent(component);
        }
    }
}
