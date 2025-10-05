# RFC-0036 Implementation - Action Checklist

**Status**: In Progress  
**Priority**: High  
**Date**: 2025-10-05

---

## Critical Actions (Do First) 🔥

### 1. Commit Uncommitted Work
- [ ] Review all uncommitted changes in git status
- [ ] Stage new hosting contract projects
- [ ] Stage new hosting implementation projects
- [ ] Create comprehensive commit message (see template below)
- [ ] Commit with `git commit -F <file>` per R-GIT-010
- [ ] Push to feature branch `feature/rfc-0036-platform-hosting`

**Commit Message Template:**
```
feat: Implement RFC-0036 platform-agnostic hosting abstraction

Add unified hosting pattern for Console, Unity, and Godot platforms through
IWingedBeanApp abstraction and platform-specific host implementations.

New Contracts:
- WingedBean.Contracts.Hosting: IWingedBeanApp, IWingedBeanHost, IWingedBeanHostBuilder
- WingedBean.Contracts.UI: IUIApp with platform-agnostic input/rendering
- WingedBean.Contracts.TerminalUI: ITerminalApp extending IUIApp (migrated from Terminal)

New Implementations:
- WingedBean.Hosting: Factory with auto-detection for platform selection
- WingedBean.Hosting.Console: .NET Generic Host wrapper
- WingedBean.Hosting.Unity: Unity lifecycle bridge
- WingedBean.Hosting.Godot: Godot lifecycle bridge

Breaking Changes:
- WingedBean.Contracts.Terminal → WingedBean.Contracts.TerminalUI (namespace change)
- ITerminalApp now extends IUIApp (additive, requires new implementations)

Known Issues:
- Unity/Godot hosts need MonoBehaviour/Node integration (tracked in issue #XXX)
- 3 compiler warnings to be addressed
- No tests yet (tracked in issue #XXX)

Related: RFC-0036, RFC-0029

Co-Authored-By: [Agent Name] <noreply@[agent].com>
```

### 2. Fix Compiler Warnings
- [ ] **Unity Host**: Address CS1998 in `UpdateAsync`
  - Option A: Add comment explaining fire-and-forget design
  - Option B: Refactor to synchronous with proper async handling
- [ ] **Godot Host**: Address CS1998 in `ProcessAsync`
  - Same as Unity
- [ ] **Godot Host**: Remove unused `_configureConfig` field (CS0169)
- [ ] Rebuild all projects to verify warnings are fixed
- [ ] Commit fixes with message: `fix: resolve compiler warnings in Unity/Godot hosts`

### 3. Verify Build Success
- [ ] Build entire Framework solution: `dotnet build development/dotnet/framework/Framework.sln`
- [ ] Verify no errors
- [ ] Verify warning count reduced to 0
- [ ] Document any remaining warnings with justification

---

## High Priority Actions (This Week) ⚡

### 4. Create Basic Test Suite
- [ ] Create `WingedBean.Contracts.Hosting.Tests` project
  - [ ] Test `IWingedBeanApp` lifecycle state transitions
  - [ ] Test `AppStateChangedEventArgs` event firing
  - [ ] Test interface contracts
- [ ] Create `WingedBean.Hosting.Console.Tests` project
  - [ ] Test `ConsoleWingedBeanHostBuilder` configuration
  - [ ] Test host lifecycle (StartAsync, StopAsync, RunAsync)
  - [ ] Test service provider integration
  - [ ] Test graceful shutdown
- [ ] Create `WingedBean.Hosting.Tests` project
  - [ ] Test factory auto-detection for Unity
  - [ ] Test factory auto-detection for Godot
  - [ ] Test factory defaults to Console
  - [ ] Test explicit builder creation
- [ ] Run all tests: `dotnet test development/dotnet/framework/Framework.sln`
- [ ] Commit with message: `test: add unit tests for hosting contracts and console host`

### 5. Update RFC-0036 Status
- [ ] Open `docs/rfcs/0036-platform-agnostic-hosting-abstraction.md`
- [ ] Update status from `Draft` to `In Progress`
- [ ] Add implementation notes section at top
- [ ] Link to implementation summary document
- [ ] Add known issues section
- [ ] Commit with message: `docs: update RFC-0036 status to In Progress`

### 6. Create Migration Guide
- [ ] Create `docs/guides/migration-rfc-0029-to-0036.md`
  - [ ] Document namespace changes
  - [ ] Document new interface members
  - [ ] Provide code examples (before/after)
  - [ ] List breaking changes
  - [ ] Provide step-by-step migration steps
  - [ ] Include troubleshooting section
- [ ] Commit with message: `docs: add RFC-0029 to RFC-0036 migration guide`

---

## Medium Priority Actions (Next 2 Weeks) 📋

### 7. Fix Unity Platform Integration
- [ ] Research Unity MonoBehaviour lifecycle integration
- [ ] Refactor `UnityWingedBeanHost` to extend `MonoBehaviour`
- [ ] Add Unity lifecycle methods:
  - [ ] `Awake()` - Initialize service provider
  - [ ] `Start()` - Call `StartAsync()`
  - [ ] `Update()` - Call `UpdateAsync()` / `RenderAsync()`
  - [ ] `OnDestroy()` - Call `StopAsync()`
