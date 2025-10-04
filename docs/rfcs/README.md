# RFC Index - WingedBean Architecture

This directory contains RFCs (Request for Comments) documenting major architectural decisions and proposals for the WingedBean project.

## Status Legend

- **Draft** - Under discussion, not finalized
- **Proposed** - Ready for implementation, awaiting approval
- **Accepted** - Approved and ready for implementation
- **Implemented** - Completed and in production
- **Superseded** - Replaced by a newer RFC

---

## Active RFCs

### RFC-0008: Playwright and Asciinema Testing Strategy
**Status:** ✅ Implemented (CI/CD pending)
**Date:** 2025-10-01
**Completed:** 2025-10-01
**Priority:** HIGH (P1)

**Summary:** Implement Playwright for E2E visual testing and asciinema for recording Terminal.Gui sessions. Enables automated verification that Terminal.Gui renders correctly in browser and provides documentation through recordings.

**Key Achievements:**
- ✅ Playwright E2E tests (3/3 passing)
- ✅ Visual verification of Terminal.Gui in browser
- ✅ Screenshot generation for regression testing
- ✅ Asciinema recording scripts
- ✅ Dynamic recording via F9/F10 (RFC-0009)

**Follow-Up Tasks:**
- ⚠️ GitHub Actions CI/CD workflow (pending)
- ⚠️ Asciinema-player integration for docs

[Read Full RFC →](./0008-playwright-and-asciinema-testing-strategy.md)

---

### RFC-0009: Dynamic Asciinema Recording in PTY
**Status:** ✅ Implemented
**Date:** 2025-10-01
**Completed:** 2025-10-01
**Priority:** HIGH (P1)

**Summary:** Add dynamic asciinema recording to PTY service with F9/F10 keyboard shortcuts. Enables on-demand recording of Terminal.Gui sessions for debugging and documentation.

**Key Achievements:**
- ✅ RecordingManager class (36/36 tests passing)
- ✅ F9/F10 keyboard shortcuts in Terminal.Gui

---

### RFC-0012: Agent-Driven GitHub Automation
**Status:** ⚡ In Progress (Phase 1)
**Date:** 2025-10-02
**Started:** 2025-10-02
**Priority:** P0 (Foundation)
**Category:** infra, tooling

**Summary:** Implement event-driven GitHub Actions workflows and pre-commit hooks for multi-agent collaboration (GitHub Copilot, Claude Code, Windsurf). Features issue dependency validation, PR hygiene enforcement, and agent auto-retry on failure.

**Key Achievements:**
- ✅ Phase 0: Foundation Complete (2025-10-02)
  - 3 workflows: PR validation, dependency validation, agent auto-retry
  - 4 Python scripts in proper project structure
  - 6 new agent rules (R-PRC-050, R-ISS-010 through R-ISS-050)
  - Local testing with `act`
  - Pre-commit hook for dependency validation
- ⚡ Phase 1: Real-world Testing (In Progress)
  - Issue template with dependency fields
  - Monitoring runner minute usage
  - Iterating based on feedback

**Cost Savings:**
- $329.84/month vs. reference project (no cron polling)
- $75-125 saved per agent failure recovery
- $0 development testing (using `act`)

**Next Steps:**
- Monitor workflows in production
- Create tracking issue for Phase 1
- Adjust retry limits based on data
- Begin Phase 2 (advanced features)

[Read Full RFC →](./0012-agent-driven-github-automation.md)

---

### RFC-0013: Documentation Automation Tooling
**Status:** 📝 Draft
**Date:** 2025-10-02
**Priority:** P1 (High)
**Category:** tooling, documentation
**Estimated Effort:** 2-3 days

**Summary:** Implement Python-based automation tooling for documentation management (Phase 2). Build lightweight scripts for frontmatter validation, orphaned file detection, and automated archival of old chat-history/recordings.

**Phase 2 Tools:**
- Frontmatter validator (R-DOC-020 enforcement)
- Orphaned file detector
- Auto-archival for time-based retention (30-day, 90-day)
- Pre-commit integration + GitHub Actions workflows

**Context:**
- Phase 1 (Enhanced Organization) completed: commit 4335330
- 59 markdown files across 13 categories (2.3MB total)
- Archival policies defined but not automated
- Python infrastructure already in place

**Benefits:**
- ✅ Automated R-DOC-020 compliance (frontmatter validation)
- ✅ Detect orphaned/unreferenced documentation
- ✅ Automated archival reduces clutter
- ✅ Documentation can scale without becoming unmanageable

