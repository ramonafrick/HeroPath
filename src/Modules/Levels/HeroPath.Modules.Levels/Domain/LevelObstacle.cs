namespace HeroPath.Modules.Levels.Domain;

/// <summary>An obstacle placed on a cell of a level.</summary>
public sealed record LevelObstacle(int X, int Y, ObstacleKind Kind)
{
    /// <summary>The cell the obstacle lies on.</summary>
    public Position GetPosition() => new(X, Y);
}
