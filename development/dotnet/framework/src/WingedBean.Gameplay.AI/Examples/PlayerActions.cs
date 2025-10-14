using Microsoft.Extensions.Logging;
using Plate.Capability.Core.Capabilities;
using WingedBean.Gameplay.AI.Integration;

namespace WingedBean.Gameplay.AI.Examples;

/// <summary>
/// Player actions for bot-controlled testing.
/// These allow automated testing by having a bot control the player character.
/// </summary>

// === COMBAT ACTIONS ===

public class PlayerMeleeAttackAction : CapabilityAction
{
    private readonly ILogger<PlayerMeleeAttackAction> _logger;

    public PlayerMeleeAttackAction(
        ICapabilityEvaluator evaluator,
        ILogger<PlayerMeleeAttackAction> logger) : base(evaluator)
    {
        _logger = logger;
    }

    public override string RequiredCapability => "CanMeleeAttack";
    public override string WorkflowName => "PlayerCapabilities";
    public override string ActionName => "MeleeAttack";
    public override float Cost => 1.0f;

    public override Task<bool> ExecuteAsync(object context)
    {
        if (context is not PlayerContext player)
        {
            _logger.LogWarning("Invalid context type for PlayerMeleeAttackAction");
            return Task.FromResult(false);
        }

        _logger.LogInformation(
            "[Bot] Player attacks target at {Target} with {Weapon} for {Damage} damage (Stamina: {Stamina})",
            player.TargetPosition,
            player.Equipment.WeaponType,
            player.Equipment.WeaponDamage,
            player.Stamina - 10);

        return Task.FromResult(true);
    }
}

public class PlayerRangedAttackAction : CapabilityAction
{
    private readonly ILogger<PlayerRangedAttackAction> _logger;

    public PlayerRangedAttackAction(
        ICapabilityEvaluator evaluator,
        ILogger<PlayerRangedAttackAction> logger) : base(evaluator)
    {
        _logger = logger;
    }

    public override string RequiredCapability => "CanRangedAttack";
    public override string WorkflowName => "PlayerCapabilities";
    public override string ActionName => "RangedAttack";
    public override float Cost => 1.5f;

    public override Task<bool> ExecuteAsync(object context)
    {
        if (context is not PlayerContext player)
        {
            _logger.LogWarning("Invalid context type for PlayerRangedAttackAction");
            return Task.FromResult(false);
        }

        _logger.LogInformation(
            "[Bot] Player shoots arrow at target (Distance: {Distance:F1}, Stamina: {Stamina})",
            player.DistanceToTarget,
            player.Stamina - 5);

        return Task.FromResult(true);
    }
}

public class PlayerCastSpellAction : CapabilityAction
{
    private readonly ILogger<PlayerCastSpellAction> _logger;

    public PlayerCastSpellAction(
        ICapabilityEvaluator evaluator,
        ILogger<PlayerCastSpellAction> logger) : base(evaluator)
    {
        _logger = logger;
    }

    public override string RequiredCapability => "CanCastSpell";
    public override string WorkflowName => "PlayerCapabilities";
    public override string ActionName => "CastSpell";
    public override float Cost => 2.0f;

    public override Task<bool> ExecuteAsync(object context)
    {
        if (context is not PlayerContext player)
        {
            _logger.LogWarning("Invalid context type for PlayerCastSpellAction");
            return Task.FromResult(false);
        }

        _logger.LogInformation(
            "[Bot] Player casts spell (Mana: {Mana}/{MaxMana})",
            player.Mana - 20,
            player.MaxMana);

        return Task.FromResult(true);
    }
}

// === SURVIVAL ACTIONS ===

public class PlayerUseHealthPotionAction : CapabilityAction
{
    private readonly ILogger<PlayerUseHealthPotionAction> _logger;

    public PlayerUseHealthPotionAction(
        ICapabilityEvaluator evaluator,
        ILogger<PlayerUseHealthPotionAction> logger) : base(evaluator)
    {
        _logger = logger;
    }

    public override string RequiredCapability => "CanUseHealthPotion";
    public override string WorkflowName => "PlayerCapabilities";
    public override string ActionName => "UseHealthPotion";
    public override float Cost => 3.0f;

