using System;
using System.Collections.Generic;

namespace Dungeon.Host.Console;

/// <summary>
/// Input context for bot rule evaluation
/// </summary>
public class BotRuleInput
{
    /// <summary>
    /// Session identifier
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Seconds elapsed since recording started
    /// </summary>
    public double ElapsedSeconds { get; set; }

    /// <summary>
    /// Number of events generated so far
    /// </summary>
    public int EventCount { get; set; }

    /// <summary>
    /// Total recording duration in seconds
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// Recording start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
