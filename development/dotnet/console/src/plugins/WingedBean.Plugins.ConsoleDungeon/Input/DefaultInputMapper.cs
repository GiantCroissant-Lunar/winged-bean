using WingedBean.Contracts.Game;
using WingedBean.Contracts.Input;

namespace WingedBean.Plugins.ConsoleDungeon.Input;

/// <summary>
/// Default input mapper for console/terminal environments.
/// </summary>
public class DefaultInputMapper : IInputMapper, IDisposable
{
    private readonly System.Timers.Timer _escTimer;
    private bool _escPending = false;
    private const int EscDisambiguationMs = 150;

    public DefaultInputMapper()
    {
        _escTimer = new System.Timers.Timer(EscDisambiguationMs);
        _escTimer.AutoReset = false;
        _escTimer.Elapsed += (s, e) => Reset();
    }

    public GameInputEvent? Map(RawKeyEvent rawEvent)
    {
        // Priority 1: VirtualKey codes
        if (rawEvent.VirtualKey.HasValue)
        {
            var mapped = MapVirtualKey(rawEvent.VirtualKey.Value, rawEvent.IsCtrl);
            if (mapped.HasValue)
                return new GameInputEvent(mapped.Value, rawEvent.Timestamp);
        }

        // Priority 2: Character mappings
        if (rawEvent.Rune.HasValue)
        {
            var mapped = MapCharacter(rawEvent.Rune.Value);
            if (mapped.HasValue)
                return new GameInputEvent(mapped.Value, rawEvent.Timestamp);
        }

        return null;
    }

    public void Reset()
    {
        _escPending = false;
        _escTimer.Stop();
    }

    private GameInputType? MapVirtualKey(int virtualKey, bool isCtrl)
    {
        return virtualKey switch
        {
            38 => GameInputType.MoveUp,
            40 => GameInputType.MoveDown,
            37 => GameInputType.MoveLeft,
            39 => GameInputType.MoveRight,
            32 => GameInputType.Attack,
            77 or 109 => GameInputType.ToggleMenu,
            27 => GameInputType.Quit,
            3 when isCtrl => GameInputType.Quit,
            _ => null
        };
    }

    private GameInputType? MapCharacter(uint rune)
    {
        return rune switch
        {
            'W' or 'w' => GameInputType.MoveUp,
            'A' or 'a' => GameInputType.MoveLeft,
            'S' or 's' => GameInputType.MoveDown,
            'D' or 'd' => GameInputType.MoveRight,
            ' ' => GameInputType.Attack,
            'M' or 'm' => GameInputType.ToggleMenu,
            'Q' or 'q' => GameInputType.Quit,
            _ => null
        };
    }

    public void Dispose() => _escTimer?.Dispose();
}
