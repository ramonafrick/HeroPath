using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Game.Tests.Domain;

public class InterpreterTests
{
    private static readonly Position _start = new(0, 4);
    private static readonly Position _goal = new(4, 0);

    private static Grid CreateGrid(
        bool requireAllItems = false,
        LevelObstacle[]? obstacles = null,
        LevelItem[]? items = null,
        Position? start = null) =>
        new(5, 5, start ?? _start, _goal, obstacles ?? [], items ?? [], requireAllItems);

    private static MoveCommand Move(Direction direction) => new(direction);

    private static MoveCommand[] Moves(Direction direction, int count) =>
        [.. Enumerable.Range(0, count).Select(_ => Move(direction))];

    private static RepeatCommand Repeat(int count, params Command[] body) => new(count, body);

    private static ProgramDefinition Program(params Command[] commands) => new(commands);

    private static List<ExecutionStep> RunToEnd(Interpreter interpreter)
    {
        var steps = new List<ExecutionStep>();
        ExecutionStep step;
        do
        {
            step = interpreter.Step();
            steps.Add(step);
        }
        while (step.State == ExecutionState.Running);

        return steps;
    }

    private static Command[] GoalSequence() =>
        [.. Moves(Direction.Up, 4), .. Moves(Direction.Right, 4)];

    private static ProgramDefinition BackAndForth(int moves)
    {
        var commands = new List<Command>();
        var pairs = moves / 2;
        for (; pairs >= 400; pairs -= 400)
        {
            commands.Add(Repeat(20, Repeat(20, Move(Direction.Right), Move(Direction.Left))));
        }

        for (; pairs >= 20; pairs -= 20)
        {
            commands.Add(Repeat(20, Move(Direction.Right), Move(Direction.Left)));
        }

        if (pairs > 0)
        {
            commands.Add(Repeat(pairs, Move(Direction.Right), Move(Direction.Left)));
        }

        if (moves % 2 == 1)
        {
            commands.Add(Move(Direction.Right));
        }

        return new ProgramDefinition(commands);
    }

