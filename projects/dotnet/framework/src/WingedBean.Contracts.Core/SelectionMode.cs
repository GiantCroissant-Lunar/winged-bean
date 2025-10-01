namespace WingedBean.Contracts.Core;

/// <summary>
/// Selection mode for retrieving services from registry.
/// </summary>
public enum SelectionMode
{
    /// <summary>
    /// Get the single registered implementation (error if multiple exist).
    /// </summary>
    One,

    /// <summary>
    /// Get the implementation with the highest priority value.
    /// </summary>
    HighestPriority,

    /// <summary>
    /// Get all registered implementations (for fan-out scenarios).
    /// </summary>
    All
}
