---
id: RFC-0048
title: Inventory and Item Management System (Revised - Using InventoryPoki)
status: Proposed
category: game-engine, ecs, gameplay
created: 2025-10-14
updated: 2025-10-15
priority: P1
effort: 4-6 hours (reduced from 8-10 hours)
---

# RFC-0048: Inventory and Item Management System (Revised)

**Status:** Proposed  
**Date:** 2025-10-15 (Revised)  
**Author:** Development Team  
**Category:** game-engine, ecs, gameplay  
**Priority:** HIGH (P1)  
**Estimated Effort:** 4-6 hours (reduced by leveraging InventoryPoki plugin)  
**Depends On:** 
- RFC-0007: Arch ECS Integration (Implemented)
- RFC-0046: Turn-Based Game Loop (Implemented)
- **Plate.Plugins.InventoryPoki** (Existing plugin)

---

## Summary

Implement inventory and item management by **integrating the existing InventoryPoki plugin** with the ECS-based dungeon game. InventoryPoki provides robust inventory abstractions (`IInventory`, `IItem`, `StackableItem`, `SlottedInventory`) that we'll bridge to ECS components. This approach reduces implementation time from 8-10 hours to 4-6 hours while gaining battle-tested inventory logic.

---

## Motivation

### Why Use InventoryPoki?

**Existing Plugin Provides:**
- ✅ **`IInventory` interface** - Flexible inventory abstraction
- ✅ **`SlottedInventory`** - Capacity-based inventory with slots
- ✅ **`StackableItem`** - Items with quantity and max stack size
- ✅ **`UniqueItem`** - Non-stackable items
- ✅ **`InventoryTransaction`** - Transaction-based operations (add/remove/move)
- ✅ **`IItemSlot`** - Slot abstraction with drag/drop/swap
- ✅ **Constraints & Behaviors** - Extensible validation system
- ✅ **Well-tested** - xUnit tests already exist

**What We Need to Add:**
- ECS component wrappers
- Equipment slot management
- Item templates for dungeon game (weapons, armor, potions)
- Integration with turn-based system
- Consumable item effects

### Original vs Revised Approach

| Aspect | Original RFC | Revised RFC (InventoryPoki) |
|--------|--------------|----------------------------|
| **Inventory Logic** | Build from scratch | Use InventoryPoki |
| **Stacking** | Custom implementation | `StackableItem` ✅ |
| **Transactions** | Custom add/remove | `InventoryTransaction` ✅ |
| **Slots** | Custom slot system | `IItemSlot` ✅ |
| **Testing** | Write all tests | Reuse existing tests ✅ |
| **Effort** | 8-10 hours | **4-6 hours** ⚡ |

---

## Proposal

### 1. Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│              ECS Integration Layer                       │
│  ┌───────────────────────────────────────────────────┐  │
│  │  ECS Components (Arch ECS)                        │  │
│  │  - InventoryComponent (wraps IInventory)          │  │
│  │  - EquipmentComponent (equipment slots)           │  │
│  │  - ItemEntityComponent (links entity to IItem)    │  │
│  └───────────────────────────────────────────────────┘  │
│                         ↓                                │
│  ┌───────────────────────────────────────────────────┐  │
│  │  InventoryPoki Plugin                             │  │
│  │  - IInventory, SlottedInventory                   │  │
│  │  - IItem, StackableItem, UniqueItem               │  │
│  │  - InventoryTransaction                           │  │
│  │  - IItemSlot (drag/drop/swap)                     │  │
│  └───────────────────────────────────────────────────┘  │
│                         ↓                                │
│  ┌───────────────────────────────────────────────────┐  │
│  │  Game-Specific Layer                              │  │
│  │  - DungeonItemTemplates (weapons, armor, potions) │  │
│  │  - ConsumableEffects (HP/Mana restoration)        │  │
│  │  - EquipmentStats (damage, defense bonuses)       │  │
│  └───────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

---

### 2. ECS Component Wrappers

#### 2.1 InventoryComponent

