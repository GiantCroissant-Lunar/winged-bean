# Game Entity Guide

**Version**: 1.0.0
**Last Updated**: 2025-10-02
**Related**: [ECS Architecture Guide](./ecs-architecture.md)

## Overview

This guide describes the entity types and component compositions used in ConsoleDungeon. Each entity is composed of components that define its data and behavior.

## Entity Types

### Player Entity

The player-controlled character.

**Components**:
```csharp
var player = world.CreateEntity();
world.AttachComponent(player, new Player());           // Tag component
world.AttachComponent(player, new Position(40, 12, 1)); // Starting position
world.AttachComponent(player, new Stats
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
world.AttachComponent(player, new Renderable
{
    Symbol = '@',
    ForegroundColor = ConsoleColor.Yellow,
    BackgroundColor = ConsoleColor.Black,
    RenderLayer = 2  // Creatures layer
});
```

**Characteristics**:
- Single instance per game
- Controlled by user input
- Can level up through experience
- Has health and mana pools

**Processed by**:
- `MovementSystem`: Handles user movement input
- `CombatSystem`: Deals damage to enemies
- `RenderSystem`: Renders player sprite and stats UI

### Enemy Entities

#### Goblin

Weak, fast enemies with low health.

**Components**:
```csharp
var goblin = world.CreateEntity();
world.AttachComponent(goblin, new Position(x, y, 1));
world.AttachComponent(goblin, new Enemy
{
    Type = EnemyType.Goblin,
    State = AIState.Idle,
    AggroRange = 8.0f,
    Target = null
});
world.AttachComponent(goblin, new Stats
{
    MaxHP = 20,
    CurrentHP = 20,
    Strength = 5,
    Dexterity = 8,
    Intelligence = 3,
    Defense = 2,
    Level = 1,
    Experience = 0
});
world.AttachComponent(goblin, new Renderable
{
    Symbol = 'g',
    ForegroundColor = ConsoleColor.Green,
    BackgroundColor = ConsoleColor.Black,
    RenderLayer = 2
});
```

**AI Behavior**:
- Idle when player not in range
- Chases player within aggro range (8 tiles)
- Attacks when adjacent
- Awards 10 XP on death

**Processed by**:
- `AISystem`: Updates AI state and pathfinding
- `MovementSystem`: Bounds checking
- `CombatSystem`: Deals/receives damage
- `RenderSystem`: Renders enemy sprite

#### Other Enemy Types (Future)

**Orc**: Stronger, slower, higher HP
- MaxHP: 40, Strength: 8, Defense: 5
- Symbol: 'o', Color: DarkRed
- AggroRange: 10.0f

**Skeleton**: Undead, resistant to some damage
- MaxHP: 15, Strength: 6, Defense: 3
- Symbol: 's', Color: White
- AggroRange: 6.0f

**Troll**: High HP regeneration
- MaxHP: 80, Strength: 12, Defense: 8
- Symbol: 'T', Color: DarkGreen
- AggroRange: 5.0f

**Dragon**: Boss enemy, area attacks
- MaxHP: 200, Strength: 20, Defense: 15
- Symbol: 'D', Color: Red
- AggroRange: 15.0f

### Item Entities (Future)

#### Health Potion

**Components**:
```csharp
var potion = world.CreateEntity();
world.AttachComponent(potion, new Position(x, y, 1));
world.AttachComponent(potion, new Item
{
    Type = ItemType.HealthPotion,
    Value = 25,  // Restores 25 HP
    Stackable = true,
    StackCount = 1
});
world.AttachComponent(potion, new Renderable
{
    Symbol = '!',
    ForegroundColor = ConsoleColor.Red,
    BackgroundColor = ConsoleColor.Black,
    RenderLayer = 1  // Items layer
});
```

#### Gold

