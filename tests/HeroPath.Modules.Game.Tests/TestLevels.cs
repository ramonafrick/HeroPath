using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Game.Tests;

internal static class TestLevels
{
    public static Grid DemoGrid() => new(
        10,
        10,
        new Position(1, 6),
        new Position(6, 3),
        [new LevelObstacle(3, 6, ObstacleKind.Tree), new LevelObstacle(4, 3, ObstacleKind.Rock), new LevelObstacle(6, 5, ObstacleKind.Pond)],
        [],
        false);

    public static Direction[] Directions(GameSession session) =>
        [.. session.Program.Commands.OfType<MoveCommand>().Select(c => c.Direction)];

    public static void Fill(GameSession session, params Direction[] directions)
    {
        foreach (var direction in directions)
        {
            session.Add(direction);
        }
    }

    /// <summary>The shortest solution of the demo level: Up, Up, Right x5, Up.</summary>
    public static Direction[] DemoSolution { get; } =
        [Direction.Up, Direction.Up, Direction.Right, Direction.Right, Direction.Right, Direction.Right, Direction.Right, Direction.Up];
}
