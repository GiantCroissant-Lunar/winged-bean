using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WingedBean.Contracts.Gameplay;

/// <summary>
/// Inventory operations for player/entities.
/// </summary>
public interface IInventoryService
{
    Task<IReadOnlyList<ItemStack>> GetInventoryAsync(string entityId, CancellationToken ct = default);
    Task<bool> AddItemAsync(string entityId, ItemStack item, CancellationToken ct = default);
    Task<bool> RemoveItemAsync(string entityId, string itemId, int amount = 1, CancellationToken ct = default);
}

/// <summary>Simple item stack model for contracts.</summary>
public sealed class ItemStack
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Amount { get; set; }
}
