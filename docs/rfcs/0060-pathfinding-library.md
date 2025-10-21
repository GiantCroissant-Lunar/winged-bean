# RFC-Plate-0001: Pathfinding Library

**Project Name:** pathfinding-astar (or pathfinding-route)
**Status:** Proposed
**Priority:** P1 - High
**Effort:** 1-2 weeks
**Layer:** Plate (Reusable Framework)
**Consumers:** WingedBean, future grid-based games

---

## Problem Statement

WingedBean needs A* pathfinding for intelligent enemy AI movement. Currently, enemies move directly toward the player and get stuck on walls. This is a common requirement for:
- Roguelike games
- Tactics games
- Tower defense games
- Any grid-based game with AI navigation

A reusable pathfinding library at the Plate layer benefits all future games.

---

## Proposed Solution

Create **Plate.Pathfinding** - A generic, zero-dependency pathfinding library for grid-based games.

### Features
- A* algorithm implementation
- Dijkstra algorithm (for cost-based pathfinding)
- Generic cost function support
- Heuristic function support (Manhattan, Euclidean, Chebyshev)
- Path smoothing (optional)
- Multiple movement modes (4-direction, 8-direction, custom)

---

## Architecture

```
pathfinding-astar/
├── dotnet/framework/
│   ├── src/
│   │   └── Plate.Pathfinding/
│   │       ├── Core/
│   │       │   ├── IPathfinder.cs
│   │       │   ├── PathfinderOptions.cs
│   │       │   ├── PathResult.cs
│   │       │   └── IGrid.cs
│   │       ├── Algorithms/
│   │       │   ├── AStarPathfinder.cs
│   │       │   ├── DijkstraPathfinder.cs
│   │       │   └── GreedyBestFirstPathfinder.cs
│   │       ├── Heuristics/
│   │       │   ├── ManhattanDistance.cs
│   │       │   ├── EuclideanDistance.cs
│   │       │   └── ChebyshevDistance.cs
│   │       ├── Smoothing/
│   │       │   └── PathSmoother.cs
│   │       └── Grid/
│   │           ├── Grid2D.cs
│   │           └── GridAdapter.cs
│   ├── tests/
│   │   └── Plate.Pathfinding.Tests/
│   └── examples/
│       └── Plate.Pathfinding.Example/
└── docs/
    └── README.md
```

---

## Data Structures

### Core Interfaces

```csharp
// Plate.Pathfinding/Core/IPathfinder.cs
namespace Plate.Pathfinding;

/// <summary>
/// Pathfinding algorithm interface
/// </summary>
public interface IPathfinder
{
    /// <summary>
    /// Find path from start to goal
    /// </summary>
    PathResult FindPath(Point start, Point goal, IGrid grid, PathfinderOptions? options = null);
}

// Plate.Pathfinding/Core/IGrid.cs
/// <summary>
/// Grid interface for pathfinding
/// </summary>
public interface IGrid
{
    /// <summary>
    /// Is this position walkable?
    /// </summary>
    bool IsWalkable(int x, int y);

    /// <summary>
    /// Get movement cost for this position (1.0 = normal)
    /// </summary>
    float GetMovementCost(int x, int y);

    /// <summary>
    /// Get neighbors of this position
    /// </summary>
    IEnumerable<Point> GetNeighbors(int x, int y);

    /// <summary>
    /// Grid width
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Grid height
    /// </summary>
    int Height { get; }
}

// Plate.Pathfinding/Core/PathResult.cs
/// <summary>
/// Result of pathfinding
/// </summary>
public class PathResult
{
    /// <summary>
    /// Was a path found?
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Path from start to goal (includes start and goal)
    /// </summary>
    public List<Point> Path { get; init; } = new();

    /// <summary>
    /// Total cost of path
    /// </summary>
    public float Cost { get; init; }

    /// <summary>
    /// Number of nodes explored
    /// </summary>
    public int NodesExplored { get; init; }

    /// <summary>
    /// Time taken to find path (milliseconds)
    /// </summary>
    public double TimeMs { get; init; }

    /// <summary>
    /// Create failed result
    /// </summary>
    public static PathResult Failed() => new PathResult { Success = false };
}

// Plate.Pathfinding/Core/PathfinderOptions.cs
/// <summary>
/// Options for pathfinding
/// </summary>
public class PathfinderOptions
{
    /// <summary>
    /// Movement mode (4-direction, 8-direction)
    /// </summary>
    public MovementMode MovementMode { get; set; } = MovementMode.EightDirection;

    /// <summary>
    /// Heuristic function
    /// </summary>
    public IHeuristic Heuristic { get; set; } = new ManhattanDistance();

    /// <summary>
    /// Maximum nodes to explore (prevents infinite loops)
    /// </summary>
    public int MaxNodesExplored { get; set; } = 10000;

    /// <summary>
    /// Diagonal movement cost multiplier (typically 1.414)
    /// </summary>
    public float DiagonalCost { get; set; } = 1.414f;

    /// <summary>
    /// Enable path smoothing?
    /// </summary>
    public bool SmoothPath { get; set; } = false;
}

public enum MovementMode
{
    FourDirection,   // Up, Down, Left, Right
    EightDirection,  // Includes diagonals
}
```

