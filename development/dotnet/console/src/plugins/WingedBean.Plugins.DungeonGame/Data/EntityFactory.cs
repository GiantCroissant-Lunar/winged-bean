using Plate.CrossMilo.Contracts.ECS;
using WingedBean.Plugins.DungeonGame.Components;

namespace WingedBean.Plugins.DungeonGame.Data;

/// <summary>
/// Factory for creating ECS entities from resource data.
/// Converts DTOs to component data and attaches to entities.
/// </summary>
public static class EntityFactory
{
    /// <summary>
    /// Create a player entity from player data.
    /// </summary>
    public static void CreatePlayer(IWorld world, PlayerData data)
    {
        var entity = world.CreateEntity();

        // Tag component
        world.AttachComponent(entity, new Player());

        // Position
        world.AttachComponent(entity, new Position(
            data.StartingPosition.X,
            data.StartingPosition.Y,
            data.StartingPosition.Floor
        ));

        // Stats
        world.AttachComponent(entity, new Stats
        {
            MaxHP = data.StartingStats.MaxHP,
            CurrentHP = data.StartingStats.MaxHP,
            MaxMana = data.StartingStats.MaxMana,
            CurrentMana = data.StartingStats.MaxMana,
            Strength = data.StartingStats.Strength,
            Dexterity = data.StartingStats.Dexterity,
            Intelligence = data.StartingStats.Intelligence,
            Defense = data.StartingStats.Defense,
            Level = data.StartingStats.Level,
            Experience = data.StartingStats.Experience
        });

        // Visual
        world.AttachComponent(entity, CreateRenderable(data.Visual));
    }

    /// <summary>
    /// Create an enemy entity from enemy data at a specific position.
    /// </summary>
    public static void CreateEnemy(IWorld world, EnemyData data, Position position)
    {
        var entity = world.CreateEntity();

        // Position
        world.AttachComponent(entity, position);

        // Enemy AI
        world.AttachComponent(entity, new Enemy
        {
            Type = ParseEnemyType(data.AI.Type),
            State = AIState.Idle,
            AggroRange = data.AI.AggroRange,
            Target = null
        });

        // Stats
        world.AttachComponent(entity, new Stats
        {
            MaxHP = data.Stats.MaxHP,
            CurrentHP = data.Stats.MaxHP,
            MaxMana = data.Stats.MaxMana,
            CurrentMana = data.Stats.MaxMana,
            Strength = data.Stats.Strength,
            Dexterity = data.Stats.Dexterity,
            Intelligence = data.Stats.Intelligence,
            Defense = data.Stats.Defense,
            Level = data.Stats.Level,
            Experience = data.Stats.Experience
        });

        // Visual
        world.AttachComponent(entity, CreateRenderable(data.Visual));
    }

    /// <summary>
    /// Create an item entity from item data at a specific position.
    /// </summary>
    public static void CreateItem(IWorld world, ItemData data, Position position)
    {
        var entity = world.CreateEntity();

        // Position
        world.AttachComponent(entity, position);

        // Item
        world.AttachComponent(entity, new Item
        {
            Type = ParseItemType(data.Type),
            Value = data.Value,
            Stackable = data.Stackable,
            StackCount = 1
        });

        // Visual
        world.AttachComponent(entity, CreateRenderable(data.Visual));
    }

    /// <summary>
    /// Create a Renderable component from visual data.
    /// </summary>
    private static Renderable CreateRenderable(VisualData visual)
    {
        return new Renderable
        {
            Symbol = visual.Symbol.Length > 0 ? visual.Symbol[0] : '?',
            ForegroundColor = ParseColor(visual.ForegroundColor),
            BackgroundColor = ParseColor(visual.BackgroundColor),
            RenderLayer = visual.RenderLayer
        };
    }

    /// <summary>
    /// Parse ConsoleColor from string.
    /// </summary>
    private static ConsoleColor ParseColor(string colorName)
    {
        return Enum.TryParse<ConsoleColor>(colorName, ignoreCase: true, out var color)
            ? color
            : ConsoleColor.White;
    }

