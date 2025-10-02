namespace WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// Position in the dungeon (tile-based).
/// </summary>
public struct Position
{
    public int X;
    public int Y;
    public int Floor;

    public Position(int x, int y, int floor = 1)
    {
        X = x;
        Y = y;
        Floor = floor;
    }
}

/// <summary>
/// Character statistics.
/// </summary>
public struct Stats
{
    public int MaxHP;
    public int CurrentHP;
    public int MaxMana;
    public int CurrentMana;
    public int Strength;
    public int Dexterity;
    public int Intelligence;
    public int Defense;
    public int Level;
    public int Experience;
}

/// <summary>
/// Visual representation in terminal.
/// </summary>
public struct Renderable
{
    public char Symbol;
    public ConsoleColor ForegroundColor;
    public ConsoleColor BackgroundColor;
    public int RenderLayer; // 0=floor, 1=items, 2=creatures, 3=effects
}

/// <summary>
/// Collision/blocking component.
/// </summary>
public struct Blocking
{
    public bool BlocksMovement;
    public bool BlocksLight;
}
