using System.Text.Json.Serialization;

namespace ConsoleDungeon.Host;

/// <summary>
/// Defines when a plugin should be loaded.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LoadStrategy
{
    /// <summary>
    /// Load immediately at startup.
    /// </summary>
    Eager,

    /// <summary>
    /// Load on first use.
    /// </summary>
    Lazy,

    /// <summary>
    /// Load only when explicitly requested.
    /// </summary>
    Explicit
}