```csharp
var gold = world.CreateEntity();
world.AttachComponent(gold, new Position(x, y, 1));
world.AttachComponent(gold, new Item
{
    Type = ItemType.Gold,
    Value = 10,
    Stackable = true,
    StackCount = 1
});
world.AttachComponent(gold, new Renderable
{
    Symbol = '$',
    ForegroundColor = ConsoleColor.Yellow,
    BackgroundColor = ConsoleColor.Black,
    RenderLayer = 1
});
```

#### Equipment (Sword, Shield, Armor)

```csharp
var sword = world.CreateEntity();
world.AttachComponent(sword, new Position(x, y, 1));
world.AttachComponent(sword, new Item
{
    Type = ItemType.Sword,
    Value = 50,  // Price
    Stackable = false,
    StackCount = 1
});
world.AttachComponent(sword, new Equipment
{
    Slot = EquipmentSlot.Weapon,
    StrengthBonus = 5,
    DefenseBonus = 0
});
world.AttachComponent(sword, new Renderable
{
    Symbol = '/',
    ForegroundColor = ConsoleColor.Cyan,
    BackgroundColor = ConsoleColor.Black,
    RenderLayer = 1
});
```

### Environmental Entities (Future)

#### Wall

```csharp
var wall = world.CreateEntity();
world.AttachComponent(wall, new Position(x, y, 1));
world.AttachComponent(wall, new Blocking
{
    BlocksMovement = true,
    BlocksLight = true
});
world.AttachComponent(wall, new Renderable
{
    Symbol = '#',
    ForegroundColor = ConsoleColor.Gray,
    BackgroundColor = ConsoleColor.Black,
    RenderLayer = 0  // Floor layer
});
```

#### Door

```csharp
var door = world.CreateEntity();
world.AttachComponent(door, new Position(x, y, 1));
world.AttachComponent(door, new Blocking
{
    BlocksMovement = true,  // Until opened
    BlocksLight = true
});
world.AttachComponent(door, new Interactable
{
    Type = InteractionType.Open,
    RequiresKey = false
});
world.AttachComponent(door, new Renderable
{
    Symbol = '+',
    ForegroundColor = ConsoleColor.DarkYellow,
    BackgroundColor = ConsoleColor.Black,
    RenderLayer = 0
});
```

#### Stairs (Down/Up)

```csharp
var stairs = world.CreateEntity();
world.AttachComponent(stairs, new Position(x, y, 1));
world.AttachComponent(stairs, new Transition
{
    TargetFloor = 2,
    TargetX = x,
    TargetY = y
});
world.AttachComponent(stairs, new Renderable
{
    Symbol = '>',
    ForegroundColor = ConsoleColor.White,
    BackgroundColor = ConsoleColor.Black,
    RenderLayer = 0
});
```

## Component Reference

### Tag Components

Components with no fields, used for identification:

- **`Player`**: Marks the player entity
- **`Boss`**: Marks boss enemies
- **`Quest`**: Marks quest-related entities

### Data Components

| Component | Purpose | Key Fields |
|-----------|---------|------------|
| `Position` | Spatial location | X, Y, Floor |
| `Stats` | Character attributes | HP, Mana, Strength, Defense, Level, XP |
| `Renderable` | Visual display | Symbol, ForegroundColor, RenderLayer |
| `Enemy` | AI behavior | Type, State, AggroRange, Target |
| `Item` | Inventory item | Type, Value, Stackable, StackCount |
| `Blocking` | Collision | BlocksMovement, BlocksLight |
| `Inventory` | Held items | Items (List), MaxSlots |

### Future Components

| Component | Purpose | Fields |
|-----------|---------|--------|
| `Equipment` | Worn items | Slot, StatBonuses |
| `Interactable` | Usable objects | Type, RequiresKey |
| `Transition` | Level changes | TargetFloor, TargetX, TargetY |
| `Light` | Light source | Radius, Intensity, Color |
| `Projectile` | Ranged attacks | Direction, Speed, Damage |
| `StatusEffect` | Buffs/debuffs | Type, Duration, Intensity |

