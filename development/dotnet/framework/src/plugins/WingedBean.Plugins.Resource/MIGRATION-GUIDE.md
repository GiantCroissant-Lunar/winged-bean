# Migration Guide: RFC-0054 Resource Management Enhancement

**Last Updated:** 2025-10-15  
**Applies To:** WingedBean.Plugins.Resource v2.0+ (RFC-0054 Phase 2)

## Overview

WingedBean's resource system has been enhanced with **automatic reference counting** and **lifecycle management** (RFC-0054). This guide helps you migrate existing code to take advantage of the new capabilities while maintaining backward compatibility.

## ✅ Good News: Your Code Still Works!

**No breaking changes** - All existing code using `IResourceService` continues working exactly as before. The enhancements are opt-in.

## What Changed?

### Internal Changes (Transparent)

`FileSystemResourceService` now uses `ResourceManager` internally, which provides:
- ✅ **Automatic load deduplication** - Multiple concurrent loads → single I/O
- ✅ **Thread-safe caching** - Safe resource sharing between systems
- ✅ **Smart cleanup** - Resources automatically unloaded after idle timeout

### New API (Opt-In)

You can now use the **handle-based API** for automatic resource management:

```csharp
// Old API (still works):
var texture = await resourceService.LoadAsync<Texture>("texture.png");
// ... use texture ...
// Must remember to call Unload() - easy to forget!

// New API (automatic cleanup):
using var handle = resourceManager.GetHandle<Texture>("texture.png");
var texture = await handle.GetAsync();
// ... use texture ...
// Automatic cleanup when handle disposed ✨
```

## Migration Patterns

### Pattern 1: Simple Load (No Change Required)

**Before (RFC-0027):**
```csharp
public class MySystem
{
    private readonly IResourceService _resources;

    public async Task Initialize()
    {
        var config = await _resources.LoadAsync<GameConfig>("config.json");
        // ... use config ...
    }
}
```

**After (RFC-0054 - Same Code!):**
```csharp
public class MySystem
{
    private readonly IResourceService _resources;

    public async Task Initialize()
    {
        var config = await _resources.LoadAsync<GameConfig>("config.json");
        // ... use config ...
        // ✅ Works exactly the same
        // ✅ Benefits from load deduplication automatically
    }
}
```

**Action:** ✅ **No action required** - code works unchanged with automatic improvements.

---

### Pattern 2: Load with Manual Unload → Automatic Handle

**Before (RFC-0027):**
```csharp
public class LevelLoader
{
    private readonly IResourceService _resources;

    public async Task LoadLevel(string levelId)
    {
        var level = await _resources.LoadAsync<LevelData>($"levels/{levelId}.json");
        
        // ... use level ...
        
        // Manual cleanup - easy to forget!
        _resources.Unload($"levels/{levelId}.json");
    }
}
```

**After (RFC-0054 - Recommended):**
```csharp
public class LevelLoader
{
    private readonly ResourceManager _resources; // Inject ResourceManager instead

    public async Task LoadLevel(string levelId)
    {
        using var handle = _resources.GetHandle<LevelData>($"levels/{levelId}.json");
        var level = await handle.GetAsync();
        
        // ... use level ...
        
        // ✅ Automatic cleanup on dispose - no Unload() needed!
    }
}
```

**Action:** 🔄 **Migrate gradually** - Replace `IResourceService` with `ResourceManager` injection and use handles.

---

### Pattern 3: Long-Lived Resource → Keep Handle Alive

**Before (RFC-0027):**
```csharp
public class GameSession
{
    private readonly IResourceService _resources;
    private GameData? _gameData;

    public async Task Start()
    {
        _gameData = await _resources.LoadAsync<GameData>("game-data.json");
        // Resource stays loaded but no lifetime management
    }

    public void Stop()
    {
        // Must remember to unload
        _resources.Unload("game-data.json");
        _gameData = null;
    }
}
```

