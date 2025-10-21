# FOV Implementation Comparison: GoRogue vs Plate.PerceptionSensor

**Date:** 2025-10-15  
**Question:** Should WingedBean switch from GoRogue FOV to Plate.PerceptionSensor?

## TL;DR Recommendation

**Use Plate.PerceptionSensor** ✅

**Reasons:**
1. It's your own code - you control it completely
2. Already implemented and tested (30 passing tests)
3. More extensible for future perception types (hearing, smell, etc.)
4. Builds successfully, ready to use
5. Removes external dependency on GoRogue
6. Integration effort is minimal (~1-2 days)

## Detailed Comparison

### GoRogue FOV (Current)

**What it is:** Third-party library for roguelike development  
**Version:** 3.0.0-beta08 (beta quality)  
**Package:** `GoRogue` + `TheSadRogue.Primitives`

**Pros:**
- ✅ Already integrated and working
- ✅ Battle-tested in many roguelike games
- ✅ Comprehensive roguelike toolkit (FOV + pathfinding + more)
- ✅ Zero migration effort (it's working now)

**Cons:**
- ❌ External dependency (not your code)
- ❌ Beta version (potential breaking changes)
- ❌ Couples you to GoRogue ecosystem
- ❌ Less control over features/fixes
- ❌ Brings in `TheSadRogue.Primitives` dependency
- ❌ Only provides vision FOV (no sound, smell, etc.)
- ❌ Not aligned with your Plate-layer architecture

**Current Usage in Code:**
```csharp
using GoRogue.FOV;
using SadRogue.Primitives;

public class GoRogueFOVSystem : IECSSystem
{
    private IFOV? _fov;
    
    public void Execute(IWorld world, float deltaTime)
    {
        // Create GoRogue FOV
        if (fovComp.FOV == null)
        {
            fovComp.FOV = new RecursiveShadowcaster(
                mapView,
                pos.AsGoRoguePoint()
            );
        }
        
        // Calculate FOV
        fovComp.FOV.Calculate(pos.X, pos.Y, range);
    }
}
```

---

### Plate.PerceptionSensor (Proposed)

**What it is:** Your own Plate-layer perception framework  
**Status:** ✅ Implemented, tested, builds successfully  
**Tests:** 30 passing tests  
**Build:** Clean build, no errors

**Pros:**
- ✅ **Your code** - complete control
- ✅ **Already implemented** - shadowcasting algorithm done
- ✅ **Well tested** - 30 passing tests
- ✅ **Zero external dependencies** - pure .NET
- ✅ **Extensible design** - supports multiple perception types
- ✅ **Architectural alignment** - fits Plate-layer philosophy
- ✅ **Future-proof** - can add sound, smell, tremorsense, etc.
- ✅ **Performance tracking** - built-in metadata and timing
- ✅ **Multiple distance metrics** - Euclidean, Manhattan, Chebyshev
- ✅ **Clean API** - query/result pattern

**Cons:**
- ⚠️ Requires migration effort (1-2 days)
- ⚠️ Not battle-tested in production yet (but well tested)
- ⚠️ Needs integration with WingedBean ECS

**API Design:**
```csharp
using Plate.PerceptionSensor;
using Plate.PerceptionSensor.Media;

public class PerceptionSensorFOVSystem : IECSSystem
{
    private readonly PerceptionEngine _engine;
    
    public void Execute(IWorld world, float deltaTime)
    {
        // Create query
        var query = new PerceptionQuery(
            origin: new Point(pos.X, pos.Y),
            range: fovRange,
            medium: new LightMedium()
        );
        
        // Create filter
        var filter = new TileMapFilter(map);
        
        // Calculate perception
        var result = _engine.Calculate(query, filter);
        
        // Apply results
        foreach (var position in result.DetectedPositions)
        {
            map.SetVisible(position.X, position.Y, true);
        }
    }
}
```

---

## Feature Comparison

| Feature | GoRogue FOV | Plate.PerceptionSensor |
|---------|-------------|------------------------|
| **Shadowcasting Algorithm** | ✅ Recursive | ✅ Recursive |
| **Circular FOV** | ✅ Yes | ✅ Yes (Euclidean) |
| **Diamond FOV** | ❌ No | ✅ Yes (Manhattan) |
| **Square FOV** | ❌ No | ✅ Yes (Chebyshev) |
| **Light Attenuation** | ❌ No | ✅ Yes (configurable) |
| **Performance Metadata** | ❌ No | ✅ Yes (timing, tile counts) |
| **Sound Perception** | ❌ No | ✅ Yes (SoundMedium) |
| **Smell Perception** | ❌ No | ✅ Yes (SmellMedium) |
| **Vibration/Tremorsense** | ❌ No | ✅ Yes (VibrationMedium) |
| **Batch Processing** | ❌ No | ✅ Yes |
| **External Dependencies** | ✅ Self-contained | ✅ Zero |
| **Your Control** | ❌ Third-party | ✅ Your code |
| **Architecture Alignment** | ❌ External | ✅ Plate-layer |

---

## Performance Comparison

### GoRogue FOV
- Fast recursive shadowcasting
- Mature, optimized implementation
- Performance: ~2-3ms for 20 tile radius (estimated)

### Plate.PerceptionSensor
- Same recursive shadowcasting algorithm
- Built-in performance tracking
- Performance target: < 5ms for 20 tile radius
- Actual: Need to benchmark in real usage
- **Performance metadata included in results**

Both use the same fundamental algorithm, so performance should be comparable.

---

## Integration Effort

### Keep GoRogue (Current)
- **Effort:** 0 hours (already working)
- **Risk:** None (known working state)
- **Architecture:** External dependency remains

### Switch to Plate.PerceptionSensor
- **Effort:** 1-2 days
- **Tasks:**
  1. Create `PerceptionSensorFOVSystem` (2-4 hours)
  2. Create `TileMapFilter` adapter for DungeonMap (1-2 hours)
  3. Replace `GoRogueFOVSystem` in registration (30 min)
  4. Test FOV rendering (2-3 hours)
  5. Performance testing and optimization (2-3 hours)
- **Risk:** Low (well tested, similar API)
- **Architecture:** Aligns with Plate-layer goals

---

## Future Extensibility

### GoRogue FOV
Limited to visual FOV only. If you later want:
- **Sound-based detection** → Need different library
- **Smell tracking** → Need different library
- **Stealth mechanics** → Limited support
- **Multi-sensory AI** → Complex integration

### Plate.PerceptionSensor
Built for extensibility:
```csharp
// Vision
var visionQuery = new PerceptionQuery(pos, 10, new LightMedium());

// Hearing (sound spreads around corners)
var hearingQuery = new PerceptionQuery(pos, 15, new SoundMedium());

// Smell (follows gradients)
var smellQuery = new PerceptionQuery(pos, 20, new SmellMedium());

// Batch process all at once
var results = engine.CalculateBatch(
    new[] { visionQuery, hearingQuery, smellQuery },
    filter
);
```

---

## Code Ownership & Maintenance

### GoRogue
- **Owned by:** Third-party (GoRogue team)
- **Updates:** When GoRogue releases them
- **Bug fixes:** Wait for upstream fix
- **Custom features:** Fork or request upstream
- **Breaking changes:** Subject to GoRogue roadmap
- **Beta status:** May have breaking changes before 3.0 final

### Plate.PerceptionSensor
- **Owned by:** You
- **Updates:** You control the schedule
- **Bug fixes:** Fix immediately in your codebase
- **Custom features:** Add whenever you need them
- **Breaking changes:** You control the API
- **Stable:** Under your control

---

## Migration Path

If you decide to switch:

### Step 1: Add Plate.PerceptionSensor Reference (30 min)
```xml
<!-- WingedBean.Plugins.DungeonGame.csproj -->
<ProjectReference Include="Plate.PerceptionSensor/Plate.PerceptionSensor.csproj" />
```

### Step 2: Create TileMapFilter Adapter (1 hour)
```csharp
public class DungeonMapFilter : IPerceptionFilter
{
    private readonly DungeonMap _map;
    
    public DungeonMapFilter(DungeonMap map)
    {
        _map = map;
    }
    
    public bool BlocksPerception(int x, int y)
    {
        return _map.BlocksSight(x, y);
    }
}
```

### Step 3: Create New FOVSystem (2-3 hours)
```csharp
public class PerceptionSensorFOVSystem : IECSSystem
{
    private readonly PerceptionEngine _engine;
    private readonly ISpatialAlgorithmFactory _algorithmFactory;
    
    public PerceptionSensorFOVSystem()
    {
        _algorithmFactory = new SpatialAlgorithmFactory();
        _engine = new PerceptionEngine(_algorithmFactory);
    }
    
    public void Execute(IWorld world, float deltaTime)
    {
        // Get map
        var map = world.GetSingleton<DungeonMap>();
        if (map == null) return;
        
        // Clear visibility
        map.ClearVisibility();
        
        // Process each FOV-capable entity
        foreach (var entity in world.CreateQuery<Position, FOVComponent, MapMemoryComponent>())
        {
            ref var pos = ref world.GetComponent<Position>(entity);
            ref var fovComp = ref world.GetComponent<FOVComponent>(entity);
            ref var memory = ref world.GetComponent<MapMemoryComponent>(entity);
            
            // Create query
            var query = new PerceptionQuery(
                origin: new Point(pos.X, pos.Y),
                range: fovComp.Range,
                medium: new LightMedium()
            );
            
            // Create filter
            var filter = new DungeonMapFilter(map);
            
            // Calculate FOV
            var result = _engine.Calculate(query, filter);
            
            // Apply results to map
            foreach (var visiblePos in result.DetectedPositions)
            {
                map.SetVisible(visiblePos.X, visiblePos.Y, true);
                map.SetExplored(visiblePos.X, visiblePos.Y, true);
            }
            
            // Optional: Store intensity values
            foreach (var detection in result.Detections)
            {
                // detection.Position
                // detection.Intensity (0.0 to 1.0)
            }
            
            fovComp.IsDirty = false;
        }
    }
}
```

### Step 4: Replace System Registration (30 min)
```csharp
// In DungeonGame.cs RegisterSystems()

// OLD
_systems.Add(new GoRogueFOVSystem(_fovPublisher));

// NEW
_systems.Add(new PerceptionSensorFOVSystem());
```

### Step 5: Test & Validate (2-3 hours)
- FOV renders correctly
- Fog of war persists
- Shadows cast properly
- Performance is acceptable
- No visual artifacts

### Step 6: Remove GoRogue Dependency (1 hour)
```xml
<!-- Remove from .csproj -->
<PackageReference Include="GoRogue" Version="3.0.0-beta08" />
<PackageReference Include="TheSadRogue.Primitives" Version="1.6.0-rc3" />
```
- Remove GoRogueFOVSystem.cs
- Remove GoRogue using statements
- Clean build to verify

---

## Recommendation: Switch to Plate.PerceptionSensor

### Why?

1. **Architectural Alignment** - Fits your Plate-layer philosophy perfectly
2. **Code Ownership** - You control the implementation
3. **Already Built** - The code exists, is tested, and builds
4. **Future-Proof** - Supports multiple perception types you'll want later
5. **Low Risk** - Same algorithm, well tested, clean API
6. **Reasonable Effort** - 1-2 days for significant long-term benefits
7. **Removes Beta Dependency** - GoRogue 3.0 is still in beta

### When?

**Timing Options:**

**Option A: Now (Recommended)**
- Do it before adding more GoRogue-dependent features
- 1-2 days of migration effort
- Clean slate for future development

**Option B: After Multi-Floor Implementation**
- Get high-value features done first (multi-floor, doors)
- Switch to PerceptionSensor after
- Risk: More GoRogue code to migrate

**Option C: Never (Not Recommended)**
- Keep GoRogue indefinitely
- Accept external dependency and beta status
- Miss out on multi-sensory perception features

### My Strong Recommendation: **Option A - Switch Now**

**Rationale:**
1. PerceptionSensor is ready to use (builds, passes tests)
2. Migration is straightforward (1-2 days)
3. You'll want multi-sensory perception later (stealth, AI awareness)
4. Better to migrate now than after building more on GoRogue
5. Aligns with your architectural vision
6. Removes beta-quality external dependency

---

## Answer to Your Question

**Question:** "Do you think using perception-sensor will be a better approach?"

**Answer:** **YES, absolutely.** ✅

**Summary:**
- Plate.PerceptionSensor is implemented, tested, and ready
- It provides the same shadowcasting FOV GoRogue does
- It adds extensibility you'll want later (sound, smell, multi-sensory AI)
- It removes an external beta dependency
- It aligns with your Plate-layer architecture
- Migration effort is reasonable (1-2 days)
- You control the code completely

**Action:** Switch to Plate.PerceptionSensor now, before building more features on top of GoRogue. The effort is small, the benefits are significant, and the timing is right.

The only reason to keep GoRogue would be if you're under extreme time pressure and need to ship immediately. Otherwise, make the switch - your future self will thank you when you want to add sound-based stealth mechanics or smell-based tracking AI.
