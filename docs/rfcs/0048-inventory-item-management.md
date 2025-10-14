---
id: RFC-0048
title: Inventory and Item Management System
status: Proposed
category: game-engine, ecs, gameplay
created: 2025-10-14
updated: 2025-10-14
priority: P1
effort: 8-10 hours
---

# RFC-0048: Inventory and Item Management System

**Status:** Proposed  
**Date:** 2025-10-14  
**Author:** Development Team  
**Category:** game-engine, ecs, gameplay  
**Priority:** HIGH (P1)  
**Estimated Effort:** 8-10 hours  
**Depends On:** 
- RFC-0007: Arch ECS Integration (Implemented)
- RFC-0043: Tile-Based Map System (In Progress)
- RFC-0046: Turn-Based Game Loop (Proposed)

---

## Summary

Implement a comprehensive **inventory and item management system** for the dungeon crawler. Provides item storage, equipment slots, pickup/drop mechanics, item usage, stacking, and weight limits. Items are ECS entities with components defining their properties.

---

## Motivation

### Current Problem

Minimal item support exists:
- No inventory system
- No equipment slots
- No pickup/drop mechanics
- No item usage
- No weight/capacity limits

### Why Inventory System?

**Gameplay Benefits:**
- 🎒 Resource management
- ⚔️ Character progression through equipment
- 🧪 Consumables for survival
- 💰 Economy foundation
- 🗝️ Quest items

**Technical Benefits:**
- ✅ ECS-native (items are entities)
- ✅ Data-driven (JSON definitions)
- ✅ Flexible and extensible

### Success Criteria

✅ Player has inventory with capacity limit  
✅ Items can be picked up/dropped  
✅ Equipment slots (weapon, armor, etc.)  
✅ Consumables can be used  
✅ Stackable items stack correctly  
✅ Item properties affect stats  

---

## Proposal

### 1. Core Components

```csharp
// Inventory.cs
public class Inventory
{
    public List<EntityHandle> Items { get; set; } = new();
    public int Capacity { get; set; }
    public float MaxWeight { get; set; }
    public float CurrentWeight { get; set; }
    public int Gold { get; set; }
}

// Equipment.cs
public class Equipment
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
}

// Item.cs (Enhanced)
public struct Item
{
    public ItemType Type { get; set; }
    public string SubType { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int Value { get; set; }
    public float Weight { get; set; }
    public bool Stackable { get; set; }
    public int StackCount { get; set; }
    public int MaxStackSize { get; set; }
    public ItemQuality Quality { get; set; }
    public bool IsEquipped { get; set; }
    public EquipmentSlot? EquipSlot { get; set; }
}

public enum ItemType
{
    Consumable, Weapon, Armor, Accessory, 
    Helmet, Gloves, Boots, Currency, QuestItem
}

public enum ItemQuality
{
    Common, Uncommon, Rare, Epic, Legendary, Artifact
}

// ItemStats.cs
public struct ItemStats
{
    public int BonusDamage { get; set; }
    public int BonusDefense { get; set; }
    public int BonusStrength { get; set; }
    public int BonusDexterity { get; set; }
    public int BonusIntelligence { get; set; }
    public int BonusMaxHP { get; set; }
    public int BonusMaxMana { get; set; }
}

// Consumable.cs
public struct Consumable
{
    public ConsumableEffect Effect { get; set; }
    public int EffectValue { get; set; }
    public int Duration { get; set; }
    public bool UsableInCombat { get; set; }
    public bool ConsumedOnUse { get; set; }
}

public enum ConsumableEffect
{
    RestoreHP, RestoreMana, RestoreBoth, 
    Buff, Debuff, Teleport
}
```

---

### 2. Inventory System

