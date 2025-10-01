#!/usr/bin/env bash
# Script to create GitHub issues from RFC execution plans
# Usage: ./create-github-issues.sh

set -e

REPO="GiantCroissant-Lunar/winged-bean"

# Check if gh CLI is installed
if ! command -v gh &> /dev/null; then
    echo "Error: GitHub CLI (gh) is not installed."
    echo "Install it from: https://cli.github.com/"
    exit 1
fi

# Check if authenticated
if ! gh auth status &> /dev/null; then
    echo "Error: Not authenticated with GitHub CLI."
    echo "Run: gh auth login"
    exit 1
fi

echo "Creating GitHub issues for RFC-0005, RFC-0006, RFC-0007..."
echo "Repository: $REPO"
echo ""

# RFC-0005: Target Framework Compliance (#23-#48)
echo "=== Creating RFC-0005 Issues (#23-#48) ==="

gh issue create --repo "$REPO" --title "Update WingedBean.Contracts.Core to netstandard2.1" \
  --body "**RFC:** RFC-0005
**Phase:** 1 - Tier 1 Contracts
**Wave:** 1.1 (PARALLEL)
**Depends on:** RFC-0005 approved
**Time estimate:** 30 minutes
**Priority:** 🔴 CRITICAL

## Description
Update WingedBean.Contracts.Core project to target netstandard2.1 for Unity/Godot compatibility.

## Tasks
- [ ] Update .csproj TargetFramework to netstandard2.1
- [ ] Clean build
- [ ] Verify no compilation errors

## Success Criteria
- ✅ Project targets netstandard2.1
- ✅ Clean build succeeds
- ✅ No Unity-incompatible APIs used"

gh issue create --repo "$REPO" --title "Update WingedBean.Contracts.Config to netstandard2.1" \
  --body "**RFC:** RFC-0005
**Phase:** 1 - Tier 1 Contracts
**Wave:** 1.1 (PARALLEL)
**Depends on:** #23
**Time estimate:** 30 minutes

## Description
Update WingedBean.Contracts.Config project to target netstandard2.1.

## Tasks
- [ ] Update .csproj TargetFramework to netstandard2.1
- [ ] Clean build
- [ ] Verify reference to WingedBean.Contracts.Core

## Success Criteria
- ✅ Project targets netstandard2.1
- ✅ Clean build succeeds"

gh issue create --repo "$REPO" --title "Update WingedBean.Contracts.Audio to netstandard2.1" \
  --body "**RFC:** RFC-0005
**Phase:** 1 - Tier 1 Contracts
**Wave:** 1.1 (PARALLEL)
**Depends on:** #23
**Time estimate:** 30 minutes

## Tasks
- [ ] Update .csproj TargetFramework to netstandard2.1
- [ ] Clean build"

gh issue create --repo "$REPO" --title "Update WingedBean.Contracts.Resource to netstandard2.1" \
  --body "**RFC:** RFC-0005
**Phase:** 1 - Tier 1 Contracts
**Wave:** 1.1 (PARALLEL)
**Depends on:** #23
**Time estimate:** 30 minutes

## Tasks
- [ ] Update .csproj TargetFramework to netstandard2.1
- [ ] Clean build"

gh issue create --repo "$REPO" --title "Update WingedBean.Contracts.WebSocket to netstandard2.1" \
  --body "**RFC:** RFC-0005
**Phase:** 1 - Tier 1 Contracts
**Wave:** 1.1 (PARALLEL)
**Depends on:** #23
**Time estimate:** 30 minutes

## Tasks
- [ ] Update .csproj TargetFramework to netstandard2.1
- [ ] Clean build"

gh issue create --repo "$REPO" --title "Update WingedBean.Contracts.TerminalUI to netstandard2.1" \
  --body "**RFC:** RFC-0005
**Phase:** 1 - Tier 1 Contracts
**Wave:** 1.1 (PARALLEL)
**Depends on:** #23
**Time estimate:** 30 minutes

## Tasks
- [ ] Update .csproj TargetFramework to netstandard2.1
- [ ] Clean build"

gh issue create --repo "$REPO" --title "Update WingedBean.Contracts.Pty to netstandard2.1" \
  --body "**RFC:** RFC-0005
**Phase:** 1 - Tier 1 Contracts
**Wave:** 1.1 (PARALLEL)
**Depends on:** #23
**Time estimate:** 30 minutes

## Tasks
- [ ] Update .csproj TargetFramework to netstandard2.1
- [ ] Clean build"

gh issue create --repo "$REPO" --title "Verify all Tier 1 contracts build together" \
  --body "**RFC:** RFC-0005
**Phase:** 1 - Tier 1 Contracts
**Wave:** 1.2 (SERIAL)
**Depends on:** #23-#29
**Time estimate:** 30 minutes
**Priority:** 🔴 BLOCKS Phase 2

## Description
Build all 7 contract projects together to verify compatibility.

## Tasks
- [ ] dotnet build all contract projects
- [ ] Verify no dependency conflicts
- [ ] Check for Unity-incompatible APIs

## Success Criteria
- ✅ All 7 projects build successfully
- ✅ No warnings related to framework targeting"

gh issue create --repo "$REPO" --title "Update WingedBean.Registry to netstandard2.1" \
  --body "**RFC:** RFC-0005
