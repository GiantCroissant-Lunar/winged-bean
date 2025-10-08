using System;
using System.Collections.Generic;
using System.Linq;
using Plate.PluginManoi.Contracts;
using Plate.CrossMilo.Contracts.ECS;
using Plate.CrossMilo.Contracts.ECS.Services;

namespace WingedBean.Plugins.ArchECS;

/// <summary>
/// Arch-based implementation of <see cref="IService"/>.
/// Provides high-performance Entity Component System functionality using Arch ECS library.
/// </summary>
[Plugin(
    Name = "Arch.ECS",
    Provides = new[] { typeof(IService) },
    Priority = 100
)]
public class ArchECSService : IService
{
    private readonly Dictionary<int, WorldMetadata> _worlds = new();
    private readonly Dictionary<AuthoringNodeId, AuthoringMapping> _authoringMappings = new();

    private int _nextWorldId;
    private WorldHandle _authoringHandle = WorldHandle.Invalid;
    private WorldHandle _defaultRuntimeHandle = WorldHandle.Invalid;
    private GameMode _currentMode = GameMode.Play;

    public event EventHandler<GameMode>? ModeChanged;

    public IWorld CreateWorld()
    {
        var metadata = CreateWorldInternal(WorldKind.Runtime, null);
        EnsureDefaultRuntime(metadata.Handle);
        return metadata.World;
    }

    public WorldHandle AuthoringWorld => EnsureAuthoringWorld();

    public WorldHandle DefaultRuntimeWorld
    {
        get
        {
            if (!_defaultRuntimeHandle.IsValid)
            {
                var existing = GetRuntimeWorlds().FirstOrDefault();
                _defaultRuntimeHandle = existing.IsValid && existing.Kind == WorldKind.Runtime
                    ? existing
                    : CreateRuntimeWorld("runtime-0");
            }

            return _defaultRuntimeHandle;
        }
    }

    public WorldHandle CreateRuntimeWorld(string name)
    {
        var metadata = CreateWorldInternal(WorldKind.Runtime, name);
        EnsureDefaultRuntime(metadata.Handle);
        return metadata.Handle;
    }

    public void DestroyWorld(IWorld world)
    {
        if (world is not ArchWorld archWorld)
        {
            throw new ArgumentException("World is not an ArchWorld instance", nameof(world));
        }

        var entry = _worlds.FirstOrDefault(kv => ReferenceEquals(kv.Value.World, archWorld));
        if (entry.Value is null)
        {
            return;
        }

        DestroyWorld(entry.Value.Handle);
    }

    public bool DestroyWorld(WorldHandle handle)
    {
        if (!_worlds.TryGetValue(handle.Id, out var metadata))
        {
            return false;
        }

        if (metadata.Handle.Kind == WorldKind.Authoring)
        {
            throw new InvalidOperationException("Authoring world cannot be destroyed.");
        }

        _worlds.Remove(handle.Id);
        ClearAuthoringMappingsFor(handle);

        if (_defaultRuntimeHandle == handle)
        {
            _defaultRuntimeHandle = WorldHandle.Invalid;
        }

        return true;
    }

    public IWorld? GetWorld(int worldId)
    {
        return _worlds.TryGetValue(worldId, out var metadata) ? metadata.World : null;
    }

    public IWorld? GetWorld(WorldHandle handle)
    {
        if (handle.Kind == WorldKind.Authoring && !_authoringHandle.IsValid)
        {
            EnsureAuthoringWorld();
        }

        return _worlds.TryGetValue(handle.Id, out var metadata) ? metadata.World : null;
    }

    public IEnumerable<WorldHandle> GetRuntimeWorlds()
    {
        return _worlds.Values
            .Where(metadata => metadata.Handle.Kind == WorldKind.Runtime)
            .Select(metadata => metadata.Handle);
    }

    public GameMode CurrentMode => _currentMode;

    public void SetMode(GameMode mode)
    {
        if (_currentMode == mode)
        {
            return;
        }

        _currentMode = mode;
        ModeChanged?.Invoke(this, mode);
    }

    public void MapAuthoringToRuntime(AuthoringNodeId authoringId, WorldHandle runtimeWorld, EntityHandle runtimeEntity)
    {
        if (!_worlds.TryGetValue(runtimeWorld.Id, out var metadata) || metadata.Handle.Kind != WorldKind.Runtime)
        {
            throw new InvalidOperationException($"Runtime world '{runtimeWorld}' does not exist.");
        }

        _authoringMappings[authoringId] = new AuthoringMapping(runtimeWorld, runtimeEntity);
    }

    public EntityHandle? GetRuntimeEntity(AuthoringNodeId authoringId)
    {
        return _authoringMappings.TryGetValue(authoringId, out var mapping) ? mapping.Entity : null;
    }

    private WorldMetadata CreateWorldInternal(WorldKind kind, string? name)
    {
        var archWorld = new ArchWorld();
        var id = _nextWorldId++;
        var resolvedName = string.IsNullOrWhiteSpace(name) ? $"{kind.ToString().ToLowerInvariant()}-{id}" : name;
        var handle = new WorldHandle(id, kind);
        var metadata = new WorldMetadata(handle, resolvedName, archWorld);
        _worlds[id] = metadata;
        return metadata;
    }

    private WorldHandle EnsureAuthoringWorld()
    {
        if (_authoringHandle.IsValid)
        {
            return _authoringHandle;
        }

        var metadata = CreateWorldInternal(WorldKind.Authoring, "authoring");
        _authoringHandle = metadata.Handle;
        return _authoringHandle;
    }

    private void EnsureDefaultRuntime(WorldHandle handle)
    {
        if (!_defaultRuntimeHandle.IsValid && handle.Kind == WorldKind.Runtime)
        {
            _defaultRuntimeHandle = handle;
        }
    }

    private void ClearAuthoringMappingsFor(WorldHandle runtimeWorld)
    {
        var stale = _authoringMappings
            .Where(pair => pair.Value.World == runtimeWorld)
            .Select(pair => pair.Key)
            .ToList();

        foreach (var key in stale)
        {
            _authoringMappings.Remove(key);
        }
    }

    private sealed record WorldMetadata(WorldHandle Handle, string Name, ArchWorld World);

    private readonly record struct AuthoringMapping(WorldHandle World, EntityHandle Entity);
}
