# GOAP: Use Directly vs Port - Decision Analysis

**Date:** 2025-10-13  
**Question:** Should we use GOAP as-is or port it?

---

## TL;DR - Recommendation

**✅ USE DIRECTLY (with thin adapter layer)**

**Why:**
1. GOAP is **Unity Package Manager (UPM)** only - **NO NuGet package exists**
2. Would need to **manually extract and port** anyway
3. Core is already **Unity-agnostic** (interfaces only)
4. **Thin adapter layer** is easier than full port
5. Can **contribute back** improvements to upstream

---

## Current State Analysis

### GOAP Distribution

| Platform | Available? | Package Manager |
|----------|-----------|-----------------|
| **Unity** | ✅ Yes | UPM (Unity Package Manager) |
| **NuGet** | ❌ No | Not published |
| **GitHub** | ✅ Yes | Source code only |

**Key Finding:** There is **NO official NuGet package** for GOAP.

### Package Structure

```
Package/
├── Runtime/
│   ├── CrashKonijn.Agent.Core/       ← Pure C# interfaces
│   ├── CrashKonijn.Agent.Runtime/    ← Minimal Unity deps
│   ├── CrashKonijn.Goap.Core/        ← Pure C# interfaces
│   ├── CrashKonijn.Goap.Resolver/    ← Pure C# (planning algorithm)
│   └── CrashKonijn.Goap.Runtime/     ← Unity MonoBehaviour wrappers
├── Editor/                            ← Unity Editor tools (not needed)
└── package.json                       ← Unity package manifest
```

**Dependencies:**
- `com.unity.collections` (1.2.4) - Unity's native collections
- No other dependencies

---

## Option Comparison

### Option A: Use Directly (Recommended) ✅

**Approach:**
1. Copy source code from GitHub to Plate layer
2. Create thin adapter layer to replace Unity-specific code
3. Keep core GOAP logic unchanged
4. Maintain upstream compatibility

**Pros:**
- ✅ **Minimal effort** - Only adapt Unity-specific parts
- ✅ **Upstream compatibility** - Can pull updates easily
- ✅ **Bug fixes** - Benefit from upstream improvements
- ✅ **Community support** - Active development and Discord
- ✅ **Proven code** - Battle-tested in production games
- ✅ **No reinventing wheel** - Core algorithm is solid

**Cons:**
- ⚠️ Need to replace `Unity.Collections` (NativeArray, etc.)
- ⚠️ Need to adapt MonoBehaviour wrappers
- ⚠️ Manual updates (no package manager)

**Effort Estimate:** 20-30 hours

---

### Option B: Full Port (Not Recommended) ❌

**Approach:**
1. Rewrite GOAP from scratch using original as reference
2. Redesign architecture for .NET
3. Implement own planning algorithm
4. Create own abstractions

**Pros:**
- ✅ Full control over codebase
- ✅ .NET-native design
- ✅ No Unity artifacts

**Cons:**
- ❌ **Massive effort** - 200+ hours of work
- ❌ **Bug risk** - Reimplementing complex algorithms
- ❌ **No upstream benefits** - Can't use bug fixes
- ❌ **Maintenance burden** - We own all bugs
- ❌ **Reinventing wheel** - Core algorithm already works
- ❌ **Testing overhead** - Need to retest everything

**Effort Estimate:** 200-300 hours

---

## Recommended Approach: Thin Adapter Layer

### Architecture

```
┌─────────────────────────────────────────────────────────┐
│    WingedBean.Plugins.DungeonGame (Game-Specific)      │
│  - AttackAction, FleeAction (implements IAction)       │
│  - DefeatBossGoal (implements IGoal)                   │
└─────────────────────────────────────────────────────────┘
                          ↓ uses
┌─────────────────────────────────────────────────────────┐
│         Plate.Goap.Adapters (Thin Layer)               │
│  - Replace Unity.Collections with System.Collections   │
│  - Replace MonoBehaviour with interfaces               │
│  - Replace Transform with custom Position struct       │
└─────────────────────────────────────────────────────────┘
                          ↓ wraps
┌─────────────────────────────────────────────────────────┐
│    Plate.Goap.Core (Copied from CrashKonijn)          │
│  - IAction, IGoal, ISensor (unchanged)                 │
│  - Planner/Resolver (unchanged)                        │
│  - Core algorithm (unchanged)                          │
└─────────────────────────────────────────────────────────┘
```

