using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WingedBean.Contracts.Analytics;
using Xunit;

namespace WingedBean.Plugins.Analytics.Tests;

/// <summary>
/// Unit tests for AnalyticsService.
/// </summary>
public class AnalyticsServiceTests
{
    private readonly AnalyticsService _service;
    private readonly InMemoryAnalyticsBackend _backend;

    public AnalyticsServiceTests()
    {
        var config = new AnalyticsConfig
        {
            Enabled = true,
            Backend = AnalyticsBackend.InMemory,
            ScrubPii = true,
            MaxBreadcrumbs = 10
        };

        _backend = new InMemoryAnalyticsBackend();
        var logger = NullLogger<AnalyticsService>.Instance;
        _service = new AnalyticsService(logger, config, _backend);
    }

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        _service.IsEnabled.Should().BeTrue();
        _service.UserId.Should().BeNull();
        _service.SessionId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Identify_SetsUserId()
    {
        var userId = "test-user-123";
        var traits = new Dictionary<string, object> { ["name"] = "Test User" };

        _service.Identify(userId, traits);

        _service.UserId.Should().Be(userId);
        _backend.GetUserId().Should().Be(userId);
        _backend.GetUserTraits().Should().ContainKey("name").WhoseValue.Should().Be("Test User");
    }

    [Fact]
    public void Track_CustomEvent_RecordsEvent()
    {
        var eventName = "test_event";
        var properties = new Dictionary<string, object> { ["key"] = "value" };

        _service.Track(eventName, properties);

        var events = _backend.GetEvents();
        events.Should().HaveCount(1);
        events[0].Name.Should().Be(eventName);
        events[0].Properties.Should().ContainKey("key").WhoseValue.Should().Be("value");
    }

    [Fact]
    public void TrackGameEvent_RecordsCorrectly()
    {
        _service.TrackGameEvent(GameEventType.LevelComplete);

        var events = _backend.GetEvents();
        events.Should().HaveCount(1);
        events[0].Name.Should().Be("Game: LevelComplete");
        events[0].Properties.Should().ContainKey("eventType").WhoseValue.Should().Be("LevelComplete");
    }

    [Fact]
    public void TrackProgression_RecordsCorrectly()
    {
        _service.TrackProgression(ProgressionType.Level, "tutorial_level_1");

        var events = _backend.GetEvents();
        events.Should().HaveCount(1);
        events[0].Name.Should().Be("Progression: Level");
        events[0].Properties.Should().ContainKey("progressionId").WhoseValue.Should().Be("tutorial_level_1");
    }

    [Fact]
    public void TrackResource_RecordsCorrectly()
    {
        _service.TrackResource(ResourceFlowType.Source, "gold", 100.0);

        var events = _backend.GetEvents();
        events.Should().HaveCount(1);
        events[0].Name.Should().Be("Resource: Source");
        events[0].Properties.Should().ContainKey("resourceId").WhoseValue.Should().Be("gold");
        events[0].Properties.Should().ContainKey("amount").WhoseValue.Should().Be(100.0);
    }

    [Fact]
    public void TrackRevenue_RecordsCorrectly()
    {
        _service.TrackRevenue("sword_upgrade", 9.99m, "USD");

        var events = _backend.GetEvents();
        events.Should().HaveCount(1);
        events[0].Name.Should().Be("Revenue");
        events[0].Properties.Should().ContainKey("productId").WhoseValue.Should().Be("sword_upgrade");
        events[0].Properties.Should().ContainKey("price").WhoseValue.Should().Be(9.99m);
        events[0].Properties.Should().ContainKey("currency").WhoseValue.Should().Be("USD");
    }

    [Fact]
    public void TrackPurchase_RecordsCorrectly()
    {
        var receipt = "receipt_123";
        _service.TrackPurchase("magic_potion", 4.99m, "EUR", receipt);

        var events = _backend.GetEvents();
        events.Should().HaveCount(1);
        events[0].Name.Should().Be("Purchase");
        events[0].Properties.Should().ContainKey("receipt").WhoseValue.Should().Be(receipt);
    }

    [Fact]
    public void StartFunnel_RecordsCorrectly()
    {
        _service.StartFunnel("purchase_flow", "product_view", 1);

        var events = _backend.GetEvents();
        events.Should().HaveCount(1);
        events[0].Name.Should().Be("Funnel Step");
        events[0].Properties.Should().ContainKey("funnelName").WhoseValue.Should().Be("purchase_flow");
        events[0].Properties.Should().ContainKey("stepName").WhoseValue.Should().Be("product_view");
        events[0].Properties.Should().ContainKey("action").WhoseValue.Should().Be("start");
    }

