using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Game.Application;

/// <summary>State of the current play page: loaded grid and the program the child assembles. Memory only.</summary>
public sealed class GameSession
{
    private readonly List<MoveCommand> _commands = [];

    /// <summary>Raised after every change of the session.</summary>
    public event Action? Changed;

    /// <summary>The loaded level grid, or <c>null</c> before <see cref="Load"/> was called.</summary>
    public Grid? Grid { get; private set; }

    /// <summary>Blocks of the program in execution order.</summary>
    public IReadOnlyList<MoveCommand> Commands => _commands;

    /// <summary>Where the current run stands. Changed only by the <see cref="ExecutionRunner"/>.</summary>
    public RunStatus Status { get; private set; } = RunStatus.Ready;

    /// <summary>The last executed step of the current run, or <c>null</c> before the first step.</summary>
    public ExecutionStep? CurrentStep { get; private set; }

    /// <summary>Whether the program may be changed: only while the run is <see cref="RunStatus.Ready"/>.</summary>
    public bool IsEditable => Status == RunStatus.Ready;

    /// <summary>Whether another block fits into the program.</summary>
    public bool CanAdd => _commands.Count < ProgramLimits.MaxProgramCommands;

    /// <summary>Position of the hero on the board: after the current step, else the start cell of the loaded grid.</summary>
    public Position HeroPosition => CurrentStep?.Position ?? Grid?.Start ?? default;

    /// <summary>
    /// Cell to mark after a failed move: the obstacle cell for <see cref="ExecutionError.Obstacle"/>, the hero cell for
    /// <see cref="ExecutionError.OutOfBounds"/>, otherwise <c>null</c>.
    /// </summary>
    public Position? MarkedCell => CurrentStep switch
    {
        { Error: ExecutionError.OutOfBounds } => HeroPosition,
        { Error: ExecutionError.Obstacle } step when _commands.Find(command => command.Id == step.CommandId) is { } move =>
            move.Direction.MoveFrom(HeroPosition),
        _ => null,
    };

    /// <summary>Loads a level grid and keeps the current program.</summary>
    public void Load(Grid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        Grid = grid;
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

    internal void UpdateRun(RunStatus status, ExecutionStep step)
    {
        Status = status;
        CurrentStep = step;
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
        Changed?.Invoke();
    }

    internal void NotifyChanged() => Changed?.Invoke();

    /// <summary>Builds the program the interpreter runs.</summary>
    public ProgramDefinition ToProgram() => new([.. _commands]);
}
