using Microsoft.Extensions.Logging;
using RulesEngine.Models;

namespace WingedBean.Gameplay.AI.Capabilities;

/// <summary>
/// Capability evaluator that uses Microsoft RulesEngine for declarative capability definitions.
/// Wraps RulesEngine to provide a clean API for GOAP integration.
/// </summary>
/// <remarks>
/// Architecture Benefits:
/// - Declarative: Define capabilities in JSON, not code
/// - Fast: RulesEngine optimized for boolean evaluations
/// - Flexible: Same evaluator for all entity types
/// - Testable: Easy to unit test with mock contexts
/// 
/// Rules Engine Pattern:
/// - Input: Entity state, target, environment
/// - Rules: Boolean expressions (e.g., "entity.Health > 0 && distance <= 1.5")
/// - Output: Can/cannot perform capability
/// 
/// Integration with GOAP:
/// - GOAP actions call CanPerformAsync() in CheckPreconditions()
/// - Planner only considers actions with satisfied capabilities
/// - Optimal path found using only valid actions
/// </remarks>
public class RulesEngineCapabilityEvaluator : ICapabilityEvaluator
{
    private readonly RulesEngine.RulesEngine _rulesEngine;
    private readonly ILogger<RulesEngineCapabilityEvaluator> _logger;

    /// <summary>
    /// Create evaluator with pre-configured RulesEngine
    /// </summary>
    /// <param name="workflows">Rule workflows loaded from JSON</param>
    /// <param name="logger">Logger for diagnostics</param>
    public RulesEngineCapabilityEvaluator(
        Workflow[] workflows,
        ILogger<RulesEngineCapabilityEvaluator> logger)
    {
        _rulesEngine = new RulesEngine.RulesEngine(workflows, null);
        _logger = logger;
    }

    /// <summary>
    /// Evaluate all capabilities in a workflow.
    /// Use this when you need to know everything an entity can do.
    /// </summary>
    public async Task<CapabilityResult[]> EvaluateAllAsync(string workflowName, object context)
    {
        try
        {
            var results = await _rulesEngine.ExecuteAllRulesAsync(workflowName, context);

            return results.Select(r => new CapabilityResult
            {
                CapabilityName = r.Rule.RuleName,
                CanPerform = r.IsSuccess,
                Cost = ExtractCost(r),
                Reason = r.IsSuccess ? null : r.ExceptionMessage,
                Metadata = ExtractMetadata(r)
            }).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate capabilities for workflow: {WorkflowName}", workflowName);
            return Array.Empty<CapabilityResult>();
        }
    }

    /// <summary>
    /// Check single capability (optimized for GOAP preconditions).
    /// Returns just true/false, no extra data.
    /// </summary>
    public async Task<bool> CanPerformAsync(string capabilityName, string workflowName, object context)
    {
        try
        {
            var results = await _rulesEngine.ExecuteAllRulesAsync(workflowName, context);
            var result = results.FirstOrDefault(r => r.Rule.RuleName == capabilityName);
            return result?.IsSuccess ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Capability check failed: {Capability} in {Workflow}", capabilityName, workflowName);
            return false;
        }
    }

    /// <summary>
    /// Get detailed capability result (for debugging, UI tooltips, etc.).
    /// </summary>
    public async Task<CapabilityResult> EvaluateAsync(string capabilityName, string workflowName, object context)
    {
        try
        {
            var results = await _rulesEngine.ExecuteAllRulesAsync(workflowName, context);
            var result = results.FirstOrDefault(r => r.Rule.RuleName == capabilityName);

            if (result == null)
            {
                return CapabilityResult.Failure(capabilityName, $"Capability not found in workflow: {workflowName}");
            }

            return new CapabilityResult
            {
                CapabilityName = capabilityName,
                CanPerform = result.IsSuccess,
                Cost = ExtractCost(result),
                Reason = result.IsSuccess ? null : result.ExceptionMessage,
                Metadata = ExtractMetadata(result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate capability: {Capability} in {Workflow}", capabilityName, workflowName);
            return CapabilityResult.Failure(capabilityName, $"Evaluation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Extract cost from rule metadata.
    /// For now returns default cost - can be enhanced to parse from rule properties.
    /// </summary>
    private float ExtractCost(RuleResultTree result)
    {
        // Future enhancement: parse from custom rule properties or expressions
        return 1.0f; // Default cost
    }

    /// <summary>
    /// Extract metadata from rule result.
    /// For now returns null - can be enhanced to extract from rule properties.
    /// </summary>
    private Dictionary<string, object>? ExtractMetadata(RuleResultTree result)
    {
        // Future enhancement: extract from rule properties or local params
        return null;
    }
}
