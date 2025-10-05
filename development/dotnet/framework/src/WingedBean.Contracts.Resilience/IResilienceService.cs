using System;
using System.Threading;
using System.Threading.Tasks;

namespace WingedBean.Contracts.Resilience;

/// <summary>
/// Resilience service for executing operations with retry, circuit breaker, timeout, and fallback policies.
/// Powered by Polly library for production-grade resilience patterns.
/// </summary>
public interface IResilienceService
{
    /// <summary>
    /// Execute an async operation with resilience policies applied.
    /// Uses default retry + circuit breaker policies.
    /// </summary>
    /// <typeparam name="TResult">Return type</typeparam>
    /// <param name="operation">Operation to execute</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken ct = default);

    /// <summary>
    /// Execute an async operation with a named resilience strategy.
    /// </summary>
    /// <typeparam name="TResult">Return type</typeparam>
    /// <param name="strategyName">Named strategy (e.g., "http-retry", "database-circuit-breaker")</param>
    /// <param name="operation">Operation to execute</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<TResult> ExecuteAsync<TResult>(
        string strategyName,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken ct = default);

    /// <summary>
    /// Execute an operation with custom resilience options.
    /// </summary>
    /// <typeparam name="TResult">Return type</typeparam>
    /// <param name="options">Resilience configuration</param>
    /// <param name="operation">Operation to execute</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<TResult> ExecuteAsync<TResult>(
        ResilienceOptions options,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken ct = default);

    /// <summary>
    /// Execute a void async operation with resilience policies.
    /// </summary>
    /// <param name="operation">Operation to execute</param>
    /// <param name="ct">Cancellation token</param>
    Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken ct = default);

    /// <summary>
    /// Execute a void async operation with a named strategy.
    /// </summary>
    /// <param name="strategyName">Named strategy</param>
    /// <param name="operation">Operation to execute</param>
    /// <param name="ct">Cancellation token</param>
    Task ExecuteAsync(
        string strategyName,
        Func<CancellationToken, Task> operation,
        CancellationToken ct = default);

    /// <summary>
    /// Execute a synchronous operation with resilience policies.
    /// </summary>
    /// <typeparam name="TResult">Return type</typeparam>
    /// <param name="operation">Operation to execute</param>
    /// <returns>Operation result</returns>
    TResult Execute<TResult>(Func<TResult> operation);

    /// <summary>
    /// Execute a synchronous operation with a named strategy.
    /// </summary>
    /// <typeparam name="TResult">Return type</typeparam>
    /// <param name="strategyName">Named strategy</param>
    /// <param name="operation">Operation to execute</param>
    /// <returns>Operation result</returns>
    TResult Execute<TResult>(string strategyName, Func<TResult> operation);

    /// <summary>
    /// Register a named resilience strategy for reuse.
    /// </summary>
    /// <param name="name">Strategy name</param>
    /// <param name="options">Resilience configuration</param>
    void RegisterStrategy(string name, ResilienceOptions options);

    /// <summary>
    /// Get statistics for a named strategy (circuit breaker state, retry counts, etc.).
    /// </summary>
    /// <param name="strategyName">Strategy name</param>
    /// <returns>Strategy statistics, or null if not found</returns>
    ResilienceStatistics? GetStatistics(string strategyName);

    /// <summary>
    /// Reset a circuit breaker to closed state.
    /// </summary>
    /// <param name="strategyName">Strategy name</param>
    void ResetCircuitBreaker(string strategyName);

    /// <summary>
    /// Event raised when a resilience policy is triggered (retry, circuit breaker opened, etc.).
    /// </summary>
    event EventHandler<ResilienceEventArgs>? ResilienceEventOccurred;
}
