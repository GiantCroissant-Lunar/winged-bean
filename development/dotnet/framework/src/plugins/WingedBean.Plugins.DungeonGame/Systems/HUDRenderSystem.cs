using System.Text;
using Microsoft.Extensions.Logging;
using WingedBean.Plugins.ArchECS.Internal;
using WingedBean.Plugins.DungeonGame.Components;
using WingedBean.Plugins.DungeonGame.Items;

namespace WingedBean.Plugins.DungeonGame.Systems;

/// <summary>
/// System that renders HUD overlay on top of game view.
/// Displays health, mana, equipment, messages, and turn information.
/// </summary>
public class HUDRenderSystem : IECSSystem
{
    private readonly ILogger<HUDRenderSystem>? _logger;
    private readonly StringBuilder _hudBuffer = new();
    
    public string? LastRenderedHUD { get; private set; }
    
    public HUDRenderSystem(ILogger<HUDRenderSystem>? logger = null)
    {
        _logger = logger;
    }
    
    public void Execute(IWorld world, float deltaTime)
    {
        // Get HUD config (or create default)
        var hudConfig = world.GetSingleton<HUDConfig>() ?? new HUDConfig();
        
        // Find player entity
        var playerQuery = world.CreateQuery<Player, Stats>();
        var playerEntity = playerQuery.FirstOrDefault();
        
        if (playerEntity == default)
            return;
        
        // Render HUD based on layout
        _hudBuffer.Clear();
        
        switch (hudConfig.Layout)
        {
            case HUDLayout.Standard:
                RenderStandardHUD(world, hudConfig, playerEntity);
                break;
                
            case HUDLayout.Compact:
                RenderCompactHUD(world, hudConfig, playerEntity);
                break;
                
            case HUDLayout.Full:
                RenderFullHUD(world, hudConfig, playerEntity);
                break;
                
            case HUDLayout.Minimal:
                RenderMinimalHUD(world, hudConfig, playerEntity);
                break;
        }
        
        LastRenderedHUD = _hudBuffer.ToString();
    }
    
    private void RenderStandardHUD(IWorld world, HUDConfig config, EntityHandle playerEntity)
    {
        var stats = world.GetComponent<Stats>(playerEntity);
        
        // Top bar: Health, Mana, Turn
        _hudBuffer.AppendLine("╔════════════════════════════════════════════════════════════════════════════╗");
        
        if (config.ShowHealthBar || config.ShowManaBar)
        {
            _hudBuffer.Append("║ ");
            
            if (config.ShowHealthBar)
            {
                RenderHealthBar(stats, 20);
                _hudBuffer.Append("  ");
            }
            
            if (config.ShowManaBar)
            {
                RenderManaBar(stats, 20);
                _hudBuffer.Append("  ");
            }
            
            if (config.ShowTurnInfo)
            {
                var turnOrder = world.GetSingleton<TurnOrder>();
                if (turnOrder != null)
                {
                    _hudBuffer.Append($"Turn: {turnOrder.TurnNumber}");
                }
            }
            
            // Pad to end
            PadToWidth(78);
            _hudBuffer.AppendLine(" ║");
        }
        
        // Equipment line
        if (config.ShowEquipment && world.HasComponent<EquipmentComponent>(playerEntity))
        {
            _hudBuffer.Append("║ ");
            RenderEquipmentLine(world, playerEntity);
            PadToWidth(78);
            _hudBuffer.AppendLine(" ║");
        }
        
        // Inventory count
        if (config.ShowInventoryCount && world.HasComponent<InventoryComponent>(playerEntity))
        {
            var inventory = world.GetComponent<InventoryComponent>(playerEntity);
            _hudBuffer.Append($"║ Items: {inventory.Inventory.Items.Count()}");
            
            if (inventory.MaxWeight > 0)
            {
                _hudBuffer.Append($"  Weight: {inventory.CurrentWeight:F1}/{inventory.MaxWeight:F1}");
            }
            
            _hudBuffer.Append($"  Gold: {inventory.Gold}");
            
            PadToWidth(78);
            _hudBuffer.AppendLine(" ║");
        }
        
        _hudBuffer.AppendLine("╚════════════════════════════════════════════════════════════════════════════╝");
        
        // Message log at bottom
        if (config.ShowMessageLog)
        {
            RenderMessageLog(world, config.MessageLogHeight);
        }
    }
    
