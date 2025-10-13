using ObservableCollections;
using ReactiveUI;
using MessagePipe;
using SadRogue.Primitives;
using WingedBean.Plugins.DungeonGame.Events;
using System.Reactive.Linq;

namespace WingedBean.Plugins.DungeonGame.ViewModels;

/// <summary>
/// Main ViewModel for the dungeon game.
/// Uses ObservableCollections for efficient UI binding and MessagePipe for event handling.
/// </summary>
public class DungeonViewModel : ReactiveObject, IDisposable
{
    private readonly IDisposable _tileSubscription;
    private readonly IDisposable _entityMovedSubscription;
    private readonly IDisposable _entitySpawnedSubscription;
    private readonly IDisposable _entityDestroyedSubscription;
    private readonly IDisposable _fovSubscription;
    private readonly IDisposable _playerStatsSubscription;

    private int _playerHP;
    private int _playerMaxHP;
    private int _playerMana;
    private int _playerMaxMana;
    private int _playerLevel;
    private int _playerExperience;
    private int _currentFloor;
    private bool _isFOVDirty;

    // ObservableCollections for efficient UI updates
    public ObservableDictionary<Point, TileViewModel> Tiles { get; }
    public ObservableList<EntityViewModel> Entities { get; }
    public ObservableHashSet<Point> VisibleTiles { get; }
    public ObservableHashSet<Point> ExploredTiles { get; }

    // Reactive properties for player stats
    public int PlayerHP
    {
        get => _playerHP;
        set => this.RaiseAndSetIfChanged(ref _playerHP, value);
    }

    public int PlayerMaxHP
    {
        get => _playerMaxHP;
        set => this.RaiseAndSetIfChanged(ref _playerMaxHP, value);
    }

    public int PlayerMana
    {
        get => _playerMana;
        set => this.RaiseAndSetIfChanged(ref _playerMana, value);
    }

    public int PlayerMaxMana
    {
        get => _playerMaxMana;
        set => this.RaiseAndSetIfChanged(ref _playerMaxMana, value);
    }

    public int PlayerLevel
    {
        get => _playerLevel;
        set => this.RaiseAndSetIfChanged(ref _playerLevel, value);
    }

    public int PlayerExperience
    {
        get => _playerExperience;
        set => this.RaiseAndSetIfChanged(ref _playerExperience, value);
    }

    public int CurrentFloor
    {
        get => _currentFloor;
        set => this.RaiseAndSetIfChanged(ref _currentFloor, value);
    }

    public bool IsFOVDirty
    {
        get => _isFOVDirty;
        set => this.RaiseAndSetIfChanged(ref _isFOVDirty, value);
    }

    public DungeonViewModel(
        ISubscriber<TileChangedEvent> tileSubscriber,
        ISubscriber<EntityMovedEvent> entityMovedSubscriber,
        ISubscriber<EntitySpawnedEvent> entitySpawnedSubscriber,
        ISubscriber<EntityDestroyedEvent> entityDestroyedSubscriber,
        ISubscriber<FOVCalculatedEvent> fovSubscriber,
        ISubscriber<PlayerStatsChangedEvent> playerStatsSubscriber)
    {
        // Initialize ObservableCollections
        Tiles = new ObservableDictionary<Point, TileViewModel>();
        Entities = new ObservableList<EntityViewModel>();
        VisibleTiles = new ObservableHashSet<Point>();
        ExploredTiles = new ObservableHashSet<Point>();

        // Subscribe to MessagePipe events
        _tileSubscription = tileSubscriber.Subscribe(OnTileChanged);
        _entityMovedSubscription = entityMovedSubscriber.Subscribe(OnEntityMoved);
        _entitySpawnedSubscription = entitySpawnedSubscriber.Subscribe(OnEntitySpawned);
        _entityDestroyedSubscription = entityDestroyedSubscriber.Subscribe(OnEntityDestroyed);
        _fovSubscription = fovSubscriber.Subscribe(OnFOVCalculated);
        _playerStatsSubscription = playerStatsSubscriber.Subscribe(OnPlayerStatsChanged);
    }

    private void OnTileChanged(TileChangedEvent evt)
    {
        if (Tiles.TryGetValue(evt.Position, out var tileVM))
        {
            tileVM.UpdateFromTile(evt.NewTile);
        }
        else
        {
            Tiles[evt.Position] = new TileViewModel(evt.Position, evt.NewTile);
        }
    }

    private void OnEntityMoved(EntityMovedEvent evt)
    {
        var entity = Entities.FirstOrDefault(e => e.Id == evt.EntityId);
        if (entity != null)
        {
            entity.Position = evt.NewPosition;
        }
    }

    private void OnEntitySpawned(EntitySpawnedEvent evt)
    {
        // Create a new entity ViewModel
        var entityVM = new EntityViewModel(
            evt.EntityId,
            evt.EntityType,
            evt.Position,
            GetGlyphForEntityType(evt.EntityType),
            GetColorForEntityType(evt.EntityType),
            Color.Black
        );
        
        Entities.Add(entityVM);
    }

    private void OnEntityDestroyed(EntityDestroyedEvent evt)
    {
        var entity = Entities.FirstOrDefault(e => e.Id == evt.EntityId);
        if (entity != null)
        {
            Entities.Remove(entity);
        }
    }

    private void OnFOVCalculated(FOVCalculatedEvent evt)
    {
        // Update visible tiles
        VisibleTiles.Clear();
        foreach (var tile in evt.VisibleTiles)
        {
            VisibleTiles.Add(tile);
            ExploredTiles.Add(tile); // Once seen, always explored
        }

        // Update tile visibility
        foreach (var kvp in Tiles)
        {
            kvp.Value.IsVisible = VisibleTiles.Contains(kvp.Key);
            if (ExploredTiles.Contains(kvp.Key))
            {
                kvp.Value.IsExplored = true;
            }
        }

        // Update entity visibility
        foreach (var entity in Entities)
        {
            entity.IsVisible = VisibleTiles.Contains(entity.Position);
        }

        IsFOVDirty = false;
    }

    private void OnPlayerStatsChanged(PlayerStatsChangedEvent evt)
    {
        PlayerHP = evt.HP;
        PlayerMaxHP = evt.MaxHP;
        PlayerMana = evt.Mana;
        PlayerMaxMana = evt.MaxMana;
        PlayerLevel = evt.Level;
        PlayerExperience = evt.Experience;
    }

    private static char GetGlyphForEntityType(string entityType) => entityType switch
    {
        "Player" => '@',
        "Goblin" => 'g',
        "Orc" => 'o',
        "Skeleton" => 's',
        "Troll" => 'T',
        "Dragon" => 'D',
        "HealthPotion" => '!',
        "Gold" => '$',
        _ => '?'
    };

    private static Color GetColorForEntityType(string entityType) => entityType switch
    {
        "Player" => Color.Yellow,
        "Goblin" => Color.Green,
        "Orc" => Color.DarkGreen,
        "Skeleton" => Color.White,
        "Troll" => Color.Brown,
        "Dragon" => Color.Red,
        "HealthPotion" => Color.Magenta,
        "Gold" => Color.Gold,
        _ => Color.Gray
    };

    public void Dispose()
    {
        _tileSubscription?.Dispose();
        _entityMovedSubscription?.Dispose();
        _entitySpawnedSubscription?.Dispose();
        _entityDestroyedSubscription?.Dispose();
        _fovSubscription?.Dispose();
        _playerStatsSubscription?.Dispose();
    }
}
