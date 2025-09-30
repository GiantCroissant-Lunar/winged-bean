using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace WingedBean.Host;

/// <summary>
/// Plugin update manager for handling hot updates and rollbacks
/// </summary>
public interface IPluginUpdateManager
{
    /// <summary>Check for available updates for a plugin</summary>
    Task<PluginUpdateInfo?> CheckForUpdatesAsync(string pluginId, CancellationToken ct = default);

    /// <summary>Update a plugin to a new version</summary>
    Task<bool> UpdatePluginAsync(string pluginId, string targetVersion, CancellationToken ct = default);

    /// <summary>Rollback a plugin to previous version</summary>
    Task<bool> RollbackPluginAsync(string pluginId, CancellationToken ct = default);

    /// <summary>Get update history for a plugin</summary>
    Task<IEnumerable<PluginUpdateRecord>> GetUpdateHistoryAsync(string pluginId, CancellationToken ct = default);

    /// <summary>Enable/disable automatic updates for a plugin</summary>
    Task SetAutoUpdateAsync(string pluginId, bool enabled, CancellationToken ct = default);

    /// <summary>Event triggered when plugin update is available</summary>
    event EventHandler<PluginUpdateAvailableEventArgs> UpdateAvailable;

    /// <summary>Event triggered when plugin update starts</summary>
    event EventHandler<PluginUpdateEventArgs> UpdateStarted;

    /// <summary>Event triggered when plugin update completes</summary>
    event EventHandler<PluginUpdateEventArgs> UpdateCompleted;

    /// <summary>Event triggered when plugin update fails</summary>
    event EventHandler<PluginUpdateErrorEventArgs> UpdateFailed;
}

/// <summary>
/// Plugin update record for tracking update history
/// </summary>
public class PluginUpdateRecord
{
    /// <summary>Plugin ID</summary>
    public string PluginId { get; set; } = string.Empty;

    /// <summary>Previous version</summary>
    public string FromVersion { get; set; } = string.Empty;

    /// <summary>New version</summary>
    public string ToVersion { get; set; } = string.Empty;

    /// <summary>Update timestamp</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Update success status</summary>
    public bool Success { get; set; }

    /// <summary>Error message if update failed</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Update duration</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>Update type</summary>
    public PluginUpdateType UpdateType { get; set; }

    /// <summary>Rollback information</summary>
    public string? RollbackPath { get; set; }
}

/// <summary>
/// Type of plugin update
/// </summary>
public enum PluginUpdateType
{
    Manual,
    Automatic,
    Rollback,
    HotFix
}

/// <summary>
/// Event arguments for plugin update availability
/// </summary>
public class PluginUpdateAvailableEventArgs : EventArgs
{
    public string PluginId { get; set; } = string.Empty;
    public string CurrentVersion { get; set; } = string.Empty;
    public string AvailableVersion { get; set; } = string.Empty;
    public bool IsAutoUpdateEnabled { get; set; }
    public PluginUpdateInfo UpdateInfo { get; set; } = new();
}

/// <summary>
/// Event arguments for plugin update operations
/// </summary>
public class PluginUpdateEventArgs : EventArgs
{
    public string PluginId { get; set; } = string.Empty;
    public string FromVersion { get; set; } = string.Empty;
    public string ToVersion { get; set; } = string.Empty;
    public PluginUpdateType UpdateType { get; set; }
}

/// <summary>
/// Event arguments for plugin update errors
/// </summary>
public class PluginUpdateErrorEventArgs : PluginUpdateEventArgs
{
    public Exception Exception { get; set; } = new();
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Plugin update manager implementation
/// </summary>
public class PluginUpdateManager : IPluginUpdateManager
{
    private readonly IPluginLoader _pluginLoader;
    private readonly IPluginRegistry _registry;
    private readonly IPluginSignatureVerifier _signatureVerifier;
    private readonly ILogger<PluginUpdateManager>? _logger;
    private readonly Dictionary<string, List<PluginUpdateRecord>> _updateHistory = new();
    private readonly Dictionary<string, bool> _autoUpdateSettings = new();
    private readonly SemaphoreSlim _updateSemaphore = new(1, 1);

