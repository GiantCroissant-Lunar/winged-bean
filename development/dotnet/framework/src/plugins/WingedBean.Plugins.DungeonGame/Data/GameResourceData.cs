namespace WingedBean.Plugins.DungeonGame.Data;

/// <summary>
/// Data transfer objects for game resources loaded from JSON.
/// </summary>

public class EnemyData
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required StatsData Stats { get; init; }
    public required VisualData Visual { get; init; }
    public required AIData AI { get; init; }
    public LootData[]? Loot { get; init; }
}

public class ItemData
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Type { get; init; }
    public required ItemStatsData Stats { get; init; }
    public required VisualData Visual { get; init; }
    public RequirementsData? Requirements { get; init; }
    public bool Stackable { get; init; }
    public int? MaxStack { get; init; }
    public int Value { get; init; }
}

public class PlayerData
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required StatsData StartingStats { get; init; }
    public required PositionData StartingPosition { get; init; }
    public required VisualData Visual { get; init; }
    public InventoryItemData[]? StartingInventory { get; init; }
}

public class DungeonLevelData
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int Floor { get; init; }
    public required DimensionsData Dimensions { get; init; }
    public EnemySpawnData[]? EnemySpawns { get; init; }
    public ItemSpawnData[]? ItemSpawns { get; init; }
    public SafeZoneData[]? SafeZones { get; init; }
    public ExitData? ExitPosition { get; init; }
}

// Common data structures

public class StatsData
{
    public int MaxHP { get; init; }
    public int MaxMana { get; init; }
    public int Strength { get; init; }
    public int Dexterity { get; init; }
    public int Intelligence { get; init; }
    public int Defense { get; init; }
    public int Level { get; init; }
    public int Experience { get; init; }
}

public class VisualData
{
    public required string Symbol { get; init; }
    public required string ForegroundColor { get; init; }
    public required string BackgroundColor { get; init; }
    public int RenderLayer { get; init; }
}

public class AIData
{
    public required string Type { get; init; }
    public float AggroRange { get; init; }
    public float MoveSpeed { get; init; }
    public float AttackRange { get; init; }
}

public class LootData
{
    public required string ItemId { get; init; }
    public float Chance { get; init; }
    public int MinAmount { get; init; }
    public int MaxAmount { get; init; }
}

public class ItemStatsData
{
    public int? Damage { get; init; }
    public float Weight { get; init; }
    public int? Durability { get; init; }
    public float? AttackSpeed { get; init; }
    public int? HPRestore { get; init; }
    public int? ManaRestore { get; init; }
}

public class RequirementsData
{
    public int? Strength { get; init; }
    public int? Dexterity { get; init; }
    public int? Intelligence { get; init; }
    public int? Level { get; init; }
}

public class InventoryItemData
{
    public required string ItemId { get; init; }
    public int Quantity { get; init; }
    public bool Equipped { get; init; }
}

public class PositionData
{
    public int X { get; init; }
    public int Y { get; init; }
    public int Floor { get; init; }
}

public class DimensionsData
{
    public int Width { get; init; }
    public int Height { get; init; }
}

public class SpawnAreaData
{
    public int X { get; init; }
    public int Y { get; init; }
    public float Radius { get; init; }
}

public class EnemySpawnData
{
    public required string EnemyId { get; init; }
    public int MinCount { get; init; }
    public int MaxCount { get; init; }
    public SpawnAreaData[]? SpawnAreas { get; init; }
}

public class ItemSpawnData
{
    public required string ItemId { get; init; }
    public int MinCount { get; init; }
    public int MaxCount { get; init; }
    public SpawnAreaData[]? SpawnAreas { get; init; }
}

public class SafeZoneData
{
    public required string Name { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public float Radius { get; init; }
}

public class ExitData
{
    public int X { get; init; }
    public int Y { get; init; }
    public int TargetLevel { get; init; }
}
