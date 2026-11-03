using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Game.Application;

/// <summary>
/// State of the current play page: loaded grid, the program the child assembles (with selection and insert target), the run
/// state and the tempo. Memory only. The run state is written by the <see cref="ExecutionRunner"/> only. All program edits are
/// silent no-ops while the session is not <see cref="IsEditable"/>, and <see cref="Changed"/> fires only for real changes.
/// </summary>
public sealed class GameSession
{
    /// <summary>Count of a newly added repeat block.</summary>
    public const int DefaultRepeatCount = 3;

    private ProgramDefinition _program = ProgramDefinition.Empty;
    private Tempo _tempo = Tempo.Medium;

    /// <summary>Raised after every change of the session.</summary>
    public event Action? Changed;

    internal event Action? RunDiscarded;

    /// <summary>The loaded level grid, or <c>null</c> before <see cref="Load"/> was called.</summary>
    public Grid? Grid { get; private set; }

    /// <summary>The program the child assembles. Immutable; every edit replaces it.</summary>
    public ProgramDefinition Program => _program;

    /// <summary>Id of the selected block, or <c>null</c>. Cleared when the block is removed or a run starts.</summary>
    public Guid? SelectedId { get; private set; }

    /// <summary>
    /// Where the next block from the palette goes: the end of the selected repeat's body, the end of the body the selected
    /// block sits in, otherwise the end of the program.
    /// </summary>
    public BlockSlot InsertTarget
    {
        get
        {
            if (SelectedId is { } id && _program.Find(id) is { } selected)
            {
                if (selected is RepeatCommand repeat)
                {
                    return new BlockSlot(repeat.Id, repeat.Body.Count);
                }

                if (_program.SlotOf(id) is { ParentId: { } parentId } && _program.Find(parentId) is RepeatCommand parent)
                {
                    return new BlockSlot(parentId, parent.Body.Count);
                }
            }

            return BlockSlot.Root(_program.Commands.Count);
        }
    }

    /// <summary>Where the run stands. Changed only by the <see cref="ExecutionRunner"/>.</summary>
    public RunStatus Status { get; private set; } = RunStatus.Ready;

    /// <summary>The last executed step of the current run, or <c>null</c> before the first step.</summary>
    public ExecutionStep? CurrentStep { get; private set; }

    /// <summary>Speed of Play. A change during a run applies to the next wait.</summary>
    public Tempo Tempo
    {
        get => _tempo;
        set
        {
            if (_tempo == value)
            {
                return;
            }

            _tempo = value;
            Changed?.Invoke();
        }
    }

    /// <summary>Whether the program may be changed: only while the run is <see cref="RunStatus.Ready"/>.</summary>
    public bool IsEditable => Status == RunStatus.Ready;

    /// <summary>Whether another block fits into the program (fewer than <see cref="ProgramLimits.MaxProgramCommands"/> blocks, counted recursively).</summary>
    public bool CanAdd => _program.CountAll() < ProgramLimits.MaxProgramCommands;

    /// <summary>Whether a repeat block fits into the <see cref="InsertTarget"/>: the program is not full and the nesting depth allows it.</summary>
    public bool CanAddRepeat => ProgramEditing.CanInsert(_program, NewRepeat(), InsertTarget);

    /// <summary>Position of the hero on the board: after the current step, else the start cell of the loaded grid.</summary>
    public Position HeroPosition => CurrentStep?.Position ?? Grid?.Start ?? default;

    /// <summary>
    /// Cell to mark after a failed move: the obstacle cell for <see cref="ExecutionError.Obstacle"/>, the hero cell for
    /// <see cref="ExecutionError.OutOfBounds"/>, otherwise <c>null</c>. Computed when a step is applied.
    /// </summary>
    public Position? MarkedCell { get; private set; }

    /// <summary>
    /// Loads a level grid. Loading an equal grid again is a no-op, so returning to the page or re-rendering keeps
    /// program and run. A different grid discards the run (status <see cref="RunStatus.Ready"/>, hero on the new start,
    /// editable) and keeps the program.
    /// </summary>
    public void Load(Grid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        if (grid.Equals(Grid))
        {
            return;
        }

        Grid = grid;
        Status = RunStatus.Ready;
        CurrentStep = null;
        MarkedCell = null;
        RunDiscarded?.Invoke();
        Changed?.Invoke();
    }

