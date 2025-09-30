using WingedBean.Contracts.Core;

namespace WingedBean.Registry;

/// <summary>
/// Thread-safe implementation of IRegistry for managing service implementations and selection strategies.
/// </summary>
public class ActualRegistry : IRegistry
{
    private readonly Dictionary<Type, List<ServiceEntry>> _services = new();
    private readonly object _lock = new();

    private class ServiceEntry
    {
        public object Implementation { get; }
        public ServiceMetadata Metadata { get; }

        public ServiceEntry(object implementation, ServiceMetadata metadata)
        {
            Implementation = implementation;
            Metadata = metadata;
        }
    }

    /// <inheritdoc />
    public void Register<TService>(TService implementation, int priority = 0)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(implementation);

        var metadata = new ServiceMetadata
        {
            Priority = priority
        };

        Register(implementation, metadata);
    }

    /// <inheritdoc />
    public void Register<TService>(TService implementation, ServiceMetadata metadata)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(implementation);
        ArgumentNullException.ThrowIfNull(metadata);

        lock (_lock)
        {
            var serviceType = typeof(TService);
            if (!_services.ContainsKey(serviceType))
            {
                _services[serviceType] = new List<ServiceEntry>();
            }

            _services[serviceType].Add(new ServiceEntry(implementation, metadata));
        }
    }

    /// <inheritdoc />
    public TService Get<TService>(SelectionMode mode = SelectionMode.HighestPriority)
        where TService : class
    {
        lock (_lock)
        {
            var serviceType = typeof(TService);
            
            if (!_services.TryGetValue(serviceType, out var entries) || entries.Count == 0)
            {
                throw new ServiceNotFoundException(serviceType);
            }

            return mode switch
            {
                SelectionMode.One => GetOne<TService>(entries, serviceType),
                SelectionMode.HighestPriority => GetHighestPriority<TService>(entries),
                SelectionMode.All => throw new InvalidOperationException(
                    $"Use GetAll<{serviceType.Name}>() for SelectionMode.All"),
                _ => throw new ArgumentException($"Unknown selection mode: {mode}", nameof(mode))
            };
        }
    }

    /// <inheritdoc />
    public IEnumerable<TService> GetAll<TService>()
        where TService : class
    {
        lock (_lock)
        {
            var serviceType = typeof(TService);
            
            if (!_services.TryGetValue(serviceType, out var entries))
            {
                return Enumerable.Empty<TService>();
            }

            return entries.Select(e => (TService)e.Implementation).ToList();
        }
    }

    /// <inheritdoc />
    public bool IsRegistered<TService>()
        where TService : class
    {
        lock (_lock)
        {
            var serviceType = typeof(TService);
            return _services.TryGetValue(serviceType, out var entries) && entries.Count > 0;
        }
    }

    /// <inheritdoc />
    public bool Unregister<TService>(TService implementation)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(implementation);

        lock (_lock)
        {
            var serviceType = typeof(TService);
            
            if (!_services.TryGetValue(serviceType, out var entries))
            {
                return false;
            }

            var removed = entries.RemoveAll(e => ReferenceEquals(e.Implementation, implementation));
            
            if (entries.Count == 0)
            {
                _services.Remove(serviceType);
            }

            return removed > 0;
        }
    }

    /// <inheritdoc />
    public void UnregisterAll<TService>()
        where TService : class
    {
        lock (_lock)
        {
            var serviceType = typeof(TService);
            _services.Remove(serviceType);
        }
    }

    /// <inheritdoc />
    public ServiceMetadata? GetMetadata<TService>(TService implementation)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(implementation);

        lock (_lock)
        {
            var serviceType = typeof(TService);
            
            if (!_services.TryGetValue(serviceType, out var entries))
            {
                return null;
            }

            var entry = entries.FirstOrDefault(e => ReferenceEquals(e.Implementation, implementation));
            return entry?.Metadata;
        }
    }

    private static TService GetOne<TService>(List<ServiceEntry> entries, Type serviceType)
        where TService : class
    {
        if (entries.Count > 1)
        {
            throw new MultipleServicesException(serviceType, entries.Count);
        }

        return (TService)entries[0].Implementation;
    }

    private static TService GetHighestPriority<TService>(List<ServiceEntry> entries)
        where TService : class
    {
        var highest = entries.OrderByDescending(e => e.Metadata.Priority).First();
        return (TService)highest.Implementation;
    }
}
