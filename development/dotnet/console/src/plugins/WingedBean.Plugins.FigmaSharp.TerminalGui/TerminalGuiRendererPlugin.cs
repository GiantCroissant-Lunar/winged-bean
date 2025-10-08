using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Plate.CrossMilo.Contracts.FigmaSharp;
using Plate.CrossMilo.Contracts.FigmaSharp.Renderer;
using Plate.PluginManoi.Core;

namespace WingedBean.Plugins.FigmaSharp.TerminalGui;

/// <summary>
/// Plugin activator for Terminal.Gui renderer
/// </summary>
public class TerminalGuiRendererPlugin : IPluginActivator
{
    public Task ActivateAsync(IServiceCollection services, IServiceProvider hostServices, CancellationToken ct = default)
    {
        // Register Terminal.Gui renderer as singleton
        services.AddSingleton<IService, TerminalGuiRenderer>();
        
        return Task.CompletedTask;
    }
    
    public Task DeactivateAsync(CancellationToken ct = default)
    {
        // Cleanup if needed
        return Task.CompletedTask;
    }
}
