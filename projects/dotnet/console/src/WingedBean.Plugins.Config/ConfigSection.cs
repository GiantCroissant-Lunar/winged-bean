using Microsoft.Extensions.Configuration;
using WingedBean.Contracts.Config;

namespace WingedBean.Plugins.Config;

/// <summary>
/// Implementation of IConfigSection wrapping Microsoft.Extensions.Configuration.IConfigurationSection.
/// </summary>
internal class ConfigSection : IConfigSection
{
    private readonly IConfigurationSection _section;

    public ConfigSection(IConfigurationSection section)
    {
        _section = section ?? throw new ArgumentNullException(nameof(section));
    }

    public string Key => _section.Key;

    public string? Value => _section.Value;

    public IConfigSection GetSection(string key)
    {
        return new ConfigSection(_section.GetSection(key));
    }

    public IEnumerable<IConfigSection> GetChildren()
    {
        return _section.GetChildren().Select(c => new ConfigSection(c));
    }

    public void Bind(object instance)
    {
        _section.Bind(instance);
    }

    public T? Get<T>()
    {
        return _section.Get<T>();
    }
}