```csharp
namespace WingedBean.Plugins.DungeonGame.Components;

using InventoryPoki.Inventories;

/// <summary>
/// ECS component that wraps InventoryPoki's IInventory.
/// Attached to entities that can carry items (player, NPCs, containers).
/// </summary>
public class InventoryComponent
{
    /// <summary>Underlying InventoryPoki inventory</summary>
    public IInventory Inventory { get; set; }
    
    /// <summary>Maximum weight capacity (0 = unlimited)</summary>
    public float MaxWeight { get; set; }
    
    /// <summary>Current total weight</summary>
    public float CurrentWeight { get; set; }
    
    /// <summary>Gold/currency amount</summary>
    public int Gold { get; set; }
    
    public InventoryComponent(IInventory inventory, float maxWeight = 0)
    {
        Inventory = inventory;
        MaxWeight = maxWeight;
        CurrentWeight = 0;
        Gold = 0;
    }
}
```

#### 2.2 EquipmentComponent

```csharp
namespace WingedBean.Plugins.DungeonGame.Components;

using WingedBean.Plugins.ArchECS.Internal;

/// <summary>
/// Equipment slots for entities that can equip items.
/// Stores references to item entities.
/// </summary>
public class EquipmentComponent
{
    public EntityHandle? Weapon { get; set; }
    public EntityHandle? OffHand { get; set; }
    public EntityHandle? Helmet { get; set; }
    public EntityHandle? Armor { get; set; }
    public EntityHandle? Gloves { get; set; }
    public EntityHandle? Boots { get; set; }
    public EntityHandle? Ring1 { get; set; }
    public EntityHandle? Ring2 { get; set; }
    public EntityHandle? Amulet { get; set; }
    
    public IEnumerable<EntityHandle> GetEquippedItems()
    {
        if (Weapon.HasValue) yield return Weapon.Value;
        if (OffHand.HasValue) yield return OffHand.Value;
        if (Helmet.HasValue) yield return Helmet.Value;
        if (Armor.HasValue) yield return Armor.Value;
        if (Gloves.HasValue) yield return Gloves.Value;
        if (Boots.HasValue) yield return Boots.Value;
        if (Ring1.HasValue) yield return Ring1.Value;
        if (Ring2.HasValue) yield return Ring2.Value;
        if (Amulet.HasValue) yield return Amulet.Value;
    }
}

public enum EquipmentSlot
{
    Weapon, OffHand, Helmet, Armor, Gloves, Boots, Ring1, Ring2, Amulet
}
```

#### 2.3 ItemEntityComponent

```csharp
namespace WingedBean.Plugins.DungeonGame.Components;

using InventoryPoki.Items;

/// <summary>
/// Links an ECS entity to an InventoryPoki IItem.
/// Attached to item entities in the game world.
/// </summary>
public struct ItemEntityComponent
{
    /// <summary>Underlying InventoryPoki item</summary>
    public IItem Item { get; set; }
    
    /// <summary>Is this item currently equipped?</summary>
    public bool IsEquipped { get; set; }
    
    /// <summary>Equipment slot (if equippable)</summary>
    public EquipmentSlot? EquipSlot { get; set; }
}
```

---

### 3. Dungeon Game Item Templates

#### 3.1 DungeonItemTemplate

```csharp
namespace WingedBean.Plugins.DungeonGame.Items;

using InventoryPoki.Items;

/// <summary>
/// Base template for dungeon game items.
/// Extends InventoryPoki's ItemTemplate with game-specific properties.
/// </summary>
public class DungeonItemTemplate : ItemTemplate
{
    public ItemCategory Category { get; set; }
    public ItemRarity Rarity { get; set; }
    public int Value { get; set; } // Gold value
    public bool IsEquippable { get; set; }
    public EquipmentSlot? EquipmentSlot { get; set; }
}

public enum ItemCategory
{
    Weapon, Armor, Consumable, Accessory, Currency, QuestItem, Miscellaneous
}

public enum ItemRarity
{
    Common, Uncommon, Rare, Epic, Legendary, Artifact
}
```

#### 3.2 EquippableItemTemplate

```csharp
namespace WingedBean.Plugins.DungeonGame.Items;

/// <summary>
/// Template for equippable items (weapons, armor, accessories).
/// </summary>
public class EquippableItemTemplate : DungeonItemTemplate
{
    // Stat bonuses
    public int BonusDamage { get; set; }
    public int BonusDefense { get; set; }
    public int BonusStrength { get; set; }
    public int BonusDexterity { get; set; }
    public int BonusIntelligence { get; set; }
    public int BonusMaxHP { get; set; }
    public int BonusMaxMana { get; set; }
    
    public EquippableItemTemplate()
    {
        IsEquippable = true;
    }
}
```

