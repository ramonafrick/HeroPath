using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Game.Domain;

/// <summary>
/// Executes a program one move per <see cref="Step"/>. Repeats are expanded lazily with an execution stack.
/// The step that executes the last move of the program already carries the terminal state.
/// </summary>
public sealed class Interpreter
{
    private readonly ProgramDefinition _program;
    private readonly Grid _grid;
    private readonly Person _person;
    private readonly Stack<Frame> _frames = new();
    private readonly HashSet<Position> _collected = [];
    private int _collectedStrawberries;
    private int _executedSteps;
    private MoveCommand? _pending;
    private IReadOnlyList<RepeatProgress> _pendingRepeats = [];
    private ExecutionStep? _terminal;

    /// <summary>Creates an interpreter whose person starts on the grid start cell. Create a new one to reset.</summary>
    public Interpreter(ProgramDefinition program, Grid grid)
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(grid);

        _program = program;
        _grid = grid;
        _person = new Person(grid.Start);
    }

    /// <summary>Current state; <see cref="ExecutionState.Ready"/> until the first <see cref="Step"/>.</summary>
    public ExecutionState State { get; private set; } = ExecutionState.Ready;

    /// <summary>Current position of the person.</summary>
    public Position Position => _person.Position;

    /// <summary>Cells whose items have been collected so far.</summary>
    public IReadOnlySet<Position> CollectedItems => _collected;

    /// <summary>Executes exactly one move. After a terminal state, returns the same terminal step without changes.</summary>
    public ExecutionStep Step()
    {
        if (_terminal is not null)
        {
            return _terminal;
        }

        if (State == ExecutionState.Ready)
        {
            if (Validate() is { } invalid)
            {
                return Finish(invalid);
            }

            _frames.Push(new Frame(_program.Commands, 1, null));
            _pending = NextMove();
            _pendingRepeats = Snapshot();
        }

        State = ExecutionState.Running;

        var command = _pending!;
        var repeats = _pendingRepeats;
        if (_executedSteps >= ProgramLimits.MaxExecutedSteps)
        {
            return Finish(Failure(command.Id, ExecutionError.StepLimitExceeded, _executedSteps + 1, repeats));
        }

        var result = _person.Move(command.Direction, _grid);
        if (result.Outcome == MoveOutcome.OutOfBounds)
        {
            return Finish(Failure(command.Id, ExecutionError.OutOfBounds, _executedSteps + 1, repeats));
        }

        if (result.Outcome == MoveOutcome.Blocked)
        {
            return Finish(Failure(command.Id, ExecutionError.Obstacle, _executedSteps + 1, repeats) with { HitObstacle = result.Hit });
        }

        _executedSteps++;
        var collectedItem = Collect(result.NewPosition);
        var missing = _grid.RequiredItemCount - _collectedStrawberries;
        var onGoal = result.NewPosition == _grid.Goal;
        var step = new ExecutionStep(command.Id, _executedSteps, result.NewPosition, ExecutionState.Running, CollectedItem: collectedItem) { Repeats = repeats };

        if (onGoal && (!_grid.RequireAllItems || missing == 0))
        {
            return Finish(step with { State = ExecutionState.Succeeded });
        }

        _pending = NextMove();
        _pendingRepeats = Snapshot();
        if (_pending is not null)
        {
            return step;
        }

        return onGoal
            ? Finish(step with { State = ExecutionState.Failed, Error = ExecutionError.ItemsMissing, MissingItems = missing })
            : Finish(step with { State = ExecutionState.Failed, Error = ExecutionError.GoalNotReached });
    }

    private ExecutionStep? Validate()
    {
        if (_program.Commands.Count == 0)
        {
            return Failure(Guid.Empty, ExecutionError.EmptyProgram, 0);
        }

        return _program.FindEmptyRepeat() is { } repeat
            ? Failure(repeat.Id, ExecutionError.EmptyRepeat, 0)
            : null;
    }

    private ExecutionStep Failure(Guid commandId, ExecutionError error, int stepNumber, IReadOnlyList<RepeatProgress>? repeats = null) =>
        new(commandId, stepNumber, _person.Position, ExecutionState.Failed, error) { Repeats = repeats ?? [] };

    private RepeatProgress[] Snapshot() =>
        [.. _frames.Reverse()
            .Where(frame => frame.RepeatId is not null)
            .Select(frame => new RepeatProgress(frame.RepeatId!.Value, frame.Count - frame.Remaining + 1, frame.Count))];

    private ExecutionStep Finish(ExecutionStep step)
    {
        _terminal = step;
        State = step.State;
        return step;
    }

    private ItemKind? Collect(Position position)
    {
        if (_grid.GetItem(position) is not { } item || !_collected.Add(position))
        {
            return null;
        }

        if (item == ItemKind.Strawberry)
        {
            _collectedStrawberries++;
        }

        return item;
    }

    private MoveCommand? NextMove()
    {
        while (_frames.Count > 0)
        {
            var frame = _frames.Peek();
            if (frame.Index >= frame.Commands.Count)
            {
                frame.Remaining--;
                if (frame.Remaining > 0)
                {
                    frame.Index = 0;
                }
                else
                {
                    _frames.Pop();
                }

                continue;
            }

            switch (frame.Commands[frame.Index++])
            {
                case MoveCommand move:
                    return move;
                case RepeatCommand repeat:
                    _frames.Push(new Frame(repeat.Body, repeat.Count, repeat.Id));
                    break;
            }
        }

        return null;
    }

    private sealed class Frame(IReadOnlyList<Command> commands, int count, Guid? repeatId)
    {
        public IReadOnlyList<Command> Commands { get; } = commands;

        public int Index { get; set; }

        public Guid? RepeatId { get; } = repeatId;

        public int Count { get; } = count;

        public int Remaining { get; set; } = count;
    }
}
