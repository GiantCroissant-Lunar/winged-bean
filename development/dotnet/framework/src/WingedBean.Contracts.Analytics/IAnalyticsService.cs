using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WingedBean.Contracts.Analytics;

/// <summary>
/// Analytics service for tracking user behavior and game telemetry.
/// Provides privacy-first analytics with pluggable backends.
/// </summary>
/// <remarks>
/// <para><strong>Implementation Strategy (RFC-0033):</strong></para>
/// <para>This contract is implemented as an <strong>independent service</strong> for product/business metrics:</para>
/// <list type="bullet">
///   <item><description>Backend: Segment (multi-destination) or Application Insights (Azure-native)</description></item>
///   <item><description>Focus: User behavior, engagement, funnels, retention, business KPIs</description></item>
///   <item><description>Retention: 90 days (business data), separate from system diagnostics</description></item>
/// </list>
/// <para><strong>Separation of Concerns:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Analytics (this service):</strong> Product metrics - what users do (events, funnels, cohorts)</description></item>
///   <item><description><strong>Diagnostics (IDiagnosticsService):</strong> System observability - how system behaves (errors via Sentry, traces via OTEL)</description></item>
/// </list>
/// <para><strong>Error Tracking Note:</strong></para>
/// <para>While this service has <see cref="TrackException"/> for business error events, 
/// system exceptions and crashes should use <c>IDiagnosticsService.RecordException()</c> 
/// which delegates to Sentry (RFC-0034) for proper error tracking.</para>
/// <para>See RFC-0030 for contract specification, RFC-0033 for observability strategy.</para>
/// </remarks>
public interface IAnalyticsService
{
    /// <summary>
    /// Gets whether analytics tracking is currently enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets the current user identifier (anonymous or identified).
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Gets the current session identifier.
    /// </summary>
    string? SessionId { get; }

    /// <summary>
    /// Gets the current trace ID for correlation.
    /// </summary>
    string? TraceId { get; }

    // Core Tracking Methods

    /// <summary>
    /// Enables or disables analytics tracking.
    /// </summary>
    /// <param name="enabled">Whether to enable tracking.</param>
    void SetTrackingEnabled(bool enabled);

    /// <summary>
    /// Identifies a user with their actual identity.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="traits">Optional user traits/properties.</param>
    void Identify(string userId, Dictionary<string, object>? traits = null);

    /// <summary>
    /// Aliases one user ID to another (e.g., anonymous to identified).
    /// </summary>
    /// <param name="previousId">The previous user ID to alias.</param>
    /// <param name="userId">The new user ID.</param>
    void Alias(string previousId, string userId);

    /// <summary>
    /// Resets the user identity (creates new anonymous user).
    /// </summary>
    void Reset();

    /// <summary>
    /// Updates user traits/properties.
    /// </summary>
    /// <param name="traits">User traits to update.</param>
    void SetUserTraits(Dictionary<string, object> traits);

    /// <summary>
    /// Increments a user property by a numeric value.
    /// </summary>
    /// <param name="property">The property name.</param>
    /// <param name="value">The value to increment by.</param>
    void IncrementUserProperty(string property, double value);

    /// <summary>
    /// Appends a value to a user property array.
    /// </summary>
    /// <param name="property">The property name.</param>
    /// <param name="value">The value to append.</param>
    void AppendUserProperty(string property, object value);

    // Event Tracking

    /// <summary>
    /// Tracks a custom event.
    /// </summary>
    /// <param name="eventName">The event name.</param>
    /// <param name="properties">Optional event properties.</param>
    void Track(string eventName, Dictionary<string, object>? properties = null);

    /// <summary>
    /// Tracks a page/screen view.
    /// </summary>
    /// <param name="pageName">The page/screen name.</param>
    /// <param name="properties">Optional page properties.</param>
    void Page(string pageName, Dictionary<string, object>? properties = null);

    /// <summary>
    /// Tracks a screen view (mobile/desktop).
    /// </summary>
    /// <param name="screenName">The screen name.</param>
    /// <param name="properties">Optional screen properties.</param>
    void Screen(string screenName, Dictionary<string, object>? properties = null);

    // Game-Specific Events

    /// <summary>
    /// Tracks a game-specific event.
    /// </summary>
    /// <param name="eventType">The game event type.</param>
    /// <param name="properties">Event properties.</param>
    void TrackGameEvent(GameEventType eventType, Dictionary<string, object>? properties = null);

    /// <summary>
    /// Tracks player progression.
    /// </summary>
    /// <param name="progressionType">The progression type.</param>
    /// <param name="progressionId">The progression identifier.</param>
    /// <param name="properties">Optional progression properties.</param>
    void TrackProgression(ProgressionType progressionType, string progressionId, Dictionary<string, object>? properties = null);

    /// <summary>
    /// Tracks resource events (earn/spend).
    /// </summary>
    /// <param name="resourceType">The resource type.</param>
    /// <param name="resourceId">The resource identifier.</param>
    /// <param name="amount">The amount earned/spent.</param>
    /// <param name="properties">Optional resource properties.</param>
    void TrackResource(ResourceFlowType resourceType, string resourceId, double amount, Dictionary<string, object>? properties = null);

    // Revenue & Economy

    /// <summary>
    /// Tracks revenue from purchases.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="price">The purchase price.</param>
    /// <param name="currency">The currency code.</param>
    /// <param name="properties">Optional purchase properties.</param>
    void TrackRevenue(string productId, decimal price, string currency = "USD", Dictionary<string, object>? properties = null);

