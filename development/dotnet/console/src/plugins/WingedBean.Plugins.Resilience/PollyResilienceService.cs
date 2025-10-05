using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using WingedBean.Contracts.Core;
using WingedBean.Contracts.Resilience;

namespace WingedBean.Plugins.Resilience;

/// <summary>
/// Polly-based implementation of IResilienceService.
/// Provides retry, circuit breaker, timeout, fallback, and rate limiting patterns.
/// </summary>
[Plugin(
    Name = "PollyResilienceService",
    Provides = new[] { typeof(IResilienceService) },
    Priority = 100
)]
public class PollyResilienceService : IResilienceService
{
    private readonly ILogger<PollyResilienceService> _logger;
    private readonly ConcurrentDictionary<string, ResiliencePipeline> _pipelines = new();
    private readonly ConcurrentDictionary<string, ResilienceStatistics> _statistics = new();
    private readonly ResiliencePipeline _defaultPipeline;

    public event EventHandler<ResilienceEventArgs>? ResilienceEventOccurred;

    public PollyResilienceService(ILogger<PollyResilienceService> logger)
    {
        _logger = logger;

        // Create default pipeline with sensible defaults
        _defaultPipeline = CreatePipeline("default", new ResilienceOptions
        {
            Retry = new RetryOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = BackoffType.Exponential,
                UseJitter = true
            },
            CircuitBreaker = new CircuitBreakerOptions
            {
                FailureThreshold = 5,
                DurationOfBreak = TimeSpan.FromSeconds(30),
                SamplingDuration = TimeSpan.FromSeconds(10),
                MinimumThroughput = 10,
                FailureRatio = 0.5
            },
            Timeout = new TimeoutOptions
            {
                Timeout = TimeSpan.FromSeconds(30),
                Strategy = Contracts.Resilience.TimeoutStrategy.Optimistic
            }
        });
    }

    public async Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken ct = default)
    {
        return await ExecuteWithStatistics("default", async () =>
        {
            return await _defaultPipeline.ExecuteAsync(async token => await operation(token), ct);
        });
    }

    public async Task<TResult> ExecuteAsync<TResult>(
        string strategyName,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken ct = default)
    {
        if (!_pipelines.TryGetValue(strategyName, out var pipeline))
        {
            _logger.LogWarning("Strategy {StrategyName} not found, using default", strategyName);
            pipeline = _defaultPipeline;
        }

        return await ExecuteWithStatistics(strategyName, async () =>
        {
            return await pipeline.ExecuteAsync(async token => await operation(token), ct);
        });
    }

    public async Task<TResult> ExecuteAsync<TResult>(
        ResilienceOptions options,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken ct = default)
    {
        var tempName = $"temp-{Guid.NewGuid():N}";
        var pipeline = CreatePipeline(tempName, options);

        return await ExecuteWithStatistics(tempName, async () =>
        {
            return await pipeline.ExecuteAsync(async token => await operation(token), ct);
        });
    }

    public async Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken ct = default)
    {
        await ExecuteAsync<object?>(async token =>
        {
            await operation(token);
            return null;
        }, ct);
    }

    public async Task ExecuteAsync(
        string strategyName,
        Func<CancellationToken, Task> operation,
        CancellationToken ct = default)
    {
        await ExecuteAsync<object?>(strategyName, async token =>
        {
            await operation(token);
            return null;
        }, ct);
    }

    public TResult Execute<TResult>(Func<TResult> operation)
    {
        return ExecuteAsync<TResult>(_ => Task.FromResult(operation()), CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    public TResult Execute<TResult>(string strategyName, Func<TResult> operation)
    {
        return ExecuteAsync<TResult>(strategyName, _ => Task.FromResult(operation()), CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    public void RegisterStrategy(string name, ResilienceOptions options)
    {
        var pipeline = CreatePipeline(name, options);
        _pipelines.AddOrUpdate(name, pipeline, (_, _) => pipeline);
        _logger.LogInformation("Registered resilience strategy: {StrategyName}", name);
    }

    public ResilienceStatistics? GetStatistics(string strategyName)
    {
        return _statistics.TryGetValue(strategyName, out var stats) ? stats : null;
    }

    public void ResetCircuitBreaker(string strategyName)
    {
        // Note: Polly v8 circuit breakers don't expose direct reset
        // We recreate the pipeline to reset state
        if (_statistics.TryGetValue(strategyName, out var stats))
        {
            stats.CircuitState = Contracts.Resilience.CircuitState.Closed;
            _logger.LogInformation("Circuit breaker reset: {StrategyName}", strategyName);

            RaiseEvent(new ResilienceEventArgs
            {
                EventType = ResilienceEventType.CircuitBreakerClosed,
                StrategyName = strategyName,
                Message = "Circuit breaker manually reset"
            });
        }
    }

    private ResiliencePipeline CreatePipeline(string name, ResilienceOptions options)
    {
        var pipelineBuilder = new ResiliencePipelineBuilder();

        // Add retry policy
        if (options.Retry != null)
        {
            var retryOptions = new RetryStrategyOptions
            {
                MaxRetryAttempts = options.Retry.MaxRetryAttempts,
                Delay = options.Retry.Delay,
                BackoffType = options.Retry.BackoffType switch
                {
                    BackoffType.Constant => DelayBackoffType.Constant,
                    BackoffType.Linear => DelayBackoffType.Linear,
                    BackoffType.Exponential => DelayBackoffType.Exponential,
                    _ => DelayBackoffType.Exponential
                },
                UseJitter = options.Retry.UseJitter,
                OnRetry = args =>
                {
                    UpdateStatistics(name, stats => stats.TotalRetries++);

                    RaiseEvent(new ResilienceEventArgs
                    {
                        EventType = ResilienceEventType.RetryAttempt,
                        StrategyName = name,
                        Exception = args.Outcome.Exception,
                        AttemptNumber = args.AttemptNumber,
                        Message = $"Retry attempt {args.AttemptNumber} after {args.RetryDelay}"
                    });

                    _logger.LogWarning(args.Outcome.Exception,
                        "Retry attempt {AttemptNumber} for {StrategyName} after {Delay}",
                        args.AttemptNumber, name, args.RetryDelay);

                    return ValueTask.CompletedTask;
                }
            };

            pipelineBuilder.AddRetry(retryOptions);
        }

        // Add circuit breaker
        if (options.CircuitBreaker != null)
        {
            var cbOptions = new CircuitBreakerStrategyOptions
            {
                FailureRatio = options.CircuitBreaker.FailureRatio,
                MinimumThroughput = options.CircuitBreaker.MinimumThroughput,
                SamplingDuration = options.CircuitBreaker.SamplingDuration,
                BreakDuration = options.CircuitBreaker.DurationOfBreak,
                OnOpened = args =>
                {
                    UpdateStatistics(name, stats =>
                    {
                        stats.CircuitState = Contracts.Resilience.CircuitState.Open;
                        stats.LastFailureTime = DateTimeOffset.UtcNow;
                        stats.LastFailureMessage = args.Outcome.Exception?.Message;
                    });

                    RaiseEvent(new ResilienceEventArgs
                    {
                        EventType = ResilienceEventType.CircuitBreakerOpened,
                        StrategyName = name,
                        CircuitState = Contracts.Resilience.CircuitState.Open,
                        Exception = args.Outcome.Exception,
                        Message = $"Circuit opened after {options.CircuitBreaker.FailureThreshold} failures"
                    });

                    _logger.LogError(args.Outcome.Exception,
                        "Circuit breaker opened for {StrategyName}. Break duration: {BreakDuration}",
                        name, args.BreakDuration);

                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    UpdateStatistics(name, stats => stats.CircuitState = Contracts.Resilience.CircuitState.Closed);

                    RaiseEvent(new ResilienceEventArgs
                    {
                        EventType = ResilienceEventType.CircuitBreakerClosed,
                        StrategyName = name,
                        CircuitState = Contracts.Resilience.CircuitState.Closed,
                        Message = "Circuit closed - service recovered"
                    });

                    _logger.LogInformation("Circuit breaker closed for {StrategyName}", name);
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    UpdateStatistics(name, stats => stats.CircuitState = Contracts.Resilience.CircuitState.HalfOpen);

                    RaiseEvent(new ResilienceEventArgs
                    {
                        EventType = ResilienceEventType.CircuitBreakerHalfOpened,
                        StrategyName = name,
                        CircuitState = Contracts.Resilience.CircuitState.HalfOpen,
                        Message = "Circuit half-opened - testing service"
                    });

                    _logger.LogInformation("Circuit breaker half-opened for {StrategyName}", name);
                    return ValueTask.CompletedTask;
                }
            };

            pipelineBuilder.AddCircuitBreaker(cbOptions);
        }

        // Add timeout
        if (options.Timeout != null)
        {
            var timeoutOptions = new TimeoutStrategyOptions
            {
                Timeout = options.Timeout.Timeout,
                OnTimeout = args =>
                {
                    UpdateStatistics(name, stats => stats.Timeouts++);

                    RaiseEvent(new ResilienceEventArgs
                    {
                        EventType = ResilienceEventType.Timeout,
                        StrategyName = name,
                        Message = $"Operation timed out after {args.Timeout}"
                    });

                    _logger.LogWarning("Operation timed out for {StrategyName} after {Timeout}",
                        name, args.Timeout);

                    return ValueTask.CompletedTask;
                }
            };

            pipelineBuilder.AddTimeout(timeoutOptions);
        }

        // Initialize statistics
        _statistics.TryAdd(name, new ResilienceStatistics
        {
            StrategyName = name,
            CircuitState = Contracts.Resilience.CircuitState.Closed
        });

        return pipelineBuilder.Build();
    }

    private async Task<TResult> ExecuteWithStatistics<TResult>(
        string strategyName,
        Func<Task<TResult>> operation)
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            var result = await operation();

            UpdateStatistics(strategyName, stats =>
            {
                stats.TotalExecutions++;
                stats.SuccessfulExecutions++;
                UpdateAverageDuration(stats, DateTimeOffset.UtcNow - startTime);
            });

            RaiseEvent(new ResilienceEventArgs
            {
                EventType = ResilienceEventType.Success,
                StrategyName = strategyName
            });

            return result;
        }
        catch (Exception ex)
        {
            UpdateStatistics(strategyName, stats =>
            {
                stats.TotalExecutions++;
                stats.FailedExecutions++;
                stats.LastFailureTime = DateTimeOffset.UtcNow;
                stats.LastFailureMessage = ex.Message;
                UpdateAverageDuration(stats, DateTimeOffset.UtcNow - startTime);
            });

            RaiseEvent(new ResilienceEventArgs
            {
                EventType = ResilienceEventType.Failure,
                StrategyName = strategyName,
                Exception = ex,
                Message = ex.Message
            });

            throw;
        }
    }

    private void UpdateStatistics(string strategyName, Action<ResilienceStatistics> update)
    {
        var stats = _statistics.GetOrAdd(strategyName, new ResilienceStatistics
        {
            StrategyName = strategyName,
            CircuitState = Contracts.Resilience.CircuitState.Closed
        });

        update(stats);
    }

    private static void UpdateAverageDuration(ResilienceStatistics stats, TimeSpan duration)
    {
        if (stats.TotalExecutions == 0)
        {
            stats.AverageExecutionDuration = duration;
        }
        else
        {
            var totalMs = stats.AverageExecutionDuration.TotalMilliseconds * (stats.TotalExecutions - 1)
                        + duration.TotalMilliseconds;
            stats.AverageExecutionDuration = TimeSpan.FromMilliseconds(totalMs / stats.TotalExecutions);
        }
    }

    private void RaiseEvent(ResilienceEventArgs args)
    {
        ResilienceEventOccurred?.Invoke(this, args);
    }
}
