namespace WingedBean.Plugins.ArchECS.Internal;

/// <summary>
/// Represents the high-level mode of the gameplay loop.
/// Internal to ArchECS plugin - not part of public API.
/// </summary>
public enum GameMode
{
    Play,
    EditOverlay,
    EditPaused
}
