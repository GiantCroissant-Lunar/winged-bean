using GoRogue.FOV;
using MessagePipe;
using SadRogue.Primitives;
using WingedBean.Plugins.ArchECS.Internal;
using WingedBean.Plugins.DungeonGame.Components;
using WingedBean.Plugins.DungeonGame.Events;

namespace WingedBean.Plugins.DungeonGame.Systems;

/// <summary>
/// Field of View system using GoRogue's recursive shadowcasting algorithm.
/// Publishes FOV events via MessagePipe for UI updates.
/// </summary>
public class GoRogueFOVSystem : IECSSystem
{
    private readonly IPublisher<FOVCalculatedEvent>? _fovPublisher;
    private DungeonMap? _currentMap;

    public GoRogueFOVSystem(IPublisher<FOVCalculatedEvent>? fovPublisher = null)
    {
        _fovPublisher = fovPublisher;
    }

    /// <summary>
    /// Set the current dungeon map for FOV calculations.
    /// </summary>
    public void SetMap(DungeonMap map)
    {
        _currentMap = map;
    }

    public void Execute(IWorld world, float deltaTime)
    {
        if (_currentMap == null)
            return;

        // Process all entities with FOV components
        foreach (var entity in world.CreateQuery<Position, FOVComponent, MapMemoryComponent>())
        {
            ref var pos = ref world.GetComponent<Position>(entity);
            ref var fovComp = ref world.GetComponent<FOVComponent>(entity);
            ref var memory = ref world.GetComponent<MapMemoryComponent>(entity);

            // Skip if FOV doesn't need recalculation
            if (!fovComp.IsDirty)
                continue;

            // Initialize FOV if needed
            if (fovComp.FOV == null)
            {
                fovComp.FOV = new RecursiveShadowcastingFOV(new MapTransparencyView(_currentMap));
                fovComp.DistanceCalculation = Distance.Chebyshev;
            }

            // Store previous visible tiles
            var previouslyVisible = memory.VisibleTiles ?? new HashSet<Point>();
            
            // Calculate FOV using GoRogue
            fovComp.FOV.Calculate(pos.X, pos.Y, fovComp.Range, fovComp.DistanceCalculation);

            // Update visible tiles
            var newVisibleTiles = new HashSet<Point>();
            var newlyVisibleTiles = new HashSet<Point>();

            foreach (var fovPos in fovComp.FOV.CurrentFOV)
            {
                var point = new Point(fovPos.X, fovPos.Y);
                newVisibleTiles.Add(point);

                // Track newly visible tiles
                if (!previouslyVisible.Contains(point))
                {
                    newlyVisibleTiles.Add(point);
                }

                // Update tile visibility in map
                if (_currentMap.IsInBounds(point))
                {
                    _currentMap.Tiles[point.X, point.Y].IsVisible = true;
                    _currentMap.Tiles[point.X, point.Y].IsExplored = true;
                }
            }

            // Clear visibility for tiles no longer visible
            foreach (var prevPoint in previouslyVisible)
            {
                if (!newVisibleTiles.Contains(prevPoint) && _currentMap.IsInBounds(prevPoint))
                {
                    _currentMap.Tiles[prevPoint.X, prevPoint.Y].IsVisible = false;
                }
            }

            // Update memory component
            if (memory.ExploredTiles == null)
            {
                memory.ExploredTiles = new HashSet<Point>();
            }
            if (memory.VisibleTiles == null)
            {
                memory.VisibleTiles = new HashSet<Point>();
            }

            memory.VisibleTiles = newVisibleTiles;
            foreach (var tile in newVisibleTiles)
            {
                memory.ExploredTiles.Add(tile);
            }

            // Publish FOV event via MessagePipe
            if (_fovPublisher != null && world.HasComponent<Player>(entity))
            {
                var evt = new FOVCalculatedEvent(
                    EntityId: entity.Id,
                    VisibleTiles: newVisibleTiles,
                    NewlyVisibleTiles: newlyVisibleTiles,
                    Floor: pos.Floor
                );
                _fovPublisher.Publish(evt);
            }

            // Mark FOV as clean
            fovComp.IsDirty = false;
        }
    }

    /// <summary>
    /// Adapter to provide map transparency data to GoRogue FOV.
    /// </summary>
    private class MapTransparencyView : IGridView<bool>
    {
        private readonly DungeonMap _map;

        public MapTransparencyView(DungeonMap map)
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

                return _map.Tiles[x, y].IsTransparent;
            }
        }
    }
}