    public override Task<bool> ExecuteAsync(object context)
    {
        if (context is not PlayerContext player)
        {
            _logger.LogWarning("Invalid context type for PlayerUseHealthPotionAction");
            return Task.FromResult(false);
        }

        _logger.LogInformation(
            "[Bot] Player uses health potion (HP: {Health}/{MaxHealth}, Potions: {Remaining})",
            player.Health,
            player.MaxHealth,
            player.Inventory.HealthPotions - 1);

        return Task.FromResult(true);
    }
}

public class PlayerFleeAction : CapabilityAction
{
    private readonly ILogger<PlayerFleeAction> _logger;

    public PlayerFleeAction(
        ICapabilityEvaluator evaluator,
        ILogger<PlayerFleeAction> logger) : base(evaluator)
    {
        _logger = logger;
    }

    public override string RequiredCapability => "CanFlee";
    public override string WorkflowName => "PlayerCapabilities";
    public override string ActionName => "Flee";
    public override float Cost => 2.5f;

    public override Task<bool> ExecuteAsync(object context)
    {
        if (context is not PlayerContext player)
        {
            _logger.LogWarning("Invalid context type for PlayerFleeAction");
            return Task.FromResult(false);
        }

        _logger.LogInformation(
            "[Bot] Player flees from combat! (HP: {Health}/{MaxHealth})",
            player.Health,
            player.MaxHealth);

        return Task.FromResult(true);
    }
}

public class PlayerRestAction : CapabilityAction
{
    private readonly ILogger<PlayerRestAction> _logger;

    public PlayerRestAction(
        ICapabilityEvaluator evaluator,
        ILogger<PlayerRestAction> logger) : base(evaluator)
    {
        _logger = logger;
    }

    public override string RequiredCapability => "CanRest";
    public override string WorkflowName => "PlayerCapabilities";
    public override string ActionName => "Rest";
    public override float Cost => 1.0f;

    public override Task<bool> ExecuteAsync(object context)
    {
        if (context is not PlayerContext player)
        {
            _logger.LogWarning("Invalid context type for PlayerRestAction");
            return Task.FromResult(false);
        }

        _logger.LogInformation(
            "[Bot] Player rests (HP: {HP:P0}, Mana: {MP:P0}, Stamina: {SP:P0})",
            player.HealthPercent,
            player.ManaPercent,
            player.StaminaPercent);

        return Task.FromResult(true);
    }
}

// === MOVEMENT ACTIONS ===

public class PlayerMoveToTargetAction : CapabilityAction
{
    private readonly ILogger<PlayerMoveToTargetAction> _logger;

    public PlayerMoveToTargetAction(
        ICapabilityEvaluator evaluator,
        ILogger<PlayerMoveToTargetAction> logger) : base(evaluator)
    {
        _logger = logger;
    }

    public override string RequiredCapability => "CanMoveToTarget";
    public override string WorkflowName => "PlayerCapabilities";
    public override string ActionName => "MoveToTarget";
    public override float Cost => 1.5f;

    public override Task<bool> ExecuteAsync(object context)
    {
        if (context is not PlayerContext player)
        {
            _logger.LogWarning("Invalid context type for PlayerMoveToTargetAction");
            return Task.FromResult(false);
        }

        _logger.LogInformation(
            "[Bot] Player moves from {From} toward target {Target} (Distance: {Distance:F1})",
            player.Position,
            player.TargetPosition,
            player.DistanceToTarget);

        return Task.FromResult(true);
    }
}

public class PlayerMoveToGoalAction : CapabilityAction
{
    private readonly ILogger<PlayerMoveToGoalAction> _logger;

    public PlayerMoveToGoalAction(
        ICapabilityEvaluator evaluator,
        ILogger<PlayerMoveToGoalAction> logger) : base(evaluator)
    {
        _logger = logger;
    }

    public override string RequiredCapability => "CanMoveToGoal";
    public override string WorkflowName => "PlayerCapabilities";
    public override string ActionName => "MoveToGoal";
    public override float Cost => 1.5f;

