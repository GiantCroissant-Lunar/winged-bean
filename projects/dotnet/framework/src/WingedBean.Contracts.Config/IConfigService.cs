namespace WingedBean.Contracts.Config;

/// <summary>
/// Configuration service for accessing application settings.
/// Inspired by Microsoft.Extensions.Configuration but platform-agnostic.
/// </summary>
public interface IConfigService
{
    /// <summary>
    /// Get a configuration value by key.
    /// </summary>
    /// <param name="key">Configuration key (colon-separated for nested, e.g., "Plugins:Load")</param>
    /// <returns>Configuration value as string, or null if not found</returns>
    string? Get(string key);

    /// <summary>
    /// Get a strongly-typed configuration value.
    /// </summary>
    /// <typeparam name="T">Target type</typeparam>
    /// <param name="key">Configuration key</param>
    /// <returns>Parsed value, or default(T) if not found</returns>
    T? Get<T>(string key);

    /// <summary>
    /// Get a configuration section.
    /// </summary>
    /// <param name="key">Section key</param>
    /// <returns>Configuration section</returns>
    IConfigSection GetSection(string key);

    /// <summary>
    /// Set a configuration value (if implementation supports writes).
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="value">Value to set</param>
    void Set(string key, string value);

    /// <summary>
    /// Check if a key exists in configuration.
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <returns>True if key exists, false otherwise</returns>
    bool Exists(string key);

    /// <summary>
    /// Reload configuration from source (if supported).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task ReloadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when configuration changes (if supported by implementation).
    /// </summary>
    event EventHandler<ConfigChangedEventArgs>? ConfigChanged;
}
