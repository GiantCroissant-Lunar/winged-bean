# The REAL Problem: Plugin Dependency Chain Broken

## Root Cause

**DungeonGame plugin** tries to get `ArchECSService` from the registry in its constructor, but ArchECS plugin hasn't activated yet, even though it loaded first!

##Plugin Load Order vs Activation

```
1. Resource loads → activates
2. ArchECS loads → BUT activates AFTER DungeonGame tries!
3. WebSocket loads → activates
4. TerminalUI loads → activates
5. AsciinemaRecorder loads → activates
6. DungeonGame loads → OnActivateAsync called → constructor tries registry.Get<ArchECSService>() → FAILS!
```

## The Error

```csharp
// DungeonGameService.cs line 32
public DungeonGameService(IRegistry registry)
{
    _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    _ecs = _registry.Get<ArchECSService>();  // ← FAILS HERE
}
```

When DungeonGamePlugin.OnActivateAsync is called, it does:
```csharp
_serviceInstance = new DungeonGameService(registry); // Line 40
```

But ArchECS hasn't registered ArchECSService yet!

## Why Priority Didn't Help

The manifest priority controls LOAD order but doesn't guarantee ACTIVATION order. All plugins load, then they ALL activate, possibly in parallel or different order.

## Solution 1: Lazy Resolution

Change DungeonGameService to resolve ArchECS lazily:

```csharp
public DungeonGameService(IRegistry registry)
{
    _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    // Don't get ECS in constructor - get it when Initialize() is called
}

public void Initialize()
{
    _ecs = _registry.Get<ArchECSService>();  // Get it here instead
    _game.Initialize();
    _ecs.SetMode(GameMode.Play);
}
```

## Solution 2: Add Dependencies to Manifest

Add dependencies field to DungeonGame manifest:

```json
{
  "dependencies": {
    "wingedbean.plugins.archecs": "^1.0.0"
  }
}
```

This should make the plugin loader wait for ArchECS to activate before activating DungeonGame.

## Solution 3: Try/Catch and Retry

Make DungeonGameService handle missing dependencies gracefully and retry.

## Recommended: Solution 1 (Lazy Resolution)

This is the cleanest and most reliable approach.

---

I'll implement Solution 1 now.
