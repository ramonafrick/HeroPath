namespace HeroPath.Modules.Levels.Domain;

public sealed record LevelItem(int X, int Y, ItemKind Kind)
{
    public Position GetPosition() => new(X, Y);
}
