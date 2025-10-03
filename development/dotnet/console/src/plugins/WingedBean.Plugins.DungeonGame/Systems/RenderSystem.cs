using WingedBean.Contracts.ECS;
using WingedBean.Plugins.DungeonGame.Components;

namespace WingedBean.Plugins.DungeonGame.Systems;

/// <summary>
/// System responsible for rendering entities with Renderable components to the console.
/// Processes entities with Position and Renderable components and outputs them to the terminal.
/// </summary>
public class RenderSystem : IECSSystem
{
    private readonly List<(Position pos, Renderable render)> _renderBuffer = new();

    public void Execute(IECSService ecs, IWorld world, float deltaTime)
    {
        // Clear the render buffer
        _renderBuffer.Clear();

        // Collect all renderable entities
        foreach (var entity in world.CreateQuery<Position, Renderable>())
        {
            var pos = world.GetComponent<Position>(entity);
            var render = world.GetComponent<Renderable>(entity);
            _renderBuffer.Add((pos, render));
        }

        // Sort by render layer (lower layers render first)
        _renderBuffer.Sort((a, b) => a.render.RenderLayer.CompareTo(b.render.RenderLayer));

        // For now, we'll just clear the console and render to it directly
        // In a full implementation, this would use a proper rendering service
        // Console.Clear(); // Commented out to avoid flickering in real-time

        // Render each entity
        foreach (var (pos, render) in _renderBuffer)
        {
            // Only render if within screen bounds
            if (pos.X >= 0 && pos.X < Console.WindowWidth &&
                pos.Y >= 0 && pos.Y < Console.WindowHeight)
            {
                try
                {
                    Console.SetCursorPosition(pos.X, pos.Y);
                    Console.ForegroundColor = render.ForegroundColor;
                    Console.BackgroundColor = render.BackgroundColor;
                    Console.Write(render.Symbol);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Ignore rendering errors due to window resizing
                }
            }
        }

        // Render UI at the bottom
        RenderUI(world);

        // Reset console colors
        Console.ResetColor();
    }

    private static void RenderUI(IWorld world)
    {
        // Find player and render stats
        foreach (var entity in world.CreateQuery<Player, Stats>())
        {
            var stats = world.GetComponent<Stats>(entity);

            // Render stats at bottom of screen
            int uiY = Math.Max(0, Console.WindowHeight - 1);

            try
            {
                Console.SetCursorPosition(0, uiY);
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Black;

                string ui = $"HP: {stats.CurrentHP}/{stats.MaxHP}  " +
                           $"MP: {stats.CurrentMana}/{stats.MaxMana}  " +
                           $"Lvl: {stats.Level}  " +
                           $"XP: {stats.Experience}";

                Console.Write(ui.PadRight(Console.WindowWidth));
            }
            catch (ArgumentOutOfRangeException)
            {
                // Ignore rendering errors
            }

            break; // Only render first player
        }
    }
}
