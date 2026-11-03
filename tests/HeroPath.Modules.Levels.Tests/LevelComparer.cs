using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Tests;

/// <summary>Compares levels by value including the obstacle and item lists (the record compares the lists by reference).</summary>
internal sealed class LevelComparer : IEqualityComparer<LevelDefinition>
{
    public static LevelComparer Instance { get; } = new();

    public bool Equals(LevelDefinition? x, LevelDefinition? y) =>
        ReferenceEquals(x, y)
        || (x is not null && y is not null
            && x with { Obstacles = [], Items = [] } == y with { Obstacles = [], Items = [] }
            && x.Obstacles.SequenceEqual(y.Obstacles)
            && x.Items.SequenceEqual(y.Items));

    public int GetHashCode(LevelDefinition obj) => obj.Id.GetHashCode();
}
