# Migration: Remove CrossMilo.Contracts.ECS and CrossMilo.Contracts.Game

**Date:** 2025-10-11  
**Status:** Migration Plan  
**Objective:** Move game-specific contracts from CrossMilo framework to ConsoleDungeon game project

## Overview

This migration removes two framework-level contracts that are too specific:
1. **CrossMilo.Contracts.ECS** - ECS abstractions (too implementation-specific)
2. **CrossMilo.Contracts.Game** - Game logic (too game-specific)

And creates:
- **ConsoleDungeon.Contracts** - Game-specific contracts for dungeon crawler

## Rationale

See `/plate-projects/cross-milo/docs/CONTRACT_SCOPE_ANALYSIS.md` for full analysis.

**Key Points:**
- ECS implementations (Arch, Unity DOTS, DefaultEcs) are too different to abstract
- Game logic (dungeon crawler) doesn't belong in framework
- Framework should provide generic infrastructure (Input, Audio, Scene, UI)
- Game should define its own contracts and use framework infrastructure

## What Was Created

### ✅ ConsoleDungeon.Contracts Project

**Location:** `/winged-bean/development/dotnet/console/src/game/ConsoleDungeon.Contracts/`

**Files Created:**
- `ConsoleDungeon.Contracts.csproj` - Project file
- `IDungeonService.cs` - Main game service contract
- `DungeonTypes.cs` - Game-specific types (GameInput, GameState, PlayerStats, etc.)
- `DungeonEvents.cs` - Game-specific events (EntityMovedEvent, CombatEvent, etc.)
- `GameInputEvent.cs` - Game input events
- `README.md` - Documentation

**Namespace:** `ConsoleDungeon.Contracts`

## Migration Steps

### Phase 1: Update Project References

#### 1.1 Add ConsoleDungeon.Contracts Reference

Update these projects to reference `ConsoleDungeon.Contracts` instead of `CrossMilo.Contracts.Game`:

**Projects to Update:**
```
WingedBean.Plugins.DungeonGame/WingedBean.Plugins.DungeonGame.csproj
WingedBean.Plugins.ConsoleDungeon/WingedBean.Plugins.ConsoleDungeon.csproj
WingedBean.Providers.Input/WingedBean.Providers.Input.csproj
WingedBean.Providers.TerminalGuiScene/WingedBean.Providers.TerminalGuiScene.csproj
ConsoleDungeon.Host/ConsoleDungeon.Host.csproj
ConsoleDungeon.Host.Tests/ConsoleDungeon.Host.Tests.csproj
WingedBean.Plugins.DungeonGame.Tests/WingedBean.Plugins.DungeonGame.Tests.csproj
WingedBean.Plugins.ConsoleDungeon.Tests/WingedBean.Plugins.ConsoleDungeon.Tests.csproj
WingedBean.Tests.E2E.ConsoleDungeon/WingedBean.Tests.E2E.ConsoleDungeon.csproj
```

**Change:**
```xml
<!-- Remove -->
<ProjectReference Include="../../../../../../../../plate-projects/cross-milo/dotnet/framework/src/CrossMilo.Contracts.Game/CrossMilo.Contracts.Game.csproj" />

<!-- Add -->
<ProjectReference Include="../../game/ConsoleDungeon.Contracts/ConsoleDungeon.Contracts.csproj" />
```

#### 1.2 Remove CrossMilo.Contracts.ECS Reference

Remove references to `CrossMilo.Contracts.ECS` from:

**Projects to Update:**
```
WingedBean.Plugins.ArchECS/WingedBean.Plugins.ArchECS.csproj
WingedBean.Plugins.DungeonGame/WingedBean.Plugins.DungeonGame.csproj
ConsoleDungeon.Host/ConsoleDungeon.Host.csproj
ConsoleDungeon.Host.Tests/ConsoleDungeon.Host.Tests.csproj
WingedBean.Plugins.ArchECS.Tests/WingedBean.Plugins.ArchECS.Tests.csproj
WingedBean.Plugins.DungeonGame.Tests/WingedBean.Plugins.DungeonGame.Tests.csproj
WingedBean.Tests.E2E.ConsoleDungeon/WingedBean.Tests.E2E.ConsoleDungeon.csproj
```

