using Microsoft.Extensions.Logging;
using Plate.PluginManoi.Contracts;
using Plate.CrossMilo.Contracts;
using Plate.CrossMilo.Contracts.ECS;
using Plate.CrossMilo.Contracts.Resource.Services;
using WingedBean.Plugins.DungeonGame.Components;
using WingedBean.Plugins.DungeonGame.Data;
using WingedBean.Plugins.DungeonGame.Systems;

// Type aliases for backward compatibility during namespace migration
using IECSService = Plate.CrossMilo.Contracts.ECS.Services.IService;
using IResourceService = Plate.CrossMilo.Contracts.Resource.Services.IService;

namespace WingedBean.Plugins.DungeonGame;

/// <summary>
/// Main game class that initializes and runs the ECS-based dungeon crawler.
/// Now uses IResourceService for data-driven content loading.
/// </summary>
public class DungeonGame
{
    private readonly IRegistry _registry;
    private readonly IECSService _ecs;
    private readonly IResourceService? _resourceService;
    private readonly ILogger<DungeonGame>? _logger;
    private readonly ResourceLoader? _resourceLoader;
    private IWorld _world = null!;
    private readonly List<IECSSystem> _systems = new();
    private DateTime _lastUpdateTime;
    private bool _isInitialized = false;
    private WorldHandle _runtimeHandle = WorldHandle.Invalid;

    public DungeonGame(IRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _ecs = registry.Get<IECSService>();
        
        // Try to get resource service (optional for backward compatibility)
        try
        {
            _resourceService = registry.Get<IResourceService>();
            _logger?.LogInformation("DungeonGame initialized with resource loading support");
        }
        catch
        {
            _resourceService = null;
        }
        
        // Try to get logger
        try
        {
            _logger = registry.Get<ILogger<DungeonGame>>();
        }
        catch
        {
            _logger = null;
        }
        
        if (_resourceService != null)
        {
            ILogger<ResourceLoader>? resourceLogger = null;
            try
            {
                resourceLogger = registry.Get<ILogger<ResourceLoader>>();
            }
            catch { }
            
            _resourceLoader = new ResourceLoader(_resourceService, resourceLogger);
        }
        else
        {
            _logger?.LogWarning("IResourceService not available - using hardcoded game data");
        }
    }

    /// <summary>
    /// Initialize the game world, systems, and entities.
    /// </summary>
    public async void Initialize()
    {
        if (_isInitialized)
            return;

        _logger?.LogInformation("Initializing DungeonGame with ECS...");

        // Resolve default runtime world but delay population until systems registered
        EnsureRuntimeWorld(_ecs.DefaultRuntimeWorld, populateIfEmpty: false);
        _logger?.LogInformation("Runtime world ready ({RuntimeHandle})", _runtimeHandle);

        // Register systems in execution order
        RegisterSystems();
        _logger?.LogInformation("Registered {SystemCount} systems", _systems.Count);

        // Initialize the game world with entities (now data-driven if resources available)
        if (_resourceLoader != null)
        {
            await InitializeWorldFromResourcesAsync();
        }
        else
        {
            InitializeWorldLegacy();
        }
        
        _logger?.LogInformation("World initialized with {EntityCount} entities", _world.EntityCount);

        _lastUpdateTime = DateTime.UtcNow;
        _isInitialized = true;

        _logger?.LogInformation("DungeonGame initialization complete!");
    }

    private void RegisterSystems()
    {
        // Systems execute in order of registration
        _systems.Add(new AISystem());           // AI runs first (priority: 100)
        _systems.Add(new MovementSystem());     // Then movement (priority: 90)
        _systems.Add(new CombatSystem());       // Then combat (priority: 80)
        _systems.Add(new RenderSystem());       // Finally rendering (priority: 10)
    }

    /// <summary>
    /// Initialize world from resource files (data-driven).
    /// </summary>
    private async Task InitializeWorldFromResourcesAsync()
    {
        if (_world.EntityCount > 0)
        {
            return;
        }

        _logger?.LogInformation("Loading game data from resources...");

        try
        {
            // Preload all game resources
            await _resourceLoader!.PreloadAllAsync();

            // Load dungeon level data
            var levelData = await _resourceLoader.LoadDungeonLevelAsync("dungeon-level-01");
            if (levelData == null)
            {
                _logger?.LogWarning("Could not load dungeon level, falling back to hardcoded data");
                InitializeWorldLegacy();
                return;
            }

            _logger?.LogInformation("Loaded level: {LevelName}", levelData.Name);

            // Create player from resource data
            var playerData = await _resourceLoader.LoadPlayerAsync("default-player");
            if (playerData != null)
            {
                EntityFactory.CreatePlayer(_world, playerData);
                _logger?.LogInformation("Created player from resources");
            }
            else
            {
                CreatePlayerLegacy();
            }

            // Spawn enemies from level data
            var random = Random.Shared;
            await EntityFactory.SpawnEnemiesFromLevelAsync(_world, levelData, _resourceLoader, random, _logger);

            // Spawn items from level data  
            await EntityFactory.SpawnItemsFromLevelAsync(_world, levelData, _resourceLoader, random, _logger);

            _logger?.LogInformation("World initialized from resources successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load resources, falling back to hardcoded data");
            InitializeWorldLegacy();
        }
    }

    /// <summary>
    /// Initialize world with hardcoded data (legacy/fallback).
    /// </summary>
    private void InitializeWorldLegacy()
    {
        if (_world.EntityCount > 0)
        {
            return;
        }

        _logger?.LogInformation("Using hardcoded game data (legacy mode)");

        // Create the player entity
        CreatePlayerLegacy();

        // Create some enemy entities
        CreateEnemiesLegacy(5);
    }

    private void CreatePlayerLegacy()
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

        _logger?.LogDebug("Created player entity at (40, 12)");
    }

    private void CreateEnemiesLegacy(int count)
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

            _logger?.LogDebug("Created goblin at ({X}, {Y})", x, y);
        }
    }

    /// <summary>
    /// Update the game state for one frame.
    /// </summary>
    public void Update()
    {
        if (!_isInitialized)
        {
            _logger?.LogWarning("DungeonGame.Update() called before Initialize()");
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

        _logger?.LogInformation("Starting game loop (target: 60 FPS)...");

        while (true)
        {
            Update();

            // Target 60 FPS (16ms per frame)
            await Task.Delay(16);

            // Check for game over conditions
            if (IsGameOver())
            {
                _logger?.LogInformation("Game Over!");
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
            // Use legacy initialization when called from EnsureRuntimeWorld
            InitializeWorldLegacy();
        }
    }
}
