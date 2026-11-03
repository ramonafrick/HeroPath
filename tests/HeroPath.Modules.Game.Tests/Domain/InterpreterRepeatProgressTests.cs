using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Levels.Domain;
using Microsoft.Extensions.Time.Testing;

namespace HeroPath.Modules.Game.Tests.Domain;

public class InterpreterRepeatProgressTests
{
    private static Grid OpenGrid(params LevelObstacle[] obstacles) =>
        new(12, 12, new Position(0, 11), new Position(11, 0), obstacles, [], false);

    private static MoveCommand Move(Direction direction) => new(direction);

    private static List<ExecutionStep> RunToEnd(ProgramDefinition program, Grid grid)
    {
        var interpreter = new Interpreter(program, grid);
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

    private static string[] Rounds(IEnumerable<ExecutionStep> steps) =>
        [.. steps.Select(s => string.Join(",", s.Repeats.Select(r => $"{r.Round}/{r.Count}")))];

    [Fact]
    public void Step_NoRepeat_HasEmptyRepeats()
    {
        var steps = RunToEnd(new ProgramDefinition([Move(Direction.Up), Move(Direction.Right)]), OpenGrid());

        Assert.All(steps, step => Assert.Empty(step.Repeats));
    }

    [Fact]
    public void Step_EmptyProgramAndEmptyRepeat_HaveEmptyRepeats()
    {
        var emptyProgram = new Interpreter(new ProgramDefinition([]), OpenGrid()).Step();
        var emptyRepeat = new Interpreter(new ProgramDefinition([new RepeatCommand(2, [])]), OpenGrid()).Step();

        Assert.Empty(emptyProgram.Repeats);
        Assert.Empty(emptyRepeat.Repeats);
    }

    [Fact]
    public void Step_SimpleRepeat_ReportsRoundOnEveryStep()
    {
        var repeat = new RepeatCommand(3, [Move(Direction.Right), Move(Direction.Right)]);
        var steps = RunToEnd(new ProgramDefinition([Move(Direction.Up), repeat, Move(Direction.Up)]), OpenGrid());

        Assert.Equal(["", "1/3", "1/3", "2/3", "2/3", "3/3", "3/3", ""], Rounds(steps));
        Assert.All(steps.Skip(1).Take(6), step => Assert.Equal(repeat.Id, Assert.Single(step.Repeats).RepeatId));
    }

    [Fact]
    public void Step_LastMoveOfRepeatBody_StillReportsItsOwnRound()
    {
        var repeat = new RepeatCommand(2, [Move(Direction.Right)]);
        var steps = RunToEnd(new ProgramDefinition([repeat]), OpenGrid());

        Assert.Equal(["1/2", "2/2"], Rounds(steps));
        Assert.Equal(ExecutionError.GoalNotReached, steps[^1].Error);
    }

    [Fact]
    public void Step_NestedRepeat_ReportsOuterThenInnerRound()
    {
        var inner = new RepeatCommand(2, [Move(Direction.Right)]);
        var outer = new RepeatCommand(2, [inner, Move(Direction.Up)]);
        var steps = RunToEnd(new ProgramDefinition([outer]), OpenGrid());

        Assert.Equal(["1/2,1/2", "1/2,2/2", "1/2", "2/2,1/2", "2/2,2/2", "2/2"], Rounds(steps));
        Assert.Equal([outer.Id, inner.Id], steps[0].Repeats.Select(r => r.RepeatId));
        Assert.Equal([outer.Id], steps[2].Repeats.Select(r => r.RepeatId));
    }

    [Fact]
    public void Step_ObstacleInsideBody_ReportsRoundOfAttemptedMove()
    {
        var grid = OpenGrid(new LevelObstacle(2, 11, ObstacleKind.Tree));
        var repeat = new RepeatCommand(5, [Move(Direction.Right)]);
        var steps = RunToEnd(new ProgramDefinition([repeat]), grid);

        var failed = steps[^1];
        Assert.Equal(ExecutionError.Obstacle, failed.Error);
        Assert.Equal(2, failed.StepNumber);
        Assert.Equal(new RepeatProgress(repeat.Id, 2, 5), Assert.Single(failed.Repeats));
    }

    [Fact]
    public void Step_OutOfBoundsInsideBody_ReportsRoundOfAttemptedMove()
    {
        var repeat = new RepeatCommand(3, [Move(Direction.Left)]);
        var failed = RunToEnd(new ProgramDefinition([repeat]), OpenGrid())[^1];

        Assert.Equal(ExecutionError.OutOfBounds, failed.Error);
        Assert.Equal(new RepeatProgress(repeat.Id, 1, 3), Assert.Single(failed.Repeats));
    }

    [Fact]
    public void Step_StepLimitInsideBody_ReportsRoundOfAttemptedMove()
    {
        static RepeatCommand Inner() => new(20, [Move(Direction.Right), Move(Direction.Left)]);
        var outer = new RepeatCommand(20, [Inner(), Inner()]);
        var failed = RunToEnd(new ProgramDefinition([outer, outer, outer]), OpenGrid())[^1];

        Assert.Equal(ExecutionError.StepLimitExceeded, failed.Error);
        Assert.Equal(ProgramLimits.MaxExecutedSteps + 1, failed.StepNumber);
        Assert.Equal(2, failed.Repeats.Count);
        Assert.Equal(outer.Id, failed.Repeats[0].RepeatId);
        Assert.All(failed.Repeats, r => Assert.InRange(r.Round, 1, r.Count));
    }

    [Fact]
    public void Step_SucceededInsideBody_KeepsRepeatsOfLastMove()
    {
        var grid = new Grid(5, 5, new Position(0, 4), new Position(2, 4), [], [], false);
        var repeat = new RepeatCommand(5, [Move(Direction.Right)]);
        var last = RunToEnd(new ProgramDefinition([repeat]), grid)[^1];

        Assert.Equal(ExecutionState.Succeeded, last.State);
        Assert.Equal(new RepeatProgress(repeat.Id, 2, 5), Assert.Single(last.Repeats));
    }

    [Fact]
    public void Step_AfterTerminal_ReturnsSameRepeats()
    {
        var interpreter = new Interpreter(new ProgramDefinition([new RepeatCommand(2, [Move(Direction.Right)])]), OpenGrid());
        interpreter.Step();
        var last = interpreter.Step();

        Assert.Same(last, interpreter.Step());
        Assert.Equal(2, Assert.Single(last.Repeats).Round);
    }

    [Fact]
    public async Task PlayAndDebug_ReportIdenticalRepeats()
    {
        var debugSession = BuildSession();
        var expected = Rounds(RunToEnd(debugSession.ToProgram(), debugSession.Grid!));

        var debugRunner = new ExecutionRunner(debugSession, new FakeTimeProvider());
        var debug = new List<ExecutionStep>();
        while (debugRunner.CanStart)
        {
            debugRunner.StepOnce();
            debug.Add(debugSession.CurrentStep!);
        }

        var time = new FakeTimeProvider();
        var playSession = BuildSession();
        var played = new List<ExecutionStep>();
        playSession.Changed += () =>
        {
            if (playSession.CurrentStep is { } step && (played.Count == 0 || !ReferenceEquals(played[^1], step)))
            {
                played.Add(step);
            }
        };
        var play = new ExecutionRunner(playSession, time).PlayAsync();
        for (var i = 0; i < 100 && !play.IsCompleted; i++)
        {
            time.Advance(TimeSpan.FromMilliseconds(500));
            await Task.Yield();
        }

        await play;

        Assert.Equal(expected, Rounds(debug));
        Assert.Equal(expected, Rounds(played));
        Assert.Contains("2/3", expected);
        Assert.Equal(RunStatus.Succeeded, debugSession.Status);
    }

    private static GameSession BuildSession()
    {
        var session = new GameSession();
        session.Load(TestLevels.Demo);
        session.Add(Direction.Up);
        session.Add(Direction.Up);
        session.AddRepeat();
        var repeatId = session.Program.Commands[2].Id;
        session.Select(repeatId);
        session.Add(Direction.Right);
        session.Select(repeatId);
        session.Add(Direction.Right);
        session.Add(Direction.Right);
        session.Add(Direction.Up);
        return session;
    }
}
