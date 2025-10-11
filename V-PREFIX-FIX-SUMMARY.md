# Fix: Remove 'v' Prefix from Artifact Directory Names

## Issue
Artifact directories were being created with a `v` prefix (e.g., `v0.0.1-410`) instead of the intended format without prefix (e.g., `0.0.1-410`). This was inconsistent with the get-version.sh script and the Taskfile.yml which already removed the `v` prefix.

## Root Cause
The `v` prefix was being added in the **AsciinemaRecorder plugin** when constructing artifact paths for recordings and logs directories. The plugin runtime was creating these directories when the application started.

## Files Fixed

### 1. AsciinemaRecorder Plugin Path Helper
**File:** `development/dotnet/console/src/plugins/WingedBean.Plugins.AsciinemaRecorder/GitVersionHelper.cs`

**Changed:**
- Line 100: `$"v{version}"` → `version`
- Line 104: `$"v{version}"` → `version`  
- Line 118: `$"v{version}"` → `version`
- Line 122: `$"v{version}"` → `version`

**Impact:** The AsciinemaRecorder will now create directories at:
- `build/_artifacts/{version}/dotnet/recordings/` (was: `v{version}`)
- `build/_artifacts/{version}/dotnet/logs/` (was: `v{version}`)

### 2. E2E Test Path References
**File:** `development/dotnet/console/tests/e2e/ArtifactBasedE2ETests.cs`

**Changed:**
- Line 13: Comment updated to show `{version}` instead of `v{version}`
- Line 38: `$"v{_currentVersion}"` → `_currentVersion`
- Line 50: `v{_currentVersion}` → `{_currentVersion}` (display string)
- Line 206: `$"v{versionFromScript}"` → `versionFromScript`
- Line 211: `v{versionFromScript}` → `{versionFromScript}` (display string)

**Impact:** E2E tests will now look for artifacts in the correct directories without `v` prefix.

## Verification

Before fix:
```
build/_artifacts/
├── 0.0.1-409/        (created by Taskfile)
├── v0.0.1-409/       (created by AsciinemaRecorder at runtime)
└── v0.0.1-410/       (created by AsciinemaRecorder at runtime)
```

After fix:
```
build/_artifacts/
├── 0.0.1-409/        (consistent path for all artifacts)
├── latest/           (symlink to current version)
```

## Related Context

This fix aligns with previous work documented in AUDIO-INTEGRATION-HANDOVER.md where the `v` prefix was already removed from:
- `development/nodejs/get-version.js`
- `development/nodejs/pty-service/get-version.js`

The Nuke build system (Build.cs) was already correct - it uses `GitVersion.SemVer` which does NOT include the `v` prefix.

## Testing

1. Build completed successfully with 0 errors
2. Old `v0.0.1-*` directories removed from _artifacts
3. Future builds will create consistent directory structure

## Impact

- ✅ Consistent artifact directory naming
- ✅ E2E tests will find artifacts in correct locations
- ✅ AsciinemaRecorder recordings/logs will be in version-matched directories
- ✅ No breaking changes - just consistency fix
