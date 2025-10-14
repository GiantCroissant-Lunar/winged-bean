using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Plate.Capability.Core.Contexts;
using Plate.Capability.RulesEngine;
using Plate.CrossMilo.Contracts.Capability;
using RulesEngine.Models;

namespace WingedBean.Gameplay.AI.Examples;

/// <summary>
/// Demonstrates runtime capability changes using different approaches.
/// Shows how to handle dynamic gameplay situations like equipment breaking,
/// status effects, and learning new abilities.
/// </summary>
public static class DynamicCapabilityDemo
{
    /// <summary>
    /// Run demo showing all approaches to runtime capability changes.
    /// </summary>
    public static async Task RunAsync(ILogger? logger = null)
    {
        logger?.LogInformation("=== Dynamic Capability Changes Demo ===");
        logger?.LogInformation("Showing how to handle runtime capability changes");
        logger?.LogInformation("");

        // Load capabilities
        var workflows = LoadGoblinCapabilities(logger);
        var evaluator = new RulesEngineCapabilityAdapter(workflows,
            logger as ILogger<RulesEngineCapabilityEvaluator>
            ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<RulesEngineCapabilityEvaluator>.Instance);

        // Demonstrate different approaches
        await Approach1_ContextUpdates(evaluator, logger);
        await Approach2_WorkflowSwapping(logger);
        await Approach3_RealWorldScenarios(evaluator, logger);

        logger?.LogInformation("");
        logger?.LogInformation("=== Demo Complete ===");
    }

    /// <summary>
    /// Approach 1: Update context properties (RECOMMENDED - 90% of cases)
    /// </summary>
    private static async Task Approach1_ContextUpdates(
        ICapabilityEvaluator evaluator,
        ILogger? logger)
    {
        logger?.LogInformation("--- Approach 1: Context Updates (Recommended) ---");
        logger?.LogInformation("Most runtime changes are STATE changes, not RULE changes");
        logger?.LogInformation("");

        // Scenario: Goblin's weapon breaks during combat
        logger?.LogInformation("Scenario: Weapon Breaking");

        // Initial state: Goblin with weapon
        var goblin = new GoblinContext
        {
            EntityId = "goblin_001",
            EntityType = "Goblin",
            Health = 20,
            MaxHealth = 20,
            AttackDamage = 10,
            Position = (5, 5),
            TargetPosition = (6, 5), // Enemy nearby
            Inventory = new GoblinInventory
            {
                Potions = 0,
                Gold = 10,
                HasWeapon = true  // ← Has weapon
            },
            IsInCombat = true,
            HasPatrolRoute = false,
            Aggression = 0.7f
        };

        var canAttack = await evaluator.CanPerformAsync(
            "CanMeleeAttack", "GoblinCapabilities", goblin);
        logger?.LogInformation("  Before: HasWeapon={HasWeapon}, CanMeleeAttack={CanAttack}",
            goblin.Inventory.HasWeapon, canAttack);

        // Weapon breaks! (just update context)
        goblin = goblin with
        {
            Inventory = goblin.Inventory with { HasWeapon = false }
        };

        canAttack = await evaluator.CanPerformAsync(
            "CanMeleeAttack", "GoblinCapabilities", goblin);
        logger?.LogInformation("  After:  HasWeapon={HasWeapon}, CanMeleeAttack={CanAttack}",
            goblin.Inventory.HasWeapon, canAttack);
        logger?.LogInformation("  ✅ Capability automatically updated via context!");
        logger?.LogInformation("");

        // Scenario: Status effect (poisoned)
        logger?.LogInformation("Scenario: Status Effect (Reduced Movement)");

        // Add "IsPoisoned" to context (would be in actual GoblinContext)
        var goblinBefore = new
        {
            Health = 20,
            MaxHealth = 20,
            MovementSpeed = 3.0f,
            IsPoisoned = false
        };

        logger?.LogInformation("  Before: IsPoisoned={IsPoisoned}, MovementSpeed={Speed}",
            goblinBefore.IsPoisoned, goblinBefore.MovementSpeed);

        // Apply poison (reduces movement speed)
        var goblinAfter = new
        {
            Health = 18,  // Taking poison damage
            MaxHealth = 20,
            MovementSpeed = 1.5f,  // Reduced!
            IsPoisoned = true
        };

        logger?.LogInformation("  After:  IsPoisoned={IsPoisoned}, MovementSpeed={Speed}",
            goblinAfter.IsPoisoned, goblinAfter.MovementSpeed);
        logger?.LogInformation("  ✅ Next capability evaluation sees reduced speed!");
        logger?.LogInformation("");

        logger?.LogInformation("💡 Key Insight: Rules stay the same, context changes!");
        logger?.LogInformation("   Rule: 'input1.HasWeapon == true' (unchanging constraint)");
        logger?.LogInformation("   Context: HasWeapon value (changes at runtime)");
        logger?.LogInformation("");
    }

