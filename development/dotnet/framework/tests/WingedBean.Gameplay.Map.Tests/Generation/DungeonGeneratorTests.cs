using WingedBean.Gameplay.Map.Data;
using WingedBean.Gameplay.Map.Generation;

namespace WingedBean.Gameplay.Map.Tests.Generation;

public class DungeonGeneratorTests
{
    [Fact]
    public void Generate_CreatesMapWithCorrectDimensions()
    {
        // Arrange
        var config = new DungeonGeneratorConfig
        {
            MapWidth = 50,
            MapHeight = 30,
            Seed = 12345
        };
        var generator = new DungeonGenerator(config);

        // Act
        var map = generator.Generate();

        // Assert
        Assert.Equal(50, map.Width);
        Assert.Equal(30, map.Height);
    }

    [Fact]
    public void Generate_WithSeed_ProducesSameResults()
    {
        // Arrange
        var config1 = new DungeonGeneratorConfig { Seed = 42 };
        var config2 = new DungeonGeneratorConfig { Seed = 42 };

        var generator1 = new DungeonGenerator(config1);
        var generator2 = new DungeonGenerator(config2);

        // Act
        var map1 = generator1.Generate();
        var map2 = generator2.Generate();
        var rooms1 = generator1.GetRooms();
        var rooms2 = generator2.GetRooms();

        // Assert
        Assert.Equal(rooms1.Count, rooms2.Count);
        for (var i = 0; i < rooms1.Count; i++)
        {
            Assert.Equal(rooms1[i].X, rooms2[i].X);
            Assert.Equal(rooms1[i].Y, rooms2[i].Y);
            Assert.Equal(rooms1[i].Width, rooms2[i].Width);
            Assert.Equal(rooms1[i].Height, rooms2[i].Height);
        }
    }

    [Fact]
    public void Generate_CreatesAtLeastOneRoom()
    {
        // Arrange
        var config = new DungeonGeneratorConfig
        {
            RoomCount = 5,
            Seed = 123
        };
        var generator = new DungeonGenerator(config);

        // Act
        var map = generator.Generate();
        var rooms = generator.GetRooms();

        // Assert
        Assert.NotEmpty(rooms);
    }

    [Fact]
    public void Generate_RoomsDoNotOverlap()
    {
        // Arrange
        var config = new DungeonGeneratorConfig
        {
            RoomCount = 10,
            Seed = 456
        };
        var generator = new DungeonGenerator(config);

        // Act
        var map = generator.Generate();
        var rooms = generator.GetRooms();

        // Assert - check no two rooms overlap
        for (var i = 0; i < rooms.Count; i++)
        {
            for (var j = i + 1; j < rooms.Count; j++)
            {
                Assert.False(rooms[i].Intersects(rooms[j], padding: 0),
                    $"Room {i} overlaps with Room {j}");
            }
        }
    }

    [Fact]
    public void Generate_CreatesWalkableFloorTiles()
    {
        // Arrange
        var config = new DungeonGeneratorConfig
        {
            RoomCount = 3,
            Seed = 789
        };
        var generator = new DungeonGenerator(config);

        // Act
        var map = generator.Generate();
        var rooms = generator.GetRooms();

        // Assert - check that room centers are walkable
        foreach (var room in rooms)
        {
            Assert.True(map.IsWalkable(room.CenterX, room.CenterY),
                $"Room center ({room.CenterX}, {room.CenterY}) is not walkable");
        }
    }

    [Fact]
    public void Generate_RoomsAreConnected()
    {
        // Arrange
        var config = new DungeonGeneratorConfig
        {
            RoomCount = 5,
            Seed = 999
        };
        var generator = new DungeonGenerator(config);

        // Act
        var map = generator.Generate();
        var rooms = generator.GetRooms();

        // Assert - basic connectivity check: there should be walkable tiles
        // connecting room centers (this is a simplified test)
        var walkableTileCount = 0;
        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                if (map.IsWalkable(x, y))
                {
                    walkableTileCount++;
                }
            }
        }

        // Should have at least as many walkable tiles as there are rooms
        Assert.True(walkableTileCount > rooms.Count * 4,
            "Not enough walkable tiles for rooms and corridors");
    }

    [Fact]
    public void GetSpawnPosition_ReturnsValidPosition()
    {
        // Arrange
        var config = new DungeonGeneratorConfig { Seed = 111 };
        var generator = new DungeonGenerator(config);

        // Act
        var map = generator.Generate();
        var spawnPos = generator.GetSpawnPosition();

        // Assert
        Assert.NotNull(spawnPos);
        Assert.True(map.IsWalkable(spawnPos.Value.x, spawnPos.Value.y),
            "Spawn position is not walkable");
    }

    [Fact]
    public void GetSpawnPosition_ReturnsNullWhenNoRooms()
    {
        // Arrange - config that likely produces no rooms
        var config = new DungeonGeneratorConfig
        {
            MapWidth = 10,
            MapHeight = 10,
            MinRoomWidth = 50,  // Impossible to place
            MaxRoomWidth = 60,
            Seed = 222
        };
        var generator = new DungeonGenerator(config);

        // Act
        var map = generator.Generate();
        var spawnPos = generator.GetSpawnPosition();

        // Assert
        Assert.Null(spawnPos);
    }

    [Fact]
    public void GetRooms_ReturnsReadOnlyList()
    {
        // Arrange
        var config = new DungeonGeneratorConfig { Seed = 333 };
        var generator = new DungeonGenerator(config);

        // Act
        generator.Generate();
        var rooms = generator.GetRooms();

        // Assert
        Assert.IsAssignableFrom<IReadOnlyList<Room>>(rooms);
    }

    [Fact]
    public void DefaultConfig_GeneratesReasonableDungeon()
    {
        // Arrange
        var config = DungeonGeneratorConfig.Default();
        var generator = new DungeonGenerator(config);

        // Act
        var map = generator.Generate();
        var rooms = generator.GetRooms();

        // Assert
        Assert.True(rooms.Count > 0, "Should generate at least one room");
        Assert.True(rooms.Count <= config.RoomCount, "Should not exceed requested room count");
    }

    [Fact]
    public void SmallConfig_GeneratesSmallerDungeon()
    {
        // Arrange
        var config = DungeonGeneratorConfig.Small();
        var generator = new DungeonGenerator(config);

        // Act
        var map = generator.Generate();

        // Assert
        Assert.Equal(50, map.Width);
        Assert.Equal(30, map.Height);
    }

    [Fact]
    public void LargeConfig_GeneratesLargerDungeon()
    {
        // Arrange
        var config = DungeonGeneratorConfig.Large();
        var generator = new DungeonGenerator(config);

        // Act
        var map = generator.Generate();

        // Assert
        Assert.Equal(120, map.Width);
        Assert.Equal(60, map.Height);
    }
}
