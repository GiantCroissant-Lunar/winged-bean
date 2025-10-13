---
id: RFC-0042
title: Recording Test Bot with Microsoft RulesEngine
status: Proposed
category: testing, ci-cd, automation
created: 2025-10-13
updated: 2025-10-13
priority: P2
effort: 4-6 hours
---

# RFC-0042: Recording Test Bot with Microsoft RulesEngine

**Status:** Proposed  
**Date:** 2025-10-13  
**Author:** Development Team  
**Category:** testing, ci-cd, automation  
**Priority:** MEDIUM (P2)  
**Estimated Effort:** 4-6 hours  
**Depends On:** 
- AsciinemaRecorder plugin (implemented)
- Recording CI workflow (implemented)

---

## Summary

Implement a `RecordingTestBotService` using Microsoft RulesEngine to generate synthetic terminal activity during CI recording tests. This addresses the current limitation where recording tests only capture metadata because no terminal activity occurs in headless CI environments. The bot will use JSON-defined behavior rules to produce meaningful test recordings that validate the complete recording pipeline.

---

## Motivation

### Current Problem

Recording tests in CI pass but produce minimal recordings:

```
# Current recording output (only metadata, no events)
{"version":2,"width":80,"height":24,"timestamp":1760365749,...}
# File size: 141 bytes (just header, no activity)
```

**Why?**
1. **Headless CI Environment** - Terminal app doesn't run without GUI
2. **No Activity Generation** - `RecordingControlService` just waits, doesn't produce output
3. **Incomplete Validation** - Can't verify that event recording actually works

**Impact:**
- Recording system is 50% validated (metadata works, but event capture is unverified)
- No proof that asciinema playback will work with real recordings
- Can't test URF event stream functionality
- Future recording features can't be validated in CI

### Why Microsoft RulesEngine?

**Benefits:**
1. **JSON-Driven Configuration** - Test scenarios defined in version-controlled JSON files
2. **No Code Changes for New Scenarios** - Add test patterns without rebuilding
3. **Expression-Based Logic** - Flexible time-based, condition-based rules
4. **Mature & Maintained** - Microsoft-backed, actively developed
5. **Lightweight** - Minimal dependency (~300KB NuGet package)

**Alternative Considered:**
- **Custom Time-Based Generator**: Works but limited flexibility
- **GOAP System**: Overkill for simple test pattern generation
- **Hardcoded Scenarios**: Requires recompilation for changes

### Success Criteria

✅ CI recording tests produce recordings with 20+ events  
✅ Recordings are playable with asciinema  
✅ URF format contains valid event stream  
✅ Test scenarios configurable via JSON  
✅ Bot runs in headless CI environment  
✅ No impact on production builds (test-only service)

---

## Proposal

### Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│ RecordingTestBotService (BackgroundService)            │
│                                                         │
│  ┌──────────────────────────────────────────────────┐ │
│  │ Microsoft.RulesEngine                            │ │
│  │ - Loads: recording-test-scenarios.json          │ │
│  │ - Evaluates: Time-based rules                   │ │
│  │ - Executes: OutputAction handlers               │ │
│  └──────────────────────────────────────────────────┘ │
│                      │                                  │
│                      ▼                                  │
│  ┌──────────────────────────────────────────────────┐ │
│  │ BotOutputWriter                                  │ │
│  │ - Writes to Console.Out (captured by recorder)  │ │
│  │ - Formatted output with timestamps              │ │
│  │ - Simulated game events                         │ │
│  └──────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
                         │
                         ▼
            ┌─────────────────────────┐
            │ AsciinemaRecorder       │
            │ - Captures stdout       │
            │ - Writes .cast file     │
            │ - Writes .urf.jsonl     │
            └─────────────────────────┘