[Read Full RFC →](./0013-documentation-automation-tooling.md)

---

### RFC-0018: Render and UI Services for Console Profile
**Status:** Proposed
**Date:** 2025-10-03
**Priority:** P1 (High)
**Category:** framework, architecture, console
**Estimated Effort:** 2-3 days

**Summary:** Implement proper `IRenderService` and `IGameUIService` contracts and providers following RFC-0002's 4-tier architecture. Separates rendering/UI concerns from game logic, fixes input handling (arrow keys control player), and improves UI layout (menu toggle, full-screen game view).

**Key Actions:**
- Create `IRenderService` and `IGameUIService` contracts (Tier 1)
- Implement Terminal.Gui providers (Tier 4)
- Refactor ConsoleDungeonApp to use services
- Fix input handling (capture game keys before Terminal.Gui navigation)
- New UI layout (full-screen game view, menu toggle with M key)

**Benefits:**
- ✅ 4-tier architecture compliance
- ✅ Working input (arrow keys move player)
- ✅ Better UX (full-screen game, menu system)
- ✅ Reusable across profiles (Unity/Godot can implement same contracts)

[Read Full RFC →](./0018-render-and-ui-services-for-console-profile.md)

---

### RFC-0017: Reactive Plugin Architecture for Dungeon Game
**Status:** Proposed
**Date:** 2025-10-03
**Priority:** P1 (High)
**Category:** gameplay, plugins

**Summary:** Separate ConsoleDungeon gameplay and UI concerns into dedicated plugins that communicate through reactive streams (System.Reactive, ReactiveUI, MessagePipe). Aligns the dungeon crawler experience with the four-tier architecture while enabling hot-swappable UIs.

**Key Actions:**
- Move Arch ECS gameplay systems into `WingedBean.Plugins.DungeonGame`
- Limit host responsibility to registry setup and plugin orchestration
- Provide `IDungeonGameService` observables for UI plugins

**Open Work:**
- Finalise reactive message contracts
- Update Terminal UI plugin to consume observables instead of direct ECS access

[Read Full RFC →](./0017-reactive-plugin-architecture-for-dungeon-game.md)

---

### RFC-0006: Dynamic Plugin Loading and Runtime Composition
**Status:** Proposed
**Date:** 2025-10-01
**Priority:** HIGH (P1)
**Estimated Effort:** 3 days

**Summary:** Replace static plugin references with dynamic loading via `plugins.json` configuration. Enables runtime composition, hot-reload support, and true plugin architecture.

**Key Changes:**
- Create `plugins.json` configuration format
- Remove static `ProjectReference` entries from Host
- Implement dynamic loading in `Program.cs`
- Create MSBuild targets to copy plugins

**Impact:** True plugin architecture, runtime composition, better testing

**Dependencies:** RFC-0005
**Blocks:** None

[Read Full RFC →](./0006-dynamic-plugin-loading.md)

---

### RFC-0007: Arch ECS Integration for Dungeon Crawler Gameplay
**Status:** Proposed
**Date:** 2025-10-01
**Priority:** HIGH (P1)
**Estimated Effort:** 7 days

**Summary:** Integrate Arch ECS as the core gameplay implementation layer. Provides high-performance entity management (1M+ entities) while maintaining service-oriented architecture at the application level.

**Key Changes:**
- Create `WingedBean.Contracts.ECS` contract (Tier 1)
- Create `WingedBean.Plugins.ArchECS` plugin (Tier 3)
- Define game components (Position, Stats, Renderable, etc.)
- Implement core systems (Movement, Combat, Render)
- Integrate with game loop

**Impact:** Enables dungeon crawler gameplay, 60 FPS with 10,000+ entities

**Dependencies:** RFC-0005, RFC-0006
**Blocks:** None

[Read Full RFC →](./0007-arch-ecs-integration.md)

---

## Implemented RFCs

### RFC-0001: Asciinema Recording for PTY Sessions
**Status:** Implemented
**Date:** 2025-09-29

**Summary:** Add support for recording PTY sessions to Asciinema format for documentation and replay.

[Read Full RFC →](./0001-asciinema-recording-for-pty-sessions.md)

---

### RFC-0002: Service Platform Core - 4-Tier Architecture
**Status:** Implemented
**Date:** 2025-09-30

**Summary:** Define the 4-tier architecture (Contracts, Infrastructure, Implementations, Providers) with strict dependency rules.