**After (RFC-0054 - Recommended):**
```csharp
public class GameSession : IDisposable
{
    private readonly ResourceManager _resources;
    private ResourceHandle<GameData>? _handle;
    private GameData? _gameData;

    public async Task Start()
    {
        _handle = _resources.GetHandle<GameData>("game-data.json");
        _gameData = await _handle.GetAsync();
        // ✅ Resource kept alive while handle exists
    }

    public void Dispose()
    {
        // ✅ Automatic cleanup
        _handle?.Dispose();
        _handle = null;
        _gameData = null;
    }
}
```

**Action:** 🔄 **Recommended** - Store `ResourceHandle<T>` as field and implement `IDisposable`.

---

### Pattern 4: Shared Resource Across Multiple Systems

**Before (RFC-0027 - Problematic):**
```csharp
// System A
public class RenderSystem
{
    public async Task Initialize()
    {
        var texture = await _resources.LoadAsync<Texture>("shared-texture.png");
        // ...
    }
}

// System B
public class UISystem
{
    public async Task Initialize()
    {
        var texture = await _resources.LoadAsync<Texture>("shared-texture.png");
        // ...
    }
    
    public void Cleanup()
    {
        // ❌ Problem: This unloads for BOTH systems!
        _resources.Unload("shared-texture.png");
    }
}
```

**After (RFC-0054 - Safe):**
```csharp
// System A
public class RenderSystem : IDisposable
{
    private ResourceHandle<Texture>? _textureHandle;

    public async Task Initialize()
    {
        _textureHandle = _resources.GetHandle<Texture>("shared-texture.png");
        var texture = await _textureHandle.GetAsync();
        // ...
    }

    public void Dispose() => _textureHandle?.Dispose();
}

// System B
public class UISystem : IDisposable
{
    private ResourceHandle<Texture>? _textureHandle;

    public async Task Initialize()
    {
        _textureHandle = _resources.GetHandle<Texture>("shared-texture.png");
        var texture = await _textureHandle.GetAsync();
        // ✅ Same texture, reference counted
    }

    public void Dispose()
    {
        _textureHandle?.Dispose();
        // ✅ Only unloads when BOTH systems dispose their handles
    }
}
```

**Action:** 🎯 **Critical for shared resources** - Use handles to prevent premature unloads.

---

### Pattern 5: Weak Reference (Observer Pattern)

**New in RFC-0054:**

Use `WeakResourceHandle<T>` when you want to **observe** a resource without keeping it alive.

```csharp
public class ResourceMonitor
{
    private readonly ResourceManager _resources;

    public void WatchResource(string resourceId)
    {
        // Weak handle - doesn't increment refcount
        var weakHandle = _resources.GetWeakHandle<GameData>(resourceId);

        // Subscribe to lifecycle events
        weakHandle.OnLoaded.Subscribe(data =>
        {
            Console.WriteLine($"Resource loaded: {data.Name}");
        });

        weakHandle.OnUnloaded.Subscribe(_ =>
        {
            Console.WriteLine("Resource unloaded");
        });

        // ✅ Resource can still be unloaded even though we're observing
    }
}
```

**Action:** 🆕 **New pattern** - Use for logging, debugging, UI updates that don't own resources.

---

### Pattern 6: Reactive Patterns with Observables

**New in RFC-0054:**

Resource handles expose `IObservable<T>` streams for reactive programming.

```csharp
public class ReactiveLoader
{
    private readonly ResourceManager _resources;

    public void PreloadWithProgress(string[] resourceIds)
    {
        var handles = resourceIds
            .Select(id => _resources.GetHandle<LevelData>(id))
            .ToArray();

        // Track loading progress reactively
        var loadedCount = 0;
        foreach (var handle in handles)
        {
            handle.OnLoaded.Subscribe(_ =>
            {
                loadedCount++;
                Console.WriteLine($"Loaded {loadedCount}/{handles.Length}");
            });
        }

        // Start loading
        var tasks = handles.Select(h => h.GetAsync());
        await Task.WhenAll(tasks.Select(t => t.AsTask()));

        // Cleanup
        foreach (var handle in handles)
        {
            handle.Dispose();
        }
    }
}
```

