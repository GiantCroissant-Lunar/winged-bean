---
id: RFC-0051
title: HUD and UI Overlay System
status: Proposed
category: game-engine, ui, ux
created: 2025-10-14
updated: 2025-10-14
priority: P0
effort: 6-8 hours
---

# RFC-0051: HUD and UI Overlay System

**Status:** Proposed  
**Date:** 2025-10-14  
**Author:** Development Team  
**Category:** game-engine, ui, ux  
**Priority:** CRITICAL (P0)  
**Estimated Effort:** 6-8 hours  
**Depends On:** 
- RFC-0007: Arch ECS Integration (Implemented)
- RFC-0046: Turn-Based System (Proposed)
- RFC-0048: Inventory System (Proposed)
- RFC-0049: Camera System (Proposed)

---

## Summary

Implement a **HUD (Heads-Up Display) and UI overlay system** that displays critical game information: health/mana bars, message log, status effects, equipment, minimap, and turn indicator. The HUD is composable, data-driven, and integrates with ECS components.

---

## Motivation

### Current Problem

**No HUD** - Player has no feedback:
- Can't see health/mana
- No combat messages
- No status effects display
- No equipment view
- No turn indicator

**Impact:**
- Can't tell if taking damage
- Don't know whose turn it is
- Can't see what's equipped
- Poor game feel

### Why HUD System?

**Critical Information:**
- ❤️ **Health/Mana** - Survival info
- 📜 **Message Log** - Combat feedback
- ⚔️ **Equipment** - What's equipped
- 🎯 **Status Effects** - Buffs/debuffs
- 🗺️ **Minimap** - Spatial awareness
- ⏱️ **Turn Indicator** - Whose turn

---

## Proposal

### 1. HUD Layout

```
┌────────────────────────────────────────────────────────────┐
│ Player: Level 5 Warrior          HP: ████████░░ 80/100     │ <- Status Bar
│ Floor: 3                         MP: ██████████ 50/50      │
├────────────────────────────────────────────────────────────┤
│                                                            │
│                    GAME VIEWPORT                           │ <- Camera View
│                  (40x20 or larger)                         │
│                                                            │
├────────────────────────────────────────────────────────────┤
│ Equipment: Sword +2 | Leather Armor | Health Potion x3    │ <- Equipment Bar
├────────────────────────────────────────────────────────────┤
│ > You hit the goblin for 15 damage!                       │
│ > The goblin attacks you for 5 damage.                    │ <- Message Log
│ > You picked up a health potion.                          │
│ > Your turn.                                               │
└────────────────────────────────────────────────────────────┘
```

---

### 2. HUD Components

```csharp
namespace WingedBean.Plugins.DungeonGame.UI;

/// <summary>
/// HUD configuration and layout.
/// Singleton component.
/// </summary>
public class HUDConfig
{
    /// <summary>HUD layout mode</summary>
    public HUDLayout Layout { get; set; } = HUDLayout.Standard;
    
    /// <summary>Show health bar?</summary>
    public bool ShowHealthBar { get; set; } = true;
    
    /// <summary>Show mana bar?</summary>
    public bool ShowManaBar { get; set; } = true;
    
    /// <summary>Show message log?</summary>
    public bool ShowMessageLog { get; set; } = true;
    
    /// <summary>Message log height (lines)</summary>
    public int MessageLogHeight { get; set; } = 4;
    
    /// <summary>Show equipment bar?</summary>
    public bool ShowEquipment { get; set; } = true;
    
    /// <summary>Show minimap?</summary>
    public bool ShowMinimap { get; set; } = false;
    
    /// <summary>Show status effects?</summary>
    public bool ShowStatusEffects { get; set; } = true;
}

public enum HUDLayout
{
    Standard,    // Status top, log bottom
    Compact,     // Minimal HUD
    Full,        // All features
    None         // No HUD (screenshot mode)
}
```

---

### 3. Message Log System

```csharp
namespace WingedBean.Plugins.DungeonGame.UI;

/// <summary>
/// Message log for game events.
/// Singleton component.
/// </summary>
public class MessageLog
{
    private readonly Queue<Message> _messages = new();
    private readonly int _maxMessages = 100;
    
    public IEnumerable<Message> Messages => _messages;
    
    public void AddMessage(string text, MessageType type = MessageType.Info)
    {
        _messages.Enqueue(new Message
        {
            Text = text,
            Type = type,
            Timestamp = DateTime.Now
        });
        
        // Trim old messages
        while (_messages.Count > _maxMessages)
        {
            _messages.Dequeue();
        }
    }
    
    public IEnumerable<Message> GetRecent(int count)
    {
        return _messages.TakeLast(count);
    }
}

public struct Message
{
    public string Text { get; set; }
    public MessageType Type { get; set; }
    public DateTime Timestamp { get; set; }
}

public enum MessageType
{
    Info,        // Gray
    Combat,      // White
    Damage,      // Red
    Healing,     // Green
    Pickup,      // Yellow
    System,      // Cyan
    Error        // Red bold
}
```

