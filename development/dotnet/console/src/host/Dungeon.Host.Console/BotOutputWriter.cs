using System.Text;
using Microsoft.Extensions.Logging;

namespace Dungeon.Host.Console;

/// <summary>
/// Handles formatted output generation for the recording test bot
/// </summary>
public class BotOutputWriter
{
    private readonly ILogger<BotOutputWriter> _logger;
    private readonly object _lock = new();
    private int _frameCounter = 0;
    private readonly Random _random = new();

    public BotOutputWriter(ILogger<BotOutputWriter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Write a formatted banner
    /// </summary>
    public void WriteBanner(string title, string? sessionId = null)
    {
        lock (_lock)
        {
            var separator = new string('=', 60);
            System.Console.WriteLine(separator);
            System.Console.WriteLine($"  {title}");
            if (!string.IsNullOrEmpty(sessionId))
            {
                System.Console.WriteLine($"  Session: {sessionId}");
            }
            System.Console.WriteLine(separator);
            System.Console.WriteLine();
        }
    }

    /// <summary>
    /// Write a game event with frame number
    /// </summary>
    public void WriteGameEvent(string eventType, string message)
    {
        lock (_lock)
        {
            _frameCounter++;
            var timestamp = DateTime.UtcNow.ToString("HH:mm:ss.fff");
            System.Console.WriteLine($"[Frame {_frameCounter:D3}] [{timestamp}] {eventType}: {message}");
        }
    }

    /// <summary>
    /// Write a player movement event
    /// </summary>
    public void WritePlayerMove(string direction)
    {
        var directions = new[] { "north", "south", "east", "west" };
        var actualDirection = direction == "random"
            ? directions[_random.Next(directions.Length)]
            : direction;

        WriteGameEvent("Player", $"moves {actualDirection}");
    }

    /// <summary>
    /// Write a combat event
    /// </summary>
    public void WriteCombatEvent(string attacker, string defender, int damage)
    {
        WriteGameEvent("Combat", $"{attacker} attacks {defender} for {damage} damage");
    }

    /// <summary>
    /// Write a summary section
    /// </summary>
    public void WriteSummary(string message, Dictionary<string, object>? stats = null)
    {
        lock (_lock)
        {
            System.Console.WriteLine();
            System.Console.WriteLine(new string('=', 60));
            System.Console.WriteLine($"  {message}");

            if (stats != null)
            {
                System.Console.WriteLine();
                foreach (var (key, value) in stats)
                {
                    System.Console.WriteLine($"  {key}: {value}");
                }
            }

            System.Console.WriteLine(new string('=', 60));
        }
    }

    /// <summary>
    /// Write a simple message
    /// </summary>
    public void WriteMessage(string message)
    {
        lock (_lock)
        {
            System.Console.WriteLine(message);
        }
    }

    /// <summary>
    /// Write ASCII art or multi-line content
    /// </summary>
    public void WriteMultiLine(string content)
    {
        lock (_lock)
        {
            System.Console.WriteLine(content);
        }
    }

    /// <summary>
    /// Get current frame count
    /// </summary>
    public int GetFrameCount() => _frameCounter;

    /// <summary>
    /// Reset frame counter (for testing)
    /// </summary>
    public void ResetFrameCounter() => _frameCounter = 0;
}