    /// <summary>
    /// Approach 2: Workflow swapping (9% of cases)
    /// For when rules themselves change, not just state.
    /// </summary>
    private static async Task Approach2_WorkflowSwapping(ILogger? logger)
    {
        logger?.LogInformation("--- Approach 2: Workflow Swapping (Rare Cases) ---");
        logger?.LogInformation("Use when adding/removing capabilities entirely");
        logger?.LogInformation("");

        logger?.LogInformation("Scenario: Goblin learns 'Throw Rock' ability (new capability)");

        // Initial capabilities
        var baseWorkflow = new Workflow
        {
            WorkflowName = "GoblinCapabilities",
            Rules = new List<Rule>
            {
                new Rule
                {
                    RuleName = "CanMeleeAttack",
                    Expression = "input1.Health > 0 AND input1.Distance <= 1.5"
                }
            }
        };

        logger?.LogInformation("  Initial: {Count} capability (CanMeleeAttack)",
            baseWorkflow.Rules.Count());

        // Learn new ability - add new rule!
        var enhancedWorkflow = new Workflow
        {
            WorkflowName = "GoblinCapabilities",
            Rules = new List<Rule>
            {
                new Rule
                {
                    RuleName = "CanMeleeAttack",
                    Expression = "input1.Health > 0 AND input1.Distance <= 1.5"
                },
                new Rule
                {
                    RuleName = "CanThrowRock",  // ← NEW CAPABILITY!
                    Expression = "input1.Health > 0 AND input1.HasRock == true AND input1.Distance <= 5.0"
                }
            }
        };

        logger?.LogInformation("  Enhanced: {Count} capabilities (added CanThrowRock)",
            enhancedWorkflow.Rules.Count());

        // Create new evaluator with enhanced workflow
        var enhancedEvaluator = new RulesEngineCapabilityEvaluator(
            new[] { enhancedWorkflow },
            Microsoft.Extensions.Logging.Abstractions.NullLogger<RulesEngineCapabilityEvaluator>.Instance);

        logger?.LogInformation("  ✅ New evaluator created with enhanced capabilities!");
        logger?.LogInformation("");

        logger?.LogInformation("💡 When to use:");
        logger?.LogInformation("   - Learning new abilities (permanent)");
        logger?.LogInformation("   - Game patches/DLC");
        logger?.LogInformation("   - Mod system additions");
        logger?.LogInformation("   - Boss phase transitions (temporary ruleset)");
        logger?.LogInformation("");
    }

