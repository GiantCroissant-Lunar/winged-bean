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
/// Demonstrates bot-controlled player for automated testing.
/// Bot takes control of player character and performs actions based on capabilities.
/// 
/// Use Cases:
/// - Automated gameplay testing
/// - Recording generation for CI/CD
/// - Regression testing of game mechanics
/// - Stress testing with multiple bot-controlled players
/// </summary>
public static class PlayerBotDemo
{
    /// <summary>
    /// Run automated player bot demo.
    /// Shows how a bot can control player character for testing.
    /// </summary>
    public static async Task RunAsync(ILogger? logger = null)
    {
        logger?.LogInformation("=== Player Bot Demo (Automated Testing) ===");
        logger?.LogInformation("Bot takes control of player character for testing");
        logger?.LogInformation("");

        // 1. Load player capability rules
        var workflows = LoadPlayerCapabilities(logger);

        // 2. Create capability evaluator (CrossMilo contract via adapter)
        var evaluator = new RulesEngineCapabilityAdapter(workflows,
            logger as ILogger<RulesEngineCapabilityEvaluator>
            ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<RulesEngineCapabilityEvaluator>.Instance);

        // 3. Create player actions
        var actions = CreatePlayerActions(evaluator, logger);

        // 4. Run test scenarios
        await RunTestScenario1_CombatEncounter(evaluator, actions, logger);
        await RunTestScenario2_Exploration(evaluator, actions, logger);
        await RunTestScenario3_QuestCompletion(evaluator, actions, logger);
        await RunTestScenario4_Survival(evaluator, actions, logger);

        logger?.LogInformation("");
        logger?.LogInformation("=== Player Bot Demo Complete ===");
        logger?.LogInformation("Bot successfully controlled player through multiple scenarios");
    }

    /// <summary>
    /// Test Scenario 1: Combat Encounter
    /// Player engages enemy, fights, uses potion if needed
    /// </summary>
    private static async Task RunTestScenario1_CombatEncounter(
        ICapabilityEvaluator evaluator,
        CapabilityAction[] actions,
        ILogger? logger)
    {
        logger?.LogInformation("--- Scenario 1: Combat Encounter ---");

        // Start: Player healthy, sees enemy
        var player = CreatePlayer("player_001",
            health: 100, maxHealth: 100,
            mana: 50, maxMana: 100,
            stamina: 80, maxStamina: 100,
            position: (10, 10),
            target: (11, 10), // Enemy nearby
            inCombat: true,
            inDungeon: true);

        logger?.LogInformation("[Bot] Player enters combat with enemy");
        await ExecuteBotTurn(player, evaluator, actions, logger);

        // After attack: Player damaged, needs potion
        player = player with
        {
            Health = 60,
            Stamina = 70,
            Inventory = player.Inventory with { HealthPotions = 2 }
        };
        logger?.LogInformation("[Bot] Player takes damage, evaluating options...");
        await ExecuteBotTurn(player, evaluator, actions, logger);

        // Continue fighting
        player = player with { Health = 85, Stamina = 65 };
        logger?.LogInformation("[Bot] Player healed, continuing combat...");
        await ExecuteBotTurn(player, evaluator, actions, logger);

        logger?.LogInformation("");
    }

    /// <summary>
    /// Test Scenario 2: Dungeon Exploration
    /// Player explores dungeon, searches for loot and objectives
    /// </summary>
    private static async Task RunTestScenario2_Exploration(
        ICapabilityEvaluator evaluator,
        CapabilityAction[] actions,
        ILogger? logger)
    {
        logger?.LogInformation("--- Scenario 2: Dungeon Exploration ---");

        var player = CreatePlayer("player_001",
            health: 100, maxHealth: 100,
            mana: 80, maxMana: 100,
            stamina: 90, maxStamina: 100,
            position: (5, 5),
            target: null, // No combat
            inCombat: false,
            inDungeon: true,
            goal: (15, 20)); // Distant goal

        logger?.LogInformation("[Bot] Player exploring dungeon, moving toward goal");
        await ExecuteBotTurn(player, evaluator, actions, logger);

        // Move closer to goal
        player = player with { Position = (10, 12), Stamina = 85 };
        await ExecuteBotTurn(player, evaluator, actions, logger);

        // Near goal
        player = player with { Position = (14, 19), Stamina = 80 };
        await ExecuteBotTurn(player, evaluator, actions, logger);

        logger?.LogInformation("");
    }

