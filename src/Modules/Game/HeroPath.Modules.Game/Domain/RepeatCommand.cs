namespace HeroPath.Modules.Game.Domain;

/// <summary>
/// A block that runs its <see cref="Body"/> <see cref="Count"/> times. Repeats may be nested up to
/// <see cref="ProgramLimits.MaxRepeatNestingDepth"/> levels. An empty body is allowed here and reported by the interpreter.
/// </summary>
public sealed class RepeatCommand : Command
{
    /// <summary>Creates a repeat block with a defensive copy of <paramref name="body"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The count or the nesting depth is outside the allowed limits.</exception>
    public RepeatCommand(int count, IReadOnlyList<Command> body, Guid? id = null)
        : base(id)
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
    }

    /// <summary>Number of iterations, between <see cref="ProgramLimits.MinRepeatCount"/> and <see cref="ProgramLimits.MaxRepeatCount"/>.</summary>
    public int Count { get; }

    /// <summary>Blocks executed in each iteration.</summary>
    public IReadOnlyList<Command> Body { get; }
}