```csharp
public class InventorySystem : IECSSystem
{
    public bool AddItem(IWorld world, EntityHandle entity, EntityHandle itemEntity)
    {
        ref var inventory = ref world.GetComponent<Inventory>(entity);
        var item = world.GetComponent<Item>(itemEntity);
        
        // Check capacity and weight
        if (!inventory.HasSpace() || !inventory.CanCarry(item.Weight))
            return false;
        
        // Handle stacking
        if (item.Stackable)
        {
            var existingStack = FindStackableItem(world, inventory, item);
            if (existingStack.HasValue)
            {
                ref var existing = ref world.GetComponent<Item>(existingStack.Value);
                existing.StackCount += item.StackCount;
                world.Destroy(itemEntity);
                return true;
            }
        }
        
        // Add to inventory
        inventory.Items.Add(itemEntity);
        inventory.CurrentWeight += item.Weight * item.StackCount;
        
        // Remove from ground
        world.RemoveComponent<Position>(itemEntity);
        
        return true;
    }
    
    public bool DropItem(IWorld world, EntityHandle entity, EntityHandle itemEntity)
    {
        ref var inventory = ref world.GetComponent<Inventory>(entity);
        var pos = world.GetComponent<Position>(entity);
        var item = world.GetComponent<Item>(itemEntity);
        
        inventory.Items.Remove(itemEntity);
        inventory.CurrentWeight -= item.Weight * item.StackCount;
        
        // Place on ground
        world.AddComponent(itemEntity, new Position(pos.X, pos.Y, pos.Floor));
        
        return true;
    }
    
    public bool UseItem(IWorld world, EntityHandle entity, EntityHandle itemEntity)
    {
        if (!world.HasComponent<Consumable>(itemEntity))
            return false;
        
        var consumable = world.GetComponent<Consumable>(itemEntity);
        ref var item = ref world.GetComponent<Item>(itemEntity);
        
        ApplyConsumableEffect(world, entity, consumable);
        
        if (consumable.ConsumedOnUse)
        {
            if (item.StackCount > 1)
                item.StackCount--;
            else
            {
                world.GetComponent<Inventory>(entity).Items.Remove(itemEntity);
                world.Destroy(itemEntity);
            }
        }
        
        return true;
    }
    
    public bool EquipItem(IWorld world, EntityHandle entity, EntityHandle itemEntity)
    {
        ref var equipment = ref world.GetComponent<Equipment>(entity);
        var item = world.GetComponent<Item>(itemEntity);
        
        if (!item.EquipSlot.HasValue)
            return false;
        
        // Unequip existing
        var existing = equipment.GetSlot(item.EquipSlot.Value);
        if (existing.HasValue)
            UnequipItem(world, entity, existing.Value);
        
        // Equip new
        equipment.SetSlot(item.EquipSlot.Value, itemEntity);
        world.GetComponent<Item>(itemEntity).IsEquipped = true;
        
        // Apply stats
        if (world.HasComponent<ItemStats>(itemEntity))
            ApplyItemStats(world, entity, itemEntity, apply: true);
        
        return true;
    }
    
    private void ApplyConsumableEffect(IWorld world, EntityHandle entity, Consumable consumable)
    {
        ref var stats = ref world.GetComponent<Stats>(entity);
        
        switch (consumable.Effect)
        {
            case ConsumableEffect.RestoreHP:
                stats.CurrentHP = Math.Min(stats.MaxHP, stats.CurrentHP + consumable.EffectValue);
                break;
            case ConsumableEffect.RestoreMana:
                stats.CurrentMana = Math.Min(stats.MaxMana, stats.CurrentMana + consumable.EffectValue);
                break;
        }
    }
    
    private void ApplyItemStats(IWorld world, EntityHandle entity, EntityHandle itemEntity, bool apply)
    {
        ref var stats = ref world.GetComponent<Stats>(entity);
        var itemStats = world.GetComponent<ItemStats>(itemEntity);
        
        int mult = apply ? 1 : -1;
        
        stats.Strength += itemStats.BonusStrength * mult;
        stats.Dexterity += itemStats.BonusDexterity * mult;
        stats.Intelligence += itemStats.BonusIntelligence * mult;
        stats.Defense += itemStats.BonusDefense * mult;
        stats.MaxHP += itemStats.BonusMaxHP * mult;
        stats.MaxMana += itemStats.BonusMaxMana * mult;
    }
}
```

