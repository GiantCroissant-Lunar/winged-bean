# WingedBean.Plugins.Resilience

Polly-based resilience service providing retry, circuit breaker, timeout, and rate limiting patterns for the WingedBean framework.

## Features

- **Retry Policies**: Configurable retry with exponential/linear/constant backoff
- **Circuit Breaker**: Prevents cascading failures with automatic recovery
- **Timeout**: Configurable operation timeouts (optimistic/pessimistic)
- **Statistics**: Real-time metrics and event tracking
- **Named Strategies**: Reusable resilience configurations

## Installation

The plugin is automatically loaded via ALC (AssemblyLoadContext) discovery when the `.plugin.json` manifest is present in the plugins directory.

## Usage

### Basic Execution with Default Policies

```csharp
var resilience = registry.Get<IResilienceService>();

// Execute with default retry + circuit breaker
var result = await resilience.ExecuteAsync(async ct =>
{
    return await httpClient.GetStringAsync("https://api.example.com/data", ct);
});
```

### Named Strategies

```csharp
// Register a named strategy
resilience.RegisterStrategy("http-retry", new ResilienceOptions
{
    Retry = new RetryOptions
    {
        MaxRetryAttempts = 5,
        Delay = TimeSpan.FromSeconds(1),
        BackoffType = BackoffType.Exponential,
        UseJitter = true
    },
    CircuitBreaker = new CircuitBreakerOptions
    {
        FailureThreshold = 3,
        DurationOfBreak = TimeSpan.FromSeconds(30)
    }
});

// Use the named strategy
var data = await resilience.ExecuteAsync("http-retry", async ct =>
{
    return await FetchDataAsync(ct);
});
```

### Custom Options (Ad-hoc)

```csharp
var options = new ResilienceOptions
{
    Retry = new RetryOptions
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromMilliseconds(500),
        BackoffType = BackoffType.Linear
    },
    Timeout = new TimeoutOptions
    {
        Timeout = TimeSpan.FromSeconds(10),
        Strategy = TimeoutStrategy.Pessimistic
    }
};

var result = await resilience.ExecuteAsync(options, async ct =>
{
    return await SomeOperationAsync(ct);
});
```

### Statistics and Monitoring

```csharp
// Get statistics for a strategy
var stats = resilience.GetStatistics("http-retry");
Console.WriteLine($"Total Executions: {stats.TotalExecutions}");
Console.WriteLine($"Success Rate: {stats.SuccessfulExecutions}/{stats.TotalExecutions}");
Console.WriteLine($"Circuit State: {stats.CircuitState}");

// Subscribe to resilience events
resilience.ResilienceEventOccurred += (sender, e) =>
{
    if (e.EventType == ResilienceEventType.RetryAttempt)
    {
        Console.WriteLine($"Retry attempt {e.AttemptNumber} for {e.StrategyName}");
    }
    else if (e.EventType == ResilienceEventType.CircuitBreakerOpened)
    {
        Console.WriteLine($"Circuit breaker opened for {e.StrategyName}: {e.Message}");
    }
};
```

### Circuit Breaker Management

```csharp
// Manually reset a circuit breaker
resilience.ResetCircuitBreaker("http-retry");
```

## Configuration Options

### RetryOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MaxRetryAttempts` | `int` | `3` | Maximum number of retry attempts |
| `Delay` | `TimeSpan` | `1s` | Base delay between retries |
| `BackoffType` | `BackoffType` | `Exponential` | Backoff strategy (Constant/Linear/Exponential) |
| `UseJitter` | `bool` | `true` | Add jitter to prevent thundering herd |

### CircuitBreakerOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `FailureThreshold` | `int` | `5` | Consecutive failures before opening |
| `DurationOfBreak` | `TimeSpan` | `30s` | How long circuit stays open |
| `SamplingDuration` | `TimeSpan` | `10s` | Window for failure sampling |
| `MinimumThroughput` | `int` | `10` | Minimum requests before circuit activates |
| `FailureRatio` | `double` | `0.5` | Failure ratio threshold (0.0-1.0) |

### TimeoutOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Timeout` | `TimeSpan` | `30s` | Operation timeout |
| `Strategy` | `TimeoutStrategy` | `Optimistic` | Optimistic or Pessimistic |

## Event Types

- `RetryAttempt`: Retry attempt started
- `RetryExhausted`: All retries exhausted
- `CircuitBreakerOpened`: Circuit breaker opened
- `CircuitBreakerClosed`: Circuit breaker closed
- `CircuitBreakerHalfOpened`: Circuit breaker testing recovery
- `Timeout`: Operation timed out
- `Success`: Operation succeeded
- `Failure`: Operation failed

## Architecture

The resilience service is built on **Polly v8**, the industry-standard resilience library for .NET. It provides:

- **Pipeline composition**: Retry → Circuit Breaker → Timeout
- **Thread-safe statistics**: Concurrent execution tracking
- **Event-driven monitoring**: Real-time resilience events
- **Strategy caching**: Reusable named pipelines

## Testing

Tests are located in `WingedBean.Plugins.Resilience.Tests` and cover:
- ✅ Successful operations
- ✅ Retry behavior with configurable attempts
- ✅ Named strategy registration and reuse
- ✅ Statistics tracking
- ✅ Circuit breaker state management
- ✅ Event notifications
- ✅ Synchronous and asynchronous operations

Run tests:
```bash
dotnet test WingedBean.Plugins.Resilience.Tests
```

## Dependencies

- **Polly** 8.5.0 - Core resilience library
- **Polly.Extensions** 8.5.0 - Additional extensions
- **Microsoft.Extensions.Logging.Abstractions** - Logging integration

## Plugin Metadata

```json
{
  "id": "wingedbean.plugins.resilience",
  "name": "Resilience Plugin",
  "version": "1.0.0",
  "provides": ["WingedBean.Contracts.Resilience.IResilienceService"]
}
```

## License

MIT
