# Winged Bean - build-arcane Migration Plan

**Date**: 2025-10-13  
**Issue**: winged-bean has custom NUKE build project that duplicates logic  
**Goal**: Migrate to build-arcane to eliminate duplication

---

## Problem Statement

### Current State: Code Duplication ❌

**winged-bean currently has**:
```
build/nuke/build/
  Build.cs                    # 182 lines of custom build logic
  Build.Configuration.cs      # Configuration component
  Build.NuGetPackaging.cs     # NuGet packaging logic
  Build.Reporting.cs          # Reporting logic
  Build.WrapperPath.cs        # Wrapper path logic
  _build.csproj               # Build project with dependencies
```

**This is exactly what build-arcane eliminates!**

### build-arcane's Purpose ✅

**Eliminate duplicated Build.cs files** by providing:
- ✅ Centralized build logic via .NET tool
- ✅ Config-driven builds (no custom code)
- ✅ Component-based architecture
- ✅ Simple `build-config.json` instead of 182+ lines of C#

---

## Current Build Logic Analysis

### What winged-bean's Build.cs Does

1. **Basic Targets** (Can be replaced by build-arcane):
   - ✅ `Clean` - Clean solution
   - ✅ `RestoreSolution` - Restore packages
   - ✅ `Compile` - Build solution
   - ✅ `Test` - Run tests with coverage
   - ✅ `CI` - Full CI pipeline

2. **Advanced Features** (Need investigation):
   - ⚠️ `IBuildConfigurationComponent` - Custom configuration
   - ⚠️ `IWrapperPathComponent` - Wrapper path handling
   - ⚠️ `INfunReportComponent` - Reporting integration
   - ⚠️ Multi-project support (console vs framework)
   - ⚠️ GitVersion integration

3. **Dependencies**:
   ```xml
   <ProjectReference Include="Lunar.Build.Configuration.csproj" />
   <ProjectReference Include="Lunar.Build.CoreAbstractions.csproj" />
   <ProjectReference Include="Lunar.Build.CodeQuality.csproj" />
   <ProjectReference Include="NFunReportComponents.csproj" />
   ```

---

## Migration Strategy

### Option 1: Full Migration to build-arcane (Recommended)

**Replace custom Build.cs with build-arcane**

#### Step 1: Analyze Current Functionality

**Current `build-config.json`** (winged-bean):
```json
{
  "projectType": "multi-group-build",
  "projectGroups": [
    {
      "name": "console-host",
      "buildType": "dotnet-console",
      "sourceDirectory": "../../development/dotnet/console/src",
      "outputs": [...]
    }
  ],
  "codeQuality": {...},
  "reporting": {...}
}
```

**Target `build-config.json`** (build-arcane):
```json
{
  "solution": "../../development/dotnet/console/Console.sln",
  "projects": [
    "../../development/dotnet/console/src/host/Dungeon.Host.Console/Dungeon.Host.Console.csproj"
  ],
  "tests": {
    "enabled": true,
    "projects": ["../../development/dotnet/console/tests/**/*.csproj"]
  },
  "package": {
    "enabled": false
  }
}
```

#### Step 2: Install build-arcane

```bash
# Install as local tool
cd /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean
dotnet new tool-manifest
dotnet tool install GiantCroissant.Plate.BuildArcane.Build.Runner --local
```

#### Step 3: Create Simplified build-config.json

```bash
# Move to root
mv build/nuke/build-config.json build-config.json

# Simplify to build-arcane schema
# Edit build-config.json to match build-arcane format
```

#### Step 4: Remove Custom Build Project

```bash
# Remove custom NUKE build project
rm -rf build/nuke/build/

# Keep build-config.json at root
```

#### Step 5: Update Build Scripts

```bash
# Replace build.sh with build-arcane call
cat > build.sh << 'EOF'
#!/bin/bash
dotnet lunar-build "$@"
EOF

# Replace build.ps1 with build-arcane call
cat > build.ps1 << 'EOF'
dotnet lunar-build $args
EOF
```

#### Step 6: Test Migration

```bash
# Test basic build
dotnet lunar-build compile

# Test full CI
dotnet lunar-build ci

# Test specific targets
dotnet lunar-build test
```

---

### Option 2: Enhance build-arcane First

**If build-arcane doesn't support all features**

#### Missing Features to Add to build-arcane:

1. **Multi-group builds**
   - Current: winged-bean has console-host and console-tests groups
   - Needed: build-arcane should support multiple project groups

2. **Code quality integration**
   - Current: winged-bean has codeQuality config
   - Needed: build-arcane should support code quality checks

3. **Reporting integration**
   - Current: winged-bean has reporting config
   - Needed: build-arcane should support reporting

4. **Advanced configuration**
   - Current: winged-bean has complex schema
   - Needed: build-arcane should support advanced config

**Approach**: Enhance build-arcane with these features, then migrate

---

### Option 3: Hybrid Approach

**Keep some custom logic, use build-arcane for basics**

