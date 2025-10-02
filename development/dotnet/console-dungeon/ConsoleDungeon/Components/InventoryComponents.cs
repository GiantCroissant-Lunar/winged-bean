using System.Collections.Generic;
using WingedBean.Contracts.ECS;

namespace ConsoleDungeon.Components;

/// <summary>
/// Inventory component (stores entity handles to item entities).
/// </summary>
public struct Inventory
{
    public List<EntityHandle> Items;
    public int MaxSlots;

    public Inventory(int maxSlots)
    {
        Items = new List<EntityHandle>();
        MaxSlots = maxSlots;
    }
}
