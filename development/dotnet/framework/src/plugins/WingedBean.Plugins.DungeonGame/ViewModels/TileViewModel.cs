using ReactiveUI;
using SadRogue.Primitives;
using WingedBean.Plugins.DungeonGame.Components;

namespace WingedBean.Plugins.DungeonGame.ViewModels;

/// <summary>
/// ViewModel for a single tile in the dungeon.
/// Uses ReactiveUI for property change notifications.
/// </summary>
public class TileViewModel : ReactiveObject
{
    private bool _isVisible;
    private bool _isExplored;
    private char _glyph;
    private Color _foreground;
    private Color _background;
    private TileType _type;

    public Point Position { get; }

    public TileType Type
    {
        get => _type;
        set => this.RaiseAndSetIfChanged(ref _type, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }

    public bool IsExplored
    {
        get => _isExplored;
        set => this.RaiseAndSetIfChanged(ref _isExplored, value);
    }

    public char Glyph
    {
        get => _glyph;
        set => this.RaiseAndSetIfChanged(ref _glyph, value);
    }

    public Color Foreground
    {
        get => _foreground;
        set => this.RaiseAndSetIfChanged(ref _foreground, value);
    }

    public Color Background
    {
        get => _background;
        set => this.RaiseAndSetIfChanged(ref _background, value);
    }

    public TileViewModel(Point position, Tile tile)
    {
        Position = position;
        _type = tile.Type;
        _isVisible = tile.IsVisible;
        _isExplored = tile.IsExplored;
        _glyph = tile.Glyph;
        _foreground = tile.Foreground;
        _background = tile.Background;
    }

    public void UpdateFromTile(Tile tile)
    {
        Type = tile.Type;
        IsVisible = tile.IsVisible;
        IsExplored = tile.IsExplored;
        Glyph = tile.Glyph;
        Foreground = tile.Foreground;
        Background = tile.Background;
    }
}
