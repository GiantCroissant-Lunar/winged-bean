namespace WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// Fog of war rendering configuration.
/// Singleton component attached to world.
/// </summary>
public class FogOfWarConfig
{
    /// <summary>Fog of war mode</summary>
    public FogMode Mode { get; set; } = FogMode.Standard;

    /// <summary>Brightness of explored tiles (0.0-1.0)</summary>
    public float ExploredBrightness { get; set; } = 0.4f;

    /// <summary>Brightness of visible tiles (0.0-1.0)</summary>
    public float VisibleBrightness { get; set; } = 1.0f;

    /// <summary>Show entities in explored tiles?</summary>
    public bool ShowEntitiesInMemory { get; set; } = false;

    /// <summary>Unexplored tile character</summary>
    public char UnexploredGlyph { get; set; } = ' ';

    /// <summary>
    /// Create default fog of war configuration.
    /// </summary>
    public static FogOfWarConfig CreateDefault()
    {
        return new FogOfWarConfig
        {
            Mode = FogMode.Standard,
            ExploredBrightness = 0.4f,
            VisibleBrightness = 1.0f,
            ShowEntitiesInMemory = false,
            UnexploredGlyph = ' '
        };
    }
}

/// <summary>
/// Fog of war modes.
/// </summary>
public enum FogMode
{
    /// <summary>No fog of war (see everything)</summary>
    None,

    /// <summary>Standard roguelike (visible/explored/unexplored)</summary>
    Standard,

    /// <summary>Only show currently visible (no memory)</summary>
    OnlyVisible,

    /// <summary>Gradual fade based on distance from FOV</summary>
    GradualFade
}

/// <summary>
/// Visibility state for rendering.
/// Calculated from perception/FOV data.
/// </summary>
public enum TileVisibility
{
    /// <summary>Never seen, render as black/hidden</summary>
    Unexplored,

    /// <summary>Seen before but not in current FOV, render dimmed</summary>
    Explored,

    /// <summary>Currently in FOV, render at full brightness</summary>
    Visible
}

/// <summary>
/// Map memory component - tracks which tiles have been explored.
/// Attached to entities that can explore (usually player).
/// </summary>
public class MapMemoryComponent
{
    /// <summary>Set of all tiles that have been explored</summary>
    public HashSet<(int X, int Y)> ExploredTiles { get; set; }

    /// <summary>
    /// Create a new map memory component.
    /// </summary>
    public MapMemoryComponent()
    {
        ExploredTiles = new HashSet<(int X, int Y)>();
    }

    /// <summary>
    /// Mark a tile as explored.
    /// </summary>
    public void MarkExplored(int x, int y)
    {
        ExploredTiles.Add((x, y));
    }

    /// <summary>
    /// Check if a tile has been explored.
    /// </summary>
    public bool IsExplored(int x, int y)
    {
        return ExploredTiles.Contains((x, y));
    }

    /// <summary>
    /// Clear all explored tiles (for new map/level).
    /// </summary>
    public void Clear()
    {
        ExploredTiles.Clear();
    }
}
