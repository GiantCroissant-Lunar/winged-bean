using WingedBean.Gameplay.Map.Data;

namespace WingedBean.Gameplay.Map.Tests.Data;

public class DungeonMapTests
{
    [Fact]
    public void Constructor_CreatesMapWithCorrectDimensions()
    {
        // Arrange & Act
        var map = new DungeonMap(10, 15);

        // Assert
        Assert.Equal(10, map.Width);
        Assert.Equal(15, map.Height);
    }

    [Fact]
    public void Constructor_InitializesAllTilesAsWalls()
    {
        // Arrange & Act
        var map = new DungeonMap(5, 5);

        // Assert
        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                var tile = map.GetTile(x, y);
                Assert.Equal(TileType.Wall, tile.Type);
                Assert.True(tile.BlocksMovement);
                Assert.True(tile.BlocksSight);
            }
        }
    }

    [Fact]
    public void SetTile_UpdatesTileAtPosition()
    {
        // Arrange
        var map = new DungeonMap(10, 10);
        var floorTile = MapTile.Floor();

        // Act
        map.SetTile(5, 5, floorTile);

        // Assert
        var tile = map.GetTile(5, 5);
        Assert.Equal(TileType.Floor, tile.Type);
        Assert.False(tile.BlocksMovement);
        Assert.False(tile.BlocksSight);
    }

    [Fact]
    public void GetTile_ReturnsWallForOutOfBounds()
    {
        // Arrange
        var map = new DungeonMap(10, 10);

        // Act
        var tile1 = map.GetTile(-1, 5);
        var tile2 = map.GetTile(5, -1);
        var tile3 = map.GetTile(15, 5);
        var tile4 = map.GetTile(5, 15);

        // Assert
        Assert.Equal(TileType.Wall, tile1.Type);
        Assert.Equal(TileType.Wall, tile2.Type);
        Assert.Equal(TileType.Wall, tile3.Type);
        Assert.Equal(TileType.Wall, tile4.Type);
    }

    [Fact]
    public void IsInBounds_ReturnsTrueForValidCoordinates()
    {
        // Arrange
        var map = new DungeonMap(10, 10);

        // Act & Assert
        Assert.True(map.IsInBounds(0, 0));
        Assert.True(map.IsInBounds(9, 9));
        Assert.True(map.IsInBounds(5, 5));
    }

    [Fact]
    public void IsInBounds_ReturnsFalseForInvalidCoordinates()
    {
        // Arrange
        var map = new DungeonMap(10, 10);

        // Act & Assert
        Assert.False(map.IsInBounds(-1, 0));
        Assert.False(map.IsInBounds(0, -1));
        Assert.False(map.IsInBounds(10, 0));
        Assert.False(map.IsInBounds(0, 10));
    }

    [Fact]
    public void IsWalkable_ReturnsTrueForFloorTiles()
    {
        // Arrange
        var map = new DungeonMap(10, 10);
        map.SetTile(5, 5, MapTile.Floor());

        // Act & Assert
        Assert.True(map.IsWalkable(5, 5));
    }

    [Fact]
    public void IsWalkable_ReturnsFalseForWallTiles()
    {
        // Arrange
        var map = new DungeonMap(10, 10);

        // Act & Assert
        Assert.False(map.IsWalkable(5, 5));
    }

    [Fact]
    public void IsWalkable_ReturnsFalseForOutOfBounds()
    {
        // Arrange
        var map = new DungeonMap(10, 10);

        // Act & Assert
        Assert.False(map.IsWalkable(-1, 0));
        Assert.False(map.IsWalkable(15, 15));
    }

    [Fact]
    public void SetExplored_UpdatesExploredFlag()
    {
        // Arrange
        var map = new DungeonMap(10, 10);

        // Act
        map.SetExplored(5, 5, true);

        // Assert
        var tile = map.GetTile(5, 5);
        Assert.True(tile.IsExplored);
    }

    [Fact]
    public void SetVisible_UpdatesVisibleFlag()
    {
        // Arrange
        var map = new DungeonMap(10, 10);

        // Act
        map.SetVisible(5, 5, true);

        // Assert
        var tile = map.GetTile(5, 5);
        Assert.True(tile.IsVisible);
    }

    [Fact]
    public void ClearVisibility_ClearsAllVisibilityFlags()
    {
        // Arrange
        var map = new DungeonMap(10, 10);
        map.SetVisible(5, 5, true);
        map.SetVisible(3, 3, true);
        map.SetVisible(7, 7, true);

        // Act
        map.ClearVisibility();

        // Assert
        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                var tile = map.GetTile(x, y);
                Assert.False(tile.IsVisible);
            }
        }
    }

    [Fact]
    public void CreateTestMap_CreatesMapWithRoom()
    {
        // Act
        var map = DungeonMap.CreateTestMap();

        // Assert
        Assert.Equal(20, map.Width);
        Assert.Equal(20, map.Height);

        // Check that there's a room (floor tiles) in the center
        for (var y = 5; y < 15; y++)
        {
            for (var x = 5; x < 15; x++)
            {
                var tile = map.GetTile(x, y);
                Assert.Equal(TileType.Floor, tile.Type);
            }
        }

        // Check that corners are walls
        Assert.Equal(TileType.Wall, map.GetTile(0, 0).Type);
        Assert.Equal(TileType.Wall, map.GetTile(19, 19).Type);
    }
}
