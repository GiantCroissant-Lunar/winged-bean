using System;
using Microsoft.Extensions.Logging;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using Console = SadConsole.Console;

namespace WingedBean.Plugins.SadConsole;

/// <summary>
/// Main game screen for the dungeon game.
/// </summary>
public class DungeonGameScreen : ScreenObject
{
    private readonly Console _mainConsole;
    private readonly Console _statusBar;
    private readonly ILogger? _logger;

    public DungeonGameScreen(int width, int height, ILogger? logger = null)
    {
        _logger = logger;

        // Create main game console
        _mainConsole = new Console(width, height - 3)
        {
            Position = new Point(0, 0)
        };
        Children.Add(_mainConsole);

        // Create status bar
        _statusBar = new Console(width, 3)
        {
            Position = new Point(0, height - 3)
        };
        Children.Add(_statusBar);

        // Initialize
        DrawWelcomeScreen();
        DrawStatusBar();

        _logger?.LogInformation("DungeonGameScreen created ({Width}x{Height})", width, height);
    }

    private void DrawWelcomeScreen()
    {
        _mainConsole.Clear();

        // Title
        _mainConsole.Print(2, 2, "╔══════════════════════════════════════════════════════════╗", Color.Yellow);
        _mainConsole.Print(2, 3, "║         Console Dungeon - SadConsole Edition            ║", Color.Yellow);
        _mainConsole.Print(2, 4, "╚══════════════════════════════════════════════════════════╝", Color.Yellow);

        _mainConsole.Print(2, 6, "Press any key to start...", Color.White);

        _mainConsole.Print(2, 8, "Controls:", Color.Cyan);
        _mainConsole.Print(4, 9, "Arrow Keys - Move", Color.White);
        _mainConsole.Print(4, 10, "ESC - Quit", Color.White);

        _mainConsole.Print(2, 12, "Features:", Color.Cyan);
        _mainConsole.Print(4, 13, "✓ Plugin-based architecture", Color.Green);
        _mainConsole.Print(4, 14, "✓ Shared game logic", Color.Green);
        _mainConsole.Print(4, 15, "✓ Cross-platform rendering", Color.Green);
    }

    private void DrawStatusBar()
    {
        _statusBar.Clear();
        _statusBar.DrawLine(new Point(0, 0), new Point(_statusBar.Width - 1, 0), '═', Color.Gray);
        _statusBar.Print(2, 1, "HP: 100/100", Color.Green);
        _statusBar.Print(20, 1, "Level: 1", Color.Yellow);
        _statusBar.Print(35, 1, "XP: 0/100", Color.Cyan);
        _statusBar.Print(50, 1, "Backend: SadConsole", Color.Magenta);
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        // Handle input
        if (keyboard.IsKeyPressed(Keys.Escape))
        {
            _logger?.LogInformation("ESC pressed, exiting...");
            // Exit using MonoGame API
            Game.Instance.MonoGameInstance.Exit();
            return true;
        }

        if (keyboard.IsKeyPressed(Keys.Up))
        {
            _logger?.LogDebug("UP pressed");
            // TODO: Move player up
        }
        else if (keyboard.IsKeyPressed(Keys.Down))
        {
            _logger?.LogDebug("DOWN pressed");
            // TODO: Move player down
        }
        else if (keyboard.IsKeyPressed(Keys.Left))
        {
            _logger?.LogDebug("LEFT pressed");
            // TODO: Move player left
        }
        else if (keyboard.IsKeyPressed(Keys.Right))
        {
            _logger?.LogDebug("RIGHT pressed");
            // TODO: Move player right
        }

        return base.ProcessKeyboard(keyboard);
    }
}
