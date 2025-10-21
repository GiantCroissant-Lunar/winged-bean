# WingedBean - Current State Analysis

**Date:** 2025-10-15
**Version:** 1.0.0
**Status:** Foundation Complete, Core Gameplay In Progress

---

## Executive Summary

WingedBean is a multi-language, plugin-based dungeon crawler game with a strong technical foundation. The project has ~60% of the technical infrastructure complete, but only ~30% of gameplay features implemented. The architecture is well-designed with clear separation between reusable framework components (Plate layer) and game-specific implementations (Yokan layer).

---

## Architecture Overview

### Layer Separation

```
┌─────────────────────────────────────────────────────┐
│  PLATE LAYER (Reusable Framework Components)        │
│  Location: personal-work/plate-projects/            │
│  - camera-bioca, capability-orlia, cross-milo       │
│  - edg-rogqd, goap-ckpor, inventory-poki           │
│  - perception-sensor, input-nanat, ui-blink         │
│  - host-land, plugin-manoi, modern-satsuma          │
└─────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────┐
│  YOKAN LAYER (Game-Specific Implementations)        │
│  Location: personal-work/yokan-projects/winged-bean │
│  - WingedBean.Plugins.* (game plugins)              │
│  - WingedBean.Gameplay.* (gameplay systems)         │
│  - Dungeon.Host.* (console/desktop hosts)           │
└─────────────────────────────────────────────────────┘
```

### Multi-Language Stack

- **Primary:** .NET 8.0 (console host), .NET Standard 2.1 (plugins)
- **Secondary:** Node.js (PTY service, web sites)
- **Future:** Python (utilities), Unity (host), Godot (host)

---

## ✅ Implemented Components

### Plate Layer (Framework Components)

#### 1. **camera-bioca** - 2D Camera System
**Status:** RFC Complete, Not Yet Implemented
**Location:** `plate-projects/camera-bioca/`

**Features:**
- Multiple follow modes (center, edge-scroll, free, fixed)
- Viewport culling for performance
- Smooth camera movement (lerp)
- Map bounds clamping
- Coordinate conversion (world ↔ viewport)

**Integration Required:** Yes (into WingedBean)

---

#### 2. **capability-orlia** - Capability Evaluation Framework
**Status:** Implemented
**Location:** `plate-projects/capability-orlia/`

**Features:**
- GAS-inspired attribute system (base/current values)
- RulesEngine-based capability evaluation
- Declarative capability definitions (JSON)
- GOAP integration hooks
- Context-based evaluation

**Usage in WingedBean:** Used by WingedBean.Gameplay.AI

---

#### 3. **cross-milo** - Cross-Platform Contracts
**Status:** Implemented
**Location:** `plate-projects/cross-milo/`

**Features:**
- Service contracts (IRegistry, IService, etc.)
- URF (Universal Recording Format) for testing
- Input contracts (RawKeyEvent, etc.)
- Audio contracts (IService)
- Recorder contracts

**Usage in WingedBean:** Core dependency for all plugins

---

#### 4. **edg-rogqd** - Dungeon Generation
**Status:** Implemented (v0.1.0)
**Location:** `plate-projects/edg-rogqd/`

**Features:**
- Graph-based dungeon generation
- Uses modern-satsuma for algorithms
- IDungeonGenerator interface
- Grid2D support
- Room templates and level descriptions

**Integration Required:** Yes (into WingedBean.Gameplay.Map)

---

#### 5. **goap-ckpor** - GOAP AI Planning
**Status:** Implemented (port of CrashKonijn v3.0.34)
**Location:** `plate-projects/goap-ckpor/`

**Features:**
- Goal-Oriented Action Planning
- Action preconditions and effects
- Planning algorithm
- Sensor system
- .NET adaptation of Unity GOAP library

**Usage in WingedBean:** Used by WingedBean.Gameplay.Goap

---

#### 6. **inventory-poki** - Inventory System
**Status:** Implemented
**Location:** `plate-projects/inventory-poki/`

**Features:**
- Core item abstractions (IItem, StackableItem, UniqueItem)
- Inventory implementations (SlottedInventory)
- Constraints system
- Transaction support
- Plugin-compatible

**Integration Required:** Yes (into WingedBean gameplay)

---

#### 7. **perception-sensor** - Sensory Perception
**Status:** Planning Phase (RFCs Complete)
**Location:** `plate-projects/perception-sensor/`