**Change:**
```xml
<!-- Remove -->
<ProjectReference Include="../../../../../../../../plate-projects/cross-milo/dotnet/framework/src/CrossMilo.Contracts.ECS/CrossMilo.Contracts.ECS.csproj" />
```

### Phase 2: Update Using Statements

#### 2.1 Replace CrossMilo.Contracts.Game with ConsoleDungeon.Contracts

**Files to Update (28 files):**
```
ConsoleDungeon.Host/RegistryHelper.cs
ConsoleDungeon.Host/PluginLoaderHostedService.cs
WingedBean.Plugins.DungeonGame/DungeonGamePlugin.cs
WingedBean.Plugins.DungeonGame/DungeonGamePluginActivator.cs
WingedBean.Plugins.DungeonGame/DungeonGameService.cs
WingedBean.Plugins.DungeonGame/Services/GameUIServiceProvider.cs
WingedBean.Plugins.DungeonGame/Services/RenderServiceProvider.cs
WingedBean.Plugins.ConsoleDungeon/ConsoleDungeonAppRefactored.cs
WingedBean.Plugins.ConsoleDungeon/Input/GameplayInputScope.cs
WingedBean.Plugins.ConsoleDungeon/Input/DefaultInputMapper.cs
WingedBean.Plugins.ConsoleDungeon/Input/DefaultInputRouter.cs
WingedBean.Plugins.ConsoleDungeon/Scene/TerminalGuiSceneProvider.cs
WingedBean.Providers.Input/DefaultInputMapper.cs
WingedBean.Providers.Input/DefaultInputRouter.cs
WingedBean.Providers.TerminalGuiScene/TerminalGuiSceneProvider.cs
ConsoleDungeon.Host.Tests/ConsoleDungeonApp_FakeDriverTests.cs
WingedBean.Plugins.DungeonGame.Tests/DungeonGamePluginTests.cs
WingedBean.Plugins.DungeonGame.Tests/DungeonGameServiceTests.cs
WingedBean.Plugins.ConsoleDungeon.Tests/Input/DefaultInputMapperTests.cs
WingedBean.Plugins.ConsoleDungeon.Tests/Input/DefaultInputRouterTests.cs
```

**Change:**
```csharp
// Replace
using Plate.CrossMilo.Contracts.Game;
using Plate.CrossMilo.Contracts.Game.Dungeon;

// With
using ConsoleDungeon.Contracts;
```

#### 2.2 Remove CrossMilo.Contracts.ECS Using Statements

**Files to Update (28 files):**
```
ConsoleDungeon.Host/RegistryHelper.cs
WingedBean.Plugins.ArchECS/ArchECSPlugin.cs
WingedBean.Plugins.ArchECS/ArchECSPluginActivator.cs
WingedBean.Plugins.ArchECS/ArchECSService.cs
WingedBean.Plugins.ArchECS/ArchEntity.cs
WingedBean.Plugins.ArchECS/ArchQuery.cs
WingedBean.Plugins.ArchECS/ArchWorld.cs
WingedBean.Plugins.ArchECS/SystemBase.cs
WingedBean.Plugins.DungeonGame/DungeonGame.cs
WingedBean.Plugins.DungeonGame/DungeonGameService.cs
WingedBean.Plugins.DungeonGame/Components/EntityComponents.cs
WingedBean.Plugins.DungeonGame/Data/EntityFactory.cs
WingedBean.Plugins.DungeonGame/Systems/AISystem.cs
WingedBean.Plugins.DungeonGame/Systems/CombatSystem.cs
WingedBean.Plugins.DungeonGame/Systems/MovementSystem.cs
WingedBean.Plugins.DungeonGame/Systems/RenderSystem.cs
ConsoleDungeon.Host.Tests/ConsoleDungeonApp_FakeDriverTests.cs
WingedBean.Plugins.ArchECS.Tests/*.cs (6 files)
WingedBean.Plugins.DungeonGame.Tests/*.cs (2 files)
```

