# RFC-0058: Migrate from GoRogue FOV to Plate.PerceptionSensor

**Status:** Approved - Ready for Implementation  
**Priority:** P1 - High (Architectural Improvement)  
**Effort:** 1-2 days  
**Phase:** 1 - Foundation Enhancement  
**Dependencies:** None (Plate.PerceptionSensor already exists)

---

## Problem Statement

WingedBean currently uses GoRogue library for Field of View (FOV) calculations. This creates several issues:

1. **External Dependency:** GoRogue 3.0.0-beta08 is a third-party library in beta status
2. **Limited Control:** Cannot customize or fix bugs without forking
3. **Architectural Misalignment:** Contradicts Plate-layer philosophy of owning core systems
4. **Limited Extensibility:** GoRogue only provides visual FOV, not sound/smell/tremorsense
5. **Breaking Changes Risk:** Beta version may introduce breaking changes before 3.0 release

Meanwhile, **Plate.PerceptionSensor already exists**, is fully implemented, tested (30 passing tests), and provides the same shadowcasting algorithm plus extensibility for future perception types.

### Current State

**GoRogueFOVSystem:**
```csharp
using GoRogue.FOV;
using SadRogue.Primitives;

public class GoRogueFOVSystem : IECSSystem
{
    public void Execute(IWorld world, float deltaTime)
    {
        // Uses GoRogue.FOV.RecursiveShadowcaster
        // Limited to vision only
        // External dependency
    }
}
```

**Dependencies:**
```xml
<PackageReference Include="GoRogue" Version="3.0.0-beta08" />
<PackageReference Include="TheSadRogue.Primitives" Version="1.6.0-rc3" />
```

### Impact

- **Blocks:** Multi-sensory AI (sound detection, smell tracking)
- **Blocks:** Advanced stealth mechanics
- **Risks:** Beta library breaking changes
- **Prevents:** Full control over perception algorithms

---

## Proposed Solution

**Replace GoRogueFOVSystem with PerceptionSensorFOVSystem** that uses Plate.PerceptionSensor.

### Why Plate.PerceptionSensor?

✅ **Already Implemented** - Recursive shadowcasting algorithm complete  
✅ **Well Tested** - 30 passing unit tests  
✅ **Builds Successfully** - Clean build, ready to use  
✅ **Zero Dependencies** - Pure .NET, no external packages  
✅ **Your Code** - Complete control over features and fixes  
✅ **Extensible** - Supports vision, sound, smell, vibration perception  
✅ **Performance Tracking** - Built-in metadata and timing  
✅ **Multiple Distance Metrics** - Euclidean, Manhattan, Chebyshev  

---

## Architecture

### New System Structure

```
WingedBean.Plugins.DungeonGame/
├── Systems/
│   ├── PerceptionSensorFOVSystem.cs (NEW - replaces GoRogueFOVSystem)
│   └── GoRogueFOVSystem.cs (DELETE after migration)
├── Adapters/
│   └── DungeonMapFilter.cs (NEW - IPerceptionFilter implementation)
└── Components/
    └── FOVComponent.cs (KEEP - same component structure)
```

### Plate.PerceptionSensor Integration

```
Plate.PerceptionSensor (plate-projects/perception-sensor)
├── Core/
│   ├── PerceptionEngine.cs
│   ├── PerceptionQuery.cs
│   └── PerceptionResult.cs
├── Algorithms/
│   └── Shadowcasting/
│       └── RecursiveShadowcastingAlgorithm.cs
└── Media/
    ├── LightMedium.cs (for vision)
    ├── SoundMedium.cs (future: hearing)
    └── SmellMedium.cs (future: smell tracking)
```

---

## Implementation

### Step 1: Add Project Reference

**File:** `WingedBean.Plugins.DungeonGame/WingedBean.Plugins.DungeonGame.csproj`

```xml
<!-- Add -->
<ProjectReference Include="../../../../../../../../plate-projects/perception-sensor/dotnet/src/Plate.PerceptionSensor/Plate.PerceptionSensor.csproj" />
```

### Step 2: Create DungeonMapFilter Adapter

**File:** `WingedBean.Plugins.DungeonGame/Adapters/DungeonMapFilter.cs` (NEW)

