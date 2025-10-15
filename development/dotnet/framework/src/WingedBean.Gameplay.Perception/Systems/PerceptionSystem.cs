using Arch.Core;
using Plate.PerceptionSensor.Algorithms;
using Plate.PerceptionSensor.Core;
using Plate.PerceptionSensor.Media;
using WingedBean.Gameplay.Perception.Components;
using WingedBean.Gameplay.Perception.Configuration;

namespace WingedBean.Gameplay.Perception.Systems;

/// <summary>
/// Main perception system that calculates what entities perceive.
/// Data-driven: all perception types configured via JSON.
/// </summary>
public class PerceptionSystem
{
    private readonly World _world;
    private readonly PerceptionEngine _perceptionEngine;
    private readonly IPerceptionFilter _mapFilter;
    private readonly PerceptionConfiguration _config;
    private readonly IPerceptionMediumFactory _mediumFactory;
    
    /// <summary>
    /// Creates a new perception system.
    /// </summary>
    /// <param name="world">Arch ECS world</param>
    /// <param name="perceptionEngine">Perception calculation engine</param>
    /// <param name="mapFilter">Map filter for blocking</param>
    /// <param name="config">Perception configuration loaded from JSON</param>
    public PerceptionSystem(
        World world,
        PerceptionEngine perceptionEngine,
        IPerceptionFilter mapFilter,
        PerceptionConfiguration config)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _perceptionEngine = perceptionEngine ?? throw new ArgumentNullException(nameof(perceptionEngine));
        _mapFilter = mapFilter ?? throw new ArgumentNullException(nameof(mapFilter));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _mediumFactory = new StandardPerceptionMediumFactory();
    }

    /// <summary>
    /// Updates perception for all perceiving entities.
    /// Call this each frame or when needed.
    /// </summary>
    public void Update()
    {
        // Query: Entities that can perceive
        var perceiverQuery = new QueryDescription()
            .WithAll<PerceptionCapabilitiesComponent, PositionComponent>();

        _world.Query(in perceiverQuery, (Entity entity, 
            ref PerceptionCapabilitiesComponent capabilities,
            ref PositionComponent position) =>
        {
            // Get or add results component
            if (!_world.Has<PerceptionResultsComponent>(entity))
            {
                _world.Add(entity, new PerceptionResultsComponent());
            }

            ref var results = ref _world.Get<PerceptionResultsComponent>(entity);
            
            // Clear previous results
            results.DetectedEntities.Clear();
            results.DetectedPositionsByType.Clear();
            results.SignalStrengths.Clear();

            // Calculate perception for each perception type this entity has
            foreach (var perceptionTypeId in capabilities.PerceptionTypes)
            {
                if (!capabilities.Definitions.TryGetValue(perceptionTypeId, out var definition))
                    continue;

                // Create perception query using definition from JSON
                var query = CreatePerceptionQuery(position.Position, definition);
                
                // Calculate using PerceptionSensor library
                var perceptionResult = _perceptionEngine.Calculate(query, _mapFilter);

                // Store detected positions for this perception type
                var detectedPositions = new HashSet<Point>(perceptionResult.DetectedPositions);
                results.DetectedPositionsByType[perceptionTypeId] = detectedPositions;

                // Merge signal strengths (keep highest)
                foreach (var kvp in perceptionResult.SignalStrength)
                {
                    if (!results.SignalStrengths.ContainsKey(kvp.Key) || 
                        results.SignalStrengths[kvp.Key] < kvp.Value)
                    {
                        results.SignalStrengths[kvp.Key] = kvp.Value;
                    }
                }

                // Detect entities at perceived positions
                DetectEntitiesInPositions(
                    entity, 
                    perceptionTypeId, 
                    detectedPositions, 
                    ref results);
            }

            results.LastUpdate = DateTime.UtcNow;
        });
    }

    private IPerceptionQuery CreatePerceptionQuery(Point origin, PerceptionDefinition definition)
    {
        // Convert JSON definition to IPerceptionMedium
        var medium = CreateMediumFromDefinition(definition);
        
        return new PerceptionQuery(
            origin: origin,
            range: definition.DefaultRange,
            medium: medium
        );
    }

    private IPerceptionMedium CreateMediumFromDefinition(PerceptionDefinition definition)
    {
        // Use the medium factory with JSON parameters
        var parameters = new Dictionary<string, object>
        {
            ["attenuation_rate"] = definition.AttenuationRate,
            ["name"] = definition.Name
        };

        return _mediumFactory.Create(definition.PropagationMode, parameters);
    }

    private void DetectEntitiesInPositions(
        Entity perceiver,
        string perceptionTypeId,
        HashSet<Point> detectedPositions,
        ref PerceptionResultsComponent results)
    {
        // Query all perceivable entities
        var perceivableQuery = new QueryDescription()
            .WithAll<PerceivableComponent, PositionComponent>();

        // Create local copy to avoid ref parameter in closure
        var resultsCopy = results;

        _world.Query(in perceivableQuery, (Entity target,
            ref PerceivableComponent perceivable,
            ref PositionComponent targetPos) =>
        {
            // Skip self
            if (target == perceiver)
                return;

            // Check if position is detected
            if (!detectedPositions.Contains(targetPos.Position))
                return;

            // Check if this perception type can detect this entity
            if (!perceivable.PerceivableBy.Contains(perceptionTypeId))
                return;

            // Apply signature modifiers (future enhancement)
            // double signatureModifier = 1.0;
            // if (perceivable.SignatureModifiers?.TryGetValue(perceptionTypeId, out var modifier) == true)
            // {
            //     signatureModifier = modifier;
            // }

            // Add to detected entities
            resultsCopy.DetectedEntities.Add(target);
        });

        // Copy back
        results = resultsCopy;
    }
}
