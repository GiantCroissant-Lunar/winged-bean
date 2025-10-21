# WingedBean Implementation Guide

**Date:** 2025-10-15
**Purpose:** Guide for agents implementing missing features

---

## Quick Start

1. **Read These First:**
   - [CURRENT_STATE.md](CURRENT_STATE.md) - What exists
   - [GAP_ANALYSIS.md](GAP_ANALYSIS.md) - What's missing
   - [rfcs/INDEX.md](rfcs/INDEX.md) - RFCs to implement

2. **Start Here:**
   - Phase 1: Foundation (RFCs 0001-0004) - **CRITICAL**
   - All Phase 1 RFCs are blocking and must be completed first

3. **Follow This Order:**
   - RFC-0001: Environment/Tilemap (foundational, no dependencies)
   - RFC-0002: Camera Integration (depends on RFC-0001)
   - RFC-0003: FOV System (depends on RFC-0001, RFC-0002)
   - RFC-0004: Dungeon Generation Integration (depends on RFC-0001)

---

## RFC Implementation Checklist

For each RFC:

### ☐ Before Starting
- [ ] Read RFC completely
- [ ] Check dependencies (all dependency RFCs implemented?)
- [ ] If Plate layer required, implement Plate project first
- [ ] Verify environment setup (build tools, dependencies)

### ☐ During Implementation
- [ ] Follow RFC data structures exactly
- [ ] Implement in the order specified in "Implementation Steps"
- [ ] Write unit tests as you go (TDD approach)
- [ ] Follow workspace rules (`.agent/local/overrides.md`)
- [ ] Use Task build system (`task build-dotnet`)

### ☐ After Implementation
- [ ] Run all tests (`task test`)
- [ ] Verify success criteria met (from RFC)
- [ ] Check performance targets met (from RFC)
- [ ] Run build from artifacts (`task build-dotnet`)
- [ ] Test game with changes (`./Dungeon.Host.Console`)
- [ ] Update RFC status to "Implemented"
- [ ] Update INDEX.md to mark RFC complete

---

## Build & Test Workflow

### Standard Workflow

```bash
# Navigate to winged-bean build directory
cd /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/build

# Build to artifacts
task build-dotnet

# Run tests
task test

# Run console host from artifacts
cd _artifacts/latest/dotnet/console/bin
./Dungeon.Host.Console

# Run with recording (30 seconds)
./Dungeon.Host.Console --record --record-duration=30 --record-session=test
```

### Plate Project Workflow

When implementing Plate layer projects (e.g., camera-bioca, perception-sensor):

```bash
# Navigate to plate project
cd /Users/apprenticegc/Work/lunar-horse/personal-work/plate-projects/{project-name}

# Check for build system
ls build/Taskfile.yml

# If Task build exists:
cd build
task build

# If standard .NET:
cd dotnet/framework
dotnet build
dotnet test

# Then integrate into WingedBean (Yokan layer)
```

---

## Phase 1: Foundation Implementation

**CRITICAL:** These RFCs block all other work. Implement in order.

### Week 1-2: RFC-0001 Environment/Tilemap

**Effort:** 2-3 weeks
**Blocking:** Everything

**Steps:**
1. Create `WingedBean.Gameplay.Environment` project
2. Implement data structures (Tile, DungeonMap, Door, Trap, etc.)
3. Implement MapService (IMapService)
4. Implement EnvironmentSystem, DoorSystem, TrapSystem, HazardSystem
5. Update MovementSystem with collision detection
6. Update RenderSystem to render tiles
7. Write comprehensive tests

**Success:** Entities collide with walls, doors open/close, traps trigger

---

### Week 3: RFC-0002 Camera Integration

**Effort:** 1 week
**Depends:** RFC-0001

**Steps:**
1. **Implement camera-bioca (Plate layer)**
   - Create `Plate.Camera2D` project
   - Implement Camera, CameraController, ViewportBounds
   - Write unit tests (100% coverage)

2. **Integrate into WingedBean (Yokan layer)**
   - Create `WingedBean.Gameplay.Camera` project
   - Add CameraComponent, CameraSystem
   - Create PositionCameraTarget adapter

