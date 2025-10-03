using WingedBean.Contracts.Core;
using WingedBean.Contracts.ECS;
using WingedBean.Plugins.DungeonGame.Components;
using WingedBean.Plugins.DungeonGame.Systems;

namespace WingedBean.Plugins.DungeonGame;

/// <summary>
/// Main game class that initializes and runs the ECS-based dungeon crawler.
/// </summary>
public class DungeonGame
{
    private readonly IRegistry _registry;
    private readonly IECSService _ecs;
    private IWorld _world = null!;
    private readonly List<IECSSystem> _systems = new();
    private DateTime _lastUpdateTime;
    private bool _isInitialized = false;
    private WorldHandle _runtimeHandle = WorldHandle.Invalid;

    public DungeonGame(IRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _ecs = registry.Get<IECSService>();
    }

    /// <summary>
    /// Initialize the game world, systems, and entities.
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
            return;

        Console.WriteLine("Initializing DungeonGame with ECS...");

        // Resolve default runtime world but delay population until systems registered
        EnsureRuntimeWorld(_ecs.DefaultRuntimeWorld, populateIfEmpty: false);
        Console.WriteLine($"✓ Runtime world ready ({_runtimeHandle})");

        // Register systems in execution order
        RegisterSystems();
        Console.WriteLine($"✓ Registered {_systems.Count} systems");

        // Initialize the game world with entities
        InitializeWorld();
        Console.WriteLine($"✓ World initialized with {_world.EntityCount} entities");

        _lastUpdateTime = DateTime.UtcNow;
        _isInitialized = true;

        Console.WriteLine("DungeonGame initialization complete!");
    }

    private void RegisterSystems()
    {
        // Systems execute in order of registration
        _systems.Add(new AISystem());           // AI runs first (priority: 100)
        _systems.Add(new MovementSystem());     // Then movement (priority: 90)
        _systems.Add(new CombatSystem());       // Then combat (priority: 80)
        _systems.Add(new RenderSystem());       // Finally rendering (priority: 10)
    }

    private void InitializeWorld()
    {
        if (_world.EntityCount > 0)
        {
            return;
        }

        // Create the player entity
        CreatePlayer();

        // Create some enemy entities
        CreateEnemies(5);

        // Create some items (for future implementation)
        // CreateItems(10);
    }

    private void CreatePlayer()
    {
        var player = _world.CreateEntity();

        // Core components
        _world.AttachComponent(player, new Player());
        _world.AttachComponent(player, new Position(40, 12, 1));

        // Stats
        _world.AttachComponent(player, new Stats
        {
            MaxHP = 100,
            CurrentHP = 100,
            MaxMana = 50,
            CurrentMana = 50,
            Strength = 10,
            Dexterity = 10,
            Intelligence = 10,
            Defense = 5,
            Level = 1,
            Experience = 0
        });

        // Visual
        _world.AttachComponent(player, new Renderable
        {
            Symbol = '@',
            ForegroundColor = ConsoleColor.Yellow,
            BackgroundColor = ConsoleColor.Black,
            RenderLayer = 2
        });

        Console.WriteLine("  • Created player entity at (40, 12)");
    }

    private void CreateEnemies(int count)
    {
        var random = Random.Shared;

        for (int i = 0; i < count; i++)
        {
            var enemy = _world.CreateEntity();

            // Random position (avoiding player start position)
            var x = random.Next(10, 70);
            var y = random.Next(2, 22);

            // Ensure not too close to player spawn
            while (Math.Abs(x - 40) < 5 && Math.Abs(y - 12) < 5)
            {
                x = random.Next(10, 70);
                y = random.Next(2, 22);
            }

            _world.AttachComponent(enemy, new Position(x, y, 1));

            // Enemy AI
            _world.AttachComponent(enemy, new Enemy
            {
                Type = EnemyType.Goblin,
                State = AIState.Idle,
                AggroRange = 8.0f,
                Target = null
            });

            // Stats (weak goblins)
            _world.AttachComponent(enemy, new Stats
            {
                MaxHP = 20,
                CurrentHP = 20,
                MaxMana = 0,
                CurrentMana = 0,
                Strength = 5,
                Dexterity = 8,
                Intelligence = 3,
                Defense = 2,
                Level = 1,
                Experience = 0
            });

            // Visual
            _world.AttachComponent(enemy, new Renderable
            {
                Symbol = 'g',
                ForegroundColor = ConsoleColor.Green,
                BackgroundColor = ConsoleColor.Black,
                RenderLayer = 2
            });

            Console.WriteLine($"  • Created goblin at ({x}, {y})");
        }
    }

    /// <summary>
    /// Update the game state for one frame.
    /// </summary>
    public void Update()
    {
        if (!_isInitialized)
        {
            Console.WriteLine("Warning: DungeonGame.Update() called before Initialize()");
            return;
        }

        // Calculate delta time
        var currentTime = DateTime.UtcNow;
        var deltaTime = (float)(currentTime - _lastUpdateTime).TotalSeconds;
        _lastUpdateTime = currentTime;

        // Execute all systems
        foreach (var system in _systems)
        {
            system.Execute(_ecs, _world, deltaTime);
        }
    }

    /// <summary>
    /// Run the game loop asynchronously.
    /// </summary>
    public async Task RunAsync()
    {
        Initialize();

        Console.WriteLine("Starting game loop (target: 60 FPS)...");

        while (true)
        {
            Update();

            // Target 60 FPS (16ms per frame)
            await Task.Delay(16);

            // Check for game over conditions
            if (IsGameOver())
            {
                Console.WriteLine("Game Over!");
                break;
            }
        }
    }

    private bool IsGameOver()
    {
        // Check if player is dead
        foreach (var entity in _world.CreateQuery<Player, Stats>())
        {
            var stats = _world.GetComponent<Stats>(entity);
            return stats.CurrentHP <= 0;
        }

        // No player found
        return true;
    }

    /// <summary>
    /// Get the ECS world instance (for querying entities).
    /// </summary>
    public IWorld? World => _world;

    /// <summary>
    /// Get the number of entities in the world.
    /// </summary>
    public int EntityCount => _world?.EntityCount ?? 0;

    public WorldHandle RuntimeHandle => _runtimeHandle;

    public void EnsureRuntimeWorld(WorldHandle handle)
    {
        EnsureRuntimeWorld(handle, populateIfEmpty: true);
    }

    public void SwitchRuntimeWorld(WorldHandle handle)
    {
        EnsureRuntimeWorld(handle, populateIfEmpty: true);
    }

    private void EnsureRuntimeWorld(WorldHandle handle, bool populateIfEmpty)
    {
        if (!handle.IsValid)
        {
            throw new ArgumentException("Runtime world handle is invalid", nameof(handle));
        }

        var world = _ecs.GetWorld(handle);
        if (world == null)
        {
            throw new InvalidOperationException($"Runtime world '{handle}' does not exist.");
        }

        _runtimeHandle = handle;
        _world = world;

        if (populateIfEmpty && _world.EntityCount == 0)
        {
            InitializeWorld();
        }
    }
}
