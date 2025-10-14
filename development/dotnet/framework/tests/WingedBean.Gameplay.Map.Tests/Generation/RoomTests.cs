using WingedBean.Gameplay.Map.Generation;

namespace WingedBean.Gameplay.Map.Tests.Generation;

public class RoomTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var room = new Room(10, 20, 8, 6);

        // Assert
        Assert.Equal(10, room.X);
        Assert.Equal(20, room.Y);
        Assert.Equal(8, room.Width);
        Assert.Equal(6, room.Height);
    }

    [Fact]
    public void CenterProperties_CalculateCorrectly()
    {
        // Arrange
        var room = new Room(10, 20, 8, 6);

        // Act & Assert
        Assert.Equal(14, room.CenterX); // 10 + 8/2
        Assert.Equal(23, room.CenterY); // 20 + 6/2
    }

    [Fact]
    public void BoundsProperties_CalculateCorrectly()
    {
        // Arrange
        var room = new Room(10, 20, 8, 6);

        // Act & Assert
        Assert.Equal(10, room.Left);
        Assert.Equal(17, room.Right); // 10 + 8 - 1
        Assert.Equal(20, room.Top);
        Assert.Equal(25, room.Bottom); // 20 + 6 - 1
    }

    [Fact]
    public void Intersects_ReturnsTrueForOverlappingRooms()
    {
        // Arrange
        var room1 = new Room(10, 10, 10, 10);
        var room2 = new Room(15, 15, 10, 10);

        // Act & Assert
        Assert.True(room1.Intersects(room2, padding: 0));
        Assert.True(room2.Intersects(room1, padding: 0));
    }

    [Fact]
    public void Intersects_ReturnsFalseForNonOverlappingRooms()
    {
        // Arrange
        var room1 = new Room(10, 10, 5, 5);
        var room2 = new Room(20, 20, 5, 5);

        // Act & Assert
        Assert.False(room1.Intersects(room2, padding: 0));
        Assert.False(room2.Intersects(room1, padding: 0));
    }

    [Fact]
    public void Intersects_WithPadding_DetectsNearbyRooms()
    {
        // Arrange
        var room1 = new Room(10, 10, 5, 5);
        var room2 = new Room(16, 10, 5, 5); // 1 tile gap (room1.Right=14, room2.X=16)

        // Act & Assert
        Assert.False(room1.Intersects(room2, padding: 0)); // No overlap without padding
        Assert.True(room1.Intersects(room2, padding: 1));   // Detects with padding=1
    }

    [Fact]
    public void GetAllTiles_ReturnsCorrectCount()
    {
        // Arrange
        var room = new Room(10, 10, 5, 5);

        // Act
        var tiles = room.GetAllTiles().ToList();

        // Assert
        Assert.Equal(25, tiles.Count); // 5 * 5
    }

    [Fact]
    public void GetFloorTiles_ExcludesOuterWalls()
    {
        // Arrange
        var room = new Room(10, 10, 5, 5);

        // Act
        var floorTiles = room.GetFloorTiles().ToList();

        // Assert
        Assert.Equal(9, floorTiles.Count); // 3 * 3 (inner tiles)
    }

    [Fact]
    public void GetFloorTiles_ReturnsEmptyForSmallRoom()
    {
        // Arrange - room too small to have floor tiles
        var room = new Room(10, 10, 2, 2);

        // Act
        var floorTiles = room.GetFloorTiles().ToList();

        // Assert
        Assert.Empty(floorTiles);
    }
}
