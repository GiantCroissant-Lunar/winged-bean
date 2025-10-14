using System;

namespace WingedBean.Plugins.ArchECS.Internal;

/// <summary>
/// Lightweight identifier for an ECS world tracked by the ECS service.
/// Internal to ArchECS plugin - not part of public API.
/// </summary>
public readonly record struct WorldHandle(int Id, WorldKind Kind)
{
    public bool IsValid => Id >= 0;

    public static WorldHandle Invalid => new(-1, WorldKind.Runtime);

    public override string ToString() => IsValid ? $"{Kind}:{Id}" : "Invalid";
}
