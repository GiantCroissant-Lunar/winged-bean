using Plate.CrossMilo.Contracts.Analytics;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirebaseAdmin;
using Google.Cloud.Firestore;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using System;

namespace WingedBean.Plugins.Analytics;

/// <summary>
/// Interface for analytics backends.
/// </summary>
public interface IAnalyticsBackend
{
    Task Track(AnalyticsEvent @event);
    Task Identify(string userId, Dictionary<string, object>? traits = null);
    Task Alias(string previousId, string userId);
    Task Reset();
    Task SetUserTraits(Dictionary<string, object> traits);
    Task IncrementUserProperty(string property, double value);
    Task AppendUserProperty(string property, object value);
    Task Group(string groupId, Dictionary<string, object>? traits = null);
    Task FlushAsync();
    Task ClearData();
}

/// <summary>
/// Firebase Analytics backend implementation.
/// </summary>
public class FirebaseAnalyticsBackend : IAnalyticsBackend
{
    private readonly ILogger<FirebaseAnalyticsBackend> _logger;
    private readonly FirestoreDb _firestore;
    private readonly FirebaseApp _firebaseApp;
    private string? _userId;
    private readonly Dictionary<string, object> _userTraits = new();

    public FirebaseAnalyticsBackend(ILogger<FirebaseAnalyticsBackend> logger, string projectId, string? credentialsPath = null)
    {
        _logger = logger;

        try
        {
            // Initialize Firebase App
            var credential = credentialsPath != null
                ? GoogleCredential.FromFile(credentialsPath)
                : GoogleCredential.GetApplicationDefault();

            _firebaseApp = FirebaseApp.Create(new AppOptions
            {
                Credential = credential,
                ProjectId = projectId
            }, Guid.NewGuid().ToString());

            _firestore = FirestoreDb.Create(projectId);
            _logger.LogInformation("Firebase Analytics backend initialized for project: {ProjectId}", projectId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Firebase Analytics backend");
            throw;
        }
    }

    public async Task Track(AnalyticsEvent @event)
    {
        try
        {
            var eventData = new Dictionary<string, object>
            {
                ["event_name"] = @event.Name,
                ["timestamp"] = @event.Timestamp,
                ["user_id"] = _userId,
                ["properties"] = @event.Properties ?? new Dictionary<string, object>(),
                ["user_traits"] = new Dictionary<string, object>(_userTraits)
            };

            // Store in Firestore
            var collection = _firestore.Collection("analytics_events");
            await collection.AddAsync(eventData);

            _logger.LogDebug("Tracked Firebase Analytics event: {EventName}", @event.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track Firebase Analytics event: {EventName}", @event.Name);
        }
    }

    public async Task Identify(string userId, Dictionary<string, object>? traits = null)
    {
        try
        {
            _userId = userId;

            if (traits != null)
            {
                foreach (var kvp in traits)
                {
                    _userTraits[kvp.Key] = kvp.Value;
                }
            }

            // Store user data in Firestore
            var userData = new Dictionary<string, object>
            {
                ["user_id"] = userId,
                ["traits"] = new Dictionary<string, object>(_userTraits),
                ["last_seen"] = DateTimeOffset.UtcNow
            };

            var docRef = _firestore.Collection("analytics_users").Document(userId);
            await docRef.SetAsync(userData, SetOptions.MergeAll);

            _logger.LogDebug("Identified Firebase Analytics user: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to identify Firebase Analytics user: {UserId}", userId);
        }
    }

    public async Task Alias(string previousId, string userId)
    {
        try
        {
            // Create alias record
            var aliasData = new Dictionary<string, object>
            {
                ["previous_id"] = previousId,
                ["new_id"] = userId,
                ["timestamp"] = DateTimeOffset.UtcNow
            };

            var collection = _firestore.Collection("analytics_aliases");
            await collection.AddAsync(aliasData);

            // Update user ID
            await Identify(userId, null);

            _logger.LogDebug("Created Firebase Analytics alias from {PreviousId} to {UserId}", previousId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Firebase Analytics alias from {PreviousId} to {UserId}", previousId, userId);
        }
    }

    public async Task Reset()
    {
        try
        {
            _userId = null;
            _userTraits.Clear();

            // Could optionally clear Firestore data, but typically we keep historical data
            _logger.LogDebug("Reset Firebase Analytics user context");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset Firebase Analytics user context");
        }
    }

    public async Task SetUserTraits(Dictionary<string, object> traits)
    {
        try
        {
            foreach (var kvp in traits)
            {
                _userTraits[kvp.Key] = kvp.Value;
            }

            if (_userId != null)
            {
                // Update user traits in Firestore
                var updateData = new Dictionary<string, object>
                {
                    ["traits"] = new Dictionary<string, object>(_userTraits),
                    ["last_updated"] = DateTimeOffset.UtcNow
                };

                var docRef = _firestore.Collection("analytics_users").Document(_userId);
                await docRef.UpdateAsync(updateData);
            }

            _logger.LogDebug("Updated Firebase Analytics user traits for user: {UserId}", _userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Firebase Analytics user traits for user: {UserId}", _userId);
        }
    }

    public async Task IncrementUserProperty(string property, double value)
    {
        try
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

            await SetUserTraits(new Dictionary<string, object>()); // This will update Firestore

            _logger.LogDebug("Incremented Firebase Analytics user property: {Property} by {Value}", property, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment Firebase Analytics user property: {Property}", property);
        }
    }

    public async Task AppendUserProperty(string property, object value)
    {
        try
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

            await SetUserTraits(new Dictionary<string, object>()); // This will update Firestore

            _logger.LogDebug("Appended Firebase Analytics user property: {Property}", property);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to append Firebase Analytics user property: {Property}", property);
        }
    }

    public async Task Group(string groupId, Dictionary<string, object>? traits = null)
    {
        try
        {
            var groupData = new Dictionary<string, object>
            {
                ["group_id"] = groupId,
                ["user_id"] = _userId,
                ["timestamp"] = DateTimeOffset.UtcNow
            };

            if (traits != null)
            {
                groupData["traits"] = traits;
            }

            var docRef = _firestore.Collection("analytics_groups").Document(groupId);
            await docRef.SetAsync(groupData, SetOptions.MergeAll);

            _logger.LogDebug("Updated Firebase Analytics group: {GroupId}", groupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Firebase Analytics group: {GroupId}", groupId);
        }
    }

    public async Task FlushAsync()
    {
        // Firebase operations are synchronous, but we can implement batch operations here if needed
        _logger.LogDebug("Flushed Firebase Analytics data");
        await Task.CompletedTask;
    }

    public async Task ClearData()
    {
        try
        {
            _userId = null;
            _userTraits.Clear();

            // Note: We don't clear Firestore data as it's typically kept for analytics
            _logger.LogDebug("Cleared Firebase Analytics local data");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear Firebase Analytics data");
        }
    }
}

/// <summary>
/// In-memory analytics backend for testing and development.
/// </summary>
public class InMemoryAnalyticsBackend : IAnalyticsBackend
{
    private readonly List<AnalyticsEvent> _events = new();
    private string? _userId;
    private readonly Dictionary<string, object> _userTraits = new();
    private readonly Dictionary<string, object> _groupTraits = new();

    public Task Track(AnalyticsEvent @event)
    {
        _events.Add(@event);
        return Task.CompletedTask;
    }

    public Task Identify(string userId, Dictionary<string, object>? traits = null)
    {
        _userId = userId;
        if (traits != null)
        {
            foreach (var kvp in traits)
            {
                _userTraits[kvp.Key] = kvp.Value;
            }
        }
        return Task.CompletedTask;
    }

    public Task Alias(string previousId, string userId)
    {
        _userId = userId;
        return Task.CompletedTask;
    }

    public Task Reset()
    {
        _userId = null;
        _userTraits.Clear();
        _groupTraits.Clear();
        return Task.CompletedTask;
    }

    public Task SetUserTraits(Dictionary<string, object> traits)
    {
        foreach (var kvp in traits)
        {
            _userTraits[kvp.Key] = kvp.Value;
        }
        return Task.CompletedTask;
    }

    public Task IncrementUserProperty(string property, double value)
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
        return Task.CompletedTask;
    }

    public Task AppendUserProperty(string property, object value)
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
        return Task.CompletedTask;
    }

    public Task Group(string groupId, Dictionary<string, object>? traits = null)
    {
        if (traits != null)
        {
            foreach (var kvp in traits)
            {
                _groupTraits[kvp.Key] = kvp.Value;
            }
        }
        return Task.CompletedTask;
    }

    public Task FlushAsync()
    {
        // In-memory backend doesn't need flushing
        return Task.CompletedTask;
    }

    public Task ClearData()
    {
        _events.Clear();
        _userId = null;
        _userTraits.Clear();
        _groupTraits.Clear();
        return Task.CompletedTask;
    }

    // Helper methods for testing
    public IReadOnlyList<AnalyticsEvent> GetEvents() => _events.AsReadOnly();
    public string? GetUserId() => _userId;
    public IReadOnlyDictionary<string, object> GetUserTraits() => _userTraits;
    public IReadOnlyDictionary<string, object> GetGroupTraits() => _groupTraits;
}
