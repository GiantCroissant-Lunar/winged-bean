using LibVLCSharp.Shared;
using Microsoft.Extensions.Logging;
using Plate.CrossMilo.Contracts.Audio.Services;
using Plate.CrossMilo.Contracts.Audio;
using Plate.PluginManoi.Contracts;

namespace WingedBean.Plugins.Audio;

/// <summary>
/// Audio service implementation using LibVLC for console applications.
/// </summary>
[RealizeService(typeof(IService))]
public sealed class LibVlcAudioService : IService, IDisposable
{
    private readonly ILogger<LibVlcAudioService> _logger;
    private readonly object _lock = new();
    private readonly Dictionary<string, AudioChannel> _channels = new();
    private LibVLC? _libVlc;
    private float _masterVolume = 1.0f;
    private bool _disposed;

    public LibVlcAudioService(ILogger<LibVlcAudioService> logger)
    {
        _logger = logger;
        InitializeLibVlc();
    }

    public float Volume
    {
        get
        {
            lock (_lock)
            {
                return _masterVolume;
            }
        }
        set
        {
            lock (_lock)
            {
                _masterVolume = Math.Clamp(value, 0f, 1f);
                foreach (var channel in _channels.Values)
                {
                    channel.UpdateVolume(_masterVolume);
                }
            }
        }
    }

    public void Play(string clipId, AudioPlayOptions? options = null)
    {
        ThrowIfDisposed();

        if (_libVlc == null)
        {
            _logger.LogWarning("Cannot play audio - LibVLC not initialized");
            return;
        }

        options ??= new AudioPlayOptions();

        lock (_lock)
        {
            try
            {
                // Stop existing channel if playing
                if (_channels.TryGetValue(clipId, out var existingChannel))
                {
                    existingChannel.Dispose();
                    _channels.Remove(clipId);
                }

                // Create new channel
                var channel = new AudioChannel(_libVlc, clipId, options, _masterVolume, _logger);
                _channels[clipId] = channel;

                // Start playback
                channel.Play();

                _logger.LogDebug("Started playback: {ClipId}", clipId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to play audio: {ClipId}", clipId);
            }
        }
    }

    public void Stop(string clipId)
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            if (_channels.TryGetValue(clipId, out var channel))
            {
                channel.Stop();
                channel.Dispose();
                _channels.Remove(clipId);
                _logger.LogDebug("Stopped playback: {ClipId}", clipId);
            }
        }
    }

    public void StopAll()
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            foreach (var channel in _channels.Values)
            {
                channel.Stop();
                channel.Dispose();
            }
            _channels.Clear();
            _logger.LogDebug("Stopped all audio playback");
        }
    }

    public void Pause(string clipId)
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            if (_channels.TryGetValue(clipId, out var channel))
            {
                channel.Pause();
                _logger.LogDebug("Paused playback: {ClipId}", clipId);
            }
        }
    }

    public void Resume(string clipId)
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            if (_channels.TryGetValue(clipId, out var channel))
            {
                channel.Resume();
                _logger.LogDebug("Resumed playback: {ClipId}", clipId);
            }
        }
    }

    public bool IsPlaying(string clipId)
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            return _channels.TryGetValue(clipId, out var channel) && channel.IsPlaying;
        }
    }

    public async Task<bool> LoadAsync(string clipId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_libVlc == null)
        {
            _logger.LogWarning("Cannot load audio - LibVLC not initialized");
            return false;
        }

        try
        {
            // Preload validation - check if file exists and can be accessed
            if (!File.Exists(clipId))
            {
                _logger.LogWarning("Audio file not found: {ClipId}", clipId);
                return false;
            }

            // Test creating a media object
            await Task.Run(() =>
            {
                using var media = new Media(_libVlc, clipId, FromType.FromPath);
                _ = media.Duration;
            }, cancellationToken);

            _logger.LogDebug("Preloaded audio: {ClipId}", clipId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to preload audio: {ClipId}", clipId);
            return false;
        }
    }

    public void Unload(string clipId)
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            if (_channels.TryGetValue(clipId, out var channel))
            {
                channel.Dispose();
                _channels.Remove(clipId);
                _logger.LogDebug("Unloaded audio: {ClipId}", clipId);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            StopAll();

            _libVlc?.Dispose();
            _libVlc = null;

            _disposed = true;
        }

        _logger.LogInformation("LibVlcAudioService disposed");
    }

    private void InitializeLibVlc()
    {
        try
        {
            Core.Initialize();
            _libVlc = new LibVLC("--quiet");
            _logger.LogInformation("LibVLC initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize LibVLC - audio will be unavailable");
            _libVlc = null;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(LibVlcAudioService));
        }
    }

    private sealed class AudioChannel : IDisposable
    {
        private readonly string _clipId;
        private readonly AudioPlayOptions _options;
        private readonly ILogger _logger;
        private readonly float _masterVolume;
        private Media? _media;
        private MediaPlayer? _player;
        private bool _disposed;

        public AudioChannel(LibVLC libVlc, string clipId, AudioPlayOptions options, float masterVolume, ILogger logger)
        {
            _clipId = clipId;
            _options = options;
            _logger = logger;
            _masterVolume = masterVolume;

            try
            {
                _media = new Media(libVlc, clipId, FromType.FromPath);
                _player = new MediaPlayer(_media);

                // Apply volume (LibVLC uses 0-100 scale)
                var volumePercent = (int)(Math.Clamp(options.Volume * masterVolume, 0f, 1f) * 100);
                _player.Volume = volumePercent;

                // Handle playback ended event
                _player.EndReached += OnEndReached;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public bool IsPlaying => _player?.IsPlaying ?? false;

        public void Play()
        {
            _player?.Play();
        }

        public void Stop()
        {
            _player?.Stop();
        }

        public void Pause()
        {
            _player?.Pause();
        }

        public void Resume()
        {
            _player?.Play();
        }

        public void UpdateVolume(float masterVolume)
        {
            if (_player != null)
            {
                var volumePercent = (int)(Math.Clamp(_options.Volume * masterVolume, 0f, 1f) * 100);
                _player.Volume = volumePercent;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_player != null)
            {
                _player.EndReached -= OnEndReached;
                _player.Stop();
            }

            _player?.Dispose();
            _media?.Dispose();

            _player = null;
            _media = null;
            _disposed = true;
        }

        private void OnEndReached(object? sender, EventArgs e)
        {
            // Handle looping
            if (_options.Loop && _player != null)
            {
                try
                {
                    _player.Stop();
                    _player.Play();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to loop audio: {ClipId}", _clipId);
                }
            }
        }
    }
}
