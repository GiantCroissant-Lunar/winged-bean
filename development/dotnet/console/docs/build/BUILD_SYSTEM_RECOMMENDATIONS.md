# Build System Recommendations

**Date**: 2025-10-11  
**Context**: Build-Arcane vs giantcroissant-lunar-build

---

## Question 1: Should We Extend Build-Arcane?

### Current Situation

We have **two build systems**:

1. **build-arcane** (`GiantCroissant.Plate.BuildArcane.Build.Runner`)
   - Simple, config-driven
   - Used by: cross-milo, plugin-manoi, winged-bean (new)
   - Location: `/plate-projects/build-arcane`

2. **giantcroissant-lunar-build** (`GiantCroissant.Lunar.Build.*`)
   - Complex, component-based
   - Used by: winged-bean (old NUKE build)
   - Location: `/infra-projects/giantcroissant-lunar-build`

### What WingedBean's NUKE Build Uses from Lunar

```csharp
using Lunar.Build.Configuration;        // IBuildConfigurationComponent
using Lunar.Build.Components;           // IWrapperPathComponent
using Lunar.Build.NuGet;                // INuGetLocalRepoSyncComponent
using Lunar.Build.CodeQuality.Reporting;
using Lunar.Build.Abstractions.Services;
using Lunar.NfunReport.MNuke.Components; // INfunReportComponent
```

**Key Components:**
- **Configuration**: Config-driven builds
- **NuGet**: Local feed sync, packaging workflows
- **Reporting**: Multi-format reports (HTML, JSON, Markdown)
- **Code Quality**: Quality metrics and reporting
- **Mobile**: Android/iOS build support

---

## Recommendation: Use the Right Tool for the Job

### Option A: Keep Build-Arcane for Simple Projects ✅ (Recommended)

**Use build-arcane for:**
- ✅ **cross-milo** - Simple contract libraries
- ✅ **plugin-manoi** - Simple framework libraries
- ✅ **winged-bean console** - Simple game project (current)

**Pros:**
- Simple, easy to maintain
- Consistent across projects
- Low learning curve
- Good enough for 90% of use cases

**Cons:**
- Limited features
- No custom reporting
- No advanced NuGet workflows

### Option B: Use giantcroissant-lunar-build for Complex Projects

**Use giantcroissant-lunar-build for:**
- ⚠️ **winged-bean framework** - If you need NuGet packaging
- ⚠️ **Unity projects** - Mobile builds, player builds
- ⚠️ **Complex workflows** - Custom reporting, multi-platform

**Pros:**
- Full-featured
- Extensible components
- Advanced reporting
- Mobile support

**Cons:**
- Complex setup
- Steeper learning curve
- More maintenance

### Option C: Extend Build-Arcane (Future)

**If you need features from giantcroissant-lunar-build:**

1. **Add to build-arcane**:
   - Custom reporting formats
   - NuGet local feed sync
   - Code quality metrics

2. **Keep it simple**:
   - Config-driven, not code-driven
   - Optional features via config flags
   - Maintain simplicity for common cases

**Example future build-config.json:**
```json
{
  "solution": "Console.sln",
  "reporting": {
    "enabled": true,
    "formats": ["html", "json", "markdown"]
  },
  "nuget": {
    "localFeedSync": true,
    "localFeedPath": "../../../packages/nuget-repo"
  },
  "codeQuality": {
    "enabled": true,
    "thresholds": {
      "maxWarnings": 100,
      "minCoverage": 80.0
    }
  }
}
```

---

## Recommendation Summary

### For Now: ✅ Keep Current Setup

- **cross-milo**: build-arcane ✅
- **plugin-manoi**: build-arcane ✅
- **winged-bean console**: build-arcane ✅
- **winged-bean framework** (if needed): giantcroissant-lunar-build

### For Future: Consider Extending Build-Arcane

If you find yourself needing advanced features frequently:
1. File issues/RFCs in build-arcane repo
2. Add features incrementally
3. Keep config-driven approach
4. Don't reinvent giantcroissant-lunar-build

---

## Question 2: Code Formatting - Build Time or Pre-Commit?

### TL;DR: **Pre-Commit Hook** ✅ (Recommended)

### Option A: Pre-Commit Hook ✅ (Recommended)

**Use pre-commit hooks for:**
- `dotnet format` - C# code formatting
- `jb cleanupcode` - JetBrains code cleanup
- Other linters and formatters

**Pros:**
- ✅ **Catches issues early** - Before code is committed
- ✅ **Faster feedback** - Developers see issues immediately
- ✅ **Prevents bad code** - Can't commit unformatted code
- ✅ **CI stays fast** - No formatting in CI, just validation
- ✅ **Developer-friendly** - Auto-fixes issues locally