**Action:** 🆕 **New pattern** - Use for progress tracking, loading screens, reactive UIs.

---

## Dependency Injection Changes

### Before (RFC-0027):

```csharp
public class MyService
{
    private readonly IResourceService _resources;

    public MyService(IResourceService resources)
    {
        _resources = resources; // Only IResourceService available
    }
}
```

### After (RFC-0054):

```csharp
// Option 1: Use IResourceService (backward compatible)
public class MyService
{
    private readonly IResourceService _resources;

    public MyService(IResourceService resources)
    {
        _resources = resources; // ✅ Still works
    }
}

// Option 2: Use ResourceManager (new API)
public class MyService
{
    private readonly ResourceManager _resources;

    public MyService(ResourceManager resources)
    {
        _resources = resources; // ✅ New handle-based API
    }
}

// Option 3: Use both (for migration)
public class MyService
{
    private readonly IResourceService _legacyService;
    private readonly ResourceManager _manager;

    public MyService(IResourceService legacy, ResourceManager manager)
    {
        _legacyService = legacy;
        _manager = manager;
        // Use legacy for old code paths, manager for new
    }
}
```

**Registration (Automatic via Plugin):**

Both `IResourceService` and `ResourceManager` are automatically registered by `ResourcePluginActivator`. No changes needed to your DI configuration!

---

## ECS Integration (Arch ECS)

### Component with Resource Handle:

```csharp
public struct TextureComponent : IComponent
{
    public ResourceHandle<Texture> Handle;
}

public class SpriteRenderSystem : GameSystem
{
    private readonly ResourceManager _resources;

    public void CreateSprite(World world, string texturePath)
    {
        // Create handle and store in component
        var handle = _resources.GetHandle<Texture>(texturePath);
        
        world.Create(
            new TextureComponent { Handle = handle },
            new Transform { Position = Vector2.Zero }
        );
    }

    public override void Update(World world, GameTime time)
    {
        var query = world.Query<TextureComponent, Transform>();
        
        foreach (var entity in query)
        {
            var (texComp, transform) = entity.Get<TextureComponent, Transform>();
            
            // Use handle to get resource
            var texture = texComp.Handle.GetIfLoaded();
            if (texture != null)
            {
                RenderSprite(texture, transform.Position);
            }
        }
    }

    // IMPORTANT: Clean up handles when entity destroyed!
    public void OnEntityDestroyed(Entity entity)
    {
        if (entity.Has<TextureComponent>())
        {
            var texComp = entity.Get<TextureComponent>();
            texComp.Handle.Dispose(); // ✅ Release reference
        }
    }
}
```

**Key Point:** Always dispose handles when ECS entities are destroyed!

---

## Common Pitfalls & Solutions

### ❌ Pitfall 1: Forgetting to Dispose Handles

```csharp
// BAD
public async Task LoadTexture()
{
    var handle = _resources.GetHandle<Texture>("texture.png");
    var texture = await handle.GetAsync();
    return texture;
    // ❌ Handle never disposed - memory leak!
}
```

```csharp
// GOOD
public async Task<Texture?> LoadTexture()
{
    using var handle = _resources.GetHandle<Texture>("texture.png");
    return await handle.GetAsync();
    // ✅ Handle automatically disposed
}
```

### ❌ Pitfall 2: Using Handle After Disposal

```csharp
// BAD
var handle = _resources.GetHandle<Texture>("texture.png");
handle.Dispose();
var texture = await handle.GetAsync(); // ❌ ObjectDisposedException!
```

```csharp
// GOOD
using var handle = _resources.GetHandle<Texture>("texture.png");
var texture = await handle.GetAsync();
// Only use texture while handle is alive
```

### ❌ Pitfall 3: Storing Resource Without Handle

