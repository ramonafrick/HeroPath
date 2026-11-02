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

    public Interpreter(ProgramDefinition program, Grid grid)
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(grid);

        _program = program;
        _grid = grid;
        _person = new Person(grid.Start);
    }

    public ExecutionState State { get; private set; } = ExecutionState.Ready;

    public Position Position => _person.Position;

    public IReadOnlySet<Position> CollectedItems => _collected;

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
            return Finish(Failure(command.Id, ExecutionError.StepLimitExceeded));
        }

        var result = _person.Move(command.Direction, _grid);
        if (result.Outcome == MoveOutcome.OutOfBounds)
        {
            return Finish(Failure(command.Id, ExecutionError.OutOfBounds));
        }

        if (result.Outcome == MoveOutcome.Blocked)
        {
            return Finish(Failure(command.Id, ExecutionError.Obstacle) with { HitObstacle = result.Hit });
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
            return Failure(Guid.Empty, ExecutionError.EmptyProgram);
        }

        return FindEmptyRepeat(_program.Commands) is { } repeat
            ? Failure(repeat.Id, ExecutionError.EmptyRepeat)
            : null;
    }

    private static RepeatCommand? FindEmptyRepeat(IReadOnlyList<ICommand> commands)
    {
        foreach (var command in commands)
        {
            if (command is RepeatCommand repeat)
            {
                if (repeat.Body.Count == 0)
                {
                    return repeat;
                }

                if (FindEmptyRepeat(repeat.Body) is { } inner)
                {
                    return inner;
                }
            }
        }

        return null;
    }

    private ExecutionStep Failure(Guid commandId, ExecutionError error) =>
        new(commandId, _executedSteps, _person.Position, ExecutionState.Failed, error);

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

            var command = frame.Commands[frame.Index++];
            if (command is MoveCommand move)
            {
                return move;
            }

            var repeat = (RepeatCommand)command;
            _frames.Push(new Frame(repeat.Body, repeat.Count));
        }

        return null;
    }

    private sealed class Frame(IReadOnlyList<ICommand> commands, int remaining)
    {
        public IReadOnlyList<ICommand> Commands { get; } = commands;

        public int Index { get; set; }

        public int Remaining { get; set; } = remaining;
    }
}