**Phase:** 2 - Tier 2 Registry
**Wave:** 2.1 (SERIAL)
**Depends on:** #30
**Time estimate:** 30 minutes
**Priority:** 🔴 BLOCKS Phase 3

## Description
Update ActualRegistry project to target netstandard2.1.

## Tasks
- [ ] Update .csproj TargetFramework to netstandard2.1
- [ ] Verify references only Tier 1 contracts
- [ ] Clean build

## Success Criteria
- ✅ Project targets netstandard2.1
- ✅ ZERO references to Tier 3/4 projects
- ✅ Clean build succeeds"

gh issue create --repo "$REPO" --title "Update WingedBean.Plugins.Config to net8.0" \
  --body "**RFC:** RFC-0005
**Phase:** 3 - Tier 3 Plugins
**Wave:** 3.1 (PARALLEL)
**Depends on:** #31
**Time estimate:** 30 minutes

## Tasks
- [ ] Update .csproj TargetFramework to net8.0
- [ ] Clean build
- [ ] Verify references only Tier 1 contracts"

gh issue create --repo "$REPO" --title "Update WingedBean.Plugins.WebSocket to net8.0" \
  --body "**RFC:** RFC-0005
**Phase:** 3 - Tier 3 Plugins
**Wave:** 3.1 (PARALLEL)
**Depends on:** #31
**Time estimate:** 30 minutes

## Tasks
- [ ] Update .csproj TargetFramework to net8.0
- [ ] Clean build"

gh issue create --repo "$REPO" --title "Update WingedBean.Plugins.TerminalUI to net8.0" \
  --body "**RFC:** RFC-0005
**Phase:** 3 - Tier 3 Plugins
**Wave:** 3.1 (PARALLEL)
**Depends on:** #31
**Time estimate:** 30 minutes

## Tasks
- [ ] Update .csproj TargetFramework to net8.0
- [ ] Clean build"

gh issue create --repo "$REPO" --title "Update WingedBean.Plugins.PtyService to net8.0" \
  --body "**RFC:** RFC-0005
**Phase:** 3 - Tier 3 Plugins
**Wave:** 3.1 (PARALLEL)
**Depends on:** #31
**Time estimate:** 30 minutes

## Tasks
- [ ] Update .csproj TargetFramework to net8.0
- [ ] Clean build"

gh issue create --repo "$REPO" --title "Update WingedBean.Plugins.AsciinemaRecorder to net8.0" \
  --body "**RFC:** RFC-0005
**Phase:** 3 - Tier 3 Plugins
**Wave:** 3.1 (PARALLEL)
**Depends on:** #31
**Time estimate:** 30 minutes

## Tasks
- [ ] Update .csproj TargetFramework to net8.0
- [ ] Clean build"

gh issue create --repo "$REPO" --title "Update WingedBean.Plugins.ConsoleDungeon to net8.0" \
  --body "**RFC:** RFC-0005
**Phase:** 3 - Tier 3 Plugins
**Wave:** 3.1 (PARALLEL)
**Depends on:** #31
**Time estimate:** 30 minutes

## Tasks
- [ ] Update .csproj TargetFramework to net8.0
- [ ] Clean build"

gh issue create --repo "$REPO" --title "Update WingedBean.Providers.AssemblyContext to net8.0" \
  --body "**RFC:** RFC-0005
**Phase:** 3 - Tier 4 Providers
**Wave:** 3.2 (PARALLEL)
**Depends on:** #31
**Time estimate:** 30 minutes

## Tasks
- [ ] Update .csproj TargetFramework to net8.0
- [ ] Verify references only logging
- [ ] Clean build"

gh issue create --repo "$REPO" --title "Update ConsoleDungeon.Host to net8.0" \
  --body "**RFC:** RFC-0005
**Phase:** 3 - Tier 4 Host
**Wave:** 3.3 (PARALLEL)
**Depends on:** #31
**Time estimate:** 30 minutes

## Tasks
- [ ] Update .csproj TargetFramework to net8.0
- [ ] Clean build"

gh issue create --repo "$REPO" --title "Verify entire solution builds with correct targets" \
  --body "**RFC:** RFC-0005
**Phase:** 3 - Verification
**Wave:** 3.4 (SERIAL)
**Depends on:** #32-#40
**Time estimate:** 30 minutes
**Priority:** 🔴 MUST PASS

## Description
Build entire WingedBean.sln to verify framework targeting.

## Tasks
- [ ] dotnet clean
- [ ] dotnet build WingedBean.sln
- [ ] Verify no framework conflicts

## Success Criteria
- ✅ Solution builds successfully
- ✅ No framework targeting warnings"

gh issue create --repo "$REPO" --title "Create WingedBean.SourceGenerators.Proxy project" \
  --body "**RFC:** RFC-0005
**Phase:** 4 - Source Generators
**Wave:** 4.1 (SERIAL)
**Depends on:** #41
**Time estimate:** 1 hour

## Description
Create new source generator project for proxy services.

## Tasks
- [ ] Create project targeting netstandard2.0
- [ ] Add Roslyn references
- [ ] Create project structure
- [ ] Add to solution

## Success Criteria
- ✅ Project created
- ✅ Targets netstandard2.0
- ✅ Builds successfully"

gh issue create --repo "$REPO" --title "Implement proxy service source generator" \
  --body "**RFC:** RFC-0005
**Phase:** 4 - Source Generators
**Wave:** 4.2 (SERIAL)
**Depends on:** #42
**Time estimate:** 3 hours

