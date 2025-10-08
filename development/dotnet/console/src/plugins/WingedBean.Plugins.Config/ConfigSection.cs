using Microsoft.Extensions.Configuration;
using Plate.CrossMilo.Contracts.Config.Services;
using Plate.CrossMilo.Contracts.Config;

namespace WingedBean.Plugins.Config;

/// <summary>
/// Implementation of Plate.CrossMilo.Contracts.Config.IConfigSection wrapping Microsoft.Extensions.Configuration.IConfigurationSection.
/// </summary>
internal class ConfigSection : Plate.CrossMilo.Contracts.Config.IConfigSection
{
    private readonly IConfigurationSection _section;

    public ConfigSection(IConfigurationSection section)
    {
        _section = section ?? throw new ArgumentNullException(nameof(section));
    }

    public string Key => _section.Key;

    public string? Value => _section.Value;

    public Plate.CrossMilo.Contracts.Config.IConfigSection GetSection(string key)
    {
        return new ConfigSection(_section.GetSection(key));
    }

    public IEnumerable<Plate.CrossMilo.Contracts.Config.IConfigSection> GetChildren()
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
