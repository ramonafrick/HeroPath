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

        if (State == ExecutionState.Ready && Validate() is { } invalid)
        {
            return Finish(invalid);
        }

        if (State == ExecutionState.Ready)
        {
            _frames.Push(new Frame(_program.Commands, 1));
            _pending = NextMove();
        }

        State = ExecutionState.Running;

        var command = _pending!;
        if (_executedSteps >= ProgramLimits.MaxExecutedSteps)
        {
            return Finish(Failure(command.Id, ExecutionError.StepLimitExceeded, _executedSteps + 1));
        }

        var result = _person.Move(command.Direction, _grid);
        if (result.Outcome == MoveOutcome.OutOfBounds)
        {
            return Finish(Failure(command.Id, ExecutionError.OutOfBounds, _executedSteps + 1));
        }

        if (result.Outcome == MoveOutcome.Blocked)
        {
            return Finish(Failure(command.Id, ExecutionError.Obstacle, _executedSteps + 1) with { HitObstacle = result.Hit });
        }

        _executedSteps++;
        var collectedItem = Collect(result.NewPosition);
        var missing = _grid.RequiredItemCount - _collectedStrawberries;
        var onGoal = result.NewPosition == _grid.Goal;
        var step = new ExecutionStep(command.Id, _executedSteps, result.NewPosition, ExecutionState.Running, CollectedItem: collectedItem);

        if (onGoal && (!_grid.RequireAllItems || missing == 0))
        {
            return Finish(step with { State = ExecutionState.Succeeded });
        }

        _pending = NextMove();
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

        return FindEmptyRepeat(_program.Commands) is { } repeat
            ? Failure(repeat.Id, ExecutionError.EmptyRepeat, 0)
            : null;
    }

    private static RepeatCommand? FindEmptyRepeat(IReadOnlyList<Command> commands)
    {
        foreach (var command in commands)
        {
            if (command is not RepeatCommand repeat)
            {
                continue;
            }

            if (repeat.Body.Count == 0)
            {
                return repeat;
            }

            if (FindEmptyRepeat(repeat.Body) is { } inner)
            {
                return inner;
            }
        }

        return null;
    }

    private ExecutionStep Failure(Guid commandId, ExecutionError error, int stepNumber) =>
        new(commandId, stepNumber, _person.Position, ExecutionState.Failed, error);

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
                    _frames.Push(new Frame(repeat.Body, repeat.Count));
                    break;
            }
        }

        return null;
    }

    private sealed class Frame(IReadOnlyList<Command> commands, int remaining)
    {
        public IReadOnlyList<Command> Commands { get; } = commands;

        public int Index { get; set; }

        public int Remaining { get; set; } = remaining;
    }
}