#### 3.3 ConsumableItemTemplate

```csharp
namespace WingedBean.Plugins.DungeonGame.Items;

/// <summary>
/// Template for consumable items (potions, scrolls, food).
/// </summary>
public class ConsumableItemTemplate : DungeonItemTemplate
{
    public ConsumableEffect Effect { get; set; }
    public int EffectValue { get; set; }
    public int Duration { get; set; }
    public bool ConsumedOnUse { get; set; } = true;
    
    public ConsumableItemTemplate()
    {
        Category = ItemCategory.Consumable;
    }
}

public enum ConsumableEffect
{
    RestoreHP, RestoreMana, RestoreBoth, Buff, Debuff, Teleport
}
```

---

### 4. Inventory System

```csharp
namespace WingedBean.Plugins.DungeonGame.Systems;

using Microsoft.Extensions.Logging;
using WingedBean.Plugins.ArchECS.Internal;
using WingedBean.Plugins.DungeonGame.Components;
using WingedBean.Plugins.DungeonGame.Items;
using InventoryPoki.Items;
using InventoryPoki.Inventories;

/// <summary>
/// System that manages inventory operations using InventoryPoki.
/// </summary>
public class InventorySystem : IECSSystem
{
    private readonly ILogger<InventorySystem>? _logger;
    
    public InventorySystem(ILogger<InventorySystem>? logger = null)
    {
        _logger = logger;
    }
    
    public void Execute(IWorld world, float deltaTime)
    {
        // Process inventory actions from turn-based system
        ProcessInventoryActions(world);
        
        // Update inventory weights
        UpdateInventoryWeights(world);
    }
    
    /// <summary>
    /// Add item entity to inventory.
    /// </summary>
    public bool AddItem(IWorld world, EntityHandle ownerEntity, EntityHandle itemEntity)
    {
        if (!world.HasComponent<InventoryComponent>(ownerEntity))
            return false;
        
        if (!world.HasComponent<ItemEntityComponent>(itemEntity))
            return false;
        
        var inventoryComp = world.GetComponent<InventoryComponent>(ownerEntity);
        var itemComp = world.GetComponent<ItemEntityComponent>(itemEntity);
        
        // Use InventoryPoki's transaction system
        var transaction = inventoryComp.Inventory.AddItem(itemComp.Item);
        
        if (transaction.Status == TransactionStatus.Complete)
        {
            // Remove item from world (remove Position component)
            if (world.HasComponent<Position>(itemEntity))
            {
                world.RemoveComponent<Position>(itemEntity);
            }
            
            // Update weight
            UpdateWeight(world, ownerEntity);
            
            _logger?.LogInformation("Added item to inventory: {Item}", 
                itemComp.Item.Template.DisplayName);
            
            return true;
        }
        
        _logger?.LogDebug("Failed to add item: {Status}", transaction.Status);
        return false;
    }
    
    /// <summary>
    /// Remove item from inventory and drop to ground.
    /// </summary>
    public bool DropItem(IWorld world, EntityHandle ownerEntity, EntityHandle itemEntity)
    {
        if (!world.HasComponent<InventoryComponent>(ownerEntity))
            return false;
        
        if (!world.HasComponent<Position>(ownerEntity))
            return false;
        
        var pos = world.GetComponent<Position>(ownerEntity);
        var itemComp = world.GetComponent<ItemEntityComponent>(itemEntity);
        
        // Place item on ground
        world.AddComponent(itemEntity, new Position(pos.X, pos.Y, pos.Floor));
        
        // Add renderable
        if (!world.HasComponent<Renderable>(itemEntity))
        {
            world.AddComponent(itemEntity, CreateItemRenderable(itemComp.Item));
        }
        
        // Update weight
        UpdateWeight(world, ownerEntity);
        
        _logger?.LogInformation("Dropped item: {Item}", 
            itemComp.Item.Template.DisplayName);
        
        return true;
    }
    
    /// <summary>
    /// Use consumable item.
    /// </summary>
    public bool UseItem(IWorld world, EntityHandle ownerEntity, EntityHandle itemEntity)
    {
        if (!world.HasComponent<ItemEntityComponent>(itemEntity))
            return false;
        
        var itemComp = world.GetComponent<ItemEntityComponent>(itemEntity);
        
        if (itemComp.Item.Template is not ConsumableItemTemplate consumable)
            return false;
        
        // Apply effect
        ApplyConsumableEffect(world, ownerEntity, consumable);
        
        // Handle consumption
        if (consumable.ConsumedOnUse)
        {
            if (itemComp.Item is StackableItem stackable && stackable.Quantity > 1)
            {
                // Reduce stack
                stackable.Quantity--;
            }
            else
            {
                // Destroy item
                world.Destroy(itemEntity);
            }
        }
        
        _logger?.LogInformation("Used item: {Item}", 
            itemComp.Item.Template.DisplayName);
        
        return true;
    }
    
    /// <summary>
    /// Equip item to equipment slot.
    /// </summary>
    public bool EquipItem(IWorld world, EntityHandle ownerEntity, EntityHandle itemEntity)
    {
        if (!world.HasComponent<EquipmentComponent>(ownerEntity))
            return false;
        
        var itemComp = world.GetComponent<ItemEntityComponent>(itemEntity);
        
        if (itemComp.Item.Template is not DungeonItemTemplate template || !template.IsEquippable)
            return false;
        
        if (!template.EquipmentSlot.HasValue)
            return false;
        
        ref var equipment = ref world.GetComponent<EquipmentComponent>(ownerEntity);
        var slot = template.EquipmentSlot.Value;
        
        // Unequip existing item
        var existingItem = GetEquippedItem(equipment, slot);
        if (existingItem.HasValue)
        {
            UnequipItem(world, ownerEntity, existingItem.Value);
        }
        
        // Equip new item
        SetEquippedItem(ref equipment, slot, itemEntity);
        
        ref var itemCompMut = ref world.GetComponent<ItemEntityComponent>(itemEntity);
        itemCompMut.IsEquipped = true;
        itemCompMut.EquipSlot = slot;
        
        // Apply stat bonuses
        if (template is EquippableItemTemplate equippable)
        {
            ApplyEquipmentStats(world, ownerEntity, equippable, apply: true);
        }
        
        _logger?.LogInformation("Equipped {Item} to {Slot}", 
            template.DisplayName, slot);
        
        return true;
    }
    
    private void ProcessInventoryActions(IWorld world)
    {
        var query = world.CreateQuery<TurnAction>();
        
        foreach (var entity in query)
        {
            var action = world.GetComponent<TurnAction>(entity);
            
            switch (action.Type)
            {
                case ActionType.PickupItem:
                    if (action.TargetEntity.HasValue)
                    {
                        AddItem(world, entity, action.TargetEntity.Value);
                    }
                    break;
                    
                case ActionType.DropItem:
                    if (action.TargetEntity.HasValue)
                    {
                        DropItem(world, entity, action.TargetEntity.Value);
                    }
                    break;
                    
                case ActionType.UseItem:
                    if (action.TargetEntity.HasValue)
                    {
                        UseItem(world, entity, action.TargetEntity.Value);
                    }
                    break;
            }
        }
    }
    
    private void UpdateInventoryWeights(IWorld world)
    {
        var query = world.CreateQuery<InventoryComponent>();
        
        foreach (var entity in query)
        {
            UpdateWeight(world, entity);
        }
    }
    
    private void UpdateWeight(IWorld world, EntityHandle entity)
    {
        ref var inventoryComp = ref world.GetComponent<InventoryComponent>(entity);
        
        float totalWeight = 0;
        
        foreach (var item in inventoryComp.Inventory.Items)
        {
            totalWeight += item.Template.Weight;
            
            if (item is StackableItem stackable)
            {
                totalWeight *= stackable.Quantity;
            }
        }
        
        inventoryComp.CurrentWeight = totalWeight;
    }
    
    private void ApplyConsumableEffect(IWorld world, EntityHandle entity, ConsumableItemTemplate consumable)
    {
        if (!world.HasComponent<Stats>(entity))
            return;
        
        ref var stats = ref world.GetComponent<Stats>(entity);
        
        switch (consumable.Effect)
        {
            case ConsumableEffect.RestoreHP:
                stats.CurrentHP = Math.Min(stats.MaxHP, stats.CurrentHP + consumable.EffectValue);
                break;
                
            case ConsumableEffect.RestoreMana:
                stats.CurrentMana = Math.Min(stats.MaxMana, stats.CurrentMana + consumable.EffectValue);
                break;
                
            case ConsumableEffect.RestoreBoth:
                stats.CurrentHP = Math.Min(stats.MaxHP, stats.CurrentHP + consumable.EffectValue);
                stats.CurrentMana = Math.Min(stats.MaxMana, stats.CurrentMana + consumable.EffectValue);
                break;
        }
    }
    
    private void ApplyEquipmentStats(IWorld world, EntityHandle entity, EquippableItemTemplate item, bool apply)
    {
        if (!world.HasComponent<Stats>(entity))
            return;
        
        ref var stats = ref world.GetComponent<Stats>(entity);
        
        int multiplier = apply ? 1 : -1;
        
        stats.Strength += item.BonusStrength * multiplier;
        stats.Dexterity += item.BonusDexterity * multiplier;
        stats.Intelligence += item.BonusIntelligence * multiplier;
        stats.Defense += item.BonusDefense * multiplier;
        stats.MaxHP += item.BonusMaxHP * multiplier;
        stats.MaxMana += item.BonusMaxMana * multiplier;
        
        stats.CurrentHP = Math.Min(stats.CurrentHP, stats.MaxHP);
        stats.CurrentMana = Math.Min(stats.CurrentMana, stats.MaxMana);
    }
    
    private Renderable CreateItemRenderable(IItem item)
    {
        var template = item.Template as DungeonItemTemplate;
        
        char symbol = template?.Category switch
        {
            ItemCategory.Weapon => ')',
            ItemCategory.Armor => '[',
            ItemCategory.Consumable => '!',
            ItemCategory.Currency => '$',
            _ => '?'
        };
        
        ConsoleColor color = template?.Rarity switch
        {
            ItemRarity.Common => ConsoleColor.White,
            ItemRarity.Uncommon => ConsoleColor.Green,
            ItemRarity.Rare => ConsoleColor.Blue,
            ItemRarity.Epic => ConsoleColor.Magenta,
            ItemRarity.Legendary => ConsoleColor.Yellow,
            ItemRarity.Artifact => ConsoleColor.Red,
            _ => ConsoleColor.Gray
        };
        
        return new Renderable
        {
            Symbol = symbol,
            ForegroundColor = color,
            BackgroundColor = ConsoleColor.Black,
            RenderLayer = 1
        };
    }
    
    private EntityHandle? GetEquippedItem(EquipmentComponent equipment, EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Weapon => equipment.Weapon,
            EquipmentSlot.OffHand => equipment.OffHand,
            EquipmentSlot.Helmet => equipment.Helmet,
            EquipmentSlot.Armor => equipment.Armor,
            EquipmentSlot.Gloves => equipment.Gloves,
            EquipmentSlot.Boots => equipment.Boots,
            EquipmentSlot.Ring1 => equipment.Ring1,
            EquipmentSlot.Ring2 => equipment.Ring2,
            EquipmentSlot.Amulet => equipment.Amulet,
            _ => null
        };
    }
    
    private void SetEquippedItem(ref EquipmentComponent equipment, EquipmentSlot slot, EntityHandle? item)
    {
        switch (slot)
        {
            case EquipmentSlot.Weapon: equipment.Weapon = item; break;
            case EquipmentSlot.OffHand: equipment.OffHand = item; break;
            case EquipmentSlot.Helmet: equipment.Helmet = item; break;
            case EquipmentSlot.Armor: equipment.Armor = item; break;
            case EquipmentSlot.Gloves: equipment.Gloves = item; break;
            case EquipmentSlot.Boots: equipment.Boots = item; break;
            case EquipmentSlot.Ring1: equipment.Ring1 = item; break;
            case EquipmentSlot.Ring2: equipment.Ring2 = item; break;
            case EquipmentSlot.Amulet: equipment.Amulet = item; break;
        }
    }
    
    private bool UnequipItem(IWorld world, EntityHandle ownerEntity, EntityHandle itemEntity)
    {
        ref var equipment = ref world.GetComponent<EquipmentComponent>(ownerEntity);
        var itemComp = world.GetComponent<ItemEntityComponent>(itemEntity);
        
        if (!itemComp.EquipSlot.HasValue)
            return false;
        
        SetEquippedItem(ref equipment, itemComp.EquipSlot.Value, null);
        
        ref var itemCompMut = ref world.GetComponent<ItemEntityComponent>(itemEntity);
        itemCompMut.IsEquipped = false;
        
        // Remove stat bonuses
        if (itemComp.Item.Template is EquippableItemTemplate equippable)
        {
            ApplyEquipmentStats(world, ownerEntity, equippable, apply: false);
        }
        
        return true;
    }
}
```

