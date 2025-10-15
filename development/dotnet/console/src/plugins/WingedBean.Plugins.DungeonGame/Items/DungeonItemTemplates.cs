using InventoryPoki.Items;
using WingedBean.Plugins.DungeonGame.Components;

namespace WingedBean.Plugins.DungeonGame.Items;

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

/// <summary>
/// Item category types
/// </summary>
public enum ItemCategory
{
    Weapon,
    Armor,
    Consumable,
    Accessory,
    Currency,
    QuestItem,
    Miscellaneous
}

/// <summary>
/// Item rarity/quality levels
/// </summary>
public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
    Artifact
}

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

/// <summary>
/// Consumable effect types
/// </summary>
public enum ConsumableEffect
{
    RestoreHP,
    RestoreMana,
    RestoreBoth,
    Buff,
    Debuff,
    Teleport
}
