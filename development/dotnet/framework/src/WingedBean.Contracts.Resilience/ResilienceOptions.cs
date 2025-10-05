using System;
using System.Collections.Generic;

namespace WingedBean.Contracts.Resilience;

/// <summary>
/// Configuration options for resilience policies.
/// </summary>
public class ResilienceOptions
{
    /// <summary>
    /// Retry configuration.
    /// </summary>
    public RetryOptions? Retry { get; set; }

    /// <summary>
    /// Circuit breaker configuration.
    /// </summary>
    public CircuitBreakerOptions? CircuitBreaker { get; set; }

    /// <summary>
    /// Timeout configuration.
    /// </summary>
    public TimeoutOptions? Timeout { get; set; }

    /// <summary>
    /// Fallback configuration.
    /// </summary>
    public FallbackOptions? Fallback { get; set; }

    /// <summary>
    /// Rate limiter configuration.
    /// </summary>
    public RateLimiterOptions? RateLimiter { get; set; }

    /// <summary>
    /// Additional metadata for this strategy.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Retry policy options.
/// </summary>
public class RetryOptions
{
    /// <summary>
    /// Maximum number of retry attempts. Default: 3.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retries. Default: 1 second.
    /// </summary>
    public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Backoff type: Linear, Exponential, or Constant.
    /// </summary>
    public BackoffType BackoffType { get; set; } = BackoffType.Exponential;

    /// <summary>
    /// Exceptions to retry on. Empty = retry all exceptions.
    /// </summary>
    public List<Type> RetryOn { get; set; } = new();

    /// <summary>
    /// Jitter to add to retry delays (reduces thundering herd).
    /// </summary>
    public bool UseJitter { get; set; } = true;
}

/// <summary>
/// Circuit breaker policy options.
/// </summary>
public class CircuitBreakerOptions
{
    /// <summary>
    /// Number of consecutive failures before opening circuit. Default: 5.
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Duration circuit stays open before attempting half-open. Default: 30 seconds.
    /// </summary>
    public TimeSpan DurationOfBreak { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Sampling duration for advanced circuit breaker. Default: 10 seconds.
    /// </summary>
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Minimum throughput before circuit breaker activates. Default: 10.
    /// </summary>
    public int MinimumThroughput { get; set; } = 10;

    /// <summary>
    /// Failure ratio threshold (0.0 to 1.0). Default: 0.5 (50%).
    /// </summary>
    public double FailureRatio { get; set; } = 0.5;
}

/// <summary>
/// Timeout policy options.
/// </summary>
public class TimeoutOptions
{
    /// <summary>
    /// Timeout duration. Default: 30 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Timeout strategy: Optimistic or Pessimistic.
    /// </summary>
    public TimeoutStrategy Strategy { get; set; } = TimeoutStrategy.Optimistic;
}

/// <summary>
/// Fallback policy options.
/// </summary>
public class FallbackOptions
{
    /// <summary>
    /// Fallback action when operation fails.
    /// </summary>
    public Func<Exception, object?>? FallbackAction { get; set; }

    /// <summary>
    /// Exceptions to apply fallback on. Empty = all exceptions.
    /// </summary>
    public List<Type> FallbackOn { get; set; } = new();
}

/// <summary>
/// Rate limiter options.
/// </summary>
public class RateLimiterOptions
{
    /// <summary>
    /// Maximum number of permits. Default: 100.
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// Window duration for rate limiting. Default: 1 minute.
    /// </summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Queue limit for pending requests. Default: 0 (no queue).
    /// </summary>
    public int QueueLimit { get; set; } = 0;
}

/// <summary>
/// Backoff strategy for retries.
/// </summary>
public enum BackoffType
{
    /// <summary>Constant delay between retries</summary>
    Constant,

    /// <summary>Linear increase (delay * attemptNumber)</summary>
    Linear,

    /// <summary>Exponential backoff (delay * 2^attemptNumber)</summary>
    Exponential
}

/// <summary>
/// Timeout strategy.
/// </summary>
public enum TimeoutStrategy
{
    /// <summary>Optimistic timeout - allows operation to complete naturally</summary>
    Optimistic,

    /// <summary>Pessimistic timeout - cancels operation token aggressively</summary>
    Pessimistic
}
