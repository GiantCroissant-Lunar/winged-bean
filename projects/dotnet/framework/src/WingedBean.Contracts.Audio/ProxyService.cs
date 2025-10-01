using WingedBean.Contracts.Core;

namespace WingedBean.Contracts.Audio;

/// <summary>
/// Proxy service for IAudioService.
/// </summary>
[RealizeService(typeof(IAudioService))]
[SelectionStrategy(SelectionMode.HighestPriority)]
public partial class ProxyService : IAudioService
{
    private readonly IRegistry _registry;

    public ProxyService(IRegistry registry)
    {
        _registry = registry;
    }

    // Source gen fills in all methods
    public void Play(string clipId, AudioPlayOptions? options = null)
    {
        throw new NotImplementedException("Source generator will implement this method");
    }

    public void Stop(string clipId)
    {
        throw new NotImplementedException("Source generator will implement this method");
    }

    public void StopAll()
    {
        throw new NotImplementedException("Source generator will implement this method");
    }

    public void Pause(string clipId)
    {
        throw new NotImplementedException("Source generator will implement this method");
    }

    public void Resume(string clipId)
    {
        throw new NotImplementedException("Source generator will implement this method");
    }

    public float Volume
    {
        get => throw new NotImplementedException("Source generator will implement this property");
        set => throw new NotImplementedException("Source generator will implement this property");
    }

    public bool IsPlaying(string clipId)
    {
        throw new NotImplementedException("Source generator will implement this method");
    }

    public Task<bool> LoadAsync(string clipId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Source generator will implement this method");
    }

    public void Unload(string clipId)
    {
        throw new NotImplementedException("Source generator will implement this method");
    }
}
