using System;

namespace WingedBean.Contracts.ECS;

/// <summary>
/// Lightweight identifier for an ECS world tracked by <see cref="IECSService"/>.
/// </summary>
public readonly record struct WorldHandle(int Id, WorldKind Kind)
{
    public bool IsValid => Id >= 0;

    public static WorldHandle Invalid => new(-1, WorldKind.Runtime);

    public override string ToString() => IsValid ? $"{Kind}:{Id}" : "Invalid";
}