    private void RenderCompactHUD(IWorld world, HUDConfig config, EntityHandle playerEntity)
    {
        var stats = world.GetComponent<Stats>(playerEntity);
        
        _hudBuffer.Append("HP:");
        _hudBuffer.Append($"{stats.CurrentHP}/{stats.MaxHP}");
        _hudBuffer.Append(" MP:");
        _hudBuffer.Append($"{stats.CurrentMana}/{stats.MaxMana}");
        
        if (config.ShowTurnInfo)
        {
            var turnOrder = world.GetSingleton<TurnOrder>();
            if (turnOrder != null)
            {
                _hudBuffer.Append($" T:{turnOrder.TurnNumber}");
            }
        }
        
        _hudBuffer.AppendLine();
    }
    
    private void RenderFullHUD(IWorld world, HUDConfig config, EntityHandle playerEntity)
    {
        var stats = world.GetComponent<Stats>(playerEntity);
        
        _hudBuffer.AppendLine("╔════════════════════════════════════════════════════════════════════════════╗");
        _hudBuffer.AppendLine("║                              PLAYER STATUS                                 ║");
        _hudBuffer.AppendLine("╠════════════════════════════════════════════════════════════════════════════╣");
        
        // Health and Mana
        _hudBuffer.Append("║ ");
        RenderHealthBar(stats, 35);
        PadToWidth(78);
        _hudBuffer.AppendLine(" ║");
        
        _hudBuffer.Append("║ ");
        RenderManaBar(stats, 35);
        PadToWidth(78);
        _hudBuffer.AppendLine(" ║");
        
        _hudBuffer.AppendLine("╠════════════════════════════════════════════════════════════════════════════╣");
        
        // Stats
        _hudBuffer.AppendLine($"║ STR: {stats.Strength,-3} DEX: {stats.Dexterity,-3} INT: {stats.Intelligence,-3}  " +
                            $"DEF: {stats.Defense,-3} LVL: {stats.Level,-3}                      ║");
        
        _hudBuffer.AppendLine("╠════════════════════════════════════════════════════════════════════════════╣");
        
        // Equipment
        if (world.HasComponent<EquipmentComponent>(playerEntity))
        {
            RenderEquipmentFull(world, playerEntity);
        }
        
        // Inventory
        if (world.HasComponent<InventoryComponent>(playerEntity))
        {
            var inventory = world.GetComponent<InventoryComponent>(playerEntity);
            _hudBuffer.AppendLine($"║ Inventory: {inventory.Inventory.Items.Count()} items  " +
                                $"Weight: {inventory.CurrentWeight:F1}/{inventory.MaxWeight:F1}  " +
                                $"Gold: {inventory.Gold}                ║");
        }
        
        _hudBuffer.AppendLine("╚════════════════════════════════════════════════════════════════════════════╝");
        
        // Message log
        if (config.ShowMessageLog)
        {
            RenderMessageLog(world, config.MessageLogHeight);
        }
    }
    
    private void RenderMinimalHUD(IWorld world, HUDConfig config, EntityHandle playerEntity)
    {
        var stats = world.GetComponent<Stats>(playerEntity);
        
        _hudBuffer.Append($"HP:{stats.CurrentHP}/{stats.MaxHP} ");
        _hudBuffer.Append($"MP:{stats.CurrentMana}/{stats.MaxMana}");
        _hudBuffer.AppendLine();
    }
    
    private void RenderHealthBar(Stats stats, int width)
    {
        _hudBuffer.Append("HP:[");
        
        int filled = (int)((float)stats.CurrentHP / stats.MaxHP * width);
        filled = Math.Clamp(filled, 0, width);
        
        for (int i = 0; i < width; i++)
        {
            _hudBuffer.Append(i < filled ? '█' : '░');
        }
        
        _hudBuffer.Append($"] {stats.CurrentHP}/{stats.MaxHP}");
    }
    
    private void RenderManaBar(Stats stats, int width)
    {
        _hudBuffer.Append("MP:[");
        
        int filled = (int)((float)stats.CurrentMana / stats.MaxMana * width);
        filled = Math.Clamp(filled, 0, width);
        
        for (int i = 0; i < width; i++)
        {
            _hudBuffer.Append(i < filled ? '█' : '░');
        }
        
        _hudBuffer.Append($"] {stats.CurrentMana}/{stats.MaxMana}");
    }
    
