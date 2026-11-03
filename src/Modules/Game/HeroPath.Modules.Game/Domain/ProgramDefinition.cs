namespace HeroPath.Modules.Game.Domain;

/// <summary>An immutable, ordered list of top-level blocks that the interpreter runs.</summary>
public sealed class ProgramDefinition
{
    /// <summary>Creates a program with a defensive copy of <paramref name="commands"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The program has more than <see cref="ProgramLimits.MaxProgramCommands"/> blocks in total.</exception>
    public ProgramDefinition(IReadOnlyList<Command> commands)
    {
        var copy = CommandNesting.CopyChecked(commands, nameof(commands));
        if (CommandNesting.CountAll(copy) > ProgramLimits.MaxProgramCommands)
        {
            throw new ArgumentOutOfRangeException(nameof(commands), "The program has too many blocks.");
        }

        Commands = copy;
    }

    /// <summary>A program without blocks.</summary>
    public static ProgramDefinition Empty { get; } = new([]);

    /// <summary>Top-level blocks in execution order.</summary>
    public IReadOnlyList<Command> Commands { get; }
}