    /// <summary>
    /// Test Scenario 3: Quest Completion
    /// Player accepts quest, navigates to objective, completes it
    /// </summary>
    private static async Task RunTestScenario3_QuestCompletion(
        ICapabilityEvaluator evaluator,
        CapabilityAction[] actions,
        ILogger? logger)
    {
        logger?.LogInformation("--- Scenario 3: Quest Workflow ---");

        // In town, can accept quest
        var player = CreatePlayer("player_001",
            health: 100, maxHealth: 100,
            mana: 100, maxMana: 100,
            stamina: 100, maxStamina: 100,
            position: (5, 5),
            target: null,
            inCombat: false,
            inDungeon: false,
            inTown: true,
            hasQuest: false);

        logger?.LogInformation("[Bot] Player in town, looking for quest");
        await ExecuteBotTurn(player, evaluator, actions, logger);

        // Quest accepted, move to objective
        player = player with
        {
            HasActiveQuest = true,
            QuestObjective = "ReachLocation",
            GoalPosition = (20, 20),
            IsInTown = false,
            IsInDungeon = true
        };
        logger?.LogInformation("[Bot] Quest accepted, moving to objective");
        await ExecuteBotTurn(player, evaluator, actions, logger);

        // At objective, can complete
        player = player with { Position = (20, 20) };
        logger?.LogInformation("[Bot] Reached objective, completing quest");
        await ExecuteBotTurn(player, evaluator, actions, logger);

        logger?.LogInformation("");
    }

    /// <summary>
    /// Test Scenario 4: Survival Situation
    /// Player critically injured, must flee or use resources
    /// </summary>
    private static async Task RunTestScenario4_Survival(
        ICapabilityEvaluator evaluator,
        CapabilityAction[] actions,
        ILogger? logger)
    {
        logger?.LogInformation("--- Scenario 4: Survival Mode ---");

        // Critically injured in combat
        var player = CreatePlayer("player_001",
            health: 20, maxHealth: 100, // 20% health!
            mana: 10, maxMana: 100,
            stamina: 30, maxStamina: 100,
            position: (10, 10),
            target: (11, 10),
            inCombat: true,
            inDungeon: true);

        player = player with
        {
            Inventory = player.Inventory with { HealthPotions = 1 }
        };

        logger?.LogInformation("[Bot] Player critically injured! Evaluating survival options...");
        await ExecuteBotTurn(player, evaluator, actions, logger);

        // After potion or flee
        player = player with
        {
            Health = 15, // Still low
            Stamina = 25,
            Inventory = player.Inventory with { HealthPotions = 0 }
        };
        logger?.LogInformation("[Bot] Still in danger, re-evaluating...");
        await ExecuteBotTurn(player, evaluator, actions, logger);

        logger?.LogInformation("");
    }

    /// <summary>
    /// Execute one bot turn: evaluate capabilities and take action
    /// </summary>
    private static async Task ExecuteBotTurn(
        PlayerContext player,
        ICapabilityEvaluator evaluator,
        CapabilityAction[] actions,
        ILogger? logger)
    {
        // Evaluate all capabilities
        var capabilities = await evaluator.EvaluateAllAsync("PlayerCapabilities", player);

        var available = capabilities.Where(c => c.CanPerform).ToArray();

        logger?.LogDebug("  Status: HP {HP}/{MaxHP}, Mana {MP}/{MaxMP}, Stamina {SP}/{MaxSP}, Pos {Pos}",
            player.Health, player.MaxHealth,
            player.Mana, player.MaxMana,
            player.Stamina, player.MaxStamina,
            player.Position);

        logger?.LogDebug("  Available: {Count} capabilities: {Caps}",
            available.Length,
            string.Join(", ", available.Select(c => c.CapabilityName).Take(5)));

        // Select and execute action (priority-based: lowest cost first)
        foreach (var action in actions.OrderBy(a => a.Cost))
        {
            if (await action.CanExecuteAsync(player))
            {
                await action.ExecuteAsync(player);
                break; // One action per turn
            }
        }
    }

