using System.IO;

public class DungeonGameService : IDungeonService, WingedBean.Contracts.Gameplay.ISERVICE, IWorldSnapshotProvider, IDisposable
{
    // ...

    public void Initialize()
    {
        // Resolve or create ArchECSService
        // NOTE: Due to manifest-based plugin loading issues, ArchECSService might not be registered
        // So we try to get it, and if that fails, we create and register it ourselves
        try
        {
            _ecs = _registry.Get<ArchECSService>();
        }
        catch (ServiceNotFoundException)
        {
            // ArchECSService not in registry - create and register it
            _ecs = new ArchECSService();
            _registry.Register<ArchECSService>(_ecs, priority: 100);
        }

        _ecs.ModeChanged += HandleModeChanged;

        _game.Initialize();
        _ecs.SetMode(GameMode.Play);
        _gameStateSubject.OnNext(GameState.Running);
        UpdateSnapshots();

        // Lightweight verification output to console to aid automated runs/recordings
        try
        {
            var bot = (Environment.GetEnvironmentVariable("ENABLE_BOT_MODE") == "1");
            var entityCount = _game.EntityCount;
            System.Console.WriteLine($"[Verify] Dungeon initialized; entities={entityCount}; bot={bot}");
            AppendVerifyLog($"DungeonInitialized entities={entityCount} bot={bot}");
        }
        catch { /* best-effort diagnostics */ }
    }

    // ...

    public void Dispose()
    {
        _ecs.ModeChanged -= HandleModeChanged;
    }

    private static void AppendVerifyLog(string line)
    {
        try
        {
            var overridePath = Environment.GetEnvironmentVariable("VERIFY_LOG_PATH");
            if (!string.IsNullOrWhiteSpace(overridePath))
            {
                var dir = Path.GetDirectoryName(overridePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(overridePath!, line + Environment.NewLine);
            }
        }
        catch { /* best-effort */ }
    }
}