```csharp
using Plate.PerceptionSensor.Core;
using WingedBean.Plugins.DungeonGame.Components;

namespace WingedBean.Plugins.DungeonGame.Adapters;

/// <summary>
/// Adapter that provides dungeon map transparency data to PerceptionSensor.
/// </summary>
public class DungeonMapFilter : IPerceptionFilter
{
    private readonly DungeonMap _map;

    /// <summary>
    /// Creates a new dungeon map filter.
    /// </summary>
    /// <param name="map">The dungeon map</param>
    public DungeonMapFilter(DungeonMap map)
    {
        _map = map ?? throw new ArgumentNullException(nameof(map));
    }

    /// <summary>
    /// Checks if the position completely blocks perception.
    /// </summary>
    public bool Blocks(Point position)
    {
        if (!IsInBounds(position))
            return true;

        return _map.BlocksSight(position.X, position.Y);
    }

    /// <summary>
    /// Gets attenuation factor at position (0.0 = transparent, 1.0 = opaque).
    /// </summary>
    public double GetAttenuation(Point position)
    {
        if (!IsInBounds(position))
            return 1.0; // Fully opaque

        return _map.BlocksSight(position.X, position.Y) ? 1.0 : 0.0;
    }

    /// <summary>
    /// Checks if position is within valid bounds.
    /// </summary>
    public bool IsInBounds(Point position)
    {
        return _map.IsInBounds(position.X, position.Y);
    }
}
```

### Step 3: Create PerceptionSensorFOVSystem

**File:** `WingedBean.Plugins.DungeonGame/Systems/PerceptionSensorFOVSystem.cs` (NEW)

```csharp
using Microsoft.Extensions.Logging;
using Plate.PerceptionSensor;
using Plate.PerceptionSensor.Algorithms;
using Plate.PerceptionSensor.Core;
using Plate.PerceptionSensor.Media;
using MessagePipe;
using WingedBean.Plugins.ArchECS.Internal;
using WingedBean.Plugins.DungeonGame.Adapters;
using WingedBean.Plugins.DungeonGame.Components;
using WingedBean.Plugins.DungeonGame.Data;
using WingedBean.Plugins.DungeonGame.Events;

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
        _currentMap.ClearVisibility();

        // Process all entities with FOV components
        foreach (var entity in world.CreateQuery<Position, FOVComponent, MapMemoryComponent>())
        {
            ref var pos = ref world.GetComponent<Position>(entity);
            ref var fovComp = ref world.GetComponent<FOVComponent>(entity);
            ref var memory = ref world.GetComponent<MapMemoryComponent>(entity);

            // Skip if FOV doesn't need recalculation
            if (!fovComp.IsDirty)
                continue;

            // Create perception query for vision
            var query = new PerceptionQuery(
                origin: new Point(pos.X, pos.Y),
                range: fovComp.Range,
                medium: new LightMedium(attenuationRate: 0.05)
            );

            // Create filter from dungeon map
            var filter = new DungeonMapFilter(_currentMap);

            // Calculate FOV using PerceptionSensor
            var result = _engine.Calculate(query, filter);

            // Track performance
            _logger?.LogTrace(
                "FOV calculated: {TilesDetected} tiles detected, {TilesProcessed} tiles processed, {Time}ms",
                result.Metadata.TilesDetected,
                result.Metadata.TilesProcessed,
                result.Metadata.CalculationTime.TotalMilliseconds
            );

            // Apply visibility to map
            foreach (var detectedPos in result.DetectedPositions)
            {
                _currentMap.SetVisible(detectedPos.X, detectedPos.Y, true);
                _currentMap.SetExplored(detectedPos.X, detectedPos.Y, true);
            }

            // Optional: Store light intensity values for rendering
            // foreach (var detection in result.Detections)
            // {
            //     // detection.Position
            //     // detection.Intensity (0.0 to 1.0 based on distance)
            // }

            // Mark FOV as calculated
            fovComp.IsDirty = false;
            fovComp.LastCalculationTime = DateTime.UtcNow;

            // Publish FOV calculated event
            _fovPublisher?.Publish(new FOVCalculatedEvent
            {
                EntityId = entity.Id,
                Position = (pos.X, pos.Y),
                Range = fovComp.Range,
                VisibleTileCount = result.DetectedPositions.Count
            });
        }
    }
}
```

### Step 4: Update System Registration

**File:** `WingedBean.Plugins.DungeonGame/DungeonGame.cs`

```csharp
private void RegisterSystems()
{
    // ... other systems ...

    // OLD - Remove this
    // _systems.Add(new GoRogueFOVSystem(_fovPublisher));

    // NEW - Add this
    _systems.Add(new PerceptionSensorFOVSystem(
        _fovPublisher,
        _registry.Get<ILogger<PerceptionSensorFOVSystem>>()
    ));

    // ... other systems ...
}

private void InitializeWorld()
{
    // ... existing code ...

    // Set map for FOV system
    var fovSystem = _systems.OfType<PerceptionSensorFOVSystem>().FirstOrDefault();
    if (fovSystem != null && _currentMap != null)
    {
        fovSystem.SetMap(_currentMap);
    }
}
```

