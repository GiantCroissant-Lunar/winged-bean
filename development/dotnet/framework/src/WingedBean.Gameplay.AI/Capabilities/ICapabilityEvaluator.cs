namespace WingedBean.Gameplay.AI.Capabilities;

/// <summary>
/// Evaluates what capabilities (actions) an entity can perform based on current state.
/// This forms the foundation of the Rules + GOAP architecture:
/// - Rules Engine: Defines WHAT CAN be done (capabilities/constraints)
/// - GOAP Planner: Determines WHAT SHOULD be done (optimal action sequence)
/// </summary>
/// <remarks>
/// Design Philosophy:
/// - Capabilities are declarative (defined in JSON/rules, not code)
/// - Evaluation is fast (boolean checks with optional costs)
/// - Re-evaluation is dynamic (conditions can change mid-execution)
/// - Same rules work for players, NPCs, and monsters
/// 
/// Example Usage:
/// 1. Define capabilities in JSON: "CanMeleeAttack", "CanFlee", "CanUsePotion"
/// 2. GOAP actions check capabilities as preconditions
/// 3. Planner only considers actions with satisfied capabilities
/// 4. Execute action sequence knowing all preconditions are met
/// </remarks>
public interface ICapabilityEvaluator
{
    /// <summary>
    /// Evaluate all capabilities for a given workflow.
    /// Returns which actions the entity can currently perform.
    /// </summary>
    /// <param name="workflowName">Name of the rules workflow (e.g., "GoblinCapabilities")</param>
    /// <param name="context">Current state/context for evaluation (entity, environment, etc.)</param>
    /// <returns>Array of capability results indicating what can be done</returns>
    Task<CapabilityResult[]> EvaluateAllAsync(string workflowName, object context);

    /// <summary>
    /// Check if a specific capability can be performed.
    /// Optimized for single capability checks (e.g., in GOAP preconditions).
    /// </summary>
    /// <param name="capabilityName">Name of the capability to check</param>
    /// <param name="workflowName">Name of the rules workflow</param>
    /// <param name="context">Current state/context for evaluation</param>
    /// <returns>True if capability can be performed, false otherwise</returns>
    Task<bool> CanPerformAsync(string capabilityName, string workflowName, object context);

    /// <summary>
    /// Get detailed result for a specific capability.
    /// Includes cost, metadata, and failure reasons (useful for debugging).
    /// </summary>
    /// <param name="capabilityName">Name of the capability to check</param>
    /// <param name="workflowName">Name of the rules workflow</param>
    /// <param name="context">Current state/context for evaluation</param>
    /// <returns>Detailed capability result</returns>
    Task<CapabilityResult> EvaluateAsync(string capabilityName, string workflowName, object context);
}