## Description
Implement Roslyn source generator for proxy services.

## Tasks
- [ ] Implement ISourceGenerator
- [ ] Generate proxy classes for cross-tier calls
- [ ] Add diagnostics
- [ ] Unit tests

## Success Criteria
- ✅ Generator produces valid code
- ✅ Proxies compile
- ✅ Tests pass"

gh issue create --repo "$REPO" --title "Integrate source generator into plugins" \
  --body "**RFC:** RFC-0005
**Phase:** 4 - Source Generators
**Wave:** 4.3 (SERIAL)
**Depends on:** #43
**Time estimate:** 1 hour

## Description
Add source generator references to plugin projects.

## Tasks
- [ ] Add analyzer reference to plugin projects
- [ ] Verify generated code appears
- [ ] Test proxy service calls

## Success Criteria
- ✅ All plugins reference generator
- ✅ Proxies generated correctly"

gh issue create --repo "$REPO" --title "Run full solution build verification" \
  --body "**RFC:** RFC-0005
**Phase:** 5 - Final Verification
**Wave:** 5.1 (SERIAL)
**Depends on:** #44
**Time estimate:** 30 minutes
**Priority:** 🔴 CRITICAL TEST

## Tasks
- [ ] Clean solution
- [ ] Build solution
- [ ] Verify all framework targets correct

## Success Criteria
- ✅ Solution builds
- ✅ No warnings"

gh issue create --repo "$REPO" --title "Run ConsoleDungeon.Host and verify no regressions" \
  --body "**RFC:** RFC-0005
**Phase:** 5 - Final Verification
**Wave:** 5.2 (SERIAL)
**Depends on:** #45
**Time estimate:** 30 minutes
**Priority:** 🔴 CRITICAL TEST

## Tasks
- [ ] Run ConsoleDungeon.Host
- [ ] Verify app starts
- [ ] Verify all services load

## Success Criteria
- ✅ App runs without errors
- ✅ All plugins load"

gh issue create --repo "$REPO" --title "Verify xterm.js integration still works" \
  --body "**RFC:** RFC-0005
**Phase:** 5 - Final Verification
**Wave:** 5.3 (SERIAL)
**Depends on:** #46
**Time estimate:** 30 minutes
**Priority:** 🔴 CRITICAL TEST

## Tasks
- [ ] Start ConsoleDungeon.Host
- [ ] Start Astro frontend
- [ ] Connect via xterm.js
- [ ] Verify Terminal.Gui renders

## Success Criteria
- ✅ xterm.js connects
- ✅ Terminal.Gui visible
- ✅ Commands work"

gh issue create --repo "$REPO" --title "Update RFC-0005 documentation" \
  --body "**RFC:** RFC-0005
**Phase:** 6 - Documentation
**Wave:** 6.1 (PARALLEL)
**Depends on:** #47
**Time estimate:** 1 hour

## Tasks
- [ ] Update framework targeting guide
- [ ] Document source generator usage
- [ ] Update architecture diagrams
- [ ] Mark RFC-0005 as implemented

## Success Criteria
- ✅ Documentation complete
- ✅ RFC-0005 status updated"

echo ""
echo "=== Creating RFC-0006 Issues (#49-#62) ==="

gh issue create --repo "$REPO" --title "Create plugin configuration models" \
  --body "**RFC:** RFC-0006
**Phase:** 1 - Configuration Infrastructure
**Wave:** 1.1 (SERIAL)
**Depends on:** #48
**Time estimate:** 1 hour
**Priority:** 🔴 BLOCKS all Phase 1

## Description
Create models for plugin configuration system.

## Tasks
- [ ] Create PluginConfiguration class
- [ ] Create PluginDescriptor class
- [ ] Create LoadStrategy enum
- [ ] Add JSON serialization support

## Success Criteria
- ✅ Models compile
- ✅ Can serialize/deserialize JSON"

gh issue create --repo "$REPO" --title "Create plugins.json for ConsoleDungeon.Host" \
  --body "**RFC:** RFC-0006
**Phase:** 1 - Configuration Infrastructure
**Wave:** 1.2 (PARALLEL)
**Depends on:** #49
**Time estimate:** 45 minutes

## Description
Create main plugins.json configuration file.

## Tasks
- [ ] Create plugins.json
- [ ] Define entries for all plugins
- [ ] Add copy-to-output in .csproj

## Success Criteria
- ✅ plugins.json exists
- ✅ All current plugins listed
- ✅ Copied to output directory"

gh issue create --repo "$REPO" --title "Create .plugin.json manifests for all plugins" \
  --body "**RFC:** RFC-0006
**Phase:** 1 - Configuration Infrastructure
**Wave:** 1.2 (PARALLEL)
**Depends on:** #49
**Time estimate:** 1 hour

## Description
Create .plugin.json manifest for each plugin.

## Tasks
- [ ] Config plugin manifest
- [ ] WebSocket plugin manifest
- [ ] TerminalUI plugin manifest
- [ ] PtyService plugin manifest
- [ ] AsciinemaRecorder manifest
- [ ] Add copy-to-output for each

## Success Criteria
- ✅ All 5+ manifests created
- ✅ Copied to output"

gh issue create --repo "$REPO" --title "Create copy-plugins.targets MSBuild file" \
  --body "**RFC:** RFC-0006
