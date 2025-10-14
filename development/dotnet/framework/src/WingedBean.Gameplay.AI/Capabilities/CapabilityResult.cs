namespace WingedBean.Gameplay.AI.Capabilities;

/// <summary>
/// Result of a capability evaluation.
/// Indicates whether an entity can perform a specific action.
/// </summary>
public sealed record CapabilityResult
{
    /// <summary>
    /// Name of the capability being evaluated (e.g., "CanMeleeAttack", "CanCastSpell")
    /// </summary>
    public required string CapabilityName { get; init; }

    /// <summary>
    /// Whether the entity can currently perform this capability
    /// </summary>
    public required bool CanPerform { get; init; }

    /// <summary>
    /// Optional cost/weight for performing this capability.
    /// Used by GOAP planner to prefer cheaper actions.
    /// </summary>
    public float Cost { get; init; } = 1.0f;

    /// <summary>
    /// Optional reason why capability cannot be performed (for debugging/UI)
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Additional metadata from the rules engine
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Create a successful capability result
    /// </summary>
    public static CapabilityResult Success(string capabilityName, float cost = 1.0f, Dictionary<string, object>? metadata = null) =>
        new()
        {
            CapabilityName = capabilityName,
            CanPerform = true,
            Cost = cost,
            Metadata = metadata
        };

    /// <summary>
    /// Create a failed capability result
    /// </summary>
    public static CapabilityResult Failure(string capabilityName, string? reason = null) =>
        new()
        {
            CapabilityName = capabilityName,
            CanPerform = false,
            Reason = reason
        };
}
