# WingedBean.Contracts.ECS

**Tier 1 Contract** - Entity Component System (ECS) abstraction layer

## Overview

This project provides platform-agnostic interfaces for Entity Component System (ECS) functionality in the WingedBean framework. It abstracts ECS operations to allow different implementations (Arch ECS, DefaultEcs, etc.) to be used interchangeably.

## Target Framework

- **netstandard2.1** - Compatible with Unity 2021+, Godot C#, and all .NET platforms

## Purpose

The ECS contracts enable:
- High-performance entity management for game logic
- Platform-agnostic game entity abstraction
- Pluggable ECS implementations via the WingedBean service architecture
- Separation of game logic from specific ECS library implementations

## Related Projects

- **WingedBean.Plugins.ArchECS** - Concrete implementation using Arch ECS library (Tier 3)
- **WingedBean.Contracts.Core** - Core service contracts

## RFC Reference

This project implements **RFC-0007: Arch ECS Integration for Dungeon Crawler Gameplay**.

See:
- `docs/rfcs/0007-arch-ecs-integration.md`
- `docs/implementation/rfc-0007-execution-plan.md`

## Tier 1 Guidelines

As a Tier 1 contract project:
- ✅ Only interfaces, enums, and data classes
- ✅ No implementation logic
- ✅ No platform-specific APIs
- ✅ No external dependencies (except PolySharp for polyfills)
- ❌ No System.Text.Json
- ❌ No IAsyncEnumerable<T>
- ❌ No Span<T> in public APIs
