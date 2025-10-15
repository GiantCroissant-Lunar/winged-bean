namespace WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// Configuration for HUD display.
/// Singleton component attached to world.
/// </summary>
public class HUDConfig
{
    public HUDLayout Layout { get; set; } = HUDLayout.Standard;
    public bool ShowHealthBar { get; set; } = true;
    public bool ShowManaBar { get; set; } = true;
    public bool ShowMessageLog { get; set; } = true;
    public int MessageLogHeight { get; set; } = 4;
    public bool ShowEquipment { get; set; } = true;
    public bool ShowInventoryCount { get; set; } = true;
    public bool ShowMinimap { get; set; } = false;
    public bool ShowStatusEffects { get; set; } = true;
    public bool ShowTurnInfo { get; set; } = true;
    public bool ShowDepthLevel { get; set; } = true;
}

/// <summary>
/// HUD layout styles
/// </summary>
public enum HUDLayout
{
    /// <summary>Standard layout: stats top, messages bottom</summary>
    Standard,
    
    /// <summary>Compact layout: minimal space usage</summary>
    Compact,
    
    /// <summary>Full layout: all information displayed</summary>
    Full,
    
    /// <summary>Minimal layout: only critical info</summary>
    Minimal
}

/// <summary>
/// Message log for displaying game events.
/// Singleton component attached to world.
/// </summary>
public class MessageLog
{
    private readonly List<LogMessage> _messages = new();
    private readonly int _maxMessages;
    
    public IReadOnlyList<LogMessage> Messages => _messages;
    
    public MessageLog(int maxMessages = 100)
    {
        _maxMessages = maxMessages;
    }
    
    /// <summary>
    /// Add a message to the log
    /// </summary>
    public void AddMessage(string text, MessageType type = MessageType.Info)
    {
        var message = new LogMessage
        {
            Text = text,
            Type = type,
            Timestamp = DateTime.UtcNow,
            Turn = 0 // Will be set by system
        };
        
        _messages.Add(message);
        
        // Trim old messages
        while (_messages.Count > _maxMessages)
        {
            _messages.RemoveAt(0);
        }
    }
    
    /// <summary>
    /// Get recent messages (for display)
    /// </summary>
    public IEnumerable<LogMessage> GetRecentMessages(int count)
    {
        return _messages.TakeLast(count);
    }
    
    /// <summary>
    /// Clear all messages
    /// </summary>
    public void Clear()
    {
        _messages.Clear();
    }
}

/// <summary>
/// A single message in the log
/// </summary>
public struct LogMessage
{
    public string Text { get; set; }
    public MessageType Type { get; set; }
    public DateTime Timestamp { get; set; }
    public int Turn { get; set; }
}

/// <summary>
/// Message type for color coding
/// </summary>
public enum MessageType
{
    Info,       // White - general information
    Combat,     // Red - combat events
    Pickup,     // Green - item pickup
    Drop,       // Gray - item drop
    Use,        // Cyan - item use
    Equip,      // Yellow - equipment changes
    Level,      // Magenta - level/progression
    Warning,    // DarkYellow - warnings
    Error,      // DarkRed - errors
    System      // Blue - system messages
}
