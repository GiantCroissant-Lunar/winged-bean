using Arch.Core;
using WingedBean.Gameplay.Map.Components;
using WingedBean.Gameplay.Map.Data;
using WingedBean.Gameplay.Map.Systems;

Console.WriteLine("=== WingedBean Map System Demo ===\n");

// Create the ECS world
var world = World.Create();

// Create a test map
var map = DungeonMap.CreateTestMap();
var mapEntity = world.Create(new MapComponent(map, 1));

Console.WriteLine($"Created map: {map.Width}x{map.Height}");
Console.WriteLine();

// Create render system
var renderSystem = new MapRenderSystem(world);

// Render the initial map
Console.WriteLine("Initial Map:");
Console.WriteLine(renderSystem.RenderMap());
Console.WriteLine();

// Create a player entity
var player = world.Create(
    new PositionComponent(10, 10),
    BlocksMovementComponent.Create()
);

Console.WriteLine($"Player spawned at (10, 10)");
Console.WriteLine();

// Create movement system
var movementSystem = new MovementSystem(world);

// Render map with player
Console.WriteLine("Map with Player (@):");
Console.WriteLine(renderSystem.RenderMapWithEntities());
Console.WriteLine();

// Try to move the player
Console.WriteLine("Moving player...");
Console.WriteLine($"Move right: {movementSystem.TryMove(player, 1, 0)}");
Console.WriteLine($"Move down: {movementSystem.TryMove(player, 0, 1)}");
Console.WriteLine($"Move left: {movementSystem.TryMove(player, -1, 0)}");
Console.WriteLine($"Move up: {movementSystem.TryMove(player, 0, -1)}");
Console.WriteLine();

var newPos = world.Get<PositionComponent>(player);
Console.WriteLine($"Player is now at ({newPos.X}, {newPos.Y})");
Console.WriteLine();

// Try to walk into a wall
Console.WriteLine("Attempting to walk into wall...");
var wallMove = movementSystem.TryMove(player, -10, 0);
Console.WriteLine($"Walk into wall: {wallMove} (should be false)");
Console.WriteLine();

// Render final map
Console.WriteLine("Final Map with Player:");
Console.WriteLine(renderSystem.RenderMapWithEntities());

Console.WriteLine("\n=== Demo Complete ===");
Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();
