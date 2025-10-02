using System;

namespace WingedBean.Contracts.ECS;

/// <summary>
/// Opaque handle to an entity.
/// Internal representation varies by ECS implementation.
/// </summary>
public readonly struct EntityHandle : IEquatable<EntityHandle>
{
    internal readonly int Id;
    internal readonly int WorldId;

    internal EntityHandle(int id, int worldId)
    {
        Id = id;
        WorldId = worldId;
    }

    public bool Equals(EntityHandle other) =>
        Id == other.Id && WorldId == other.WorldId;

    public override bool Equals(object? obj) =>
        obj is EntityHandle handle && Equals(handle);

    public override int GetHashCode() =>
        HashCode.Combine(Id, WorldId);

    public static bool operator ==(EntityHandle left, EntityHandle right) =>
        left.Equals(right);

    public static bool operator !=(EntityHandle left, EntityHandle right) =>
        !left.Equals(right);
}