### Step 5: Remove GoRogue Dependencies

**After testing and validating the new system works:**

**1. Remove files:**
```bash
rm WingedBean.Plugins.DungeonGame/Systems/GoRogueFOVSystem.cs
rm WingedBean.Plugins.DungeonGame/Systems/GoRoguePathfindingSystem.cs  # If also migrating pathfinding
```

**2. Remove package references:**
```xml
<!-- Remove from WingedBean.Plugins.DungeonGame.csproj -->
<PackageReference Include="GoRogue" Version="3.0.0-beta08" />
<PackageReference Include="TheSadRogue.Primitives" Version="1.6.0-rc3" />
```

**3. Remove using statements:**
```csharp
// Remove from any remaining files
using GoRogue.FOV;
using SadRogue.Primitives;
```

---

## Testing Strategy

### Unit Tests

Create `PerceptionSensorFOVSystemTests.cs`:

```csharp
[Fact]
public void Execute_CalculatesFOVCorrectly()
{
    // Arrange
    var world = CreateTestWorld();
    var map = DungeonMap.CreateTestMap();
    var system = new PerceptionSensorFOVSystem();
    system.SetMap(map);

    var player = world.CreateEntity();
    world.AddComponent(player, new Position { X = 10, Y = 10 });
    world.AddComponent(player, new FOVComponent { Range = 8, IsDirty = true });
    world.AddComponent(player, new MapMemoryComponent());

    // Act
    system.Execute(world, 0.016f);

    // Assert
    Assert.True(map.GetTile(10, 10).IsVisible); // Origin visible
    Assert.True(map.GetTile(11, 10).IsVisible); // Adjacent visible
    Assert.False(map.GetTile(25, 25).IsVisible); // Out of range not visible
}

[Fact]
public void Execute_RespectsSightBlocking()
{
    // Arrange
    var map = new DungeonMap(20, 20);
    // Create a wall at (12, 10)
    map.SetTile(12, 10, MapTile.Wall());
    
    var world = CreateTestWorld();
    var system = new PerceptionSensorFOVSystem();
    system.SetMap(map);

    var player = world.CreateEntity();
    world.AddComponent(player, new Position { X = 10, Y = 10 });
    world.AddComponent(player, new FOVComponent { Range = 8, IsDirty = true });
    world.AddComponent(player, new MapMemoryComponent());

    // Act
    system.Execute(world, 0.016f);

    // Assert
    Assert.True(map.GetTile(12, 10).IsVisible); // Wall itself visible
    Assert.False(map.GetTile(13, 10).IsVisible); // Behind wall not visible
}
```

### Integration Tests

1. **Visual Testing** - Run game and verify FOV renders correctly
2. **Fog of War** - Check that explored tiles persist
3. **Performance** - Verify < 5ms for 20 tile radius
4. **Shadow Casting** - Verify shadows cast correctly behind walls
5. **Edge Cases** - Test map boundaries, zero range, maximum range

### Performance Benchmarks

```csharp
[Fact]
public void Benchmark_FOVCalculation_Performance()
{
    var map = new DungeonMap(100, 100);
    var system = new PerceptionSensorFOVSystem();
    system.SetMap(map);

    var world = CreateTestWorld();
    var entity = CreateEntityWithFOV(world, 50, 50, range: 20);

    var stopwatch = Stopwatch.StartNew();
    for (int i = 0; i < 100; i++)
    {
        system.Execute(world, 0.016f);
    }
    stopwatch.Stop();

    var avgTime = stopwatch.ElapsedMilliseconds / 100.0;
    Assert.True(avgTime < 5.0, $"FOV calculation took {avgTime}ms (target: < 5ms)");
}
```

---

## Migration Checklist

- [ ] **Step 1:** Add Plate.PerceptionSensor project reference
- [ ] **Step 2:** Create `DungeonMapFilter` adapter
- [ ] **Step 3:** Create `PerceptionSensorFOVSystem`
- [ ] **Step 4:** Update system registration in `DungeonGame.cs`
- [ ] **Step 5:** Test FOV rendering in game
- [ ] **Step 6:** Test fog of war persistence
- [ ] **Step 7:** Verify shadow casting works correctly
- [ ] **Step 8:** Run performance benchmarks
- [ ] **Step 9:** Remove `GoRogueFOVSystem.cs`
- [ ] **Step 10:** Remove GoRogue package references
- [ ] **Step 11:** Remove GoRogue using statements
- [ ] **Step 12:** Clean build and verify no errors
- [ ] **Step 13:** Full gameplay test session
- [ ] **Step 14:** Update documentation