**Phase:** 2 - MSBuild Integration
**Wave:** 2.1 (SERIAL)
**Depends on:** #49
**Time estimate:** 1 hour
**Priority:** 🔴 BLOCKS #55

## Description
Create MSBuild targets file for plugin deployment.

## Tasks
- [ ] Create copy-plugins.targets
- [ ] Copy plugin DLLs to output/plugins/
- [ ] Copy PDBs for debugging
- [ ] Copy manifests
- [ ] Import in Host.csproj

## Success Criteria
- ✅ Targets file works
- ✅ plugins/ directory created
- ✅ All plugin files copied"

gh issue create --repo "$REPO" --title "Remove static plugin references from ConsoleDungeon.Host" \
  --body "**RFC:** RFC-0006
**Phase:** 3 - Remove Static References
**Wave:** 3.1 (SERIAL)
**Depends on:** #52
**Time estimate:** 30 minutes
**Priority:** 🔴 CRITICAL

## Description
Remove ProjectReference entries for plugins.

## Tasks
- [ ] Remove plugin ProjectReferences
- [ ] Keep Registry, PluginLoader, AssemblyContext
- [ ] Verify host still builds

## Success Criteria
- ✅ No plugin references in Host.csproj
- ✅ Host builds successfully"

gh issue create --repo "$REPO" --title "Implement dynamic plugin loading in Program.cs" \
  --body "**RFC:** RFC-0006
**Phase:** 4 - Dynamic Loading
**Wave:** 4.1 (SERIAL)
**Depends on:** #49, #50, #51, #52, #53
**Time estimate:** 2 hours
**Priority:** 🔴 BLOCKS #55

## Description
Refactor Program.cs to load plugins dynamically.

## Tasks
- [ ] Load plugins.json
- [ ] Use ActualPluginLoader
- [ ] Auto-register services
- [ ] Service verification
- [ ] Error handling

## Success Criteria
- ✅ Plugins load from JSON
- ✅ Services register correctly
- ✅ Error messages helpful"

gh issue create --repo "$REPO" --title "Verify ConsoleDungeon.Host builds with dynamic loading" \
  --body "**RFC:** RFC-0006
**Phase:** 5 - Testing
**Wave:** 5.1 (SERIAL)
**Depends on:** #54
**Time estimate:** 30 minutes
**Priority:** 🔴 MUST PASS

## Tasks
- [ ] Clean build
- [ ] Verify plugins/ directory
- [ ] Verify plugins.json copied

## Success Criteria
- ✅ Build succeeds
- ✅ Plugin files present"

gh issue create --repo "$REPO" --title "Verify dynamic plugin loading works at runtime" \
  --body "**RFC:** RFC-0006
**Phase:** 5 - Testing
**Wave:** 5.2 (SERIAL)
**Depends on:** #55
**Time estimate:** 1 hour
**Priority:** 🔴 CRITICAL TEST

## Tasks
- [ ] Run ConsoleDungeon.Host
- [ ] Verify all plugins load
- [ ] Verify services register
- [ ] Check error handling

## Success Criteria
- ✅ All plugins load
- ✅ No errors
- ✅ Services available"

gh issue create --repo "$REPO" --title "Verify xterm.js integration after dynamic loading" \
  --body "**RFC:** RFC-0006
**Phase:** 5 - Testing
**Wave:** 5.3 (SERIAL)
**Depends on:** #56
**Time estimate:** 30 minutes
**Priority:** 🔴 CRITICAL TEST

## Tasks
- [ ] Start Host + Astro
- [ ] Connect xterm.js
- [ ] Verify rendering
- [ ] Test commands

## Success Criteria
- ✅ xterm.js works
- ✅ No regressions"

gh issue create --repo "$REPO" --title "Test plugin enable/disable functionality" \
  --body "**RFC:** RFC-0006
**Phase:** 6 - Config Testing
**Wave:** 6.1 (SERIAL)
**Depends on:** #57
**Time estimate:** 45 minutes

## Tasks
- [ ] Disable plugin in JSON
- [ ] Verify error message
- [ ] Re-enable plugin
- [ ] Verify works again

## Success Criteria
- ✅ Enable/disable works
- ✅ Helpful error messages"

gh issue create --repo "$REPO" --title "Test plugin priority and load order" \
  --body "**RFC:** RFC-0006
**Phase:** 6 - Config Testing
**Wave:** 6.2 (SERIAL)
**Depends on:** #58
**Time estimate:** 30 minutes

## Tasks
- [ ] Change priorities
- [ ] Verify load order
- [ ] Verify highest priority selected

## Success Criteria
- ✅ Priority system works"

gh issue create --repo "$REPO" --title "Create plugin development guide" \
  --body "**RFC:** RFC-0006
**Phase:** 7 - Documentation
**Wave:** 7.1 (PARALLEL)
**Depends on:** #59
**Time estimate:** 1 hour

## Tasks
- [ ] Document plugin creation
- [ ] Manifest format docs
- [ ] Registration patterns

## Success Criteria
- ✅ Guide published"

gh issue create --repo "$REPO" --title "Update architecture documentation for dynamic loading" \
  --body "**RFC:** RFC-0006
**Phase:** 7 - Documentation
**Wave:** 7.1 (PARALLEL)
**Depends on:** #59
**Time estimate:** 45 minutes

## Tasks
- [ ] Architecture diagram
- [ ] Plugin lifecycle docs
- [ ] Configuration schema

