# Recording Tests - Quick Reference

## 🚀 Quick Start

### Test Locally (Fastest)

```bash
# From repository root
./.github/test-recordings-local.sh
```

### Test Console Only (3 seconds)

```bash
./.github/test-recordings-local.sh 3 build-and-test-console
```

### Test Desktop Only

```bash
./.github/test-recordings-local.sh 5 build-and-test-desktop
```

---

## 📋 Commands Cheat Sheet

### Act Commands

| Command | Description |
|---------|-------------|
| `act -l -W .github/workflows/recording-tests.yml` | List jobs |
| `act workflow_dispatch -W .github/workflows/recording-tests.yml` | Run all jobs |
| `act workflow_dispatch -W ... -j build-and-test-console` | Console only |
| `act workflow_dispatch -W ... --input record_duration=5` | Custom duration |
| `act workflow_dispatch -W ... --dryrun` | Dry run (no execution) |

### Helper Script

| Command | Description |
|---------|-------------|
| `./.github/test-recordings-local.sh` | Default (5s, all jobs) |
| `./.github/test-recordings-local.sh 10` | 10 second recording |
| `./.github/test-recordings-local.sh 3 build-and-test-console` | Console only, 3s |

### Check Results

| Command | Description |
|---------|-------------|
| `ls -la /tmp/act-artifacts/` | List all artifacts |
| `ls -la /tmp/act-artifacts/console-recordings/` | Console recordings |
| `cat /tmp/act-artifacts/console-logs/*.log` | View console logs |

---

## 🎯 What Gets Tested

### Console App ✅
- .NET 8 build
- Recording with `--record=true`
- `.cast` file creation (Asciinema format)
- `.urf.jsonl` file creation (URF format)
- Auto-start, auto-record, auto-stop

### Desktop App ⚠️
- .NET 8 build
- Plugin loading (SadConsole, Recorder)
- Recording service activation
- May not create files in headless CI

---

## 📦 Artifacts Location

**Local (act)**: `/tmp/act-artifacts/`
**GitHub Actions**: Download from workflow run

---

## ⏱️ Typical Run Times

| Test | Duration |
|------|----------|
| Console only (3s recording) | ~2-3 minutes |
| Console only (10s recording) | ~2-3 minutes |
| Desktop only | ~3-4 minutes |
| Full workflow (both + summary) | ~5-6 minutes |

*First run takes longer (downloads Docker image ~1-2GB)*

---

## 🔧 Troubleshooting

### Docker not running
```bash
docker info
# Start Docker Desktop if error
```

### No artifacts created
```bash
# Check act artifacts path
ls -la /tmp/act-artifacts/

# Clean up old artifacts
rm -rf /tmp/act-artifacts/*
```

### Workflow fails
```bash
# Run with verbose output
act workflow_dispatch -W .github/workflows/recording-tests.yml --verbose
```

---

## 📚 Documentation

- Full guide: `.github/ACT-TESTING.md`
- Setup summary: `SUMMARY-2025-10-13-Recording-CI-CD-Setup.md`
- Workflow file: `.github/workflows/recording-tests.yml`

---

## ✅ Success Indicators

### Console Test Passed ✅
- Exit code: 0
- Log shows: "✓ Recording started"
- Log shows: "✓ Recording stopped"
- Files created: `*.cast`, `*.urf.jsonl`

### Desktop Test Passed ⚠️
- Exit code: 0 (or expected failure in headless)
- Log shows: "Recording requested"
- Plugin loaded: "RecorderSnapshotsPlugin"

---

## 🚦 Exit Codes

- `0` = Success
- `1` = Test failed
- `2` = Workflow error

---

## 🎬 GitHub Actions (Online)

### Manual Trigger
1. Go to repository → **Actions** tab
2. Select **Recording Tests (URF)**
3. Click **Run workflow**
4. Set duration (default: 10s)
5. Run

### Automatic Trigger
- Push to `main` (if recording files changed)
- Pull requests (if recording files changed)

---

**Need help?** See `.github/ACT-TESTING.md` for full documentation.