3. **Update RenderSystem**
   - Remove fixed viewport
   - Use CameraController for bounds
   - Transform world → viewport coordinates

4. **Initialize in DungeonGame**
   - Create camera entity
   - Set follow target to player
   - Configure camera mode

**Success:** Camera follows player, large maps work, viewport culling functional

---

### Week 4-6: RFC-0003 FOV System

**Effort:** 2-3 weeks
**Depends:** RFC-0001, RFC-0002

**Steps:**
1. **Implement perception-sensor (Plate layer)**
   - Create `Plate.PerceptionSensor` project
   - Implement core interfaces (IPerceptionEngine, etc.)
   - Implement ShadowcastingAlgorithm
   - Write unit tests and performance tests

2. **Extend WingedBean.Gameplay.Perception**
   - Add Vision, LightSource, Visibility components
   - Implement FOVSystem
   - Write integration tests

3. **Update Existing Systems**
   - RenderSystem: fog of war, visibility culling
   - AISystem: use visibility for detection
   - Add Vision to player, Visibility to entities

4. **Test & Polish**
   - Test fog of war persistence
   - Test light sources
   - Performance profiling

**Success:** FOV working, fog of war functional, AI uses visibility

---

### Week 7-8: RFC-0004 Dungeon Generation Integration

**Effort:** 2 weeks
**Depends:** RFC-0001

**Steps:**
1. Create DungeonGenerationSystem
2. Integrate edg-rogqd GraphBasedGeneratorGrid2D
3. Convert edg-rogqd layouts to WingedBean tile format
4. Add spawn point placement (player, stairs)
5. Test dungeon generation
6. Add floor progression logic

**Success:** Procedurally generated dungeons, player spawns correctly, stairs work

---

## Phase 2-5: See RFCs

After Phase 1 is complete:
- **Phase 2:** Core Gameplay (Pathfinding, Spawn, Inventory, Loot)
- **Phase 3:** Combat & AI (Advanced Combat, Status Effects, Enhanced AI)
- **Phase 4:** Progression & Content (XP, Quests, Save/Load)
- **Phase 5:** Polish (Dialogue, Minimap, VFX, Audio)

---

## Integration Points - Critical

### MovementSystem Integration

**BEFORE (Current):**
```csharp
// No collision detection
position.X += velocity.DX;
position.Y += velocity.DY;
```

**AFTER (RFC-0001):**
```csharp
int newX = position.X + velocity.DX;
int newY = position.Y + velocity.DY;

if (_mapService.IsWalkable(position.Floor, newX, newY))
{
    var tile = _mapService.GetTile(position.Floor, newX, newY);
    if (tile.Entity == null || !EntityBlocksMovement(tile.Entity.Value))
    {
        position.X = newX;
        position.Y = newY;
    }
}
```

---

### RenderSystem Integration

**BEFORE (Current):**
```csharp
// Fixed viewport, no tiles
for (int y = 0; y < VIEWPORT_HEIGHT; y++)
{
    for (int x = 0; x < VIEWPORT_WIDTH; x++)
    {
        // Render entity at (x, y)
    }
}
```

**AFTER (RFC-0001 + RFC-0002 + RFC-0003):**
```csharp
// 1. Get camera and calculate viewport bounds
var camera = GetCamera(world);
var bounds = _cameraController.CalculateViewportBounds(camera);

// 2. Render tiles with fog of war
for (int worldY = bounds.MinY; worldY <= bounds.MaxY; worldY++)
{
    for (int worldX = bounds.MinX; worldX <= bounds.MaxX; worldX++)
    {
        var tile = map.GetTile(worldX, worldY);
        var (viewportX, viewportY) = bounds.WorldToViewport(worldX, worldY);

        if (tile.IsVisible)
        {
            RenderTile(worldX, worldY, viewportX, viewportY, tile, brightness: 1.0f);
        }
        else if (tile.IsExplored)
        {
            RenderTile(worldX, worldY, viewportX, viewportY, tile, brightness: 0.3f);
        }
    }
}

// 3. Render only visible entities
var entityQuery = world.Query<Position, Renderable, Visibility>();
foreach (var entity in entityQuery)
{
    var visibility = entity.Get<Visibility>();
    if (visibility.IsVisible)
    {
        RenderEntity(entity, viewportX, viewportY);
    }
}
```

