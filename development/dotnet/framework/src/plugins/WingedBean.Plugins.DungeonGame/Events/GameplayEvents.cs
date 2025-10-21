using SadRogue.Primitives;
using WingedBean.Plugins.DungeonGame.Components;

namespace WingedBean.Plugins.DungeonGame.Events;

/// <summary>
/// Base event for all gameplay events.
/// </summary>
public abstract record GameplayEvent(DateTimeOffset Timestamp)
{
    public GameplayEvent() : this(DateTimeOffset.UtcNow) { }
}

// ============================================================================
// Entity Events
// ============================================================================

/// <summary>
/// Published when an entity moves from one position to another.
/// </summary>
public record EntityMovedEvent(
    int EntityId,
    Point OldPosition,
    Point NewPosition,
    int Floor
) : GameplayEvent;

/// <summary>
/// Published when an entity is spawned.
/// </summary>
public record EntitySpawnedEvent(
    int EntityId,
    Point Position,
    int Floor,
    string EntityType
) : GameplayEvent;

/// <summary>
/// Published when an entity is destroyed/killed.
/// </summary>
public record EntityDestroyedEvent(
    int EntityId,
    Point Position,
    int Floor,
    string Reason
) : GameplayEvent;

/// <summary>
/// Published when an entity takes damage.
/// </summary>
public record EntityDamagedEvent(
    int EntityId,
    int Damage,
    int RemainingHP,
    int? AttackerId
) : GameplayEvent;

// ============================================================================
// Map/Environment Events
// ============================================================================

/// <summary>
/// Published when a tile changes state.
/// </summary>
public record TileChangedEvent(
    Point Position,
    int Floor,
    Tile OldTile,
    Tile NewTile
) : GameplayEvent;

/// <summary>
/// Published when FOV is recalculated.
/// </summary>
public record FOVCalculatedEvent(
    int EntityId,
    HashSet<Point> VisibleTiles,
    HashSet<Point> NewlyVisibleTiles,
    int Floor
) : GameplayEvent;

/// <summary>
/// Published when a door is opened or closed.
/// </summary>
public record DoorStateChangedEvent(
    Point Position,
    int Floor,
    bool IsOpen,
    int? ActorEntityId
) : GameplayEvent;

/// <summary>
/// Published when a trap is triggered.
/// </summary>
public record TrapTriggeredEvent(
    Point Position,
    int Floor,
    TrapType TrapType,
    int VictimEntityId
) : GameplayEvent;

// ============================================================================
// Pathfinding Events
// ============================================================================

/// <summary>
/// Published when a path is calculated for an entity.
/// </summary>
public record PathCalculatedEvent(
    int EntityId,
    Point Start,
    Point Goal,
    int PathLength,
    bool Success
) : GameplayEvent;

/// <summary>
/// Published when an entity completes following a path.
/// </summary>
public record PathCompletedEvent(
    int EntityId,
    Point FinalPosition
) : GameplayEvent;

// ============================================================================
// Combat Events
// ============================================================================

/// <summary>
/// Published when combat occurs between two entities.
/// </summary>
public record CombatEvent(
    int AttackerId,
    int DefenderId,
    int DamageDealt,
    bool IsKillingBlow
) : GameplayEvent;

/// <summary>
/// Published when an entity dies.
/// </summary>
public record EntityDeathEvent(
    int EntityId,
    Point Position,
    int Floor,
    int? KillerId,
    int ExperienceAwarded
) : GameplayEvent;

// ============================================================================
// Player Events
// ============================================================================

/// <summary>
/// Published when the player moves.
/// </summary>
public record PlayerMovedEvent(
    Point OldPosition,
    Point NewPosition,
    int Floor
) : GameplayEvent;

/// <summary>
/// Published when player stats change.
/// </summary>
public record PlayerStatsChangedEvent(
    int HP,
    int MaxHP,
    int Mana,
    int MaxMana,
    int Level,
    int Experience
) : GameplayEvent;

/// <summary>
/// Published when the player levels up.
/// </summary>
public record PlayerLevelUpEvent(
    int NewLevel,
    int HPGain,
    int ManaGain
) : GameplayEvent;

// ============================================================================
// Item Events
// ============================================================================

/// <summary>
/// Published when an item is picked up.
/// </summary>
public record ItemPickedUpEvent(
    int ItemEntityId,
    int PickerEntityId,
    Point Position,
    string ItemType
) : GameplayEvent;

/// <summary>
/// Published when an item is dropped.
/// </summary>
public record ItemDroppedEvent(
    int ItemEntityId,
    int DropperEntityId,
    Point Position,
    string ItemType
) : GameplayEvent;

/// <summary>
/// Published when an item is used.
/// </summary>
public record ItemUsedEvent(
    int ItemEntityId,
    int UserEntityId,
    string ItemType,
    bool WasConsumed
) : GameplayEvent;

// ============================================================================
// Game State Events
// ============================================================================

/// <summary>
/// Published when a new floor is generated.
/// </summary>
public record FloorGeneratedEvent(
    int FloorNumber,
    int Width,
    int Height,
    string GenerationAlgorithm
) : GameplayEvent;

/// <summary>
/// Published when the player descends to a new floor.
/// </summary>
public record FloorChangedEvent(
    int OldFloor,
    int NewFloor,
    Point NewPlayerPosition
) : GameplayEvent;

/// <summary>
/// Published when the game state changes.
/// </summary>
public record GameStateChangedEvent(
    string OldState,
    string NewState
) : GameplayEvent;
