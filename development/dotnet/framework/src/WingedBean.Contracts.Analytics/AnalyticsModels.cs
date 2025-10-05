using System;
using System.Collections.Generic;

namespace WingedBean.Contracts.Analytics;

/// <summary>
/// Configuration for the analytics service.
/// </summary>
public class AnalyticsConfig
{
    /// <summary>
    /// Whether analytics is enabled by default.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The analytics backend to use.
    /// </summary>
    public AnalyticsBackend Backend { get; set; } = AnalyticsBackend.InMemory;

    /// <summary>
    /// Backend-specific configuration.
    /// </summary>
    public Dictionary<string, object> BackendConfig { get; set; } = new();

    /// <summary>
    /// Whether to scrub personally identifiable information.
    /// </summary>
    public bool ScrubPii { get; set; } = true;

    /// <summary>
    /// Data retention period in days.
    /// </summary>
    public int RetentionDays { get; set; } = 90;

    /// <summary>
    /// Batch size for sending events.
    /// </summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>
    /// Flush interval in seconds.
    /// </summary>
    public int FlushIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to track anonymous users.
    /// </summary>
    public bool TrackAnonymous { get; set; } = true;

    /// <summary>
    /// Whether to enable breadcrumbs.
    /// </summary>
    public bool EnableBreadcrumbs { get; set; } = true;

    /// <summary>
    /// Maximum number of breadcrumbs to keep.
    /// </summary>
    public int MaxBreadcrumbs { get; set; } = 100;
}

/// <summary>
/// Analytics backend types.
/// </summary>
/// <remarks>
/// <para><strong>Recommended Production Configuration (RFC-0030/0033):</strong></para>
/// <list type="bullet">
///   <item><description><strong>Segment:</strong> Multi-destination product analytics (recommended for flexibility)</description></item>
///   <item><description><strong>Application Insights:</strong> Azure-native analytics (recommended for Azure deployments)</description></item>
/// </list>
/// <para>Focus: User behavior, engagement metrics, funnels, retention, business KPIs.</para>
/// <para>Not for system diagnostics (use IDiagnosticsService with Sentry/OTEL instead).</para>
/// </remarks>
public enum AnalyticsBackend
{
    /// <summary>
    /// In-memory storage (for testing/debugging).
    /// </summary>
    InMemory,

    /// <summary>
    /// File-based storage.
    /// </summary>
    File,

    /// <summary>
    /// Application Insights.
    /// </summary>
    /// <remarks>Azure-native analytics backend. Good for Azure-deployed applications.</remarks>
    ApplicationInsights,

    /// <summary>
    /// Segment.com.
    /// </summary>
    /// <remarks>Multi-destination analytics platform. Recommended for flexibility and vendor-agnostic approach.</remarks>
    Segment,    /// <summary>
    /// Mixpanel.
    /// </summary>
    Mixpanel,

    /// <summary>
    /// Custom backend implementation.
    /// </summary>
    Custom
}

/// <summary>
/// Game-specific event types.
/// </summary>
public enum GameEventType
{
    // Player events
    PlayerSpawn,
    PlayerDeath,
    PlayerLevelUp,
    PlayerExperienceGained,

    // Progression events
    LevelStart,
    LevelComplete,
    LevelFail,
    AchievementUnlocked,
    QuestStarted,
    QuestCompleted,
    QuestFailed,

    // Combat events
    EnemyKilled,
    DamageDealt,
    DamageReceived,
    CombatStarted,
    CombatEnded,

    // Economy events
    ItemPurchased,
    ItemUsed,
    ItemEquipped,
    CurrencyEarned,
    CurrencySpent,
    ShopOpened,
    ShopClosed,

    // Social events
    FriendAdded,
    FriendRemoved,
    GuildJoined,
    GuildLeft,
    MessageSent,
    MessageReceived,

    // Performance events
    FpsSample,
    LoadTime,
    FrameTime,
    MemoryUsage,

    // UI events
    ButtonClicked,
    MenuOpened,
    MenuClosed,
    TutorialStarted,
    TutorialCompleted,
    TutorialSkipped,

    // Custom events
    Custom
}

/// <summary>
/// Progression types for tracking player advancement.
/// </summary>
public enum ProgressionType
{
    /// <summary>
    /// Level progression.
    /// </summary>
    Level,

    /// <summary>
    /// Area/zone progression.
    /// </summary>
    Area,

    /// <summary>
    /// World progression.
    /// </summary>
    World,

    /// <summary>
    /// Chapter progression.
    /// </summary>
    Chapter,

    /// <summary>
    /// Achievement progression.
    /// </summary>
    Achievement,

    /// <summary>
    /// Skill progression.
    /// </summary>
    Skill,

    /// <summary>
    /// Custom progression.
    /// </summary>
    Custom
}

/// <summary>
/// Resource flow types for economy tracking.
/// </summary>
public enum ResourceFlowType
{
    /// <summary>
    /// Resource earned/sourced.
    /// </summary>
    Source,

    /// <summary>
    /// Resource spent/sunk.
    /// </summary>
    Sink
}

/// <summary>
/// Push notification event types.
/// </summary>
public enum PushEventType
{
    /// <summary>
    /// Push notification received.
    /// </summary>
    Received,

    /// <summary>
    /// Push notification opened.
    /// </summary>
    Opened,

    /// <summary>
    /// Push notification dismissed.
    /// </summary>
    Dismissed,

