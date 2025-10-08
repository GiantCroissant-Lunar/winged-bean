using Microsoft.Extensions.Logging;
using Plate.CrossMilo.Contracts.Resource.Services;

// Type alias for backward compatibility during namespace migration
using IResourceService = Plate.CrossMilo.Contracts.Resource.Services.IService;

namespace WingedBean.Plugins.DungeonGame.Data;

/// <summary>
/// Helper class for loading game resources.
/// Provides caching and error handling for game data.
/// </summary>
public class ResourceLoader
{
    private readonly IResourceService _resourceService;
    private readonly ILogger<ResourceLoader>? _logger;
    
    // Cached collections
    private Dictionary<string, EnemyData>? _enemies;
    private Dictionary<string, ItemData>? _items;
    private Dictionary<string, PlayerData>? _players;
    private Dictionary<string, DungeonLevelData>? _levels;

    public ResourceLoader(IResourceService resourceService, ILogger<ResourceLoader>? logger = null)
    {
        _resourceService = resourceService ?? throw new ArgumentNullException(nameof(resourceService));
        _logger = logger;
    }

    /// <summary>
    /// Load all enemy definitions.
    /// </summary>
    public async Task<Dictionary<string, EnemyData>> LoadEnemiesAsync()
    {
        if (_enemies != null)
        {
            return _enemies;
        }

        _logger?.LogInformation("Loading enemy data from resources...");

        var enemyList = await _resourceService.LoadAllAsync<EnemyData>("enemies/*.json");
        _enemies = enemyList.ToDictionary(e => e.Id, e => e);

        _logger?.LogInformation("Loaded {Count} enemy type(s)", _enemies.Count);
        return _enemies;
    }

    /// <summary>
    /// Load a specific enemy by ID.
    /// </summary>
    public async Task<EnemyData?> LoadEnemyAsync(string enemyId)
    {
        var enemies = await LoadEnemiesAsync();
        return enemies.TryGetValue(enemyId, out var enemy) ? enemy : null;
    }

    /// <summary>
    /// Load all item definitions.
    /// </summary>
    public async Task<Dictionary<string, ItemData>> LoadItemsAsync()
    {
        if (_items != null)
        {
            return _items;
        }

        _logger?.LogInformation("Loading item data from resources...");

        var itemList = await _resourceService.LoadAllAsync<ItemData>("items/*.json");
        _items = itemList.ToDictionary(i => i.Id, i => i);

        _logger?.LogInformation("Loaded {Count} item type(s)", _items.Count);
        return _items;
    }

    /// <summary>
    /// Load a specific item by ID.
    /// </summary>
    public async Task<ItemData?> LoadItemAsync(string itemId)
    {
        var items = await LoadItemsAsync();
        return items.TryGetValue(itemId, out var item) ? item : null;
    }

    /// <summary>
    /// Load all player configurations.
    /// </summary>
    public async Task<Dictionary<string, PlayerData>> LoadPlayersAsync()
    {
        if (_players != null)
        {
            return _players;
        }

        _logger?.LogInformation("Loading player data from resources...");

        var playerList = await _resourceService.LoadAllAsync<PlayerData>("players/*.json");
        _players = playerList.ToDictionary(p => p.Id, p => p);

        _logger?.LogInformation("Loaded {Count} player configuration(s)", _players.Count);
        return _players;
    }

    /// <summary>
    /// Load a specific player configuration by ID.
    /// </summary>
    public async Task<PlayerData?> LoadPlayerAsync(string playerId)
    {
        var players = await LoadPlayersAsync();
        return players.TryGetValue(playerId, out var player) ? player : null;
    }

    /// <summary>
    /// Load all dungeon level definitions.
    /// </summary>
    public async Task<Dictionary<string, DungeonLevelData>> LoadDungeonLevelsAsync()
    {
        if (_levels != null)
        {
            return _levels;
        }

        _logger?.LogInformation("Loading dungeon level data from resources...");

        var levelList = await _resourceService.LoadAllAsync<DungeonLevelData>("dungeons/*.json");
        _levels = levelList.ToDictionary(l => l.Id, l => l);

        _logger?.LogInformation("Loaded {Count} dungeon level(s)", _levels.Count);
        return _levels;
    }

    /// <summary>
    /// Load a specific dungeon level by ID.
    /// </summary>
    public async Task<DungeonLevelData?> LoadDungeonLevelAsync(string levelId)
    {
        var levels = await LoadDungeonLevelsAsync();
        return levels.TryGetValue(levelId, out var level) ? level : null;
    }

    /// <summary>
    /// Preload all game resources.
    /// </summary>
    public async Task PreloadAllAsync()
    {
        _logger?.LogInformation("Preloading all game resources...");

        await Task.WhenAll(
            LoadEnemiesAsync(),
            LoadItemsAsync(),
            LoadPlayersAsync(),
            LoadDungeonLevelsAsync()
        );

        _logger?.LogInformation("All game resources preloaded successfully");
    }

    /// <summary>
    /// Clear all cached resources.
    /// </summary>
    public void ClearCache()
    {
        _enemies = null;
        _items = null;
        _players = null;
        _levels = null;
        
        _logger?.LogDebug("Resource cache cleared");
    }
}