    public event EventHandler<PluginUpdateAvailableEventArgs>? UpdateAvailable;
    public event EventHandler<PluginUpdateEventArgs>? UpdateStarted;
    public event EventHandler<PluginUpdateEventArgs>? UpdateCompleted;
    public event EventHandler<PluginUpdateErrorEventArgs>? UpdateFailed;

    public PluginUpdateManager(
        IPluginLoader pluginLoader,
        IPluginRegistry registry,
        IPluginSignatureVerifier signatureVerifier,
        ILogger<PluginUpdateManager>? logger = null)
    {
        _pluginLoader = pluginLoader;
        _registry = registry;
        _signatureVerifier = signatureVerifier;
        _logger = logger;
    }

    public async Task<PluginUpdateInfo?> CheckForUpdatesAsync(string pluginId, CancellationToken ct = default)
    {
        try
        {
            var currentPlugin = await _registry.GetPluginAsync(pluginId, ct: ct);
            if (currentPlugin == null || currentPlugin.UpdateInfo?.UpdateUrl == null)
                return null;

            // In a real implementation, this would check the update server
            // For now, we'll check if there are newer versions in the registry
            var versions = await _registry.GetPluginVersionsAsync(pluginId, ct);
            var newerVersions = versions
                .Where(v => v.SemanticVersion > currentPlugin.SemanticVersion)
                .OrderByDescending(v => v.SemanticVersion)
                .ToList();

            if (!newerVersions.Any())
                return null;

            var latestVersion = newerVersions.First();
            var updateInfo = new PluginUpdateInfo
            {
                Channel = currentPlugin.UpdateInfo.Channel,
                AutoUpdate = currentPlugin.UpdateInfo.AutoUpdate,
                UpdateUrl = currentPlugin.UpdateInfo.UpdateUrl,
                Rollback = currentPlugin.UpdateInfo.Rollback
            };

            // Trigger update available event
            var eventArgs = new PluginUpdateAvailableEventArgs
            {
                PluginId = pluginId,
                CurrentVersion = currentPlugin.Version,
                AvailableVersion = latestVersion.Version,
                IsAutoUpdateEnabled = _autoUpdateSettings.GetValueOrDefault(pluginId, updateInfo.AutoUpdate),
                UpdateInfo = updateInfo
            };

            UpdateAvailable?.Invoke(this, eventArgs);

            return updateInfo;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to check for updates for plugin {PluginId}", pluginId);
            return null;
        }
    }