**Change:**
```csharp
// Remove
using Plate.CrossMilo.Contracts.ECS;
using Plate.CrossMilo.Contracts.ECS.Services;
```

### Phase 3: Handle ECS Abstractions

#### 3.1 Move ECS Interfaces to WingedBean.Plugins.ArchECS (Internal)

ECS abstractions should be **internal implementation details**, not public contracts.

**Option A: Keep ECS Interfaces Internal to ArchECS Plugin**

Move ECS interfaces to `WingedBean.Plugins.ArchECS/Internal/`:
```
WingedBean.Plugins.ArchECS/
├── Internal/
│   ├── IECSWorld.cs
│   ├── IECSEntity.cs
│   ├── IECSQuery.cs
│   └── IECSSystem.cs
├── ArchWorld.cs (implements IECSWorld internally)
├── ArchEntity.cs (implements IECSEntity internally)
└── ArchECSService.cs
```

**Option B: Remove ECS Abstractions Entirely**

Use Arch ECS directly without abstraction layer:
```csharp
// Before (with abstraction):
IWorld world = _ecsService.CreateWorld();
IEntity entity = world.CreateEntity();

// After (direct Arch ECS):
World world = World.Create();
Entity entity = world.Create();
```

**Recommendation:** **Option B** - Remove ECS abstractions entirely. The abstraction provides no value and adds complexity.

#### 3.2 Update DungeonGameService

**Current (exposes ECS details):**
```csharp
public class DungeonGameService : IService
{
    public IWorld World { get; } // ❌ Exposes ECS details
}
```

**After (hides ECS details):**
```csharp
public class DungeonGameService : IDungeonService
{
    private World _world; // ✅ Internal Arch ECS world
    
    public void Update(float deltaTime) { /* ... */ }
    public void HandleInput(GameInput input) { /* ... */ }
    public IObservable<GameState> GameStateObservable { get; }
    public IObservable<IReadOnlyList<EntitySnapshot>> EntitiesObservable { get; }
    public IObservable<PlayerStats> PlayerStatsObservable { get; }
}
```

### Phase 4: Update Service Registration

#### 4.1 Update RegistryHelper.cs

**Current:**
```csharp
using Plate.CrossMilo.Contracts.Game.Dungeon;
using Plate.CrossMilo.Contracts.ECS.Services;

registry.Register<IService>(dungeonService);
registry.Register<IService>(ecsService);
```

**After:**
```csharp
using ConsoleDungeon.Contracts;

registry.Register<IDungeonService>(dungeonService);
// ECS service is internal to DungeonGame plugin, not registered globally
```

#### 4.2 Update Plugin Activators

**DungeonGamePluginActivator.cs:**
```csharp
// Before
using Plate.CrossMilo.Contracts.Game.Dungeon;
services.AddSingleton<IService, DungeonGameService>();

// After
using ConsoleDungeon.Contracts;
services.AddSingleton<IDungeonService, DungeonGameService>();
```

**ArchECSPluginActivator.cs:**
```csharp
// Before
using Plate.CrossMilo.Contracts.ECS.Services;
services.AddSingleton<IService, ArchECSService>();

// After
// ECS service is internal to DungeonGame plugin
// No global registration needed
```

### Phase 5: Remove CrossMilo Projects

After all references are updated and tests pass:

#### 5.1 Remove CrossMilo.Contracts.ECS

```bash
cd /Users/apprenticegc/Work/lunar-horse/personal-work/plate-projects/cross-milo
rm -rf dotnet/framework/src/CrossMilo.Contracts.ECS
```

#### 5.2 Remove CrossMilo.Contracts.Game

```bash
cd /Users/apprenticegc/Work/lunar-horse/personal-work/plate-projects/cross-milo
rm -rf dotnet/framework/src/CrossMilo.Contracts.Game
```

### Phase 6: Build and Test

#### 6.1 Build All Projects

```bash
cd /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/development/dotnet/console
dotnet build
```

#### 6.2 Run Tests

```bash
dotnet test
```

#### 6.3 Run E2E Tests

