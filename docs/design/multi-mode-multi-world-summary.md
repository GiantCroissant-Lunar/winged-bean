# Multi-Mode & Multi-World: Summary for winged-bean

**Date**: 2025-10-02  
**Status**: Architecture Review Complete  

---

## TL;DR

✅ **Adopt**: Multi-world (authoring/runtime) and mode system (Play/EditOverlay/EditPaused) from craft-sim  
❌ **Don't Create**: IEngineProfile, IModeService, IWorldHost as separate interfaces  
✅ **Instead**: Extend existing `IECSService` with multi-world and mode support  

**Why**: winged-bean is a 4-tier service platform, not an engine abstraction. ECS is ONE service among many (config, websocket, terminal, recording). Multi-world should integrate as an ECS feature, not replace the service architecture.

---

## Key Differences: craft-sim vs winged-bean

| Aspect | craft-sim | winged-bean |
|--------|-----------|-------------|
| **Core Abstraction** | Engine (Spaces → Worlds) | Services (Tier 1 → 4) |
| **Profile System** | IEngineProfile interface | Tier 3/4 separation |
| **Plugin System** | Package manifests (JSON) | [Plugin] attributes + .plugin.json |
| **ECS Role** | Central (everything is ECS) | One service (gameplay logic) |
| **Multi-World** | Core feature | ECS service feature |

---

## Architecture Review Findings

### What winged-bean HAS ✅

1. **4-Tier Service Platform** (RFC-0002)
   - Tier 1: Contracts (netstandard2.1)
   - Tier 2: Source-Generated Façades ([RealizeService])
   - Tier 3: Adapters (Resilience, LoadContext, Telemetry)
   - Tier 4: Providers (Console: Terminal.Gui+node-pty, Unity: HybridCLR)

2. **Multi-Profile Support** (RFC-0005)
   - Console: .NET 8.0
   - Unity/Godot: .NET Standard 2.1
   - Clear profile separation via Tier 3/4

3. **Plugin Architecture** (RFC-0003, RFC-0006)
   - Everything is a plugin (except host)
   - Dynamic loading (IPluginLoader, ALC, HybridCLR)
   - .plugin.json manifests
   - [Plugin] attributes

4. **ECS Service** (RFC-0007)
   - IECSService (Tier 1 contract)
   - ArchECSService (Tier 3 plugin)
   - Single runtime world (current)

### What winged-bean LACKS ❌

1. Multi-world: No authoring/runtime separation
2. Mode system: No Play/EditOverlay/EditPaused
3. Authoring data: No stable IDs, no bake pipeline
4. Editor overlay: No in-game editor concept

---

## Recommended Approach

### ✅ DO: Extend IECSService

```csharp
// Tier 1: WingedBean.Contracts.ECS/IECSService.cs
public interface IECSService
{
    // Existing API (backward compatible)
    EntityHandle CreateEntity();
    // ...
    
    // NEW: Multi-world API
    WorldHandle AuthoringWorld { get; }
    WorldHandle CreateRuntimeWorld(string name);
    EntityHandle CreateEntity(WorldHandle world);
    
    // NEW: Mode service
    GameMode CurrentMode { get; }
    void SetMode(GameMode mode);
    event EventHandler<GameMode> ModeChanged;
    
    // NEW: Authoring mapping
    void MapAuthoringToRuntime(AuthoringNodeId authoring, EntityHandle runtime);
    EntityHandle? GetRuntimeEntity(AuthoringNodeId authoring);
}
```

