using MessagePipe;
using Microsoft.Extensions.Logging;
using Plate.PerceptionSensor.Algorithms;
using Plate.PerceptionSensor.Core;
using Plate.PerceptionSensor.Media;
using SadRogue.Primitives;
using WingedBean.Plugins.ArchECS.Internal;
using WingedBean.Plugins.DungeonGame.Adapters;
using WingedBean.Plugins.DungeonGame.Components;
using WingedBean.Plugins.DungeonGame.Events;
using PerceptionPoint = Plate.PerceptionSensor.Core.Point;
using SadPoint = SadRogue.Primitives.Point;

namespace WingedBean.Plugins.DungeonGame.Systems;

/// <summary>
/// Field of View system using Plate.PerceptionSensor's recursive shadowcasting.
/// Provides extensible perception framework for future multi-sensory AI.
/// </summary>
public class PerceptionSensorFOVSystem : IECSSystem
{
    private readonly ILogger<PerceptionSensorFOVSystem>? _logger;
    private readonly IPublisher<FOVCalculatedEvent>? _fovPublisher;
    private readonly PerceptionEngine _engine;
    private DungeonMap? _currentMap;

    /// <summary>
    /// Creates a new perception sensor FOV system.
    /// </summary>
    /// <param name="fovPublisher">Optional event publisher for FOV changes</param>
    /// <param name="logger">Optional logger</param>
    public PerceptionSensorFOVSystem(
        IPublisher<FOVCalculatedEvent>? fovPublisher = null,
        ILogger<PerceptionSensorFOVSystem>? logger = null)
    {
        _fovPublisher = fovPublisher;
        _logger = logger;

        // Initialize perception engine with shadowcasting algorithm
        var algorithmFactory = new SpatialAlgorithmFactory();
        _engine = new PerceptionEngine(algorithmFactory);
    }

    /// <summary>
    /// Set the current dungeon map for FOV calculations.
    /// </summary>
    public void SetMap(DungeonMap map)
    {
        _currentMap = map;
        _logger?.LogDebug("Map set for PerceptionSensor FOV system");
    }

    public void Execute(IWorld world, float deltaTime)
    {
        if (_currentMap == null)
        {
            _logger?.LogWarning("No map set for PerceptionSensor FOV system");
            return;
        }

        // Clear all tile visibility before recalculating
        for (var x = 0; x < _currentMap.Width; x++)
        {
            for (var y = 0; y < _currentMap.Height; y++)
            {
                var tile = _currentMap.Tiles[x, y];
                tile.IsVisible = false;
                _currentMap.Tiles[x, y] = tile;
            }
        }

        // Process all entities with FOV components
        var query = world.CreateQuery<Position, FOVComponent>();
        foreach (var entity in query)
        {
            ref var pos = ref world.GetComponent<Position>(entity);
            ref var fovComp = ref world.GetComponent<FOVComponent>(entity);

            // Skip if FOV doesn't need recalculation
            if (!fovComp.IsDirty)
                continue;

            // Create perception query for vision
            var perceptionQuery = new PerceptionQuery(
                origin: new PerceptionPoint(pos.X, pos.Y),
                range: fovComp.Range,
                medium: new LightMedium(attenuationRate: 0.05)
            );

            // Create filter from dungeon map
            var filter = new DungeonMapFilter(_currentMap);

            // Calculate FOV using PerceptionSensor
            var result = _engine.Calculate(perceptionQuery, filter);

            // Track performance
            _logger?.LogTrace(
                "FOV calculated: {TilesDetected} tiles, {Time}ms",
                result.DetectedPositions.Count,
                result.Metadata.CalculationTime.TotalMilliseconds
            );

            // Track visible and newly visible tiles for event
            var visibleTiles = new HashSet<SadPoint>();
            var newlyVisibleTiles = new HashSet<SadPoint>();

            // Apply visibility to map
            foreach (var detectedPos in result.DetectedPositions)
            {
                var sadPoint = new SadPoint(detectedPos.X, detectedPos.Y);
                visibleTiles.Add(sadPoint);

                // Get current tile
                var tile = _currentMap.Tiles[detectedPos.X, detectedPos.Y];

                // Check if this is newly visible
                if (!tile.IsExplored)
                {
                    newlyVisibleTiles.Add(sadPoint);
                }

                // Mark as visible and explored
                tile.IsVisible = true;
                tile.IsExplored = true;
                _currentMap.Tiles[detectedPos.X, detectedPos.Y] = tile;
            }

            // Mark FOV as calculated
            fovComp.IsDirty = false;

            // TODO: Publish FOV calculated event when EntityHandle.Id becomes public
            // Currently EntityHandle.Id is internal, so we cannot publish events with entity IDs
        }
    }
}