- [ ] Update builder to create GameObject with host component
- [ ] Test in actual Unity project
- [ ] Commit with message: `fix: integrate Unity host with MonoBehaviour lifecycle`

### 8. Fix Godot Platform Integration
- [ ] Research Godot Node lifecycle integration
- [ ] Refactor `GodotWingedBeanHost` to extend `Node`
- [ ] Add Godot lifecycle methods:
  - [ ] `_Ready()` - Initialize service provider
  - [ ] `_Process(double delta)` - Call `ProcessAsync()`
  - [ ] `_ExitTree()` - Call `StopAsync()`
- [ ] Update builder to create Node with host
- [ ] Test in actual Godot project
- [ ] Commit with message: `fix: integrate Godot host with Node lifecycle`

### 9. Update ConsoleDungeon.Host
- [ ] Backup current `Program.cs`
- [ ] Refactor to use `WingedBeanHost.CreateConsoleBuilder()`
- [ ] Remove old hosting pattern
- [ ] Update service registration
- [ ] Test application still works
- [ ] Commit with message: `refactor: migrate ConsoleDungeon.Host to RFC-0036 hosting pattern`

### 10. Create Example Applications
- [ ] Create `examples/console-hosting-example/`
  - [ ] Minimal console app using new hosting
  - [ ] Demonstrates configuration
  - [ ] Demonstrates logging
  - [ ] Demonstrates graceful shutdown
- [ ] Create `examples/unity-hosting-example/`
  - [ ] Unity project with host integration
  - [ ] Demonstrates Unity-specific features
  - [ ] Demonstrates shared game logic
- [ ] Create `examples/godot-hosting-example/`
  - [ ] Godot project with host integration
  - [ ] Demonstrates Godot-specific features
  - [ ] Demonstrates shared game logic
- [ ] Commit with message: `docs: add hosting pattern examples for all platforms`

---

## Low Priority Actions (Next Month) 📝

### 11. Implement Configuration Bridges
- [ ] Unity configuration bridge
  - [ ] Research Unity configuration patterns (ScriptableObject?)
  - [ ] Implement `IConfigurationProvider` for Unity
  - [ ] Update `UnityWingedBeanHostBuilder.ConfigureAppConfiguration()`
  - [ ] Test configuration loading
- [ ] Godot configuration bridge
  - [ ] Research Godot configuration patterns (ProjectSettings?)
  - [ ] Implement `IConfigurationProvider` for Godot
  - [ ] Update `GodotWingedBeanHostBuilder.ConfigureAppConfiguration()`
  - [ ] Test configuration loading
- [ ] Commit with message: `feat: add configuration bridges for Unity and Godot`

### 12. Implement Logging Bridges
- [ ] Unity logging bridge
  - [ ] Create `UnityLoggerProvider : ILoggerProvider`
  - [ ] Bridge `ILogger` calls to `Debug.Log`, `Debug.LogWarning`, `Debug.LogError`
  - [ ] Update `UnityWingedBeanHostBuilder.ConfigureLogging()`
  - [ ] Test logging in Unity
- [ ] Godot logging bridge
  - [ ] Create `GodotLoggerProvider : ILoggerProvider`
  - [ ] Bridge `ILogger` calls to `GD.Print`, `GD.PushWarning`, `GD.PushError`
  - [ ] Update `GodotWingedBeanHostBuilder.ConfigureLogging()`
  - [ ] Test logging in Godot
- [ ] Commit with message: `feat: add logging bridges for Unity and Godot`

### 13. Create Base Class Helpers
- [ ] Create `WingedBeanAppBase : IWingedBeanApp`
  - [ ] Implement common lifecycle logic
  - [ ] Implement state management
  - [ ] Implement event firing
- [ ] Create `UIAppBase : IUIApp`
  - [ ] Extend `WingedBeanAppBase`
  - [ ] Provide default implementations for UI methods
- [ ] Create `TerminalAppBase : ITerminalApp`
  - [ ] Extend `UIAppBase`
  - [ ] Provide default implementations for terminal methods
- [ ] Commit with message: `feat: add base class helpers for common hosting patterns`

### 14. Comprehensive Documentation
- [ ] Create `docs/guides/hosting-console.md`
- [ ] Create `docs/guides/hosting-unity.md`
- [ ] Create `docs/guides/hosting-godot.md`
- [ ] Create `docs/guides/hosting-architecture.md`
- [ ] Update README.md with hosting overview
- [ ] Create API documentation
- [ ] Commit with message: `docs: add comprehensive hosting documentation`

---

## Testing Checklist ✅

### Unit Tests
- [ ] All contract interfaces have tests
- [ ] Console host has full test coverage
- [ ] Factory auto-detection is tested
- [ ] Builder pattern is tested
- [ ] Service provider integration is tested

### Integration Tests
- [ ] End-to-end console app lifecycle
- [ ] Configuration loading works
- [ ] Logging integration works
- [ ] Graceful shutdown works
- [ ] DI container provides correct services

