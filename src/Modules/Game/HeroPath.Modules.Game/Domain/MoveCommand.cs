namespace HeroPath.Modules.Game.Domain;

public sealed class MoveCommand(Direction direction, Guid? id = null) : ICommand
{
    public Guid Id { get; } = id ?? Guid.NewGuid();

    public Direction Direction { get; } = direction;
}