**Architecture:**
- **Tier 1:** Contracts (pure interfaces, platform-agnostic)
- **Tier 2:** Infrastructure (Registry, core logic)
- **Tier 3:** Implementations (plugins, platform-specific)
- **Tier 4:** Providers (low-level platform integration)

[Read Full RFC →](./0002-service-platform-core-4-tier-architecture.md)

---

### RFC-0003: Plugin Architecture Foundation
**Status:** Implemented
**Date:** 2025-09-30

**Summary:** Define plugin loading, lifecycle management, and service registration patterns using `IPluginLoader` and `ActualPluginLoader`.

[Read Full RFC →](./0003-plugin-architecture-foundation.md)

---

### RFC-0004: Project Organization and Folder Structure
**Status:** Implemented
**Date:** 2025-09-30

**Summary:** Reorganize `/development/dotnet` directory to support 4-tier architecture with clear separation between framework, console, and Unity implementations.

**Structure:**
```
development/dotnet/
├── framework/          # Tier 1 & 2
│   ├── src/
│   └── tests/
├── console/            # Tier 3 & 4 (Console)
│   ├── src/
│   └── tests/
└── unity/              # Tier 3 & 4 (Unity)
```

[Read Full RFC →](./0004-project-organization-and-folder-structure.md)

---

### RFC-0005: Target Framework Compliance for Multi-Platform Support
**Status:** ✅ Implemented
**Date:** 2025-10-01
**Priority:** CRITICAL (P0)
**Completed:** 2025-10-01

**Summary:** Updated all projects to use appropriate target frameworks: `.NET Standard 2.1` for Tier 1/2 (Unity/Godot compatibility), `.NET 8.0` for Tier 3/4 Console (LTS support), and `.NET Standard 2.0` for source generators.

**Key Changes:**
- ✅ All contract projects → `netstandard2.1`
- ✅ Registry → `netstandard2.1`
- ✅ Console projects → `net8.0`
- ✅ Source generators → `netstandard2.0`

**Impact:** Enables Unity and Godot support, provides LTS stability

**Verification:**
- ✅ All 95 tests passing
- ✅ ConsoleDungeon.Host verified
- ✅ xterm.js integration verified

**Documentation:**
- [Framework Targeting Guide](../guides/framework-targeting-guide.md)
- [Source Generator Usage Guide](../guides/source-generator-usage.md)

[Read Full RFC →](./0005-target-framework-compliance.md)

---

## Superseded RFCs

### RFC-0014: Engine Profile Abstraction
**Status:** Superseded by RFC-0017
**Date:** 2025-10-03

**Summary:** Explored adding an explicit `IEngineProfile` abstraction (inspired by craft-sim) to consolidate engine-specific conventions, build pipelines, and package management. Review concluded the four-tier structure already provides the necessary separation, so the proposal was not adopted.

**Resolution:** Capture profile-specific details in Tier 3/4 adapters and extend `IECSService` for multi-world + editor mode support instead of introducing a new profile registry.

[Read Full RFC →](./0014-engine-profile-abstraction.md)

---

## Implementation Roadmap

### Phase 1: Framework Compliance (Week 1)
**Target:** 2025-10-03

1. ✅ **RFC-0005:** Target Framework Compliance
   - Update all projects to correct target frameworks
   - Verify builds across all tiers
   - Create source generator project skeleton

**Status:** Ready to start
**Effort:** 2-3 days
**Priority:** CRITICAL

---

### Phase 2: Dynamic Plugin Loading (Week 2)
**Target:** 2025-10-06

1. ✅ **RFC-0006:** Dynamic Plugin Loading
   - Create plugin configuration system
   - Remove static references
   - Implement dynamic loading
   - Create MSBuild targets

**Status:** Depends on RFC-0005
**Effort:** 3 days
**Priority:** HIGH

---

### Phase 3: ECS Integration (Week 2-3)
**Target:** 2025-10-13

1. ✅ **RFC-0007:** Arch ECS Integration
   - Create ECS contract
   - Implement Arch plugin
   - Define game components
   - Create game systems
   - Integrate with game loop

**Status:** Depends on RFC-0005, RFC-0006
**Effort:** 7 days
**Priority:** HIGH

---

### Phase 4: Testing & Documentation (Parallel with Phases 1-3)
**Target:** 2025-10-13

1. ✅ **RFC-0008:** Playwright and Asciinema Testing
   - Install Playwright and configure E2E tests
   - Create visual verification tests
   - Integrate asciinema recording in PTY service
   - Record progress for RFC-0005, RFC-0006, RFC-0007 implementations
   - Set up CI/CD workflows

