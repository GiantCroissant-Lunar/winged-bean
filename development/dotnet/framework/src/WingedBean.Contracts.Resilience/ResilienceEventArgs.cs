using System;

namespace WingedBean.Contracts.Resilience;

/// <summary>
/// Event args for resilience events (retries, circuit breaker state changes, etc.).
/// </summary>
public class ResilienceEventArgs : EventArgs
{
    /// <summary>
    /// Type of resilience event.
    /// </summary>
    public ResilienceEventType EventType { get; set; }

    /// <summary>
    /// Strategy name that triggered the event.
    /// </summary>
    public string StrategyName { get; set; } = string.Empty;

    /// <summary>
    /// Exception that caused the event (if applicable).
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Retry attempt number (for retry events).
    /// </summary>
    public int? AttemptNumber { get; set; }

    /// <summary>
    /// Circuit state (for circuit breaker events).
    /// </summary>
    public CircuitState? CircuitState { get; set; }

    /// <summary>
    /// Event timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Additional context information.
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// Types of resilience events.
/// </summary>
public enum ResilienceEventType
{
    /// <summary>Retry attempt started</summary>
    RetryAttempt,

    /// <summary>All retries exhausted</summary>
    RetryExhausted,

    /// <summary>Circuit breaker opened</summary>
    CircuitBreakerOpened,

    /// <summary>Circuit breaker closed</summary>
    CircuitBreakerClosed,

    /// <summary>Circuit breaker half-opened</summary>
    CircuitBreakerHalfOpened,

    /// <summary>Operation timed out</summary>
    Timeout,

    /// <summary>Fallback executed</summary>
    FallbackExecuted,

    /// <summary>Rate limit exceeded</summary>
    RateLimitExceeded,

    /// <summary>Operation succeeded</summary>
    Success,

    /// <summary>Operation failed</summary>
    Failure
}
