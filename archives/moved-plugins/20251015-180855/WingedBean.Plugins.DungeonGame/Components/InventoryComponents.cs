using InventoryPoki.Inventories;
using WingedBean.Plugins.ArchECS.Internal;

namespace WingedBean.Plugins.DungeonGame.Components;

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

    /// <summary>Get all equipped items</summary>
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

    /// <summary>Get equipped item in specific slot</summary>
    public EntityHandle? GetSlot(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Weapon => Weapon,
            EquipmentSlot.OffHand => OffHand,
            EquipmentSlot.Helmet => Helmet,
            EquipmentSlot.Armor => Armor,
            EquipmentSlot.Gloves => Gloves,
            EquipmentSlot.Boots => Boots,
            EquipmentSlot.Ring1 => Ring1,
            EquipmentSlot.Ring2 => Ring2,
            EquipmentSlot.Amulet => Amulet,
            _ => null
        };
    }

    /// <summary>Set equipped item in specific slot</summary>
    public void SetSlot(EquipmentSlot slot, EntityHandle? item)
    {
        switch (slot)
        {
            case EquipmentSlot.Weapon: Weapon = item; break;
            case EquipmentSlot.OffHand: OffHand = item; break;
            case EquipmentSlot.Helmet: Helmet = item; break;
            case EquipmentSlot.Armor: Armor = item; break;
            case EquipmentSlot.Gloves: Gloves = item; break;
            case EquipmentSlot.Boots: Boots = item; break;
            case EquipmentSlot.Ring1: Ring1 = item; break;
            case EquipmentSlot.Ring2: Ring2 = item; break;
            case EquipmentSlot.Amulet: Amulet = item; break;
        }
    }
}

/// <summary>
/// Equipment slot types
/// </summary>
public enum EquipmentSlot
{
    Weapon,
    OffHand,
    Helmet,
    Armor,
    Gloves,
    Boots,
    Ring1,
    Ring2,
    Amulet
}

/// <summary>
/// Links an ECS entity to an InventoryPoki IItem.
/// Attached to item entities in the game world.
/// </summary>
public struct ItemEntityComponent
{
    /// <summary>Underlying InventoryPoki item</summary>
    public InventoryPoki.Items.IItem Item { get; set; }

    /// <summary>Is this item currently equipped?</summary>
    public bool IsEquipped { get; set; }

    /// <summary>Equipment slot (if equippable)</summary>
    public EquipmentSlot? EquipSlot { get; set; }
}
