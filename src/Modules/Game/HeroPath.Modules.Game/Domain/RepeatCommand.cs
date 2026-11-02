namespace HeroPath.Modules.Game.Domain;

public sealed class RepeatCommand : ICommand
{
    public RepeatCommand(int count, IReadOnlyList<ICommand> body, Guid? id = null)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, ProgramLimits.MinRepeatCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, ProgramLimits.MaxRepeatCount);

        var copy = CommandNesting.CopyChecked(body, nameof(body));
        if (CommandNesting.DepthOf(copy) + 1 > ProgramLimits.MaxRepeatNestingDepth)
        {
            throw new ArgumentOutOfRangeException(nameof(body), "Repeat blocks are nested too deeply.");
        }

        Count = count;
        Body = copy;
        Id = id ?? Guid.NewGuid();
    }

    public Guid Id { get; }

    public int Count { get; }

    public IReadOnlyList<ICommand> Body { get; }
}
