using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Game.Domain;

/// <summary>The hero on the grid. Holds only the position; run state lives in the <see cref="Interpreter"/>.</summary>
public sealed class Person(Position start)
{
    /// <summary>Current position.</summary>
    public Position Position { get; private set; } = start;

    /// <summary>Moves one cell if the target is inside the grid and free; otherwise the position stays unchanged.</summary>
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
