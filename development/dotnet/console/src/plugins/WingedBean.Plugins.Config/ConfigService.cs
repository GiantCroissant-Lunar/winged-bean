using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Plate.CrossMilo.Contracts.Config.Services;
using Plate.CrossMilo.Contracts.Config;
using Plate.PluginManoi.Contracts;

namespace WingedBean.Plugins.Config;

/// <summary>
/// Configuration service implementation using Microsoft.Extensions.Configuration.
/// Supports JSON files, environment variables, and in-memory sources.
/// </summary>
[Plugin(
    Name = "ConfigService",
    Provides = new[] { typeof(IService) },
    Priority = 100
)]
public class ConfigService : IService
{
    private IConfigurationRoot _configuration;
    private readonly Dictionary<string, string?> _originalValues = new();

    /// <summary>
    /// Event raised when configuration changes (if supported by implementation).
    /// </summary>
    public event EventHandler<ConfigChangedEventArgs>? ConfigChanged;

    /// <summary>
    /// Initializes a new instance of ConfigService with default configuration sources.
    /// Includes appsettings.json and environment variables.
    /// </summary>
    public ConfigService()
    {
        _configuration = BuildConfiguration();

        // Register change token callback for configuration reload
        ChangeToken.OnChange(
            () => _configuration.GetReloadToken(),
            OnConfigurationReloaded
        );
    }

    /// <summary>
    /// Initializes a new instance of ConfigService with a custom configuration root.
    /// </summary>
    /// <param name="configuration">Custom configuration root</param>
    public ConfigService(IConfigurationRoot configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        // Register change token callback for configuration reload
        ChangeToken.OnChange(
            () => _configuration.GetReloadToken(),
            OnConfigurationReloaded
        );
    }

    /// <summary>
    /// Builds default configuration from appsettings.json and environment variables.
    /// </summary>
    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        return builder.Build();
    }

    /// <summary>
    /// Get a configuration value by key.
    /// </summary>
    public string? Get(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        return _configuration[key];
    }

    /// <summary>
    /// Get a strongly-typed configuration value.
    /// </summary>
    public T? Get<T>(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        try
        {
            return _configuration.GetValue<T>(key);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Get a configuration section.
    /// </summary>
    public Plate.CrossMilo.Contracts.Config.IConfigSection GetSection(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        var section = _configuration.GetSection(key);
        return new ConfigSection(section);
    }

    /// <summary>
    /// Set a configuration value (in-memory only).
    /// Note: This sets values in memory but does not persist to files.
    /// </summary>
    public void Set(string key, string value)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        var oldValue = Get(key);

        // Store the original value if not already stored
        if (!_originalValues.ContainsKey(key))
        {
            _originalValues[key] = oldValue;
        }

        // Create a new configuration with the updated value
        var inMemoryConfig = new Dictionary<string, string?> { { key, value } };

        var builder = new ConfigurationBuilder()
            .AddConfiguration(_configuration)
            .AddInMemoryCollection(inMemoryConfig!);

        _configuration = builder.Build();

        // Raise change event
        OnConfigChanged(new ConfigChangedEventArgs
        {
            Key = key,
            OldValue = oldValue,
            NewValue = value
        });
    }

    /// <summary>
    /// Check if a key exists in configuration.
    /// </summary>
    public bool Exists(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        var section = _configuration.GetSection(key);
        return section.Exists();
    }

    /// <summary>
    /// Reload configuration from source (if supported).
    /// </summary>
    public Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        _configuration.Reload();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when configuration is reloaded.
    /// </summary>
    private void OnConfigurationReloaded()
    {
        // Note: Microsoft.Extensions.Configuration doesn't provide detailed change information
        // So we raise a generic change event
        OnConfigChanged(new ConfigChangedEventArgs
        {
            Key = "*",
            OldValue = null,
            NewValue = null
        });
    }

    /// <summary>
    /// Raises the ConfigChanged event.
    /// </summary>
    protected virtual void OnConfigChanged(ConfigChangedEventArgs e)
    {
        ConfigChanged?.Invoke(this, e);
    }
}
