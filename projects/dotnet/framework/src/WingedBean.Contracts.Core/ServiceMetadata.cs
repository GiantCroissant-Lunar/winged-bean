using System.Collections.Generic;

namespace WingedBean.Contracts.Core;

/// <summary>
/// Metadata associated with a registered service.
/// </summary>
public record ServiceMetadata
{
    public string? Name { get; init; }
    public int Priority { get; init; }
    public string? Version { get; init; }
    public string? Platform { get; init; }
    public IDictionary<string, object>? Properties { get; init; }
}