### What to Adapt

#### 1. Replace Unity.Collections

**Unity Code:**
```csharp
using Unity.Collections;

public class Planner
{
    private NativeArray<Node> nodes;
    private NativeHashMap<int, int> lookup;
}
```

**Adapted Code:**
```csharp
using System.Collections.Generic;

public class Planner
{
    private Node[] nodes;
    private Dictionary<int, int> lookup;
}
```

**Effort:** 5-10 hours

---

#### 2. Replace MonoBehaviour

**Unity Code:**
```csharp
public class GoapBehaviour : MonoBehaviour, IGoap
{
    void Update() { /* ... */ }
}
```

**Adapted Code:**
```csharp
public interface IGoapBehaviour : IGoap
{
    void Update(float deltaTime);
}

public class GoapService : IGoapBehaviour
{
    public void Update(float deltaTime) { /* ... */ }
}
```

**Effort:** 5-10 hours

---

#### 3. Replace Transform/Vector3

**Unity Code:**
```csharp
public interface ITarget
{
    Vector3 Position { get; }
    Transform Transform { get; }
}
```

**Adapted Code:**
```csharp
public interface ITarget
{
    Position3D Position { get; }
    ITransformable Transformable { get; }
}

public struct Position3D
{
    public float X, Y, Z;
}
```

**Effort:** 3-5 hours

---

#### 4. Replace Coroutines

**Unity Code:**
```csharp
IEnumerator PerformAction()
{
    yield return new WaitForSeconds(1f);
}
```

**Adapted Code:**
```csharp
async Task PerformAction()
{
    await Task.Delay(TimeSpan.FromSeconds(1));
}
```

**Effort:** 5-8 hours

---

### Total Adaptation Effort

| Task | Hours |
|------|-------|
| Replace Unity.Collections | 5-10 |
| Replace MonoBehaviour | 5-10 |
| Replace Transform/Vector3 | 3-5 |
| Replace Coroutines | 5-8 |
| Testing & Integration | 10-15 |
| **Total** | **28-48 hours** |

---

## Implementation Plan

### Phase 1: Copy Source (Week 1)

1. **Copy GOAP source** from GitHub to Plate layer
   ```
   plate-projects/goap-core/
   └── dotnet/
       └── framework/
           └── src/
               ├── Plate.Goap.Core/           ← Copy CrashKonijn.Goap.Core
               ├── Plate.Goap.Resolver/       ← Copy CrashKonijn.Goap.Resolver
               ├── Plate.Agent.Core/          ← Copy CrashKonijn.Agent.Core
               └── Plate.Goap.Adapters/       ← NEW: Adapter layer
   ```

2. **Keep namespaces** initially (easier to track changes)
3. **Document** which files are unchanged vs adapted

**Effort:** 2-3 hours

---

### Phase 2: Create Adapters (Week 1-2)

1. **Create adapter interfaces**
   ```csharp
   // Plate.Goap.Adapters/ICollectionsAdapter.cs
   public interface ICollectionsAdapter
   {
       T[] CreateArray<T>(int length);
       Dictionary<TKey, TValue> CreateHashMap<TKey, TValue>();
   }
   ```

2. **Implement adapters**
   ```csharp
   // Plate.Goap.Adapters/SystemCollectionsAdapter.cs
   public class SystemCollectionsAdapter : ICollectionsAdapter
   {
       public T[] CreateArray<T>(int length) => new T[length];
       public Dictionary<TKey, TValue> CreateHashMap<TKey, TValue>() 
           => new Dictionary<TKey, TValue>();
   }
   ```

