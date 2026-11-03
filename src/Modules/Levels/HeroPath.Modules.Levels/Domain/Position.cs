namespace HeroPath.Modules.Levels.Domain;

/// <summary>A cell coordinate. The origin (0,0) is top-left, X grows to the right, Y grows downward.</summary>
public readonly record struct Position(int X, int Y)
{
    /// <summary>Whether this position lies within a grid of the given size.</summary>
    public bool IsInside(int width, int height) => X >= 0 && X < width && Y >= 0 && Y < height;
}