## Success Criteria
- ✅ Docs updated"

gh issue create --repo "$REPO" --title "Create plugin configuration migration guide" \
  --body "**RFC:** RFC-0006
**Phase:** 7 - Documentation
**Wave:** 7.1 (PARALLEL)
**Depends on:** #59
**Time estimate:** 30 minutes

## Tasks
- [ ] Migration from static to dynamic
- [ ] Troubleshooting guide
- [ ] Best practices

## Success Criteria
- ✅ Migration guide complete
- ✅ RFC-0006 marked implemented"

echo ""
echo "=== Creating RFC-0007 Issues (#63-#102) ==="

gh issue create --repo "$REPO" --title "Create WingedBean.Contracts.ECS project" \
  --body "**RFC:** RFC-0007
**Phase:** 1 - Contract Layer
**Wave:** 1.1 (PARALLEL)
**Depends on:** #48, #62
**Time estimate:** 30 minutes
**Priority:** 🔴 BLOCKS all Phase 1

## Description
Create new Tier 1 contract project for ECS abstraction.

## Tasks
- [ ] Create project targeting netstandard2.1
- [ ] Add project structure
- [ ] Add to solution
- [ ] Create README

## Success Criteria
- ✅ Project created
- ✅ Targets netstandard2.1
- ✅ Builds successfully"

gh issue create --repo "$REPO" --title "Define IECSService interface" \
  --body "**RFC:** RFC-0007
**Phase:** 1 - Contract Layer
**Wave:** 1.1 (PARALLEL)
**Depends on:** #63
**Time estimate:** 45 minutes
**Priority:** 🔴 BLOCKS Phase 2

## Tasks
- [ ] Define IECSService interface
- [ ] CreateWorld method
- [ ] DestroyWorld method
- [ ] GetWorld method
- [ ] XML documentation

## Success Criteria
- ✅ Interface complete
- ✅ Compiles"

gh issue create --repo "$REPO" --title "Define IWorld interface" \
  --body "**RFC:** RFC-0007
**Phase:** 1 - Contract Layer
**Wave:** 1.1 (PARALLEL)
**Depends on:** #63
**Time estimate:** 30 minutes
**Priority:** 🔴 BLOCKS Phase 2

## Tasks
- [ ] Define IWorld interface
- [ ] CreateEntity method
- [ ] DestroyEntity method
- [ ] AttachComponent/DetachComponent
- [ ] CreateQuery method

## Success Criteria
- ✅ Interface complete"

gh issue create --repo "$REPO" --title "Define IEntity interface" \
  --body "**RFC:** RFC-0007
**Phase:** 1 - Contract Layer
**Wave:** 1.1 (PARALLEL)
**Depends on:** #63
**Time estimate:** 30 minutes
**Priority:** 🔴 BLOCKS Phase 2

## Tasks
- [ ] Define IEntity interface
- [ ] Entity ID property
- [ ] IsAlive property
- [ ] Get/Set component methods

## Success Criteria
- ✅ Interface complete"

gh issue create --repo "$REPO" --title "Define IQuery interface" \
  --body "**RFC:** RFC-0007
**Phase:** 1 - Contract Layer
**Wave:** 1.1 (PARALLEL)
**Depends on:** #63
**Time estimate:** 30 minutes
**Priority:** 🔴 BLOCKS Phase 2

## Tasks
- [ ] Define IQuery interface
- [ ] ForEach methods
- [ ] Query execution

## Success Criteria
- ✅ Interface complete"

gh issue create --repo "$REPO" --title "Define ISystem interface" \
  --body "**RFC:** RFC-0007
**Phase:** 1 - Contract Layer
**Wave:** 1.1 (PARALLEL)
**Depends on:** #63
**Time estimate:** 30 minutes
**Priority:** 🔴 BLOCKS Phase 3

## Tasks
- [ ] Define ISystem interface
- [ ] Initialize method
- [ ] Update method
- [ ] System lifecycle

## Success Criteria
- ✅ Interface complete"

gh issue create --repo "$REPO" --title "Create WingedBean.Plugins.ArchECS project" \
  --body "**RFC:** RFC-0007
**Phase:** 2 - Arch Plugin
**Wave:** 2.1 (SERIAL)
**Depends on:** #63-#68
**Time estimate:** 30 minutes
**Priority:** 🔴 BLOCKS Phase 2 impl

## Description
Create Tier 3 plugin project for Arch ECS.

## Tasks
- [ ] Create project targeting net8.0
- [ ] Reference Arch 1.3.0
- [ ] Reference WingedBean.Contracts.ECS
- [ ] Add to solution

## Success Criteria
- ✅ Project created
- ✅ Arch referenced
- ✅ Builds"

gh issue create --repo "$REPO" --title "Implement ArchECSService" \
  --body "**RFC:** RFC-0007
**Phase:** 2 - Arch Plugin
**Wave:** 2.2 (PARALLEL)
**Depends on:** #69
**Time estimate:** 1 hour
**Priority:** 🔴 BLOCKS #82

## Description
Implement IECSService using Arch.

## Tasks
- [ ] Implement IECSService
- [ ] World management
- [ ] Service registration

## Success Criteria
- ✅ Service implemented
- ✅ Tests pass"

gh issue create --repo "$REPO" --title "Implement ArchWorld adapter" \
  --body "**RFC:** RFC-0007
