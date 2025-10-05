using WingedBean.Contracts.Hosting;

namespace WingedBean.Contracts.UI;

/// <summary>
/// Platform-agnostic UI application.
/// Extends IWingedBeanApp with rendering capabilities.
/// </summary>
public interface IUIApp : IWingedBeanApp
{
    /// <summary>
    /// Render the current frame.
    /// Called by platform-specific host's render loop.
    /// </summary>
    Task RenderAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Handle user input (platform-agnostic).
    /// </summary>
    Task HandleInputAsync(InputEvent input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resize/reconfigure the UI surface.
    /// </summary>
    Task ResizeAsync(int width, int height, CancellationToken cancellationToken = default);

    /// <summary>
    /// UI-specific events.
    /// </summary>
    event EventHandler<UIEventArgs>? UIEvent;
}

/// <summary>
/// Platform-agnostic input event.
/// </summary>
public abstract class InputEvent
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class KeyInputEvent : InputEvent
{
    public string Key { get; set; } = string.Empty;
    public bool Ctrl { get; set; }
    public bool Alt { get; set; }
    public bool Shift { get; set; }
}

public class MouseInputEvent : InputEvent
{
    public int X { get; set; }
    public int Y { get; set; }
    public MouseButton Button { get; set; }
    public MouseEventType EventType { get; set; }
}

public enum MouseButton { Left, Right, Middle, None }
public enum MouseEventType { Click, Move, Scroll }

public class UIEventArgs : EventArgs
{
    public string EventType { get; set; } = string.Empty;
    public object? Data { get; set; }
}