    /// <summary>Inserts a move block at the <see cref="InsertTarget"/>. Does nothing when the program is full or locked.</summary>
    public void Add(Direction direction) => Apply(ProgramEditing.Insert(_program, new MoveCommand(direction), InsertTarget));

    /// <summary>Inserts an empty repeat block (count <see cref="DefaultRepeatCount"/>) at the <see cref="InsertTarget"/>. Does nothing when it does not fit or the program is locked.</summary>
    public void AddRepeat() => Apply(ProgramEditing.Insert(_program, NewRepeat(), InsertTarget));

    /// <summary>Removes the block with <paramref name="id"/> (a repeat with its body). Does nothing when it is unknown or the program is locked.</summary>
    public void Remove(Guid id) => Apply(ProgramEditing.Remove(_program, id));

    /// <summary>Removes all blocks. Does nothing when the program is empty or locked.</summary>
    public void Clear() => Apply(_program.Commands.Count == 0 ? null : ProgramDefinition.Empty);

    /// <summary>Moves the block one place up (see <see cref="ProgramEditing.MoveUp"/>). Does nothing when that is impossible or the program is locked.</summary>
    public void MoveUp(Guid id) => Apply(ProgramEditing.MoveUp(_program, id));

    /// <summary>Moves the block one place down (see <see cref="ProgramEditing.MoveDown"/>). Does nothing when that is impossible or the program is locked.</summary>
    public void MoveDown(Guid id) => Apply(ProgramEditing.MoveDown(_program, id));

    /// <summary>Whether the block can move up and the program is editable.</summary>
    public bool CanMoveUp(Guid id) => IsEditable && ProgramEditing.CanMoveUp(_program, id);

    /// <summary>Whether the block can move down and the program is editable.</summary>
    public bool CanMoveDown(Guid id) => IsEditable && ProgramEditing.CanMoveDown(_program, id);

    /// <summary>Moves the block to <paramref name="slot"/> (see <see cref="ProgramEditing.Move"/>). Does nothing when invalid, unchanged or locked.</summary>
    public void Move(Guid id, BlockSlot slot) => Apply(ProgramEditing.Move(_program, id, slot));

    /// <summary>Sets the count of a repeat block. Does nothing for counts outside the limits, unknown ids, an unchanged count or a locked program.</summary>
    public void SetRepeatCount(Guid id, int count) => Apply(ProgramEditing.SetRepeatCount(_program, id, count));

    /// <summary>
    /// Selects the block with <paramref name="id"/>. Selecting the selected block again, or <c>null</c>, clears the selection.
    /// Unknown ids and a locked program do nothing.
    /// </summary>
    public void Select(Guid? id)
    {
        if (!IsEditable)
        {
            return;
        }

        var target = id is { } value && value != SelectedId && _program.Find(value) is not null ? id : null;
        if (target == SelectedId)
        {
            return;
        }

        SelectedId = target;
        Changed?.Invoke();
    }

    /// <summary>The program the interpreter runs.</summary>
    public ProgramDefinition ToProgram() => _program;

    internal void UpdateRun(RunStatus status, ExecutionStep step)
    {
        Status = status;
        CurrentStep = step;
        SelectedId = null;
        MarkedCell = MarkedCellOf(step);
        Changed?.Invoke();
    }

    internal void PauseRun()
    {
        if (Status != RunStatus.Running)
        {
            return;
        }

        Status = RunStatus.Paused;
        Changed?.Invoke();
    }

    internal void ResetRun()
    {
        if (Status == RunStatus.Ready && CurrentStep is null)
        {
            return;
        }

        Status = RunStatus.Ready;
        CurrentStep = null;
        MarkedCell = null;
        Changed?.Invoke();
    }

    private static RepeatCommand NewRepeat() => new(DefaultRepeatCount, []);

    private void Apply(ProgramDefinition? edited)
    {
        if (!IsEditable || edited is null)
        {
            return;
        }

        _program = edited;
        if (SelectedId is { } selected && _program.Find(selected) is null)
        {
            SelectedId = null;
        }

        Changed?.Invoke();
    }

    private Position? MarkedCellOf(ExecutionStep step) => step switch
    {
        { Error: ExecutionError.OutOfBounds } => step.Position,
        { Error: ExecutionError.Obstacle } when _program.Find(step.CommandId) is MoveCommand move =>
            move.Direction.MoveFrom(step.Position),
        _ => null,
    };
}
