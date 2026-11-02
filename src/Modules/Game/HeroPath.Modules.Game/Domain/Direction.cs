using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Game.Domain;

public enum Direction
{
    Up,
    Down,
    Left,
    Right,
}

public static class DirectionExtensions
{
    extension(Direction direction)
    {
        public Position Offset => direction switch
        {
            Direction.Up => new(0, -1),
            Direction.Down => new(0, 1),
            Direction.Left => new(-1, 0),
            Direction.Right => new(1, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(direction)),
        };

        public Position MoveFrom(Position position)
        {
            var offset = direction.Offset;
            return new(position.X + offset.X, position.Y + offset.Y);
        }
    }
}