    private void RenderEquipmentLine(IWorld world, EntityHandle playerEntity)
    {
        var equipment = world.GetComponent<EquipmentComponent>(playerEntity);
        
        _hudBuffer.Append("Equipped: ");
        
        var equipped = new List<string>();
        
        if (equipment.Weapon.HasValue && world.HasComponent<ItemEntityComponent>(equipment.Weapon.Value))
        {
            var item = world.GetComponent<ItemEntityComponent>(equipment.Weapon.Value);
            equipped.Add($"Wpn:{item.Item.Template.DisplayName}");
        }
        
        if (equipment.Armor.HasValue && world.HasComponent<ItemEntityComponent>(equipment.Armor.Value))
        {
            var item = world.GetComponent<ItemEntityComponent>(equipment.Armor.Value);
            equipped.Add($"Arm:{item.Item.Template.DisplayName}");
        }
        
        if (equipped.Count == 0)
        {
            _hudBuffer.Append("None");
        }
        else
        {
            _hudBuffer.Append(string.Join(", ", equipped));
        }
    }
    
    private void RenderEquipmentFull(IWorld world, EntityHandle playerEntity)
    {
        var equipment = world.GetComponent<EquipmentComponent>(playerEntity);
        
        _hudBuffer.AppendLine("║ EQUIPMENT:                                                                 ║");
        
        RenderEquipmentSlot(world, "Weapon", equipment.Weapon);
        RenderEquipmentSlot(world, "Off-Hand", equipment.OffHand);
        RenderEquipmentSlot(world, "Helmet", equipment.Helmet);
        RenderEquipmentSlot(world, "Armor", equipment.Armor);
        RenderEquipmentSlot(world, "Gloves", equipment.Gloves);
        RenderEquipmentSlot(world, "Boots", equipment.Boots);
    }
    
    private void RenderEquipmentSlot(IWorld world, string slotName, EntityHandle? itemEntity)
    {
        _hudBuffer.Append($"║   {slotName,-10}: ");
        
        if (itemEntity.HasValue && world.HasComponent<ItemEntityComponent>(itemEntity.Value))
        {
            var item = world.GetComponent<ItemEntityComponent>(itemEntity.Value);
            _hudBuffer.Append(item.Item.Template.DisplayName);
        }
        else
        {
            _hudBuffer.Append("(empty)");
        }
        
        PadToWidth(78);
        _hudBuffer.AppendLine(" ║");
    }
    
    private void RenderMessageLog(IWorld world, int height)
    {
        var messageLog = world.GetSingleton<MessageLog>();
        if (messageLog == null)
            return;
        
        _hudBuffer.AppendLine("╔════════════════════════════════════════════════════════════════════════════╗");
        _hudBuffer.AppendLine("║ MESSAGES                                                                   ║");
        _hudBuffer.AppendLine("╠════════════════════════════════════════════════════════════════════════════╣");
        
        var recentMessages = messageLog.GetRecentMessages(height).ToList();
        
        // Pad if not enough messages
        while (recentMessages.Count < height)
        {
            recentMessages.Insert(0, new LogMessage { Text = "", Type = MessageType.Info });
        }
        
        foreach (var message in recentMessages)
        {
            _hudBuffer.Append("║ ");
            
            // Add type indicator
            var indicator = message.Type switch
            {
                MessageType.Combat => "[!]",
                MessageType.Pickup => "[+]",
                MessageType.Drop => "[-]",
                MessageType.Use => "[*]",
                MessageType.Equip => "[E]",
                MessageType.Level => "[^]",
                MessageType.Warning => "[?]",
                MessageType.Error => "[X]",
                MessageType.System => "[S]",
                _ => "   "
            };
            
            _hudBuffer.Append(indicator);
            _hudBuffer.Append(' ');
            
            // Truncate message if too long
            var text = message.Text;
            if (text.Length > 70)
            {
                text = text.Substring(0, 67) + "...";
            }
            
            _hudBuffer.Append(text);
            
            PadToWidth(78);
            _hudBuffer.AppendLine(" ║");
        }
        
        _hudBuffer.AppendLine("╚════════════════════════════════════════════════════════════════════════════╝");
    }
    
    private void PadToWidth(int width)
    {
        int currentLength = GetCurrentLineLength();
        int padding = width - currentLength;
        
        if (padding > 0)
        {
            _hudBuffer.Append(new string(' ', padding));
        }
    }
    
    private int GetCurrentLineLength()
    {
        var str = _hudBuffer.ToString();
        int lastNewline = str.LastIndexOf('\n');
        
        if (lastNewline == -1)
            return str.Length;
        
        return str.Length - lastNewline - 1;
    }
}
