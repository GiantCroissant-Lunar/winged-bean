using Microsoft.Extensions.Logging;
using WingedBean.Plugins.ArchECS.Internal;
using WingedBean.Plugins.DungeonGame.Components;
using WingedBean.Plugins.DungeonGame.Services;

namespace WingedBean.Plugins.DungeonGame.Systems;

/// <summary>
/// System that handles pathfinding for entities.
/// Calculates and updates paths for entities with Pathfinding component.
/// </summary>
public class PathfindingSystem : IECSSystem
{
    private readonly ILogger<PathfindingSystem>? _logger;
    private NavigationGraph? _navGraph;
    
    public PathfindingSystem(ILogger<PathfindingSystem>? logger = null)
    {
        _logger = logger;
    }
    
    public void Execute(IWorld world, float deltaTime)
    {
        var map = world.GetSingleton<DungeonMap>();
        if (map == null)
        {
            _logger?.LogDebug("No DungeonMap found, skipping pathfinding");
            return;
        }
        
        // Initialize navigation graph if needed
        if (_navGraph == null)
        {
            _navGraph = new NavigationGraph(map, _logger);
            _logger?.LogInformation("NavigationGraph initialized for {Width}x{Height} map", 
                map.Width, map.Height);
        }
        
        // Process entities needing pathfinding
        var query = world.CreateQuery<Position, Pathfinding>();
        
        foreach (var entity in query)
        {
            ref var pos = ref world.GetComponent<Position>(entity);
            ref var pathfinding = ref world.GetComponent<Pathfinding>(entity);
            
            // Check if we need to recalculate path
            bool needsRecalculation = ShouldRecalculatePath(pathfinding);
            
            if (needsRecalculation && pathfinding.TargetPosition.HasValue)
            {
                RecalculatePath(world, entity, pos, ref pathfinding);
            }
            
            pathfinding.TurnsSinceRecalculation++;
        }
    }
    
    /// <summary>
    /// Get the next position in the entity's path.
    /// </summary>
    public (int X, int Y)? GetNextPosition(EntityHandle entity, IWorld world)
    {
        if (!world.HasComponent<Pathfinding>(entity))
            return null;
        
        var pathfinding = world.GetComponent<Pathfinding>(entity);
        
        if (pathfinding.CurrentPath == null || 
            pathfinding.PathIndex >= pathfinding.CurrentPath.Count)
            return null;
        
        return pathfinding.CurrentPath[pathfinding.PathIndex];
    }
    
    /// <summary>
    /// Advance to the next position in the path.
    /// </summary>
    public void AdvancePath(EntityHandle entity, IWorld world)
    {
        if (!world.HasComponent<Pathfinding>(entity))
            return;
        
        ref var pathfinding = ref world.GetComponent<Pathfinding>(entity);
        
        if (pathfinding.CurrentPath != null && 
            pathfinding.PathIndex < pathfinding.CurrentPath.Count)
        {
            pathfinding.PathIndex++;
        }
    }
    
    /// <summary>
    /// Set a new target for pathfinding.
    /// </summary>
    public void SetTarget(EntityHandle entity, IWorld world, int targetX, int targetY)
    {
        if (!world.HasComponent<Pathfinding>(entity))
            return;
        
        ref var pathfinding = ref world.GetComponent<Pathfinding>(entity);
        pathfinding.TargetPosition = (targetX, targetY);
        pathfinding.TurnsSinceRecalculation = int.MaxValue; // Force recalculation
    }
    
    /// <summary>
    /// Clear the current path.
    /// </summary>
    public void ClearPath(EntityHandle entity, IWorld world)
    {
        if (!world.HasComponent<Pathfinding>(entity))
            return;
        
        ref var pathfinding = ref world.GetComponent<Pathfinding>(entity);
        pathfinding.CurrentPath = null;
        pathfinding.PathIndex = 0;
        pathfinding.TargetPosition = null;
    }
    
    /// <summary>
    /// Check if entity has reached its destination.
    /// </summary>
    public bool HasReachedDestination(EntityHandle entity, IWorld world)
    {
        if (!world.HasComponent<Pathfinding>(entity))
            return true;
        
        var pathfinding = world.GetComponent<Pathfinding>(entity);
        return pathfinding.ReachedDestination;
    }
    
    /// <summary>
    /// Calculate distance between entity and target.
    /// </summary>
    public float? GetDistanceToTarget(EntityHandle entity, IWorld world, DistanceMetric metric = DistanceMetric.Euclidean)
    {
        if (_navGraph == null)
            return null;
        
        if (!world.HasComponent<Position>(entity) || !world.HasComponent<Pathfinding>(entity))
            return null;
        
        var pos = world.GetComponent<Position>(entity);
        var pathfinding = world.GetComponent<Pathfinding>(entity);
        
        if (!pathfinding.TargetPosition.HasValue)
            return null;
        
        var target = pathfinding.TargetPosition.Value;
        return _navGraph.CalculateDistance(pos.X, pos.Y, target.X, target.Y, metric);
    }
    
    private bool ShouldRecalculatePath(Pathfinding pathfinding)
    {
        // No path exists
        if (pathfinding.CurrentPath == null)
            return true;
        
        // No target
        if (!pathfinding.TargetPosition.HasValue)
            return false;
        
        // Reached end of path
        if (pathfinding.PathIndex >= pathfinding.CurrentPath.Count)
            return true;
        
        // Time to recalculate
        if (pathfinding.RecalculateInterval > 0 && 
            pathfinding.TurnsSinceRecalculation >= pathfinding.RecalculateInterval)
            return true;
        
        return false;
    }
    
    private void RecalculatePath(IWorld world, EntityHandle entity, Position pos, ref Pathfinding pathfinding)
    {
        if (_navGraph == null || !pathfinding.TargetPosition.HasValue)
            return;
        
        var target = pathfinding.TargetPosition.Value;
        
        // Get movement cost if entity has it
        MovementCost? movementCost = null;
        if (world.HasComponent<MovementCost>(entity))
        {
            movementCost = world.GetComponent<MovementCost>(entity);
        }
        
        var path = _navGraph.FindPath(
            pos.X, pos.Y, 
            target.X, target.Y, 
            pathfinding.Mode,
            movementCost);
        
        if (path != null && path.Count > 0)
        {
            pathfinding.CurrentPath = path;
            pathfinding.PathIndex = 0;
            pathfinding.TurnsSinceRecalculation = 0;
            
            _logger?.LogDebug("Path calculated for entity {Entity}: {Length} steps", 
                entity, path.Count);
        }
        else
        {
            _logger?.LogDebug("No path found for entity {Entity} from ({X},{Y}) to ({TX},{TY})", 
                entity, pos.X, pos.Y, target.X, target.Y);
            
            // Clear path if no route found
            pathfinding.CurrentPath = null;
            pathfinding.PathIndex = 0;
        }
    }
}
