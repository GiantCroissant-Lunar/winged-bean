using System.Text.Json.Serialization;

namespace Dungeon.Host.Console;

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
