using System;
using Plate.Input.Core;
using Plate.CrossMilo.Contracts.Input;
using ConsoleDungeon.Contracts;

namespace WingedBean.Providers.Input;

/// <summary>
/// Game-specific input mapper using ConsoleInputMapper from Plate.Input.Core.
/// Maps console input to GameInputEvent for the dungeon crawler game.
/// </summary>
public class GameInputMapper : ConsoleInputMapper<GameInputEvent>
{
    public GameInputMapper() : base(MapRawKeyToGameInput)
    {
    }

    private static GameInputEvent? MapRawKeyToGameInput(RawKeyEvent rawEvent)
    {
        // Priority 1: VirtualKey codes (most reliable)
        if (rawEvent.VirtualKey.HasValue)
        {
            var mapped = MapVirtualKey(rawEvent.VirtualKey.Value, rawEvent.IsCtrl);
            if (mapped.HasValue)
                return new GameInputEvent(mapped.Value, rawEvent.Timestamp);
        }

        // Priority 2: Character mappings (WASD, etc.)
        if (rawEvent.Rune.HasValue)
        {
            var charMapped = MapCharacter(rawEvent.Rune.Value);
            if (charMapped.HasValue)
                return new GameInputEvent(charMapped.Value, rawEvent.Timestamp);
        }

        return null;
    }

    private static GameInputType? MapVirtualKey(int virtualKey, bool isCtrl)
    {
        return virtualKey switch
        {
            38 => GameInputType.MoveUp,      // Up arrow
            40 => GameInputType.MoveDown,    // Down arrow
            37 => GameInputType.MoveLeft,    // Left arrow
            39 => GameInputType.MoveRight,   // Right arrow
            32 => GameInputType.Attack,      // Space
            77 or 109 => GameInputType.ToggleMenu,  // M or m
            27 => GameInputType.Quit,        // ESC
            3 when isCtrl => GameInputType.Quit,  // Ctrl+C
            _ => null
        };
    }

    private static GameInputType? MapCharacter(uint rune)
    {
        return rune switch
        {
            'W' or 'w' => GameInputType.MoveUp,
            'A' or 'a' => GameInputType.MoveLeft,
            'S' or 's' => GameInputType.MoveDown,
            'D' or 'd' => GameInputType.MoveRight,
            ' ' => GameInputType.Attack,
            'M' or 'm' => GameInputType.ToggleMenu,
            'I' or 'i' => GameInputType.ToggleInventory,
            'E' or 'e' => GameInputType.Use,
            'P' or 'p' => GameInputType.Pickup,
            'Q' or 'q' => GameInputType.Quit,
            _ => null
        };
    }
}
