---
id: RFC-0047
title: Enhanced Movement and Pathfinding System
status: Proposed
category: game-engine, ecs, gameplay, ai
created: 2025-10-14
updated: 2025-10-14
priority: P1
effort: 6-8 hours
---

# RFC-0047: Enhanced Movement and Pathfinding System

**Status:** Proposed  
**Date:** 2025-10-14  
**Author:** Development Team  
**Category:** game-engine, ecs, gameplay, ai  
**Priority:** HIGH (P1)  
**Estimated Effort:** 6-8 hours  
**Depends On:** 
- RFC-0007: Arch ECS Integration (Implemented)
- RFC-0043: Tile-Based Map System (In Progress)
- RFC-0045: Plate Library Integration (Implemented)
- RFC-0046: Turn-Based Game Loop (Proposed)
- Plate.ModernSatsuma (Available)

---

## Summary

Implement an **enhanced movement and pathfinding system** that integrates with the tile-based map (RFC-0043), turn-based game loop (RFC-0046), and Plate.ModernSatsuma graph library. This system provides intelligent pathfinding for AI entities, validates movement against map obstacles, supports diagonal movement, and calculates movement costs based on terrain.

---

## Motivation

### Current Problem

The current movement system is **extremely basic**:

```csharp
// AISystem.cs - Current approach
int dx = Math.Sign(playerPos.X - enemyPos.X);
int dy = Math.Sign(playerPos.Y - pos.Y);
enemyPos.X += dx;
enemyPos.Y += dy;
```

**Issues:**
1. **No Pathfinding** - Enemies walk into walls, get stuck in corners
2. **No Obstacle Avoidance** - Can't navigate around furniture, other entities
3. **Unrealistic Movement** - Straight-line only, no tactical positioning
4. **No Terrain Costs** - Walking through water same as floor
5. **No Diagonal Movement** - Limited to 4 directions
6. **No Distance Calculation** - Can't determine "closest enemy" accurately

### Why Enhanced Movement?

**Gameplay Benefits:**
- 🧠 **Smarter AI** - Enemies navigate around obstacles intelligently
- 🎯 **Tactical Depth** - Positioning matters (choke points, flanking)
- 🌊 **Terrain Variety** - Water slows movement, lava damages
- 📏 **Accurate Distances** - Spells, abilities use real distances

**Technical Benefits:**
- ✅ **Proven Algorithms** - A* is industry-standard, well-tested
- ✅ **Reusable** - Pathfinding used by AI, player auto-move, ranged attacks
- ✅ **Performant** - ModernSatsuma optimized for game use

### Success Criteria

✅ AI entities use A* pathfinding to navigate around obstacles  
✅ Movement validates against tile-based map (RFC-0043)  
✅ Diagonal movement supported (8-direction)  
✅ Terrain costs affect pathfinding (water = 2x cost)  
✅ Distance calculations use proper metrics  
✅ Performance: pathfinding < 5ms for typical dungeon

---

## Proposal

### 1. Component Definitions

#### 1.1 Pathfinding Component

```csharp
namespace WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// Pathfinding state for entities that navigate the dungeon.
/// </summary>
public struct Pathfinding
{
    /// <summary>Current path being followed</summary>
    public List<(int X, int Y)>? CurrentPath;
    
    /// <summary>Index of next position in path</summary>
    public int PathIndex;
    
    /// <summary>Target position for pathfinding</summary>
    public (int X, int Y)? TargetPosition;
    
    /// <summary>Recalculate path every N turns</summary>
    public int RecalculateInterval;
    
    /// <summary>Turns since last recalculation</summary>
    public int TurnsSinceRecalculation;
    
    /// <summary>Movement mode</summary>
    public MovementMode Mode;
}

public enum MovementMode
{
    Cardinal,    // 4-directional
    Diagonal,    // 8-directional
    Phasing,     // Can move through walls
    Aquatic      // Water is walkable
}
```

#### 1.2 MovementCost Component

```csharp
namespace WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// Movement cost multipliers for different terrain types.
/// </summary>
public struct MovementCost
{
    public float BaseCost;
    public float FloorCost;
    public float WaterCost;
    public float GrassCost;
    public float DiagonalCost;
    public bool CanFly;
    public bool CanSwim;
    public bool CanPhase;
    
    public static MovementCost Default => new()
    {
        BaseCost = 1.0f,
        FloorCost = 1.0f,
        WaterCost = 2.0f,
        GrassCost = 1.2f,
        DiagonalCost = 1.414f, // sqrt(2)
        CanFly = false,
        CanSwim = false,
        CanPhase = false
    };
}
```

---

### 2. Navigation Graph Service