### A* Implementation

```csharp
// Plate.Pathfinding/Algorithms/AStarPathfinder.cs
public class AStarPathfinder : IPathfinder
{
    public PathResult FindPath(Point start, Point goal, IGrid grid, PathfinderOptions? options = null)
    {
        options ??= new PathfinderOptions();
        var startTime = DateTime.UtcNow;

        // Priority queue (open set) - ordered by f(n) = g(n) + h(n)
        var openSet = new PriorityQueue<Node, float>();
        var openSetLookup = new HashSet<Point>();

        // Closed set (already evaluated)
        var closedSet = new HashSet<Point>();

        // g(n) - cost from start to n
        var gScore = new Dictionary<Point, float>();

        // f(n) - estimated total cost (g(n) + h(n))
        var fScore = new Dictionary<Point, float>();

        // Parent pointers for path reconstruction
        var cameFrom = new Dictionary<Point, Point>();

        // Initialize start node
        var startNode = new Node { Position = start };
        gScore[start] = 0;
        fScore[start] = options.Heuristic.Calculate(start, goal);
        openSet.Enqueue(startNode, fScore[start]);
        openSetLookup.Add(start);

        int nodesExplored = 0;

        while (openSet.Count > 0 && nodesExplored < options.MaxNodesExplored)
        {
            // Get node with lowest f(n)
            var current = openSet.Dequeue();
            openSetLookup.Remove(current.Position);
            nodesExplored++;

            // Goal reached?
            if (current.Position == goal)
            {
                var path = ReconstructPath(cameFrom, current.Position);
                if (options.SmoothPath)
                {
                    path = PathSmoother.Smooth(path, grid);
                }

                return new PathResult
                {
                    Success = true,
                    Path = path,
                    Cost = gScore[current.Position],
                    NodesExplored = nodesExplored,
                    TimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }

            closedSet.Add(current.Position);

            // Explore neighbors
            foreach (var neighbor in GetNeighbors(current.Position, grid, options))
            {
                if (closedSet.Contains(neighbor)) continue;

                // Calculate tentative g(n)
                float movementCost = grid.GetMovementCost(neighbor.X, neighbor.Y);
                bool isDiagonal = IsDiagonal(current.Position, neighbor);
                float edgeCost = isDiagonal ? options.DiagonalCost * movementCost : movementCost;
                float tentativeG = gScore[current.Position] + edgeCost;

                // Is this a better path?
                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current.Position;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + options.Heuristic.Calculate(neighbor, goal);

                    if (!openSetLookup.Contains(neighbor))
                    {
                        var neighborNode = new Node { Position = neighbor };
                        openSet.Enqueue(neighborNode, fScore[neighbor]);
                        openSetLookup.Add(neighbor);
                    }
                }
            }
        }

        // No path found
        return new PathResult
        {
            Success = false,
            NodesExplored = nodesExplored,
            TimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds
        };
    }

    private List<Point> ReconstructPath(Dictionary<Point, Point> cameFrom, Point current)
    {
        var path = new List<Point> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }

        return path;
    }

    private IEnumerable<Point> GetNeighbors(Point position, IGrid grid, PathfinderOptions options)
    {
        var neighbors = new List<Point>();

        // 4-direction
        var directions = new[]
        {
            new Point(0, -1),  // Up
            new Point(0, 1),   // Down
            new Point(-1, 0),  // Left
            new Point(1, 0),   // Right
        };

        // 8-direction (add diagonals)
        if (options.MovementMode == MovementMode.EightDirection)
        {
            directions = directions.Concat(new[]
            {
                new Point(-1, -1), // Up-Left
                new Point(1, -1),  // Up-Right
                new Point(-1, 1),  // Down-Left
                new Point(1, 1),   // Down-Right
            }).ToArray();
        }

        foreach (var dir in directions)
        {
            var neighbor = new Point(position.X + dir.X, position.Y + dir.Y);

            if (grid.IsWalkable(neighbor.X, neighbor.Y))
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    private bool IsDiagonal(Point a, Point b)
    {
        return a.X != b.X && a.Y != b.Y;
    }

    private class Node
    {
        public Point Position { get; set; }
    }
}
```

### Heuristics

```csharp
// Plate.Pathfinding/Heuristics/IHeuristic.cs
public interface IHeuristic
{
    float Calculate(Point from, Point to);
}

// Plate.Pathfinding/Heuristics/ManhattanDistance.cs
public class ManhattanDistance : IHeuristic
{
    public float Calculate(Point from, Point to)
    {
        return Math.Abs(from.X - to.X) + Math.Abs(from.Y - to.Y);
    }
}

// Plate.Pathfinding/Heuristics/EuclideanDistance.cs
public class EuclideanDistance : IHeuristic
{
    public float Calculate(Point from, Point to)
    {
        float dx = from.X - to.X;
        float dy = from.Y - to.Y;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }
}

// Plate.Pathfinding/Heuristics/ChebyshevDistance.cs
public class ChebyshevDistance : IHeuristic
{
    public float Calculate(Point from, Point to)
    {
        return Math.Max(Math.Abs(from.X - to.X), Math.Abs(from.Y - to.Y));
    }
}
```

