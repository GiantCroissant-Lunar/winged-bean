using WingedBean.Gameplay.AI.Behaviors;

namespace WingedBean.Gameplay.AI.Examples;

/// <summary>
/// Player character context for capability evaluation.
/// Used for bot-controlled player testing (automated gameplay testing, recording generation, CI/CD).
/// </summary>
/// <remarks>
/// Use Case: Automated Testing
/// - Bot takes control of player character
/// - Evaluates player capabilities (same as NPCs)
/// - Executes player actions (move, attack, use items, etc.)
/// - Generates test recordings for CI/CD validation
/// 
/// Pattern: Same as NPCs, just different capabilities/stats
/// </remarks>
public sealed record PlayerContext : BehaviorContext
{
    // === Character Stats ===
    public required int Health { get; init; }
    public required int MaxHealth { get; init; }
    public required int Mana { get; init; }
    public required int MaxMana { get; init; }
    public required int Stamina { get; init; }
    public required int MaxStamina { get; init; }
    public required int Level { get; init; }
    public required int Experience { get; init; }

    // === Position & Movement ===
    public required (int X, int Y) Position { get; init; }
    public required (int X, int Y)? TargetPosition { get; init; }
    public required (int X, int Y)? GoalPosition { get; init; } // For pathfinding/quests

    /// <summary>
    /// Distance to target (for combat)
    /// </summary>
    public float DistanceToTarget => TargetPosition.HasValue
        ? MathF.Sqrt(
            MathF.Pow(TargetPosition.Value.X - Position.X, 2) +
            MathF.Pow(TargetPosition.Value.Y - Position.Y, 2))
        : float.MaxValue;

    /// <summary>
    /// Distance to goal (for navigation)
    /// </summary>
    public float DistanceToGoal => GoalPosition.HasValue
        ? MathF.Sqrt(
            MathF.Pow(GoalPosition.Value.X - Position.X, 2) +
            MathF.Pow(GoalPosition.Value.Y - Position.Y, 2))
        : float.MaxValue;

    // === Inventory & Equipment ===
    public required PlayerInventory Inventory { get; init; }
    public required PlayerEquipment Equipment { get; init; }

    // === Combat State ===
    public required bool IsInCombat { get; init; }
    public required bool HasTarget { get; init; }
    public required string? TargetType { get; init; } // "Enemy", "NPC", "Object"
    public required int? TargetHealth { get; init; }
    public required int? TargetMaxHealth { get; init; }

    // === World State ===
    public required bool IsInDungeon { get; init; }
    public required bool IsInTown { get; init; }
    public required bool IsSafe { get; init; } // No enemies nearby

    // === Quest/Objective State ===
    public required bool HasActiveQuest { get; init; }
    public required string? QuestObjective { get; init; } // "KillEnemies", "ReachLocation", etc.

    // === Calculated Properties (for rules) ===

    /// <summary>
    /// Health percentage (0.0 to 1.0)
    /// </summary>
    public float HealthPercent => MaxHealth > 0 ? (float)Health / MaxHealth : 0f;

    /// <summary>
    /// Mana percentage (0.0 to 1.0)
    /// </summary>
    public float ManaPercent => MaxMana > 0 ? (float)Mana / MaxMana : 0f;

    /// <summary>
    /// Stamina percentage (0.0 to 1.0)
    /// </summary>
    public float StaminaPercent => MaxStamina > 0 ? (float)Stamina / MaxStamina : 0f;

    /// <summary>
    /// Is player critically injured (< 30% health)
    /// </summary>
    public bool IsCriticallyInjured => HealthPercent < 0.3f;

    /// <summary>
    /// Target health percentage (if target exists)
    /// </summary>
    public float? TargetHealthPercent => TargetMaxHealth.HasValue && TargetMaxHealth.Value > 0
        ? (float)(TargetHealth ?? 0) / TargetMaxHealth.Value
        : null;
}

/// <summary>
/// Player inventory for capability checks
/// </summary>
public sealed record PlayerInventory
{
    public required int HealthPotions { get; init; }
    public required int ManaPotions { get; init; }
    public required int Gold { get; init; }
    public required int Keys { get; init; }
    public required bool HasMap { get; init; }
    public required int FoodItems { get; init; }

    /// <summary>
    /// Can use health potion
    /// </summary>
    public bool HasHealthPotion => HealthPotions > 0;

    /// <summary>
    /// Can use mana potion
    /// </summary>
    public bool HasManaPotion => ManaPotions > 0;

    /// <summary>
    /// Has resources for basic actions
    /// </summary>
    public bool HasBasicResources => HealthPotions > 0 || FoodItems > 0;
}

/// <summary>
/// Player equipment for capability checks
/// </summary>
public sealed record PlayerEquipment
{
    public required bool HasWeapon { get; init; }
    public required bool HasArmor { get; init; }
    public required bool HasShield { get; init; }
    public required string? WeaponType { get; init; } // "Sword", "Bow", "Staff"
    public required int WeaponDamage { get; init; }
    public required int ArmorDefense { get; init; }

    /// <summary>
    /// Can perform melee combat
    /// </summary>
    public bool CanMelee => HasWeapon && (WeaponType == "Sword" || WeaponType == "Axe");

    /// <summary>
    /// Can perform ranged combat
    /// </summary>
    public bool CanRanged => HasWeapon && WeaponType == "Bow";

    /// <summary>
    /// Can cast spells
    /// </summary>
    public bool CanCastSpells => HasWeapon && WeaponType == "Staff";
}