```

### Component Design

#### 1. RecordingTestBotService

**Purpose:** Background service that runs during recording tests to generate terminal activity

**Location:** `development/dotnet/console/src/host/Dungeon.Host.Console/RecordingTestBotService.cs`

**Key Responsibilities:**
- Load JSON rules on startup
- Execute rules based on elapsed time
- Generate formatted output to stdout
- Coordinate with RecordingControlService lifecycle

**Service Registration:**
```csharp
// Only register when --record=true AND --bot-enabled=true
if (shouldRecord && botEnabled)
{
    services.AddHostedService<RecordingTestBotService>();
}
```

#### 2. Bot Action Handlers

**Built-in Actions:**
- `PrintMessage` - Output simple text
- `PrintGameEvent` - Formatted game event (player move, combat, etc.)
- `PrintAsciiArt` - Multi-line ASCII art
- `PrintProgress` - Progress bar/counter
- `PrintTable` - Formatted table output
- `Wait` - Delay between outputs

#### 3. JSON Rule Configuration

**Location:** `development/dotnet/console/src/host/Dungeon.Host.Console/recording-test-scenarios.json`

**Schema:**
```json
{
  "WorkflowName": "BasicRecordingTest",
  "GlobalParams": {
    "SessionDuration": 10,
    "EventsPerSecond": 2
  },
  "Rules": [
    {
      "RuleName": "InitialBanner",
      "Expression": "input1.ElapsedSeconds < 0.5",
      "SuccessEvent": "10",
      "Actions": {
        "OnSuccess": {
          "Name": "PrintBanner",
          "Context": {
            "Title": "Recording Test Session",
            "SessionId": "{{input1.SessionId}}"
          }
        }
      }
    },
    {
      "RuleName": "SimulateGameActivity",
      "Expression": "input1.ElapsedSeconds % 1 == 0 AND input1.ElapsedSeconds > 0",
      "SuccessEvent": "20",
      "Actions": {
        "OnSuccess": {
          "Name": "PrintGameEvent",
          "Context": {
            "EventType": "PlayerMove",
            "Frame": "{{input1.ElapsedSeconds}}",
            "Data": "Player moves {{RandomDirection}}"
          }
        }
      }
    },
    {
      "RuleName": "CombatSequence",
      "Expression": "input1.ElapsedSeconds >= 3 AND input1.ElapsedSeconds < 5",
      "SuccessEvent": "30",
      "Actions": {
        "OnSuccess": {
          "Name": "PrintCombatEvent",
          "Context": {
            "Attacker": "Player",
            "Defender": "Goblin",
            "Damage": "{{Random:5-15}}"
          }
        }
      }
    },
    {
      "RuleName": "FinalSummary",
      "Expression": "input1.ElapsedSeconds >= {{input1.Duration - 1}}",
      "SuccessEvent": "40",
      "Actions": {
        "OnSuccess": {
          "Name": "PrintSummary",
          "Context": {
            "TotalEvents": "{{input1.EventCount}}",
            "Duration": "{{input1.ElapsedSeconds}}"
          }
        }
      }
    }
  ]
}
```

### Implementation Plan

#### Phase 1: Foundation (2 hours)

1. **Add NuGet Package**
   ```xml
   <PackageReference Include="Microsoft.RulesEngine" Version="5.1.0" />
   ```

2. **Create RecordingTestBotService**
   - Implement `BackgroundService`
   - Load rules from embedded JSON
   - Basic rule execution loop

3. **Create BotOutputWriter**
   - Format helpers for different output types
   - Thread-safe console writing
   - Timestamp injection

#### Phase 2: Actions & Rules (2 hours)

4. **Implement Action Handlers**
   - `PrintMessage` - Simple text output
   - `PrintGameEvent` - Structured game event
   - `PrintBanner` - Formatted header/footer

5. **Create Basic Rule Set**
   - `recording-test-scenarios.json`
   - 10-second test with ~20 events
   - Variety of output types

6. **Integration with RecordingControlService**
   - Coordinate start/stop timing
   - Pass session metadata
   - Ensure proper shutdown

#### Phase 3: CI Integration (1-2 hours)

7. **Update Workflow**
   ```yaml
   - name: Test console recording
     run: |
       ./Dungeon.Host.Console \
         --record=true \
         --record-duration=10 \
         --record-session=ci-console-test \
         --bot-enabled=true \  # Enable test bot
         2>&1 | tee console-recording.log
   ```

8. **Add Verification**
   - Check event count > 20
   - Validate asciinema format
   - Test asciinema playback (if tool available)

9. **Update Documentation**
   - README with bot usage
   - Scenario authoring guide
   - CI workflow documentation

### Example Output

**Expected Recording Content:**
```
========================================
Recording Test Session: ci-console-test
========================================

