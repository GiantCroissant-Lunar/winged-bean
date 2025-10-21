using Microsoft.Extensions.Logging;
using WingedBean.Plugins.ArchECS.Internal;
using WingedBean.Plugins.DungeonGame.Components;

namespace WingedBean.Plugins.DungeonGame.Systems;

/// <summary>
/// Core turn-based game loop system.
/// Manages energy accumulation, turn order, and action processing.
/// </summary>
public class TurnManagerSystem : IECSSystem
{
    private readonly ILogger<TurnManagerSystem>? _logger;

    public TurnManagerSystem(ILogger<TurnManagerSystem>? logger = null)
    {
        _logger = logger;
    }

    public void Execute(IWorld world, float deltaTime)
    {
        // Get or create turn order singleton
        var turnOrder = world.GetSingleton<TurnOrder>() ?? InitializeTurnOrder(world);

        switch (turnOrder.State)
        {
            case TurnState.PlayerTurn:
                ProcessPlayerTurn(world, turnOrder);
                break;

            case TurnState.EnemyTurn:
                ProcessEnemyTurn(world, turnOrder);
                break;

            case TurnState.ProcessingActions:
                ProcessActions(world, turnOrder);
                break;

            case TurnState.GameOver:
                // Do nothing, game ended
                break;
        }
    }

    private TurnOrder InitializeTurnOrder(IWorld world)
    {
        var turnOrder = new TurnOrder
        {
            TurnNumber = 1,
            State = TurnState.PlayerTurn
        };

        world.SetSingleton(turnOrder);

        // Initialize energy for all entities with Stats
        foreach (var entity in world.CreateQuery<Stats>())
        {
            if (!world.HasComponent<Energy>(entity))
            {
                var stats = world.GetComponent<Stats>(entity);
                world.AddComponent(entity, new Energy
                {
                    Current = 0,
                    Threshold = 100,
                    GainRate = CalculateEnergyGainRate(stats),
                    IsActive = false,
                    TurnProcessed = false
                });
            }
        }

        _logger?.LogInformation("Turn-based system initialized");
        return turnOrder;
    }

    private void ProcessPlayerTurn(IWorld world, TurnOrder turnOrder)
    {
        // Find player entity
        var playerQuery = world.CreateQuery<Player, Energy>();
        var playerEntity = playerQuery.FirstOrDefault();

        if (playerEntity == default)
        {
            _logger?.LogWarning("No player entity found");
            turnOrder.State = TurnState.GameOver;
            return;
        }

        ref var energy = ref world.GetComponent<Energy>(playerEntity);

        // Accumulate energy if below threshold
        if (energy.Current < energy.Threshold)
        {
            energy.Current += energy.GainRate;

            // Check if any enemies are ready to act first
            if (HasEnemiesReady(world))
            {
                turnOrder.State = TurnState.EnemyTurn;
                return;
            }

            return; // Still accumulating energy
        }

        // Player has enough energy for a turn
        // Check if player has set an action
        if (!world.HasComponent<TurnAction>(playerEntity))
        {
            // Wait for player input
            turnOrder.WaitingForPlayerInput = true;
            turnOrder.ActiveEntity = playerEntity;
            return;
        }

        var action = world.GetComponent<TurnAction>(playerEntity);

        if (action.Type == ActionType.None)
        {
            // Wait for player input
            turnOrder.WaitingForPlayerInput = true;
            turnOrder.ActiveEntity = playerEntity;
            return;
        }

        // Player has chosen an action
        turnOrder.WaitingForPlayerInput = false;
        energy.Current -= action.EnergyCost;
        energy.TurnProcessed = true;

        _logger?.LogDebug("Player action: {Action} (cost: {Cost})",
            action.Type, action.EnergyCost);

        // Process action immediately
        ProcessEntityAction(world, playerEntity, action);

        // Clear action
        world.RemoveComponent<TurnAction>(playerEntity);

        // Check for enemies ready to act
        turnOrder.State = HasEnemiesReady(world)
            ? TurnState.EnemyTurn
            : TurnState.PlayerTurn;
    }