    public async Task<bool> UpdatePluginAsync(string pluginId, string targetVersion, CancellationToken ct = default)
    {
        await _updateSemaphore.WaitAsync(ct);
        try
        {
            var currentPlugin = await _registry.GetPluginAsync(pluginId, ct: ct);
            if (currentPlugin == null)
            {
                _logger?.LogError("Plugin {PluginId} not found for update", pluginId);
                return false;
            }

            var targetPlugin = await _registry.GetPluginAsync(pluginId, targetVersion, ct);
            if (targetPlugin == null)
            {
                _logger?.LogError("Target version {Version} not found for plugin {PluginId}", targetVersion, pluginId);
                return false;
            }

            var updateEventArgs = new PluginUpdateEventArgs
            {
                PluginId = pluginId,
                FromVersion = currentPlugin.Version,
                ToVersion = targetVersion,
                UpdateType = PluginUpdateType.Manual
            };

            UpdateStarted?.Invoke(this, updateEventArgs);

            var startTime = DateTimeOffset.UtcNow;
            var success = false;
            string? errorMessage = null;
            string? rollbackPath = null;

            try
            {
                // Create rollback point
                rollbackPath = await CreateRollbackPointAsync(currentPlugin, ct);

                // Validate target plugin signature
                if (targetPlugin.Security?.Signature != null)
                {
                    var pluginPath = GetPluginPath(targetPlugin);
                    if (!await _signatureVerifier.VerifySignatureAsync(targetPlugin, pluginPath, ct))
                    {
                        throw new InvalidOperationException("Plugin signature verification failed");
                    }
                }

                // Perform hot update
                success = await PerformHotUpdateAsync(currentPlugin, targetPlugin, ct);

                if (success)
                {
                    // Record successful update
                    RecordUpdate(pluginId, currentPlugin.Version, targetVersion, true, null,
                        DateTimeOffset.UtcNow - startTime, PluginUpdateType.Manual, rollbackPath);

                    UpdateCompleted?.Invoke(this, updateEventArgs);
                    _logger?.LogInformation("Successfully updated plugin {PluginId} from {FromVersion} to {ToVersion}",
                        pluginId, currentPlugin.Version, targetVersion);
                }
                else
                {
                    throw new InvalidOperationException("Hot update failed");
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                _logger?.LogError(ex, "Failed to update plugin {PluginId} from {FromVersion} to {ToVersion}",
                    pluginId, currentPlugin.Version, targetVersion);

                // Attempt rollback if rollback point was created
                if (!string.IsNullOrEmpty(rollbackPath))
                {
                    try
                    {
                        await PerformRollbackAsync(pluginId, rollbackPath, ct);
                        _logger?.LogInformation("Rolled back plugin {PluginId} after failed update", pluginId);
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger?.LogError(rollbackEx, "Failed to rollback plugin {PluginId} after failed update", pluginId);
                    }
                }

                var errorEventArgs = new PluginUpdateErrorEventArgs
                {
                    PluginId = pluginId,
                    FromVersion = currentPlugin.Version,
                    ToVersion = targetVersion,
                    UpdateType = PluginUpdateType.Manual,
                    Exception = ex,
                    ErrorMessage = ex.Message
                };

                UpdateFailed?.Invoke(this, errorEventArgs);
            }

            // Record update attempt
            if (!success)
            {
                RecordUpdate(pluginId, currentPlugin.Version, targetVersion, false, errorMessage,
                    DateTimeOffset.UtcNow - startTime, PluginUpdateType.Manual, rollbackPath);
            }

            return success;
        }
        finally
        {
            _updateSemaphore.Release();
        }
    }

    public async Task<bool> RollbackPluginAsync(string pluginId, CancellationToken ct = default)
    {
        await _updateSemaphore.WaitAsync(ct);
        try
        {
            var history = await GetUpdateHistoryAsync(pluginId, ct);
            var lastSuccessfulUpdate = history
                .Where(h => h.Success && !string.IsNullOrEmpty(h.RollbackPath))
                .OrderByDescending(h => h.UpdatedAt)
                .FirstOrDefault();

            if (lastSuccessfulUpdate == null)
            {
                _logger?.LogWarning("No rollback point found for plugin {PluginId}", pluginId);
                return false;
            }

            return await PerformRollbackAsync(pluginId, lastSuccessfulUpdate.RollbackPath!, ct);
        }
        finally
        {
            _updateSemaphore.Release();
        }
    }

    public async Task<IEnumerable<PluginUpdateRecord>> GetUpdateHistoryAsync(string pluginId, CancellationToken ct = default)
    {
        return _updateHistory.TryGetValue(pluginId, out var history)
            ? history.OrderByDescending(h => h.UpdatedAt)
            : Enumerable.Empty<PluginUpdateRecord>();
    }

    public async Task SetAutoUpdateAsync(string pluginId, bool enabled, CancellationToken ct = default)
    {
        _autoUpdateSettings[pluginId] = enabled;
        _logger?.LogInformation("Set auto-update for plugin {PluginId} to {Enabled}", pluginId, enabled);
    }

    private async Task<string> CreateRollbackPointAsync(PluginManifest plugin, CancellationToken ct)
    {
        var rollbackDir = Path.Combine(Path.GetTempPath(), "winged-bean-rollback", plugin.Id, plugin.Version);
        Directory.CreateDirectory(rollbackDir);

        var pluginPath = GetPluginPath(plugin);
        var rollbackManifestPath = Path.Combine(rollbackDir, "plugin.json");

        // Save current plugin manifest
        var manifestJson = System.Text.Json.JsonSerializer.Serialize(plugin, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(rollbackManifestPath, manifestJson, ct);

        // Copy plugin files
        if (Directory.Exists(pluginPath))
        {
            await CopyDirectoryAsync(pluginPath, Path.Combine(rollbackDir, "files"), ct);
        }

        return rollbackDir;
    }

    private async Task<bool> PerformHotUpdateAsync(PluginManifest currentPlugin, PluginManifest targetPlugin, CancellationToken ct)
    {
        // In a real implementation, this would:
        // 1. Download new plugin files
        // 2. Unload current plugin using plugin loader
        // 3. Replace plugin files
        // 4. Load new plugin version

        // For now, we'll simulate the process
        await Task.Delay(1000, ct); // Simulate update process

        // Update registry with new version
        await _registry.UpdatePluginAsync(targetPlugin, ct);

        return true;
    }

    private async Task<bool> PerformRollbackAsync(string pluginId, string rollbackPath, CancellationToken ct)
    {
        try
        {
            var rollbackManifestPath = Path.Combine(rollbackPath, "plugin.json");
            if (!File.Exists(rollbackManifestPath))
                return false;

            var manifestJson = await File.ReadAllTextAsync(rollbackManifestPath, ct);
            var rollbackPlugin = System.Text.Json.JsonSerializer.Deserialize<PluginManifest>(manifestJson);

            if (rollbackPlugin == null)
                return false;

            // Restore plugin files
            var pluginPath = GetPluginPath(rollbackPlugin);
            var rollbackFilesPath = Path.Combine(rollbackPath, "files");

            if (Directory.Exists(rollbackFilesPath))
            {
                if (Directory.Exists(pluginPath))
                    Directory.Delete(pluginPath, true);

                await CopyDirectoryAsync(rollbackFilesPath, pluginPath, ct);
            }

            // Update registry
            await _registry.UpdatePluginAsync(rollbackPlugin, ct);

            // Record rollback
            RecordUpdate(pluginId, "unknown", rollbackPlugin.Version, true, null,
                TimeSpan.FromSeconds(1), PluginUpdateType.Rollback, null);

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to perform rollback for plugin {PluginId}", pluginId);
            return false;
        }
    }

    private void RecordUpdate(string pluginId, string fromVersion, string toVersion, bool success,
        string? errorMessage, TimeSpan duration, PluginUpdateType updateType, string? rollbackPath)
    {
        if (!_updateHistory.ContainsKey(pluginId))
            _updateHistory[pluginId] = new List<PluginUpdateRecord>();

        var record = new PluginUpdateRecord
        {
            PluginId = pluginId,
            FromVersion = fromVersion,
            ToVersion = toVersion,
            UpdatedAt = DateTimeOffset.UtcNow,
            Success = success,
            ErrorMessage = errorMessage,
            Duration = duration,
            UpdateType = updateType,
            RollbackPath = rollbackPath
        };

        _updateHistory[pluginId].Add(record);

        // Keep only recent records to prevent memory growth
        if (_updateHistory[pluginId].Count > 50)
        {
            _updateHistory[pluginId] = _updateHistory[pluginId]
                .OrderByDescending(r => r.UpdatedAt)
                .Take(50)
                .ToList();
        }
    }

    private string GetPluginPath(PluginManifest plugin)
    {
        // In a real implementation, this would resolve the actual plugin path
        return Path.Combine("plugins", plugin.Id, plugin.Version);
    }

    private async Task CopyDirectoryAsync(string sourceDir, string destDir, CancellationToken ct)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();

            var relativePath = Path.GetRelativePath(sourceDir, file);
            var destFile = Path.Combine(destDir, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
            using var sourceStream = File.OpenRead(file);
            using var destStream = File.Create(destFile);
            await sourceStream.CopyToAsync(destStream, ct);
        }
    }
}
