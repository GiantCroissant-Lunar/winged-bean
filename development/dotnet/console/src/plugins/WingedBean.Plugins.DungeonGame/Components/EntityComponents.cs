using WingedBean.Contracts.ECS;

namespace WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// Player-controlled entity marker (tag component).
/// </summary>
public struct Player { }

/// <summary>
/// Enemy AI component.
/// </summary>
public struct Enemy
{
    public EnemyType Type;
    public AIState State;
    public float AggroRange;
    public EntityHandle? Target;
}

public enum EnemyType
{
    Goblin,
    Orc,
    Skeleton,
    Troll,
    Dragon
}

public enum AIState
{
    Idle,
    Patrol,
    Chase,
    Attack,
    Flee
}

/// <summary>
/// Item component.
/// </summary>
public struct Item
{
    public ItemType Type;
    public int Value;
    public bool Stackable;
    public int StackCount;
}

public enum ItemType
{
    Gold,
    HealthPotion,
    ManaPotion,
    Sword,
    Shield,
    Armor,
    Helmet,
    Boots,
    Key
}
