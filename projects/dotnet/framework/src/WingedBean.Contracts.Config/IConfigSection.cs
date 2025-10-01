namespace WingedBean.Contracts.Config;

/// <summary>
/// Represents a section of configuration.
/// </summary>
public interface IConfigSection
{
    /// <summary>
    /// Section key/path.
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Section value (if it's a leaf node).
    /// </summary>
    string? Value { get; }

    /// <summary>
    /// Get a child section.
    /// </summary>
    /// <param name="key">Child section key</param>
    /// <returns>Child configuration section</returns>
    IConfigSection GetSection(string key);

    /// <summary>
    /// Get all child sections.
    /// </summary>
    /// <returns>All child sections</returns>
    IEnumerable<IConfigSection> GetChildren();

    /// <summary>
    /// Bind this section to an object instance.
    /// </summary>
    /// <param name="instance">Target object instance to bind to</param>
    void Bind(object instance);

    /// <summary>
    /// Get this section as a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">Target type</typeparam>
    /// <returns>Strongly-typed object representation of this section</returns>
    T? Get<T>();
}
