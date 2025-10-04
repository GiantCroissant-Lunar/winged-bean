namespace WingedBean.Contracts.FigmaSharp;

/// <summary>
/// RGBA color (replaces UnityEngine.Color)
/// </summary>
public struct Color
{
    public float R { get; set; }
    public float G { get; set; }
    public float B { get; set; }
    public float A { get; set; }
    
    public Color(float r, float g, float b, float a = 1.0f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }
    
    public static Color White => new(1, 1, 1, 1);
    public static Color Black => new(0, 0, 0, 1);
    public static Color Transparent => new(0, 0, 0, 0);
    public static Color Red => new(1, 0, 0, 1);
    public static Color Green => new(0, 1, 0, 1);
    public static Color Blue => new(0, 0, 1, 1);
    
    public static bool operator ==(Color a, Color b) => 
        a.R == b.R && a.G == b.G && a.B == b.B && a.A == b.A;
    public static bool operator !=(Color a, Color b) => !(a == b);
    
    public override bool Equals(object? obj) => obj is Color other && this == other;
    public override int GetHashCode() => (R, G, B, A).GetHashCode();
    public override string ToString() => $"RGBA({R:F2}, {G:F2}, {B:F2}, {A:F2})";
}
