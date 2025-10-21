namespace WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// Pathfinding state for entities that navigate the dungeon.
/// Stores current path, target, and recalculation settings.
/// </summary>
public class Pathfinding
{
    /// <summary>Current path being followed</summary>
    public List<(int X, int Y)>? CurrentPath { get; set; }

    /// <summary>Index of next position in path</summary>
    public int PathIndex { get; set; }

    /// <summary>Target position for pathfinding</summary>
    public (int X, int Y)? TargetPosition { get; set; }

    /// <summary>Recalculate path every N turns (0 = never recalculate)</summary>
    public int RecalculateInterval { get; set; }

    /// <summary>Turns since last recalculation</summary>
    public int TurnsSinceRecalculation { get; set; }

    /// <summary>Movement mode (4-dir, 8-dir, phasing, etc.)</summary>
    public MovementMode Mode { get; set; }

    /// <summary>Has the entity reached its destination?</summary>
    public bool ReachedDestination =>
        CurrentPath == null || PathIndex >= CurrentPath.Count;
}

/// <summary>
/// Movement mode for pathfinding
/// </summary>
public enum MovementMode
{
    /// <summary>4-directional movement (N, S, E, W)</summary>
    Cardinal,

    /// <summary>8-directional movement (includes diagonals)</summary>
    Diagonal,

    /// <summary>Can move through walls</summary>
    Phasing,

    /// <summary>Water is walkable</summary>
    Aquatic
}

/// <summary>
/// Movement cost multipliers for different terrain types.
/// Attached to entities to customize their movement behavior.
/// </summary>
public struct MovementCost
{
    public float BaseCost { get; set; }
    public float FloorCost { get; set; }
    public float WaterCost { get; set; }
    public float GrassCost { get; set; }
    public float LavaCost { get; set; }
    public float DiagonalCost { get; set; }
    public bool CanFly { get; set; }
    public bool CanSwim { get; set; }
    public bool CanPhase { get; set; }

    /// <summary>Default movement costs for normal entities</summary>
    public static MovementCost Default => new()
    {
        BaseCost = 1.0f,
        FloorCost = 1.0f,
        WaterCost = 2.0f,
        GrassCost = 1.2f,
        LavaCost = 5.0f,
        DiagonalCost = 1.414f, // sqrt(2)
        CanFly = false,
        CanSwim = false,
        CanPhase = false
    };

    /// <summary>Flying creatures ignore terrain</summary>
    public static MovementCost Flying => new()
    {
        BaseCost = 1.0f,
        FloorCost = 1.0f,
        WaterCost = 1.0f,
        GrassCost = 1.0f,
        LavaCost = 1.0f,
        DiagonalCost = 1.414f,
        CanFly = true,
        CanSwim = true,
        CanPhase = false
    };

    /// <summary>Aquatic creatures prefer water</summary>
    public static MovementCost Aquatic => new()
    {
        BaseCost = 1.0f,
        FloorCost = 2.0f,
        WaterCost = 1.0f,
        GrassCost = 1.5f,
        LavaCost = 10.0f,
        DiagonalCost = 1.414f,
        CanFly = false,
        CanSwim = true,
        CanPhase = false
    };

    /// <summary>Ghosts can phase through walls</summary>
    public static MovementCost Phasing => new()
    {
        BaseCost = 1.0f,
        FloorCost = 1.0f,
        WaterCost = 1.0f,
        GrassCost = 1.0f,
        LavaCost = 1.0f,
        DiagonalCost = 1.414f,
        CanFly = true,
        CanSwim = true,
        CanPhase = true
    };
}

/// <summary>
/// Distance metric for pathfinding and range calculations
/// </summary>
public enum DistanceMetric
{
    /// <summary>Straight-line distance (sqrt(dx² + dy²))</summary>
    Euclidean,

    /// <summary>Grid distance (|dx| + |dy|)</summary>
    Manhattan,

    /// <summary>Chessboard distance (max(|dx|, |dy|))</summary>
    Chebyshev
}
