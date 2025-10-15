---
id: RFC-0050
title: Fog of War Rendering System
status: Proposed
category: game-engine, rendering, gameplay
created: 2025-10-14
updated: 2025-10-14
priority: P0
effort: 3-4 hours
---

# RFC-0050: Fog of War Rendering System

**Status:** Proposed  
**Date:** 2025-10-14  
**Author:** Development Team  
**Category:** game-engine, rendering, gameplay  
**Priority:** CRITICAL (P0)  
**Estimated Effort:** 3-4 hours  
**Depends On:** 
- RFC-0007: Arch ECS Integration (Implemented)
- RFC-0043: Tile-Based Map System (In Progress)
- RFC-0049: Camera System (Proposed)
- GoRogueFOVSystem (Existing)

---

## Summary

Implement **fog of war rendering** that visually distinguishes between visible tiles (in FOV), explored tiles (seen before), and unexplored tiles (never seen). Integrates with existing `GoRogueFOVSystem` to provide classic roguelike exploration experience.

---

## Motivation

### Current Problem

FOV calculation works ✅ but rendering doesn't use it ❌:

```csharp
// GoRogueFOVSystem.cs - Calculates FOV correctly
fovComp.FOV.Calculate(pos.X, pos.Y, fovComp.Range);
_currentMap.Tiles[x, y].IsVisible = true;
_currentMap.Tiles[x, y].IsExplored = true;

// RenderSystem.cs - Ignores FOV data!
buffer[y, x] = tile.Glyph; // Renders ALL tiles at full brightness
```

**Issues:**
1. **No fog of war** - Can see entire map
2. **No exploration feel** - Everything visible from start
3. **No tactical advantage** from vision
4. **Breaks roguelike conventions**

### Why Fog of War?

**Gameplay Benefits:**
- 🌫️ **Exploration** - Discover dungeon gradually
- 👁️ **Tactical** - Vision range matters
- 🎯 **Tension** - Don't know what's around corner
- 🎮 **Classic Roguelike Feel**

**Visual States:**
- **Visible** (in FOV) - Full brightness, see entities
- **Explored** (memory) - Dim, see terrain only
- **Unexplored** - Black/hidden

### Success Criteria

✅ Three visual states (visible/explored/unexplored)  
✅ Entities only visible in FOV  
✅ Explored tiles show terrain but dimmed  
✅ Smooth visual transitions  
✅ Performance: No FPS impact  
✅ Configurable fog modes

---

## Proposal

### 1. Fog of War Configuration

```csharp
namespace WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// Fog of war rendering configuration.
/// Singleton component attached to world.
/// </summary>
public class FogOfWarConfig
{
    /// <summary>Fog of war mode</summary>
    public FogMode Mode { get; set; } = FogMode.Standard;
    
    /// <summary>Brightness of explored tiles (0.0-1.0)</summary>
    public float ExploredBrightness { get; set; } = 0.4f;
    
    /// <summary>Brightness of visible tiles (0.0-1.0)</summary>
    public float VisibleBrightness { get; set; } = 1.0f;
    
    /// <summary>Show entities in explored tiles?</summary>
    public bool ShowEntitiesInMemory { get; set; } = false;
    
    /// <summary>Unexplored tile character</summary>
    public char UnexploredGlyph { get; set; } = ' ';
    
    /// <summary>Explored tile color modifier</summary>
    public ConsoleColor ExploredColor { get; set; } = ConsoleColor.DarkGray;
}

public enum FogMode
{
    /// <summary>No fog of war (see everything)</summary>
    None,
    
    /// <summary>Standard roguelike (visible/explored/unexplored)</summary>
    Standard,
    
    /// <summary>Only show currently visible (no memory)</summary>
    OnlyVisible,
    
    /// <summary>Gradual fade based on distance from FOV</summary>
    GradualFade
}
```

---

### 2. Tile Visibility State

```csharp
namespace WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// Visibility state for rendering.
/// Calculated from FOV data.
/// </summary>
public enum TileVisibility
{
    /// <summary>Never seen, render as black/hidden</summary>
    Unexplored,
    
    /// <summary>Seen before but not in current FOV, render dimmed</summary>
    Explored,
    
    /// <summary>Currently in FOV, render at full brightness</summary>
    Visible
}
```

---

### 3. Enhanced Render System with Fog of War