### Manual Tests
- [ ] Console app runs and shuts down correctly
- [ ] Unity app integrates with Unity lifecycle
- [ ] Godot app integrates with Godot lifecycle
- [ ] Shared game logic works on all platforms
- [ ] Configuration works on all platforms
- [ ] Logging works on all platforms

---

## Documentation Checklist 📚

- [ ] RFC-0036 status updated
- [ ] Implementation summary created
- [ ] Migration guide from RFC-0029 created
- [ ] Console hosting guide created
- [ ] Unity hosting guide created
- [ ] Godot hosting guide created
- [ ] API documentation generated
- [ ] Examples documented
- [ ] Troubleshooting guide created
- [ ] FAQ document created

---

## Quality Gates ✨

Before considering RFC-0036 "complete", verify:

- [ ] ✅ All projects build without errors
- [ ] ✅ Zero compiler warnings (or all justified and documented)
- [ ] ✅ Test coverage >80% for core contracts
- [ ] ✅ Test coverage >80% for console host
- [ ] ✅ Integration tests pass for console
- [ ] ✅ Unity host properly integrates with MonoBehaviour
- [ ] ✅ Godot host properly integrates with Node
- [ ] ✅ ConsoleDungeon.Host migrated successfully
- [ ] ✅ Example apps created and tested
- [ ] ✅ Documentation complete
- [ ] ✅ Migration guide tested
- [ ] ✅ Code review completed
- [ ] ✅ RFC-0036 status updated to "Accepted"
- [ ] ✅ ADR created for any design deviations

---

## Progress Tracking

| Phase | Status | Completion | Notes |
|-------|--------|------------|-------|
| Phase 1: Contracts | ✅ Complete | 100% | All contracts implemented and building |
| Phase 2: Console Host | ✅ Complete | 100% | Implementation complete, needs tests |
| Phase 3: Unity/Godot Hosts | ⚠️ Partial | 70% | Built but missing platform integration |
| Phase 4: Testing | ❌ Not Started | 0% | No tests exist yet |
| Phase 5: Documentation | ⚠️ Partial | 30% | RFC exists, need guides and examples |
| Phase 6: Migration | ❌ Not Started | 0% | ConsoleDungeon.Host not migrated |

**Overall Progress**: 50%

---

## Issues to Track

Create GitHub issues for:

1. **Issue**: Unity host missing MonoBehaviour integration
   - **Labels**: `bug`, `unity`, `rfc-0036`, `priority:high`
   - **Milestone**: RFC-0036 Implementation

2. **Issue**: Godot host missing Node integration
   - **Labels**: `bug`, `godot`, `rfc-0036`, `priority:high`
   - **Milestone**: RFC-0036 Implementation

3. **Issue**: No tests for hosting implementation
   - **Labels**: `test`, `rfc-0036`, `priority:high`
   - **Milestone**: RFC-0036 Implementation

4. **Issue**: Missing migration guide from RFC-0029
   - **Labels**: `documentation`, `rfc-0036`, `priority:medium`
   - **Milestone**: RFC-0036 Implementation

5. **Issue**: Configuration bridges for Unity/Godot
   - **Labels**: `enhancement`, `unity`, `godot`, `rfc-0036`, `priority:low`
   - **Milestone**: RFC-0036 Implementation

6. **Issue**: Logging bridges for Unity/Godot
   - **Labels**: `enhancement`, `unity`, `godot`, `rfc-0036`, `priority:low`
   - **Milestone**: RFC-0036 Implementation

---

## Command Reference

### Build Commands
```bash
# Build entire framework
dotnet build development/dotnet/framework/Framework.sln

# Build specific project
dotnet build development/dotnet/framework/src/WingedBean.Hosting/WingedBean.Hosting.csproj

# Build with detailed output
dotnet build -v detailed

# Clean and rebuild
dotnet clean && dotnet build
```

### Test Commands
```bash
# Run all tests
dotnet test development/dotnet/framework/Framework.sln

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test development/dotnet/framework/tests/WingedBean.Hosting.Tests/
```

### Git Commands
```bash
# Check status
git status --short

# Stage new files
git add development/dotnet/framework/src/WingedBean.Contracts.Hosting/
git add development/dotnet/framework/src/WingedBean.Hosting*/

# Commit with file
git commit -F commit-message.txt

# Create feature branch
git checkout -b feature/rfc-0036-platform-hosting

# Push to remote
git push origin feature/rfc-0036-platform-hosting
```

---

## Success Criteria

RFC-0036 implementation is considered **successful** when:

1. ✅ All code is committed and merged to main
2. ✅ All quality gates are met
3. ✅ ConsoleDungeon.Host successfully migrated
4. ✅ At least one Unity example works
5. ✅ At least one Godot example works
6. ✅ All documentation is complete
7. ✅ RFC-0036 status is "Accepted"
8. ✅ No known critical bugs

---

**Document Version**: 1.0  
**Last Updated**: 2025-10-05  
**Owner**: [Your Name]  
**Next Review**: 2025-10-12
