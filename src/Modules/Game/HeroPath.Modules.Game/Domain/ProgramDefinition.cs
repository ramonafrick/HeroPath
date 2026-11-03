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

    /// <summary>Number of all blocks, counting moves and repeats recursively.</summary>
    public int CountAll() => CommandNesting.CountAll(Commands);

    /// <summary>The block with <paramref name="id"/> anywhere in the tree, or <c>null</c>.</summary>
    public Command? Find(Guid id) => Locate(Commands, id, null, [])?.Command;

    /// <summary>
    /// The 1-based hierarchical number of the block, for example <c>[3, 2]</c> for the second block in the body of block 3,
    /// or <c>null</c> when the id is unknown.
    /// </summary>
    public IReadOnlyList<int>? PathOf(Guid id) => Locate(Commands, id, null, [])?.Path;

    /// <summary>The list and index the block currently sits in, or <c>null</c> when the id is unknown.</summary>
    public BlockSlot? SlotOf(Guid id) =>
        Locate(Commands, id, null, []) is { } located ? new BlockSlot(located.ParentId, located.Index) : null;

    /// <summary>
    /// Nesting depth of the list <paramref name="repeatId"/> opens: <c>0</c> for <c>null</c> (top level), <c>1</c> for a top-level repeat.
    /// <c>null</c> when the id is unknown or not a repeat block.
    /// </summary>
    public int? DepthAt(Guid? repeatId)
    {
        if (repeatId is null)
        {
            return 0;
        }

        return Locate(Commands, repeatId.Value, null, []) is { Command: RepeatCommand } located ? located.Path.Count : null;
    }

    private static Located? Locate(IReadOnlyList<Command> commands, Guid id, Guid? parentId, List<int> prefix)
    {
        for (var index = 0; index < commands.Count; index++)
        {
            var command = commands[index];
            List<int> path = [.. prefix, index + 1];
            if (command.Id == id)
            {
                return new Located(command, path, parentId, index);
            }

            if (command is RepeatCommand repeat && Locate(repeat.Body, id, repeat.Id, path) is { } inner)
            {
                return inner;
            }
        }

        return null;
    }

    private sealed record Located(Command Command, IReadOnlyList<int> Path, Guid? ParentId, int Index);
}