---

## Implementation Plan

### Phase 1: Add InventoryPoki Reference (1 hour)
- [ ] Add InventoryPoki plugin reference to DungeonGame project
- [ ] Update project dependencies
- [ ] Verify plugin loads correctly

### Phase 2: ECS Components (1 hour)
- [ ] Create `InventoryComponent` (wraps IInventory)
- [ ] Create `EquipmentComponent`
- [ ] Create `ItemEntityComponent`
- [ ] Unit tests

### Phase 3: Item Templates (1 hour)
- [ ] Create `DungeonItemTemplate`
- [ ] Create `EquippableItemTemplate`
- [ ] Create `ConsumableItemTemplate`
- [ ] Create item factory helpers

### Phase 4: Inventory System (2 hours)
- [ ] Create `InventorySystem`
- [ ] Implement add/drop/use/equip methods
- [ ] Integrate with turn-based system
- [ ] Integration tests

---

## Benefits of Using InventoryPoki

### Time Savings
- **Original estimate:** 8-10 hours
- **Revised estimate:** 4-6 hours
- **Savings:** 4 hours (40% reduction) ⚡

### Quality Improvements
- ✅ **Battle-tested** - InventoryPoki already has comprehensive tests
- ✅ **Transaction-based** - Robust add/remove/move operations
- ✅ **Extensible** - Constraints and behaviors system
- ✅ **Slot management** - Drag/drop/swap already implemented
- ✅ **Stacking logic** - Proven stackable item implementation

