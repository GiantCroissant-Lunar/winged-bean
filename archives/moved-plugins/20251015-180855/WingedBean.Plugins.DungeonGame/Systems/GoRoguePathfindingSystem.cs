using GoRogue.Pathing;
using MessagePipe;
using SadRogue.Primitives;
using WingedBean.Plugins.ArchECS.Internal;
using WingedBean.Plugins.DungeonGame.Components;
using WingedBean.Plugins.DungeonGame.Events;

namespace WingedBean.Plugins.DungeonGame.Systems;

/// <summary>
/// Pathfinding system using GoRogue's A* algorithm.
/// Publishes path events via MessagePipe.
/// </summary>
public class GoRoguePathfindingSystem : IECSSystem
{
    private readonly IPublisher<PathCalculatedEvent>? _pathPublisher;
    private DungeonMap? _currentMap;

    public GoRoguePathfindingSystem(IPublisher<PathCalculatedEvent>? pathPublisher = null)
    {
        _pathPublisher = pathPublisher;
    }

    /// <summary>
    /// Set the current dungeon map for pathfinding.
    /// </summary>
    public void SetMap(DungeonMap map)
    {
        _currentMap = map;
    }

    public void Execute(IWorld world, float deltaTime)
    {
        if (_currentMap == null)
            return;

        // Process all entities with pathfinding components
        foreach (var entity in world.CreateQuery<Position, PathfindingComponent>())
        {
            ref var pos = ref world.GetComponent<Position>(entity);
            ref var pathComp = ref world.GetComponent<PathfindingComponent>(entity);

            // Skip if no target or throttle recalculation
            if (!pathComp.TargetPosition.HasValue)
                continue;

            pathComp.TimeSinceLastCalculation += deltaTime;
            if (pathComp.TimeSinceLastCalculation < pathComp.RecalculationInterval)
                continue;

            // Initialize pathfinder if needed
            if (pathComp.Pathfinder == null)
            {
                pathComp.Pathfinder = new AStar(new MapWalkabilityView(_currentMap), Distance.Chebyshev);
            }

            var start = new Point(pos.X, pos.Y);
            var goal = pathComp.TargetPosition.Value;

            // Calculate path using GoRogue A*
            var path = pathComp.Pathfinder.ShortestPath(start, goal);

            bool success = path != null && path.Length > 0;

            if (success)
            {
                pathComp.CurrentPath = path;
                pathComp.CurrentStep = 0;
            }
            else
            {
                pathComp.CurrentPath = null;
                pathComp.CurrentStep = 0;
            }

            pathComp.TimeSinceLastCalculation = 0f;

            // Publish path event via MessagePipe
            if (_pathPublisher != null)
            {
                var evt = new PathCalculatedEvent(
                    EntityId: entity.Id,
                    Start: start,
                    Goal: goal,
                    PathLength: path?.Length ?? 0,
                    Success: success
                );
                _pathPublisher.Publish(evt);
            }
        }
    }

    /// <summary>
    /// Follow the current path by moving to the next step.
    /// Call this from your AI or movement system.
    /// </summary>
    public static bool FollowPath(ref Position pos, ref PathfindingComponent pathComp)
    {
        if (pathComp.CurrentPath == null || pathComp.CurrentStep >= pathComp.CurrentPath.Length)
        {
            return false; // No path or path completed
        }

        var nextStep = pathComp.CurrentPath.GetStep(pathComp.CurrentStep);
        pos.X = nextStep.X;
        pos.Y = nextStep.Y;
        pathComp.CurrentStep++;

        return pathComp.CurrentStep < pathComp.CurrentPath.Length; // Returns true if more steps remain
    }

    /// <summary>
    /// Adapter to provide map walkability data to GoRogue pathfinding.
    /// </summary>
    private class MapWalkabilityView : IGridView<bool>
    {
        private readonly DungeonMap _map;

        public MapWalkabilityView(DungeonMap map)
        {
            _map = map;
        }

        public int Height => _map.Height;
        public int Width => _map.Width;

        public bool this[Point pos] => this[pos.X, pos.Y];

        public bool this[int x, int y]
        {
            get
            {
                if (!_map.IsInBounds(x, y))
                    return false;

                return _map.Tiles[x, y].IsWalkable;
            }
        }
    }
}
