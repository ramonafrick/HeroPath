using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Levels.Domain;
using Microsoft.Extensions.Time.Testing;

namespace HeroPath.Modules.Game.Tests.Application;

public class GameMessageTests
{
    private readonly GameSession _session = new();
    private readonly ExecutionRunner _runner;

    public GameMessageTests()
    {
        _session.Load(TestLevels.DemoGrid());
        _runner = new ExecutionRunner(_session, new FakeTimeProvider());
    }

    private void Fill(params Direction[] directions) => TestLevels.Fill(_session, directions);

    private Guid IdAt(params int[] path) => Flatten(_session.Program.Commands)
        .First(c => _session.Program.PathOf(c.Id)!.SequenceEqual(path)).Id;

    private static IEnumerable<Command> Flatten(IEnumerable<Command> commands) =>
        commands.SelectMany(c => c is RepeatCommand r ? new[] { c }.Concat(Flatten(r.Body)) : [c]);

    private GameMessage RunToEnd()
    {
        for (var i = 0; i < 1200 && _session.Status is RunStatus.Ready or RunStatus.Paused; i++)
        {
            _runner.StepOnce();
        }

        return Assert.IsType<GameMessage>(_session.Message);
    }

    [Theory]
    [InlineData(ObstacleKind.Tree, new[] { Direction.Right, Direction.Right }, 2)]
    [InlineData(ObstacleKind.Rock, new[] { Direction.Up, Direction.Up, Direction.Up, Direction.Right, Direction.Right, Direction.Right }, 6)]
    [InlineData(ObstacleKind.Pond, new[] { Direction.Up, Direction.Right, Direction.Right, Direction.Right, Direction.Right, Direction.Right }, 6)]
    public void Message_HitObstacle_IsErrorWithKindAndBlockNumber(ObstacleKind kind, Direction[] directions, int block)
    {
        Fill(directions);

        var message = RunToEnd();

        Assert.Equal(GameMessageKind.Error, message.Kind);
        Assert.Equal(GameMessageCode.Obstacle, message.Code);
        Assert.Equal(kind, message.Obstacle);
        Assert.Equal([block], message.BlockPath);
    }

    [Fact]
    public void Message_ObstacleInsideRepeat_HasNestedPath()
    {
        _session.AddRepeat();
        _session.Select(IdAt(1));
        Fill(Direction.Right, Direction.Right, Direction.Right);

        var message = RunToEnd();

        Assert.Equal(GameMessageCode.Obstacle, message.Code);
        Assert.Equal([1, 2], message.BlockPath);
    }

    [Fact]
    public void Message_LeavingTheBoard_IsOutOfBoundsWithBlockNumber()
    {
        Fill(Direction.Left, Direction.Left);

        var message = RunToEnd();

        Assert.Equal(GameMessageKind.Error, message.Kind);
        Assert.Equal(GameMessageCode.OutOfBounds, message.Code);
        Assert.Equal([2], message.BlockPath);
    }

    [Fact]
    public void Message_ProgramEndsAwayFromGoal_IsGoalNotReached()
    {
        Fill(Direction.Up);

        var message = RunToEnd();

        Assert.Equal(new GameMessage(GameMessageKind.Error, GameMessageCode.GoalNotReached), message);
    }

    [Fact]
    public void Message_StepLimit_IsErrorWithAttemptedBlockPath()
    {
        _session.AddRepeat();
        _session.SetRepeatCount(IdAt(1), 20);
        _session.Select(IdAt(1));
        _session.AddRepeat();
        _session.AddRepeat();
        foreach (var inner in new[] { IdAt(1, 1), IdAt(1, 2) })
        {
            _session.SetRepeatCount(inner, 20);
            _session.Select(inner);
            Fill(Direction.Up, Direction.Down);
        }

        var message = RunToEnd();

        Assert.Equal(GameMessageKind.Error, message.Kind);
        Assert.Equal(GameMessageCode.StepLimitExceeded, message.Code);
        Assert.Equal([1, 2, 1], message.BlockPath);
    }

    [Fact]
    public void Message_Solution_IsSuccess()
    {
        Fill(TestLevels.DemoSolution);

        var message = RunToEnd();

        Assert.Equal(new GameMessage(GameMessageKind.Success, GameMessageCode.Succeeded), message);
    }