    /// <summary>
    /// Tracks in-app purchases.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="price">The purchase price.</param>
    /// <param name="currency">The currency code.</param>
    /// <param name="receipt">The purchase receipt.</param>
    /// <param name="properties">Optional purchase properties.</param>
    void TrackPurchase(string productId, decimal price, string currency = "USD", string? receipt = null, Dictionary<string, object>? properties = null);

    // Funnel Analysis

    /// <summary>
    /// Starts tracking a funnel step.
    /// </summary>
    /// <param name="funnelName">The funnel name.</param>
    /// <param name="stepName">The step name.</param>
    /// <param name="stepNumber">The step number in the funnel.</param>
    /// <param name="properties">Optional step properties.</param>
    void StartFunnel(string funnelName, string stepName, int stepNumber, Dictionary<string, object>? properties = null);

    /// <summary>
    /// Completes a funnel step.
    /// </summary>
    /// <param name="funnelName">The funnel name.</param>
    /// <param name="stepName">The step name.</param>
    /// <param name="properties">Optional completion properties.</param>
    void CompleteFunnel(string funnelName, string stepName, Dictionary<string, object>? properties = null);

    /// <summary>
    /// Abandons a funnel.
    /// </summary>
    /// <param name="funnelName">The funnel name.</param>
    /// <param name="stepName">The step where abandonment occurred.</param>
    /// <param name="properties">Optional abandonment properties.</param>
    void AbandonFunnel(string funnelName, string stepName, Dictionary<string, object>? properties = null);

    // Error & Exception Tracking

    /// <summary>
    /// Tracks an error/exception.
    /// </summary>
    /// <param name="error">The error information.</param>
    void TrackError(AnalyticsError error);

    /// <summary>
    /// Tracks an exception with automatic error details.
    /// </summary>
    /// <param name="exception">The exception to track.</param>
    /// <param name="tags">Optional error tags.</param>
    void TrackException(Exception exception, Dictionary<string, string>? tags = null);

    // Performance & Metrics

    /// <summary>
    /// Tracks a performance metric.
    /// </summary>
    /// <param name="metric">The performance metric.</param>
    void TrackPerformance(PerformanceMetric metric);

    /// <summary>
    /// Tracks timing for an operation.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <param name="duration">The operation duration.</param>
    /// <param name="properties">Optional timing properties.</param>
    void TrackTiming(string operation, TimeSpan duration, Dictionary<string, object>? properties = null);

    /// <summary>
    /// Starts timing an operation.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <returns>A timing token to stop timing.</returns>
    IDisposable StartTiming(string operation);

    // Group/Organization Tracking

    /// <summary>
    /// Associates user with a group/organization.
    /// </summary>
    /// <param name="groupId">The group identifier.</param>
    /// <param name="traits">Optional group traits.</param>
    void Group(string groupId, Dictionary<string, object>? traits = null);

    // Search Tracking

    /// <summary>
    /// Tracks search queries.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="properties">Optional search properties.</param>
    void TrackSearch(string query, Dictionary<string, object>? properties = null);

    // Push Notifications

    /// <summary>
    /// Tracks push notification events.
    /// </summary>
    /// <param name="eventType">The push event type.</param>
    /// <param name="campaignId">The campaign identifier.</param>
    /// <param name="properties">Optional push properties.</param>
    void TrackPush(PushEventType eventType, string? campaignId = null, Dictionary<string, object>? properties = null);

    // Experimentation/Feature Flags

    /// <summary>
    /// Tracks experiment participation.
    /// </summary>
    /// <param name="experimentId">The experiment identifier.</param>
    /// <param name="variant">The variant assigned.</param>
    /// <param name="properties">Optional experiment properties.</param>
    void TrackExperiment(string experimentId, string variant, Dictionary<string, object>? properties = null);

    // Social Features

    /// <summary>
    /// Tracks social interactions.
    /// </summary>
    /// <param name="interactionType">The interaction type.</param>
    /// <param name="target">The interaction target.</param>
    /// <param name="properties">Optional interaction properties.</param>
    void TrackSocial(SocialInteractionType interactionType, string target, Dictionary<string, object>? properties = null);

    // Advanced Features

    /// <summary>
    /// Tracks custom metric with aggregation support.
    /// </summary>
    /// <param name="metricName">The metric name.</param>
    /// <param name="value">The metric value.</param>
    /// <param name="metricType">The metric aggregation type.</param>
    /// <param name="properties">Optional metric properties.</param>
    void TrackMetric(string metricName, double value, MetricType metricType = MetricType.Gauge, Dictionary<string, object>? properties = null);

    /// <summary>
    /// Adds a breadcrumb for error context.
    /// </summary>
    /// <param name="message">The breadcrumb message.</param>
    /// <param name="category">The breadcrumb category.</param>
    /// <param name="level">The breadcrumb level.</param>
    void AddBreadcrumb(string message, string category = "default", BreadcrumbLevel level = BreadcrumbLevel.Info);

    /// <summary>
    /// Clears all breadcrumbs.
    /// </summary>
    void ClearBreadcrumbs();

    // Configuration & Management

    /// <summary>
    /// Flushes any pending analytics events to backends.
    /// </summary>
    /// <returns>A task representing the flush operation.</returns>
    Task FlushAsync();

    /// <summary>
    /// Gets the current analytics configuration.
    /// </summary>
    /// <returns>The analytics configuration.</returns>
    AnalyticsConfig GetConfig();

    /// <summary>
    /// Updates the analytics configuration.
    /// </summary>
    /// <param name="config">The new configuration.</param>
    void UpdateConfig(AnalyticsConfig config);

    /// <summary>
    /// Gets analytics statistics.
    /// </summary>
    /// <returns>Analytics statistics.</returns>
    AnalyticsStats GetStats();

    /// <summary>
    /// Clears all stored analytics data.
    /// </summary>
    void ClearData();
}
