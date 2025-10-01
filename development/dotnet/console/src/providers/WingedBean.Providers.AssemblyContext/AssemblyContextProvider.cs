using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;

namespace WingedBean.Providers.AssemblyContext;

/// <summary>
/// Tier 4 provider for .NET AssemblyLoadContext-based assembly loading.
/// Wraps AssemblyLoadContext functionality for plugin hot-reload support.
/// Thread-safe implementation for concurrent operations.
/// </summary>
public class AssemblyContextProvider : IDisposable
{
    private readonly Dictionary<string, AssemblyLoadContext> _contexts = new();
    private readonly Dictionary<string, Assembly> _loadedAssemblies = new();
    private readonly ILogger<AssemblyContextProvider>? _logger;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Initialize the AssemblyContextProvider.
    /// </summary>
    public AssemblyContextProvider()
    {
    }

    /// <summary>
    /// Initialize the AssemblyContextProvider with logger.
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public AssemblyContextProvider(ILogger<AssemblyContextProvider> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Create a new isolated AssemblyLoadContext.
    /// </summary>
    /// <param name="contextName">Unique name for the context</param>
    /// <param name="isCollectible">Whether the context should be collectible (for hot-reload)</param>
    /// <returns>Created context identifier</returns>
    public string CreateContext(string contextName, bool isCollectible = true)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(contextName);

        lock (_lock)
        {
            if (_contexts.ContainsKey(contextName))
            {
                throw new InvalidOperationException($"Context '{contextName}' already exists");
            }

            _logger?.LogDebug("Creating AssemblyLoadContext: {ContextName}, Collectible: {IsCollectible}",
                contextName, isCollectible);

            var alc = new AssemblyLoadContext(contextName, isCollectible);
            _contexts[contextName] = alc;

            _logger?.LogInformation("Created AssemblyLoadContext: {ContextName}", contextName);
            return contextName;
        }
    }

    /// <summary>
    /// Load an assembly from a file path into a specific context.
    /// </summary>
    /// <param name="contextName">Context identifier</param>
    /// <param name="assemblyPath">Path to the assembly file</param>
    /// <returns>Loaded assembly</returns>
    public Assembly LoadAssembly(string contextName, string assemblyPath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(contextName);
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);

        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException($"Assembly not found: {assemblyPath}");
        }

        lock (_lock)
        {
            if (!_contexts.TryGetValue(contextName, out var alc))
            {
                throw new InvalidOperationException($"Context '{contextName}' not found");
            }

            try
            {
                _logger?.LogDebug("Loading assembly: {AssemblyPath} into context: {ContextName}",
                    assemblyPath, contextName);

                var fullPath = Path.GetFullPath(assemblyPath);
                var assembly = alc.LoadFromAssemblyPath(fullPath);

                var assemblyKey = $"{contextName}:{assembly.FullName}";
                _loadedAssemblies[assemblyKey] = assembly;

                _logger?.LogInformation("Loaded assembly: {AssemblyName} from {AssemblyPath}",
                    assembly.FullName, assemblyPath);

                return assembly;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load assembly: {AssemblyPath} into context: {ContextName}",
                    assemblyPath, contextName);
                throw;
            }
        }
    }

    /// <summary>
    /// Get an existing AssemblyLoadContext by name.
    /// </summary>
    /// <param name="contextName">Context identifier</param>
    /// <returns>The AssemblyLoadContext, or null if not found</returns>
    public AssemblyLoadContext? GetContext(string contextName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(contextName);

        lock (_lock)
        {
            return _contexts.TryGetValue(contextName, out var alc) ? alc : null;
        }
    }

    /// <summary>
    /// Unload a context and all its assemblies.
    /// </summary>
    /// <param name="contextName">Context identifier</param>
    /// <param name="waitForUnload">Whether to wait for unload to complete</param>
    /// <returns>Task that completes when unload is finished (if waitForUnload is true)</returns>
    public async Task UnloadContextAsync(string contextName, bool waitForUnload = true)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(contextName);

        AssemblyLoadContext? alc;

        lock (_lock)
        {
            if (!_contexts.TryGetValue(contextName, out alc))
            {
                _logger?.LogWarning("Context '{ContextName}' not found for unload", contextName);
                return;
            }

            _logger?.LogDebug("Unloading context: {ContextName}", contextName);

            // Remove from tracking
            _contexts.Remove(contextName);

            // Remove all assemblies loaded in this context
            var keysToRemove = _loadedAssemblies.Keys
                .Where(k => k.StartsWith($"{contextName}:"))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _loadedAssemblies.Remove(key);
            }
        }

        try
        {
            // Unload the context
            alc.Unload();

            _logger?.LogInformation("Initiated unload for context: {ContextName}", contextName);

            if (waitForUnload)
            {
                // Give GC time to clean up
                await Task.Run(() =>
                {
                    for (int i = 0; i < 10 && alc.IsCollectible; i++)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        Thread.Sleep(100);
                    }
                });

                _logger?.LogInformation("Context unloaded: {ContextName}", contextName);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error unloading context: {ContextName}", contextName);
            throw;
        }
    }

    /// <summary>
    /// Get all loaded contexts.
    /// </summary>
    /// <returns>Collection of context names</returns>
    public IEnumerable<string> GetLoadedContexts()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            return _contexts.Keys.ToList();
        }
    }

    /// <summary>
    /// Check if a context exists.
    /// </summary>
    /// <param name="contextName">Context identifier</param>
    /// <returns>True if the context exists, false otherwise</returns>
    public bool ContextExists(string contextName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(contextName);

        lock (_lock)
        {
            return _contexts.ContainsKey(contextName);
        }
    }

    /// <summary>
    /// Dispose of all contexts and resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_lock)
        {
            _logger?.LogDebug("Disposing AssemblyContextProvider with {Count} contexts", _contexts.Count);

            var contextNames = _contexts.Keys.ToList();
            foreach (var contextName in contextNames)
            {
                try
                {
                    UnloadContextAsync(contextName, waitForUnload: false).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error disposing context: {ContextName}", contextName);
                }
            }

            _contexts.Clear();
            _loadedAssemblies.Clear();
            _disposed = true;

            _logger?.LogInformation("AssemblyContextProvider disposed");
        }
    }
}
