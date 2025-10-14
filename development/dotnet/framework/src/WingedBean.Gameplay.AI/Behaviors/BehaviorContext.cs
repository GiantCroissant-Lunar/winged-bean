namespace WingedBean.Gameplay.AI.Behaviors;

/// <summary>
/// Base context for AI behavior evaluation.
/// Contains entity state, environment, and other factors needed for capability checks.
/// </summary>
/// <remarks>
/// Design Pattern:
/// - Derive from this for specific entity types (PlayerContext, GoblinContext, etc.)
/// - Include all data needed by rules expressions
/// - Keep immutable where possible (use records)
/// - Pass to capability evaluator for rule execution
/// 
/// Example Usage:
/// var context = new GoblinContext
/// {
///     Health = 15, MaxHealth = 20,
///     Position = (5, 5), TargetPosition = (6, 5),
///     Inventory = new Inventory { Potions = 2 }
/// };
/// var canAttack = await evaluator.CanPerformAsync("CanMeleeAttack", "GoblinCapabilities", context);
/// </remarks>
public abstract record BehaviorContext
{
    /// <summary>
    /// Unique identifier for the entity this context represents
    /// </summary>
    public required string EntityId { get; init; }

    /// <summary>
    /// Current time in the simulation (for time-based rules)
    /// </summary>
    public DateTime CurrentTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Elapsed time since entity spawned (for cooldowns, etc.)
    /// </summary>
    public TimeSpan ElapsedTime { get; init; } = TimeSpan.Zero;

    /// <summary>
    /// Random seed for deterministic random behavior
    /// </summary>
    public int RandomSeed { get; init; } = Random.Shared.Next();

    /// <summary>
    /// Additional dynamic properties for flexible rule evaluation
    /// </summary>
    public Dictionary<string, object>? Properties { get; init; }
}
