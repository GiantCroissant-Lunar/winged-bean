using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Plate.CrossMilo.Contracts.Analytics.Services;
using Plate.CrossMilo.Contracts.Analytics;

namespace WingedBean.Plugins.Analytics;

/// <summary>
/// Comprehensive analytics service implementation supporting multiple backends.
/// </summary>
public class AnalyticsService : IService
{
    private readonly ILogger<AnalyticsService> _logger;
    private readonly AnalyticsConfig _config;
    private readonly IAnalyticsBackend _backend;
    private readonly object _lock = new();

    // Current state
    private bool _isEnabled = true;
    private string? _userId;
    private string? _sessionId;
    private string? _traceId;
    private readonly Dictionary<string, object> _userTraits = new();
    private readonly List<AnalyticsBreadcrumb> _breadcrumbs = new();

    public AnalyticsService(
        ILogger<AnalyticsService> logger,
        AnalyticsConfig config,
        IAnalyticsBackend backend)
    {
        _logger = logger;
        _config = config;
        _backend = backend;

        _isEnabled = config.Enabled;
        _sessionId = Guid.NewGuid().ToString("N");

        _logger.LogInformation("Analytics service initialized with backend: {Backend}", config.Backend);
    }

    public bool IsEnabled => _isEnabled;
    public string? UserId => _userId;
    public string? SessionId => _sessionId;
    public string? TraceId => _traceId;

    public void SetTrackingEnabled(bool enabled)
    {
        lock (_lock)
        {
            _isEnabled = enabled;
            _logger.LogInformation("Analytics tracking {State}", enabled ? "enabled" : "disabled");
        }
    }

    public void Identify(string userId, Dictionary<string, object>? traits = null)
    {
        if (!CheckEnabled()) return;

        lock (_lock)
        {
            _userId = userId;
            if (traits != null)
            {
                foreach (var kvp in traits)
                {
                    _userTraits[kvp.Key] = kvp.Value;
                }
            }

            _backend.Identify(userId, traits);
            _logger.LogDebug("User identified: {UserId}", userId);
        }
    }

    public void Alias(string previousId, string userId)
    {
        if (!CheckEnabled()) return;

        lock (_lock)
        {
            _backend.Alias(previousId, userId);
            _logger.LogDebug("User aliased: {PreviousId} -> {UserId}", previousId, userId);
        }
    }

    public void Reset()
    {
        if (!CheckEnabled()) return;

        lock (_lock)
        {
            _userId = null;
            _userTraits.Clear();
            _sessionId = Guid.NewGuid().ToString("N");
            _backend.Reset();
            _logger.LogDebug("Analytics reset - new session: {SessionId}", _sessionId);
        }
    }

    public void SetUserTraits(Dictionary<string, object> traits)
    {
        if (!CheckEnabled()) return;

        lock (_lock)
        {
            foreach (var kvp in traits)
            {
                _userTraits[kvp.Key] = kvp.Value;
            }
            _backend.SetUserTraits(traits);
        }
    }

    public void IncrementUserProperty(string property, double value)
    {
        if (!CheckEnabled()) return;

        lock (_lock)
        {
            if (_userTraits.TryGetValue(property, out var current))
            {
                if (current is double currentDouble)
                {
                    _userTraits[property] = currentDouble + value;
                }
                else if (current is int currentInt)
                {
                    _userTraits[property] = currentInt + (int)value;
                }
                else if (current is long currentLong)
                {
                    _userTraits[property] = currentLong + (long)value;
                }
            }
            else
            {
                _userTraits[property] = value;
            }

            _backend.IncrementUserProperty(property, value);
        }
    }

    public void AppendUserProperty(string property, object value)
    {
        if (!CheckEnabled()) return;

        lock (_lock)
        {
            if (!_userTraits.TryGetValue(property, out var current) || current == null)
            {
                _userTraits[property] = new List<object> { value };
            }
            else if (current is List<object> list)
            {
                list.Add(value);
            }
            else
            {
                _userTraits[property] = new List<object> { current, value };
            }

            _backend.AppendUserProperty(property, value);
        }
    }

    public void Track(string eventName, Dictionary<string, object>? properties = null)
    {
        if (!CheckEnabled()) return;

        var eventData = CreateEvent(eventName, properties);
        _backend.Track(eventData);
        _logger.LogTrace("Event tracked: {EventName}", eventName);
    }

    public void Page(string pageName, Dictionary<string, object>? properties = null)
    {
        if (!CheckEnabled()) return;

        var props = properties ?? new Dictionary<string, object>();
        props["page"] = pageName;
        Track("Page Viewed", props);
    }