```csharp
namespace WingedBean.Plugins.DungeonGame.Services;

using Satsuma;
using Satsuma.Algorithms.ShortestPath;
using WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// Navigation graph built from DungeonMap.
/// Uses ModernSatsuma for graph representation and pathfinding.
/// </summary>
public class NavigationGraph
{
    private readonly DungeonMap _map;
    private readonly CompleteGraph _graph;
    private readonly Dictionary<int, (int X, int Y)> _nodeToPosition;
    private readonly Dictionary<(int X, int Y), int> _positionToNode;
    private readonly Dictionary<Arc, float> _edgeWeights;
    
    public NavigationGraph(DungeonMap map)
    {
        _map = map;
        _nodeToPosition = new();
        _positionToNode = new();
        _edgeWeights = new();
        _graph = BuildGraph();
    }
    
    /// <summary>
    /// Find shortest path using A*.
    /// </summary>
    public List<(int X, int Y)>? FindPath(
        int startX, int startY, 
        int endX, int endY,
        MovementMode mode = MovementMode.Diagonal)
    {
        if (!_positionToNode.TryGetValue((startX, startY), out int startNode))
            return null;
        
        if (!_positionToNode.TryGetValue((endX, endY), out int endNode))
            return null;
        
        // Use A* from ModernSatsuma
        var astar = new AStar<int>(
            _graph,
            arc => _edgeWeights[arc],
            node => HeuristicDistance(_nodeToPosition[node], (endX, endY))
        );
        
        var pathNodes = astar.Run(startNode, endNode);
        
        if (pathNodes == null || !pathNodes.Any())
            return null;
        
        return pathNodes.Select(node => _nodeToPosition[node]).ToList();
    }
    
    /// <summary>
    /// Calculate distance between positions.
    /// </summary>
    public float CalculateDistance(
        int x1, int y1, 
        int x2, int y2, 
        DistanceMetric metric = DistanceMetric.Euclidean)
    {
        return metric switch
        {
            DistanceMetric.Manhattan => Math.Abs(x2 - x1) + Math.Abs(y2 - y1),
            DistanceMetric.Chebyshev => Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1)),
            DistanceMetric.Euclidean => MathF.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1)),
            _ => 0
        };
    }
    
    /// <summary>
    /// Find all positions within radius.
    /// </summary>
    public List<(int X, int Y)> FindPositionsInRadius(
        int centerX, int centerY, 
        float radius, 
        DistanceMetric metric = DistanceMetric.Euclidean)
    {
        var positions = new List<(int X, int Y)>();
        
        int minX = Math.Max(0, centerX - (int)Math.Ceiling(radius));
        int maxX = Math.Min(_map.Width - 1, centerX + (int)Math.Ceiling(radius));
        int minY = Math.Max(0, centerY - (int)Math.Ceiling(radius));
        int maxY = Math.Min(_map.Height - 1, centerY + (int)Math.Ceiling(radius));
        
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                float distance = CalculateDistance(centerX, centerY, x, y, metric);
                if (distance <= radius)
                {
                    positions.Add((x, y));
                }
            }
        }
        
        return positions;
    }
    
    private CompleteGraph BuildGraph()
    {
        var graph = new CompleteGraph(directed: false);
        int nodeId = 0;
        
        // Create nodes for walkable tiles
        for (int x = 0; x < _map.Width; x++)
        {
            for (int y = 0; y < _map.Height; y++)
            {
                var tile = _map.GetTile(x, y);
                if (tile == null || !tile.Value.IsWalkable)
                    continue;
                
                var node = graph.AddNode();
                _nodeToPosition[nodeId] = (x, y);
                _positionToNode[(x, y)] = nodeId;
                nodeId++;
            }
        }
        
        // Create edges between adjacent tiles
        foreach (var (nodeId1, pos1) in _nodeToPosition)
        {
            var neighbors = GetNeighbors(pos1.X, pos1.Y);
            
            foreach (var (nx, ny) in neighbors)
            {
                if (!_positionToNode.TryGetValue((nx, ny), out int nodeId2))
                    continue;
                
                float weight = CalculateMovementCost(pos1.X, pos1.Y, nx, ny);
                var arc = graph.AddArc(nodeId1, nodeId2);
                _edgeWeights[arc] = weight;
            }
        }
        
        return graph;
    }
    
    private List<(int X, int Y)> GetNeighbors(int x, int y)
    {
        var neighbors = new List<(int X, int Y)>();
        
        // 8 directions (cardinal + diagonal)
        var directions = new[] 
        { 
            (0, -1), (0, 1), (-1, 0), (1, 0),  // Cardinal
            (-1, -1), (-1, 1), (1, -1), (1, 1)  // Diagonal
        };
        
        foreach (var (dx, dy) in directions)
        {
            int nx = x + dx;
            int ny = y + dy;
            
            if (_map.IsInBounds(nx, ny))
            {
                var tile = _map.GetTile(nx, ny);
                if (tile != null && tile.Value.IsWalkable)
                {
                    neighbors.Add((nx, ny));
                }
            }
        }
        
        return neighbors;
    }
    
    private float CalculateMovementCost(int x1, int y1, int x2, int y2)
    {
        float cost = 1.0f;
        
        // Diagonal costs more
        if (x1 != x2 && y1 != y2)
            cost *= 1.414f;
        
        // Terrain cost
        var tile = _map.GetTile(x2, y2);
        if (tile != null)
        {
            cost *= tile.Value.Type switch
            {
                TileType.Floor => 1.0f,
                TileType.Water => 2.0f,
                TileType.Grass => 1.2f,
                TileType.Lava => 5.0f,
                _ => 1.0f
            };
        }
        
        return cost;
    }
    
    private float HeuristicDistance((int X, int Y) from, (int X, int Y) to)
    {
        int dx = to.X - from.X;
        int dy = to.Y - from.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
}

public enum DistanceMetric
{
    Euclidean,   // Straight-line
    Manhattan,   // Grid distance
    Chebyshev    // Chessboard distance
}
```