```bash
dotnet test tests/e2e/WingedBean.Tests.E2E.ConsoleDungeon
```

### Phase 7: Update Documentation

#### 7.1 Update CrossMilo Documentation

Update these documents:
- `/plate-projects/cross-milo/README.md` - Remove ECS and Game from contract list
- `/plate-projects/cross-milo/docs/ARCHITECTURE.md` - Update architecture diagrams

#### 7.2 Update WingedBean Documentation

Create/update these documents:
- `/winged-bean/development/dotnet/console/README.md` - Document ConsoleDungeon.Contracts
- `/winged-bean/development/dotnet/console/docs/ARCHITECTURE.md` - Show game-specific contracts

### Phase 8: Commit Changes

#### 8.1 Commit CrossMilo Changes

```bash
cd /Users/apprenticegc/Work/lunar-horse/personal-work/plate-projects/cross-milo
git add -A
git commit -m "refactor: remove ECS and Game contracts from framework

- Removed CrossMilo.Contracts.ECS (too implementation-specific)
- Removed CrossMilo.Contracts.Game (too game-specific)
- Framework now provides only generic infrastructure contracts
- Game-specific contracts moved to ConsoleDungeon.Contracts

See docs/CONTRACT_SCOPE_ANALYSIS.md for rationale."
git push
```

#### 8.2 Commit WingedBean Changes

```bash
cd /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean
git add -A
git commit -m "refactor: create ConsoleDungeon.Contracts for game-specific contracts

- Created ConsoleDungeon.Contracts project
- Moved game-specific types from CrossMilo.Contracts.Game
- Updated all references to use ConsoleDungeon.Contracts
- Removed dependency on CrossMilo.Contracts.ECS
- ECS is now internal implementation detail of DungeonGame plugin

This separates framework contracts (generic infrastructure) from
game contracts (dungeon crawler specific logic)."
git push
```

## Benefits After Migration

### ✅ 1. Clear Separation

- **Framework (CrossMilo)** = Generic infrastructure (Input, Audio, Scene, UI)
- **Game (ConsoleDungeon)** = Specific game logic (Dungeon, Inventory, Battle)

### ✅ 2. No Leaky Abstractions

- ECS details are hidden inside DungeonGameService
- UI consumes clean game contracts (IDungeonService)
- No exposure of IWorld, IEntity, IQuery

### ✅ 3. True Portability

- CrossMilo can be used for ANY game (platformer, racing, puzzle)
- Not tied to dungeon crawler specifics
- Not tied to specific ECS library

### ✅ 4. Easier to Understand

- Framework contracts are generic and obvious
- Game contracts are specific and clear
- No confusion about what belongs where

### ✅ 5. Better Reusability

- Framework can be reused for other games
- Game logic is self-contained
- ECS library can be swapped without affecting contracts

## Rollback Plan

If migration fails, rollback by:

1. Revert WingedBean changes: `git reset --hard HEAD~1`
2. Revert CrossMilo changes: `git reset --hard HEAD~1`
3. Restore CrossMilo.Contracts.ECS and CrossMilo.Contracts.Game from git history

## Next Steps

1. ✅ ConsoleDungeon.Contracts project created
2. ⏳ Update project references (Phase 1)
3. ⏳ Update using statements (Phase 2)
4. ⏳ Handle ECS abstractions (Phase 3)
5. ⏳ Update service registration (Phase 4)
6. ⏳ Remove CrossMilo projects (Phase 5)
7. ⏳ Build and test (Phase 6)
8. ⏳ Update documentation (Phase 7)
9. ⏳ Commit changes (Phase 8)

## Estimated Effort

- **Phase 1-2:** 1-2 hours (mechanical find-replace)
- **Phase 3:** 2-3 hours (refactor ECS abstractions)
- **Phase 4:** 1 hour (update service registration)
- **Phase 5-6:** 1 hour (remove projects, build, test)
- **Phase 7-8:** 1 hour (documentation, commit)

**Total:** 6-9 hours

## Questions?

See `/plate-projects/cross-milo/docs/CONTRACT_SCOPE_ANALYSIS.md` for full rationale and analysis.
