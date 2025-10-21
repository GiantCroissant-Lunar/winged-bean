using Microsoft.Extensions.Logging;
using WingedBean.Plugins.ArchECS.Internal;
using WingedBean.Plugins.DungeonGame.Components;
using WingedBean.Plugins.DungeonGame.Items;
using InventoryPoki.Items;
using InventoryPoki.Inventories;

namespace WingedBean.Plugins.DungeonGame.Systems;

/// <summary>
/// System that manages inventory operations using InventoryPoki.
/// Handles add, drop, use, equip, and unequip operations.
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
        {
            _logger?.LogDebug("Entity {Owner} has no inventory", ownerEntity);
            return false;
        }

        if (!world.HasComponent<ItemEntityComponent>(itemEntity))
        {
            _logger?.LogDebug("Entity {Item} is not an item", itemEntity);
            return false;
        }

        var inventoryComp = world.GetComponent<InventoryComponent>(ownerEntity);
        var itemComp = world.GetComponent<ItemEntityComponent>(itemEntity);

        // Check weight capacity
        float itemWeight = itemComp.Item.Template.Weight;
        if (itemComp.Item is StackableItem stackable)
        {
            itemWeight *= stackable.Quantity;
        }

        if (inventoryComp.MaxWeight > 0 &&
            inventoryComp.CurrentWeight + itemWeight > inventoryComp.MaxWeight)
        {
            _logger?.LogDebug("Inventory full (weight): {Current}/{Max}",
                inventoryComp.CurrentWeight, inventoryComp.MaxWeight);
            return false;
        }

        // Use InventoryPoki's transaction system
        var transaction = inventoryComp.Inventory.AddItem(itemComp.Item);

        if (transaction.Status == TransactionStatus.Complete)
        {
            // Remove item from world (remove Position component)
            if (world.HasComponent<Position>(itemEntity))
            {
                world.RemoveComponent<Position>(itemEntity);
            }

            // Remove renderable (item is now in inventory, not visible)
            if (world.HasComponent<Renderable>(itemEntity))
            {
                world.RemoveComponent<Renderable>(itemEntity);
            }

            // Update weight
            UpdateWeight(world, ownerEntity);

            _logger?.LogInformation("Added {Item} to inventory",
                itemComp.Item.Template.DisplayName);

            // Add message to log
            var messageLog = world.GetSingleton<MessageLog>();
            messageLog?.AddMessage($"Picked up {itemComp.Item.Template.DisplayName}", MessageType.Pickup);

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

        if (!world.HasComponent<ItemEntityComponent>(itemEntity))
            return false;

        var pos = world.GetComponent<Position>(ownerEntity);
        var itemComp = world.GetComponent<ItemEntityComponent>(itemEntity);

        // If item is equipped, unequip first
        if (itemComp.IsEquipped && world.HasComponent<EquipmentComponent>(ownerEntity))
        {
            UnequipItem(world, ownerEntity, itemEntity);
        }

        // Place item on ground
        world.AddComponent(itemEntity, new Position(pos.X, pos.Y, pos.Floor));

        // Add renderable
        if (!world.HasComponent<Renderable>(itemEntity))
        {
            world.AddComponent(itemEntity, CreateItemRenderable(itemComp.Item));
        }

        // Update weight
        UpdateWeight(world, ownerEntity);

        _logger?.LogInformation("Dropped {Item} at ({X}, {Y})",
            itemComp.Item.Template.DisplayName, pos.X, pos.Y);

        // Add message to log
        var messageLog = world.GetSingleton<MessageLog>();
        messageLog?.AddMessage($"Dropped {itemComp.Item.Template.DisplayName}", MessageType.Drop);

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
        {
            _logger?.LogDebug("Item {Item} is not consumable",
                itemComp.Item.Template.DisplayName);
            return false;
        }

        // Apply effect
        ApplyConsumableEffect(world, ownerEntity, consumable);

        // Handle consumption
        if (consumable.ConsumedOnUse)
        {
            if (itemComp.Item is StackableItem stackable && stackable.Quantity > 1)
            {
                // Reduce stack
                stackable.Quantity--;
                _logger?.LogDebug("Reduced stack to {Quantity}", stackable.Quantity);
            }
            else
            {
                // Remove from inventory
                if (world.HasComponent<InventoryComponent>(ownerEntity))
                {
                    var inventoryComp = world.GetComponent<InventoryComponent>(ownerEntity);
                    // Note: InventoryPoki will handle removal via transaction
                }

                // Destroy item entity
                world.Destroy(itemEntity);
                _logger?.LogDebug("Destroyed consumed item");
            }
        }

        _logger?.LogInformation("Used {Item}", itemComp.Item.Template.DisplayName);

        // Add message to log
        var messageLog = world.GetSingleton<MessageLog>();
        messageLog?.AddMessage($"Used {itemComp.Item.Template.DisplayName}", MessageType.Use);

        return true;
    }

    /// <summary>
    /// Equip item to equipment slot.
    /// </summary>
    public bool EquipItem(IWorld world, EntityHandle ownerEntity, EntityHandle itemEntity)
    {
        if (!world.HasComponent<EquipmentComponent>(ownerEntity))
        {
            _logger?.LogDebug("Entity {Owner} has no equipment component", ownerEntity);
            return false;
        }

        if (!world.HasComponent<ItemEntityComponent>(itemEntity))
            return false;

        var itemComp = world.GetComponent<ItemEntityComponent>(itemEntity);

        if (itemComp.Item.Template is not DungeonItemTemplate template || !template.IsEquippable)
        {
            _logger?.LogDebug("Item {Item} is not equippable",
                itemComp.Item.Template.DisplayName);
            return false;
        }

        if (!template.EquipmentSlot.HasValue)
        {
            _logger?.LogWarning("Equippable item {Item} has no equipment slot defined",
                template.DisplayName);
            return false;
        }

        ref var equipment = ref world.GetComponent<EquipmentComponent>(ownerEntity);
        var slot = template.EquipmentSlot.Value;

        // Unequip existing item in slot
        var existingItem = equipment.GetSlot(slot);
        if (existingItem.HasValue)
        {
            UnequipItem(world, ownerEntity, existingItem.Value);
        }

        // Equip new item
        equipment.SetSlot(slot, itemEntity);

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

        // Add message to log
        var messageLog = world.GetSingleton<MessageLog>();
        messageLog?.AddMessage($"Equipped {template.DisplayName}", MessageType.Equip);

        return true;
    }

    /// <summary>
    /// Unequip item from equipment slot.
    /// </summary>
    public bool UnequipItem(IWorld world, EntityHandle ownerEntity, EntityHandle itemEntity)
    {
        if (!world.HasComponent<EquipmentComponent>(ownerEntity))
            return false;

        if (!world.HasComponent<ItemEntityComponent>(itemEntity))
            return false;

        ref var equipment = ref world.GetComponent<EquipmentComponent>(ownerEntity);
        var itemComp = world.GetComponent<ItemEntityComponent>(itemEntity);

        if (!itemComp.EquipSlot.HasValue)
        {
            _logger?.LogDebug("Item not equipped in any slot");
            return false;
        }

        var slot = itemComp.EquipSlot.Value;

        // Remove from equipment slot
        equipment.SetSlot(slot, null);

        ref var itemCompMut = ref world.GetComponent<ItemEntityComponent>(itemEntity);
        itemCompMut.IsEquipped = false;
        itemCompMut.EquipSlot = null;

        // Remove stat bonuses
        if (itemComp.Item.Template is EquippableItemTemplate equippable)
        {
            ApplyEquipmentStats(world, ownerEntity, equippable, apply: false);
        }

        _logger?.LogInformation("Unequipped {Item} from {Slot}",
            itemComp.Item.Template.DisplayName, slot);

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
        if (!world.HasComponent<InventoryComponent>(entity))
            return;

        ref var inventoryComp = ref world.GetComponent<InventoryComponent>(entity);

        float totalWeight = 0;

        foreach (var item in inventoryComp.Inventory.Items)
        {
            float weight = item.Template.Weight;

            if (item is StackableItem stackable)
            {
                weight *= stackable.Quantity;
            }

            totalWeight += weight;
        }

        inventoryComp.CurrentWeight = totalWeight;
    }

    private void ApplyConsumableEffect(IWorld world, EntityHandle entity, ConsumableItemTemplate consumable)
    {
        if (!world.HasComponent<Stats>(entity))
        {
            _logger?.LogDebug("Entity {Entity} has no stats to apply effect to", entity);
            return;
        }

        ref var stats = ref world.GetComponent<Stats>(entity);

        switch (consumable.Effect)
        {
            case ConsumableEffect.RestoreHP:
                stats.CurrentHP = Math.Min(stats.MaxHP, stats.CurrentHP + consumable.EffectValue);
                _logger?.LogInformation("Restored {HP} HP (now {Current}/{Max})",
                    consumable.EffectValue, stats.CurrentHP, stats.MaxHP);

                var messageLog = world.GetSingleton<MessageLog>();
                messageLog?.AddMessage($"Restored {consumable.EffectValue} HP", MessageType.Use);
                break;

            case ConsumableEffect.RestoreMana:
                stats.CurrentMana = Math.Min(stats.MaxMana, stats.CurrentMana + consumable.EffectValue);
                _logger?.LogInformation("Restored {Mana} Mana (now {Current}/{Max})",
                    consumable.EffectValue, stats.CurrentMana, stats.MaxMana);

                var messageLog = world.GetSingleton<MessageLog>();
                messageLog?.AddMessage($"Restored {consumable.EffectValue} Mana", MessageType.Use);
                break;

            case ConsumableEffect.RestoreBoth:
                stats.CurrentHP = Math.Min(stats.MaxHP, stats.CurrentHP + consumable.EffectValue);
                stats.CurrentMana = Math.Min(stats.MaxMana, stats.CurrentMana + consumable.EffectValue);
                _logger?.LogInformation("Restored {Value} HP and Mana", consumable.EffectValue);

                var messageLog = world.GetSingleton<MessageLog>();
                messageLog?.AddMessage($"Restored {consumable.EffectValue} HP and Mana", MessageType.Use);
                break;

                // Future: Implement Buff, Debuff, Teleport effects
        }
    }

    private void ApplyEquipmentStats(IWorld world, EntityHandle entity, EquippableItemTemplate item, bool apply)
    {
        if (!world.HasComponent<Stats>(entity))
        {
            _logger?.LogDebug("Entity {Entity} has no stats to modify", entity);
            return;
        }

        ref var stats = ref world.GetComponent<Stats>(entity);

        int multiplier = apply ? 1 : -1;

        stats.Strength += item.BonusStrength * multiplier;
        stats.Dexterity += item.BonusDexterity * multiplier;
        stats.Intelligence += item.BonusIntelligence * multiplier;
        stats.Defense += item.BonusDefense * multiplier;
        stats.MaxHP += item.BonusMaxHP * multiplier;
        stats.MaxMana += item.BonusMaxMana * multiplier;

        // Clamp current HP/Mana to new max
        stats.CurrentHP = Math.Min(stats.CurrentHP, stats.MaxHP);
        stats.CurrentMana = Math.Min(stats.CurrentMana, stats.MaxMana);

        if (apply)
        {
            _logger?.LogDebug("Applied equipment stats: STR+{Str} DEX+{Dex} INT+{Int} DEF+{Def} HP+{HP} MP+{MP}",
                item.BonusStrength, item.BonusDexterity, item.BonusIntelligence,
                item.BonusDefense, item.BonusMaxHP, item.BonusMaxMana);
        }
        else
        {
            _logger?.LogDebug("Removed equipment stats");
        }
    }

    private Renderable CreateItemRenderable(IItem item)
    {
        var template = item.Template as DungeonItemTemplate;

        char symbol = template?.Category switch
        {
            ItemCategory.Weapon => ')',
            ItemCategory.Armor => '[',
            ItemCategory.Consumable => '!',
            ItemCategory.Accessory => '=',
            ItemCategory.Currency => '$',
            ItemCategory.QuestItem => '*',
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
            RenderLayer = 1 // Items layer
        };
    }
}
