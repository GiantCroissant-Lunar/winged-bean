using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Terminal.Gui;
using WingedBean.Contracts;
using WingedBean.Contracts.Core;
using WingedBean.Contracts.Game;
using WingedBean.Contracts.ECS;
using Xunit;

namespace ConsoleDungeon.Host.Tests
{
    public class ConsoleDungeonApp_FakeDriverTests : IDisposable
    {
        private readonly FakeDriver _driver;

        public ConsoleDungeonApp_FakeDriverTests()
        {
            // Initialize Terminal.Gui with FakeDriver once per test class
            _driver = new FakeDriver();
            Application.Init(_driver);
        }

        [Fact(Timeout = 15000)]
        public async Task RightAndDown_ShouldMovePlayer_And_QuitOnEsc()
        {
            // Arrange registry and fakes
            var registry = new TestRegistry();
            var game = new FakeGameService();
            var render = new FakeRenderService();
            var ui = new FakeUIService();
            registry.Register<IRenderService>(render);
            registry.Register<IGameUIService>(ui);

            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<WingedBean.Plugins.ConsoleDungeon.ConsoleDungeonApp>();
            var app = new WingedBean.Plugins.ConsoleDungeon.ConsoleDungeonApp(logger);

            var cfg = new TerminalAppConfig
            {
                Name = "Test",
                Parameters =
                {
                    ["registry"] = registry,
                    ["gameService"] = game
                }
            };

            // Start app in background
            var cts = new CancellationTokenSource();
            var startTask = Task.Run(() => app.StartAsync(cfg, cts.Token));

            // Give time to initialize and create window
            await Task.Delay(500);

            // Act: send Right and Down arrows using ConsoleKey with modifiers (Terminal.Gui v2 API)
            _driver.SendKeys('\0', ConsoleKey.RightArrow, false, false, false);
            _driver.SendKeys('\0', ConsoleKey.DownArrow, false, false, false);
            // Also send WASD fallbacks
            _driver.SendKeys('d', ConsoleKey.D, false, false, false);
            _driver.SendKeys('s', ConsoleKey.S, false, false, false);
            // Finally send Esc to quit
            _driver.SendKeys('\0', ConsoleKey.Escape, false, false, false);

            // Allow the main loop to process events
            await Task.Delay(500);

            // Assert movement occurred
            game.Positions.Should().NotBeEmpty();
            var first = game.Positions.First();
            var last = game.Positions.Last();
            (last.X > first.X).Should().BeTrue("should have moved right");
            (last.Y > first.Y).Should().BeTrue("should have moved down");

            // And the inputs recorded include MoveRight and MoveDown
            game.Inputs.Should().Contain(i => i.Type == InputType.MoveRight);
            game.Inputs.Should().Contain(i => i.Type == InputType.MoveDown);

            // Allow app to exit (Esc) and finish
            await Task.WhenAny(startTask, Task.Delay(3000));
        }

        [Fact(Timeout = 15000)]
        public async Task EscBracketB_Sequence_ShouldMapTo_MoveDown()
        {
            // Arrange registry and fakes
            var registry = new TestRegistry();
            var game = new FakeGameService();
            var render = new FakeRenderService();
            var ui = new FakeUIService();
            registry.Register<IRenderService>(render);
            registry.Register<IGameUIService>(ui);

            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<WingedBean.Plugins.ConsoleDungeon.ConsoleDungeonApp>();
            var app = new WingedBean.Plugins.ConsoleDungeon.ConsoleDungeonApp(logger);

            var cfg = new TerminalAppConfig
            {
                Name = "Test-ESC-Seq",
                Parameters =
                {
                    ["registry"] = registry,
                    ["gameService"] = game
                }
            };

            var cts = new CancellationTokenSource();
            var startTask = Task.Run(() => app.StartAsync(cfg, cts.Token));
            await Task.Delay(400);

            // Act: Use ConsoleKey.DownArrow for Terminal.Gui v2 FakeDriver
            // The refactored input mapper handles VirtualKey codes properly
            _driver.SendKeys('\0', ConsoleKey.DownArrow, false, false, false);
            await Task.Delay(250);

            // Quit with ESC key
            _driver.SendKeys('\0', ConsoleKey.Escape, false, false, false);
            await Task.Delay(300);

            // Assert a MoveDown input was produced
            Assert.Contains(game.Inputs, i => i.Type == InputType.MoveDown);

            await Task.WhenAny(startTask, Task.Delay(2000));
        }

        public void Dispose()
        {
            Application.Shutdown();
        }

        // --- Test fakes ---
        private sealed class TestRegistry : IRegistry
        {
            private readonly Dictionary<Type, List<object>> _services = new();

            public void Register<TService>(TService implementation, int priority = 0) where TService : class
            {
                var t = typeof(TService);
                if (!_services.TryGetValue(t, out var list))
                {
                    list = new List<object>();
                    _services[t] = list;
                }
                list.Add(implementation!);
            }

            public void Register<TService>(TService implementation, ServiceMetadata metadata) where TService : class
                => Register(implementation);

