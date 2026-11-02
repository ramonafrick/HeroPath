namespace HeroPath.Modules.Game.Domain;

public sealed class ProgramDefinition
{
    public ProgramDefinition(IReadOnlyList<ICommand> commands)
    {
        var copy = CommandNesting.CopyChecked(commands, nameof(commands));
        if (CommandNesting.DepthOf(copy) > ProgramLimits.MaxRepeatNestingDepth)
        {
            throw new ArgumentOutOfRangeException(nameof(commands), "Repeat blocks are nested too deeply.");
        }

        Commands = copy;
    }

    public static ProgramDefinition Empty { get; } = new([]);

    public IReadOnlyList<ICommand> Commands { get; }
}
