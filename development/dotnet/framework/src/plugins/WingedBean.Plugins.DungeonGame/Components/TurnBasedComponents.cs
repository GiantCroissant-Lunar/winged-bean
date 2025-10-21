using WingedBean.Plugins.ArchECS.Internal;

namespace WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// Energy/action point component for turn-based gameplay.
/// Entities gain energy based on speed and act when energy >= threshold.
/// </summary>
public struct Energy
{
    /// <summary>Current energy accumulated</summary>
    public float Current;

    /// <summary>Energy threshold to take a turn (typically 100)</summary>
    public float Threshold;

    /// <summary>Energy gained per tick (derived from speed stat)</summary>
    public float GainRate;

    /// <summary>Is this entity currently taking their turn?</summary>
    public bool IsActive;

    /// <summary>Has this entity's turn been processed this cycle?</summary>
    public bool TurnProcessed;
}

/// <summary>
/// Pending action for an entity's turn.
/// Set by input handler (player) or AI system (enemies).
/// Consumed by action processing system.
/// </summary>
public struct TurnAction
{
    public ActionType Type;
    public int TargetX;
    public int TargetY;
    public EntityHandle? TargetEntity;
    public string? ItemId;
    public int EnergyCost;
}

public enum ActionType
{
    None,
    Move,
    MeleeAttack,
    RangedAttack,
    UseItem,
    PickupItem,
    DropItem,
    OpenDoor,
    Wait,
    Interact
}

/// <summary>
/// Global turn state and ordering.
/// Singleton component attached to world.
/// </summary>
public class TurnOrder
{
    /// <summary>Current turn number (increments each full cycle)</summary>
    public int TurnNumber { get; set; }

    /// <summary>Entity currently taking their turn</summary>
    public EntityHandle? ActiveEntity { get; set; }

    /// <summary>Queue of entities ready to act (energy >= threshold)</summary>
    public Queue<EntityHandle> ReadyQueue { get; set; } = new();

    /// <summary>Is the game waiting for player input?</summary>
    public bool WaitingForPlayerInput { get; set; }

    /// <summary>Game state (player turn, enemy turn, processing)</summary>
    public TurnState State { get; set; }
}

public enum TurnState
{
    PlayerTurn,
    EnemyTurn,
    ProcessingActions,
    GameOver
}