    /// <summary>
    /// Push notification action taken.
    /// </summary>
    Action
}

/// <summary>
/// Social interaction types.
/// </summary>
public enum SocialInteractionType
{
    /// <summary>
    /// User liked content.
    /// </summary>
    Like,

    /// <summary>
    /// User shared content.
    /// </summary>
    Share,

    /// <summary>
    /// User commented on content.
    /// </summary>
    Comment,

    /// <summary>
    /// User followed another user.
    /// </summary>
    Follow,

    /// <summary>
    /// User unfollowed another user.
    /// </summary>
    Unfollow,

    /// <summary>
    /// User invited another user.
    /// </summary>
    Invite,

    /// <summary>
    /// User joined a group.
    /// </summary>
    Join,

    /// <summary>
    /// User left a group.
    /// </summary>
    Leave
}

/// <summary>
/// Metric aggregation types.
/// </summary>
public enum MetricType
{
    /// <summary>
    /// Gauge metric (current value).
    /// </summary>
    Gauge,

    /// <summary>
    /// Counter metric (incremental value).
    /// </summary>
    Counter,

    /// <summary>
    /// Timer metric (duration).
    /// </summary>
    Timer,

    /// <summary>
    /// Histogram metric (distribution).
    /// </summary>
    Histogram,

    /// <summary>
    /// Set metric (unique values).
    /// </summary>
    Set
}

/// <summary>
/// Analytics error information.
/// </summary>
public class AnalyticsError
{
    /// <summary>
    /// The error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The error type/class name.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// The stack trace.
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Additional error properties.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// Breadcrumb trail leading to the error.
    /// </summary>
    public List<AnalyticsBreadcrumb> Breadcrumbs { get; set; } = new();

    /// <summary>
    /// Timestamp when the error occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Analytics breadcrumb for error context.
/// </summary>
public class AnalyticsBreadcrumb
{
    /// <summary>
    /// The breadcrumb message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The breadcrumb category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// The breadcrumb level.
    /// </summary>
    public BreadcrumbLevel Level { get; set; } = BreadcrumbLevel.Info;

    /// <summary>
    /// Additional breadcrumb data.
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// Timestamp when the breadcrumb was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Breadcrumb severity levels.
/// </summary>
public enum BreadcrumbLevel
{
    Debug,
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Performance metric information.
/// </summary>
public class PerformanceMetric
{
    /// <summary>
    /// The metric name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The metric value.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// The metric unit.
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Additional metric properties.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// Timestamp when the metric was recorded.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Analytics event data.
/// </summary>
public class AnalyticsEvent
{
    /// <summary>
    /// The event name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The user ID associated with the event.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// The session ID.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// The trace ID for correlation.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Event properties.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// Timestamp when the event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// User traits/properties.
/// </summary>
public class UserTraits
{
    /// <summary>
    /// The user's display name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The user's email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// The user's avatar URL.
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// The user's game level.
    /// </summary>
    public int? Level { get; set; }

    /// <summary>
    /// The user's experience points.
    /// </summary>
    public long? Experience { get; set; }

    /// <summary>
    /// Additional custom traits.
    /// </summary>
    public Dictionary<string, object> CustomTraits { get; set; } = new();
}

/// <summary>
/// Analytics statistics.
/// </summary>
public class AnalyticsStats
{
    /// <summary>
    /// Total number of events tracked.
    /// </summary>
    public long TotalEvents { get; set; }

    /// <summary>
    /// Number of events in current session.
    /// </summary>
    public long SessionEvents { get; set; }

    /// <summary>
    /// Number of users tracked.
    /// </summary>
    public long TotalUsers { get; set; }

    /// <summary>
    /// Number of active users.
    /// </summary>
    public long ActiveUsers { get; set; }

    /// <summary>
    /// Number of identified users.
    /// </summary>
    public long IdentifiedUsers { get; set; }

    /// <summary>
    /// Number of anonymous users.
    /// </summary>
    public long AnonymousUsers { get; set; }

    /// <summary>
    /// Average events per user.
    /// </summary>
    public double AvgEventsPerUser { get; set; }

    /// <summary>
    /// Number of errors tracked.
    /// </summary>
    public long ErrorCount { get; set; }

    /// <summary>
    /// Number of pending events to flush.
    /// </summary>
    public long PendingEvents { get; set; }

    /// <summary>
    /// Last flush timestamp.
    /// </summary>
    public DateTimeOffset? LastFlush { get; set; }

    /// <summary>
    /// Current session start time.
    /// </summary>
    public DateTimeOffset? SessionStart { get; set; }

    /// <summary>
    /// Additional statistics.
    /// </summary>
    public Dictionary<string, object> AdditionalStats { get; set; } = new();
}

/// <summary>
/// Timing token for operation tracking.
/// </summary>
public class TimingToken : IDisposable
{
    private readonly string _operationName;
    private readonly DateTimeOffset _startTime;
    private readonly Action<string, TimeSpan> _completeAction;

    /// <summary>
    /// Creates a new timing token.
    /// </summary>
    public TimingToken(string operationName, Action<string, TimeSpan> completeAction)
    {
        _operationName = operationName;
        _startTime = DateTimeOffset.UtcNow;
        _completeAction = completeAction;
    }

    /// <summary>
    /// Disposes the timing token and records the timing.
    /// </summary>
    public void Dispose()
    {
        var duration = DateTimeOffset.UtcNow - _startTime;
        _completeAction(_operationName, duration);
    }
}
