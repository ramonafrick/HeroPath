using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Game.Domain;

/// <summary>Direction of a move. Y grows downward, so Up decreases Y.</summary>
public enum Direction
{
    Up,
    Down,
    Left,
    Right,
}

/// <summary>Grid offsets for <see cref="Direction"/>.</summary>
public static class DirectionExtensions
{
    extension(Direction direction)
    {
        /// <summary>Unit offset of the direction.</summary>
        public Position Offset => direction switch
        {
            Direction.Up => new(0, -1),
            Direction.Down => new(0, 1),
            Direction.Left => new(-1, 0),
            Direction.Right => new(1, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(direction)),
        };

        /// <summary>The position one cell from <paramref name="position"/> in this direction; not checked against the grid.</summary>
        public Position MoveFrom(Position position)
        {
            var offset = direction.Offset;
            return new(position.X + offset.X, position.Y + offset.Y);
        }
    }
}
