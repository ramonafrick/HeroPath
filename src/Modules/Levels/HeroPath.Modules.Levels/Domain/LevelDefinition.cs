namespace HeroPath.Modules.Levels.Domain;

public sealed record LevelDefinition
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public string Id { get; init; } = string.Empty;

    public string? Name { get; init; }

    public int Width { get; init; } = LevelLimits.DefaultGridSize;

    public int Height { get; init; } = LevelLimits.DefaultGridSize;

    public Position Start { get; init; }

    public Position Goal { get; init; }

    public bool RequireAllItems { get; init; }

    public IReadOnlyList<LevelObstacle> Obstacles { get; init; } = [];

    public IReadOnlyList<LevelItem> Items { get; init; } = [];

    public Grid ToGrid() => new(Width, Height, Start, Goal, Obstacles, Items, RequireAllItems);
}
