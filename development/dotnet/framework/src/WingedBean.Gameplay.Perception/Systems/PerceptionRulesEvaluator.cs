using Arch.Core;
using RulesEngine.Models;
using System.Text.Json;
using WingedBean.Gameplay.Perception.Components;
using WingedBean.Gameplay.Perception.Configuration;

namespace WingedBean.Gameplay.Perception.Systems;

/// <summary>
/// Uses RulesEngine to evaluate which perception capabilities an entity should have.
/// Loads rules from perception-rules.json and grants perceptions based on entity attributes.
/// </summary>
public class PerceptionRulesEvaluator
{
    private readonly RulesEngine.RulesEngine _rulesEngine;
    private readonly PerceptionConfiguration _config;

    /// <summary>
    /// Creates a new perception rules evaluator.
    /// </summary>
    /// <param name="rulesJsonPath">Path to perception-rules.json</param>
    /// <param name="config">Perception configuration</param>
    public PerceptionRulesEvaluator(string rulesJsonPath, PerceptionConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        
        if (!File.Exists(rulesJsonPath))
        {
            throw new FileNotFoundException($"Perception rules file not found: {rulesJsonPath}");
        }

        var rulesJson = File.ReadAllText(rulesJsonPath);
        var workflows = JsonSerializer.Deserialize<List<Workflow>>(rulesJson);
        
        if (workflows == null || workflows.Count == 0)
        {
            throw new InvalidOperationException("No workflows found in rules file");
        }

        _rulesEngine = new RulesEngine.RulesEngine(workflows.ToArray());
    }

    /// <summary>
    /// Evaluates rules and grants perception capabilities to entity.
    /// </summary>
    /// <param name="world">Arch ECS world</param>
    /// <param name="entity">Entity to evaluate</param>
    /// <param name="entityData">Entity data for rule evaluation (race, class, level, etc.)</param>
    public void EvaluateAndGrantPerceptions(World world, Entity entity, Dictionary<string, object> entityData)
    {
        if (world == null) throw new ArgumentNullException(nameof(world));
        if (entityData == null) throw new ArgumentNullException(nameof(entityData));

        // Execute rules
        var results = _rulesEngine.ExecuteAllRulesAsync("PerceptionRules", entityData).Result;
        
        // Collect granted perceptions
        var grantedPerceptions = new HashSet<string>();
        var rangeModifiers = new Dictionary<string, int>();

        foreach (var result in results)
        {
            if (result.IsSuccess && result.ActionResult?.Output != null)
            {
                try
                {
                    var actions = JsonSerializer.Deserialize<Configuration.RuleActions>(result.ActionResult.Output.ToString() ?? "{}");
                    
                    if (actions?.OnSuccess?.GrantPerception != null)
                    {
                        foreach (var perception in actions.OnSuccess.GrantPerception)
                        {
                            grantedPerceptions.Add(perception);
                        }
                    }
                    
                    if (actions?.OnSuccess?.RemovePerception != null)
                    {
                        foreach (var perception in actions.OnSuccess.RemovePerception)
                        {
                            grantedPerceptions.Remove(perception);
                        }
                    }
                    
                    if (actions?.OnSuccess?.ModifyRange != null)
                    {
                        foreach (var kvp in actions.OnSuccess.ModifyRange)
                        {
                            rangeModifiers[kvp.Key] = kvp.Value;
                        }
                    }
                }
                catch (JsonException)
                {
                    // Skip invalid action results
                    continue;
                }
            }
        }

        // Apply perceptions to entity
        if (!world.Has<PerceptionCapabilitiesComponent>(entity))
        {
            world.Add(entity, new PerceptionCapabilitiesComponent());
        }

        ref var capabilities = ref world.Get<PerceptionCapabilitiesComponent>(entity);
        capabilities.PerceptionTypes = grantedPerceptions.ToList();
        
        // Load definitions with any range modifiers applied
        capabilities.Definitions.Clear();
        foreach (var perceptionType in grantedPerceptions)
        {
            var baseDef = _config.GetDefinition(perceptionType);
            if (baseDef != null)
            {
                var modifiedDef = CloneDefinition(baseDef);
                
                if (rangeModifiers.TryGetValue(perceptionType, out var newRange))
                {
                    modifiedDef.DefaultRange = newRange;
                }
                
                capabilities.Definitions[perceptionType] = modifiedDef;
            }
        }
    }

    private static PerceptionDefinition CloneDefinition(PerceptionDefinition source)
    {
        // Deep clone the definition
        var json = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize<PerceptionDefinition>(json) 
            ?? throw new InvalidOperationException("Failed to clone perception definition");
    }
}
