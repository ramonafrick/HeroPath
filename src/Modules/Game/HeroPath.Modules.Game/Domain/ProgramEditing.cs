namespace HeroPath.Modules.Game.Domain;

/// <summary>
/// Pure edit operations on an immutable <see cref="ProgramDefinition"/>. Each operation rebuilds the affected path of the
/// tree (a <see cref="RepeatCommand"/> keeps its id) and returns the new program. Invalid requests, and requests that would
/// change nothing, return <c>null</c> (or <c>false</c>) instead of throwing, so the UI can call them with any user input.
/// Slot indexes refer to the program as it is before the operation.
/// </summary>
public static class ProgramEditing
{
    /// <summary>
    /// Whether <paramref name="block"/> (new to the program) fits into <paramref name="slot"/>: the slot exists, the program stays
    /// within <see cref="ProgramLimits.MaxProgramCommands"/> and the nesting depth stays within <see cref="ProgramLimits.MaxRepeatNestingDepth"/>.
    /// </summary>
    public static bool CanInsert(ProgramDefinition program, Command block, BlockSlot slot)
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(block);

        return program.Find(block.Id) is null
            && program.CountAll() + CommandNesting.CountAll([block]) <= ProgramLimits.MaxProgramCommands
            && FitsAt(program, block, slot);
    }

    /// <summary>Inserts a new block at <paramref name="slot"/>, or returns <c>null</c> when <see cref="CanInsert"/> is false.</summary>
    public static ProgramDefinition? Insert(ProgramDefinition program, Command block, BlockSlot slot)
    {
        if (!CanInsert(program, block, slot))
        {
            return null;
        }

        return Edit(program, slot.ParentId, list =>
        {
            list.Insert(slot.Index, block);
            return true;
        });
    }

    /// <summary>Removes the block with its body, or returns <c>null</c> for an unknown id.</summary>
    public static ProgramDefinition? Remove(ProgramDefinition program, Guid id)
    {
        ArgumentNullException.ThrowIfNull(program);

        if (program.SlotOf(id) is not { } at)
        {
            return null;
        }

        return Edit(program, at.ParentId, list =>
        {
            list.RemoveAt(at.Index);
            return true;
        });
    }

    /// <summary>
    /// Moves an existing block (with its body) to <paramref name="slot"/>. Returns <c>null</c> for an unknown id, a slot that
    /// does not exist, a repeat moved into its own body, a depth above <see cref="ProgramLimits.MaxRepeatNestingDepth"/>, or a
    /// move that leaves the block where it is.
    /// </summary>
    public static ProgramDefinition? Move(ProgramDefinition program, Guid id, BlockSlot slot)
    {
        ArgumentNullException.ThrowIfNull(program);

        if (program.SlotOf(id) is not { } from || program.Find(id) is not { } block)
        {
            return null;
        }

        var sameList = slot.ParentId == from.ParentId;
        var index = sameList && slot.Index > from.Index ? slot.Index - 1 : slot.Index;
        if (sameList && index == from.Index)
        {
            return null;
        }

        return Remove(program, id) is { } without ? Insert(without, block, new BlockSlot(slot.ParentId, index)) : null;
    }

    /// <summary>Whether <see cref="Move"/> would change the program.</summary>
    public static bool CanMove(ProgramDefinition program, Guid id, BlockSlot slot) => Move(program, id, slot) is not null;

    /// <summary>
    /// Moves the block one place up among its siblings. As the first block of a repeat body it leaves the body and lands directly
    /// before the repeat block. Returns <c>null</c> for the first top-level block.
    /// </summary>
    public static ProgramDefinition? MoveUp(ProgramDefinition program, Guid id) =>
        UpTarget(program, id) is { } slot ? Move(program, id, slot) : null;

    /// <summary>
    /// Moves the block one place down among its siblings. As the last block of a repeat body it leaves the body and lands directly
    /// after the repeat block. Returns <c>null</c> for the last top-level block.
    /// </summary>
    public static ProgramDefinition? MoveDown(ProgramDefinition program, Guid id) =>
        DownTarget(program, id) is { } slot ? Move(program, id, slot) : null;

    /// <summary>Whether <see cref="MoveUp"/> would change the program.</summary>
    public static bool CanMoveUp(ProgramDefinition program, Guid id) => MoveUp(program, id) is not null;

    /// <summary>Whether <see cref="MoveDown"/> would change the program.</summary>
    public static bool CanMoveDown(ProgramDefinition program, Guid id) => MoveDown(program, id) is not null;

    /// <summary>
    /// Sets the count of a repeat block. Returns <c>null</c> for an unknown id, a block that is not a repeat, a count outside
    /// <see cref="ProgramLimits.MinRepeatCount"/> to <see cref="ProgramLimits.MaxRepeatCount"/> or an unchanged count.
    /// </summary>
    public static ProgramDefinition? SetRepeatCount(ProgramDefinition program, Guid id, int count)
    {
        ArgumentNullException.ThrowIfNull(program);

        if (count is < ProgramLimits.MinRepeatCount or > ProgramLimits.MaxRepeatCount
            || program.SlotOf(id) is not { } at
            || program.Find(id) is not RepeatCommand repeat
            || repeat.Count == count)
        {
            return null;
        }

        return Edit(program, at.ParentId, list =>
        {
            list[at.Index] = new RepeatCommand(count, repeat.Body, repeat.Id);
            return true;
        });
    }

    private static BlockSlot? UpTarget(ProgramDefinition program, Guid id)
    {
        ArgumentNullException.ThrowIfNull(program);

        if (program.SlotOf(id) is not { } at)
        {
            return null;
        }

        if (at.Index > 0)
        {
            return at with { Index = at.Index - 1 };
        }

        return at.ParentId is { } parentId ? program.SlotOf(parentId) : null;
    }

    private static BlockSlot? DownTarget(ProgramDefinition program, Guid id)
    {
        ArgumentNullException.ThrowIfNull(program);

        if (program.SlotOf(id) is not { } at || ListOf(program, at.ParentId) is not { } siblings)
        {
            return null;
        }

        if (at.Index < siblings.Count - 1)
        {
            return at with { Index = at.Index + 2 };
        }

        return at.ParentId is { } parentId && program.SlotOf(parentId) is { } parentSlot
            ? parentSlot with { Index = parentSlot.Index + 1 }
            : null;
    }

    private static bool FitsAt(ProgramDefinition program, Command block, BlockSlot slot) =>
        program.DepthAt(slot.ParentId) is { } depth
        && ListOf(program, slot.ParentId) is { } list
        && slot.Index >= 0
        && slot.Index <= list.Count
        && depth + CommandNesting.DepthOf([block]) <= ProgramLimits.MaxRepeatNestingDepth;

    private static IReadOnlyList<Command>? ListOf(ProgramDefinition program, Guid? parentId) => parentId is null
        ? program.Commands
        : (program.Find(parentId.Value) as RepeatCommand)?.Body;

    private static ProgramDefinition? Edit(ProgramDefinition program, Guid? parentId, Func<List<Command>, bool> edit) =>
        EditList(program.Commands, parentId, edit) is { } commands ? new ProgramDefinition(commands) : null;

    private static List<Command>? EditList(IReadOnlyList<Command> commands, Guid? parentId, Func<List<Command>, bool> edit)
    {
        if (parentId is null)
        {
            return Apply(commands, edit);
        }

        var copy = commands.ToList();
        for (var index = 0; index < copy.Count; index++)
        {
            if (copy[index] is not RepeatCommand repeat)
            {
                continue;
            }

            var body = repeat.Id == parentId ? Apply(repeat.Body, edit) : EditList(repeat.Body, parentId, edit);
            if (body is not null)
            {
                copy[index] = new RepeatCommand(repeat.Count, body, repeat.Id);
                return copy;
            }
        }

        return null;
    }

    private static List<Command>? Apply(IReadOnlyList<Command> commands, Func<List<Command>, bool> edit)
    {
        var copy = commands.ToList();
        return edit(copy) ? copy : null;
    }
}