#### What to Keep:
- ⚠️ Advanced reporting (if build-arcane doesn't support)
- ⚠️ Custom NuGet packaging (if build-arcane doesn't support)

#### What to Replace:
- ✅ Basic targets (Clean, Restore, Compile, Test, CI)
- ✅ GitVersion integration
- ✅ Solution/project management

**Not Recommended**: Adds complexity, defeats purpose of build-arcane

---

## Recommended Approach

### Phase 1: Assess build-arcane Capabilities ⏳

**Questions to Answer**:
1. ❓ Does build-arcane support multi-project builds?
2. ❓ Does build-arcane support code quality checks?
3. ❓ Does build-arcane support reporting?
4. ❓ Does build-arcane support GitVersion?
5. ❓ Does build-arcane support custom output paths?

**Action**: Review build-arcane documentation and features

---

### Phase 2: Gap Analysis ⏳

**Compare**:
- What winged-bean needs
- What build-arcane provides
- What's missing

**Document Gaps**:
- Critical gaps (blockers)
- Nice-to-have gaps (can work around)
- Non-issues (already supported)

---

### Phase 3: Decision ⏳

**If gaps are small**: Migrate to build-arcane ✅
**If gaps are large**: Enhance build-arcane first, then migrate ✅
**If gaps are fundamental**: Keep custom build (but document why) ⚠️

---

## Expected Benefits

### After Migration to build-arcane:

1. **Less Code** ✅
   - Remove 182+ lines of Build.cs
   - Remove 4 partial class files
   - Remove _build.csproj

2. **Simpler Maintenance** ✅
   - No custom build code to maintain
   - Updates via `dotnet tool update`
   - Shared improvements across projects

3. **Consistency** ✅
   - Same build system as cross-milo, plugin-manoi, etc.
   - Same patterns, same commands
   - Easier onboarding

4. **Focus** ✅
   - Focus on application code, not build code
   - Build system is infrastructure, not feature

---

## Migration Checklist

### Pre-Migration
- [ ] Review build-arcane capabilities
- [ ] Identify gaps
- [ ] Document current build requirements
- [ ] Create migration plan
- [ ] Get approval

### Migration
- [ ] Install build-arcane as local tool
- [ ] Create simplified build-config.json
- [ ] Remove custom Build.cs files
- [ ] Update build scripts
- [ ] Test all build targets
- [ ] Update CI/CD pipelines
- [ ] Update documentation

### Post-Migration
- [ ] Verify all builds work
- [ ] Verify CI/CD works
- [ ] Document any workarounds
- [ ] Train team on new build system
- [ ] Monitor for issues

---

## Risks and Mitigation

### Risk 1: Feature Loss
**Risk**: build-arcane doesn't support all features  
**Mitigation**: Enhance build-arcane first, or document workarounds

### Risk 2: Breaking Changes
**Risk**: Migration breaks existing builds  
**Mitigation**: Test thoroughly, keep backup of old build

### Risk 3: CI/CD Impact
**Risk**: CI/CD pipelines break  
**Mitigation**: Update pipelines as part of migration

### Risk 4: Team Disruption
**Risk**: Team unfamiliar with build-arcane  
**Mitigation**: Documentation, training, gradual rollout

---

## Timeline Estimate

### Quick Migration (if build-arcane supports all features):
- **Day 1**: Install build-arcane, create config
- **Day 2**: Test and fix issues
- **Day 3**: Update CI/CD, documentation
- **Total**: 3 days

### Full Migration (if build-arcane needs enhancement):
- **Week 1**: Gap analysis, enhancement planning
- **Week 2-3**: Enhance build-arcane
- **Week 4**: Migrate winged-bean
- **Week 5**: Test, document, train
- **Total**: 5 weeks

---

## Comparison: Before vs After

### Before (Current)
```
build/nuke/build/
  Build.cs                    # 182 lines
  Build.Configuration.cs      # ~100 lines
  Build.NuGetPackaging.cs     # ~150 lines
  Build.Reporting.cs          # ~130 lines
  Build.WrapperPath.cs        # ~50 lines
  _build.csproj               # ~45 lines

Total: ~657 lines of custom build code
```

### After (build-arcane)
```
build-config.json             # ~30 lines
.config/dotnet-tools.json     # ~10 lines

Total: ~40 lines of configuration
```

**Reduction**: ~617 lines eliminated (94% reduction)

---

## Immediate Next Steps

### 1. Review build-arcane Capabilities ⏳

**Action**: Check if build-arcane supports:
- Multi-project builds
- Code quality checks
- Reporting
- GitVersion
- Custom output paths

**Owner**: You  
**Timeline**: 1 hour

### 2. Create Gap Analysis Document ⏳

**Action**: Document what's missing

**Owner**: You  
**Timeline**: 2 hours

### 3. Decide on Approach ⏳

**Options**:
- A: Migrate now (if no gaps)
- B: Enhance build-arcane first (if small gaps)
- C: Keep custom build (if fundamental gaps)

**Owner**: You  
**Timeline**: 1 day

---

## Conclusion

**You're absolutely right!** 

The custom NUKE build project in winged-bean is **exactly the duplication** that build-arcane is designed to eliminate.

**Recommendation**: 
1. ✅ Assess build-arcane capabilities
2. ✅ Identify gaps
3. ✅ Migrate to build-arcane (or enhance it first if needed)

**Expected Result**: Eliminate ~617 lines of custom build code, replace with ~40 lines of config.

---

**Status**: Migration plan created, awaiting gap analysis and decision
