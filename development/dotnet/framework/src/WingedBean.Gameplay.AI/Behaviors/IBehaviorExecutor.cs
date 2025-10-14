using Plate.Capability.Core.Contexts;

namespace WingedBean.Gameplay.AI.Behaviors;

/// <summary>
/// Executes AI behaviors using the Rules + GOAP architecture.
/// Orchestrates capability evaluation and action planning.
/// </summary>
/// <remarks>
/// Execution Flow:
/// 1. Evaluate capabilities (what CAN entity do?)
/// 2. Filter GOAP actions by capabilities
/// 3. Plan action sequence to reach goal
/// 4. Execute actions (with dynamic re-evaluation)
/// 
/// Example:
/// var executor = new NpcBehaviorExecutor(evaluator, planner);
/// var plan = await executor.PlanAsync(goblinContext, "DefeatEnemy");
/// await executor.ExecuteAsync(plan, cancellationToken);
/// </remarks>
public interface IBehaviorExecutor
{
    /// <summary>
    /// Plan a sequence of actions to achieve a goal.
    /// Uses capability evaluation to filter available actions.
    /// </summary>
    /// <param name="context">Current entity state and environment</param>
    /// <param name="goalName">Name of the goal to achieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Planned action sequence, or null if no plan found</returns>
    Task<BehaviorPlan?> PlanAsync(BehaviorContext context, string goalName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a planned behavior sequence.
    /// Re-evaluates capabilities between actions to handle dynamic changes.
    /// </summary>
    /// <param name="plan">The plan to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if plan completed successfully, false if failed or cancelled</returns>
    Task<bool> ExecuteAsync(BehaviorPlan plan, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a planned sequence of actions.
/// </summary>
public sealed record BehaviorPlan
{
    public required string GoalName { get; init; }
    public required BehaviorContext Context { get; init; }
    public required string[] ActionSequence { get; init; }
    public required float TotalCost { get; init; }
    public DateTime PlanTime { get; init; } = DateTime.UtcNow;
}