**Planned Features:**
- Vision (FOV/shadowcasting)
- Hearing (sound propagation)
- Smell (gradient tracking)
- Tremorsense, telepathy, echolocation
- Generic perception engine

**Integration Required:** Yes (when implemented)

---

#### 8. **input-nanat** - Input System
**Status:** Implemented
**Location:** `plate-projects/input-nanat/`

**Features:**
- Stack-based input router
- Modal input handling (dialogs, menus, gameplay)
- Console/terminal input mapper
- Escape sequence support (CSI, SS3, VirtualKey)
- Scope management

**Usage in WingedBean:** Used by console/desktop hosts

---

#### 9. **ui-blink** - UI Contracts
**Status:** Contracts Defined
**Location:** `plate-projects/ui-blink/`

**Features:**
- IUIFramework, IWindow interfaces
- Framework-agnostic UI abstractions
- Plugin-based UI implementations

**Usage in WingedBean:** UI plugins reference these contracts

---

#### 10. **host-land** - Hosting Infrastructure
**Status:** Implemented
**Location:** `plate-projects/host-land/`

**Features:**
- Generic hosting infrastructure
- Plugin host support

**Usage in WingedBean:** Foundation for Dungeon.Host.Console, Dungeon.Host.SadConsole

---

#### 11. **plugin-manoi** - Plugin System
**Status:** Implemented
**Location:** `plate-projects/plugin-manoi/`

**Features:**
- AssemblyLoadContext-based plugin loading
- Hot-reload support
- Dependency resolution
- Manifest-based discovery (.plugin.json)
- Multi-profile support (console, unity, godot, web)

**Usage in WingedBean:** Core plugin infrastructure

---

#### 12. **modern-satsuma** - Graph Algorithms
**Status:** Implemented
**Location:** `plate-projects/modern-satsuma/`

**Features:**
- Graph data structures
- Pathfinding (Dijkstra, A*, BFS)
- Connectivity algorithms
- Graph matching

**Usage:** Used by edg-rogqd for dungeon generation

---

#### 13. **ffmpeg-calox** - Media Handling
**Status:** Unknown
**Location:** `plate-projects/ffmpeg-calox/`

**Purpose:** Video/media processing (likely for recordings)

---

### Yokan Layer (WingedBean Game)

#### WingedBean Plugins

##### 1. **WingedBean.Plugins.ArchECS** - ECS Integration
**Status:** Implemented
**Location:** `development/dotnet/framework/src/plugins/WingedBean.Plugins.ArchECS/`

**Features:**
- Arch ECS integration
- ArchWorld wrapper
- ArchEntity, ArchQuery abstractions
- SystemBase for ECS systems
- Plugin-based ECS service

**Dependencies:** Arch (NuGet)

---

##### 2. **WingedBean.Plugins.Audio** - Audio System
**Status:** Implemented
**Location:** `development/dotnet/framework/src/plugins/WingedBean.Plugins.Audio/`

**Features:**
- LibVLC-based audio playback
- Audio streaming support
- Volume control
- Console profile support

**Dependencies:** LibVLC

---

##### 3. **WingedBean.Plugins.Analytics** - Analytics
**Status:** Implemented
**Location:** `development/dotnet/framework/src/plugins/WingedBean.Plugins.Analytics/`

**Purpose:** Game analytics tracking

---

##### 4. **WingedBean.Plugins.Config** - Configuration
**Status:** Implemented
**Location:** `development/dotnet/framework/src/plugins/WingedBean.Plugins.Config/`

**Purpose:** Configuration management

---

##### 5. **WingedBean.Plugins.Diagnostics** - Diagnostics Suite
**Status:** Implemented
**Location:** `development/dotnet/framework/src/plugins/WingedBean.Plugins.Diagnostics*/`

**Variants:**
- Base diagnostics
- Sentry integration
- OpenTelemetry integration
- Firebase integration

**Purpose:** Error tracking, telemetry, monitoring

---

##### 6. **WingedBean.Plugins.DungeonGame** - Core Game Logic
**Status:** Partially Implemented
**Location:** `development/dotnet/console/src/plugins/WingedBean.Plugins.DungeonGame/`

**Implemented Components:**
- `Player` - Tag component
- `Enemy` - AI component (Type, State, AggroRange, Target)
- `Item` - Item component (Type, Value)
- `Position` - X, Y, Floor coordinates
- `Stats` - HP, Mana, Str, Dex, Int, Def, Level, XP
- `Renderable` - Symbol, Colors, RenderLayer
- `Blocking` - BlocksMovement, BlocksLight

