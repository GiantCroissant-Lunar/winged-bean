using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Plate.Capability.RulesEngine;
using Plate.CrossMilo.Contracts.Capability;
using RulesEngine.Models;

namespace WingedBean.Gameplay.AI.Integration;

/// <summary>
/// Local adapter: wraps capability-orlia RulesEngine evaluator to the CrossMilo capability contract.
/// Useful for demos/tests when a plate plugin is not present.
/// </summary>
public sealed class RulesEngineCapabilityAdapter : ICapabilityEvaluator
{
    private readonly RulesEngineCapabilityEvaluator _inner;

    public RulesEngineCapabilityAdapter(IEnumerable<Workflow> workflows, ILogger<RulesEngineCapabilityEvaluator>? logger = null)
    {
        _inner = new RulesEngineCapabilityEvaluator(workflows, logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<RulesEngineCapabilityEvaluator>.Instance);
    }

    public Task<bool> CanPerformAsync(string capability, string workflow, object context, CancellationToken ct = default)
        => _inner.CanPerformAsync(capability, workflow, context, ct);

    public async Task<CapabilityResult> EvaluateAsync(string capability, string workflow, object context, CancellationToken ct = default)
    {
        var r = await _inner.EvaluateAsync(capability, workflow, context, ct).ConfigureAwait(false);
        return new CapabilityResult(r.CapabilityName, r.CanPerform, r.Reason, r.Score);
    }

    public async Task<IReadOnlyList<CapabilityResult>> EvaluateAllAsync(string workflow, object context, CancellationToken ct = default)
    {
        var list = await _inner.EvaluateAllAsync(workflow, context, ct).ConfigureAwait(false);
        var res = new List<CapabilityResult>(list.Count);
        foreach (var r in list)
            res.Add(new CapabilityResult(r.CapabilityName, r.CanPerform, r.Reason, r.Score));
        return res;
    }
}