[Frame 1] Player spawns at (5,5)
[Frame 2] Player moves north
[Frame 3] Enemy detected: Goblin (HP: 20)
[Frame 4] Combat initiated
[Frame 5] Player attacks Goblin for 12 damage
[Frame 6] Goblin attacks Player for 3 damage
[Frame 7] Player attacks Goblin for 8 damage
[Frame 8] Goblin defeated! XP gained: 15
[Frame 9] Player moves east
[Frame 10] Test session complete

========================================
Session Summary
Duration: 10.00s
Events: 24
Status: SUCCESS
========================================
```

**File Sizes:**
- Before: `141 bytes` (metadata only)
- After: `~2-4 KB` (metadata + events)

---

## Technical Details

### Rule Input Context

```csharp
public class BotRuleInput
{
    public string SessionId { get; set; }
    public double ElapsedSeconds { get; set; }
    public int EventCount { get; set; }
    public int Duration { get; set; }
    public DateTime StartTime { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

### Custom Functions

RulesEngine allows custom functions for expressions:

```csharp
public class BotFunctions
{
    public static string RandomDirection() =>
        new[] { "north", "south", "east", "west" }[Random.Shared.Next(4)];
        
    public static int RandomInt(int min, int max) =>
        Random.Shared.Next(min, max + 1);
        
    public static string FormatTimestamp(double seconds) =>
        TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss\.ff");
}
```

### Error Handling

**Resilience:**
- Bot failure doesn't stop recording
- Logs errors but continues execution
- Falls back to minimal output if rules fail
- Graceful degradation (e.g., if JSON missing, use hardcoded scenario)

**Monitoring:**
```csharp
try
{
    await ExecuteRulesAsync(input, ct);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Bot rule execution failed, using fallback");
    await WriteFallbackOutputAsync(ct);
}
```

---

## Configuration

### Command Line Arguments

**New Arguments:**
- `--bot-enabled=true` - Enable recording test bot
- `--bot-scenario=path/to/scenario.json` - Custom scenario file
- `--bot-verbose=true` - Extra bot logging

**Example:**
```bash
./Dungeon.Host.Console \
  --record=true \
  --record-duration=10 \
  --record-session=test \
  --bot-enabled=true \
  --bot-scenario=scenarios/combat-test.json
```

### Environment Variables

**For CI:**
```yaml
env:
  RECORDING_BOT_ENABLED: "true"
  RECORDING_BOT_SCENARIO: "default"
```

---

## Testing Strategy

### Unit Tests

**Location:** `development/dotnet/console/tests/host/Dungeon.Host.Console.Tests/`

**Test Cases:**
1. Rule loading and parsing
2. Action handler execution
3. Timing and scheduling
4. Output formatting
5. Error handling

### Integration Tests

**Scenarios:**
1. **Basic Output Test** - Verify bot produces output
2. **Timing Test** - Verify events occur at correct times
3. **Recording Integration** - Verify recorder captures bot output
4. **Playback Test** - Verify asciinema can play recording

### CI Validation

**Workflow Checks:**
- Recording file size > 500 bytes
- Event count > 20
- Both .cast and .urf.jsonl created
- Files contain expected patterns (grep test)

---

## Scenarios Library

### Built-in Scenarios

**1. default.json** - Basic activity test
- Duration: 10s
- Events: ~25
- Content: Simple game events

**2. stress-test.json** - High-frequency output
- Duration: 5s
- Events: ~200 (40/sec)
- Content: Rapid combat simulation

**3. ascii-art.json** - Visual test
- Duration: 15s
- Events: ~50
- Content: Maps, UI elements, ASCII art

**4. minimal.json** - Quick smoke test
- Duration: 3s
- Events: ~5
- Content: Start banner, few events, end summary

### Custom Scenarios

Users can create custom scenarios for specific testing needs:

```bash
# Test with custom scenario
./Dungeon.Host.Console \
  --record=true \
  --bot-enabled=true \
  --bot-scenario=my-custom-test.json
```

---

## Migration Path

### Phase 1: Parallel (Current + Bot)
- Bot is opt-in via `--bot-enabled`
- Existing tests unchanged
- New tests use bot for validation

### Phase 2: Default Enabled
- Bot becomes default for CI recording tests
- Local testing can disable with `--bot-enabled=false`
- Clear documentation of bot behavior

### Phase 3: Enhanced Scenarios
- More sophisticated test patterns
- Game-realistic simulations
- Performance testing scenarios

---

## Future Enhancements

### Potential Extensions

1. **AI-Driven Scenarios**
   - GOAP integration for realistic AI behavior
   - Dynamic scenario generation

2. **Replay Validation**
   - Automatic asciinema playback testing
   - Screenshot comparison

3. **Performance Testing**
   - High-volume output scenarios
   - Recorder performance validation

4. **Multi-Language Support**
   - Localized test output
   - Unicode/emoji testing

5. **Interactive Mode**
   - Bot responds to simulated input
   - Complex state machines

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Bot adds CI time | Low | Bot runs during recording, no extra time |
| Rules complexity grows | Medium | Keep scenarios simple, template library |
| RulesEngine bugs | Low | Mature library, fallback to hardcoded |
| Output interferes with recorder | Medium | Test thoroughly, use stderr for bot logs |
| JSON schema changes | Low | Validation on load, clear error messages |

---

## Alternatives Considered

### 1. Hardcoded Test Patterns
**Pros:** Simpler, no dependencies  
**Cons:** Inflexible, requires recompilation  
**Decision:** RulesEngine provides better long-term maintainability

### 2. Custom DSL for Scenarios
**Pros:** Tailored to needs  
**Cons:** Development overhead, maintenance burden  
**Decision:** RulesEngine is proven and sufficient

### 3. GOAP-Based Bot
**Pros:** Realistic AI behavior  
**Cons:** Overkill for simple test patterns  
**Decision:** Keep GOAP for game AI, use RulesEngine for test bot

### 4. No Bot (Manual Testing Only)
**Pros:** No development needed  
**Cons:** Incomplete CI validation, manual overhead  
**Decision:** Bot enables automated validation

---

## Success Metrics

**After Implementation:**
- ✅ 100% of recording CI tests produce meaningful recordings
- ✅ Event count per recording > 20
- ✅ Zero recording-related CI failures due to empty recordings
- ✅ 3+ reusable test scenarios in library
- ✅ Bot documentation complete

**6 Months After:**
- Recording test coverage expanded to multiple scenarios
- Community contributors add custom scenarios
- Bot validates new recording features automatically

---

## Dependencies

### NuGet Packages
- `Microsoft.RulesEngine` (5.1.0) - Core rules engine
- `Newtonsoft.Json` (13.0.3) - JSON handling (may already be referenced)

### Project References
- No new project references needed
- Uses existing `RecordingControlService`
- Integrates with existing `AsciinemaRecorder`

---

## Documentation Requirements

1. **Developer Guide**
   - How to create custom scenarios
   - Rule syntax and expressions
   - Action handler reference

2. **CI/CD Guide**
   - Workflow configuration
   - Validation checks
   - Troubleshooting

3. **RFC Documentation**
   - This RFC
   - Implementation notes
   - Decision log

---

## Open Questions

1. **Q:** Should bot be default-enabled in CI?  
   **A:** Propose: Start opt-in, become default after validation

2. **Q:** How to test bot without recording?  
   **A:** Add `--bot-only=true` flag for bot testing

3. **Q:** Should scenarios be embedded or external files?  
   **A:** Propose: Embedded default, optional external override

4. **Q:** How to version scenarios?  
   **A:** Propose: Keep scenarios in git, version with code

---

## Related RFCs

- **RFC-0001:** Asciinema Recording for PTY Sessions
- **RFC-0009:** Dynamic Asciinema Recording in PTY
- **RFC-0008:** Playwright and Asciinema Testing Strategy

---

## Appendix A: Example Scenario File

See `recording-test-scenarios.json` in implementation PR for complete example.

---

## Appendix B: Action Handler API

```csharp
public interface IBotActionHandler
{
    string ActionName { get; }
    Task ExecuteAsync(ActionContext context, CancellationToken ct);
}

public class ActionContext
{
    public Dictionary<string, object> Parameters { get; set; }
    public BotRuleInput Input { get; set; }
    public TextWriter Output { get; set; }
}
```

---

**End of RFC-0042**
