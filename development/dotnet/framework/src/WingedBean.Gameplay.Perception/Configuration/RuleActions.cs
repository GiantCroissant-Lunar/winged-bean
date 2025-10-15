using System.Text.Json.Serialization;

namespace WingedBean.Gameplay.Perception.Configuration;

/// <summary>
/// Actions to take when a perception rule succeeds.
/// Used with Microsoft RulesEngine.
/// </summary>
public class RuleActions
{
    [JsonPropertyName("onSuccess")]
    public RuleActionDetails? OnSuccess { get; set; }
}

/// <summary>
/// Details of rule actions.
/// </summary>
public class RuleActionDetails
{
    [JsonPropertyName("grant_perception")]
    public List<string>? GrantPerception { get; set; }
    
    [JsonPropertyName("remove_perception")]
    public List<string>? RemovePerception { get; set; }
    
    [JsonPropertyName("modify_range")]
    public Dictionary<string, int>? ModifyRange { get; set; }
    
    [JsonPropertyName("modify_perceivability")]
    public Dictionary<string, double>? ModifyPerceivability { get; set; }
}
