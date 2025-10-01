using System;

namespace WingedBean.Contracts.Core;

/// <summary>
/// Exception thrown when a requested service is not found in the registry.
/// </summary>
public class ServiceNotFoundException : Exception
{
    public Type ServiceType { get; }

    public ServiceNotFoundException(Type serviceType)
        : base($"Service {serviceType.Name} not found in registry")
    {
        ServiceType = serviceType;
    }
}

/// <summary>
/// Exception thrown when multiple services found but SelectionMode.One was specified.
/// </summary>
public class MultipleServicesException : Exception
{
    public Type ServiceType { get; }
    public int Count { get; }

    public MultipleServicesException(Type serviceType, int count)
        : base($"Multiple services ({count}) found for {serviceType.Name}, but SelectionMode.One was specified")
    {
        ServiceType = serviceType;
        Count = count;
    }
}