### Future Features (Free)
- Inventory constraints (weight, item type restrictions)
- Inventory behaviors (auto-sort, auto-stack)
- Drag-and-drop UI (when GUI is added)
- Item slot filtering
- Inventory events/callbacks

---

## Testing Strategy

### Unit Tests
- ECS component wrappers
- Item template creation
- Equipment stat bonuses
- Consumable effects

### Integration Tests
- Add/remove items using InventoryPoki transactions
- Equipment system with stat application
- Stacking logic for consumables
- Weight/capacity limits

### Reuse Existing Tests
- InventoryPoki's test suite validates core inventory logic
- Focus our tests on ECS integration layer

---

## Migration from Original RFC

### What Changed
1. **Inventory storage** - Use `IInventory` instead of custom `List<EntityHandle>`
2. **Item representation** - Use `IItem` instead of custom `Item` struct
3. **Transactions** - Use `InventoryTransaction` instead of direct manipulation
4. **Stacking** - Use `StackableItem` instead of custom logic

### What Stayed the Same
- Equipment slots (still ECS-specific)
- Consumable effects (still game-specific)
- Turn-based integration (still uses `TurnAction`)
- ECS entity management (still uses EntityHandle)

---

## Related RFCs

- **RFC-0007**: Arch ECS Integration ✅ Implemented
- **RFC-0046**: Turn-Based System ✅ Implemented
- **RFC-0051**: HUD System ⏭️ Proposed (will display inventory)

---

## References

- InventoryPoki: `/plate-projects/inventory-poki`
- InventoryPoki Tests: `/plate-projects/inventory-poki/dotnet/tests`
- Plate.Plugins.InventoryPoki: Plugin integration example

---

**Decision:** Proposed - Awaiting Review  
**Next Steps:**
1. Review revised approach with team
2. Add InventoryPoki plugin reference
3. Implement ECS wrapper components
4. Create item templates
5. Implement InventorySystem

**Estimated Time:** 4-6 hours (40% faster than original!) 🚀
