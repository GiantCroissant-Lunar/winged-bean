---
id: RFC-0014
title: Engine Profile Abstraction
status: Superseded
category: architecture, profiles
created: 2025-10-03
updated: 2025-10-03
author: WingedBean Team
superseded_by: RFC-0017
---

# RFC-0014: Engine Profile Abstraction

## Summary

Evaluate whether WingedBean should introduce an explicit **engine profile abstraction** (inspired by craft-sim's `Profiles.Unity` pattern) to standardise how Console, Unity, Godot, and future engines plug into the four-tier service platform.

## Motivation

- Contributors opening WingedBean expect to find a single concept that describes "what it means to be a Unity profile" versus a Console profile.
- craft-sim exposes an `IEngineProfile` interface that groups conventions, package manifests, build pipelines, and world hosts into a cohesive unit.
- Without a canonical abstraction WingedBean relies on documentation and directory structure to explain the relationship between Tier 3 adapters and Tier 4 providers.

## Goals

1. Document the problem space and craft-sim inspiration.
2. Outline a possible set of interfaces (`IEngineProfile`, `IEngineConventions`, `IPackageManager`).
3. Decide whether implementing these interfaces benefits the existing architecture.

## Non-Goals

- Replacing the Tier 3/4 separation that already models profile-specific behaviour.
- Forcing every profile to use the same build pipeline or packaging strategy.
- Changing plugin discovery (`.plugin.json`, `[Plugin]` attribute).

## Proposed Design (Original)

```csharp
public interface IEngineProfile
{
    string Name { get; }
    IEngineConventions Conventions { get; }
    IProfileBuildPipeline BuildPipeline { get; }
    IProfilePackageManager Packages { get; }
}

public interface IEngineConventions
{
    IReadOnlyDictionary<string, RelationKind> RelationKinds { get; }
    AssetNamingRules AssetRules { get; }
    BuildArtifactLayout ArtifactLayout { get; }
}

public interface IProfileBuildPipeline
{
    Task<RuntimeWorld> BuildAsync(AuthoringWorld authoringWorld, CancellationToken ct = default);
}

public interface IProfilePackageManager
{
    Task<IReadOnlyList<PluginDescriptor>> ResolveAsync(ProfileManifest manifest, CancellationToken ct = default);
}
```

- Profiles are registered with a `IProfileRegistry` service.
- Every host chooses a profile (`console`, `unity`, `godot`) at boot.
- Build pipelines translate authoring data into runtime worlds.

## Evaluation

During the 2025-10-02 architecture review (`docs/design/multi-mode-multi-world-summary.md`) we re-evaluated this proposal against the four-tier service platform and concluded:

- The platform already has a clear separation of concerns: Tier 3 adapters and Tier 4 providers encode engine-specific details.
- Introducing `IEngineProfile` would duplicate existing responsibilities and add a parallel registry to maintain.
- Multi-world support and in-game editor modes are better expressed as extensions to `IECSService` and gameplay services rather than a cross-cutting profile abstraction.

## Decision

This RFC is **superseded** by RFC-0017 (Reactive Plugin Architecture for Dungeon Game) and the multi-world design notes. Instead of adding a new profile layer we will:

1. Extend `IECSService` with multi-world and mode APIs.
2. Keep Tier 3/4 boundaries as the canonical location for profile-specific code.
3. Document per-profile conventions in lightweight guides (e.g. `docs/unity/README.md`).

## Impact

- No new interfaces are introduced.
- Documentation will continue to emphasise the four-tier structure and how profile-specific code lives in adapters/providers.
- Future engine integrations (Unity, Godot) remain free to supply additional plugins without conforming to a large interface hierarchy.

## Alternatives Considered

| Option | Outcome |
| --- | --- |
| Implement `IEngineProfile` | Adds parallel abstraction, increases complexity, duplicates Tier 3/4 responsibilities. |
| Use JSON manifests only | Already covered by `.plugin.json`; does not address gameplay/editor pipeline questions. |
| Documentation-only (chosen) | Keeps architecture lean, relies on guides + RFC-0017 for reactive gameplay separation. |

## Follow-up Tasks

- Update docs to point engine-specific contributors to the correct Tier 3/4 packages.
- Capture Unity/Godot conventions in `docs/unity/` and future `docs/godot/` guides.
- Ensure `IECSService` roadmap tracks multi-world and mode features.
