using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Plate.Capability.Core.Contexts;
using Plate.Capability.RulesEngine;
using Plate.CrossMilo.Contracts.Capability;
using RulesEngine.Models;
using WingedBean.Gameplay.AI.Integration;

namespace WingedBean.Gameplay.AI.Examples;

/// <summary>
/// Demonstrates the Rules + GOAP architecture with a Goblin NPC.
/// Shows how capabilities are evaluated and actions are selected.
/// </summary>
public static class GoblinDemo
{
    /// <summary>
    /// Run the demo - evaluates capabilities and executes actions for a Goblin
    /// </summary>
    public static async Task RunAsync(ILogger? logger = null)
    {
        logger?.LogInformation("=== Goblin AI Demo ===");
        logger?.LogInformation("Demonstrating Rules Engine + Capability-Based Actions");
        logger?.LogInformation("");

        // 1. Load capability rules from embedded JSON
        var workflows = LoadGoblinCapabilities(logger);

        // 2. Create capability evaluator
        var evaluator = new RulesEngineCapabilityEvaluator(workflows,
            logger as ILogger<RulesEngineCapabilityEvaluator>
            ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<RulesEngineCapabilityEvaluator>.Instance);

        // 3. Scenario 1: Healthy goblin with target nearby
        logger?.LogInformation("--- Scenario 1: Healthy Goblin, Target Nearby ---");
        var healthyGoblin = CreateGoblinContext("goblin_001",
            health: 20, maxHealth: 20,
            position: (5, 5), target: (6, 5));
        await EvaluateAndExecuteAsync(healthyGoblin, evaluator, logger);
        logger?.LogInformation("");

        // 4. Scenario 2: Injured goblin (should consider fleeing)
        logger?.LogInformation("--- Scenario 2: Injured Goblin (25% health) ---");
        var injuredGoblin = healthyGoblin with { Health = 5 };
        await EvaluateAndExecuteAsync(injuredGoblin, evaluator, logger);
        logger?.LogInformation("");

        // 5. Scenario 3: Injured goblin with potion (should use it)
        logger?.LogInformation("--- Scenario 3: Injured Goblin with Potion ---");
        var goblinWithPotion = injuredGoblin with
        {
            Health = 12,
            Inventory = injuredGoblin.Inventory with { Potions = 1 }
        };
        await EvaluateAndExecuteAsync(goblinWithPotion, evaluator, logger);
        logger?.LogInformation("");

        // 6. Scenario 4: Target far away (should move closer)
        logger?.LogInformation("--- Scenario 4: Target Far Away ---");
        var distantTarget = healthyGoblin with { TargetPosition = (10, 8) };
        await EvaluateAndExecuteAsync(distantTarget, evaluator, logger);
        logger?.LogInformation("");

        logger?.LogInformation("=== Demo Complete ===");
    }

    private static async Task EvaluateAndExecuteAsync(
        GoblinContext context,
        ICapabilityEvaluator evaluator,
        ILogger? logger)
    {
        // Evaluate all capabilities
        var capabilities = await evaluator.EvaluateAllAsync("GoblinCapabilities", context);

        var available = capabilities.Where(c => c.CanPerform).ToArray();
        var unavailable = capabilities.Where(c => !c.CanPerform).ToArray();

        logger?.LogInformation("Context: HP {Health}/{MaxHealth}, Position {Pos}, Target {Target}, Distance {Distance:F1}",
            context.Health, context.MaxHealth, context.Position, context.TargetPosition, context.Distance);

        logger?.LogInformation("Available Capabilities ({Count}): {Capabilities}",
            available.Length,
            string.Join(", ", available.Select(c => c.CapabilityName)));

        if (unavailable.Length > 0)
        {
            logger?.LogDebug("Unavailable Capabilities ({Count}): {Capabilities}",
                unavailable.Length,
                string.Join(", ", unavailable.Select(c => c.CapabilityName)));
        }

        // Create actions and execute first available one (simplified planner)
        var actions = CreateActions(evaluator, logger);

        foreach (var action in actions.OrderBy(a => a.Cost))
        {
            if (await action.CanExecuteAsync(context))
            {
                logger?.LogInformation("Executing: {Action} (Cost: {Cost})",
                    action.ActionName, action.Cost);
                await action.ExecuteAsync(context);
                break; // Execute one action per tick
            }
        }
    }

    private static GoblinContext CreateGoblinContext(
        string id,
        int health,
        int maxHealth,
        (int X, int Y) position,
        (int X, int Y)? target)
    {
        return new GoblinContext
        {
            EntityId = id,
            EntityType = "Goblin",
            Health = health,
            MaxHealth = maxHealth,
            AttackDamage = 5,
            Position = position,
            TargetPosition = target,
            Inventory = new GoblinInventory
            {
                Potions = 0,
                Gold = 10,
                HasWeapon = true
            },
            IsInCombat = target.HasValue,
            HasPatrolRoute = true,
            Aggression = 0.7f
        };
    }

    private static CapabilityAction[] CreateActions(
        ICapabilityEvaluator evaluator,
        ILogger? logger)
    {
        var loggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;

        return new CapabilityAction[]
        {
            new GoblinFleeAction(evaluator,
                logger as ILogger<GoblinFleeAction> ?? loggerFactory.CreateLogger<GoblinFleeAction>()),
            new GoblinUsePotionAction(evaluator,
                logger as ILogger<GoblinUsePotionAction> ?? loggerFactory.CreateLogger<GoblinUsePotionAction>()),
            new GoblinMeleeAttackAction(evaluator,
                logger as ILogger<GoblinMeleeAttackAction> ?? loggerFactory.CreateLogger<GoblinMeleeAttackAction>()),
            new GoblinMoveToTargetAction(evaluator,
                logger as ILogger<GoblinMoveToTargetAction> ?? loggerFactory.CreateLogger<GoblinMoveToTargetAction>())
        };
    }

    private static Workflow[] LoadGoblinCapabilities(ILogger? logger)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "WingedBean.Gameplay.AI.Examples.goblin-capabilities.json";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                logger?.LogWarning("Could not load goblin capabilities from embedded resource");
                return Array.Empty<Workflow>();
            }

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var workflows = JsonSerializer.Deserialize<List<Workflow>>(json);

            if (workflows != null && workflows.Count > 0)
            {
                logger?.LogDebug("Loaded {Count} capability workflow(s)", workflows.Count);
                return workflows.ToArray();
            }

            return Array.Empty<Workflow>();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to load goblin capabilities");
            return Array.Empty<Workflow>();
        }
    }
}