**Phase:** 2 - Arch Plugin
**Wave:** 2.2 (PARALLEL)
**Depends on:** #69
**Time estimate:** 45 minutes

## Description
Adapter from IWorld to Arch.Core.World.

## Tasks
- [ ] Implement IWorld
- [ ] Entity creation wrapper
- [ ] Query builder wrapper

## Success Criteria
- ✅ Adapter complete"

gh issue create --repo "$REPO" --title "Implement ArchEntity adapter" \
  --body "**RFC:** RFC-0007
**Phase:** 2 - Arch Plugin
**Wave:** 2.2 (PARALLEL)
**Depends on:** #69
**Time estimate:** 30 minutes

## Description
Adapter from IEntity to Arch EntityReference.

## Tasks
- [ ] Implement IEntity
- [ ] Component access wrapper

## Success Criteria
- ✅ Adapter complete"

gh issue create --repo "$REPO" --title "Implement ArchQuery adapter" \
  --body "**RFC:** RFC-0007
**Phase:** 2 - Arch Plugin
**Wave:** 2.2 (PARALLEL)
**Depends on:** #69
**Time estimate:** 45 minutes

## Description
Adapter from IQuery to Arch QueryDescription.

## Tasks
- [ ] Implement IQuery
- [ ] ForEach delegation

## Success Criteria
- ✅ Adapter complete"

gh issue create --repo "$REPO" --title "Define core components (Position, Stats, Renderable)" \
  --body "**RFC:** RFC-0007
**Phase:** 3 - Game Components
**Wave:** 3.1 (PARALLEL)
**Depends on:** #69
**Time estimate:** 45 minutes

## Description
Define core game components as structs.

## Tasks
- [ ] Position component (X, Y, Z)
- [ ] Stats component (HP, Attack, Defense)
- [ ] Renderable component (Character, Color, Layer)

## Success Criteria
- ✅ Components defined
- ✅ Structs for performance"

gh issue create --repo "$REPO" --title "Define entity components (Player, Enemy, Item)" \
  --body "**RFC:** RFC-0007
**Phase:** 3 - Game Components
**Wave:** 3.1 (PARALLEL)
**Depends on:** #69
**Time estimate:** 30 minutes

## Tasks
- [ ] Player marker component
- [ ] Enemy component (AIType, AggroRange)
- [ ] Item component (ItemType, Stackable)

## Success Criteria
- ✅ Components defined"

gh issue create --repo "$REPO" --title "Define inventory/combat components" \
  --body "**RFC:** RFC-0007
**Phase:** 3 - Game Components
**Wave:** 3.1 (PARALLEL)
**Depends on:** #69
**Time estimate:** 30 minutes

## Tasks
- [ ] Inventory component
- [ ] CombatState component
- [ ] Movement component

## Success Criteria
- ✅ Components defined"

gh issue create --repo "$REPO" --title "Create SystemBase abstract class" \
  --body "**RFC:** RFC-0007
**Phase:** 4 - Game Systems
**Wave:** 4.1 (SERIAL)
**Depends on:** #68, #69
**Time estimate:** 30 minutes
**Priority:** 🔴 BLOCKS all systems

## Description
Create base class for all game systems.

## Tasks
- [ ] Implement ISystem
- [ ] World reference
- [ ] Query caching

## Success Criteria
- ✅ Base class complete
- ✅ Reusable by systems"

gh issue create --repo "$REPO" --title "Implement MovementSystem" \
  --body "**RFC:** RFC-0007
**Phase:** 4 - Game Systems
**Wave:** 4.2 (PARALLEL)
**Depends on:** #77
**Time estimate:** 1 hour

## Description
System to update entity positions.

## Tasks
- [ ] Query Position + Movement
- [ ] Update positions
- [ ] Collision detection hooks

## Success Criteria
- ✅ System implemented
- ✅ Movement works"

gh issue create --repo "$REPO" --title "Implement RenderSystem" \
  --body "**RFC:** RFC-0007
**Phase:** 4 - Game Systems
**Wave:** 4.2 (PARALLEL)
**Depends on:** #77
**Time estimate:** 1.5 hours
**Priority:** 🔴 CRITICAL - Terminal.Gui integration

## Description
System to render entities via Terminal.Gui.

## Tasks
- [ ] Query Position + Renderable
- [ ] Terminal.Gui integration
- [ ] Layer sorting

## Success Criteria
- ✅ Rendering works
- ✅ xterm.js compatible"

gh issue create --repo "$REPO" --title "Implement CombatSystem" \
  --body "**RFC:** RFC-0007
**Phase:** 4 - Game Systems
**Wave:** 4.2 (PARALLEL)
**Depends on:** #77
**Time estimate:** 1.5 hours

## Description
System to handle combat logic.

## Tasks
- [ ] Query CombatState + Stats
- [ ] Damage calculation
- [ ] HP updates
- [ ] Death handling

## Success Criteria
- ✅ Combat works
- ✅ Death handled"

gh issue create --repo "$REPO" --title "Implement AISystem" \
  --body "**RFC:** RFC-0007
**Phase:** 4 - Game Systems
**Wave:** 4.2 (PARALLEL)
**Depends on:** #77
**Time estimate:** 2 hours

## Description
System for enemy AI.

## Tasks
- [ ] Query Enemy + Position + Stats
- [ ] Pathfinding to player
- [ ] Attack logic
- [ ] State machine

