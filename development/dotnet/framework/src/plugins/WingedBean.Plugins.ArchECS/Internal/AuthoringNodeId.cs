using System;

namespace WingedBean.Plugins.ArchECS.Internal;

/// <summary>
/// Stable identifier for authoring graph nodes used to resolve runtime entities.
/// Internal to ArchECS plugin - not part of public API.
/// </summary>
public readonly record struct AuthoringNodeId(Guid Value)
{
    public static AuthoringNodeId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
