using System;
using System.Threading;
using System.Threading.Tasks;

namespace WingedBean.Contracts.Gameplay;

/// <summary>
/// Core gameplay service entry-point used by both console and desktop UIs.
/// Provides lifecycle and update hooks.
/// </summary>
public interface IService
{
    /// <summary>Initialize gameplay systems and load initial state.</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Advance gameplay by a timestep (called by host or UI loop).</summary>
    Task UpdateAsync(TimeSpan delta, CancellationToken cancellationToken = default);

    /// <summary>Stop and cleanup gameplay systems.</summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}
