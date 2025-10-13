using ReactiveUI;
using SadRogue.Primitives;

namespace WingedBean.Plugins.DungeonGame.ViewModels;

/// <summary>
/// ViewModel for a game entity (player, enemy, item).
/// Uses ReactiveUI for property change notifications.
/// </summary>
public class EntityViewModel : ReactiveObject
{
    private Point _position;
    private char _glyph;
    private Color _foreground;
    private Color _background;
    private bool _isVisible;
    private int _hp;
    private int _maxHP;

    public int Id { get; }
    public string EntityType { get; }

    public Point Position
    {
        get => _position;
        set => this.RaiseAndSetIfChanged(ref _position, value);
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

    public bool IsVisible
    {
        get => _isVisible;
        set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }

    public int HP
    {
        get => _hp;
        set => this.RaiseAndSetIfChanged(ref _hp, value);
    }

    public int MaxHP
    {
        get => _maxHP;
        set => this.RaiseAndSetIfChanged(ref _maxHP, value);
    }

    public EntityViewModel(int id, string entityType, Point position, char glyph, Color foreground, Color background)
    {
        Id = id;
        EntityType = entityType;
        _position = position;
        _glyph = glyph;
        _foreground = foreground;
        _background = background;
        _isVisible = true;
        _hp = 0;
        _maxHP = 0;
    }
}
