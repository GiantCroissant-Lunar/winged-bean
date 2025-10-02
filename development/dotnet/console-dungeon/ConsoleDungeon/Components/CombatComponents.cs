using WingedBean.Contracts.ECS;

namespace ConsoleDungeon.Components;

/// <summary>
/// Combat state component tracking targeting and cooldown.
/// </summary>
public struct CombatState
{
    public EntityHandle? Target;
    public float Cooldown;
}