    public override Task<bool> ExecuteAsync(object context)
    {
        if (context is not PlayerContext player)
        {
            _logger.LogWarning("Invalid context type for PlayerMoveToGoalAction");
            return Task.FromResult(false);
        }

        _logger.LogInformation(
            "[Bot] Player moves toward quest goal {Goal} (Distance: {Distance:F1})",
            player.GoalPosition,
            player.DistanceToGoal);

        return Task.FromResult(true);
    }
}

public class PlayerExploreAction : CapabilityAction
{
    private readonly ILogger<PlayerExploreAction> _logger;

    public PlayerExploreAction(
        ICapabilityEvaluator evaluator,
        ILogger<PlayerExploreAction> logger) : base(evaluator)
    {
        _logger = logger;
    }

    public override string RequiredCapability => "CanExplore";
    public override string WorkflowName => "PlayerCapabilities";
    public override string ActionName => "Explore";
    public override float Cost => 1.0f;

    public override Task<bool> ExecuteAsync(object context)
    {
        if (context is not PlayerContext player)
        {
            _logger.LogWarning("Invalid context type for PlayerExploreAction");
            return Task.FromResult(false);
        }

        _logger.LogInformation(
            "[Bot] Player explores dungeon from {Position}",
            player.Position);

        return Task.FromResult(true);
    }
}

// === QUEST/INTERACTION ACTIONS ===

public class PlayerInteractWithNPCAction : CapabilityAction
{
    private readonly ILogger<PlayerInteractWithNPCAction> _logger;

    public PlayerInteractWithNPCAction(
        ICapabilityEvaluator evaluator,
        ILogger<PlayerInteractWithNPCAction> logger) : base(evaluator)
    {
        _logger = logger;
    }

    public override string RequiredCapability => "CanInteractWithNPC";
    public override string WorkflowName => "PlayerCapabilities";
    public override string ActionName => "InteractWithNPC";
    public override float Cost => 1.0f;

    public override Task<bool> ExecuteAsync(object context)
    {
        if (context is not PlayerContext player)
        {
            _logger.LogWarning("Invalid context type for PlayerInteractWithNPCAction");
            return Task.FromResult(false);
        }

        _logger.LogInformation(
            "[Bot] Player interacts with NPC in town");

        return Task.FromResult(true);
    }
}

public class PlayerAcceptQuestAction : CapabilityAction
{
    private readonly ILogger<PlayerAcceptQuestAction> _logger;

    public PlayerAcceptQuestAction(
        ICapabilityEvaluator evaluator,
        ILogger<PlayerAcceptQuestAction> logger) : base(evaluator)
    {
        _logger = logger;
    }

    public override string RequiredCapability => "CanAcceptQuest";
    public override string WorkflowName => "PlayerCapabilities";
    public override string ActionName => "AcceptQuest";
    public override float Cost => 1.0f;

    public override Task<bool> ExecuteAsync(object context)
    {
        if (context is not PlayerContext player)
        {
            _logger.LogWarning("Invalid context type for PlayerAcceptQuestAction");
            return Task.FromResult(false);
        }

        _logger.LogInformation(
            "[Bot] Player accepts quest");

        return Task.FromResult(true);
    }
}

public class PlayerCompleteQuestAction : CapabilityAction
{
    private readonly ILogger<PlayerCompleteQuestAction> _logger;

    public PlayerCompleteQuestAction(
        ICapabilityEvaluator evaluator,
        ILogger<PlayerCompleteQuestAction> logger) : base(evaluator)
    {
        _logger = logger;
    }

    public override string RequiredCapability => "CanCompleteQuest";
    public override string WorkflowName => "PlayerCapabilities";
    public override string ActionName => "CompleteQuest";
    public override float Cost => 0.5f; // High priority

    public override Task<bool> ExecuteAsync(object context)
    {
        if (context is not PlayerContext player)
        {
            _logger.LogWarning("Invalid context type for PlayerCompleteQuestAction");
            return Task.FromResult(false);
        }

        _logger.LogInformation(
            "[Bot] Player completes quest: {Objective}",
            player.QuestObjective);

        return Task.FromResult(true);
    }
}