---

### 3. Item Factory

```csharp
public static class ItemFactory
{
    public static EntityHandle CreateHealthPotion(IWorld world, int x, int y)
    {
        var entity = world.Create();
        
        world.AddComponent(entity, new Item
        {
            Type = ItemType.Consumable,
            SubType = "HealthPotion",
            Name = "Health Potion",
            Description = "Restores 50 HP",
            Value = 25,
            Weight = 0.5f,
            Stackable = true,
            StackCount = 1,
            MaxStackSize = 10,
            Quality = ItemQuality.Common
        });
        
        world.AddComponent(entity, new Consumable
        {
            Effect = ConsumableEffect.RestoreHP,
            EffectValue = 50,
            UsableInCombat = true,
            ConsumedOnUse = true
        });
        
        world.AddComponent(entity, new Position(x, y));
        world.AddComponent(entity, new Renderable { Symbol = '!', ForegroundColor = ConsoleColor.Red });
        
        return entity;
    }
    
    public static EntityHandle CreateSword(IWorld world, int x, int y, ItemQuality quality = ItemQuality.Common)
    {
        var entity = world.Create();
        
        int damage = quality switch
        {
            ItemQuality.Common => 5,
            ItemQuality.Rare => 12,
            ItemQuality.Legendary => 25,
            _ => 5
        };
        
        world.AddComponent(entity, new Item
        {
            Type = ItemType.Weapon,
            Name = $"{quality} Sword",
            Value = 50 * (int)quality,
            Weight = 3.0f,
            Quality = quality,
            EquipSlot = EquipmentSlot.Weapon
        });
        
        world.AddComponent(entity, new ItemStats { BonusDamage = damage });
        world.AddComponent(entity, new Position(x, y));
        world.AddComponent(entity, new Renderable { Symbol = ')', ForegroundColor = ConsoleColor.Cyan });
        
        return entity;
    }
}
```

---

## Implementation Plan

### Phase 1: Core Components (2 hours)
- [ ] Create Inventory, Equipment, enhanced Item components
- [ ] Create ItemStats, Consumable components
- [ ] Unit tests

### Phase 2: Inventory System (3 hours)
- [ ] Implement InventorySystem
- [ ] Add/remove/use/equip methods
- [ ] Weight and capacity checks
- [ ] Stacking logic

### Phase 3: Item Factory (2 hours)
- [ ] Create ItemFactory
- [ ] Implement common items (potions, weapons, armor)
- [ ] Data-driven item loading (JSON)

### Phase 4: Integration (2 hours)
- [ ] Integrate with turn-based system
- [ ] Add pickup/drop actions
- [ ] UI for inventory display
- [ ] Testing

---

## Testing Strategy

### Unit Tests
- Add/remove items
- Stacking logic
- Weight/capacity limits
- Equipment stat bonuses
- Consumable effects

### Integration Tests
- Pickup items from ground
- Drop items to ground
- Equip/unequip items
- Use consumables in combat
- Full inventory scenarios

---

## Related RFCs

- **RFC-0007**: Arch ECS Integration ✅ Implemented
- **RFC-0043**: Tile-Based Map System ⏭️ In Progress
- **RFC-0046**: Turn-Based Game Loop ⏭️ Proposed
- **RFC-0047**: Enhanced Movement ⏭️ Proposed

---

## References

- Roguelike Inventory: [RogueBasin](http://www.roguebasin.com/index.php?title=Inventory)
- ECS Item Systems: [Game Programming Patterns](https://gameprogrammingpatterns.com/)

---

**Decision:** Proposed - Awaiting Review  
**Next Steps:**
1. Review with team
2. Prototype inventory UI
3. Create implementation branch
