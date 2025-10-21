using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Plate.CrossMilo.Contracts;
using Plate.CrossMilo.Contracts.Resource.Services;
using Plate.PluginManoi.Contracts;
using WingedBean.Gameplay.Perception;
using WingedBean.Gameplay.Perception.Components;
using WingedBean.Plugins.ArchECS;
using WingedBean.Plugins.ArchECS.Internal;
using WingedBean.Plugins.DungeonGame.Components;
using WingedBean.Plugins.DungeonGame.Data;
using WingedBean.Plugins.DungeonGame.Systems;
// Type alias for backward compatibility during namespace migration
using IResourceService = Plate.CrossMilo.Contracts.Resource.Services.IService;

namespace WingedBean.Plugins.DungeonGame;

/// <summary>
/// Main game class that initializes and runs the ECS-based dungeon crawler.
/// Now uses IResourceService for data-driven content loading.
/// </summary>
public class DungeonGame
{
    private readonly IRegistry _registry;
    private ArchECSService _ecs = null!;
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
        // Note: Don't resolve ArchECSService here - it may not be registered yet
        // It will be resolved in Initialize() when all plugins have activated

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

        // Resolve ArchECSService now that all plugins should be activated
        _ecs = _registry.Get<ArchECSService>();

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
        // Phase 1: Perception & FOV (calculate what entities can see)
        _systems.Add(new PerceptionSensorFOVSystem());  // Priority: 200 (migrated from GoRogue)

        // Phase 2: AI & Decision Making
        _systems.Add(new AISystem());                    // Priority: 100

        // Phase 3: Turn Management & Actions
        // _systems.Add(new TurnManagerSystem());        // Priority: 95 (excluded - needs singleton API)

        // Phase 4: Movement & Pathfinding
        // _systems.Add(new PathfindingSystem());        // Priority: 92 (excluded - class components)
        // _systems.Add(new GoRoguePathfindingSystem()); // Priority: 91 (excluded - class components)
        _systems.Add(new MovementSystem());              // Priority: 90

        // Phase 5: Combat & Interactions
        _systems.Add(new CombatSystem());                // Priority: 80
                                                         // _systems.Add(new InventorySystem());          // Priority: 70 (excluded - needs refactoring)

        // Phase 6: Camera & Rendering
        // _systems.Add(new CameraSystem());             // Priority: 20 (excluded - CameraComponent is class, needs struct)
        // _systems.Add(new RenderSystem());             // Priority: 10 (excluded - needs refactoring)
        // _systems.Add(new HUDRenderSystem());          // Priority: 5 (excluded - needs refactoring)
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

            // Emit verify markers similar to legacy path
            var width = levelData.Dimensions.Width;
            var height = levelData.Dimensions.Height;
            _logger?.LogInformation("[Verify] Map created: {Width}x{Height}", width, height);
            try { System.Console.WriteLine($"[Verify] Map created: {width}x{height}"); } catch { }

            // Ensure bot mode when requested (attach Enemy to player so AISystem can drive)
            var enableBotMode = Environment.GetEnvironmentVariable("ENABLE_BOT_MODE") == "1";
            if (enableBotMode)
            {
                foreach (var entity in _world.CreateQuery<Player>())
                {
                    _world.AttachComponent(entity, new Enemy
                    {
                        Type = EnemyType.Goblin,
                        State = AIState.Patrol,
                        AggroRange = 15.0f,
                        Target = null
                    });
                    _logger?.LogInformation("[Verify] Bot mode enabled: player is AI-controlled");
                    try { System.Console.WriteLine("[Verify] Bot mode enabled: player is AI-controlled"); } catch { }
                    break;
                }
            }

            _logger?.LogInformation("[Verify] World initialized: entityCount={EntityCount}", _world.EntityCount);
            try { System.Console.WriteLine($"[Verify] World initialized: entityCount={_world.EntityCount}"); } catch { }

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

        // Create a minimal dungeon map (80x25) with solid border walls and open interior
        var map = new DungeonMap(80, 25, 1);
        for (var x = 0; x < map.Width; x++)
        {
            for (var y = 0; y < map.Height; y++)
            {
                var isBorder = (x == 0 || y == 0 || x == map.Width - 1 || y == map.Height - 1);
                map.Tiles[x, y] = new Tile
                {
                    Type = isBorder ? TileType.Wall : TileType.Floor,
                    IsWalkable = !isBorder,
                    IsTransparent = !isBorder,
                    IsExplored = false,
                    IsVisible = false,
                    Glyph = isBorder ? '#' : '.',
                    Foreground = isBorder ? SadRogue.Primitives.Color.White : SadRogue.Primitives.Color.Gray,
                    Background = SadRogue.Primitives.Color.Black
                };
            }
        }

        // Provide the map to the FOV system if present
        var fovSystem = _systems.OfType<PerceptionSensorFOVSystem>().FirstOrDefault();
        fovSystem?.SetMap(map);
        _logger?.LogInformation("[Verify] Map created: {Width}x{Height}", map.Width, map.Height);
        AppendVerifyLog($"MapCreated width={map.Width} height={map.Height}");
        try { System.Console.WriteLine($"[Verify] Map created: {map.Width}x{map.Height}"); } catch { }

