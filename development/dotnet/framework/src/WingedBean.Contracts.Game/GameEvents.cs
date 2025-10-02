using System;

namespace WingedBean.Contracts.Game;

/// <summary>
/// Event published when an entity moves to a new position.
/// </summary>
public record EntityMovedEvent(Guid EntityId, Position OldPosition, Position NewPosition);

/// <summary>
/// Event published when combat occurs between entities.
/// </summary>
public record CombatEvent(
    Guid AttackerId,
    Guid DefenderId,
    int Damage,
    bool DefenderKilled
);

/// <summary>
/// Event published when the overall game state changes.
/// </summary>
public record GameStateChangedEvent(GameState OldState, GameState NewState);

/// <summary>
/// Event published when a new entity is spawned in the world.
/// </summary>
public record EntitySpawnedEvent(Guid EntityId, EntityType Type, Position Position);

/// <summary>
/// Event published when an entity dies and is removed from the world.
/// </summary>
public record EntityDiedEvent(Guid EntityId, EntityType Type);

/// <summary>
/// Event published when an entity collects an item.
/// </summary>
public record ItemCollectedEvent(Guid CollectorId, Guid ItemId);

/// <summary>
/// Event published when an entity levels up.
/// </summary>
public record LevelUpEvent(Guid EntityId, int NewLevel);

/// <summary>
/// Types of entities in the game world.
/// </summary>
public enum EntityType
{
    Player,
    Enemy,
    Item,
    Wall,
    Floor
}
