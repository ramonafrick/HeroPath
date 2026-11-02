using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Game.Domain;

public sealed class Person(Position start)
{
    public Position Position { get; private set; } = start;

    public MoveResult Move(Direction direction, Grid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);

        var target = direction.MoveFrom(Position);
        if (!grid.IsInside(target))
        {
            return new MoveResult(MoveOutcome.OutOfBounds, Position);
        }

        if (grid.GetObstacle(target) is { } obstacle)
        {
            return new MoveResult(MoveOutcome.Blocked, Position, obstacle);
        }

        Position = target;
        return new MoveResult(MoveOutcome.Moved, target);
    }
}