    /// <summary>
    /// Create player context with default values
    /// </summary>
    private static PlayerContext CreatePlayer(
        string id,
        int health, int maxHealth,
        int mana, int maxMana,
        int stamina, int maxStamina,
        (int X, int Y) position,
        (int X, int Y)? target,
        bool inCombat,
        bool inDungeon,
        bool inTown = false,
        (int X, int Y)? goal = null,
        bool hasQuest = false,
        string? questObjective = null)
    {
        return new PlayerContext
        {
            EntityId = id,
            EntityType = "Player",
            Health = health,
            MaxHealth = maxHealth,
            Mana = mana,
            MaxMana = maxMana,
            Stamina = stamina,
            MaxStamina = maxStamina,
            Level = 5,
            Experience = 1000,
            Position = position,
            TargetPosition = target,
            GoalPosition = goal,
            Inventory = new PlayerInventory
            {
                HealthPotions = 0,
                ManaPotions = 0,
                Gold = 100,
                Keys = 1,
                HasMap = true,
                FoodItems = 5
            },
            Equipment = new PlayerEquipment
            {
                HasWeapon = true,
                HasArmor = true,
                HasShield = false,
                WeaponType = "Sword",
                WeaponDamage = 15,
                ArmorDefense = 10
            },
            IsInCombat = inCombat,
            HasTarget = target.HasValue,
            TargetType = target.HasValue ? "Enemy" : null,
            TargetHealth = target.HasValue ? 50 : null,
            TargetMaxHealth = target.HasValue ? 100 : null,
            IsInDungeon = inDungeon,
            IsInTown = inTown,
            IsSafe = !inCombat,
            HasActiveQuest = hasQuest,
            QuestObjective = questObjective
        };
    }

    /// <summary>
    /// Create all player actions for bot control
    /// </summary>
    private static CapabilityAction[] CreatePlayerActions(
        ICapabilityEvaluator evaluator,
        ILogger? logger)
    {
        var loggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;

        return new CapabilityAction[]
        {
            // Combat actions
            new PlayerMeleeAttackAction(evaluator,
                logger as ILogger<PlayerMeleeAttackAction> ?? loggerFactory.CreateLogger<PlayerMeleeAttackAction>()),
            new PlayerRangedAttackAction(evaluator,
                logger as ILogger<PlayerRangedAttackAction> ?? loggerFactory.CreateLogger<PlayerRangedAttackAction>()),
            new PlayerCastSpellAction(evaluator,
                logger as ILogger<PlayerCastSpellAction> ?? loggerFactory.CreateLogger<PlayerCastSpellAction>()),
            
            // Survival actions
            new PlayerUseHealthPotionAction(evaluator,
                logger as ILogger<PlayerUseHealthPotionAction> ?? loggerFactory.CreateLogger<PlayerUseHealthPotionAction>()),
            new PlayerFleeAction(evaluator,
                logger as ILogger<PlayerFleeAction> ?? loggerFactory.CreateLogger<PlayerFleeAction>()),
            new PlayerRestAction(evaluator,
                logger as ILogger<PlayerRestAction> ?? loggerFactory.CreateLogger<PlayerRestAction>()),
            
            // Movement actions
            new PlayerMoveToTargetAction(evaluator,
                logger as ILogger<PlayerMoveToTargetAction> ?? loggerFactory.CreateLogger<PlayerMoveToTargetAction>()),
            new PlayerMoveToGoalAction(evaluator,
                logger as ILogger<PlayerMoveToGoalAction> ?? loggerFactory.CreateLogger<PlayerMoveToGoalAction>()),
            new PlayerExploreAction(evaluator,
                logger as ILogger<PlayerExploreAction> ?? loggerFactory.CreateLogger<PlayerExploreAction>()),
            
            // Quest/interaction actions
            new PlayerInteractWithNPCAction(evaluator,
                logger as ILogger<PlayerInteractWithNPCAction> ?? loggerFactory.CreateLogger<PlayerInteractWithNPCAction>()),
            new PlayerAcceptQuestAction(evaluator,
                logger as ILogger<PlayerAcceptQuestAction> ?? loggerFactory.CreateLogger<PlayerAcceptQuestAction>()),
            new PlayerCompleteQuestAction(evaluator,
                logger as ILogger<PlayerCompleteQuestAction> ?? loggerFactory.CreateLogger<PlayerCompleteQuestAction>())
        };
    }

    /// <summary>
    /// Load player capabilities from embedded JSON
    /// </summary>
    private static Workflow[] LoadPlayerCapabilities(ILogger? logger)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "WingedBean.Gameplay.AI.Examples.player-capabilities.json";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                logger?.LogWarning("Could not load player capabilities from embedded resource");
                return Array.Empty<Workflow>();
            }

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var workflows = JsonSerializer.Deserialize<List<Workflow>>(json);

            if (workflows != null && workflows.Count > 0)
            {
                logger?.LogDebug("Loaded {Count} player capability workflow(s)", workflows.Count);
                return workflows.ToArray();
            }

            return Array.Empty<Workflow>();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to load player capabilities");
            return Array.Empty<Workflow>();
        }
    }
}