    public void Screen(string screenName, Dictionary<string, object>? properties = null)
    {
        if (!CheckEnabled()) return;

        var props = properties ?? new Dictionary<string, object>();
        props["screen"] = screenName;
        Track("Screen Viewed", props);
    }

    public void TrackGameEvent(GameEventType eventType, Dictionary<string, object>? properties = null)
    {
        if (!CheckEnabled()) return;

        var eventName = $"Game: {eventType}";
        var props = properties ?? new Dictionary<string, object>();
        props["eventType"] = eventType.ToString();
        Track(eventName, props);
    }

    public void TrackProgression(ProgressionType progressionType, string progressionId, Dictionary<string, object>? properties = null)
    {
        if (!CheckEnabled()) return;

        var eventName = $"Progression: {progressionType}";
        var props = properties ?? new Dictionary<string, object>();
        props["progressionType"] = progressionType.ToString();
        props["progressionId"] = progressionId;
        Track(eventName, props);
    }

    public void TrackResource(ResourceFlowType resourceType, string resourceId, double amount, Dictionary<string, object>? properties = null)
    {
        if (!CheckEnabled()) return;

        var eventName = $"Resource: {resourceType}";
        var props = properties ?? new Dictionary<string, object>();
        props["resourceType"] = resourceType.ToString();
        props["resourceId"] = resourceId;
        props["amount"] = amount;
        Track(eventName, props);
    }

    public void TrackRevenue(string productId, decimal price, string currency = "USD", Dictionary<string, object>? properties = null)
    {
        if (!CheckEnabled()) return;

        var props = properties ?? new Dictionary<string, object>();
        props["productId"] = productId;
        props["price"] = price;
        props["currency"] = currency;
        Track("Revenue", props);
    }

    public void TrackPurchase(string productId, decimal price, string currency = "USD", string? receipt = null, Dictionary<string, object>? properties = null)
    {
        if (!CheckEnabled()) return;

        var props = properties ?? new Dictionary<string, object>();
        props["productId"] = productId;
        props["price"] = price;
        props["currency"] = currency;
        if (receipt != null) props["receipt"] = receipt;
        Track("Purchase", props);
    }

    public void StartFunnel(string funnelName, string stepName, int stepNumber, Dictionary<string, object>? properties = null)
    {
        if (!CheckEnabled()) return;

        var props = properties ?? new Dictionary<string, object>();
        props["funnelName"] = funnelName;
        props["stepName"] = stepName;
        props["stepNumber"] = stepNumber;
        props["action"] = "start";
        Track("Funnel Step", props);
    }

    public void CompleteFunnel(string funnelName, string stepName, Dictionary<string, object>? properties = null)
    {
        if (!CheckEnabled()) return;

        var props = properties ?? new Dictionary<string, object>();
        props["funnelName"] = funnelName;
        props["stepName"] = stepName;
        props["action"] = "complete";
        Track("Funnel Step", props);
    }

    public void AbandonFunnel(string funnelName, string stepName, Dictionary<string, object>? properties = null)
    {
        if (!CheckEnabled()) return;

        var props = properties ?? new Dictionary<string, object>();
        props["funnelName"] = funnelName;
        props["stepName"] = stepName;
        props["action"] = "abandon";
        Track("Funnel Step", props);
    }

    public void TrackError(AnalyticsError error)
    {
        if (!CheckEnabled()) return;

        var props = new Dictionary<string, object>
        {
            ["message"] = error.Message,
            ["timestamp"] = error.Timestamp
        };

        if (error.Type != null) props["type"] = error.Type;
        if (error.StackTrace != null) props["stackTrace"] = error.StackTrace;

        foreach (var kvp in error.Properties)
        {
            props[kvp.Key] = kvp.Value;
        }

        Track("Error", props);
        _logger.LogWarning("Error tracked: {Message}", error.Message);
    }

    public void TrackException(Exception exception, Dictionary<string, string>? tags = null)
    {
        if (!CheckEnabled()) return;

        var error = new AnalyticsError
        {
            Message = exception.Message,
            Type = exception.GetType().FullName,
            StackTrace = exception.StackTrace,
            Properties = tags?.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value) ?? new Dictionary<string, object>()
        };