**Status:** Can run in parallel with RFC-0005-0007
**Effort:** 3-4 days
**Priority:** HIGH

**Recording Strategy:**
- Record baseline before RFC-0005 (current state)
- Record after RFC-0005 (framework compliance changes)
- Record after RFC-0006 (dynamic plugin loading demo)
- Record after RFC-0007 (ECS gameplay demo)

---

## Implementation Order

**Strict Order (blocking dependencies):**
```
RFC-0005 (Framework Compliance)
   ↓
RFC-0006 (Dynamic Loading) ← Can start after RFC-0005
   ↓
RFC-0007 (Arch ECS) ← Can start after RFC-0005 & RFC-0006

RFC-0008 (Testing & Recording) ← Can run in parallel with all above
```

**Recommended Schedule:**
- **Day 0:** RFC-0008 Setup (Playwright + Asciinema) + Record baseline
- **Days 1-2:** RFC-0005 (Framework Compliance) + Record after completion
- **Days 3-5:** RFC-0006 (Dynamic Plugin Loading) + Record demo
- **Days 6-12:** RFC-0007 (Arch ECS Integration) + Record gameplay demo
- **Day 13:** RFC-0008 Finalization (CI/CD workflows, documentation)

**Total Time:** ~13 working days (2.5 weeks)

**Recording Milestones:**
1. **Baseline Recording** (Day 0): Current Terminal.Gui state
2. **Post-RFC-0005** (Day 2): Framework compliance verified
3. **Post-RFC-0006** (Day 5): Dynamic plugin loading demo
4. **Post-RFC-0007** (Day 12): ECS gameplay with 10,000+ entities

---

---

### RFC-0008: Playwright and Asciinema Testing Strategy
**Status:** Proposed
**Date:** 2025-10-01
**Priority:** HIGH (P1)
**Estimated Effort:** 3-4 days

**Summary:** Implement visual E2E testing with Playwright and asciinema recording for Terminal.Gui PTY integration. Enables automated visual verification and progress documentation.

**Key Changes:**
- Add Playwright for visual testing and screenshot comparison
- Integrate asciinema recording in PTY service
- Create GitHub Actions workflows for automated testing
- Record development progress for RFC-0005, RFC-0006, RFC-0007

**Impact:** Visual verification of Terminal.Gui rendering, automated regression testing, progress documentation

**Dependencies:** None (can run in parallel with RFC-0005-0007)
**Blocks:** None

[Read Full RFC →](./0008-playwright-and-asciinema-testing-strategy.md)

---

### RFC-0022: FigmaSharp Core Architecture
**Status:** 📝 Draft
**Date:** 2025-10-04
**Priority:** P1 (High)
**Category:** architecture, ui, figma
**Estimated Effort:** 2-3 weeks

**Summary:** Establish the FigmaSharp framework architecture for transforming Figma designs into UI framework implementations. Provides a framework-agnostic abstraction layer between Figma's design model and specific UI frameworks (Terminal.Gui, Unity UGUI, Unity UI Toolkit, Godot UI).