---

## Success Criteria

- ✅ FOV calculations produce identical results to GoRogue
- ✅ Fog of war functions correctly (explored vs visible)
- ✅ Shadows cast properly behind walls
- ✅ Performance: < 5ms for range 20 FOV
- ✅ No visual artifacts or rendering issues
- ✅ All tests passing
- ✅ GoRogue dependency removed
- ✅ Clean build with zero warnings

---

## Benefits

### Immediate Benefits

1. **Code Ownership** - Complete control over FOV implementation
2. **Architectural Alignment** - Fits Plate-layer philosophy
3. **No Beta Dependency** - Removes third-party beta library
4. **Performance Tracking** - Built-in metrics and timing data
5. **Better Debugging** - Can debug into your own code

### Future Benefits

1. **Multi-Sensory AI** - Add sound detection for stealth mechanics
2. **Smell Tracking** - AI can track player by scent
3. **Tremorsense** - Detect vibrations through ground
4. **Custom Perception** - Add any perception type you need
5. **No Breaking Changes** - You control the API

---

## Performance Targets

| Metric | Target | Current (GoRogue) | Expected (PerceptionSensor) |
|--------|--------|-------------------|----------------------------|
| FOV Range 10 | < 2ms | ~2ms | ~2ms |
| FOV Range 20 | < 5ms | ~3ms | ~5ms |
| FOV Range 30 | < 10ms | ~5ms | ~10ms |
| Memory per FOV | < 100KB | ~80KB | ~100KB |

---

## Risks & Mitigation

### Risk 1: Performance Degradation
- **Mitigation:** Run benchmarks before and after migration
- **Fallback:** Keep GoRogue system temporarily during testing

### Risk 2: Visual Artifacts
- **Mitigation:** Extensive visual testing across different map layouts
- **Fallback:** Compare side-by-side with GoRogue results

### Risk 3: Integration Issues
- **Mitigation:** Maintain same component structure (FOVComponent)
- **Fallback:** Adapter pattern allows easy switching back

---

## Future Enhancements (Post-Migration)

Once migration is complete, you can add:

### 1. Multi-Sensory Perception (RFC-0062)
```csharp
// Vision
var visionQuery = new PerceptionQuery(pos, 10, new LightMedium());

// Hearing (for stealth mechanics)
var hearingQuery = new PerceptionQuery(pos, 15, new SoundMedium());

// Smell tracking (for hunting AI)
var smellQuery = new PerceptionQuery(pos, 20, new SmellMedium());
```

### 2. Light Sources & Intensity
```csharp
// Use detection intensity for lighting effects
foreach (var detection in result.Detections)
{
    var brightness = detection.Intensity; // 0.0 to 1.0
    RenderTileWithBrightness(detection.Position, brightness);
}
```

### 3. Perception-Based AI
```csharp
// AI only knows about what it can perceive
var aiPerception = _engine.Calculate(aiVisionQuery, filter);
if (aiPerception.DetectedPositions.Contains(playerPos))
{
    // AI sees player - chase!
}
```

---

## Timeline

**Total Effort:** 1-2 days

- **Day 1 Morning:** Add reference, create adapter (3 hours)
- **Day 1 Afternoon:** Create new FOV system (3 hours)
- **Day 1 Evening:** Initial testing (2 hours)
- **Day 2 Morning:** Performance testing and optimization (3 hours)
- **Day 2 Afternoon:** Remove GoRogue, final validation (2 hours)
- **Buffer:** Additional testing and polish (2-3 hours)

---

## Decision

**Recommendation:** ✅ **APPROVE AND IMPLEMENT**

**Rationale:**
1. Plate.PerceptionSensor is production-ready (30 passing tests, clean build)
2. Removes beta-quality external dependency
3. Enables future multi-sensory perception features
4. Aligns with architectural goals
5. Reasonable migration effort (1-2 days)
6. Low risk (same algorithm, well-tested)

**Next Steps:**
1. Approve this RFC
2. Create feature branch `feature/migrate-perception-sensor`
3. Follow implementation steps
4. Test thoroughly
5. Merge when all success criteria met

---

**Author:** WingedBean Team  
**Date:** 2025-10-15  
**Version:** 1.0  
**Related RFCs:**
- Supersedes: RFC-0058 (Field of View System)
- Enables: Future multi-sensory perception RFC