    [Fact]
    public void Message_WhilePaused_IsNull()
    {
        Fill(Direction.Up, Direction.Up);
        _runner.StepOnce();

        Assert.Equal(RunStatus.Paused, _session.Status);
        Assert.Null(_session.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Reset_ClearsErrorAndSuccessMessage(bool success)
    {
        Fill(success ? TestLevels.DemoSolution : [Direction.Left, Direction.Left]);
        RunToEnd();

        _runner.Reset();

        Assert.Null(_session.Message);
    }

    [Fact]
    public void Message_EmptyProgram_IsHintUntilABlockIsAdded()
    {
        Assert.Equal(new GameMessage(GameMessageKind.Hint, GameMessageCode.EmptyProgram), _session.Message);

        _session.Add(Direction.Up);

        Assert.Null(_session.Message);
    }

    [Fact]
    public void Message_FullProgram_IsHintUntilABlockIsRemoved()
    {
        Fill([.. Enumerable.Repeat(Direction.Up, ProgramLimits.MaxProgramCommands)]);

        Assert.Equal(new GameMessage(GameMessageKind.Hint, GameMessageCode.ProgramFull), _session.Message);

        _session.Remove(IdAt(1));

        Assert.Null(_session.Message);
    }

    [Fact]
    public void Message_RepeatDoesNotFitIntoInsertTarget_IsNestingTooDeepHintUntilSelectionChanges()
    {
        _session.AddRepeat();
        _session.Select(IdAt(1));
        _session.AddRepeat();
        Assert.Null(_session.Message);

        _session.Select(IdAt(1, 1));

        Assert.False(_session.CanAddRepeat);
        Assert.Equal(new GameMessage(GameMessageKind.Hint, GameMessageCode.NestingTooDeep), _session.Message);

        _session.Select(IdAt(1, 1));

        Assert.Null(_session.Message);
    }

    [Fact]
    public void Message_RefusedStartWithEmptyRepeat_IsHintWithPathAndMarksTheRepeat()
    {
        Fill(Direction.Up);
        _session.AddRepeat();
        var repeatId = IdAt(2);

        _runner.StepOnce();

        var message = Assert.IsType<GameMessage>(_session.Message);
        Assert.Equal(GameMessageKind.Hint, message.Kind);
        Assert.Equal(GameMessageCode.EmptyRepeat, message.Code);
        Assert.Equal([2], message.BlockPath);
        Assert.Equal(repeatId, _session.HintedId);
        Assert.Equal(RunStatus.Ready, _session.Status);
    }

    [Fact]
    public void Load_DifferentGrid_ClearsEmptyRepeatHint()
    {
        _session.AddRepeat();
        _runner.StepOnce();
        Assert.NotNull(_session.HintedId);

        _session.Load(new Grid(5, 5, new Position(0, 0), new Position(4, 4), [], [], false));

        Assert.Null(_session.HintedId);
        Assert.NotEqual(GameMessageCode.EmptyRepeat, _session.Message?.Code);
    }

    [Fact]
    public void RefusedStart_WithSameHintAgain_RaisesNoChanged()
    {
        _session.AddRepeat();
        _runner.StepOnce();
        var changes = 0;
        _session.Changed += () => changes++;

        _runner.StepOnce();

        Assert.Equal(0, changes);
        Assert.Equal(GameMessageCode.EmptyRepeat, _session.Message?.Code);
    }

    [Fact]
    public void Edit_AfterEmptyRepeatHint_ClearsHintAndMark()
    {
        _session.AddRepeat();
        _runner.StepOnce();
        _session.Select(IdAt(1));
        Assert.Equal(GameMessageCode.EmptyRepeat, _session.Message?.Code);

        _session.Add(Direction.Right);

        Assert.Null(_session.HintedId);
        Assert.Null(_session.Message);
    }

    [Fact]
    public void Reset_ClearsEmptyRepeatHint()
    {
        _session.AddRepeat();
        _runner.StepOnce();

        _runner.Reset();

        Assert.Null(_session.HintedId);
        Assert.Null(_session.Message);
    }

    [Fact]
    public void Message_EmptyRepeatHintWinsOverNestingHint()
    {
        _session.AddRepeat();
        _session.Select(IdAt(1));
        _session.AddRepeat();
        _session.Select(IdAt(1, 1));
        Assert.Equal(GameMessageCode.NestingTooDeep, _session.Message?.Code);

        _runner.StepOnce();

        Assert.Equal(GameMessageCode.EmptyRepeat, _session.Message?.Code);
        Assert.Equal([1, 1], _session.Message?.BlockPath);
    }
}
