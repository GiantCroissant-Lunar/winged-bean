using FluentAssertions;
using WingedBean.Contracts.Game;
using WingedBean.Contracts.Input;
using WingedBean.Plugins.ConsoleDungeon.Input;

namespace WingedBean.Plugins.ConsoleDungeon.Tests.Input;

public class DefaultInputRouterTests
{
    private readonly DefaultInputRouter _router;

    public DefaultInputRouterTests()
    {
        _router = new DefaultInputRouter();
    }

    [Fact]
    public void Top_WhenEmpty_ReturnsNull()
    {
        // Assert
        _router.Top.Should().BeNull();
    }

    [Fact]
    public void PushScope_AddsToStack()
    {
        // Arrange
        var scope = new TestInputScope();

        // Act
        _router.PushScope(scope);

        // Assert
        _router.Top.Should().Be(scope);
    }

    [Fact]
    public void PushScope_MultipleScopes_LastIsTop()
    {
        // Arrange
        var scope1 = new TestInputScope();
        var scope2 = new TestInputScope();

        // Act
        _router.PushScope(scope1);
        _router.PushScope(scope2);

        // Assert
        _router.Top.Should().Be(scope2);
    }

    [Fact]
    public void PushScope_Dispose_RemovesScope()
    {
        // Arrange
        var scope1 = new TestInputScope();
        var scope2 = new TestInputScope();
        _router.PushScope(scope1);
        var handle = _router.PushScope(scope2);

        // Act
        handle.Dispose();

        // Assert
        _router.Top.Should().Be(scope1);
    }

    [Fact]
    public void Dispatch_WithNoScopes_DoesNotThrow()
    {
        // Arrange
        var inputEvent = new GameInputEvent(GameInputType.MoveUp, DateTimeOffset.UtcNow);

        // Act
        Action act = () => _router.Dispatch(inputEvent);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispatch_CallsTopScope()
    {
        // Arrange
        var scope = new TestInputScope();
        _router.PushScope(scope);
        var inputEvent = new GameInputEvent(GameInputType.MoveUp, DateTimeOffset.UtcNow);

        // Act
        _router.Dispatch(inputEvent);

        // Assert
        scope.HandledEvents.Should().ContainSingle();
        scope.HandledEvents[0].Type.Should().Be(GameInputType.MoveUp);
    }

    [Fact]
    public void Dispatch_ModalScope_BlocksLowerScopes()
    {
        // Arrange
        var gameplayScope = new TestInputScope(captureAll: false);
        var modalScope = new TestInputScope(captureAll: true);

        _router.PushScope(gameplayScope);
        _router.PushScope(modalScope);

        var inputEvent = new GameInputEvent(GameInputType.MoveUp, DateTimeOffset.UtcNow);

        // Act
        _router.Dispatch(inputEvent);

        // Assert
        modalScope.HandledEvents.Should().ContainSingle();
        gameplayScope.HandledEvents.Should().BeEmpty(); // Modal scope blocks propagation
    }

    [Fact]
    public void PushScope_NestedDispose_WorksCorrectly()
    {
        // Arrange
        var scope1 = new TestInputScope();
        var scope2 = new TestInputScope();
        var scope3 = new TestInputScope();

        var handle1 = _router.PushScope(scope1);
        var handle2 = _router.PushScope(scope2);
        var handle3 = _router.PushScope(scope3);

        // Act - Dispose in reverse order
        handle3.Dispose();
        handle2.Dispose();
        handle1.Dispose();

        // Assert
        _router.Top.Should().BeNull();
    }

    [Fact]
    public void Dispatch_MultipleEvents_AllRoutedToTopScope()
    {
        // Arrange
        var scope = new TestInputScope();
        _router.PushScope(scope);

        var event1 = new GameInputEvent(GameInputType.MoveUp, DateTimeOffset.UtcNow);
        var event2 = new GameInputEvent(GameInputType.MoveDown, DateTimeOffset.UtcNow);
        var event3 = new GameInputEvent(GameInputType.Attack, DateTimeOffset.UtcNow);

        // Act
        _router.Dispatch(event1);
        _router.Dispatch(event2);
        _router.Dispatch(event3);

        // Assert
        scope.HandledEvents.Should().HaveCount(3);
        scope.HandledEvents[0].Type.Should().Be(GameInputType.MoveUp);
        scope.HandledEvents[1].Type.Should().Be(GameInputType.MoveDown);
        scope.HandledEvents[2].Type.Should().Be(GameInputType.Attack);
    }

    // Helper test scope
    private class TestInputScope : IInputScope
    {
        public List<GameInputEvent> HandledEvents { get; } = new();
        public bool CaptureAll { get; }

        public TestInputScope(bool captureAll = false)
        {
            CaptureAll = captureAll;
        }

        public bool Handle(GameInputEvent inputEvent)
        {
            HandledEvents.Add(inputEvent);
            return true; // Always return true for test purposes
        }
    }
}
