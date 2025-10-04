namespace WingedBean.Contracts.FigmaSharp;

/// <summary>
/// 2D vector (replaces UnityEngine.Vector2)
/// </summary>
public struct Vector2
{
    public float X { get; set; }
    public float Y { get; set; }
    
    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }
    
    public static Vector2 Zero => new(0, 0);
    public static Vector2 One => new(1, 1);
    
    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2 operator *(Vector2 a, float scalar) => new(a.X * scalar, a.Y * scalar);
    public static Vector2 operator /(Vector2 a, float scalar) => new(a.X / scalar, a.Y / scalar);
    
    public static bool operator ==(Vector2 a, Vector2 b) => a.X == b.X && a.Y == b.Y;
    public static bool operator !=(Vector2 a, Vector2 b) => !(a == b);
    
    public override bool Equals(object? obj) => obj is Vector2 other && this == other;
    public override int GetHashCode() => (X, Y).GetHashCode();
    public override string ToString() => $"({X}, {Y})";
}
