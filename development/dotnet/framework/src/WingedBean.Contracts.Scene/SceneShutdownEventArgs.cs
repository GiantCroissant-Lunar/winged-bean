using System;

namespace WingedBean.Contracts.Scene;

/// <summary>
/// Event args for scene shutdown.
/// </summary>
public class SceneShutdownEventArgs : EventArgs
{
    public ShutdownReason Reason { get; init; }
}

public enum ShutdownReason
{
    UserRequest,    // User closed window or pressed quit
    Error,          // Unhandled error
    Timeout         // Idle timeout (optional)
}
