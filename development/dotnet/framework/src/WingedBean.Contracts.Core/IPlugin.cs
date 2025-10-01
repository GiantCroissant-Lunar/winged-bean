using System.Threading;
using System.Threading.Tasks;

namespace WingedBean.Contracts.Core;

/// <summary>
/// Optional marker interface for plugins.
/// Plugins can implement this for lifecycle hooks, or just provide services directly.
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Plugin unique identifier.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Plugin version.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Called when the plugin is activated.
    /// </summary>
    Task OnActivateAsync(IRegistry registry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when the plugin is deactivated.
    /// </summary>
    Task OnDeactivateAsync(CancellationToken cancellationToken = default);
}