## Success Criteria
- ✅ AI behaves correctly
- ✅ Enemies move toward player"

gh issue create --repo "$REPO" --title "Create ArchECSPlugin class" \
  --body "**RFC:** RFC-0007
**Phase:** 5 - Plugin Registration
**Wave:** 5.1 (SERIAL)
**Depends on:** #70, #77-#81
**Time estimate:** 1 hour
**Priority:** 🔴 BLOCKS #83

## Description
Create IPlugin implementation for ArchECS.

## Tasks
- [ ] Implement IPlugin
- [ ] Register ArchECSService
- [ ] Register all systems
- [ ] Plugin metadata

## Success Criteria
- ✅ Plugin complete
- ✅ Services register"

gh issue create --repo "$REPO" --title "Add ArchECS to plugins.json" \
  --body "**RFC:** RFC-0007
**Phase:** 5 - Plugin Registration
**Wave:** 5.2 (SERIAL)
**Depends on:** #82
**Time estimate:** 15 minutes
**Priority:** 🔴 BLOCKS #84

## Tasks
- [ ] Add ArchECS entry
- [ ] Set priority 100 (load early)

## Success Criteria
- ✅ Entry added"

gh issue create --repo "$REPO" --title "Create .plugin.json for ArchECS" \
  --body "**RFC:** RFC-0007
**Phase:** 5 - Plugin Registration
**Wave:** 5.3 (SERIAL)
**Depends on:** #83
**Time estimate:** 15 minutes
**Priority:** 🔴 BLOCKS #85

## Tasks
- [ ] Create manifest
- [ ] Dependency declarations

## Success Criteria
- ✅ Manifest complete"

gh issue create --repo "$REPO" --title "Integrate ECS into ConsoleDungeon plugin" \
  --body "**RFC:** RFC-0007
**Phase:** 6 - Integration
**Wave:** 6.1 (SERIAL)
**Depends on:** #84
**Time estimate:** 2 hours
**Priority:** 🔴 BLOCKS #86

## Description
Wire up ECS in ConsoleDungeon plugin.

## Tasks
- [ ] Resolve IECSService
- [ ] Create game world
- [ ] Initialize systems
- [ ] World lifecycle

## Success Criteria
- ✅ ECS integrated
- ✅ Systems initialized"

gh issue create --repo "$REPO" --title "Create player and enemy entities" \
  --body "**RFC:** RFC-0007
**Phase:** 6 - Integration
**Wave:** 6.2 (SERIAL)
**Depends on:** #85
**Time estimate:** 1.5 hours
**Priority:** 🔴 BLOCKS #87

## Tasks
- [ ] Create player entity
- [ ] Enemy spawning logic
- [ ] Item placement

## Success Criteria
- ✅ Entities created
- ✅ Components attached"

gh issue create --repo "$REPO" --title "Implement ECS game loop" \
  --body "**RFC:** RFC-0007
**Phase:** 6 - Integration
**Wave:** 6.3 (SERIAL)
**Depends on:** #86
**Time estimate:** 1 hour
**Priority:** 🔴 BLOCKS Phase 7

## Tasks
- [ ] Update systems in order
- [ ] Frame timing
- [ ] Input handling bridge

## Success Criteria
- ✅ Game loop works
- ✅ Systems execute"

gh issue create --repo "$REPO" --title "Verify ArchECS plugin builds" \
  --body "**RFC:** RFC-0007
**Phase:** 7 - Testing
**Wave:** 7.1 (SERIAL)
**Depends on:** #87
**Time estimate:** 30 minutes
**Priority:** 🔴 MUST PASS

## Tasks
- [ ] Clean build
- [ ] Verify Arch reference

## Success Criteria
- ✅ Build succeeds"

gh issue create --repo "$REPO" --title "Verify ArchECS plugin loads dynamically" \
  --body "**RFC:** RFC-0007
**Phase:** 7 - Testing
**Wave:** 7.2 (SERIAL)
**Depends on:** #88
**Time estimate:** 30 minutes
**Priority:** 🔴 CRITICAL TEST

## Tasks
- [ ] Run ConsoleDungeon.Host
- [ ] Verify ArchECS loads
- [ ] Verify IECSService registered

## Success Criteria
- ✅ Plugin loads
- ✅ Service available"

gh issue create --repo "$REPO" --title "Verify systems execute correctly" \
  --body "**RFC:** RFC-0007
**Phase:** 7 - Testing
**Wave:** 7.3 (SERIAL)
**Depends on:** #89
**Time estimate:** 1 hour
**Priority:** 🔴 CRITICAL TEST

## Tasks
- [ ] Verify MovementSystem
- [ ] Verify CombatSystem
- [ ] Verify AISystem

## Success Criteria
- ✅ All systems work"

gh issue create --repo "$REPO" --title "Verify rendering in xterm.js" \
  --body "**RFC:** RFC-0007
**Phase:** 7 - Testing
**Wave:** 7.4 (SERIAL)
**Depends on:** #90
**Time estimate:** 1 hour
**Priority:** 🔴 CRITICAL TEST

## Tasks
- [ ] Start Host + Astro
- [ ] Verify entities render
- [ ] Verify movement visible
- [ ] Verify combat effects

## Success Criteria
- ✅ Rendering works in xterm.js
- ✅ No regressions"

