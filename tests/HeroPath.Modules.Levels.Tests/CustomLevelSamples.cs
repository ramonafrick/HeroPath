using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Tests;

internal static class CustomLevelSamples
{
    /// <summary>A solvable 5x5 level (shortest solution 8 moves) with an own name.</summary>
    public static LevelDefinition Valid(string id, string? name = null) => new()
    {
        Id = id,
        Name = name ?? $"Level {id}",
        Width = 5,
        Height = 5,
        Start = new Position(0, 4),
        Goal = new Position(4, 0),
        Obstacles = [new LevelObstacle(2, 2, ObstacleKind.Tree)],
        Items = [new LevelItem(1, 4, ItemKind.Strawberry)],
    };

    /// <summary>Valid by the rules, but the goal is walled in.</summary>
    public static LevelDefinition Unsolvable(string id) => Valid(id) with
    {
        Obstacles = [new LevelObstacle(3, 0, ObstacleKind.Rock), new LevelObstacle(4, 1, ObstacleKind.Rock)],
    };

    public static LevelDefinition TooManyStrawberries(string id) => Valid(id) with
    {
        Items = [.. Enumerable.Range(0, 11).Select(i => new LevelItem(i % 5, i / 5, ItemKind.Strawberry))],
    };
}