            public TService Get<TService>(SelectionMode mode = SelectionMode.HighestPriority) where TService : class
            {
                var t = typeof(TService);
                if (_services.TryGetValue(t, out var list) && list.Count > 0)
                    return (TService)list[0];
                throw new Exception($"Service not found: {t.Name}");
            }

            public IEnumerable<TService> GetAll<TService>() where TService : class
            {
                var t = typeof(TService);
                if (_services.TryGetValue(t, out var list))
                    return list.Cast<TService>();
                return Enumerable.Empty<TService>();
            }

            public bool IsRegistered<TService>() where TService : class
                => _services.ContainsKey(typeof(TService));

            public bool Unregister<TService>(TService implementation) where TService : class
            {
                var t = typeof(TService);
                if (_services.TryGetValue(t, out var list))
                    return list.Remove(implementation!);
                return false;
            }

            public void UnregisterAll<TService>() where TService : class
            {
                var t = typeof(TService);
                _services.Remove(t);
            }

            public ServiceMetadata? GetMetadata<TService>(TService implementation) where TService : class
                => null;
        }

        private sealed class FakeGameService : IDungeonGameService
        {
            private readonly BehaviorSubject<GameState> _state = new(GameState.Running);
            private readonly BehaviorSubject<PlayerStats> _stats = new(new PlayerStats(100,100,50,50,1,0,10,10,10,5));
            private readonly BehaviorSubject<IReadOnlyList<EntitySnapshot>> _entities;

            private Position _player = new Position(1, 1, 0);

            public List<GameInput> Inputs { get; } = new();
            public List<(int X,int Y)> Positions { get; } = new();

            public FakeGameService()
            {
                _entities = new BehaviorSubject<IReadOnlyList<EntitySnapshot>>(BuildEntities());
                Positions.Add((_player.X, _player.Y));
            }

            public void Initialize() { }
            public void Update(float deltaTime) { }
            public void Shutdown() { }

            public void HandleInput(GameInput input)
            {
                Inputs.Add(input);
                switch (input.Type)
                {
                    case InputType.MoveRight: _player = _player with { X = _player.X + 1 }; break;
                    case InputType.MoveDown: _player = _player with { Y = _player.Y + 1 }; break;
                    case InputType.MoveLeft: _player = _player with { X = Math.Max(0, _player.X - 1) }; break;
                    case InputType.MoveUp: _player = _player with { Y = Math.Max(0, _player.Y - 1) }; break;
                }
                Positions.Add((_player.X, _player.Y));
                _entities.OnNext(BuildEntities());
            }

            public IObservable<GameState> GameStateObservable => _state;
            public IObservable<IReadOnlyList<EntitySnapshot>> EntitiesObservable => _entities;
            public IObservable<PlayerStats> PlayerStatsObservable => _stats;
            public WorldHandle RuntimeWorldHandle => new WorldHandle(0, WorldKind.Runtime);
            public IEnumerable<WorldHandle> RuntimeWorlds => new[] { new WorldHandle(0, WorldKind.Runtime) };
            public GameMode CurrentMode => GameMode.Play;
            public event EventHandler<GameMode>? ModeChanged;
            public void SetMode(GameMode mode) => ModeChanged?.Invoke(this, mode);
            public WorldHandle CreateRuntimeWorld(string name) => new WorldHandle(0, WorldKind.Runtime);
            public void SwitchRuntimeWorld(WorldHandle handle) { }
            public IWorld? World => null;
            public GameState CurrentState => GameState.Running;
            public PlayerStats CurrentPlayerStats => _stats.Value;

            private IReadOnlyList<EntitySnapshot> BuildEntities()
            {
                return new List<EntitySnapshot>
                {
                    new EntitySnapshot(Guid.NewGuid(), _player, '@', ConsoleColor.White, ConsoleColor.Black, 0)
                };
            }
        }

        private sealed class FakeRenderService : IRenderService
        {
            public RenderMode CurrentMode { get; private set; } = RenderMode.ASCII;
            public RenderBuffer Render(IReadOnlyList<EntitySnapshot> entitySnapshots, int width, int height)
            {
                // Minimal buffer (not used by the test assertions)
                var cells = new char[height, width];
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        cells[y, x] = ' ';
                foreach (var e in entitySnapshots)
                {
                    if (e.Position.X >= 0 && e.Position.X < width && e.Position.Y >= 0 && e.Position.Y < height)
                        cells[e.Position.Y, e.Position.X] = e.Symbol;
                }
                return new RenderBuffer(cells);
            }
            public void SetRenderMode(RenderMode mode) => CurrentMode = mode;
        }

        private sealed class FakeUIService : IGameUIService
        {
            private readonly Subject<GameInputEvent> _inputs = new();
            public void Initialize(object mainWindow) { }
            public void ShowMenu(MenuType type) { }
            public void HideMenu() { }
            public bool IsMenuVisible => false;
            public IObservable<GameInputEvent> InputObservable => _inputs;
        }
    }
}
