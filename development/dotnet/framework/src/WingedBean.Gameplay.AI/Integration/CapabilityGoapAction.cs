using WingedBean.Gameplay.AI.Capabilities;

namespace WingedBean.Gameplay.AI.Integration;

/// <summary>
/// Base class for actions that use capability checking for preconditions.
/// Bridges the Rules Engine (capabilities) with action execution.
/// </summary>
/// <remarks>
/// Architecture Pattern:
/// - Rules Engine: Defines "can this be done?" (capabilities)
/// - Action: Defines "what happens if done?" (effects)
/// - Planner (GOAP): Determines "should this be done?" (optimal sequence)
/// 
/// This is a simplified base that demonstrates the pattern.
/// For full GOAP integration, derive from Plate.Goap.Core.IGoapAction instead.
/// 
/// Example:
/// public class GoblinMeleeAttack : CapabilityAction
/// {
///     public override string RequiredCapability => "CanMeleeAttack";
///     public override string WorkflowName => "GoblinCapabilities";
///     
///     protected override Task&lt;bool&gt; ExecuteActionAsync(object context)
///     {
///         // Apply damage, play animation, etc.
///         return Task.FromResult(true);
///     }
/// }
/// </remarks>
public abstract class CapabilityAction
{
    private readonly ICapabilityEvaluator _evaluator;

    protected CapabilityAction(ICapabilityEvaluator evaluator)
    {
        _evaluator = evaluator;
    }

    /// <summary>
    /// Name of the capability required to perform this action.
    /// Must match a rule name in the workflow.
    /// </summary>
    public abstract string RequiredCapability { get; }

    /// <summary>
    /// Name of the rules workflow containing the capability.
    /// </summary>
    public abstract string WorkflowName { get; }

    /// <summary>
    /// Action name (for logging/debugging)
    /// </summary>
    public abstract string ActionName { get; }

    /// <summary>
    /// Cost of this action (lower is preferred)
    /// </summary>
    public virtual float Cost => 1.0f;

    /// <summary>
    /// Check if this action's preconditions are met.
    /// Uses capability evaluator to determine if action is valid.
    /// </summary>
    public virtual async Task<bool> CanExecuteAsync(object context)
    {
        try
        {
            return await _evaluator.CanPerformAsync(RequiredCapability, WorkflowName, context);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get detailed capability result (for debugging/costs)
    /// </summary>
    public virtual async Task<CapabilityResult> EvaluateCapabilityAsync(object context)
    {
        return await _evaluator.EvaluateAsync(RequiredCapability, WorkflowName, context);
    }

    /// <summary>
    /// Execute the action's logic (damage, movement, etc.)
    /// Override this in derived classes.
    /// </summary>
    public abstract Task<bool> ExecuteAsync(object context);
}
