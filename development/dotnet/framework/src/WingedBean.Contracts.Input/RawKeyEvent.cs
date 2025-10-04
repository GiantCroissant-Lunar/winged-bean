using System;

namespace WingedBean.Contracts.Input;

/// <summary>
/// Raw key/rune event from a platform.
/// Implementations translate framework-specific events (Terminal.Gui, Unity, etc.) into this.
/// </summary>
public readonly struct RawKeyEvent
{
    public int? VirtualKey { get; init; }        // e.g., ConsoleKey cast or framework-specific code
    public uint? Rune { get; init; }             // Unicode code point if available
    public bool IsCtrl { get; init; }
    public bool IsAlt { get; init; }
    public bool IsShift { get; init; }
    public DateTimeOffset Timestamp { get; init; }

    public RawKeyEvent(
        int? virtualKey,
        uint? rune,
        bool isCtrl,
        bool isAlt,
        bool isShift,
        DateTimeOffset timestamp)
    {
        VirtualKey = virtualKey;
        Rune = rune;
        IsCtrl = isCtrl;
        IsAlt = isAlt;
        IsShift = isShift;
        Timestamp = timestamp;
    }
}