```csharp
namespace WingedBean.Plugins.DungeonGame.Systems;

using WingedBean.Plugins.ArchECS.Internal;
using WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// Render system with fog of war support.
/// </summary>
public class FogOfWarRenderSystem : IECSSystem
{
    private readonly List<(Position pos, Renderable render, bool isPlayer)> _renderBuffer = new();
    
    public string? LastRenderedFrame { get; private set; }
    
    public void Execute(IWorld world, float deltaTime)
    {
        var cameraQuery = world.CreateQuery<Camera, ViewportBounds>();
        var cameraEntity = cameraQuery.FirstOrDefault();
        
        if (cameraEntity == default)
            return;
        
        var camera = world.GetComponent<Camera>(cameraEntity);
        var viewport = world.GetComponent<ViewportBounds>(cameraEntity);
        var fogConfig = world.GetSingleton<FogOfWarConfig>() ?? new FogOfWarConfig();
        
        // Get player FOV data
        var playerFOV = GetPlayerFOV(world);
        
        _renderBuffer.Clear();
        
        // Collect renderable entities
        foreach (var entity in world.CreateQuery<Position, Renderable>())
        {
            var pos = world.GetComponent<Position>(entity);
            
            if (!viewport.Contains(pos.X, pos.Y))
                continue;
            
            var render = world.GetComponent<Renderable>(entity);
            bool isPlayer = world.HasComponent<Player>(entity);
            
            _renderBuffer.Add((pos, render, isPlayer));
        }
        
        _renderBuffer.Sort((a, b) => a.render.RenderLayer.CompareTo(b.render.RenderLayer));
        
        LastRenderedFrame = RenderWithFogOfWar(world, camera, viewport, fogConfig, playerFOV);
    }
    
    private string RenderWithFogOfWar(
        IWorld world,
        Camera camera,
        ViewportBounds viewport,
        FogOfWarConfig fogConfig,
        HashSet<Point>? visibleTiles)
    {
        var map = world.GetSingleton<DungeonMap>();
        if (map == null)
            return string.Empty;
        
        var buffer = new char[camera.ViewportHeight, camera.ViewportWidth];
        var colorBuffer = new TileVisibility[camera.ViewportHeight, camera.ViewportWidth];
        
        // Render map tiles with fog of war
        for (int worldY = viewport.MinY; worldY <= viewport.MaxY; worldY++)
        {
            for (int worldX = viewport.MinX; worldX <= viewport.MaxX; worldX++)
            {
                var (vx, vy) = viewport.WorldToViewport(worldX, worldY);
                
                if (vx < 0 || vx >= camera.ViewportWidth ||
                    vy < 0 || vy >= camera.ViewportHeight)
                    continue;
                
                if (!map.IsInBounds(worldX, worldY))
                {
                    buffer[vy, vx] = ' ';
                    colorBuffer[vy, vx] = TileVisibility.Unexplored;
                    continue;
                }
                
                var tile = map.GetTile(worldX, worldY);
                if (tile == null)
                    continue;
                
                // Determine visibility state
                var visibility = GetTileVisibility(
                    worldX, worldY, 
                    tile.Value, 
                    visibleTiles, 
                    fogConfig);
                
                colorBuffer[vy, vx] = visibility;
                
                // Render based on visibility
                switch (visibility)
                {
                    case TileVisibility.Unexplored:
                        buffer[vy, vx] = fogConfig.UnexploredGlyph;
                        break;
                        
                    case TileVisibility.Explored:
                        buffer[vy, vx] = GetExploredGlyph(tile.Value);
                        break;
                        
                    case TileVisibility.Visible:
                        buffer[vy, vx] = tile.Value.Glyph;
                        break;
                }
            }
        }
        
        // Render entities (only if visible or player)
        foreach (var (pos, render, isPlayer) in _renderBuffer)
        {
            var (vx, vy) = viewport.WorldToViewport(pos.X, pos.Y);
            
            if (vx < 0 || vx >= camera.ViewportWidth ||
                vy < 0 || vy >= camera.ViewportHeight)
                continue;
            
            var visibility = colorBuffer[vy, vx];
            
            // Always show player
            if (isPlayer)
            {
                buffer[vy, vx] = render.Symbol;
                continue;
            }
            
            // Only show entities in visible tiles
            if (visibility == TileVisibility.Visible)
            {
                buffer[vy, vx] = render.Symbol;
            }
            else if (visibility == TileVisibility.Explored && fogConfig.ShowEntitiesInMemory)
            {
                // Optionally show entities in memory (dimmed)
                buffer[vy, vx] = render.Symbol;
            }
        }
        
        // Convert to string
        var sb = new StringBuilder();
        for (int y = 0; y < camera.ViewportHeight; y++)
        {
            for (int x = 0; x < camera.ViewportWidth; x++)
            {
                sb.Append(buffer[y, x]);
            }
            if (y < camera.ViewportHeight - 1)
                sb.AppendLine();
        }
        
        return sb.ToString();
    }
    
    private TileVisibility GetTileVisibility(
        int x, int y,
        Tile tile,
        HashSet<Point>? visibleTiles,
        FogOfWarConfig fogConfig)
    {
        if (fogConfig.Mode == FogMode.None)
            return TileVisibility.Visible;
        
        var point = new Point(x, y);
        
        // Check if in current FOV
        bool isVisible = visibleTiles != null && visibleTiles.Contains(point);
        
        if (isVisible)
            return TileVisibility.Visible;
        
        if (fogConfig.Mode == FogMode.OnlyVisible)
            return TileVisibility.Unexplored;
        
        // Check if explored
        if (tile.IsExplored)
            return TileVisibility.Explored;
        
        return TileVisibility.Unexplored;
    }
    
    private char GetExploredGlyph(Tile tile)
    {
        // Return simplified glyph for explored tiles
        // Could be same as visible or simplified version
        return tile.Glyph;
    }
    
    private HashSet<Point>? GetPlayerFOV(IWorld world)
    {
        // Find player entity with FOV
        var playerQuery = world.CreateQuery<Player, MapMemoryComponent>();
        var playerEntity = playerQuery.FirstOrDefault();
        
        if (playerEntity == default)
            return null;
        
        var memory = world.GetComponent<MapMemoryComponent>(playerEntity);
        return memory.VisibleTiles;
    }
}
```

