using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RulesEngine.Models;

namespace Dungeon.Host.Console;

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

    public RecordingTestBotService(
        IConfiguration configuration,
        ILogger<RecordingTestBotService> logger,
        ILogger<BotOutputWriter> outputWriterLogger)
    {
        _configuration = configuration;
        _logger = logger;
        _outputWriter = new BotOutputWriter(outputWriterLogger);

        // Load rules from embedded JSON
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
                if (result.IsSuccess && result.Rule.Actions?.OnSuccess != null)
                {
                    await ExecuteActionAsync(result.Rule.Actions.OnSuccess, input, ct);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Bot] Rule execution error at {ElapsedSeconds}s", input.ElapsedSeconds);
        }
    }

    private Task ExecuteActionAsync(
        RuleActions.ActionInfo actionInfo,
        BotRuleInput input,
        CancellationToken ct)
    {
        try
        {
            var context = actionInfo.Context;
            if (context == null) return Task.CompletedTask;

            // Parse context as JSON to get action parameters
            var contextJson = context.ToString() ?? "{}";
            var actionParams = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(contextJson);

            if (actionParams == null) return Task.CompletedTask;

            var actionType = actionParams.GetValueOrDefault("Type").GetString() ?? actionInfo.Name;

            switch (actionType)
            {
                case "Banner":
                    ExecuteBannerAction(actionParams, input);
                    break;

                case "GameEvent":
                    ExecuteGameEventAction(actionParams, input);
                    break;

                case "Summary":
                    ExecuteSummaryAction(actionParams, input);
                    break;

                default:
                    _logger.LogWarning("[Bot] Unknown action type: {ActionType}", actionType);
                    break;
            }

            _eventCount++;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Bot] Action execution failed: {ActionName}", actionInfo.Name);
        }

        return Task.CompletedTask;
    }

    private void ExecuteBannerAction(Dictionary<string, JsonElement> parameters, BotRuleInput input)
    {
        var title = parameters.GetValueOrDefault("Title").GetString() ?? "Test Session";
        var sessionId = parameters.GetValueOrDefault("SessionId").GetString();

        // Replace {{SessionId}} token
        if (sessionId != null && sessionId.Contains("{{SessionId}}"))
        {
            sessionId = input.SessionId;
        }

        _outputWriter.WriteBanner(title, sessionId);
    }

    private void ExecuteGameEventAction(Dictionary<string, JsonElement> parameters, BotRuleInput input)
    {
        var eventType = parameters.GetValueOrDefault("EventType").GetString() ?? "Event";
        var message = parameters.GetValueOrDefault("Message").GetString() ?? "Unknown event";

        // Handle different event types with varied output
        switch (eventType)
        {
            case "PlayerMove":
                var directions = new[] { "north", "south", "east", "west" };
                var direction = directions[Random.Shared.Next(directions.Length)];
                _outputWriter.WritePlayerMove(direction);
                break;

            case "Combat":
                var damage = Random.Shared.Next(5, 15);
                _outputWriter.WriteCombatEvent("Player", "Enemy", damage);
                break;

            default:
                _outputWriter.WriteGameEvent(eventType, message);
                break;
        }
    }

    private void ExecuteSummaryAction(Dictionary<string, JsonElement> parameters, BotRuleInput input)
    {
        var message = parameters.GetValueOrDefault("Message").GetString() ?? "Session Complete";

        var stats = new Dictionary<string, object>
        {
            ["Duration"] = $"{input.ElapsedSeconds:F2}s",
            ["Events"] = _eventCount,
            ["Status"] = "SUCCESS"
        };

        _outputWriter.WriteSummary(message, stats);
    }

    private List<Workflow> LoadRuleWorkflows()
    {
        try
        {
            // Try to load from embedded resource
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Dungeon.Host.Console.recording-test-scenarios.json";

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

            _logger.LogWarning("[Bot] Embedded resource not found, trying file...");

            // Fallback: try to load from file
            var scenarioPath = Path.Combine(AppContext.BaseDirectory, "recording-test-scenarios.json");
            if (File.Exists(scenarioPath))
            {
                var json = File.ReadAllText(scenarioPath);
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
                WorkflowName = "FallbackTest",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        RuleName = "FallbackBanner",
                        Expression = "input1.ElapsedSeconds < 0.5",
                        SuccessEvent = "10",
                        Actions = new RuleActions
                        {
                            OnSuccess = new RuleActions.ActionInfo
                            {
                                Name = "PrintBanner",
                                Context = JsonSerializer.Serialize(new
                                {
                                    Type = "Banner",
                                    Title = "Recording Test (Fallback)",
                                    SessionId = "{{SessionId}}"
                                })
                            }
                        }
                    },
                    new Rule
                    {
                        RuleName = "FallbackEvents",
                        Expression = "input1.ElapsedSeconds >= 1 && (input1.ElapsedSeconds % 2) < 0.2",
                        SuccessEvent = "20",
                        Actions = new RuleActions
                        {
                            OnSuccess = new RuleActions.ActionInfo
                            {
                                Name = "PrintGameEvent",
                                Context = JsonSerializer.Serialize(new
                                {
                                    Type = "GameEvent",
                                    EventType = "GameEvent",
                                    Message = "Test event"
                                })
                            }
                        }
                    }
                }
            }
        };
    }
}