        TrackError(error);
    }

    public void TrackPerformance(PerformanceMetric metric)
    {
        if (!CheckEnabled()) return;

        var props = new Dictionary<string, object>
        {
            ["name"] = metric.Name,
            ["value"] = metric.Value,
            ["unit"] = metric.Unit,
            ["timestamp"] = metric.Timestamp
        };

        foreach (var kvp in metric.Properties)
        {
            props[kvp.Key] = kvp.Value;
        }

        Track("Performance", props);
    }

    public void TrackTiming(string operation, TimeSpan duration, Dictionary<string, object>? properties = null)
    {
        if (!CheckEnabled()) return;

        var metric = new PerformanceMetric
        {
            Name = operation,
            Value = duration.TotalMilliseconds,
            Unit = "ms",
            Properties = properties ?? new Dictionary<string, object>(),
            Timestamp = DateTimeOffset.UtcNow
        };

        TrackPerformance(metric);
    }

    public IDisposable StartTiming(string operation)
    {
        return new TimingToken(operation, (op, duration) => TrackTiming(op, duration));
    }

    public void Group(string groupId, Dictionary<string, object>? traits = null)
    {
        if (!CheckEnabled()) return;

        _backend.Group(groupId, traits);
        _logger.LogDebug("User grouped: {GroupId}", groupId);
    }

    public void TrackSearch(string query, Dictionary<string, object>? properties = null)
    {
        if (!CheckEnabled()) return;

        var props = properties ?? new Dictionary<string, object>();
        props["query"] = query;
        Track("Search", props);
    }

    public void TrackPush(PushEventType eventType, string? campaignId = null, Dictionary<string, object>? properties = null)
    {
        if (!CheckEnabled()) return;

        var props = properties ?? new Dictionary<string, object>();
        props["eventType"] = eventType.ToString();
        if (campaignId != null) props["campaignId"] = campaignId;
        Track("Push Notification", props);
    }

    public void TrackExperiment(string experimentId, string variant, Dictionary<string, object>? properties = null)
    {
        if (!CheckEnabled()) return;

        var props = properties ?? new Dictionary<string, object>();
        props["experimentId"] = experimentId;
        props["variant"] = variant;
        Track("Experiment", props);
    }

    public void TrackSocial(SocialInteractionType interactionType, string target, Dictionary<string, object>? properties = null)
    {
        if (!CheckEnabled()) return;

        var props = properties ?? new Dictionary<string, object>();
        props["interactionType"] = interactionType.ToString();
        props["target"] = target;
        Track("Social", props);
    }

    public void TrackMetric(string metricName, double value, MetricType metricType = MetricType.Gauge, Dictionary<string, object>? properties = null)
    {
        if (!CheckEnabled()) return;

        var props = properties ?? new Dictionary<string, object>();
        props["metricType"] = metricType.ToString();
        Track($"Metric: {metricName}", props);
    }

    public void AddBreadcrumb(string message, string category = "default", BreadcrumbLevel level = BreadcrumbLevel.Info)
    {
        if (!CheckEnabled()) return;

        lock (_lock)
        {
            var breadcrumb = new AnalyticsBreadcrumb
            {
                Message = message,
                Category = category,
                Level = level,
                Timestamp = DateTimeOffset.UtcNow
            };

            _breadcrumbs.Add(breadcrumb);

            // Keep breadcrumbs within limit
            while (_breadcrumbs.Count > _config.MaxBreadcrumbs)
            {
                _breadcrumbs.RemoveAt(0);
            }
        }
    }

    public void ClearBreadcrumbs()
    {
        lock (_lock)
        {
            _breadcrumbs.Clear();
        }
    }

    public async Task FlushAsync()
    {
        await _backend.FlushAsync();
        _logger.LogDebug("Analytics flushed");
    }

    public AnalyticsConfig GetConfig()
    {
        return _config;
    }

    public void UpdateConfig(AnalyticsConfig config)
    {
        // Note: In a real implementation, this might restart the service
        _logger.LogWarning("Config update requested but not implemented");
    }

    public AnalyticsStats GetStats()
    {
        return new AnalyticsStats
        {
            // These would be tracked in a real implementation
            TotalEvents = 0,
            SessionEvents = 0,
            TotalUsers = _userId != null ? 1 : 0,
            IdentifiedUsers = _userId != null ? 1 : 0,
            AnonymousUsers = _userId == null ? 1 : 0,
            AvgEventsPerUser = 0,
            ErrorCount = 0,
            PendingEvents = 0,
            SessionStart = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
    }

    public void ClearData()
    {
        lock (_lock)
        {
            _userTraits.Clear();
            _breadcrumbs.Clear();
            _backend.ClearData();
            _logger.LogInformation("Analytics data cleared");
        }
    }

    private bool CheckEnabled()
    {
        if (!_isEnabled)
        {
            _logger.LogTrace("Analytics disabled - skipping operation");
            return false;
        }
        return true;
    }

    private AnalyticsEvent CreateEvent(string eventName, Dictionary<string, object>? properties = null)
    {
        return new AnalyticsEvent
        {
            Name = eventName,
            UserId = _userId,
            SessionId = _sessionId,
            TraceId = _traceId,
            Properties = properties ?? new Dictionary<string, object>(),
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}