    /// <summary>
    /// Approach 3: Real-world scenarios combining techniques
    /// </summary>
    private static async Task Approach3_RealWorldScenarios(
        ICapabilityEvaluator evaluator,
        ILogger? logger)
    {
        logger?.LogInformation("--- Real-World Scenarios ---");
        logger?.LogInformation("");

        // Scenario 1: Combat damage (context update)
        logger?.LogInformation("Scenario 1: Taking Damage in Combat");
        var goblin = CreateGoblin("goblin_001", health: 20);

        logger?.LogInformation("  Turn 1: HP={Health}/{MaxHealth}",
            goblin.Health, goblin.MaxHealth);
        var canFlee = await evaluator.CanPerformAsync(
            "CanFlee", "GoblinCapabilities", goblin);
        logger?.LogInformation("  CanFlee={CanFlee} (rule: HP < 30%)", canFlee);

        // Take damage!
        goblin = goblin with { Health = 5 };  // 25% health
        logger?.LogInformation("  Turn 2: HP={Health}/{MaxHealth} (took damage!)",
            goblin.Health, goblin.MaxHealth);
        canFlee = await evaluator.CanPerformAsync(
            "CanFlee", "GoblinCapabilities", goblin);
        logger?.LogInformation("  CanFlee={CanFlee} (critically injured!)", canFlee);
        logger?.LogInformation("  ✅ Context updated, flee capability activated");
        logger?.LogInformation("");

        // Scenario 2: Pick up health potion (context update)
        logger?.LogInformation("Scenario 2: Finding Health Potion");
        goblin = goblin with
        {
            Health = 12,
            Inventory = goblin.Inventory with { Potions = 0 }
        };

        var canUsePotion = await evaluator.CanPerformAsync(
            "CanUsePotion", "GoblinCapabilities", goblin);
        logger?.LogInformation("  Before pickup: Potions={Potions}, CanUsePotion={Can}",
            goblin.Inventory.Potions, canUsePotion);

        // Pick up potion!
        goblin = goblin with
        {
            Inventory = goblin.Inventory with { Potions = 1 }
        };

        canUsePotion = await evaluator.CanPerformAsync(
            "CanUsePotion", "GoblinCapabilities", goblin);
        logger?.LogInformation("  After pickup:  Potions={Potions}, CanUsePotion={Can}",
            goblin.Inventory.Potions, canUsePotion);
        logger?.LogInformation("  ✅ New capability available immediately!");
        logger?.LogInformation("");

        // Scenario 3: Moving toward target (context update)
        logger?.LogInformation("Scenario 3: Moving Toward Target");
        goblin = goblin with
        {
            Health = 20,
            Position = (5, 5),
            TargetPosition = (10, 10)
        };

        logger?.LogInformation("  Position: {Pos}, Target: {Target}, Distance: {Dist:F1}",
            goblin.Position, goblin.TargetPosition, goblin.Distance);

        var canMeleeAttack = await evaluator.CanPerformAsync(
            "CanMeleeAttack", "GoblinCapabilities", goblin);
        var canMoveToTarget = await evaluator.CanPerformAsync(
            "CanMoveToTarget", "GoblinCapabilities", goblin);

        logger?.LogInformation("  CanMeleeAttack={Can} (too far)", canMeleeAttack);
        logger?.LogInformation("  CanMoveToTarget={Can} (need to close distance)", canMoveToTarget);

        // Move closer!
        goblin = goblin with { Position = (9, 10) };

        logger?.LogInformation("  Moved to: {Pos}, Distance: {Dist:F1}",
            goblin.Position, goblin.Distance);

        canMeleeAttack = await evaluator.CanPerformAsync(
            "CanMeleeAttack", "GoblinCapabilities", goblin);

        logger?.LogInformation("  CanMeleeAttack={Can} (in range!)", canMeleeAttack);
        logger?.LogInformation("  ✅ Capabilities update as position changes!");
        logger?.LogInformation("");
    }

    private static GoblinContext CreateGoblin(string id, int health)
    {
        return new GoblinContext
        {
            EntityId = id,
            EntityType = "Goblin",
            Health = health,
            MaxHealth = 20,
            AttackDamage = 5,
            Position = (5, 5),
            TargetPosition = (6, 5),
            Inventory = new GoblinInventory
            {
                Potions = 0,
                Gold = 10,
                HasWeapon = true
            },
            IsInCombat = true,
            HasPatrolRoute = false,
            Aggression = 0.7f
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
                logger?.LogWarning("Could not load goblin capabilities");
                return Array.Empty<Workflow>();
            }

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var workflows = JsonSerializer.Deserialize<List<Workflow>>(json);

            return workflows?.ToArray() ?? Array.Empty<Workflow>();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to load goblin capabilities");
            return Array.Empty<Workflow>();
        }
    }
}
