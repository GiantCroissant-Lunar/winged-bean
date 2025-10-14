using Microsoft.Extensions.Logging;
using Plate.CrossMilo.Contracts.Capability;
using WingedBean.Gameplay.AI.Integration;

namespace WingedBean.Gameplay.AI.Examples;

/// <summary>
/// Example action: Goblin melee attack.
/// Demonstrates capability-based action execution.
/// </summary>
public class GoblinMeleeAttackAction : CapabilityAction
{
    private readonly ILogger<GoblinMeleeAttackAction> _logger;

    public GoblinMeleeAttackAction(
        ICapabilityEvaluator evaluator,
        ILogger<GoblinMeleeAttackAction> logger) : base(evaluator)
    {
        _logger = logger;
    }

    public override string RequiredCapability => "CanMeleeAttack";
    public override string WorkflowName => "GoblinCapabilities";
    public override string ActionName => "MeleeAttack";
    public override float Cost => 1.0f;

    public override Task<bool> ExecuteAsync(object context)
    {
        if (context is not GoblinContext goblin)
        {
            _logger.LogWarning("Invalid context type for GoblinMeleeAttackAction");
            return Task.FromResult(false);
        }

        _logger.LogInformation(
            "Goblin {EntityId} attacks target at {Target} for {Damage} damage",
            goblin.EntityId,
            goblin.TargetPosition,
            goblin.AttackDamage);

        // In real game: apply damage, play animation, etc.
        return Task.FromResult(true);
    }
}

/// <summary>
/// Example action: Goblin flees from combat.
/// </summary>
public class GoblinFleeAction : CapabilityAction
{
    private readonly ILogger<GoblinFleeAction> _logger;

    public GoblinFleeAction(
        ICapabilityEvaluator evaluator,
        ILogger<GoblinFleeAction> logger) : base(evaluator)
    {
        _logger = logger;
    }

    public override string RequiredCapability => "CanFlee";
    public override string WorkflowName => "GoblinCapabilities";
    public override string ActionName => "Flee";
    public override float Cost => 2.0f;

    public override Task<bool> ExecuteAsync(object context)
    {
        if (context is not GoblinContext goblin)
        {
            _logger.LogWarning("Invalid context type for GoblinFleeAction");
            return Task.FromResult(false);
        }

        _logger.LogInformation(
            "Goblin {EntityId} flees from combat (Health: {Health}/{MaxHealth})",
            goblin.EntityId,
            goblin.Health,
            goblin.MaxHealth);

        // In real game: move away from target, set fleeing state, etc.
        return Task.FromResult(true);
    }
}

/// <summary>
/// Example action: Goblin uses health potion.
/// </summary>
public class GoblinUsePotionAction : CapabilityAction
{
    private readonly ILogger<GoblinUsePotionAction> _logger;

    public GoblinUsePotionAction(
        ICapabilityEvaluator evaluator,
        ILogger<GoblinUsePotionAction> logger) : base(evaluator)
    {
        _logger = logger;
    }

    public override string RequiredCapability => "CanUsePotion";
    public override string WorkflowName => "GoblinCapabilities";
    public override string ActionName => "UsePotion";
    public override float Cost => 3.0f;

    public override Task<bool> ExecuteAsync(object context)
    {
        if (context is not GoblinContext goblin)
        {
            _logger.LogWarning("Invalid context type for GoblinUsePotionAction");
            return Task.FromResult(false);
        }

        _logger.LogInformation(
            "Goblin {EntityId} uses health potion ({Remaining} remaining)",
            goblin.EntityId,
            goblin.Inventory.Potions - 1);

        // In real game: heal, consume potion, play effect, etc.
        return Task.FromResult(true);
    }
}

/// <summary>
/// Example action: Goblin moves toward target.
/// </summary>
public class GoblinMoveToTargetAction : CapabilityAction
{
    private readonly ILogger<GoblinMoveToTargetAction> _logger;

    public GoblinMoveToTargetAction(
        ICapabilityEvaluator evaluator,
        ILogger<GoblinMoveToTargetAction> logger) : base(evaluator)
    {
        _logger = logger;
    }

    public override string RequiredCapability => "CanMoveToTarget";
    public override string WorkflowName => "GoblinCapabilities";
    public override string ActionName => "MoveToTarget";
    public override float Cost => 1.5f;

    public override Task<bool> ExecuteAsync(object context)
    {
        if (context is not GoblinContext goblin)
        {
            _logger.LogWarning("Invalid context type for GoblinMoveToTargetAction");
            return Task.FromResult(false);
        }

        _logger.LogInformation(
            "Goblin {EntityId} moves from {From} toward {Target}",
            goblin.EntityId,
            goblin.Position,
            goblin.TargetPosition);

        // In real game: pathfind, update position, play animation, etc.
        return Task.FromResult(true);
    }
}
