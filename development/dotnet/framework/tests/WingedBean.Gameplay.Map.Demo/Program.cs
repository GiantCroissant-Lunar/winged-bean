using Arch.Core;
using WingedBean.Gameplay.Map.Components;
using WingedBean.Gameplay.Map.Data;
using WingedBean.Gameplay.Map.Generation;
using WingedBean.Gameplay.Map.Systems;

Console.WriteLine("=== WingedBean Dungeon Generation Demo ===\n");

// Create the ECS world
var world = World.Create();

// Configure and generate a dungeon
Console.WriteLine("Generating dungeon...");
var config = DungeonGeneratorConfig.Small();
config.Seed = 12345; // Use fixed seed for reproducible output

var generator = new DungeonGenerator(config);
var map = generator.Generate();
var rooms = generator.GetRooms();

Console.WriteLine($"Generated dungeon: {map.Width}x{map.Height}");
Console.WriteLine($"Rooms created: {rooms.Count}");
Console.WriteLine();

// Create map entity
var mapEntity = world.Create(new MapComponent(map, 1));

// Get spawn position and create player
var spawnPos = generator.GetSpawnPosition();
if (spawnPos.HasValue)
{
    var player = world.Create(
        new PositionComponent(spawnPos.Value.x, spawnPos.Value.y),
        BlocksMovementComponent.Create()
    );

    Console.WriteLine($"Player spawned at ({spawnPos.Value.x}, {spawnPos.Value.y})");
}

Console.WriteLine();

// Create render system
var renderSystem = new MapRenderSystem(world);

// Render the generated map
Console.WriteLine("Generated Dungeon Map:");
Console.WriteLine(new string('=', config.MapWidth));
Console.WriteLine(renderSystem.RenderMapWithEntities());
Console.WriteLine(new string('=', config.MapWidth));
Console.WriteLine();

// Show room details
Console.WriteLine("Room Details:");
for (var i = 0; i < rooms.Count; i++)
{
    var room = rooms[i];
    Console.WriteLine($"  Room {i + 1}: Position({room.X}, {room.Y}), Size({room.Width}x{room.Height}), Center({room.CenterX}, {room.CenterY})");
}

Console.WriteLine("\n=== Demo Complete ===");
