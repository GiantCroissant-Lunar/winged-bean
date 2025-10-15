namespace WingedBean.Gameplay.Perception.Components;

/// <summary>
/// Entity is perceivable by certain perception types.
/// Determines which perception systems can detect this entity.
/// </summary>
public struct PerceivableComponent
{
    /// <summary>
    /// Which perception types can detect this entity.
    /// e.g., ["vision", "detect_magic"] means visible and detectable by magic sense
    /// </summary>
    public List<string> PerceivableBy { get; set; }
    
    /// <summary>
    /// Optional: Signature strength modifier (1.0 = normal, 2.0 = easier to detect, 0.5 = harder).
    /// Key: perception type, Value: modifier
    /// </summary>
    public Dictionary<string, double> SignatureModifiers { get; set; }

    /// <summary>
    /// Initializes a new perceivable component.
    /// </summary>
    public PerceivableComponent()
    {
        PerceivableBy = new List<string>();
        SignatureModifiers = new Dictionary<string, double>();
    }
}
