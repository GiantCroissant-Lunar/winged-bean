using FluentAssertions;
using WingedBean.Contracts.Game;
using WingedBean.Contracts.Input;
using WingedBean.Plugins.ConsoleDungeon.Input;

namespace WingedBean.Plugins.ConsoleDungeon.Tests.Input;

public class DefaultInputMapperTests : IDisposable
{
    private readonly DefaultInputMapper _mapper;

    public DefaultInputMapperTests()
    {
        _mapper = new DefaultInputMapper();
    }

    public void Dispose()
    {
        _mapper?.Dispose();
    }

    [Theory]
    [InlineData(38, GameInputType.MoveUp)]      // UpArrow
    [InlineData(40, GameInputType.MoveDown)]    // DownArrow
    [InlineData(37, GameInputType.MoveLeft)]    // LeftArrow
    [InlineData(39, GameInputType.MoveRight)]   // RightArrow
    [InlineData(32, GameInputType.Attack)]      // Space
    [InlineData(27, GameInputType.Quit)]        // ESC
    public void Map_VirtualKey_MapsToCorrectGameInputType(int virtualKey, GameInputType expected)
    {
        // Arrange
        var rawEvent = new RawKeyEvent(
            virtualKey: virtualKey,
            rune: null,
            isCtrl: false,
            isAlt: false,
            isShift: false,
            timestamp: DateTimeOffset.UtcNow
        );

        // Act
        var result = _mapper.Map(rawEvent);

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be(expected);
    }

    [Theory]
    [InlineData('W', GameInputType.MoveUp)]
    [InlineData('w', GameInputType.MoveUp)]
    [InlineData('A', GameInputType.MoveLeft)]
    [InlineData('a', GameInputType.MoveLeft)]
    [InlineData('S', GameInputType.MoveDown)]
    [InlineData('s', GameInputType.MoveDown)]
    [InlineData('D', GameInputType.MoveRight)]
    [InlineData('d', GameInputType.MoveRight)]
    [InlineData('M', GameInputType.ToggleMenu)]
    [InlineData('m', GameInputType.ToggleMenu)]
    [InlineData('Q', GameInputType.Quit)]
    [InlineData('q', GameInputType.Quit)]
    public void Map_Character_MapsToCorrectGameInputType(char character, GameInputType expected)
    {
        // Arrange
        var rawEvent = new RawKeyEvent(
            virtualKey: null,
            rune: (uint)character,
            isCtrl: false,
            isAlt: false,
            isShift: false,
            timestamp: DateTimeOffset.UtcNow
        );

        // Act
        var result = _mapper.Map(rawEvent);

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be(expected);
    }

    [Fact]
    public void Map_CtrlC_MapsToQuit()
    {
        // Arrange
        var rawEvent = new RawKeyEvent(
            virtualKey: 3,  // Ctrl+C
            rune: null,
            isCtrl: true,
            isAlt: false,
            isShift: false,
            timestamp: DateTimeOffset.UtcNow
        );

        // Act
        var result = _mapper.Map(rawEvent);

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be(GameInputType.Quit);
    }

    [Fact]
    public void Map_UnknownKey_ReturnsNull()
    {
        // Arrange
        var rawEvent = new RawKeyEvent(
            virtualKey: 999,  // Unknown key
            rune: null,
            isCtrl: false,
            isAlt: false,
            isShift: false,
            timestamp: DateTimeOffset.UtcNow
        );

        // Act
        var result = _mapper.Map(rawEvent);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Reset_ClearsState()
    {
        // Arrange - Map some keys first
        var event1 = new RawKeyEvent(38, null, false, false, false, DateTimeOffset.UtcNow);
        _mapper.Map(event1);

        // Act
        _mapper.Reset();

        // Assert - Should not throw and should continue to work
        var event2 = new RawKeyEvent(40, null, false, false, false, DateTimeOffset.UtcNow);
        var result = _mapper.Map(event2);
        result.Should().NotBeNull();
        result!.Type.Should().Be(GameInputType.MoveDown);
    }

    [Fact]
    public void Map_PreservesTimestamp()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var rawEvent = new RawKeyEvent(
            virtualKey: 38,
            rune: null,
            isCtrl: false,
            isAlt: false,
            isShift: false,
            timestamp: timestamp
        );

        // Act
        var result = _mapper.Map(rawEvent);

        // Assert
        result.Should().NotBeNull();
        result!.Timestamp.Should().Be(timestamp);
    }
}
