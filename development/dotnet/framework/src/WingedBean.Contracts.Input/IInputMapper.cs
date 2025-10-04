using WingedBean.Contracts.Game;

namespace WingedBean.Contracts.Input;

/// <summary>
/// Maps raw key events into high-level game input events.
/// Framework-agnostic: implementations handle platform-specific quirks
/// (CSI sequences, SS3, ESC disambiguation, etc.)
/// </summary>
public interface IInputMapper
{
    /// <summary>
    /// Map a raw key into a GameInputEvent or null if not recognized/pending.
    /// Implementations may buffer incomplete sequences (e.g., ESC [ A)
    /// or use short timers to disambiguate standalone ESC vs CSI sequences.
    /// </summary>
    GameInputEvent? Map(RawKeyEvent rawEvent);

    /// <summary>
    /// Reset mapper state (clear buffered sequences, timers).
    /// Called when focus lost or context changed.
    /// </summary>
    void Reset();
}