---

### AISystem Integration

**BEFORE (Current):**
```csharp
// Always sees player
if (distance < aggroRange)
{
    ChasePlayer();
}
```

**AFTER (RFC-0003):**
```csharp
// Check visibility first
bool canSeePlayer = CanSeeTarget(world, enemyPos, playerPos);

if (canSeePlayer && distance < aggroRange)
{
    ChasePlayer();
}
else
{
    Patrol();
}
```

---

## Common Pitfalls

### ❌ DON'T:
- Run `dotnet build` or `dotnet run` directly from `development/` directories
- Skip tests
- Ignore performance targets in RFCs
- Change .NET target frameworks without approval
- Create git repositories in lunar-horse root (workspace, not a repo)

### ✅ DO:
- Use Task build system (`task build-dotnet`)
- Run from artifacts (`_artifacts/latest/dotnet/console/bin`)
- Write tests as you implement (TDD)
- Follow workspace rules (`.agent/local/overrides.md`)
- Check performance metrics
- Read project-specific rules before starting

---

## Testing Strategy

### Unit Tests
- Data structures (Tile, DungeonMap, etc.)
- Algorithms (pathfinding, shadowcasting, etc.)
- Services (MapService, etc.)
- Pure logic (collision, FOV, etc.)

### Integration Tests
- System interactions (MovementSystem + EnvironmentSystem)
- Full workflows (door opening, trap triggering, etc.)
- Rendering (tiles + entities)

### Performance Tests
- FOV calculation < 5ms (range 20)
- Pathfinding < 5ms per entity
- Map access < 1ms (100x100 map)
- Render < 16ms (60 FPS)

### Manual Testing
- Run game from artifacts
- Test all features interactively
- Record sessions with URF (`--record`)
- Verify no regressions

---

## Performance Targets

From RFCs, these are critical:

| System | Operation | Target | RFC |
|--------|-----------|--------|-----|
| Environment | Tile access | < 1ms | 0001 |
| Camera | Update | < 0.1ms | 0002 |
| Camera | Coordinate transform | < 0.001ms | 0002 |
| FOV | Range 10 | < 2ms | 0003 |
| FOV | Range 20 | < 5ms | 0003 |
| Pathfinding | Per entity | < 5ms | 0005 |
| Render | Full frame | < 16ms (60 FPS) | All |

---

## Getting Help

1. **Read RFC thoroughly** - Most questions answered in RFC
2. **Check existing code** - See how similar features work
3. **Check dependencies** - Ensure all dependency RFCs implemented
4. **Review workspace rules** - `.agent/local/overrides.md`
5. **Ask user** - If design decision unclear

---

## Success Metrics

### Phase 1 Complete When:
- ✅ Dungeon maps with walls, floors, doors working
- ✅ Camera follows player smoothly
- ✅ FOV and fog of war functional
- ✅ Procedural dungeons generate correctly
- ✅ All Phase 1 tests passing
- ✅ Performance targets met
- ✅ Game playable with new features

### Phase 2-5: See GAP_ANALYSIS.md

---

## File Locations

### Documentation
- Current State: `docs/CURRENT_STATE.md`
- Gap Analysis: `docs/GAP_ANALYSIS.md`
- RFCs: `docs/rfcs/`
- RFC Index: `docs/rfcs/INDEX.md`

### Code
- WingedBean Framework: `development/dotnet/framework/src/`
- Plugins: `development/dotnet/framework/src/plugins/`
- Console Host: `development/dotnet/console/src/host/`
- Tests: `development/dotnet/framework/tests/`

### Build
- Build directory: `build/`
- Taskfile: `build/Taskfile.yml`
- Artifacts: `build/_artifacts/{version}/`

### Plate Projects
- Location: `../../../plate-projects/`
- Examples: `camera-bioca/`, `edg-rogqd/`, `inventory-poki/`

---

**Last Updated:** 2025-10-15
**Version:** 1.0