    [Fact]
    public void Constructor_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => new Interpreter(null!, CreateGrid()));
        Assert.Throws<ArgumentNullException>(() => new Interpreter(ProgramDefinition.Empty, null!));
    }

    [Fact]
    public void New_Interpreter_IsReadyAtStart()
    {
        var interpreter = new Interpreter(Program(Move(Direction.Up)), CreateGrid());

        Assert.Equal(ExecutionState.Ready, interpreter.State);
        Assert.Equal(_start, interpreter.Position);
        Assert.Empty(interpreter.CollectedItems);
    }

    [Fact]
    public void Step_EmptyProgram_FailsWithoutMoving()
    {
        var interpreter = new Interpreter(ProgramDefinition.Empty, CreateGrid());

        var step = interpreter.Step();

        Assert.Equal(ExecutionState.Failed, step.State);
        Assert.Equal(ExecutionError.EmptyProgram, step.Error);
        Assert.Equal(Guid.Empty, step.CommandId);
        Assert.Equal(0, step.StepNumber);
        Assert.Equal(_start, step.Position);
        Assert.Equal(ExecutionState.Failed, interpreter.State);
    }

    [Fact]
    public void Step_EmptyRepeatAtTopLevel_FailsWithRepeatIdAndNoMovement()
    {
        var empty = Repeat(3);
        var interpreter = new Interpreter(Program(Move(Direction.Up), empty), CreateGrid());

        var step = interpreter.Step();

        Assert.Equal(ExecutionError.EmptyRepeat, step.Error);
        Assert.Equal(empty.Id, step.CommandId);
        Assert.Equal(_start, step.Position);
        Assert.Equal(_start, interpreter.Position);
        Assert.Equal(0, step.StepNumber);
    }

    [Fact]
    public void Step_NestedEmptyRepeat_FindsFirstInExecutionOrder()
    {
        var firstEmpty = Repeat(2);
        var secondEmpty = Repeat(2);
        var program = Program(
            Repeat(2, Move(Direction.Up), firstEmpty),
            secondEmpty);
        var interpreter = new Interpreter(program, CreateGrid());

        var step = interpreter.Step();

        Assert.Equal(ExecutionError.EmptyRepeat, step.Error);
        Assert.Equal(firstEmpty.Id, step.CommandId);
        Assert.Equal(_start, interpreter.Position);
    }

    [Fact]
    public void Step_SimpleSequence_SucceedsOnLastMove()
    {
        var commands = GoalSequence();
        var interpreter = new Interpreter(Program(commands), CreateGrid());

        var steps = RunToEnd(interpreter);

        Assert.Equal(8, steps.Count);
        Assert.All(steps.Take(7), s => Assert.Equal(ExecutionState.Running, s.State));
        var last = steps[^1];
        Assert.Equal(ExecutionState.Succeeded, last.State);
        Assert.Null(last.Error);
        Assert.Equal(_goal, last.Position);
        Assert.Equal(8, last.StepNumber);
        Assert.Equal(commands.Select(c => c.Id), steps.Select(s => s.CommandId));
        Assert.Equal(Enumerable.Range(1, 8), steps.Select(s => s.StepNumber));
    }

    [Fact]
    public void Step_GoalReachedWithBlocksRemaining_SucceedsAndSkipsRest()
    {
        var program = Program([.. GoalSequence(), Move(Direction.Down), Move(Direction.Down)]);
        var interpreter = new Interpreter(program, CreateGrid());

        var steps = RunToEnd(interpreter);

        Assert.Equal(8, steps.Count);
        Assert.Equal(ExecutionState.Succeeded, steps[^1].State);
        Assert.Equal(8, steps[^1].StepNumber);
        Assert.Equal(_goal, interpreter.Position);
    }

    [Theory]
    [InlineData(ObstacleKind.Tree)]
    [InlineData(ObstacleKind.Rock)]
    [InlineData(ObstacleKind.Pond)]
    public void Step_Obstacle_FailsWithMoveIdAndUnchangedPosition(ObstacleKind kind)
    {
        var blocked = Move(Direction.Right);
        var grid = CreateGrid(obstacles: [new LevelObstacle(1, 3, kind)]);
        var interpreter = new Interpreter(Program(Move(Direction.Up), blocked, Move(Direction.Up)), grid);

        var steps = RunToEnd(interpreter);

        Assert.Equal(2, steps.Count);
        var failed = steps[^1];
        Assert.Equal(ExecutionState.Failed, failed.State);
        Assert.Equal(ExecutionError.Obstacle, failed.Error);
        Assert.Equal(kind, failed.HitObstacle);
        Assert.Equal(blocked.Id, failed.CommandId);
        Assert.Equal(new Position(0, 3), failed.Position);
        Assert.Equal(2, failed.StepNumber);
    }

    [Theory]
    [InlineData(Direction.Up, 2, 0)]
    [InlineData(Direction.Down, 2, 4)]
    [InlineData(Direction.Left, 0, 2)]
    [InlineData(Direction.Right, 4, 2)]
    public void Step_OutOfBounds_FailsWithAttemptedStepAndUnchangedPosition(Direction direction, int x, int y)
    {
        var outside = Move(direction);
        var interpreter = new Interpreter(Program(outside), CreateGrid(start: new(x, y)));

        var step = interpreter.Step();

        Assert.Equal(ExecutionState.Failed, step.State);
        Assert.Equal(ExecutionError.OutOfBounds, step.Error);
        Assert.Equal(outside.Id, step.CommandId);
        Assert.Equal(1, step.StepNumber);
        Assert.Equal(new Position(x, y), step.Position);
        Assert.Null(step.HitObstacle);
    }

    [Fact]
    public void Step_FailureInsideRepeat_ReportsInnerMoveId()
    {
        var up = Move(Direction.Up);
        var left = Move(Direction.Left);
        var interpreter = new Interpreter(Program(Repeat(3, up, left)), CreateGrid());

        var steps = RunToEnd(interpreter);

        Assert.Equal(2, steps.Count);
        Assert.Equal(left.Id, steps[^1].CommandId);
        Assert.Equal(ExecutionError.OutOfBounds, steps[^1].Error);
    }

    [Fact]
    public void Step_NestedRepeats_ReportCorrectCommandIdsAcrossIterations()
    {
        var up = Move(Direction.Up);
        var right = Move(Direction.Right);
        var down = Move(Direction.Down);
        var left = Move(Direction.Left);
        var outer = Repeat(2, up, Repeat(2, right, left), down);
        var interpreter = new Interpreter(Program(outer), CreateGrid(start: new(2, 2)));

        var steps = RunToEnd(interpreter);

        var oneRound = new[] { up.Id, right.Id, left.Id, right.Id, left.Id, down.Id };
        Assert.Equal([.. oneRound, .. oneRound], steps.Select(s => s.CommandId));
        Assert.Equal(12, steps[^1].StepNumber);
        Assert.Equal(ExecutionError.GoalNotReached, steps[^1].Error);
    }

    [Fact]
    public void Step_ProgramEndsOffGoal_LastMoveStepIsGoalNotReached()
    {
        var up = Move(Direction.Up);
        var interpreter = new Interpreter(Program(up), CreateGrid());

        var step = interpreter.Step();

        Assert.Equal(ExecutionState.Failed, step.State);
        Assert.Equal(ExecutionError.GoalNotReached, step.Error);
        Assert.Equal(up.Id, step.CommandId);
        Assert.Equal(1, step.StepNumber);
        Assert.Equal(new Position(0, 3), step.Position);
    }

    [Fact]
    public void Step_GoalWithMissingStrawberries_ContinuesAndCanStillSucceed()
    {
        var grid = CreateGrid(requireAllItems: true, items: [new LevelItem(4, 1, ItemKind.Strawberry)]);
        var program = Program([.. GoalSequence(), Move(Direction.Down), Move(Direction.Up)]);
        var interpreter = new Interpreter(program, grid);

        var steps = RunToEnd(interpreter);

        Assert.Equal(10, steps.Count);
        Assert.Equal(_goal, steps[7].Position);
        Assert.Equal(ExecutionState.Running, steps[7].State);
        Assert.Equal(ItemKind.Strawberry, steps[8].CollectedItem);
        Assert.Equal(ExecutionState.Succeeded, steps[9].State);
        Assert.Equal(_goal, steps[9].Position);
    }

    [Fact]
    public void Step_ProgramEndsOnGoalWithStrawberriesMissing_FailsWithMissingCount()
    {
        var grid = CreateGrid(
            requireAllItems: true,
            items: [new LevelItem(1, 4, ItemKind.Strawberry), new LevelItem(2, 4, ItemKind.Strawberry), new LevelItem(3, 4, ItemKind.Strawberry)]);
        Command[] commands = [Move(Direction.Right), .. Moves(Direction.Up, 4), .. Moves(Direction.Right, 3)];
        var interpreter = new Interpreter(Program(commands), grid);

        var steps = RunToEnd(interpreter);

        var last = steps[^1];
        Assert.Equal(ExecutionState.Failed, last.State);
        Assert.Equal(ExecutionError.ItemsMissing, last.Error);
        Assert.Equal(2, last.MissingItems);
        Assert.Equal(_goal, last.Position);
        Assert.Equal(commands[^1].Id, last.CommandId);
    }

    [Fact]
    public void Step_AllStrawberriesCollectedBeforeGoal_Succeeds()
    {
        var grid = CreateGrid(requireAllItems: true, items: [new LevelItem(0, 2, ItemKind.Strawberry)]);
        var interpreter = new Interpreter(Program(GoalSequence()), grid);

        var steps = RunToEnd(interpreter);

        Assert.Equal(ExecutionState.Succeeded, steps[^1].State);
        Assert.Equal(0, steps[^1].MissingItems);
        Assert.Contains(new Position(0, 2), interpreter.CollectedItems);
    }

    [Fact]
    public void Step_PizzaIsNotRequired_SucceedsWithoutIt()
    {
        var grid = CreateGrid(
            requireAllItems: true,
            items: [new LevelItem(0, 2, ItemKind.Strawberry), new LevelItem(4, 3, ItemKind.Pizza)]);
        var interpreter = new Interpreter(Program(GoalSequence()), grid);

        var steps = RunToEnd(interpreter);

        Assert.Equal(ExecutionState.Succeeded, steps[^1].State);
        Assert.DoesNotContain(new Position(4, 3), interpreter.CollectedItems);
    }

    [Fact]
    public void Step_ItemWalkedOverTwice_IsCollectedOnce()
    {
        var grid = CreateGrid(items: [new LevelItem(0, 3, ItemKind.Strawberry)]);
        var program = Program(Move(Direction.Up), Move(Direction.Down), Move(Direction.Up), Move(Direction.Down));
        var interpreter = new Interpreter(program, grid);

        var steps = RunToEnd(interpreter);

        Assert.Equal(ItemKind.Strawberry, steps[0].CollectedItem);
        Assert.All(steps.Skip(1), s => Assert.Null(s.CollectedItem));
        Assert.Equal([new Position(0, 3)], interpreter.CollectedItems);
    }

    [Fact]
    public void Step_PizzaCollected_ReportsKindInThatStepOnly()
    {
        var grid = CreateGrid(items: [new LevelItem(0, 3, ItemKind.Pizza)]);
        var interpreter = new Interpreter(Program(Move(Direction.Up), Move(Direction.Right)), grid);

        var steps = RunToEnd(interpreter);

        Assert.Equal(ItemKind.Pizza, steps[0].CollectedItem);
        Assert.Null(steps[1].CollectedItem);
    }

    [Fact]
    public void Step_ExactlyMaxSteps_IsAllowed()
    {
        var interpreter = new Interpreter(BackAndForth(ProgramLimits.MaxExecutedSteps), CreateGrid(start: new(0, 0)));

        var steps = RunToEnd(interpreter);

        Assert.Equal(ProgramLimits.MaxExecutedSteps, steps.Count);
        Assert.Equal(ExecutionError.GoalNotReached, steps[^1].Error);
        Assert.Equal(ProgramLimits.MaxExecutedSteps, steps[^1].StepNumber);
    }

    [Fact]
    public void Step_OneMoreThanMaxSteps_FailsWithStepLimitWithoutMoving()
    {
        var interpreter = new Interpreter(BackAndForth(ProgramLimits.MaxExecutedSteps + 1), CreateGrid(start: new(0, 0)));

        var steps = RunToEnd(interpreter);

        Assert.Equal(ProgramLimits.MaxExecutedSteps + 1, steps.Count);
        var last = steps[^1];
        Assert.Equal(ExecutionError.StepLimitExceeded, last.Error);
        Assert.Equal(ExecutionState.Failed, last.State);
        Assert.Equal(ProgramLimits.MaxExecutedSteps + 1, last.StepNumber);
        Assert.Equal(steps[^2].Position, last.Position);
    }

    [Fact]
    public void Step_AfterTerminalState_ReturnsSameStepAndChangesNothing()
    {
        var interpreter = new Interpreter(Program(GoalSequence()), CreateGrid());
        var terminal = RunToEnd(interpreter)[^1];

        Assert.Same(terminal, interpreter.Step());
        Assert.Same(terminal, interpreter.Step());
        Assert.Equal(_goal, interpreter.Position);
        Assert.Equal(ExecutionState.Succeeded, interpreter.State);
    }

    [Fact]
    public void Step_AfterFailure_StaysFailed()
    {
        var interpreter = new Interpreter(Program(Move(Direction.Left), Move(Direction.Up)), CreateGrid());
        var failed = interpreter.Step();

        Assert.Same(failed, interpreter.Step());
        Assert.Equal(_start, interpreter.Position);
    }

    [Fact]
    public void Step_TwoInterpretersOnSameProgram_YieldIdenticalSequences()
    {
        var grid = CreateGrid(items: [new LevelItem(0, 2, ItemKind.Strawberry)]);
        var program = Program(
            Repeat(2, Move(Direction.Up), Repeat(2, Move(Direction.Right), Move(Direction.Left))),
            Move(Direction.Up));

        var first = RunToEnd(new Interpreter(program, grid));
        var second = RunToEnd(new Interpreter(program, grid));

        Assert.Equal(first.Select(s => s with { Repeats = [] }), second.Select(s => s with { Repeats = [] }));
        Assert.Equal(first.Select(s => s.Repeats.Select(r => (r.RepeatId, r.Round, r.Count))), second.Select(s => s.Repeats.Select(r => (r.RepeatId, r.Round, r.Count))));
    }
}
