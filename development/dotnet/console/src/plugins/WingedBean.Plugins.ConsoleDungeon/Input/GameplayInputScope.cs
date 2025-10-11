using Microsoft.Extensions.Logging;
using ConsoleDungeon.Contracts;
using Plate.CrossMilo.Contracts.Input;

// Type aliases for IService pattern
using IInputScope = Plate.CrossMilo.Contracts.Input.Scope.IService;

namespace WingedBean.Plugins.ConsoleDungeon.Input;

/// <summary>
/// Gameplay input scope - converts GameInputEvent to GameInput and sends to game service.
/// </summary>
public class GameplayInputScope : IInputScope
{
    private readonly IDungeonService _gameService;
    private readonly ILogger? _logger;

    public bool CaptureAll => false;

    public GameplayInputScope(IDungeonService gameService, ILogger? logger = null)
    {
        _gameService = gameService;
        _logger = logger;
    }

    public bool Handle(GameInputEvent inputEvent)
    {
        _logger?.LogDebug($"GameplayScope handling: {inputEvent.Type}");

        var gameInput = inputEvent.Type switch
        {
            GameInputType.MoveUp => new GameInput(InputType.MoveUp),
            GameInputType.MoveDown => new GameInput(InputType.MoveDown),
            GameInputType.MoveLeft => new GameInput(InputType.MoveLeft),
            GameInputType.MoveRight => new GameInput(InputType.MoveRight),
            GameInputType.Attack => new GameInput(InputType.Attack),
            GameInputType.Use => new GameInput(InputType.UseItem),
            GameInputType.ToggleInventory => new GameInput(InputType.Inventory),
            GameInputType.Quit => new GameInput(InputType.Quit),
            _ => null
        };

        if (gameInput == null)
            return false;

        _gameService.HandleInput(gameInput);
        return true;
    }
}
