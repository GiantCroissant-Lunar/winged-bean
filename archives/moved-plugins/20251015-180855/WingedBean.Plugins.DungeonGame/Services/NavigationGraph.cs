using Microsoft.Extensions.Logging;
using WingedBean.Plugins.DungeonGame.Components;
using WingedBean.Plugins.DungeonGame.Map;

namespace WingedBean.Plugins.DungeonGame.Services;

/// <summary>
/// Navigation graph built from DungeonMap.
/// Provides A* pathfinding and distance calculations.
/// </summary>
public class NavigationGraph
{
    private readonly DungeonMap _map;
    private readonly ILogger<NavigationGraph>? _logger;

    public NavigationGraph(DungeonMap map, ILogger<NavigationGraph>? logger = null)
    {
        _map = map;
        _logger = logger;
    }

    /// <summary>
    /// Find shortest path using A* algorithm.
    /// </summary>
    public List<(int X, int Y)>? FindPath(
        int startX, int startY,
        int endX, int endY,
        MovementMode mode = MovementMode.Diagonal,
        MovementCost? movementCost = null)
    {
        if (!_map.IsInBounds(startX, startY) || !_map.IsInBounds(endX, endY))
            return null;

        if (!_map.IsWalkable(endX, endY))
            return null;

        var cost = movementCost ?? MovementCost.Default;

        // A* data structures
        var openSet = new PriorityQueue<(int X, int Y), float>();
        var cameFrom = new Dictionary<(int X, int Y), (int X, int Y)>();
        var gScore = new Dictionary<(int X, int Y), float>();
        var fScore = new Dictionary<(int X, int Y), float>();

        var start = (startX, startY);
        var end = (endX, endY);

        gScore[start] = 0;
        fScore[start] = HeuristicDistance(start, end);
        openSet.Enqueue(start, fScore[start]);

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();

            if (current == end)
            {
                return ReconstructPath(cameFrom, current);
            }

            foreach (var neighbor in GetNeighbors(current.X, current.Y, mode, cost))
            {
                float tentativeGScore = gScore[current] +
                    CalculateMovementCost(current.X, current.Y, neighbor.X, neighbor.Y, cost);

                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = tentativeGScore + HeuristicDistance(neighbor, end);

                    openSet.Enqueue(neighbor, fScore[neighbor]);
                }
            }
        }

        // No path found
        return null;
    }

    /// <summary>
    /// Calculate distance between two positions.
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

    /// <summary>
    /// Check if there's a clear line of sight between two positions.
    /// Uses Bresenham's line algorithm.
    /// </summary>
    public bool HasLineOfSight(int x1, int y1, int x2, int y2)
    {
        int dx = Math.Abs(x2 - x1);
        int dy = Math.Abs(x2 - y1);
        int sx = x1 < x2 ? 1 : -1;
        int sy = y1 < y2 ? 1 : -1;
        int err = dx - dy;

        int x = x1;
        int y = y1;

        while (true)
        {
            // Check if current position blocks sight
            var tile = _map.GetTile(x, y);
            if (tile == null || !tile.Value.IsTransparent)
                return false;

            if (x == x2 && y == y2)
                return true;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }
    }

    private List<(int X, int Y)> GetNeighbors(int x, int y, MovementMode mode, MovementCost cost)
    {
        var neighbors = new List<(int X, int Y)>();

        // Cardinal directions
        var directions = new List<(int dx, int dy)>
        {
            (0, -1), (0, 1), (-1, 0), (1, 0)
        };

        // Add diagonals for diagonal movement
        if (mode == MovementMode.Diagonal)
        {
            directions.AddRange(new[] { (-1, -1), (-1, 1), (1, -1), (1, 1) });
        }

        foreach (var (dx, dy) in directions)
        {
            int nx = x + dx;
            int ny = y + dy;

            if (!_map.IsInBounds(nx, ny))
                continue;

            var tile = _map.GetTile(nx, ny);
            if (tile == null)
                continue;

            // Check walkability based on movement mode
            bool canMove = mode switch
            {
                MovementMode.Phasing => cost.CanPhase || tile.Value.IsWalkable,
                MovementMode.Aquatic => cost.CanSwim || tile.Value.IsWalkable,
                _ => tile.Value.IsWalkable
            };

            if (canMove)
            {
                neighbors.Add((nx, ny));
            }
        }

        return neighbors;
    }

    private float CalculateMovementCost(int x1, int y1, int x2, int y2, MovementCost cost)
    {
        float moveCost = cost.BaseCost;

        // Diagonal costs more
        if (x1 != x2 && y1 != y2)
            moveCost *= cost.DiagonalCost;

        // Terrain cost
        var tile = _map.GetTile(x2, y2);
        if (tile != null)
        {
            moveCost *= tile.Value.Type switch
            {
                TileType.Floor => cost.FloorCost,
                TileType.Water => cost.WaterCost,
                TileType.Grass => cost.GrassCost,
                TileType.Lava => cost.LavaCost,
                _ => 1.0f
            };
        }

        return moveCost;
    }

    private float HeuristicDistance((int X, int Y) from, (int X, int Y) to)
    {
        // Euclidean distance for A* heuristic
        int dx = to.X - from.X;
        int dy = to.Y - from.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private List<(int X, int Y)> ReconstructPath(
        Dictionary<(int X, int Y), (int X, int Y)> cameFrom,
        (int X, int Y) current)
    {
        var path = new List<(int X, int Y)> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }

        return path;
    }
}