**Key Components:**
- Figma domain model (plain C#, no Unity types)
- Abstract UI model (UIElement, LayoutData, StyleData)
- IUIRenderer interface for framework-specific renderers
- FigmaTransformer (port from D.A. Assets)
- FigmaToUIPipeline (orchestration)

**Benefits:**
- ✅ One-to-many transformation (Figma → multiple frameworks)
- ✅ Reusable transformation logic
- ✅ No Unity dependencies in core
- ✅ Plugin-based renderer system

[Read Full RFC →](./0022-figmasharp-core-architecture.md)

---

### RFC-0023: FigmaSharp Layout Transformation
**Status:** 📝 Draft
**Date:** 2025-10-04
**Priority:** P1 (High)
**Category:** architecture, ui, figma, layout
**Estimated Effort:** 2 weeks

**Summary:** Define the layout transformation algorithm for converting Figma's auto-layout system to framework-agnostic layout data. Ports D.A. Assets' layout calculation logic to plain C# and adapts it for multi-framework support.

**Key Algorithms:**
- Global rect calculation (position + size)
- Auto-layout transformation (horizontal/vertical/wrap)
- SPACE_BETWEEN distribution
- LayoutGrow and LayoutAlign handling
- Padding calculation with adjustment
- Framework-specific adapters (Terminal.Gui, Unity, Godot)

**Benefits:**
- ✅ Handles all Figma layout modes
- ✅ Supports nested auto-layouts
- ✅ Framework-agnostic output
- ✅ Pixel-to-character conversion for Terminal.Gui

[Read Full RFC →](./0023-figmasharp-layout-transformation.md)

---

### RFC-0024: FigmaSharp Plugin Integration
**Status:** 📝 Draft
**Date:** 2025-10-04
**Priority:** P1 (High)
**Category:** architecture, plugins, figma
**Estimated Effort:** 2-3 weeks

**Summary:** Integrate FigmaSharp with the WingedBean plugin architecture (RFC-0003) to enable hot-swappable UI renderers. FigmaSharp core is a framework package, while renderer implementations are plugins.

**Key Features:**
- FigmaSharp core as Tier 1/2 framework packages
- Renderer plugins as Tier 4 (hot-swappable)
- Profile-specific loading (console → Terminal.Gui, Unity → UGUI)
- Dependency resolution (core loads before renderers)
- Service composition (multiple renderers coexist)

**Benefits:**
- ✅ Hot-reload renderers without restart
- ✅ Profile-agnostic design
- ✅ Plugin discovery and dependency resolution
- ✅ Service registration via IPluginActivator

[Read Full RFC →](./0024-figmasharp-plugin-integration.md)

---

### RFC-0025: FigmaSharp Renderer Implementations
**Status:** 📝 Draft
**Date:** 2025-10-04
**Priority:** P1 (High)
**Category:** implementation, ui, figma, renderers
**Estimated Effort:** 3-4 weeks

**Summary:** Define implementation specifications for FigmaSharp renderer plugins across different UI frameworks: Terminal.Gui v2, Unity UGUI, Unity UI Toolkit, and Godot UI.

**Renderers:**
- **Terminal.Gui v2**: Character-based, manual auto-layout positioning
- **Unity UGUI**: RectTransform, LayoutGroup, full styling
- **Unity UI Toolkit**: Flexbox, USS styling, UXML generation
- **Godot UI**: Control nodes, Container layout, anchors

**Benefits:**
- ✅ Complete renderer implementations
- ✅ Framework-specific optimizations
- ✅ Handles all layout and style features
- ✅ Consistent API across frameworks

[Read Full RFC →](./0025-figmasharp-renderer-implementations.md)

---

## Future RFCs (Planned)

### RFC-0009: Dungeon Generation System (Planned)
**Priority:** MEDIUM
**Estimated Effort:** 5 days

BSP or cellular automata-based procedural dungeon generation with room/corridor systems.

### RFC-0010: PTY Dual-Mode Support (Planned)
**Priority:** MEDIUM
**Estimated Effort:** 3 days

Support both standalone Terminal.Gui and PTY/xterm.js modes with a single codebase.

### RFC-0011: Save/Load System (Planned)
**Priority:** LOW
**Estimated Effort:** 4 days

Serialize ECS world state to disk with multiple save slots support.

### RFC-0012: Content System (Items, Enemies, Loot) (Planned)
**Priority:** LOW
**Estimated Effort:** 7 days

Data-driven item database, enemy templates, loot tables, and progression systems.

---

## RFC Template

When creating a new RFC, use this structure:

```markdown
# RFC-XXXX: Title

## Status
[Draft | Proposed | Accepted | Implemented | Superseded]

## Date
YYYY-MM-DD

## Summary
Brief description (2-3 sentences)

## Motivation
### Current Problem
What problem are we solving?

### Why Now?
Why is this important?

## Proposal
### Overview
High-level description

### Detailed Design
Implementation details

### Migration Plan
How to migrate existing code

## Benefits
What do we gain?

## Risks and Mitigations
What could go wrong? How do we handle it?

## Definition of Done
Clear acceptance criteria

## Dependencies
What must be done first?

## References
Links to related docs

---

**Author:** [Name]
**Reviewers:** [Names]
**Status:** [Status]
**Priority:** [P0-P3]
**Estimated Effort:** [X days]
**Target Date:** [YYYY-MM-DD]
```

---

## Contributing

1. **Create RFC:** Copy template, fill in details
2. **Discuss:** Share with team, gather feedback
3. **Revise:** Update based on feedback
4. **Approve:** Team consensus or lead approval
5. **Implement:** Follow RFC-0004 execution plan
6. **Update:** Mark as Implemented when done

---

## Related Documentation

- [Architecture Design Docs](../design/)
- [Implementation Plans](../implementation/)
- [Test Results](../test-results/)
- [Development Guides](../development/)

---

**Last Updated:** 2025-10-03
**Maintainer:** WingedBean Architecture Team