## Entity Creation Patterns

### Factory Method Pattern

```csharp
public static class EntityFactory
{
    public static EntityHandle CreateGoblin(IWorld world, int x, int y)
    {
        var entity = world.CreateEntity();
        world.AttachComponent(entity, new Position(x, y, 1));
        world.AttachComponent(entity, new Enemy
        {
            Type = EnemyType.Goblin,
            State = AIState.Idle,
            AggroRange = 8.0f
        });
        world.AttachComponent(entity, new Stats
        {
            MaxHP = 20,
            CurrentHP = 20,
            Strength = 5,
            Defense = 2
        });
        world.AttachComponent(entity, new Renderable
        {
            Symbol = 'g',
            ForegroundColor = ConsoleColor.Green,
            RenderLayer = 2
        });
        return entity;
    }

    public static EntityHandle CreateHealthPotion(IWorld world, int x, int y)
    {
        // Similar pattern...
    }
}
```

### Template-Based Creation (Future)

```csharp
public class EntityTemplate
{
    public string Name { get; set; }
    public Dictionary<Type, object> Components { get; set; }
}

// Load from JSON
var template = JsonSerializer.Deserialize<EntityTemplate>(json);
var entity = world.CreateFromTemplate(template);
```

## Entity Lifecycle

```
Create → Add Components → Active → (Destroyed) → Recycled
```

1. **Create**: `world.CreateEntity()` returns `EntityHandle`
2. **Compose**: Attach components with `world.AttachComponent()`
3. **Process**: Systems query and modify components
4. **Destroy**: `world.DestroyEntity()` removes entity and components
5. **Recycle**: Arch reuses entity slots for performance

## Best Practices

### Entity Creation

✅ **DO**: Create entities in batches for performance
✅ **DO**: Use factory methods for complex entities
✅ **DO**: Initialize all required components immediately
❌ **DON'T**: Create entities during system iteration
❌ **DON'T**: Leave entities in invalid states (missing required components)

### Component Management

✅ **DO**: Query for minimal component sets
✅ **DO**: Check `IsAlive()` before using cached handles
✅ **DO**: Use tag components for entity identification
❌ **DON'T**: Store entity references long-term
❌ **DON'T**: Add duplicate components to same entity

### Performance

✅ **DO**: Batch entity destruction
✅ **DO**: Use object pooling for frequently created entities
❌ **DON'T**: Create/destroy entities every frame
❌ **DON'T**: Store large data in components (use asset references)

## Debugging Tips

### Entity Inspector (Future)

```csharp
public static void PrintEntity(IWorld world, EntityHandle entity)
{
    Console.WriteLine($"Entity {entity.Id}:");
    if (world.HasComponent<Position>(entity))
    {
        var pos = world.GetComponent<Position>(entity);
        Console.WriteLine($"  Position: ({pos.X}, {pos.Y}, Floor {pos.Floor})");
    }
    if (world.HasComponent<Stats>(entity))
    {
        var stats = world.GetComponent<Stats>(entity);
        Console.WriteLine($"  HP: {stats.CurrentHP}/{stats.MaxHP}");
    }
    // ... check other components
}
```

### Entity Count Monitoring

```csharp
Console.WriteLine($"Total entities: {world.EntityCount}");
Console.WriteLine($"Players: {world.CreateQuery<Player>().Count()}");
Console.WriteLine($"Enemies: {world.CreateQuery<Enemy>().Count()}");
Console.WriteLine($"Items: {world.CreateQuery<Item>().Count()}");
```

## References

- [ECS Architecture Guide](./ecs-architecture.md)
- [RFC-0007: Arch ECS Integration](../rfcs/0007-arch-ecs-integration.md)
- Component definitions: `console/src/plugins/WingedBean.Plugins.DungeonGame/Components/`

---

**Maintained by**: Winged Bean Team
