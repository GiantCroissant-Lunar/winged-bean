using System;

namespace WingedBean.Contracts.Resilience;

/// <summary>
/// Statistics for a resilience strategy.
/// </summary>
public class ResilienceStatistics
{
    /// <summary>
    /// Strategy name.
    /// </summary>
    public string StrategyName { get; set; } = string.Empty;

    /// <summary>
    /// Circuit breaker state (if applicable).
    /// </summary>
    public CircuitState? CircuitState { get; set; }

    /// <summary>
    /// Total number of executions.
    /// </summary>
    public long TotalExecutions { get; set; }

    /// <summary>
    /// Number of successful executions.
    /// </summary>
    public long SuccessfulExecutions { get; set; }

    /// <summary>
    /// Number of failed executions.
    /// </summary>
    public long FailedExecutions { get; set; }

    /// <summary>
    /// Total number of retries performed.
    /// </summary>
    public long TotalRetries { get; set; }

    /// <summary>
    /// Number of timeouts.
    /// </summary>
    public long Timeouts { get; set; }

    /// <summary>
    /// Number of fallbacks executed.
    /// </summary>
    public long FallbacksExecuted { get; set; }

    /// <summary>
    /// Last failure time (if any).
    /// </summary>
    public DateTimeOffset? LastFailureTime { get; set; }

    /// <summary>
    /// Last failure exception message.
    /// </summary>
    public string? LastFailureMessage { get; set; }

    /// <summary>
    /// Average execution duration.
    /// </summary>
    public TimeSpan AverageExecutionDuration { get; set; }
}

/// <summary>
/// Circuit breaker state.
/// </summary>
public enum CircuitState
{
    /// <summary>Circuit is closed - operations flow normally</summary>
    Closed,

    /// <summary>Circuit is open - operations are blocked</summary>
    Open,

    /// <summary>Circuit is half-open - testing if service recovered</summary>
    HalfOpen,

    /// <summary>Circuit is isolated - manually opened</summary>
    Isolated
}