**Implemented Systems:**
- `AISystem` - Basic enemy AI (aggro, chase, flee)
- `CombatSystem` - Adjacent melee combat
- `MovementSystem` - Position validation
- `RenderSystem` - Simple text buffer rendering

**Implemented Infrastructure:**
- `EntityFactory` - Data-driven entity creation
- `ResourceLoader` - JSON-based resource loading
- `DungeonGame` - ECS initialization, game loop

**See:** `DUNGEON_CRAWLER_ENHANCEMENT_PLAN.md` for details on missing features

---

##### 7. **WingedBean.Plugins.Resource** - Resource Management
**Status:** Implemented
**Location:** `development/dotnet/framework/src/plugins/WingedBean.Plugins.Resource/`

**Purpose:** Resource loading and management

---

##### 8. **WingedBean.Plugins.WebSocket** - WebSocket Support
**Status:** Implemented
**Location:** `development/dotnet/framework/src/plugins/WingedBean.Plugins.WebSocket/`

**Purpose:** WebSocket communication (likely for web UI)

---

#### WingedBean Gameplay Systems

##### 1. **WingedBean.Gameplay.AI** - AI Framework
**Status:** Implemented
**Location:** `development/dotnet/framework/src/WingedBean.Gameplay.AI/`

**Architecture:** Rules Engine + GOAP

**Features:**
- `ICapabilityEvaluator` - Capability checking
- `RulesEngineCapabilityEvaluator` - RulesEngine implementation
- `BehaviorContext` - Base context for AI evaluation
- `CapabilityGoapAction` - GOAP action with capability checks

**Integration:**
- Uses capability-orlia (Plate layer)
- Uses goap-ckpor (Plate layer)
- Provides declarative AI behavior

**See:** `WingedBean.Gameplay.AI/README.md` for architecture details

---

##### 2. **WingedBean.Gameplay.Goap** - GOAP Planner Service
**Status:** Implemented
**Location:** `development/dotnet/framework/src/WingedBean.Gameplay.Goap/`

**Features:**
- `IGoapPlannerService` - Service interface
- `GoapPlannerService` - Service implementation
- `GoapDemoRunner` - Demo/example code

---

##### 3. **WingedBean.Gameplay.Map** - Map System
**Status:** Partially Implemented
**Location:** `development/dotnet/framework/src/WingedBean.Gameplay.Map/`

**Structure:**
- `Components/` - Map-related ECS components
- `Data/` - Map data structures
- `Generation/` - Map generation logic
- `Systems/` - Map-related ECS systems

**Integration Required:** Yes (with edg-rogqd)

---

##### 4. **WingedBean.Gameplay.Perception** - Perception System
**Status:** Partially Implemented
**Location:** `development/dotnet/framework/src/WingedBean.Gameplay.Perception/`

**Structure:**
- `Components/` - Perception-related ECS components
- `Configuration/` - Perception configuration
- `Data/` - Perception data structures
- `Filters/` - Perception filters
- `Systems/` - Perception ECS systems

**Integration Required:** Yes (with perception-sensor when completed)

---

##### 5. **WingedBean.Contracts.Gameplay** - Gameplay Contracts
**Status:** Implemented
**Location:** `development/dotnet/framework/src/WingedBean.Contracts.Gameplay/`

**Interfaces:**
- `IService` - Base service interface
- `IInventoryService` - Inventory management
- `ICombatService` - Combat resolution
- `IQuestService` - Quest management
- `IWorldSnapshotProvider` - World state snapshots

**Purpose:** Define contracts for gameplay services (implementations pending)

---

#### Other WingedBean Systems

##### 1. **WingedBean.Testing.Recorder** - URF Recording
**Status:** Implemented
**Location:** `development/dotnet/framework/src/WingedBean.Testing.Recorder/`

**Features:**
- URF (Universal Recording Format) support
- Recording test bot service
- Automated testing support
- Command-line recording (--record flag)

**Integration:** Used for CI/CD testing, regression detection

---

##### 2. **WingedBean.Reporting.Runtime** - Runtime Reporting
**Status:** Implemented
**Location:** `development/dotnet/framework/src/WingedBean.Reporting.Runtime/`

**Purpose:** Runtime reporting and metrics

---

##### 3. **WingedBean.FigmaSharp.Core** - Figma Integration
**Status:** Implemented
**Location:** `development/dotnet/framework/src/WingedBean.FigmaSharp.Core/`