    private void ProcessEnemyTurn(IWorld world, TurnOrder turnOrder)
    {
        // Find enemies with enough energy
        var enemyQuery = world.CreateQuery<Enemy, Energy>();

        foreach (var entity in enemyQuery)
        {
            ref var energy = ref world.GetComponent<Energy>(entity);

            if (energy.Current < energy.Threshold)
            {
                energy.Current += energy.GainRate;
                continue;
            }

            if (energy.TurnProcessed)
                continue; // Already acted this cycle

            // Enemy has a turn
            turnOrder.ActiveEntity = entity;

            // Check if AI has set action
            if (!world.HasComponent<TurnAction>(entity))
            {
                // AI hasn't decided yet, skip for now
                // AISystem should set TurnAction
                continue;
            }

            var action = world.GetComponent<TurnAction>(entity);

            if (action.Type == ActionType.None)
                continue; // AI still thinking

            // Process enemy action
            energy.Current -= action.EnergyCost;
            energy.TurnProcessed = true;

            _logger?.LogDebug("Enemy action: {Action} at ({X}, {Y})",
                action.Type, action.TargetX, action.TargetY);

            ProcessEntityAction(world, entity, action);

            // Clear action
            world.RemoveComponent<TurnAction>(entity);
        }

        // Check if all entities have acted
        if (AllEntitiesProcessed(world))
        {
            // Reset turn flags
            ResetTurnFlags(world);
            turnOrder.TurnNumber++;
            turnOrder.State = TurnState.PlayerTurn;

            _logger?.LogInformation("Turn {Turn} complete", turnOrder.TurnNumber);
        }
    }

    private void ProcessActions(IWorld world, TurnOrder turnOrder)
    {
        // Process any queued actions (future: animations, effects)
        turnOrder.State = TurnState.PlayerTurn;
    }

    private void ProcessEntityAction(IWorld world, EntityHandle entity, TurnAction action)
    {
        switch (action.Type)
        {
            case ActionType.Move:
                ProcessMoveAction(world, entity, action);
                break;

            case ActionType.MeleeAttack:
                ProcessMeleeAttack(world, entity, action);
                break;

            case ActionType.Wait:
                // Do nothing, energy already consumed
                break;

                // Other actions handled by specialized systems
        }
    }

    private void ProcessMoveAction(IWorld world, EntityHandle entity, TurnAction action)
    {
        if (!world.HasComponent<Position>(entity))
            return;

        ref var pos = ref world.GetComponent<Position>(entity);

        // TODO: Validate movement with map (RFC-0043 integration point)
        // For now, allow all movement within bounds
        var map = world.GetSingleton<DungeonMap>();
        if (map != null)
        {
            var tile = map.GetTile(action.TargetX, action.TargetY);
            if (tile == null || !tile.Value.IsWalkable)
            {
                _logger?.LogDebug("Movement blocked at ({X}, {Y})",
                    action.TargetX, action.TargetY);
                return;
            }
        }

        // Apply movement
        pos.X = action.TargetX;
        pos.Y = action.TargetY;

        _logger?.LogDebug("Entity {Entity} moved to ({X}, {Y})",
            entity, pos.X, pos.Y);
    }

    private void ProcessMeleeAttack(IWorld world, EntityHandle entity, TurnAction action)
    {
        if (!action.TargetEntity.HasValue)
            return;

        // Delegate to CombatSystem
        // (CombatSystem will handle damage calculation)
        _logger?.LogDebug("Melee attack from {Attacker} to {Target}",
            entity, action.TargetEntity.Value);
    }

    private bool HasEnemiesReady(IWorld world)
    {
        var enemyQuery = world.CreateQuery<Enemy, Energy>();

        foreach (var entity in enemyQuery)
        {
            var energy = world.GetComponent<Energy>(entity);
            if (energy.Current >= energy.Threshold && !energy.TurnProcessed)
                return true;
        }

        return false;
    }

    private bool AllEntitiesProcessed(IWorld world)
    {
        var entityQuery = world.CreateQuery<Energy>();

        foreach (var entity in entityQuery)
        {
            var energy = world.GetComponent<Energy>(entity);
            if (energy.Current >= energy.Threshold && !energy.TurnProcessed)
                return false;
        }

        return true;
    }

    private void ResetTurnFlags(IWorld world)
    {
        var entityQuery = world.CreateQuery<Energy>();

        foreach (var entity in entityQuery)
        {
            ref var energy = ref world.GetComponent<Energy>(entity);
            energy.TurnProcessed = false;
        }
    }

    private float CalculateEnergyGainRate(Stats stats)
    {
        // Base energy gain from Dexterity stat
        // Dex 10 = 10 energy/tick (10 ticks to reach 100)
        // Dex 20 = 20 energy/tick (5 ticks to reach 100)
        return Math.Max(1, stats.Dexterity);
    }
}
