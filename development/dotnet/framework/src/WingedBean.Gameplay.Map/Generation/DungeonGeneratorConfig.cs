namespace WingedBean.Gameplay.Map.Generation;

/// <summary>
/// Configuration for procedural dungeon generation.
/// </summary>
public class DungeonGeneratorConfig
{
    /// <summary>
    /// Width of the map in tiles.
    /// </summary>
    public int MapWidth { get; set; } = 80;

    /// <summary>
    /// Height of the map in tiles.
    /// </summary>
    public int MapHeight { get; set; } = 45;

    /// <summary>
    /// Number of rooms to generate.
    /// </summary>
    public int RoomCount { get; set; } = 8;

    /// <summary>
    /// Minimum room width.
    /// </summary>
    public int MinRoomWidth { get; set; } = 6;

    /// <summary>
    /// Maximum room width.
    /// </summary>
    public int MaxRoomWidth { get; set; } = 12;

    /// <summary>
    /// Minimum room height.
    /// </summary>
    public int MinRoomHeight { get; set; } = 6;

    /// <summary>
    /// Maximum room height.
    /// </summary>
    public int MaxRoomHeight { get; set; } = 12;

    /// <summary>
    /// Maximum attempts to place a room before giving up.
    /// </summary>
    public int MaxPlacementAttempts { get; set; } = 50;

    /// <summary>
    /// Random seed for reproducible generation (null = random seed).
    /// </summary>
    public int? Seed { get; set; } = null;

    /// <summary>
    /// Creates a default configuration.
    /// </summary>
    public static DungeonGeneratorConfig Default() => new();

    /// <summary>
    /// Creates a small dungeon configuration.
    /// </summary>
    public static DungeonGeneratorConfig Small() => new()
    {
        MapWidth = 50,
        MapHeight = 30,
        RoomCount = 5,
        MinRoomWidth = 5,
        MaxRoomWidth = 10,
        MinRoomHeight = 5,
        MaxRoomHeight = 10
    };

    /// <summary>
    /// Creates a large dungeon configuration.
    /// </summary>
    public static DungeonGeneratorConfig Large() => new()
    {
        MapWidth = 120,
        MapHeight = 60,
        RoomCount = 15,
        MinRoomWidth = 8,
        MaxRoomWidth = 16,
        MinRoomHeight = 8,
        MaxRoomHeight = 16
    };
}
