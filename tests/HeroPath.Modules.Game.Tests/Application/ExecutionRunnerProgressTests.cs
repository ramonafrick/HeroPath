using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Levels.Infrastructure;
using Microsoft.Extensions.Time.Testing;

namespace HeroPath.Modules.Game.Tests.Application;

public class ExecutionRunnerProgressTests
{
    private static readonly Direction[] _easy1Solution =
        [Direction.Right, Direction.Right, Direction.Up, Direction.Right, Direction.Right, Direction.Right, Direction.Down];

    private readonly FakeTimeProvider _time = new();
    private readonly GameSession _session = new();
    private readonly LevelProgress _progress = new(TestLevels.NewRepository(StandardLevels.All));
    private readonly ExecutionRunner _runner;

    public ExecutionRunnerProgressTests()
    {
        _session.Load(StandardLevels.Get("easy-1"));
        _runner = new ExecutionRunner(_session, _progress, _time);
    }

    [Fact]
    public void StepOnce_SuccessfulRun_MarksLoadedLevelSolved()
    {
        TestLevels.Fill(_session, _easy1Solution);

        for (var i = 0; i < _easy1Solution.Length; i++)
        {
            Assert.False(_progress.IsSolved("easy-1"));
            _runner.StepOnce();
        }

        Assert.Equal(RunStatus.Succeeded, _session.Status);
        Assert.True(_progress.IsSolved("easy-1"));
        Assert.False(_progress.IsSolved("easy-2"));
    }

    [Fact]
    public async Task Play_SuccessfulRun_MarksLoadedLevelSolved()
    {
        TestLevels.Fill(_session, _easy1Solution);

        var play = _runner.PlayAsync();
        for (var i = 0; i < 50 && !play.IsCompleted; i++)
        {
            _time.Advance(TimeSpan.FromMilliseconds(500));
            await Task.Yield();
        }

        await play;

        Assert.True(_progress.IsSolved("easy-1"));
    }

    [Fact]
    public void StepOnce_FailedRun_DoesNotMarkSolved()
    {
        TestLevels.Fill(_session, Direction.Up);

        _runner.StepOnce();
        _runner.StepOnce();

        Assert.Equal(RunStatus.Failed, _session.Status);
        Assert.False(_progress.IsSolved("easy-1"));
    }

    [Fact]
    public void StepOnce_SuccessAfterSwitchingLevel_MarksTheSwitchedLevel()
    {
        _session.Load(StandardLevels.Get("free-field"));
        TestLevels.Fill(_session, [.. Enumerable.Repeat(Direction.Right, 5), .. Enumerable.Repeat(Direction.Up, 4)]);

        for (var i = 0; i < 9; i++)
        {
            _runner.StepOnce();
        }

        Assert.True(_progress.IsSolved("free-field"));
        Assert.False(_progress.IsSolved("easy-1"));
    }

    [Fact]
    public async Task Play_SwitchingLevelWhileRunning_StopsTheRunAndMarksNothing()
    {
        TestLevels.Fill(_session, _easy1Solution);
        var play = _runner.PlayAsync();
        Assert.Equal(RunStatus.Running, _session.Status);

        _session.Load(StandardLevels.Get("easy-2"));
        _time.Advance(TimeSpan.FromSeconds(10));
        await play;

        Assert.Equal(RunStatus.Ready, _session.Status);
        Assert.Empty(_session.Program.Commands);
        Assert.False(_progress.IsSolved("easy-1"));
    }
}