---

### 3. Pathfinding System

```csharp
namespace WingedBean.Plugins.DungeonGame.Systems;

using WingedBean.Plugins.ArchECS.Internal;
using WingedBean.Plugins.DungeonGame.Components;
using WingedBean.Plugins.DungeonGame.Services;

/// <summary>
/// System that handles pathfinding for entities.
/// </summary>
public class PathfindingSystem : IECSSystem
{
    private NavigationGraph? _navGraph;
    
    public void Execute(IWorld world, float deltaTime)
    {
        var map = world.GetSingleton<DungeonMap>();
        if (map == null)
            return;
        
        if (_navGraph == null)
            _navGraph = new NavigationGraph(map);
        
        // Process entities needing pathfinding
        var query = world.CreateQuery<Position, Pathfinding>();
        
        foreach (var entity in query)
        {
            ref var pos = ref world.GetComponent<Position>(entity);
            ref var pathfinding = ref world.GetComponent<Pathfinding>(entity);
            
            bool needsRecalculation = 
                pathfinding.CurrentPath == null ||
                pathfinding.TargetPosition == null ||
                (pathfinding.RecalculateInterval > 0 && 
                 pathfinding.TurnsSinceRecalculation >= pathfinding.RecalculateInterval);
            
            if (needsRecalculation && pathfinding.TargetPosition.HasValue)
            {
                var target = pathfinding.TargetPosition.Value;
                var path = _navGraph.FindPath(
                    pos.X, pos.Y, 
                    target.X, target.Y, 
                    pathfinding.Mode);
                
                if (path != null)
                {
                    pathfinding.CurrentPath = path;
                    pathfinding.PathIndex = 0;
                    pathfinding.TurnsSinceRecalculation = 0;
                }
            }
            
            pathfinding.TurnsSinceRecalculation++;
        }
    }
    
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
}
```

---

## Implementation Plan

### Phase 1: Navigation Graph (3 hours)
- [ ] Create `Services/NavigationGraph.cs`
- [ ] Integrate Plate.ModernSatsuma
- [ ] Implement A* pathfinding
- [ ] Unit tests

### Phase 2: Pathfinding System (2 hours)
- [ ] Create `Components/Pathfinding.cs`
- [ ] Create `PathfindingSystem.cs`
- [ ] Integration tests

### Phase 3: Movement Enhancement (2 hours)
- [ ] Update `MovementSystem.cs` with validation
- [ ] Add terrain effects
- [ ] Test with tile-based map

### Phase 4: AI Integration (1 hour)
- [ ] Update `AISystem.cs` to use pathfinding
- [ ] Performance profiling
- [ ] Documentation

---

## Testing Strategy

### Unit Tests
- Path finding between two points
- Obstacle avoidance
- Diagonal movement
- Terrain cost calculation
- Distance metrics

### Integration Tests
- AI navigates around walls
- Multiple entities pathfind simultaneously
- Path recalculation when target moves
- Performance with 50+ entities

---

## Performance Considerations

| Operation | Target | Typical |
|-----------|--------|---------|
| **Graph Build** | < 50ms | ~20ms |
| **A* Pathfind** | < 5ms | ~2ms |
| **Distance Query** | < 0.1ms | ~0.05ms |
| **Area Query** | < 1ms | ~0.5ms |

**Optimizations:**
- Cache navigation graph (rebuild only when map changes)
- Limit pathfinding distance (max 50 tiles)
- Use spatial hashing for entity queries

---

## Related RFCs

- **RFC-0007**: Arch ECS Integration ✅ Implemented
- **RFC-0043**: Tile-Based Map System ⏭️ In Progress
- **RFC-0045**: Plate Library Integration ✅ Implemented
- **RFC-0046**: Turn-Based Game Loop ⏭️ Proposed
- **RFC-0048**: Inventory System ⏭️ Proposed

---

## References

- Plate.ModernSatsuma: `/plate-projects/modern-satsuma`
- A* Algorithm: [Red Blob Games](https://www.redblobgames.com/pathfinding/a-star/)
- Roguelike Pathfinding: [RogueBasin](http://www.roguebasin.com/index.php?title=Pathfinding)

---

**Decision:** Proposed - Awaiting Review  
**Next Steps:**
1. Review with team
2. Prototype navigation graph
3. Test A* performance
4. Create implementation branch
