namespace HeroPath.Modules.Game.Domain;

/// <summary>A block that moves the person one cell in <see cref="Direction"/>.</summary>
public sealed class MoveCommand(Direction direction, Guid? id = null) : Command(id)
{
    /// <summary>Direction of the move.</summary>
    public Direction Direction { get; } = direction;
}