---

### 4. HUD Render System

```csharp
namespace WingedBean.Plugins.DungeonGame.Systems;

using WingedBean.Plugins.ArchECS.Internal;
using WingedBean.Plugins.DungeonGame.Components;
using WingedBean.Plugins.DungeonGame.UI;

/// <summary>
/// Renders HUD overlay on top of game viewport.
/// </summary>
public class HUDRenderSystem : IECSSystem
{
    public string? LastRenderedHUD { get; private set; }
    
    public void Execute(IWorld world, float deltaTime)
    {
        var hudConfig = world.GetSingleton<HUDConfig>() ?? new HUDConfig();
        
        if (hudConfig.Layout == HUDLayout.None)
        {
            LastRenderedHUD = string.Empty;
            return;
        }
        
        var sb = new StringBuilder();
        
        // Render status bar
        if (hudConfig.ShowHealthBar || hudConfig.ShowManaBar)
        {
            sb.AppendLine(RenderStatusBar(world, hudConfig));
        }
        
        // Render equipment bar
        if (hudConfig.ShowEquipment)
        {
            sb.AppendLine(RenderEquipmentBar(world));
        }
        
        // Render message log
        if (hudConfig.ShowMessageLog)
        {
            sb.Append(RenderMessageLog(world, hudConfig.MessageLogHeight));
        }
        
        LastRenderedHUD = sb.ToString();
    }
    
    private string RenderStatusBar(IWorld world, HUDConfig config)
    {
        // Find player
        var playerQuery = world.CreateQuery<Player, Stats>();
        var playerEntity = playerQuery.FirstOrDefault();
        
        if (playerEntity == default)
            return "No player found";
        
        var stats = world.GetComponent<Stats>(playerEntity);
        
        var sb = new StringBuilder();
        
        // Left side: Player info
        sb.Append($"Player: Level {stats.Level}");
        sb.Append("  ");
        
        // Right side: Health/Mana
        if (config.ShowHealthBar)
        {
            sb.Append($"HP: {RenderBar(stats.CurrentHP, stats.MaxHP, 10)} {stats.CurrentHP}/{stats.MaxHP}");
            sb.Append("  ");
        }
        
        if (config.ShowManaBar)
        {
            sb.Append($"MP: {RenderBar(stats.CurrentMana, stats.MaxMana, 10)} {stats.CurrentMana}/{stats.MaxMana}");
        }
        
        return sb.ToString();
    }
    
    private string RenderBar(int current, int max, int width)
    {
        if (max == 0)
            return new string('░', width);
        
        float ratio = (float)current / max;
        int filled = (int)(ratio * width);
        
        return new string('█', filled) + new string('░', width - filled);
    }
    
    private string RenderEquipmentBar(IWorld world)
    {
        var playerQuery = world.CreateQuery<Player, Equipment, Inventory>();
        var playerEntity = playerQuery.FirstOrDefault();
        
        if (playerEntity == default)
            return "Equipment: None";
        
        var equipment = world.GetComponent<Equipment>(playerEntity);
        var inventory = world.GetComponent<Inventory>(playerEntity);
        
        var sb = new StringBuilder();
        sb.Append("Equipment: ");
        
        // Show equipped items
        var items = new List<string>();
        
        if (equipment.Weapon.HasValue)
        {
            var weaponItem = world.GetComponent<Item>(equipment.Weapon.Value);
            items.Add(weaponItem.Name);
        }
        
        if (equipment.Armor.HasValue)
        {
            var armorItem = world.GetComponent<Item>(equipment.Armor.Value);
            items.Add(armorItem.Name);
        }
        
        // Show consumables count
        int potionCount = CountConsumables(world, inventory, "HealthPotion");
        if (potionCount > 0)
        {
            items.Add($"Health Potion x{potionCount}");
        }
        
        sb.Append(items.Count > 0 ? string.Join(" | ", items) : "None");
        
        return sb.ToString();
    }
    
    private string RenderMessageLog(IWorld world, int height)
    {
        var messageLog = world.GetSingleton<MessageLog>();
        if (messageLog == null)
            return string.Empty;
        
        var sb = new StringBuilder();
        var recent = messageLog.GetRecent(height).ToList();
        
        foreach (var message in recent)
        {
            sb.AppendLine($"> {message.Text}");
        }
        
        return sb.ToString();
    }
    
    private int CountConsumables(IWorld world, Inventory inventory, string subType)
    {
        int count = 0;
        
        foreach (var itemEntity in inventory.Items)
        {
            if (!world.IsAlive(itemEntity))
                continue;
            
            var item = world.GetComponent<Item>(itemEntity);
            
            if (item.Type == ItemType.Consumable && item.SubType == subType)
            {
                count += item.StackCount;
            }
        }
        
        return count;
    }
}
```