**Rationale**:
- ✅ Integrates with existing service architecture
- ✅ Maintains backward compatibility
- ✅ Follows R-CODE-010 (edit existing, don't create new)
- ✅ ECS is ONE service, not the whole platform

### ❌ DON'T: Create Separate Interfaces

```csharp
// ❌ Don't create these - we already have Tier 3/4 separation
public interface IEngineProfile { }
public interface IWorldHost { }
public interface IModeService { }
public interface IRuntimeBuilder { }
```

**Why not**:
- We already have profile abstraction (Tier 3/4)
- IECSService can expose mode and world management
- Creating separate interfaces would duplicate existing architecture

---

## Migration Path

### Phase 1: Extend IECSService (Immediate)

✅ Completed in code (2025-10-03)

1. Add multi-world methods to `IECSService`
2. Add mode service properties to `IECSService`
3. Add authoring mapping methods to `IECSService`
4. Implement in `ArchECSService` (Tier 3 plugin)
5. **Maintain backward compatibility** (existing single-world API works)

### Phase 2: ConsoleDungeon Prototype (Near-term)

1. Create authoring entities with stable IDs
2. Build runtime world from authoring
3. Implement mode switching (Tab, Ctrl+P)
4. Add editor panels (Hierarchy, Inspector)
5. Test save/load authoring data

### Phase 3: Unity Profile (Medium-term)

1. Unity-specific Tier 3/4 adapters (already planned)
2. Unity authoring → runtime builder
3. Unity editor integration
4. MonoBehaviour lifecycle integration

---

## What NOT to Adopt from craft-sim

### ❌ IEngineProfile Abstraction

**craft-sim has**: Engine profile interface with conventions, build pipeline, package manager

**winged-bean has**: Tier 3/4 separation already provides profile abstraction
- Console profile: `console/src/plugins/` + `console/src/providers/`
- Unity profile: `unity/src/` (Tier 3/4 Unity-specific code)

**Decision**: Profile conventions can be documented (markdown), not code interfaces.

### ❌ Package Manager JSON Manifests

**craft-sim has**: Elaborate package.json with extension points, dependencies, build steps

**winged-bean has**: 
- [Plugin] attributes for metadata
- .plugin.json for manifests (already exists)
- IPluginLoader for discovery

**Decision**: Don't create duplicate package manifest system. Enhance .plugin.json if needed.

### ❌ Separate IModeService Interface

**craft-sim has**: Standalone IModeService that everything depends on

**winged-bean should have**: Mode service as part of IECSService

**Rationale**: 
- Mode is gameplay-specific (not relevant to IConfigService, IWebSocketService)
- ECS systems need mode gating
- Keep mode service integrated with ECS

---

## Success Criteria

### Phase 1 (Extend IECSService)
- [ ] IECSService extended with multi-world API
- [ ] ArchECSService implements new API
- [ ] Backward compatibility maintained (existing code works)
- [ ] No new interfaces created (follow R-CODE-010)
- [ ] RFC-0007 updated with multi-world section

### Phase 2 (ConsoleDungeon)
- [ ] Authoring and runtime worlds functional
- [ ] Mode switching works (keyboard shortcuts)
- [ ] Save/load authoring data
- [ ] Editor panels show/hide on demand
- [ ] Runtime rebuilds from authoring

### Phase 3 (Unity Profile)
- [ ] Unity Tier 3/4 adapters created
- [ ] Unity authoring → runtime builder
- [ ] Unity editor integration
- [ ] Asset pipeline working

---

## Related Documents

- **Full Plan**: `docs/design/multi-mode-multi-world-adoption-plan.md`
- **RFCs**: RFC-0002, RFC-0003, RFC-0005, RFC-0006, RFC-0007, RFC-0017
- **craft-sim**: `ref-projects/craft-sim/projects/craft-sim/docs/`
  - `Design-PlayEdit-Modes.md`
  - `Unity-Profile.md`

---

## Key Principles (from .agent/base/20-rules.md)

- **R-CODE-010**: Prefer editing existing files over creating new ones ✅
- **R-CODE-020**: Never fabricate code; ask when uncertain ✅
- **R-CODE-050**: Respect project structure; no orphaned files ✅
- **R-PRC-010**: Propose options for architectural decisions ✅
- **R-DOC-050**: Only create docs when explicitly requested ✅

---

**Conclusion**: Extend `IECSService` with multi-world and mode support. Don't create separate interface hierarchy. Integrate craft-sim concepts within existing 4-tier service architecture.

**Next Steps**:
1. Review this approach with stakeholders
2. Update RFC-0007 with multi-world section
3. Implement Phase 1 (extend IECSService)
4. Prototype in ConsoleDungeon

---

**Version**: 1.0  
**Author**: GitHub Copilot (post-architecture review)  
**Reviewed**: RFCs 0002, 0003, 0005, 0006, 0007, 0014  
