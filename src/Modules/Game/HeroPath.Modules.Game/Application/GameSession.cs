using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Game.Application;

/// <summary>
/// State of the current play page: loaded grid, the program the child assembles, the run state and the tempo.
/// Memory only. The run state is written by the <see cref="ExecutionRunner"/> only.
/// </summary>
public sealed class GameSession
{
    private readonly List<MoveCommand> _commands = [];
    private Tempo _tempo = Tempo.Medium;

    /// <summary>Raised after every change of the session.</summary>
    public event Action? Changed;

    internal event Action? RunDiscarded;

    /// <summary>The loaded level grid, or <c>null</c> before <see cref="Load"/> was called.</summary>
    public Grid? Grid { get; private set; }

    /// <summary>Blocks of the program in execution order.</summary>
    public IReadOnlyList<MoveCommand> Commands => _commands;

    /// <summary>Where the current run stands. Changed only by the <see cref="ExecutionRunner"/>.</summary>
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

    /// <summary>Whether another block fits into the program.</summary>
    public bool CanAdd => _commands.Count < ProgramLimits.MaxProgramCommands;

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

    /// <summary>Appends a move block. Does nothing when the program is full or locked.</summary>
    public void Add(Direction direction)
    {
        if (!IsEditable || !CanAdd)
        {
            return;
        }

        _commands.Add(new MoveCommand(direction));
        Changed?.Invoke();
    }

    /// <summary>Removes the block with <paramref name="id"/>. Does nothing when it is unknown or the program is locked.</summary>
    public void Remove(Guid id)
    {
        if (!IsEditable)
        {
            return;
        }

        var index = _commands.FindIndex(command => command.Id == id);
        if (index < 0)
        {
            return;
        }

        _commands.RemoveAt(index);
        Changed?.Invoke();
    }

    /// <summary>Removes all blocks. Does nothing when the program is empty or locked.</summary>
    public void Clear()
    {
        if (!IsEditable || _commands.Count == 0)
        {
            return;
        }

        _commands.Clear();
        Changed?.Invoke();
    }

    /// <summary>Builds the program the interpreter runs.</summary>
    public ProgramDefinition ToProgram() => new([.. _commands]);

    internal void UpdateRun(RunStatus status, ExecutionStep step)
    {
        Status = status;
        CurrentStep = step;
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

    private Position? MarkedCellOf(ExecutionStep step) => step switch
    {
        { Error: ExecutionError.OutOfBounds } => step.Position,
        { Error: ExecutionError.Obstacle } when _commands.Find(command => command.Id == step.CommandId) is { } move =>
            move.Direction.MoveFrom(step.Position),
        _ => null,
    };
}
