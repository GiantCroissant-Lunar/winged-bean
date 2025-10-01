namespace WingedBean.Contracts.Audio;

/// <summary>
/// Audio service for playing sounds and music.
/// Platform implementations: NAudio (Console), Unity AudioSource (Unity), etc.
/// </summary>
public interface IAudioService
{
    /// <summary>
    /// Play an audio clip by name/path.
    /// </summary>
    void Play(string clipId, AudioPlayOptions? options = null);

    /// <summary>
    /// Stop a playing audio clip.
    /// </summary>
    void Stop(string clipId);

    /// <summary>
    /// Stop all playing audio.
    /// </summary>
    void StopAll();

    /// <summary>
    /// Pause a playing audio clip.
    /// </summary>
    void Pause(string clipId);

    /// <summary>
    /// Resume a paused audio clip.
    /// </summary>
    void Resume(string clipId);

    /// <summary>
    /// Master volume (0.0 to 1.0).
    /// </summary>
    float Volume { get; set; }

    /// <summary>
    /// Check if an audio clip is currently playing.
    /// </summary>
    bool IsPlaying(string clipId);

    /// <summary>
    /// Load an audio clip (preload for faster playback).
    /// </summary>
    Task<bool> LoadAsync(string clipId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unload an audio clip (free memory).
    /// </summary>
    void Unload(string clipId);
}