---

### 5. Message Log Integration

```csharp
// In CombatSystem.cs
public class CombatSystem : IECSSystem
{
    private readonly MessageLog _messageLog;
    
    public void Execute(IWorld world, float deltaTime)
    {
        var messageLog = world.GetSingleton<MessageLog>();
        if (messageLog == null)
            return;
        
        // Process combat
        foreach (var attacker in world.CreateQuery<TurnAction>())
        {
            var action = world.GetComponent<TurnAction>(attacker);
            
            if (action.Type != ActionType.MeleeAttack || !action.TargetEntity.HasValue)
                continue;
            
            var target = action.TargetEntity.Value;
            
            // Calculate damage
            int damage = CalculateDamage(world, attacker, target);
            
            // Apply damage
            ref var targetStats = ref world.GetComponent<Stats>(target);
            targetStats.CurrentHP -= damage;
            
            // Log message
            string attackerName = GetEntityName(world, attacker);
            string targetName = GetEntityName(world, target);
            
            messageLog.AddMessage(
                $"{attackerName} hit {targetName} for {damage} damage!",
                MessageType.Combat
            );
            
            // Check death
            if (targetStats.CurrentHP <= 0)
            {
                messageLog.AddMessage(
                    $"{targetName} died!",
                    MessageType.System
                );
            }
        }
    }
}
```

---

### 6. Turn Indicator

```csharp
// In TurnManagerSystem.cs
private void ProcessPlayerTurn(IWorld world, TurnOrder turnOrder)
{
    var messageLog = world.GetSingleton<MessageLog>();
    
    if (turnOrder.WaitingForPlayerInput && !_playerTurnAnnounced)
    {
        messageLog?.AddMessage("Your turn.", MessageType.System);
        _playerTurnAnnounced = true;
    }
    
    // ... rest of turn processing
}
```

---

## Implementation Plan

### Phase 1: Message Log (2 hours)
- [ ] Create `MessageLog` component
- [ ] Create `Message` struct
- [ ] Implement message queue
- [ ] Test message display

### Phase 2: Status Bar (2 hours)
- [ ] Create `HUDConfig` component
- [ ] Implement health/mana bars
- [ ] Render player stats
- [ ] Test with different values

### Phase 3: Equipment Bar (1 hour)
- [ ] Render equipped items
- [ ] Show consumable counts
- [ ] Test with inventory changes

### Phase 4: Integration (2 hours)
- [ ] Integrate with combat system
- [ ] Integrate with inventory system
- [ ] Integrate with turn system
- [ ] Full HUD testing

---

## Testing Strategy

### Unit Tests
- Message log queue management
- Health bar rendering
- Equipment display

### Integration Tests
- Messages appear on combat
- Health updates in real-time
- Equipment changes reflect immediately
- Turn indicator works

### Visual Tests
- HUD layout looks good
- Text alignment correct
- No overlap with viewport

---

## HUD Layouts

### Standard (Default)
```
┌──────────────────────────────────────┐
│ HP: ████████░░ 80/100  MP: ██████ 50 │
├──────────────────────────────────────┤
│          GAME VIEWPORT               │
├──────────────────────────────────────┤
│ Equipment: Sword | Armor             │
│ > You hit goblin for 15 damage!      │
│ > Goblin attacks for 5 damage.       │
└──────────────────────────────────────┘
```

### Compact
```
┌──────────────────────────────────────┐
│ HP:80/100 MP:50/50  Sword | Armor    │
├──────────────────────────────────────┤
│          GAME VIEWPORT               │
└──────────────────────────────────────┘
```

### Full (Future)
```
┌──────────────────────────────────────┐
│ HP: ████ 80/100  [Poisoned] [Hasted] │
│ MP: ████ 50/50   XP: 1250/2000       │
├──────────────────────────────────────┤
│          GAME VIEWPORT        │ Map  │
│                               │ #### │
│                               │ #@.# │
├──────────────────────────────────────┤
│ Weapon: Sword +2 (15 dmg)            │
│ Armor: Leather (5 def)               │
├──────────────────────────────────────┤
│ > Combat log messages...             │
└──────────────────────────────────────┘
```

---

## Related RFCs

- **RFC-0046**: Turn-Based System ⏭️ Proposed
- **RFC-0048**: Inventory System ⏭️ Proposed
- **RFC-0049**: Camera System ⏭️ Proposed
- **RFC-0053**: Combat Feedback ⏭️ Future

---

**Decision:** Proposed - Awaiting Review  
**Next Steps:**
1. Review with team
2. Prototype message log
3. Design HUD layout
4. Create implementation branch