gh issue create --repo "$REPO" --title "Benchmark ECS performance" \
  --body "**RFC:** RFC-0007
**Phase:** 7 - Testing
**Wave:** 7.5 (SERIAL)
**Depends on:** #91
**Time estimate:** 1 hour

## Tasks
- [ ] Spawn 1000 entities
- [ ] Measure frame time
- [ ] Verify <16ms per frame (60 FPS)
- [ ] Profile bottlenecks

## Success Criteria
- ✅ 60 FPS achieved"

gh issue create --repo "$REPO" --title "Unit tests for ArchWorld adapter" \
  --body "**RFC:** RFC-0007
**Phase:** 8 - Unit Testing
**Wave:** 8.1 (PARALLEL)
**Depends on:** #92
**Time estimate:** 1 hour

## Tasks
- [ ] Entity creation tests
- [ ] Component attach/detach tests
- [ ] Query building tests

## Success Criteria
- ✅ Tests pass"

gh issue create --repo "$REPO" --title "Unit tests for ArchEntity adapter" \
  --body "**RFC:** RFC-0007
**Phase:** 8 - Unit Testing
**Wave:** 8.1 (PARALLEL)
**Depends on:** #92
**Time estimate:** 45 minutes

## Tasks
- [ ] Component access tests
- [ ] Entity lifecycle tests

## Success Criteria
- ✅ Tests pass"

gh issue create --repo "$REPO" --title "Unit tests for ArchQuery adapter" \
  --body "**RFC:** RFC-0007
**Phase:** 8 - Unit Testing
**Wave:** 8.1 (PARALLEL)
**Depends on:** #92
**Time estimate:** 45 minutes

## Tasks
- [ ] Query execution tests
- [ ] ForEach tests

## Success Criteria
- ✅ Tests pass"

gh issue create --repo "$REPO" --title "Unit tests for MovementSystem" \
  --body "**RFC:** RFC-0007
**Phase:** 8 - Unit Testing
**Wave:** 8.2 (PARALLEL)
**Depends on:** #92
**Time estimate:** 1 hour

## Tasks
- [ ] Position update tests
- [ ] Collision tests

## Success Criteria
- ✅ Tests pass"

gh issue create --repo "$REPO" --title "Unit tests for CombatSystem" \
  --body "**RFC:** RFC-0007
**Phase:** 8 - Unit Testing
**Wave:** 8.2 (PARALLEL)
**Depends on:** #92
**Time estimate:** 1.5 hours

## Tasks
- [ ] Damage calculation tests
- [ ] Death handling tests

## Success Criteria
- ✅ Tests pass"

gh issue create --repo "$REPO" --title "Unit tests for AISystem" \
  --body "**RFC:** RFC-0007
**Phase:** 8 - Unit Testing
**Wave:** 8.2 (PARALLEL)
**Depends on:** #92
**Time estimate:** 1.5 hours

## Tasks
- [ ] Pathfinding tests
- [ ] Attack logic tests

## Success Criteria
- ✅ Tests pass"

gh issue create --repo "$REPO" --title "Unit tests for RenderSystem" \
  --body "**RFC:** RFC-0007
**Phase:** 8 - Unit Testing
**Wave:** 8.2 (PARALLEL)
**Depends on:** #92
**Time estimate:** 1 hour

## Tasks
- [ ] Rendering tests
- [ ] Layer sorting tests

## Success Criteria
- ✅ Tests pass"

gh issue create --repo "$REPO" --title "Create ECS architecture guide" \
  --body "**RFC:** RFC-0007
**Phase:** 9 - Documentation
**Wave:** 9.1 (PARALLEL)
**Depends on:** #99
**Time estimate:** 2 hours

## Tasks
- [ ] Component design patterns
- [ ] System implementation guide
- [ ] Query optimization tips
- [ ] Arch best practices

## Success Criteria
- ✅ Guide complete"

gh issue create --repo "$REPO" --title "Create game entity guide" \
  --body "**RFC:** RFC-0007
**Phase:** 9 - Documentation
**Wave:** 9.1 (PARALLEL)
**Depends on:** #99
**Time estimate:** 1 hour

## Tasks
- [ ] How to define components
- [ ] How to create systems
- [ ] Entity lifecycle management

## Success Criteria
- ✅ Guide complete"

gh issue create --repo "$REPO" --title "Update dungeon crawler roadmap" \
  --body "**RFC:** RFC-0007
**Phase:** 9 - Documentation
**Wave:** 9.1 (PARALLEL)
**Depends on:** #99
**Time estimate:** 30 minutes

## Tasks
- [ ] Mark Phase 1 complete
- [ ] Celebrate ECS integration 🎉
- [ ] Document next steps (Phase 2: Map Generation)

## Success Criteria
- ✅ Roadmap updated
- ✅ RFC-0007 marked implemented"

echo ""
echo "✅ All 80 GitHub issues created successfully!"
echo ""
echo "Summary:"
echo "- RFC-0005: Issues #23-#48 (26 issues) - Target Framework Compliance"
echo "- RFC-0006: Issues #49-#62 (14 issues) - Dynamic Plugin Loading"
echo "- RFC-0007: Issues #63-#102 (40 issues) - Arch ECS Integration"
echo ""
echo "Next steps:"
echo "1. Review issues in GitHub"
echo "2. Start with RFC-0005 (CRITICAL PATH)"
echo "3. Follow execution plans in docs/implementation/"
