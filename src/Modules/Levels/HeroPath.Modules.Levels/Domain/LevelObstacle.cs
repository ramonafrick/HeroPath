namespace HeroPath.Modules.Levels.Domain;

public sealed record LevelObstacle(int X, int Y, ObstacleKind Kind)
{
    public Position GetPosition() => new(X, Y);
}
