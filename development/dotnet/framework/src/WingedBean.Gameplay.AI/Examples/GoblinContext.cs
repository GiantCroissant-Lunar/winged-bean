using Plate.Capability.Core.Contexts;

namespace WingedBean.Gameplay.AI.Examples;

/// <summary>
/// Example context for a Goblin NPC.
/// Contains all state needed for capability evaluation and action execution.
/// </summary>
public sealed record GoblinContext : BehaviorContext
{
    // === Health & Combat ===
    public required int Health { get; init; }
    public required int MaxHealth { get; init; }
    public required int AttackDamage { get; init; }

    // === Position & Movement ===
    public required (int X, int Y) Position { get; init; }
    public required (int X, int Y)? TargetPosition { get; init; }

    /// <summary>
    /// Distance to target (calculated property for rules)
    /// </summary>
    public float Distance => TargetPosition.HasValue
        ? MathF.Sqrt(
            MathF.Pow(TargetPosition.Value.X - Position.X, 2) +
            MathF.Pow(TargetPosition.Value.Y - Position.Y, 2))
        : float.MaxValue;

    // === Inventory & Items ===
    public required GoblinInventory Inventory { get; init; }

    // === AI State ===
    public required bool IsInCombat { get; init; }
    public required bool HasPatrolRoute { get; init; }
    public required float Aggression { get; init; } // 0.0 to 1.0

    /// <summary>
    /// Health percentage (for rules like "flee if health < 30%")
    /// </summary>
    public float HealthPercent => MaxHealth > 0 ? (float)Health / MaxHealth : 0f;
}

/// <summary>
/// Goblin inventory for capability checks
/// </summary>
public sealed record GoblinInventory
{
    public required int Potions { get; init; }
    public required int Gold { get; init; }
    public required bool HasWeapon { get; init; }
}
