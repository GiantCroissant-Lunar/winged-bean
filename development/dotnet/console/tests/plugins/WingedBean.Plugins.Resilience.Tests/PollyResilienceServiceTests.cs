using FluentAssertions;
using Microsoft.Extensions.Logging;
using WingedBean.Contracts.Resilience;
using WingedBean.Plugins.Resilience;
using Xunit;

namespace WingedBean.Plugins.Resilience.Tests;

public class PollyResilienceServiceTests
{
    private readonly PollyResilienceService _service;
    private readonly List<ResilienceEventArgs> _events = new();

    public PollyResilienceServiceTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<PollyResilienceService>();
        _service = new PollyResilienceService(logger);
        _service.ResilienceEventOccurred += (_, e) => _events.Add(e);
    }

    [Fact]
    public async Task ExecuteAsync_SuccessfulOperation_ReturnsResult()
    {
        // Arrange
        var expectedResult = 42;

        // Act
        var result = await _service.ExecuteAsync(async ct =>
        {
            await Task.Delay(10, ct);
            return expectedResult;
        });

        // Assert
        result.Should().Be(expectedResult);
        _events.Should().ContainSingle(e => e.EventType == ResilienceEventType.Success);
    }

    [Fact]
    public async Task ExecuteAsync_FailingOperation_RetriesAndThrows()
    {
        // Arrange
        var attemptCount = 0;

        // Act
        var act = async () => await _service.ExecuteAsync(async ct =>
        {
            attemptCount++;
            await Task.CompletedTask;
            throw new InvalidOperationException($"Attempt {attemptCount}");
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        attemptCount.Should().Be(4); // 1 initial + 3 retries (default)
        _events.Should().Contain(e => e.EventType == ResilienceEventType.RetryAttempt);
    }

    [Fact]
    public async Task RegisterStrategy_CanReuseNamedStrategy()
    {
        // Arrange
        var strategyName = "fast-retry";
        var options = new ResilienceOptions
        {
            Retry = new RetryOptions
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromMilliseconds(5),
                BackoffType = BackoffType.Constant
            }
        };

        _service.RegisterStrategy(strategyName, options);

        var attemptCount = 0;

        // Act
        var act = async () => await _service.ExecuteAsync(strategyName, async ct =>
        {
            attemptCount++;
            await Task.CompletedTask;
            throw new InvalidOperationException($"Attempt {attemptCount}");
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        attemptCount.Should().Be(3); // 1 initial + 2 retries
    }

    [Fact]
    public async Task GetStatistics_TracksExecutionMetrics()
    {
        // Arrange
        var strategyName = "test-stats";
        _service.RegisterStrategy(strategyName, new ResilienceOptions
        {
            Retry = new RetryOptions { MaxRetryAttempts = 2 }
        });

        // Act - Execute some operations
        await _service.ExecuteAsync(strategyName, async ct =>
        {
            await Task.CompletedTask;
            return "success";
        });

        try
        {
            await _service.ExecuteAsync(strategyName, async ct =>
            {
                await Task.CompletedTask;
                throw new InvalidOperationException("Test failure");
            });
        }
        catch { /* Expected */ }

        // Assert
        var stats = _service.GetStatistics(strategyName);
        stats.Should().NotBeNull();
        stats!.StrategyName.Should().Be(strategyName);
        stats.TotalExecutions.Should().Be(2);
        stats.SuccessfulExecutions.Should().Be(1);
        stats.FailedExecutions.Should().Be(1);
        stats.TotalRetries.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Execute_SynchronousOperation_Works()
    {
        // Arrange & Act
        var result = _service.Execute(() =>
        {
            return 123;
        });

        // Assert
        result.Should().Be(123);
    }

    [Fact]
    public async Task ExecuteAsync_VoidOperation_Succeeds()
    {
        // Arrange
        var executed = false;

        // Act
        await _service.ExecuteAsync(async ct =>
        {
            await Task.Delay(10, ct);
            executed = true;
        });

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public void ResilienceEventOccurred_IsRaisedOnRetry()
    {
        // Arrange
        var attemptCount = 0;

        // Act
        try
        {
            _service.Execute<int>(() =>
            {
                attemptCount++;
                throw new InvalidOperationException("Test");
            });
        }
        catch { /* Expected */ }

        // Assert
        _events.Should().Contain(e => e.EventType == ResilienceEventType.RetryAttempt);
        _events.Should().Contain(e => e.AttemptNumber > 0);
    }

    [Fact]
    public async Task ResetCircuitBreaker_ResetsCircuitState()
    {
        // Arrange
        var strategyName = "reset-test";
        var options = new ResilienceOptions
        {
            CircuitBreaker = new CircuitBreakerOptions
            {
                FailureThreshold = 2,
                DurationOfBreak = TimeSpan.FromSeconds(10),
                MinimumThroughput = 1
            },
            Retry = null
        };

        _service.RegisterStrategy(strategyName, options);

        // Trigger circuit breaker
        for (int i = 0; i < 3; i++)
        {
            try
            {
                await _service.ExecuteAsync(strategyName, async ct => throw new InvalidOperationException());
            }
            catch { /* Expected */ }
        }

        // Act
        _service.ResetCircuitBreaker(strategyName);

        // Assert
        var stats = _service.GetStatistics(strategyName);
        stats?.CircuitState.Should().Be(CircuitState.Closed);
        _events.Should().Contain(e =>
            e.EventType == ResilienceEventType.CircuitBreakerClosed &&
            e.Message == "Circuit breaker manually reset");
    }
}
