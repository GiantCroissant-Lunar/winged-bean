using GoRogue.FOV;
using GoRogue.Pathing;
using SadRogue.Primitives;

namespace WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// Field of View component using GoRogue's recursive shadowcasting algorithm.
/// </summary>
public struct FOVComponent
{
    /// <summary>
    /// GoRogue FOV calculator instance.
    /// </summary>
    public RecursiveShadowcastingFOV? FOV;
    
    /// <summary>
    /// Vision range in tiles.
    /// </summary>
    public int Range;
    
    /// <summary>
    /// FOV calculation mode (basic, ripple, etc.).
    /// </summary>
    public Distance DistanceCalculation;
    
    /// <summary>
    /// Whether FOV needs recalculation this frame.
    /// </summary>
    public bool IsDirty;
}

/// <summary>
/// Pathfinding component using GoRogue's A* algorithm.
/// </summary>
public struct PathfindingComponent
{
    /// <summary>
    /// GoRogue A* pathfinder instance.
    /// </summary>
    public AStar? Pathfinder;
    
    /// <summary>
    /// Current calculated path.
    /// </summary>
    public Path? CurrentPath;
    
    /// <summary>
    /// Current step index in the path.
    /// </summary>
    public int CurrentStep;
    
    /// <summary>
    /// Target position for pathfinding.
    /// </summary>
    public Point? TargetPosition;
    
    /// <summary>
    /// Time since last path recalculation (for throttling).
    /// </summary>
    public float TimeSinceLastCalculation;
    
    /// <summary>
    /// Minimum time between path recalculations in seconds.
    /// </summary>
    public float RecalculationInterval;
}

/// <summary>
/// Map awareness component - tracks what tiles the entity has explored.
/// </summary>
public struct MapMemoryComponent
{
    /// <summary>
    /// Set of explored tile positions.
    /// </summary>
    public HashSet<Point>? ExploredTiles;
    
    /// <summary>
    /// Set of currently visible tile positions.
    /// </summary>
    public HashSet<Point>? VisibleTiles;
}

/// <summary>
/// Light source component for dynamic lighting.
/// </summary>
public struct LightSourceComponent
{
    /// <summary>
    /// Light radius in tiles.
    /// </summary>
    public int Radius;
    
    /// <summary>
    /// Light intensity (0.0 to 1.0).
    /// </summary>
    public float Intensity;
    
    /// <summary>
    /// Whether the light is currently active.
    /// </summary>
    public bool IsActive;
    
    /// <summary>
    /// Light color (for rendering).
    /// </summary>
    public ConsoleColor Color;
}
