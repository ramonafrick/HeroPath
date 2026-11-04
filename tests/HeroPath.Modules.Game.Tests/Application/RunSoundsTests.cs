using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Shared.Application;
using HeroPath.Shared.Application.Contracts;

namespace HeroPath.Modules.Game.Tests.Application;

public class RunSoundsTests : IDisposable
{
    private readonly GameSession _session = new();
    private readonly FakeSoundPlayer _player = new();
    private readonly SoundSettings _settings = new(new InMemoryAppStorage());
    private readonly ExecutionRunner _runner;
    private readonly RunSounds _sounds;

    public RunSoundsTests()
    {
        _session.Load(TestLevels.Demo);
        _runner = new ExecutionRunner(_session, TestLevels.NewProgress(), TimeProvider.System);
        _sounds = new RunSounds(_session, _player, _settings);
        _settings.SetAsync(true, CancellationToken.None).GetAwaiter().GetResult();
    }

    public void Dispose() => _sounds.Dispose();

    private void Debug(params Direction[] directions)
    {
        TestLevels.Fill(_session, directions);
        for (var i = 0; i < directions.Length && _session.Status is RunStatus.Ready or RunStatus.Paused; i++)
        {
            _runner.StepOnce();
        }
    }

    [Fact]
    public void Step_Plain_IsSilent()
    {
        TestLevels.Fill(_session, Direction.Up, Direction.Up);

        _runner.StepOnce();

        Assert.Equal(RunStatus.Paused, _session.Status);
        Assert.Empty(_player.Played);
    }

    [Fact]
    public void Solved_PlaysSuccess()
    {
        Debug(TestLevels.DemoSolution);

        Assert.Equal([SoundEffect.Success], _player.Played);
    }

    [Theory]
    [InlineData(ObstacleKind.Tree)]
    [InlineData(ObstacleKind.Rock)]
    [InlineData(ObstacleKind.Pond)]
    public void Obstacle_PlaysError(ObstacleKind kind)
    {
        Direction[] program = kind switch
        {
            ObstacleKind.Tree => [Direction.Right, Direction.Right],
            ObstacleKind.Rock => [Direction.Up, Direction.Up, Direction.Up, Direction.Right, Direction.Right, Direction.Right],
            _ => [Direction.Up, Direction.Right, Direction.Right, Direction.Right, Direction.Right, Direction.Right, Direction.Down],
        };

        Debug(program);

        Assert.Equal(GameMessageCode.Obstacle, _session.Message!.Code);
        Assert.Equal([SoundEffect.Error], _player.Played);
    }

    [Fact]
    public void OutOfBounds_PlaysError()
    {
        Debug(Direction.Left, Direction.Left);

        Assert.Equal(GameMessageCode.OutOfBounds, _session.Message!.Code);
        Assert.Equal([SoundEffect.Error], _player.Played);
    }

    [Fact]
    public void GoalNotReached_PlaysError()
    {
        Debug(Direction.Up);
        _runner.StepOnce();

        Assert.Equal(GameMessageCode.GoalNotReached, _session.Message!.Code);
        Assert.Equal([SoundEffect.Error], _player.Played);
    }

    [Fact]
    public void ItemsMissing_PlaysError()
    {
        _session.Load(TestLevels.WithItems("items", 4, 3, new Position(0, 1), new Position(3, 1), new LevelItem(1, 0, ItemKind.Strawberry)));

        Debug(Direction.Right, Direction.Right, Direction.Right);

        Assert.Equal(GameMessageCode.ItemsMissing, _session.Message!.Code);
        Assert.Equal([SoundEffect.Error], _player.Played);
    }

    [Fact]
    public void StepLimit_PlaysError()
    {
        _session.AddRepeat();
        var outer = _session.Program.Commands[0];
        _session.Select(outer.Id);
        _session.AddRepeat();
        var inner = ((RepeatCommand)_session.Program.Commands[0]).Body[0];
        _session.Select(inner.Id);
        TestLevels.Fill(_session, Direction.Up, Direction.Down, Direction.Down, Direction.Up);
        _session.SetRepeatCount(inner.Id, ProgramLimits.MaxRepeatCount);
        _session.SetRepeatCount(outer.Id, ProgramLimits.MaxRepeatCount);

        for (var i = 0; i <= ProgramLimits.MaxExecutedSteps && _session.Status != RunStatus.Failed; i++)
        {
            _runner.StepOnce();
        }

        Assert.Equal(GameMessageCode.StepLimitExceeded, _session.Message!.Code);
        Assert.Equal([SoundEffect.Error], _player.Played);
    }

    [Fact]
    public void CollectingItem_PlaysItem()
    {
        _session.Load(TestLevels.WithItems("items", 4, 3, new Position(0, 1), new Position(3, 1), new LevelItem(1, 1, ItemKind.Strawberry)));

        TestLevels.Fill(_session, Direction.Right, Direction.Right);

        _runner.StepOnce();

        Assert.Equal(RunStatus.Paused, _session.Status);
        Assert.Equal([SoundEffect.Item], _player.Played);
    }

    [Fact]
    public void ItemOnGoalStep_PlaysSuccessOnly()
    {
        _session.Load(TestLevels.WithItems("items", 3, 3, new Position(0, 1), new Position(2, 1), new LevelItem(2, 1, ItemKind.Strawberry)));

        Debug(Direction.Right, Direction.Right);

        Assert.Equal(RunStatus.Succeeded, _session.Status);
        Assert.Equal([SoundEffect.Success], _player.Played);
    }

    [Fact]
    public async Task SoundOff_IsSilent()
    {
        await _settings.SetAsync(false, CancellationToken.None);

        Debug(TestLevels.DemoSolution);

        Assert.Empty(_player.Played);
    }

    [Fact]
    public void RefusedStart_EmptyRepeat_IsSilent()
    {
        _session.AddRepeat();

        _runner.StepOnce();

        Assert.Equal(GameMessageCode.EmptyRepeat, _session.Message!.Code);
        Assert.Empty(_player.Played);
    }

    [Fact]
    public void EmptyProgram_IsSilent()
    {
        _runner.StepOnce();

        Assert.Empty(_player.Played);
    }

    [Fact]
    public void ThrowingPlayer_DoesNotBreakTheRun()
    {
        _player.Throws = new InvalidOperationException("no audio");

        Debug(TestLevels.DemoSolution);

        Assert.Equal(RunStatus.Succeeded, _session.Status);
        Assert.Equal([SoundEffect.Success], _player.Played);
    }

    [Fact]
    public void Dispose_Unsubscribes()
    {
        _sounds.Dispose();

        Debug(TestLevels.DemoSolution);

        Assert.Empty(_player.Played);
    }

    [Fact]
    public void Created_SubscribesExactlyOnce()
    {
        Debug(TestLevels.DemoSolution);

        Assert.Single(_player.Played);
    }
}
