namespace WingedBean.Contracts.Scene;

/// <summary>
/// Viewport dimensions (width, height in characters/pixels).
/// </summary>
public readonly struct Viewport
{
    public int Width { get; init; }
    public int Height { get; init; }

    public Viewport(int width, int height)
    {
        Width = width;
        Height = height;
    }
}
