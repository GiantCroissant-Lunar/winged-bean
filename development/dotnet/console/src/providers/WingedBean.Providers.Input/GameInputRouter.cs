using Plate.Input.Core;
using ConsoleDungeon.Contracts;

namespace WingedBean.Providers.Input;

/// <summary>
/// Game-specific input router using generic StackInputRouter from Plate.Input.Core.
/// Specialized for GameInputEvent.
/// </summary>
public class GameInputRouter : StackInputRouter<GameInputEvent>
{
    // Inherits all functionality from StackInputRouter<GameInputEvent>
    // This is just a type alias for convenience and clarity in WingedBean
}