**Cons:**
- ⚠️ Requires setup on each developer machine
- ⚠️ Can be bypassed with `--no-verify`

**Implementation:**

`.pre-commit-config.yaml`:
```yaml
repos:
  - repo: local
    hooks:
      # C# formatting with dotnet format
      - id: dotnet-format
        name: dotnet format
        entry: dotnet format
        language: system
        files: \.(cs|csproj|sln)$
        pass_filenames: false
        
      # JetBrains code cleanup (if jb CLI is installed)
      - id: jb-cleanupcode
        name: JetBrains CleanupCode
        entry: jb cleanupcode
        args: [--profile=Built-in: Full Cleanup]
        language: system
        files: \.cs$
        pass_filenames: false
```

**Setup:**
```bash
# Install pre-commit
pip install pre-commit

# Install hooks
pre-commit install

# Run manually
pre-commit run --all-files
```

### Option B: Build Time (Not Recommended)

**Run formatting during build:**

**Pros:**
- ✅ Guaranteed to run in CI
- ✅ No developer setup needed

**Cons:**
- ❌ **Slower builds** - Formatting adds time
- ❌ **Late feedback** - Developers don't see issues until build
- ❌ **Modifies source** - Build shouldn't modify source files
- ❌ **CI failures** - Formatting issues fail builds
- ❌ **Merge conflicts** - Auto-formatting can cause conflicts

**Not recommended because:**
- Builds should be **read-only** operations
- Formatting should happen **before** commit, not during build

### Option C: Hybrid Approach ✅ (Best Practice)

**Combine both:**

1. **Pre-commit**: Auto-format locally
   ```yaml
   - id: dotnet-format
     name: dotnet format (auto-fix)
     entry: dotnet format
   ```

2. **CI**: Validate only (fail if not formatted)
   ```yaml
   - name: Check formatting
     run: dotnet format --verify-no-changes
   ```

**Benefits:**
- ✅ Developers auto-format locally
- ✅ CI validates formatting
- ✅ Fast feedback loop
- ✅ Prevents unformatted code from merging

---

## Recommended Setup for WingedBean

### 1. Pre-Commit Hooks

Create `.pre-commit-config.yaml` in repo root:

```yaml
repos:
  # C# formatting
  - repo: local
    hooks:
      - id: dotnet-format
        name: Format C# code
        entry: dotnet format
        args: [--include]
        language: system
        files: \.cs$
        pass_filenames: true
        
  # JetBrains cleanup (optional, if jb CLI installed)
  - repo: local
    hooks:
      - id: jb-cleanupcode
        name: JetBrains CleanupCode
        entry: bash
        args:
          - -c
          - |
            if command -v jb &> /dev/null; then
              jb cleanupcode Console.sln --profile="Built-in: Full Cleanup"
            else
              echo "jb CLI not installed, skipping"
            fi
        language: system
        files: \.cs$
        pass_filenames: false
```

### 2. CI Validation

In GitHub Actions / GitLab CI:

```yaml
- name: Check code formatting
  run: dotnet format --verify-no-changes
  
- name: Build
  run: dotnet lunar-build ci
```

### 3. Editor Configuration

`.editorconfig` (already exists):
```ini
[*.cs]
# Formatting rules
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false

# Code style rules
csharp_prefer_braces = true:warning
csharp_prefer_simple_using_statement = true:suggestion
```

---

## Summary

### Build System
- ✅ **Keep build-arcane** for cross-milo, plugin-manoi, winged-bean console
- ⚠️ **Use giantcroissant-lunar-build** only if you need advanced features
- 🔮 **Extend build-arcane** in the future if needed

### Code Formatting
- ✅ **Pre-commit hooks** for auto-formatting (dotnet format, jb cleanupcode)
- ✅ **CI validation** to enforce formatting
- ❌ **Not in build** - builds should be read-only

### Action Items

1. **Add pre-commit hooks**:
   ```bash
   pip install pre-commit
   # Create .pre-commit-config.yaml
   pre-commit install
   ```

2. **Add CI formatting check**:
   ```yaml
   - run: dotnet format --verify-no-changes
   ```

3. **Document in README**:
   ```markdown
   ## Development Setup
   
   Install pre-commit hooks:
   ```bash
   pip install pre-commit
   pre-commit install
   ```
   ```

4. **Keep build-arcane simple** - Don't add formatting to build targets

---

## References

- Build-Arcane: `/plate-projects/build-arcane/README.md`
- giantcroissant-lunar-build: `/infra-projects/giantcroissant-lunar-build/README.md`
- Pre-commit: https://pre-commit.com/
- dotnet format: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-format
- JetBrains CLI: https://www.jetbrains.com/help/rider/Command_Line_Tools.html