### Grid Adapter

```csharp
// Plate.Pathfinding/Grid/GridAdapter.cs
/// <summary>
/// Adapter for custom grid implementations
/// </summary>
public class GridAdapter : IGrid
{
    private readonly Func<int, int, bool> _isWalkable;
    private readonly Func<int, int, float> _getMovementCost;
    private readonly int _width;
    private readonly int _height;
    private readonly MovementMode _movementMode;

    public GridAdapter(
        int width,
        int height,
        Func<int, int, bool> isWalkable,
        Func<int, int, float>? getMovementCost = null,
        MovementMode movementMode = MovementMode.EightDirection)
    {
        _width = width;
        _height = height;
        _isWalkable = isWalkable;
        _getMovementCost = getMovementCost ?? ((x, y) => 1.0f);
        _movementMode = movementMode;
    }

    public bool IsWalkable(int x, int y)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height)
            return false;

        return _isWalkable(x, y);
    }

    public float GetMovementCost(int x, int y)
    {
        return _getMovementCost(x, y);
    }

    public IEnumerable<Point> GetNeighbors(int x, int y)
    {
        // Implemented by pathfinding algorithm
        throw new NotImplementedException("Use pathfinder to get neighbors");
    }

    public int Width => _width;
    public int Height => _height;
}
```

---

## Usage Example

```csharp
using Plate.Pathfinding;

// Create grid adapter from your game's tilemap
var grid = new GridAdapter(
    width: mapWidth,
    height: mapHeight,
    isWalkable: (x, y) => map.IsWalkable(x, y),
    getMovementCost: (x, y) => map.GetMovementCost(x, y)
);

// Create pathfinder
var pathfinder = new AStarPathfinder();

// Configure options
var options = new PathfinderOptions
{
    MovementMode = MovementMode.EightDirection,
    Heuristic = new ManhattanDistance(),
    DiagonalCost = 1.414f,
    SmoothPath = true
};

// Find path
var start = new Point(enemyX, enemyY);
var goal = new Point(playerX, playerY);
var result = pathfinder.FindPath(start, goal, grid, options);

if (result.Success)
{
    // Move enemy along path
    foreach (var step in result.Path)
    {
        MoveEnemyTo(step.X, step.Y);
    }

    Console.WriteLine($"Path found! Cost: {result.Cost}, Nodes: {result.NodesExplored}");
}
else
{
    Console.WriteLine("No path found!");
}
```

---

## Implementation Steps

### Step 1: Core Interfaces (1-2 days)
1. Create `IPathfinder`, `IGrid`, `PathResult`, `PathfinderOptions`
2. Create `Point` struct
3. Write interface documentation

### Step 2: A* Algorithm (3-4 days)
1. Implement `AStarPathfinder`
2. Implement priority queue (or use built-in)
3. Implement path reconstruction
4. Write unit tests

### Step 3: Heuristics (1 day)
1. Implement Manhattan, Euclidean, Chebyshev
2. Write tests

### Step 4: Grid Adapter (1-2 days)
1. Implement `GridAdapter`
2. Add 4-direction and 8-direction support
3. Write tests

### Step 5: Path Smoothing (1-2 days)
1. Implement `PathSmoother`
2. Line-of-sight path shortening
3. Write tests

### Step 6: Additional Algorithms (2-3 days, optional)
1. Implement Dijkstra
2. Implement Greedy Best-First
3. Write comparison tests

---

## Success Criteria

- ✅ A* algorithm correctly finds optimal path
- ✅ Handles 4-direction and 8-direction movement
- ✅ Supports custom movement costs
- ✅ Path smoothing reduces waypoints
- ✅ Performance: < 5ms for 50x50 grid, range 30
- ✅ Zero dependencies (pure .NET)
- ✅ 100% test coverage
- ✅ Well documented

---

## Performance Targets

| Grid Size | Path Length | Target Time |
|-----------|-------------|-------------|
| 50x50 | 20-30 tiles | < 2ms |
| 100x100 | 50-70 tiles | < 5ms |
| 200x200 | 100-150 tiles | < 20ms |

---

## Testing Strategy

### Unit Tests
- Straight paths (no obstacles)
- Paths around obstacles
- No path available
- Diagonal movement
- Custom movement costs
- Heuristic comparison

### Performance Tests
- Large grids (200x200)
- Complex obstacles
- Long paths
- Many pathfinding requests

---

## Dependencies

- **None** (pure .NET, zero dependencies)

---

## Consumers

- **WingedBean** (RFC-0005: Pathfinding System Integration)
- Future grid-based games

---

**Author:** WingedBean Team
**Date:** 2025-10-15
**Version:** 1.0
