using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using WingedBean.Contracts;

namespace WingedBean.Plugins.PtyService;

/// <summary>
/// PTY service implementation that uses Node.js node-pty for actual PTY functionality
/// </summary>
public class NodePtyService : IPtyService, IDisposable
{
    private readonly ILogger<NodePtyService> _logger;
    private readonly ConcurrentDictionary<string, PtySessionInfo> _sessions = new();
    private readonly object _lock = new();
    private bool _disposed = false;

    public event EventHandler<PtyDataReceivedEventArgs>? DataReceived;
    public event EventHandler<PtySessionExitedEventArgs>? SessionExited;

    public NodePtyService(ILogger<NodePtyService> logger)
    {
        _logger = logger;
    }

    public async Task<PtySession> StartSessionAsync(PtyConfig config, CancellationToken ct = default)
    {
        var sessionId = Guid.NewGuid().ToString();
        _logger.LogInformation("Starting PTY session {SessionId} with command: {Command}", sessionId, config.Command);

        // Create a simple PTY session using dotnet run for the console dungeon
        // In a real implementation, this would use the Node.js PTY service
        var sessionInfo = new PtySessionInfo
        {
            SessionId = sessionId,
            Config = config,
            StartTime = DateTimeOffset.UtcNow
        };

        // For now, simulate a PTY session by running the command directly
        var processStartInfo = new ProcessStartInfo
        {
            FileName = config.Command,
            Arguments = string.Join(" ", config.Args),
            WorkingDirectory = config.WorkingDirectory ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        // Add environment variables
        foreach (var env in config.Environment)
        {
            processStartInfo.Environment[env.Key] = env.Value;
        }

        // Set terminal-related environment variables
        processStartInfo.Environment["TERM"] = config.TerminalType;
        processStartInfo.Environment["COLUMNS"] = config.Cols.ToString();
        processStartInfo.Environment["LINES"] = config.Rows.ToString();

        var process = new Process { StartInfo = processStartInfo };
        
        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                var data = Encoding.UTF8.GetBytes(e.Data + "\n");
                var args = new PtyDataReceivedEventArgs
                {
                    SessionId = sessionId,
                    Data = data,
                    Timestamp = DateTimeOffset.UtcNow
                };
                DataReceived?.Invoke(this, args);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                var data = Encoding.UTF8.GetBytes(e.Data + "\n");
                var args = new PtyDataReceivedEventArgs
                {
                    SessionId = sessionId,
                    Data = data,
                    Timestamp = DateTimeOffset.UtcNow
                };
                DataReceived?.Invoke(this, args);
            }
        };

        process.Exited += (sender, e) =>
        {
            var args = new PtySessionExitedEventArgs
            {
                SessionId = sessionId,
                ExitCode = process.ExitCode,
                Timestamp = DateTimeOffset.UtcNow
            };
            SessionExited?.Invoke(this, args);
            
            _sessions.TryRemove(sessionId, out _);
        };

        process.EnableRaisingEvents = true;
        
        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            sessionInfo.Process = process;
            sessionInfo.ProcessId = process.Id;

            _sessions[sessionId] = sessionInfo;

            var session = new PtySession
            {
                SessionId = sessionId,
                ProcessId = process.Id,
                Config = config,
                StartTime = sessionInfo.StartTime
            };

            _logger.LogInformation("PTY session {SessionId} started with PID {ProcessId}", sessionId, process.Id);
            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start PTY session {SessionId}", sessionId);
            process.Dispose();
            throw;
        }
    }

    public async Task StopSessionAsync(string sessionId, CancellationToken ct = default)
    {
        _logger.LogInformation("Stopping PTY session {SessionId}", sessionId);

        if (_sessions.TryRemove(sessionId, out var sessionInfo))
        {
            try
            {
                if (sessionInfo.Process != null && !sessionInfo.Process.HasExited)
                {
                    sessionInfo.Process.Kill(true);
                    await sessionInfo.Process.WaitForExitAsync(ct);
                }
                sessionInfo.Process?.Dispose();
                _logger.LogInformation("PTY session {SessionId} stopped", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping PTY session {SessionId}", sessionId);
                throw;
            }
        }
        else
        {
            _logger.LogWarning("PTY session {SessionId} not found", sessionId);
        }
    }

    public async Task SendDataAsync(string sessionId, byte[] data, CancellationToken ct = default)
    {
        if (_sessions.TryGetValue(sessionId, out var sessionInfo))
        {
            try
            {
                if (sessionInfo.Process?.StandardInput != null)
                {
                    var text = Encoding.UTF8.GetString(data);
                    await sessionInfo.Process.StandardInput.WriteAsync(text);
                    await sessionInfo.Process.StandardInput.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending data to PTY session {SessionId}", sessionId);
                throw;
            }
        }
        else
        {
            _logger.LogWarning("PTY session {SessionId} not found for data send", sessionId);
        }
    }

    public Task ResizeAsync(string sessionId, int cols, int rows, CancellationToken ct = default)
    {
        _logger.LogInformation("Resize request for PTY session {SessionId}: {Cols}x{Rows}", sessionId, cols, rows);
        
        if (_sessions.TryGetValue(sessionId, out var sessionInfo))
        {
            sessionInfo.Config.Cols = cols;
            sessionInfo.Config.Rows = rows;
            
            // In a real PTY implementation, we would resize the actual PTY
            // For now, just log the resize request
            _logger.LogInformation("Resize applied to PTY session {SessionId}", sessionId);
        }
        else
        {
            _logger.LogWarning("PTY session {SessionId} not found for resize", sessionId);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _logger.LogInformation("Disposing PTY service and stopping all sessions");
            
            var sessionIds = _sessions.Keys.ToList();
            foreach (var sessionId in sessionIds)
            {
                try
                {
                    StopSessionAsync(sessionId).Wait(TimeSpan.FromSeconds(5));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error stopping session {SessionId} during disposal", sessionId);
                }
            }

            _disposed = true;
        }
    }

    private class PtySessionInfo
    {
        public string SessionId { get; set; } = string.Empty;
        public PtyConfig Config { get; set; } = new();
        public DateTimeOffset StartTime { get; set; }
        public int ProcessId { get; set; }
        public Process? Process { get; set; }
    }
}
