namespace HeroPath.Modules.Levels.Domain;

/// <summary>An item placed on a cell of a level.</summary>
public sealed record LevelItem(int X, int Y, ItemKind Kind)
{
    /// <summary>The cell the item lies on.</summary>
    public Position GetPosition() => new(X, Y);
}