**Purpose:** UI design import from Figma (unclear integration)

---

## Build & Development Infrastructure

### Build System
- **Task** - Build automation (Taskfile.yml in build/)
- **NUKE** - Advanced build components
- **Artifact-First Policy** - Always build to versioned artifacts in `build/_artifacts/{version}/`

### CI/CD
- **MegaLinter** - Multi-language linting
- **Act** - Local GitHub Actions testing
- **Pre-commit Hooks** - GitLeaks, Ruff, Terraform, dotnet format

### Testing
- **URF Recording** - Automated gameplay recording
- **xUnit** - Unit testing framework
- **Golden Recording Comparison** - Regression detection

### Infrastructure
- **Terraform Cloud** - Infrastructure as Code
- **SOPS + age** - Secret encryption
- **GitLeaks** - Secret detection

---

## Project Statistics

### Code Organization
- **Plate Projects:** 13 framework libraries
- **WingedBean Plugins:** 8+ plugins
- **WingedBean Gameplay:** 5 gameplay systems
- **Hosts:** Console (Dungeon.Host.Console), Desktop (Dungeon.Host.SadConsole), Unity (planned)

### Technology Stack
- **.NET:** 8.0 (hosts), Standard 2.1 (plugins, Unity/Godot compatibility)
- **ECS:** Arch (high-performance ECS)
- **Audio:** LibVLC
- **Diagnostics:** Sentry, OpenTelemetry, Firebase
- **AI:** RulesEngine + GOAP (CrashKonijn port)
- **Graphs:** modern-satsuma (graph algorithms)

### Development Workflow
```bash
# Build
cd /path/to/winged-bean/build
task build-dotnet

# Run from artifacts
cd _artifacts/latest/dotnet/console/bin
./Dungeon.Host.Console --record --record-duration=30 --record-session=test

# Test
task test
```

---

## Integration Status

| Component | Plate Layer | Yokan Layer | Integration Status |
|-----------|-------------|-------------|-------------------|
| ECS | - | ✅ ArchECS Plugin | ✅ Complete |
| Audio | ✅ Contracts (cross-milo) | ✅ LibVLC Plugin | ✅ Complete |
| AI (GOAP) | ✅ goap-ckpor | ✅ Gameplay.Goap | ✅ Complete |
| AI (Capabilities) | ✅ capability-orlia | ✅ Gameplay.AI | ✅ Complete |
| Input | ✅ input-nanat | ✅ Used in hosts | ✅ Complete |
| Plugin System | ✅ plugin-manoi | ✅ All plugins | ✅ Complete |
| Camera | ✅ camera-bioca | ❌ Not integrated | ❌ Pending |
| Dungeon Gen | ✅ edg-rogqd | ⚠️ Gameplay.Map (partial) | ⚠️ In Progress |
| Perception/FOV | ⚠️ perception-sensor (planning) | ⚠️ Gameplay.Perception (partial) | ❌ Pending |
| Inventory | ✅ inventory-poki | ❌ Not integrated | ❌ Pending |

---

## Current Game Features

### Working Features
- ✅ **Basic ECS** - Entity creation, component management
- ✅ **Player Entity** - Player character with stats
- ✅ **Enemy Entities** - Goblin, Orc, Skeleton, Troll, Dragon
- ✅ **AI States** - Idle, Patrol, Chase, Attack, Flee
- ✅ **Stats System** - HP, Mana, Str, Dex, Int, Def, Level, XP
- ✅ **Melee Combat** - Adjacent tile combat
- ✅ **Movement** - Position validation, bounds checking
- ✅ **Rendering** - Text-based rendering (40x15 viewport)
- ✅ **Audio Playback** - LibVLC-based audio
- ✅ **Input Handling** - Keyboard input with modal support
- ✅ **Resource Loading** - JSON-based entity definitions
- ✅ **Recording** - URF-based gameplay recording
- ✅ **Plugin Loading** - Dynamic plugin system

### Partially Working Features
- ⚠️ **AI Behavior** - Basic chase/flee, needs pathfinding
- ⚠️ **Blocking** - Component exists, partial implementation
- ⚠️ **Map System** - Data structures exist, no dungeon generation
- ⚠️ **Perception** - Components exist, no FOV implementation

---

## Next Steps

See `GAP_ANALYSIS.md` for missing features and `docs/rfcs/` for implementation plans.

---

**Document Version:** 1.0
**Last Updated:** 2025-10-15
**Maintained By:** WingedBean Team