3. **Replace Unity calls** with adapter calls

**Effort:** 15-20 hours

---

### Phase 3: Integration (Week 2-3)

1. **Create WingedBean plugin**
   ```
   WingedBean.Plugins.DungeonGame/
   └── AI/
       ├── Actions/
       │   ├── AttackAction.cs
       │   ├── FleeAction.cs
       │   └── PatrolAction.cs
       ├── Goals/
       │   ├── DefeatBossGoal.cs
       │   └── GuardTreasureGoal.cs
       └── Sensors/
           ├── EnemySensor.cs
           └── HealthSensor.cs
   ```

2. **Integrate with ECS**
   ```csharp
   public struct GoapAgentComponent
   {
       public IGoapAgent Agent;
       public IGoal CurrentGoal;
   }
   ```

3. **Test with dungeon game**

**Effort:** 15-20 hours

---

### Phase 4: Testing & Polish (Week 3-4)

1. **Unit tests** for adapters
2. **Integration tests** with game
3. **Performance profiling**
4. **Documentation**

**Effort:** 10-15 hours

---

## Maintenance Strategy

### Upstream Tracking

1. **Watch GitHub repo** for updates
2. **Pull bug fixes** from upstream
3. **Contribute back** adapter improvements
4. **Document changes** in CHANGELOG

### Version Management

```
Plate.Goap.Core v3.0.34-plate.1
                 ↑       ↑
                 |       └─ Plate adaptation version
                 └───────── Upstream GOAP version
```

---

## Comparison Table

| Aspect | Use Directly | Full Port |
|--------|--------------|-----------|
| **Effort** | 28-48 hours | 200-300 hours |
| **Risk** | Low (proven code) | High (new bugs) |
| **Upstream Benefits** | ✅ Yes | ❌ No |
| **Maintenance** | Low (pull updates) | High (own all bugs) |
| **Community Support** | ✅ Yes | ❌ No |
| **Customization** | Medium | High |
| **Time to Production** | 2-3 weeks | 2-3 months |

---

## Decision Matrix

| Criteria | Weight | Use Directly | Full Port |
|----------|--------|--------------|-----------|
| Development Speed | 30% | 9/10 | 2/10 |
| Code Quality | 25% | 9/10 | 5/10 |
| Maintainability | 20% | 8/10 | 4/10 |
| Flexibility | 15% | 6/10 | 10/10 |
| Risk | 10% | 9/10 | 3/10 |
| **Weighted Score** | | **8.3/10** | **4.4/10** |

---

## Final Recommendation

### ✅ Use GOAP Directly with Thin Adapter Layer

**Rationale:**
1. **No NuGet package exists** - Would need to copy source anyway
2. **Core is already Unity-agnostic** - Minimal adaptation needed
3. **Proven in production** - Used in shipped games
4. **Active development** - Regular updates and bug fixes
5. **Community support** - Discord, GitHub issues
6. **Time-efficient** - 28-48 hours vs 200-300 hours
7. **Lower risk** - Battle-tested code vs new implementation

**Next Steps:**
1. Copy GOAP source to `plate-projects/goap-core/`
2. Create adapter layer for Unity-specific code
3. Integrate with WingedBean dungeon game
4. Test and iterate

---

## Alternative: Wait for Official NuGet Package?

**Status:** No indication of official NuGet package planned

**Options:**
1. ❌ **Wait** - Could be months/years, may never happen
2. ✅ **Use directly** - Start now, benefit immediately
3. ⚠️ **Request from author** - Could ask CrashKonijn to publish NuGet

**Recommendation:** Don't wait. Use directly now.

---

## Conclusion

**Use GOAP directly with a thin adapter layer in the Plate layer.**

This gives you:
- ✅ Fast time to value (2-3 weeks)
- ✅ Proven, battle-tested code
- ✅ Upstream bug fixes and improvements
- ✅ Community support
- ✅ Minimal maintenance burden
- ✅ Ability to contribute back

**The adapter layer is the sweet spot between "use as-is" and "full port".**
