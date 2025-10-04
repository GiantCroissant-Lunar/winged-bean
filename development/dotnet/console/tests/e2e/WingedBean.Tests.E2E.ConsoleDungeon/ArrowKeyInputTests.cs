using Terminal.Gui;
using Xunit;
using Xunit.Abstractions;

namespace WingedBean.Tests.E2E.ConsoleDungeon;

/// <summary>
/// E2E tests for arrow key input handling in ConsoleDungeonApp
/// Uses Terminal.Gui FakeDriver to simulate key events
/// Issue #214: https://github.com/GiantCroissant-Lunar/winged-bean/issues/214
/// </summary>
public class ArrowKeyInputTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly FakeDriver _driver;

    public ArrowKeyInputTests(ITestOutputHelper output)
    {
        _output = output;

        // Initialize Terminal.Gui with FakeDriver for testing
        _driver = new FakeDriver();
        Application.Init(_driver);

        _output.WriteLine("Terminal.Gui initialized with FakeDriver");
    }

    [Fact]
    public void FakeDriver_ShouldGenerateKeyEvents_ForAllArrowKeys()
    {
        // Arrange
        var keysReceived = new List<(KeyCode keyCode, uint rune)>();
        var window = new Window("Test Window");

        window.KeyDown += (s, e) =>
        {
            keysReceived.Add((e.KeyCode, e.AsRune.Value));
            _output.WriteLine($"KeyDown: KeyCode={e.KeyCode}, Rune=0x{e.AsRune.Value:X}");
        };

        Application.Top.Add(window);
        window.SetFocus();

        // Act - Send all 4 arrow keys
        _output.WriteLine("Sending Up arrow (ESC [ A)");
        _driver.SendKeys('\x1b', '[', 'A', ConsoleKey.UpArrow, '\x1b', '[', 'A');

        _output.WriteLine("Sending Down arrow (ESC [ B)");
        _driver.SendKeys('\x1b', '[', 'B', ConsoleKey.DownArrow, '\x1b', '[', 'B');

        _output.WriteLine("Sending Left arrow (ESC [ D)");
        _driver.SendKeys('\x1b', '[', 'D', ConsoleKey.LeftArrow, '\x1b', '[', 'D');

        _output.WriteLine("Sending Right arrow (ESC [ C)");
        _driver.SendKeys('\x1b', '[', 'C', ConsoleKey.RightArrow, '\x1b', '[', 'C');

        // Process events
        Application.RunIteration(ref Toplevel._forcedTopLevelIteration);

        // Assert
        _output.WriteLine($"\nTotal KeyDown events received: {keysReceived.Count}");

        foreach (var (keyCode, rune) in keysReceived)
        {
            _output.WriteLine($"  - KeyCode={keyCode}, Rune=0x{rune:X}");
        }

        // Expect at least 4 events (one per arrow)
        Assert.True(keysReceived.Count >= 4, $"Expected at least 4 KeyDown events, got {keysReceived.Count}");

        // Check if we got all 4 arrow KeyCodes
        var upEvents = keysReceived.Count(k => k.keyCode == KeyCode.CursorUp);
        var downEvents = keysReceived.Count(k => k.keyCode == KeyCode.CursorDown);
        var leftEvents = keysReceived.Count(k => k.keyCode == KeyCode.CursorLeft);
        var rightEvents = keysReceived.Count(k => k.keyCode == KeyCode.CursorRight);

        _output.WriteLine($"\nKeyCode counts:");
        _output.WriteLine($"  CursorUp: {upEvents}");
        _output.WriteLine($"  CursorDown: {downEvents}");
        _output.WriteLine($"  CursorLeft: {leftEvents}");
        _output.WriteLine($"  CursorRight: {rightEvents}");

        // Issue #214: Down and Right arrows don't generate events
        Assert.True(upEvents > 0, "CursorUp events not received");
        Assert.True(downEvents > 0, "CursorDown events not received - ISSUE #214");
        Assert.True(leftEvents > 0, "CursorLeft events not received");
        Assert.True(rightEvents > 0, "CursorRight events not received - ISSUE #214");
    }

    [Fact]
    public void Label_ShouldNotInterceptArrowKeys_WhenCanFocusIsFalse()
    {
        // Arrange
        var keysReceived = new List<KeyCode>();
        var window = new Window("Test Window");

        var label = new Label
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = false, // Same as ConsoleDungeonApp game world view
            Text = "Test label"
        };

        window.Add(label);

        window.KeyDown += (s, e) =>
        {
            keysReceived.Add(e.KeyCode);
            _output.WriteLine($"Window.KeyDown: {e.KeyCode}");
        };

        Application.Top.Add(window);
        window.SetFocus();

        // Act - Send Down and Right arrows (the problematic ones)
        _output.WriteLine("Sending Down arrow");
        _driver.SendKeys('\x1b', '[', 'B', ConsoleKey.DownArrow, '\x1b', '[', 'B');

        _output.WriteLine("Sending Right arrow");
        _driver.SendKeys('\x1b', '[', 'C', ConsoleKey.RightArrow, '\x1b', '[', 'C');

        Application.RunIteration(ref Toplevel._forcedTopLevelIteration);

        // Assert
        _output.WriteLine($"\nKeyDown events at Window level: {keysReceived.Count}");

        var downCount = keysReceived.Count(k => k == KeyCode.CursorDown);
        var rightCount = keysReceived.Count(k => k == KeyCode.CursorRight);

        _output.WriteLine($"  CursorDown: {downCount}");
        _output.WriteLine($"  CursorRight: {rightCount}");

        // If Label intercepts these keys, Window.KeyDown won't fire
        Assert.True(downCount > 0, "Label intercepted Down arrow - Window.KeyDown not fired");
        Assert.True(rightCount > 0, "Label intercepted Right arrow - Window.KeyDown not fired");
    }

    [Theory]
    [InlineData(ConsoleKey.UpArrow, KeyCode.CursorUp)]
    [InlineData(ConsoleKey.DownArrow, KeyCode.CursorDown)]
    [InlineData(ConsoleKey.LeftArrow, KeyCode.CursorLeft)]
    [InlineData(ConsoleKey.RightArrow, KeyCode.CursorRight)]
    public void ArrowKey_ShouldGenerateCorrectKeyCode(ConsoleKey consoleKey, KeyCode expectedKeyCode)
    {
        // Arrange
        KeyCode? receivedKeyCode = null;
        var window = new Window("Test");

        window.KeyDown += (s, e) =>
        {
            receivedKeyCode = e.KeyCode;
            _output.WriteLine($"KeyDown: {e.KeyCode}");
        };

        Application.Top.Add(window);
        window.SetFocus();

        // Act
        _output.WriteLine($"Sending {consoleKey}");
        _driver.SendKeys('\x1b', '[', 'X', consoleKey, '\x1b', '[', 'X'); // X is placeholder

        Application.RunIteration(ref Toplevel._forcedTopLevelIteration);

        // Assert
        Assert.NotNull(receivedKeyCode);
        Assert.Equal(expectedKeyCode, receivedKeyCode);
    }

    [Fact]
    public void EscapeKey_ShouldGenerateEscKeyCode()
    {
        // Arrange
        var keysReceived = new List<KeyCode>();
        var window = new Window("Test");

        window.KeyDown += (s, e) =>
        {
            keysReceived.Add(e.KeyCode);
            _output.WriteLine($"KeyDown: {e.KeyCode}");
        };

        Application.Top.Add(window);
        window.SetFocus();

        // Act
        _output.WriteLine("Sending Esc key");
        _driver.SendKeys('\x1b', ConsoleKey.Escape, '\x1b');

        Application.RunIteration(ref Toplevel._forcedTopLevelIteration);

        // Assert
        var escCount = keysReceived.Count(k => k == KeyCode.Esc);
        _output.WriteLine($"Esc KeyCode events: {escCount}");

        Assert.True(escCount > 0, "Esc key not generating KeyCode.Esc");
    }

    [Fact]
    public void CharacterKey_M_ShouldGenerateRuneEvent()
    {
        // Arrange
        uint? receivedRune = null;
        var window = new Window("Test");

        window.KeyDown += (s, e) =>
        {
            receivedRune = e.AsRune.Value;
            _output.WriteLine($"KeyDown: Rune='{(char)e.AsRune.Value}' (0x{e.AsRune.Value:X})");
        };

        Application.Top.Add(window);
        window.SetFocus();

        // Act
        _output.WriteLine("Sending 'M' key");
        _driver.SendKeys('M', ConsoleKey.M, 'M');

        Application.RunIteration(ref Toplevel._forcedTopLevelIteration);

        // Assert
        Assert.NotNull(receivedRune);
        Assert.Equal((uint)'M', receivedRune);
    }

    public void Dispose()
    {
        Application.Shutdown();
        _output.WriteLine("Terminal.Gui shutdown");
    }
}
