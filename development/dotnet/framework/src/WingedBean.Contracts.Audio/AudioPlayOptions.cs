namespace WingedBean.Contracts.Audio;

/// <summary>
/// Options for playing audio clips.
/// </summary>
public record AudioPlayOptions
{
    public float Volume { get; init; } = 1.0f;
    public bool Loop { get; init; } = false;
    public float Pitch { get; init; } = 1.0f;
    public float FadeInDuration { get; init; } = 0f;
    public string? MixerGroup { get; init; }
}
