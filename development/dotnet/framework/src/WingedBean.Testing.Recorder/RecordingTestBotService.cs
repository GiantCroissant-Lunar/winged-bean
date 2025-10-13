using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RulesEngine.Models;

namespace WingedBean.Testing.Recorder;

/// <summary>
/// Background service that generates synthetic terminal activity for recording tests
/// Uses Microsoft RulesEngine to execute JSON-defined test scenarios
/// </summary>
public class RecordingTestBotService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RecordingTestBotService> _logger;
    private readonly BotOutputWriter _outputWriter;
    private readonly RulesEngine.RulesEngine _rulesEngine;
    private readonly Stopwatch _stopwatch = new();
    private int _eventCount = 0;
    private readonly Random _random = new();

    public RecordingTestBotService(
        IConfiguration configuration,
        ILogger<RecordingTestBotService> logger,
        ILogger<BotOutputWriter> outputWriterLogger)
    {
        _configuration = configuration;
        _logger = logger;
        _outputWriter = new BotOutputWriter(outputWriterLogger);

        // Load rules from file or use fallback
        var workflows = LoadRuleWorkflows();
        _rulesEngine = new RulesEngine.RulesEngine(workflows.ToArray(), null);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var sessionId = _configuration["record-session"] ?? "bot-test";
            var duration = _configuration.GetValue<int>("record-duration", 10);

            _logger.LogInformation("[Bot] Starting test bot for session: {SessionId}, duration: {Duration}s",
                sessionId, duration);

            // Small delay to let recording service start
            await Task.Delay(500, stoppingToken);

            _stopwatch.Start();

            // Run until duration elapsed or cancellation
            while (!stoppingToken.IsCancellationRequested && _stopwatch.Elapsed.TotalSeconds < duration)
            {
                var input = new BotRuleInput
                {
                    SessionId = sessionId,
                    ElapsedSeconds = _stopwatch.Elapsed.TotalSeconds,
                    EventCount = _eventCount,
                    Duration = duration,
                    StartTime = DateTime.UtcNow.AddSeconds(-_stopwatch.Elapsed.TotalSeconds)
                };

                await ExecuteRulesAsync(input, stoppingToken);

                // Run at ~10Hz for smooth timing
                await Task.Delay(100, stoppingToken);
            }

            _logger.LogInformation("[Bot] Test bot completed. Events generated: {EventCount}", _eventCount);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[Bot] Test bot cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Bot] Test bot error");
            // Don't throw - let recording continue even if bot fails
        }
    }

    private async Task ExecuteRulesAsync(BotRuleInput input, CancellationToken ct)
    {
        try
        {
            var results = await _rulesEngine.ExecuteAllRulesAsync("BasicRecordingTest", input);

            foreach (var result in results)
            {
                if (result.IsSuccess)
                {
                    // Extract action from rule metadata or use rule name
                    var actionType = result.Rule.RuleName;
                    ExecuteSimpleAction(actionType, input);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Bot] Rule execution error at {ElapsedSeconds}s", input.ElapsedSeconds);
        }
    }

    private void ExecuteSimpleAction(string actionType, BotRuleInput input)
    {
        try
        {
            switch (actionType)
            {
                case "InitialBanner":
                    _outputWriter.WriteBanner("Recording Test Session", input.SessionId);
                    _eventCount++;
                    break;

                case "PlayerSpawn":
                    _outputWriter.WriteGameEvent("Spawn", "Player spawns at (5,5)");
                    _eventCount++;
                    break;

                case "PlayerMovement":
                    var directions = new[] { "north", "south", "east", "west" };
                    var direction = directions[_random.Next(directions.Length)];
                    _outputWriter.WritePlayerMove(direction);
                    _eventCount++;
                    break;

                case "EnemyEncounter":
                    _outputWriter.WriteGameEvent("Enemy", "Enemy detected: Goblin (HP: 20)");
                    _eventCount++;
                    break;

                case "CombatEvents":
                    var damage = _random.Next(5, 15);
                    _outputWriter.WriteCombatEvent("Player", "Goblin", damage);
                    _eventCount++;
                    break;

                case "FinalSummary":
                    var stats = new Dictionary<string, object>
                    {
                        ["Duration"] = $"{input.ElapsedSeconds:F2}s",
                        ["Events"] = _eventCount,
                        ["Status"] = "SUCCESS"
                    };
                    _outputWriter.WriteSummary("Test session complete", stats);
                    _eventCount++;
                    break;

                default:
                    _logger.LogDebug("[Bot] Unknown action: {ActionType}", actionType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Bot] Action execution failed: {ActionType}", actionType);
        }
    }

    private List<Workflow> LoadRuleWorkflows()
    {
        try
        {
            // First try embedded resource
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "WingedBean.Testing.Recorder.recording-test-scenarios.json";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                var workflows = JsonSerializer.Deserialize<List<Workflow>>(json);

                if (workflows != null && workflows.Count > 0)
                {
                    _logger.LogInformation("[Bot] Loaded {Count} workflow(s) from embedded resource", workflows.Count);
                    return workflows;
                }
            }

            // Fallback: try external file (for development/testing)
            var scenarioPath = System.IO.Path.Combine(AppContext.BaseDirectory, "recording-test-scenarios.json");
            if (System.IO.File.Exists(scenarioPath))
            {
                var json = System.IO.File.ReadAllText(scenarioPath);
                var workflows = JsonSerializer.Deserialize<List<Workflow>>(json);

                if (workflows != null && workflows.Count > 0)
                {
                    _logger.LogInformation("[Bot] Loaded {Count} workflow(s) from file", workflows.Count);
                    return workflows;
                }
            }

            _logger.LogWarning("[Bot] No scenario file found, using fallback scenario");
            return GetFallbackWorkflow();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Bot] Failed to load rule workflows, using fallback");
            return GetFallbackWorkflow();
        }
    }

    private List<Workflow> GetFallbackWorkflow()
    {
        // Hardcoded fallback for when JSON can't be loaded
        return new List<Workflow>
        {
            new Workflow
            {
                WorkflowName = "BasicRecordingTest",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        RuleName = "InitialBanner",
                        Expression = "input1.ElapsedSeconds < 0.5"
                    },
                    new Rule
                    {
                        RuleName = "PlayerSpawn",
                        Expression = "input1.ElapsedSeconds >= 1 && input1.ElapsedSeconds < 1.5"
                    },
                    new Rule
                    {
                        RuleName = "PlayerMovement",
                        Expression = "input1.ElapsedSeconds >= 2 && input1.ElapsedSeconds < 8 && (input1.ElapsedSeconds % 1) < 0.1"
                    },
                    new Rule
                    {
                        RuleName = "EnemyEncounter",
                        Expression = "input1.ElapsedSeconds >= 3 && input1.ElapsedSeconds < 3.5"
                    },
                    new Rule
                    {
                        RuleName = "CombatEvents",
                        Expression = "input1.ElapsedSeconds >= 4 && input1.ElapsedSeconds < 7 && (input1.ElapsedSeconds % 0.8) < 0.1"
                    },
                    new Rule
                    {
                        RuleName = "FinalSummary",
                        Expression = "input1.ElapsedSeconds >= 9"
                    }
                }
            }
        };
    }
}
