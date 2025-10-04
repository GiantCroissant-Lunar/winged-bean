using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;
using WingedBean.Contracts;
using WingedBean.Contracts.Core;
using WingedBean.Contracts.ECS;
using WingedBean.Contracts.Game;
using Xunit;

namespace WingedBean.Tests.E2E.ConsoleDungeon
{
    /// <summary>
    /// Integration test: run ConsoleDungeonApp with FakeDriver and a fake game service.
    /// Verifies Right/Down arrows and WASD map to movement and that app can quit.
    /// </summary>
    public class ConsoleDungeonApp_FakeDriverIntegrationTests : IDisposable
    {
        private readonly FakeDriver _driver;

        public ConsoleDungeonApp_FakeDriverIntegrationTests()
        {
            _driver = new FakeDriver();
            Application.Init(_driver);
            Environment.SetEnvironmentVariable("DEBUG_MINIMAL_UI", "1");
        }

        [Fact(Timeout = 7000)]
        public async Task ArrowsAndWasd_ShouldMovePlayer_Then_Quit()
        {
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
                Name = "FakeDriver-E2E",
                Parameters =
                {
                    ["registry"] = registry,
                    ["gameService"] = game
                }
            };

            var cts = new CancellationTokenSource();
            var startTask = app.StartAsync(cfg, cts.Token);
            await Task.Delay(400);

            // Send arrows via ConsoleKey (no modifiers)
            _driver.SendKeys('\0', ConsoleKey.RightArrow, false, false, false);
            _driver.SendKeys('\0', ConsoleKey.DownArrow, false, false, false);
            // WASD fallbacks
            _driver.SendKeys('d', ConsoleKey.D, false, false, false);
            _driver.SendKeys('s', ConsoleKey.S, false, false, false);
            // Quit with ESC
            _driver.SendKeys('\0', ConsoleKey.Escape, false, false, false);

            await Task.Delay(600);

            Assert.NotEmpty(game.Positions);
            var first = game.Positions.First();
            var last = game.Positions.Last();
            Assert.True(last.X > first.X, "should have moved right");
            Assert.True(last.Y > first.Y, "should have moved down");

            Assert.Contains(game.Inputs, i => i.Type == InputType.MoveRight);
            Assert.Contains(game.Inputs, i => i.Type == InputType.MoveDown);

            // Ensure the app exits deterministically via UI thread
            Application.Invoke(() => Application.RequestStop());
            await Task.Delay(80);
            cts.Cancel();
            // Request graceful stop which also disposes timers/subscriptions
            await app.StopAsync();
            await Task.WhenAny(startTask, Task.Delay(2000));
            // Hard shutdown as a final safety to avoid hangs in CI/FakeDriver
            try { Application.Shutdown(); } catch { }
        }

        public void Dispose()
        {
            Application.Shutdown();
        }

        private sealed class TestRegistry : IRegistry
        {
            private readonly Dictionary<Type, List<object>> _services = new();
            public void Register<TService>(TService implementation, int priority = 0) where TService : class
            {
                var t = typeof(TService);
                if (!_services.TryGetValue(t, out var list)) { list = new List<object>(); _services[t] = list; }
                list.Add(implementation!);
            }
            public void Register<TService>(TService implementation, ServiceMetadata metadata) where TService : class => Register(implementation);
            public TService Get<TService>(SelectionMode mode = SelectionMode.HighestPriority) where TService : class
            {
                var t = typeof(TService);
                if (_services.TryGetValue(t, out var list) && list.Count > 0) return (TService)list[0];
                throw new Exception($"Service not found: {t.Name}");
            }
            public IEnumerable<TService> GetAll<TService>() where TService : class
            {
                var t = typeof(TService);
                if (_services.TryGetValue(t, out var list)) return list.Cast<TService>();
                return Enumerable.Empty<TService>();
            }
            public bool IsRegistered<TService>() where TService : class => _services.ContainsKey(typeof(TService));
            public bool Unregister<TService>(TService implementation) where TService : class
            {
                var t = typeof(TService);
                if (_services.TryGetValue(t, out var list)) return list.Remove(implementation!);
                return false;
            }
            public void UnregisterAll<TService>() where TService : class { _services.Remove(typeof(TService)); }
            public ServiceMetadata? GetMetadata<TService>(TService implementation) where TService : class => null;
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
