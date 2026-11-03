namespace HeroPath.Modules.Game.Domain;

/// <summary>
/// A program block. The hierarchy is closed: <see cref="MoveCommand"/> and <see cref="RepeatCommand"/> are the only subclasses.
/// Equality is by identity; <see cref="Id"/> is stable and lets the UI highlight the block.
/// </summary>
public abstract class Command
{
    private protected Command(Guid? id) => Id = id ?? Guid.NewGuid();

    /// <summary>Stable identifier of this block, generated unless given explicitly.</summary>
    public Guid Id { get; }
}
