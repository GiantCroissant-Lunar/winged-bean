using WingedBean.Gameplay.Map.Data;

namespace WingedBean.Gameplay.Map.Generation;

/// <summary>
/// Procedural dungeon generator using room-and-corridor algorithm.
/// This is a simple implementation that will be replaced with Plate.EdgRogqd integration later.
/// </summary>
public class DungeonGenerator
{
    private readonly Random _random;
    private readonly DungeonGeneratorConfig _config;
    private readonly List<Room> _rooms;

    public DungeonGenerator(DungeonGeneratorConfig? config = null)
    {
        _config = config ?? DungeonGeneratorConfig.Default();
        _random = _config.Seed.HasValue
            ? new Random(_config.Seed.Value)
            : new Random();
        _rooms = new List<Room>();
    }

    /// <summary>
    /// Generates a new dungeon map.
    /// </summary>
    public DungeonMap Generate()
    {
        var map = new DungeonMap(_config.MapWidth, _config.MapHeight);
        _rooms.Clear();

        // Generate rooms
        GenerateRooms();

        // Carve rooms into map
        foreach (var room in _rooms)
        {
            CarveRoom(map, room);
        }

        // Connect rooms with corridors
        ConnectRooms(map);

        return map;
    }

    /// <summary>
    /// Gets the list of generated rooms (for testing/debugging).
    /// </summary>
    public IReadOnlyList<Room> GetRooms() => _rooms.AsReadOnly();

    private void GenerateRooms()
    {
        for (var i = 0; i < _config.RoomCount; i++)
        {
            Room? room = TryPlaceRoom();
            if (room != null)
            {
                _rooms.Add(room);
            }
        }
    }

    private Room? TryPlaceRoom()
    {
        for (var attempt = 0; attempt < _config.MaxPlacementAttempts; attempt++)
        {
            var width = _random.Next(_config.MinRoomWidth, _config.MaxRoomWidth + 1);
            var height = _random.Next(_config.MinRoomHeight, _config.MaxRoomHeight + 1);

            // Check if room can fit in map
            if (width >= _config.MapWidth - 2 || height >= _config.MapHeight - 2)
            {
                continue;
            }

            var x = _random.Next(1, _config.MapWidth - width - 1);
            var y = _random.Next(1, _config.MapHeight - height - 1);

            var room = new Room(x, y, width, height);

            // Check if room overlaps with existing rooms
            var overlaps = _rooms.Any(r => r.Intersects(room, padding: 2));
            if (!overlaps)
            {
                return room;
            }
        }

        return null;
    }

    private void CarveRoom(DungeonMap map, Room room)
    {
        // Carve floor tiles (inner area)
        foreach (var (x, y) in room.GetFloorTiles())
        {
            map.SetTile(x, y, MapTile.Floor());
        }

        // Walls are already set by default in DungeonMap constructor
    }

    private void ConnectRooms(DungeonMap map)
    {
        // Connect each room to the next one
        for (var i = 0; i < _rooms.Count - 1; i++)
        {
            var room1 = _rooms[i];
            var room2 = _rooms[i + 1];

            ConnectRoomCenters(map, room1.CenterX, room1.CenterY, room2.CenterX, room2.CenterY);
        }
    }

    private void ConnectRoomCenters(DungeonMap map, int x1, int y1, int x2, int y2)
    {
        // Use L-shaped corridor
        if (_random.Next(2) == 0)
        {
            // Horizontal then vertical
            CreateHorizontalCorridor(map, x1, x2, y1);
            CreateVerticalCorridor(map, y1, y2, x2);
        }
        else
        {
            // Vertical then horizontal
            CreateVerticalCorridor(map, y1, y2, x1);
            CreateHorizontalCorridor(map, x1, x2, y2);
        }
    }

    private void CreateHorizontalCorridor(DungeonMap map, int x1, int x2, int y)
    {
        var startX = Math.Min(x1, x2);
        var endX = Math.Max(x1, x2);

        for (var x = startX; x <= endX; x++)
        {
            if (map.IsInBounds(x, y))
            {
                map.SetTile(x, y, MapTile.Floor());
            }
        }
    }

    private void CreateVerticalCorridor(DungeonMap map, int y1, int y2, int x)
    {
        var startY = Math.Min(y1, y2);
        var endY = Math.Max(y1, y2);

        for (var y = startY; y <= endY; y++)
        {
            if (map.IsInBounds(x, y))
            {
                map.SetTile(x, y, MapTile.Floor());
            }
        }
    }

    /// <summary>
    /// Gets a random walkable position in the first room (for player spawn).
    /// </summary>
    public (int x, int y)? GetSpawnPosition()
    {
        if (_rooms.Count == 0)
        {
            return null;
        }

        var firstRoom = _rooms[0];
        return (firstRoom.CenterX, firstRoom.CenterY);
    }
}