    [Fact]
    public void TrackException_RecordsError()
    {
        var exception = new InvalidOperationException("Test exception");

        _service.TrackException(exception);

        var events = _backend.GetEvents();
        events.Should().HaveCount(1);
        events[0].Name.Should().Be("Error");
        events[0].Properties.Should().ContainKey("message").WhoseValue.Should().Be("Test exception");
        events[0].Properties.Should().ContainKey("type").WhoseValue.Should().Be("System.InvalidOperationException");
    }

    [Fact]
    public void TrackPerformance_RecordsCorrectly()
    {
        var metric = new PerformanceMetric
        {
            Name = "load_time",
            Value = 150.5,
            Unit = "ms",
            Properties = new Dictionary<string, object> { ["scene"] = "main_menu" }
        };

        _service.TrackPerformance(metric);

        var events = _backend.GetEvents();
        events.Should().HaveCount(1);
        events[0].Name.Should().Be("Performance");
        events[0].Properties.Should().ContainKey("name").WhoseValue.Should().Be("load_time");
        events[0].Properties.Should().ContainKey("value").WhoseValue.Should().Be(150.5);
        events[0].Properties.Should().ContainKey("unit").WhoseValue.Should().Be("ms");
    }

    [Fact]
    public void StartTiming_ReturnsDisposableToken()
    {
        using var token = _service.StartTiming("test_operation");

        token.Should().NotBeNull();
        token.Should().BeAssignableTo<IDisposable>();
    }

    [Fact]
    public void AddBreadcrumb_IncreasesBreadcrumbCount()
    {
        _service.AddBreadcrumb("Test breadcrumb", "test");

        var breadcrumbs = _backend.GetBreadcrumbs();
        breadcrumbs.Should().HaveCount(1);
        breadcrumbs[0].Message.Should().Be("Test breadcrumb");
        breadcrumbs[0].Category.Should().Be("test");
    }

    [Fact]
    public void ClearBreadcrumbs_RemovesAllBreadcrumbs()
    {
        _service.AddBreadcrumb("Breadcrumb 1");
        _service.AddBreadcrumb("Breadcrumb 2");

        _service.ClearBreadcrumbs();

        var breadcrumbs = _backend.GetBreadcrumbs();
        breadcrumbs.Should().BeEmpty();
    }

    [Fact]
    public void Reset_ClearsUserData()
    {
        _service.Identify("user123");
        _service.SetUserTraits(new Dictionary<string, object> { ["key"] = "value" });

        _service.Reset();

        _service.UserId.Should().BeNull();
        _backend.GetUserTraits().Should().BeEmpty();
        _service.SessionId.Should().NotBeNullOrEmpty(); // New session created
    }

    [Fact]
    public void SetTrackingEnabled_DisablesTracking()
    {
        _service.SetTrackingEnabled(false);

        _service.IsEnabled.Should().BeFalse();

        // Events should not be recorded when disabled
        _service.Track("test_event");
        _backend.GetEvents().Should().BeEmpty();
    }

    [Fact]
    public void IncrementUserProperty_IncreasesValue()
    {
        _service.Identify("user123");
        _service.IncrementUserProperty("level", 5.0);

        _backend.GetUserTraits().Should().ContainKey("level").WhoseValue.Should().Be(5.0);

        _service.IncrementUserProperty("level", 3.0);
        _backend.GetUserTraits().Should().ContainKey("level").WhoseValue.Should().Be(8.0);
    }

    [Fact]
    public void AppendUserProperty_AddsToArray()
    {
        _service.Identify("user123");
        _service.AppendUserProperty("achievements", "first_kill");

        var traits = _backend.GetUserTraits();
        traits.Should().ContainKey("achievements");

        var achievements = traits["achievements"] as List<object>;
        achievements.Should().NotBeNull();
        achievements.Should().Contain("first_kill");

        _service.AppendUserProperty("achievements", "level_up");
        achievements = (traits["achievements"] as List<object>)!;
        achievements.Should().HaveCount(2);
    }

    [Fact]
    public void GetStats_ReturnsStatistics()
    {
        _service.Identify("user123");

        var stats = _service.GetStats();

        stats.Should().NotBeNull();
        stats.IdentifiedUsers.Should().Be(1);
        stats.AnonymousUsers.Should().Be(0);
        stats.TotalUsers.Should().Be(1);
    }
}
