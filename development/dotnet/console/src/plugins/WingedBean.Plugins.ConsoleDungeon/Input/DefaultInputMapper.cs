using ConsoleDungeon.Contracts;
using Plate.CrossMilo.Contracts.Input;

// Type alias for IService pattern
using IInputMapper = Plate.CrossMilo.Contracts.Input.Mapper.IService;

namespace WingedBean.Plugins.ConsoleDungeon.Input;

/// <summary>
/// Default input mapper for console/terminal environments.
/// Supports VirtualKey codes, CSI/SS3 sequences, ESC disambiguation, and WASD fallback.
/// </summary>
public class DefaultInputMapper : IInputMapper, IDisposable
{
    private readonly System.Timers.Timer _escTimer;
    private bool _escPending = false;
    private bool _escBracketPending = false;  // CSI: ESC [
    private bool _escOPending = false;        // SS3: ESC O
    private readonly List<uint> _sequenceBuffer = new();
    private const int EscDisambiguationMs = 150;
    private const int SequenceTimeoutMs = 200;

    public DefaultInputMapper()
    {
        _escTimer = new System.Timers.Timer(EscDisambiguationMs);
        _escTimer.AutoReset = false;
        _escTimer.Elapsed += (s, e) => OnSequenceTimeout();
    }

    public GameInputEvent? Map(RawKeyEvent rawEvent)
    {
        // Priority 1: VirtualKey codes (most reliable)
        if (rawEvent.VirtualKey.HasValue)
        {
            var mapped = MapVirtualKey(rawEvent.VirtualKey.Value, rawEvent.IsCtrl);
            if (mapped.HasValue)
                return new GameInputEvent(mapped.Value, rawEvent.Timestamp);
        }

        // Priority 2: Escape sequence handling
        if (rawEvent.Rune.HasValue)
        {
            var rune = rawEvent.Rune.Value;
            
            // ESC sequence start
            if (rune == 0x1B && !_escPending && !_escBracketPending && !_escOPending)
            {
                _escPending = true;
                _sequenceBuffer.Clear();
                _escTimer.Stop();
                _escTimer.Interval = EscDisambiguationMs;
                _escTimer.Start();
                return null; // Wait for next character or timeout
            }

            // ESC [ (CSI sequence)
            if (_escPending && rune == '[')
            {
                _escBracketPending = true;
                _escPending = false;
                _escTimer.Stop();
                _escTimer.Interval = SequenceTimeoutMs;
                _escTimer.Start();
                return null;
            }

            // ESC O (SS3 sequence)
            if (_escPending && rune == 'O')
            {
                _escOPending = true;
                _escPending = false;
                _escTimer.Stop();
                _escTimer.Interval = SequenceTimeoutMs;
                _escTimer.Start();
                return null;
            }

            // CSI arrow keys: ESC [ A-D
            if (_escBracketPending)
            {
                _sequenceBuffer.Add(rune);
                var mapped = TryCompleteCSISequence();
                if (mapped.HasValue)
                {
                    Reset();
                    return new GameInputEvent(mapped.Value, rawEvent.Timestamp);
                }
                // Continue buffering if not complete
                return null;
            }

            // SS3 arrow keys: ESC O A-D
            if (_escOPending)
            {
                _sequenceBuffer.Add(rune);
                var mapped = TryCompleteSS3Sequence();
                if (mapped.HasValue)
                {
                    Reset();
                    return new GameInputEvent(mapped.Value, rawEvent.Timestamp);
                }
                // Continue buffering if not complete
                return null;
            }

            // Standalone ESC (after disambiguation timeout) -> Quit
            if (_escPending)
            {
                Reset();
                return new GameInputEvent(GameInputType.Quit, rawEvent.Timestamp);
            }

            // Priority 3: Character mappings (WASD, etc.)
            var charMapped = MapCharacter(rune);
            if (charMapped.HasValue)
                return new GameInputEvent(charMapped.Value, rawEvent.Timestamp);
        }

        return null;
    }

    public void Reset()
    {
        _escPending = false;
        _escBracketPending = false;
        _escOPending = false;
        _sequenceBuffer.Clear();
        _escTimer.Stop();
    }

    private void OnSequenceTimeout()
    {
        // Timeout means standalone ESC or incomplete sequence
        if (_escPending)
        {
            // Standalone ESC -> treat as Quit
            _escPending = false;
        }
        else if (_escBracketPending || _escOPending)
        {
            // Incomplete sequence -> discard
            Reset();
        }
    }

    private GameInputType? TryCompleteCSISequence()
    {
        if (_sequenceBuffer.Count == 0) return null;

        // Simple CSI sequences: ESC [ A-D (arrow keys)
        if (_sequenceBuffer.Count == 1)
        {
            return _sequenceBuffer[0] switch
            {
                'A' => GameInputType.MoveUp,
                'B' => GameInputType.MoveDown,
                'C' => GameInputType.MoveRight,
                'D' => GameInputType.MoveLeft,
                _ => null
            };
        }

        // Extended CSI sequences (e.g., ESC [ 1 ; 5 A for Ctrl+Up)
        // Not yet implemented - return null to continue buffering
        return null;
    }

    private GameInputType? TryCompleteSS3Sequence()
    {
        if (_sequenceBuffer.Count == 0) return null;

        // SS3 sequences: ESC O A-D (arrow keys in application mode)
        if (_sequenceBuffer.Count == 1)
        {
            return _sequenceBuffer[0] switch
            {
                'A' => GameInputType.MoveUp,
                'B' => GameInputType.MoveDown,
                'C' => GameInputType.MoveRight,
                'D' => GameInputType.MoveLeft,
                _ => null
            };
        }

        return null;
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
