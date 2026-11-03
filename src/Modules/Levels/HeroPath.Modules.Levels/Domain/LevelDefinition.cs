namespace HeroPath.Modules.Levels.Domain;

/// <summary>
/// Plain data of a level as stored in JSON. It is not validated on construction; use
/// <c>LevelValidator</c> before <see cref="ToGrid"/>.
/// </summary>
public sealed record LevelDefinition
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>Stable identifier, lowercase letters, digits and hyphens. Standard levels map it to a resx name.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Display name of a custom level; <see langword="null"/> for standard levels.</summary>
    public string? Name { get; init; }

    public int Width { get; init; } = LevelLimits.DefaultGridSize;

    public int Height { get; init; } = LevelLimits.DefaultGridSize;

    public Position Start { get; init; }

    public Position Goal { get; init; }

    /// <summary>When true, all strawberries must be collected before the goal counts.</summary>
    public bool RequireAllItems { get; init; }

    public IReadOnlyList<LevelObstacle> Obstacles { get; init; } = [];

    public IReadOnlyList<LevelItem> Items { get; init; } = [];

    /// <summary>Builds the immutable grid. Throws <see cref="ArgumentException"/> if the definition is invalid.</summary>
    public Grid ToGrid() => new(Width, Height, Start, Goal, Obstacles, Items, RequireAllItems);
}