---

### 4. Color/Brightness Support (Future)

For terminals that support color:

```csharp
public struct RenderTile
{
    public char Glyph;
    public ConsoleColor Foreground;
    public ConsoleColor Background;
    public float Brightness; // 0.0-1.0
}

// Apply brightness to color
private ConsoleColor ApplyBrightness(ConsoleColor color, float brightness)
{
    if (brightness >= 1.0f)
        return color;
    
    if (brightness <= 0.3f)
        return ConsoleColor.DarkGray;
    
    if (brightness <= 0.6f)
        return GetDarkVersion(color);
    
    return color;
}
```

---

## Implementation Plan

### Phase 1: Core Fog System (2 hours)
- [ ] Create `FogOfWarConfig` component
- [ ] Create `TileVisibility` enum
- [ ] Update `RenderSystem` with fog logic
- [ ] Test three visibility states

### Phase 2: Integration (1 hour)
- [ ] Integrate with `GoRogueFOVSystem`
- [ ] Test with player movement
- [ ] Verify explored tiles persist
- [ ] Test with camera system

### Phase 3: Polish (1 hour)
- [ ] Add fog mode options
- [ ] Brightness/color support
- [ ] Configuration UI
- [ ] Performance testing

---

## Testing Strategy

### Unit Tests
- Tile visibility calculation
- Fog mode switching
- Explored tile persistence

### Integration Tests
- Player explores dungeon, tiles stay explored
- Entities only visible in FOV
- Fog works with camera movement
- Performance with large maps

### Visual Tests
- Screenshot comparison
- Fog transitions smooth
- Colors/brightness correct

---

## User Experience

### Before (No Fog)
```
##########
#........#
#..@.....#  <- Can see entire room
#...E....#     including enemy (E)
##########
```

### After (With Fog)
```
####
#..#       <- Only see tiles in FOV
#.@#          Enemy not visible until
####          player gets closer
```

---

## Configuration Examples

### Classic Roguelike
```csharp
new FogOfWarConfig
{
    Mode = FogMode.Standard,
    ExploredBrightness = 0.4f,
    ShowEntitiesInMemory = false
}
```

### Full Visibility (Debug)
```csharp
new FogOfWarConfig
{
    Mode = FogMode.None
}
```

### Hardcore Mode
```csharp
new FogOfWarConfig
{
    Mode = FogMode.OnlyVisible, // No memory
    VisibleBrightness = 1.0f
}
```

---

## Related RFCs

- **RFC-0043**: Tile-Based Map System ⏭️ In Progress
- **RFC-0049**: Camera System ⏭️ Proposed
- **RFC-0051**: HUD System ⏭️ Proposed (next)
- **RFC-0055**: Dynamic Lighting ⏭️ Future

---

**Decision:** Proposed - Awaiting Review  
**Next Steps:**
1. Review with team
2. Prototype fog rendering
3. Test with different FOV ranges
4. Create implementation branch
