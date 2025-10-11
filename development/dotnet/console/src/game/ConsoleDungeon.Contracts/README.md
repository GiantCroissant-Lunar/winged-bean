# ConsoleDungeon.Contracts

**Game-Specific Contracts** for the Console Dungeon crawler game.

## Overview

This project contains **game-specific contracts** for the Console Dungeon game. These contracts define the dungeon crawler's game logic interface, types, and events.

**Important:** This is NOT a framework-level contract. This is specific to the Console Dungeon game.

## Purpose

- Define game-specific service interfaces (`IDungeonService`)
- Define game-specific types (`GameInput`, `GameState`, `PlayerStats`, etc.)
- Define game-specific events (`EntityMovedEvent`, `CombatEvent`, etc.)
- Provide a clean contract for UI plugins to consume game state

## Key Contracts

### IDungeonService

Main game logic service that:
- Manages game state (NotStarted, Running, Paused, GameOver, Victory)
- Handles player input (Move, Attack, UseItem, etc.)
- Exposes reactive observables for UI updates
- Provides snapshots of entities and player stats

### Types

- **GameInput** - Input commands (MoveUp, Attack, UseItem, etc.)
- **GameState** - Overall game state
- **EntitySnapshot** - Immutable entity data for rendering
- **PlayerStats** - Player statistics (HP, Mana, Level, etc.)
- **Position** - 3D position in the dungeon

### Events

- **EntityMovedEvent** - Entity moved to new position
- **CombatEvent** - Combat occurred between entities
- **GameStateChangedEvent** - Game state changed
- **EntitySpawnedEvent** - New entity spawned
- **EntityDiedEvent** - Entity died
- **ItemCollectedEvent** - Item collected
- **LevelUpEvent** - Entity leveled up

## Implementation

The implementation of these contracts is in:
- **WingedBean.Plugins.DungeonGame** - Game logic implementation using Arch ECS

## Why Game-Specific Contracts?

**Framework contracts** (CrossMilo.Contracts.*) provide generic infrastructure:
- Input, Audio, Scene, UI, Hosting, etc.

**Game-specific contracts** (ConsoleDungeon.Contracts) provide game logic:
- Dungeon crawler specific types and behavior

This separation allows:
- ✅ CrossMilo to be reused for ANY game (platformer, racing, puzzle, etc.)
- ✅ Game logic to be specific and clear
- ✅ No leaky abstractions (ECS details stay hidden)
- ✅ Clean separation of concerns

## Related Projects

- **WingedBean.Plugins.DungeonGame** - Game logic implementation
- **WingedBean.Plugins.ConsoleDungeon** - UI integration
- **CrossMilo.Contracts.*** - Framework-level contracts

## References

- **Architecture Document:** `/plate-projects/cross-milo/docs/CONTRACT_SCOPE_ANALYSIS.md`
- **RFC-0007:** Arch ECS Integration for Dungeon Crawler Gameplay
- **RFC-0018:** Render and UI Services for Console Profile