        // TODO: Initialize fog of war config (singleton API not available)
        // var fogConfig = FogOfWarConfig.CreateDefault();
        // _world.SetSingleton(fogConfig);
        // _logger?.LogDebug("Fog of war config initialized");

        // TODO: Camera entity creation disabled - CameraComponent is class, needs to be struct for Arch ECS
        // var cameraEntity = _world.CreateEntity();
        // var camera = Plate.Camera2D.Camera.CreateDefault(40, 20);
        // camera.Mode = Plate.Camera2D.CameraMode.CenterOnTarget;
        // var cameraComp = new CameraComponent(40, 20) { FollowTarget = null };
        // _world.AttachComponent(cameraEntity, cameraComp);
        // _logger?.LogDebug("Camera entity created (40x20 viewport)");

        // Create the player entity
        var playerEntity = CreatePlayerLegacy();

        // TODO: Set camera to follow player when CameraComponent is converted to struct
        var playerPos = _world.GetComponent<Position>(playerEntity);
        _logger?.LogDebug("Player created at position ({X}, {Y})", playerPos.X, playerPos.Y);

        // Create some enemy entities
        CreateEnemiesLegacy(5);

        // Emit verify marker for world state
        _logger?.LogInformation("[Verify] World initialized: entityCount={EntityCount}", _world.EntityCount);
        AppendVerifyLog($"WorldInitialized entities={_world.EntityCount}");
        try { System.Console.WriteLine($"[Verify] World initialized: entityCount={_world.EntityCount}"); } catch { }
    }

    private WingedBean.Plugins.ArchECS.Internal.EntityHandle CreatePlayerLegacy()
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

        // TODO: Fog of war - Map memory (MapMemoryComponent is class, needs struct)
        // _world.AttachComponent(player, new MapMemoryComponent());
        // _logger?.LogDebug("Added MapMemoryComponent to player");

        // TODO: Perception config for FOV (PerceptionConfigComponent not compatible with new system)
        // _world.AttachComponent(player, new PerceptionConfigComponent
        // {
        //     PerceptionTypes = new List<PerceptionTypeConfig>
        //     {
        //         new PerceptionTypeConfig
        //         {
        //             TypeId = "vision",
        //             Range = 10.0f,
        //             Algorithm = PerceptionAlgorithm.Shadowcast
        //         }
        //     }
        // });
        // _logger?.LogDebug("Added PerceptionConfigComponent to player (vision range: 10)");

        // BOT MODE: Add Enemy component to make player act as bot
        // This allows AISystem to control the player for automated testing
        var enableBotMode = Environment.GetEnvironmentVariable("ENABLE_BOT_MODE") == "1";
        if (enableBotMode)
        {
            _world.AttachComponent(player, new Enemy
            {
                Type = EnemyType.Goblin, // Reuse enemy type for bot behavior
                State = AIState.Patrol,  // Start in patrol mode (wander around)
                AggroRange = 15.0f,      // Larger range for bot exploration
                Target = null
            });
            _logger?.LogInformation("🤖 BOT MODE ENABLED - Player will be AI-controlled for automated testing");
            _logger?.LogInformation("[Verify] Bot mode enabled: player is AI-controlled");
            AppendVerifyLog("BotMode enabled=true");
            // Also echo to console to ensure visibility in recordings when logger is not available
            try { System.Console.WriteLine("[Verify] Bot mode enabled: player is AI-controlled"); } catch { }
        }

        _logger?.LogDebug("Created player entity at (40, 12)");
        return player;
    }

    private static void AppendVerifyLog(string line)
    {
        try
        {
            var overridePath = Environment.GetEnvironmentVariable("VERIFY_LOG_PATH");
            if (!string.IsNullOrWhiteSpace(overridePath))
            {
                var dir = Path.GetDirectoryName(overridePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(overridePath!, line + Environment.NewLine);
                return;
            }

            var baseDir = AppContext.BaseDirectory;
            var logsDir = Path.Combine(baseDir, "logs");
            Directory.CreateDirectory(logsDir);
            var path = Path.Combine(logsDir, "verify-game.txt");
            File.AppendAllText(path, line + Environment.NewLine);
        }
        catch { /* best-effort */ }

        // Also try to write to host bin logs directory if running from plugin subfolder
        try
        {
            var baseDir = AppContext.BaseDirectory;
            // Walk up 5 levels: net8.0 -> Debug -> bin -> <PluginName> -> plugins -> bin (host)
            var hostBin = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
            var hostLogs = Path.Combine(hostBin, "logs");
            Directory.CreateDirectory(hostLogs);
            var path2 = Path.Combine(hostLogs, "verify-game.txt");
            File.AppendAllText(path2, line + Environment.NewLine);
        }
        catch { /* best-effort */ }
    }

    private void CreateEnemiesLegacy(int count)
    {
        { { ... } }

        for (var i = 0; i < count; i++)
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
            system.Execute(_world, deltaTime);
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