    /// <summary>
    /// Parse EnemyType from string.
    /// </summary>
    private static EnemyType ParseEnemyType(string typeName)
    {
        return Enum.TryParse<EnemyType>(typeName, ignoreCase: true, out var type)
            ? type
            : EnemyType.Goblin;
    }

    /// <summary>
    /// Parse ItemType from string.
    /// </summary>
    private static ItemType ParseItemType(string typeName)
    {
        return typeName.ToLowerInvariant() switch
        {
            "consumable" => ItemType.HealthPotion, // Default for consumables
            "weapon" => ItemType.Sword,
            "armor" => ItemType.Armor,
            "shield" => ItemType.Shield,
            "helmet" => ItemType.Helmet,
            "boots" => ItemType.Boots,
            _ => ItemType.Gold
        };
    }

    /// <summary>
    /// Spawn enemies from dungeon level spawn data.
    /// </summary>
    public static async Task SpawnEnemiesFromLevelAsync(
        IWorld world,
        DungeonLevelData level,
        ResourceLoader resourceLoader,
        Random random)
    {
        if (level.EnemySpawns == null)
        {
            return;
        }

        foreach (var spawn in level.EnemySpawns)
        {
            var enemyData = await resourceLoader.LoadEnemyAsync(spawn.EnemyId);
            if (enemyData == null)
            {
                Console.WriteLine($"Warning: Enemy '{spawn.EnemyId}' not found in resources");
                continue;
            }

            var count = random.Next(spawn.MinCount, spawn.MaxCount + 1);

            for (int i = 0; i < count; i++)
            {
                // Pick a random spawn area
                var spawnAreas = spawn.SpawnAreas ?? Array.Empty<SpawnAreaData>();
                if (spawnAreas.Length == 0)
                {
                    continue;
                }

                var area = spawnAreas[random.Next(spawnAreas.Length)];

                // Generate random position within spawn area
                var angle = random.NextDouble() * 2 * Math.PI;
                var distance = random.NextDouble() * area.Radius;
                var x = (int)(area.X + distance * Math.Cos(angle));
                var y = (int)(area.Y + distance * Math.Sin(angle));

                // Clamp to level bounds
                x = Math.Clamp(x, 0, level.Dimensions.Width - 1);
                y = Math.Clamp(y, 0, level.Dimensions.Height - 1);

                var position = new Position(x, y, level.Floor);
                CreateEnemy(world, enemyData, position);
            }

            Console.WriteLine($"  • Spawned {count} {spawn.EnemyId}(s)");
        }
    }

    /// <summary>
    /// Spawn items from dungeon level spawn data.
    /// </summary>
    public static async Task SpawnItemsFromLevelAsync(
        IWorld world,
        DungeonLevelData level,
        ResourceLoader resourceLoader,
        Random random)
    {
        if (level.ItemSpawns == null)
        {
            return;
        }

        foreach (var spawn in level.ItemSpawns)
        {
            var itemData = await resourceLoader.LoadItemAsync(spawn.ItemId);
            if (itemData == null)
            {
                Console.WriteLine($"Warning: Item '{spawn.ItemId}' not found in resources");
                continue;
            }

            var count = random.Next(spawn.MinCount, spawn.MaxCount + 1);

            for (int i = 0; i < count; i++)
            {
                // Pick a random spawn area
                var spawnAreas = spawn.SpawnAreas ?? Array.Empty<SpawnAreaData>();
                if (spawnAreas.Length == 0)
                {
                    continue;
                }

                var area = spawnAreas[random.Next(spawnAreas.Length)];

                // Generate random position within spawn area
                var angle = random.NextDouble() * 2 * Math.PI;
                var distance = random.NextDouble() * area.Radius;
                var x = (int)(area.X + distance * Math.Cos(angle));
                var y = (int)(area.Y + distance * Math.Sin(angle));

                // Clamp to level bounds
                x = Math.Clamp(x, 0, level.Dimensions.Width - 1);
                y = Math.Clamp(y, 0, level.Dimensions.Height - 1);

                var position = new Position(x, y, level.Floor);
                CreateItem(world, itemData, position);
            }

            Console.WriteLine($"  • Spawned {count} {spawn.ItemId}(s)");
        }
    }
}
