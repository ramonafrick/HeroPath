using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.Infrastructure;
using HeroPath.Shared.Application.Contracts;

namespace HeroPath.Modules.Game.Tests;

internal static class TestLevels
{
    public static LevelDefinition Demo { get; } = new()
    {
        Id = "test-demo",
        Width = 10,
        Height = 10,
        Start = new Position(1, 6),
        Goal = new Position(6, 3),
        Obstacles = [new LevelObstacle(3, 6, ObstacleKind.Tree), new LevelObstacle(4, 3, ObstacleKind.Rock), new LevelObstacle(6, 5, ObstacleKind.Pond)],
    };

    public static LevelDefinition Empty(string id, int width, int height, Position start, Position goal) => new()
    {
        Id = id,
        Width = width,
        Height = height,
        Start = start,
        Goal = goal,
    };

    public static LevelDefinition WithItems(string id, int width, int height, Position start, Position goal, params LevelItem[] items) => new()
    {
        Id = id,
        Width = width,
        Height = height,
        Start = start,
        Goal = goal,
        RequireAllItems = true,
        Items = items,
    };

    public static LevelRepository NewRepository(IReadOnlyList<LevelDefinition> standard, IAppStorage? storage = null) =>
        new(new StorageCustomLevelStore(storage ?? new InMemoryAppStorage()), standard, LevelJson.Read);

    public static LevelProgress NewProgress() => new(NewRepository([Demo, .. StandardLevels.All]));

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