```csharp
// BAD
private Texture? _texture;

public async Task LoadTexture()
{
    using var handle = _resources.GetHandle<Texture>("texture.png");
    _texture = await handle.GetAsync();
    // ❌ Texture might be unloaded after handle disposes!
}
```

```csharp
// GOOD
private ResourceHandle<Texture>? _handle;

public async Task LoadTexture()
{
    _handle = _resources.GetHandle<Texture>("texture.png");
    var texture = await _handle.GetAsync();
    // ✅ Texture kept alive while _handle exists
}

public void Dispose()
{
    _handle?.Dispose();
}
```

---

## Testing Your Migration

### Unit Test Example:

```csharp
[Fact]
public async Task MigratedCode_WorksWithHandles()
{
    // Arrange
    var provider = new TestProvider();
    provider.AddResource("test", new TestResource());
    
    var manager = new ResourceManager(logger, new[] { provider });

    // Act
    using var handle = manager.GetHandle<TestResource>("test");
    var resource1 = await handle.GetAsync();
    var resource2 = await handle.GetAsync();

    // Assert
    resource1.Should().BeSameAs(resource2);
    handle.IsLoaded.Should().BeTrue();
}
```

---

## Performance Considerations

### Load Deduplication

**Before (RFC-0027):**
```csharp
// These would load the file 3 times
var task1 = _resources.LoadAsync<Config>("config.json");
var task2 = _resources.LoadAsync<Config>("config.json");
var task3 = _resources.LoadAsync<Config>("config.json");
await Task.WhenAll(task1, task2, task3);
```

**After (RFC-0054):**
```csharp
// These load the file only ONCE (deduplicated)
var task1 = _resources.LoadAsync<Config>("config.json");
var task2 = _resources.LoadAsync<Config>("config.json");
var task3 = _resources.LoadAsync<Config>("config.json");
await Task.WhenAll(task1, task2, task3);
// ✅ Automatic deduplication!
```

### Memory Overhead

- **Per resource**: ~48 bytes (refcount, state, timestamps, observables)
- **Per handle**: 32 bytes
- **Trade-off**: Small overhead for safety and convenience

---

## Checklist: Migration Steps

- [ ] **Phase 1:** Verify existing code still works (should be automatic)
- [ ] **Phase 2:** Identify long-lived resources that need handles
- [ ] **Phase 3:** Migrate one system at a time to use `ResourceManager`
- [ ] **Phase 4:** Replace `IResourceService` injection with `ResourceManager`
- [ ] **Phase 5:** Use `using` statements or `IDisposable` for handles
- [ ] **Phase 6:** Remove manual `Unload()` calls (now automatic)
- [ ] **Phase 7:** Add lifecycle event subscriptions if needed
- [ ] **Phase 8:** Test with concurrent loads to verify deduplication
- [ ] **Phase 9:** Profile memory usage (should be similar or better)
- [ ] **Phase 10:** Document your resource ownership model

---

## Getting Help

### Debugging

Enable debug logging to see resource lifecycle:

```csharp
var options = new ResourceManagerOptions
{
    EnableDebugLogging = true // Logs every AddRef/RemoveRef
};
```

### Common Questions

**Q: Can I mix old and new APIs?**  
A: Yes! Both `IResourceService` and `ResourceManager` work together.

**Q: Do I have to migrate all code at once?**  
A: No. Migrate gradually, one system at a time.

**Q: What about Unity/Godot?**  
A: Phase 2 focuses on Console profile. Unity/Godot profiles will be addressed in separate RFCs.

**Q: Are there breaking changes?**  
A: No. All existing code continues working unchanged.

---

## Further Reading

- **RFC-0054**: Full specification
- **Phase 1 Implementation**: Core infrastructure details
- **API Reference**: Generated XML documentation
- **Examples**: See tests in `WingedBean.Plugins.Resource.Tests`

---

**Ready to migrate? Start with Pattern 1 (no changes needed!) and gradually adopt handles where they add value.** 🚀
