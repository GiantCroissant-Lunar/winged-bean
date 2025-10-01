namespace WingedBean.Contracts.Config;

/// <summary>
/// Event args for configuration changes.
/// </summary>
public class ConfigChangedEventArgs : EventArgs
{
    /// <summary>
    /// The configuration key that changed.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// The old value before the change (null if the key was not previously set).
    /// </summary>
    public string? OldValue { get; init; }

    /// <summary>
    /// The new value after the change (null if the key was removed).
    /// </summary>
    public string? NewValue { get; init; }
}
