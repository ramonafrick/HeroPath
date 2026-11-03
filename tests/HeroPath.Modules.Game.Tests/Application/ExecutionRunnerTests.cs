using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Levels.Domain;
using Microsoft.Extensions.Time.Testing;

namespace HeroPath.Modules.Game.Tests.Application;

public class ExecutionRunnerTests
{
    private readonly FakeTimeProvider _time = new();
    private readonly GameSession _session = new();
    private readonly ExecutionRunner _runner;

    public ExecutionRunnerTests()
    {
        _session.Load(TestLevels.DemoGrid());
        _runner = new ExecutionRunner(_session, _time);
    }

    private void Program(params Direction[] directions) => TestLevels.Fill(_session, directions);

    private async Task AdvanceStepAsync(int milliseconds = 500)
    {
        _time.Advance(TimeSpan.FromMilliseconds(milliseconds));
        await Task.Yield();
    }

    private async Task FinishAsync(Task play)
    {
        for (var i = 0; i < 50 && !play.IsCompleted; i++)
        {
            await AdvanceStepAsync();
        }

        await play;
    }

    [Fact]
    public async Task Play_DemoSolution_ReachesGoalAndSucceeds()
    {
        Program(TestLevels.DemoSolution);

        await FinishAsync(_runner.PlayAsync());

        Assert.Equal(RunStatus.Succeeded, _session.Status);
        Assert.Equal(new Position(6, 3), _session.HeroPosition);
        Assert.Equal(ExecutionState.Succeeded, _session.CurrentStep!.State);
        Assert.False(_session.IsEditable);
        Assert.Null(_session.MarkedCell);
    }

    [Fact]
    public async Task Play_ExecutesFirstStepImmediately_ThenWaitsForTempo()
    {
        Program(Direction.Up, Direction.Up, Direction.Up);

        var play = _runner.PlayAsync();

        Assert.Equal(RunStatus.Running, _session.Status);
        Assert.Equal(new Position(1, 5), _session.HeroPosition);

        await AdvanceStepAsync(499);
        Assert.Equal(new Position(1, 5), _session.HeroPosition);

        await AdvanceStepAsync(1);
        Assert.Equal(new Position(1, 4), _session.HeroPosition);

        _runner.Reset();
        await play;
    }

    [Theory]
    [InlineData(Tempo.Slow, 900)]
    [InlineData(Tempo.Medium, 500)]
    [InlineData(Tempo.Fast, 200)]
    public async Task Play_WaitsTempoDurationBetweenSteps(Tempo tempo, int milliseconds)
    {
        Program(Direction.Up, Direction.Up, Direction.Up);
        _session.Tempo = tempo;

        var play = _runner.PlayAsync();
        await AdvanceStepAsync(milliseconds - 1);
        Assert.Equal(1, _session.CurrentStep!.StepNumber);

        await AdvanceStepAsync(1);
        Assert.Equal(2, _session.CurrentStep!.StepNumber);

        _runner.Reset();
        await play;
    }

    [Fact]
    public void Tempo_DefaultIsMediumAndDurationsMatch()
    {
        Assert.Equal(Tempo.Medium, _session.Tempo);
        Assert.Equal(TimeSpan.FromMilliseconds(900), Tempo.Slow.Duration);
        Assert.Equal(TimeSpan.FromMilliseconds(500), Tempo.Medium.Duration);
        Assert.Equal(TimeSpan.FromMilliseconds(200), Tempo.Fast.Duration);
    }

    [Fact]
    public async Task Play_TempoChangedDuringRun_AffectsNextWait()
    {
        Program(Direction.Up, Direction.Up, Direction.Up, Direction.Up);

        var play = _runner.PlayAsync();
        await AdvanceStepAsync();
        Assert.Equal(2, _session.CurrentStep!.StepNumber);

        _session.Tempo = Tempo.Fast;
        await AdvanceStepAsync();
        Assert.Equal(3, _session.CurrentStep!.StepNumber);

        await AdvanceStepAsync(200);
        Assert.Equal(4, _session.CurrentStep!.StepNumber);

        _runner.Reset();
        await play;
    }

    [Fact]
    public void Tempo_Change_RaisesChangedOnlyWhenDifferent()
    {
        var changes = 0;
        _session.Changed += () => changes++;

        _session.Tempo = Tempo.Fast;
        _session.Tempo = Tempo.Fast;

        Assert.Equal(1, changes);
    }

    [Fact]
    public void StepOnce_ExecutesExactlyOneStep_PausesAndLocksEditing()
    {
        Program(Direction.Up, Direction.Up);

        _runner.StepOnce();

        Assert.Equal(RunStatus.Paused, _session.Status);
        Assert.Equal(new Position(1, 5), _session.HeroPosition);
        Assert.Equal(_session.Program.Commands[0].Id, _session.CurrentStep!.CommandId);
        Assert.False(_session.IsEditable);
    }

    [Fact]
    public async Task StepOnce_ThenPlay_ContinuesOnSameInterpreter()
    {
        Program(TestLevels.DemoSolution);
        _runner.StepOnce();
        _runner.StepOnce();
        Assert.Equal(new Position(1, 4), _session.HeroPosition);

        var play = _runner.PlayAsync();
        Assert.Equal(3, _session.CurrentStep!.StepNumber);
        await FinishAsync(play);

        Assert.Equal(RunStatus.Succeeded, _session.Status);
        Assert.Equal(new Position(6, 3), _session.HeroPosition);
        Assert.Equal(8, _session.CurrentStep!.StepNumber);
    }

    [Fact]
    public void StepOnce_LastStepOfSolution_EndsInSucceededNotPaused()
    {
        Program(TestLevels.DemoSolution);

        for (var i = 0; i < TestLevels.DemoSolution.Length; i++)
        {
            _runner.StepOnce();
        }

        Assert.Equal(RunStatus.Succeeded, _session.Status);
    }

    [Fact]
    public void StepOnce_AfterEndState_IsIgnored()
    {
        Program(Direction.Up);
        _runner.StepOnce();
        Assert.Equal(RunStatus.Failed, _session.Status);
        var step = _session.CurrentStep;
        var changes = 0;
        _session.Changed += () => changes++;

        _runner.StepOnce();

        Assert.Same(step, _session.CurrentStep);
        Assert.Equal(0, changes);
    }

    [Fact]
    public async Task StepOnce_WhileRunning_IsIgnored()
    {
        Program(Direction.Up, Direction.Up, Direction.Up);
        var play = _runner.PlayAsync();

        _runner.StepOnce();

        Assert.Equal(RunStatus.Running, _session.Status);
        Assert.Equal(1, _session.CurrentStep!.StepNumber);

        _runner.Reset();
        await play;
    }

    [Fact]
    public async Task Play_WhileRunning_StartsNoSecondLoop()
    {
        Program(Direction.Up, Direction.Up, Direction.Up, Direction.Up);
        var first = _runner.PlayAsync();

        var second = _runner.PlayAsync();
        Assert.True(second.IsCompletedSuccessfully);

        await AdvanceStepAsync();
        Assert.Equal(2, _session.CurrentStep!.StepNumber);

        _runner.Reset();
        await first;
    }

    [Fact]
    public async Task Play_AfterEndState_IsIgnored()
    {
        Program(Direction.Up);
        await _runner.PlayAsync();
        Assert.Equal(RunStatus.Failed, _session.Status);
        var step = _session.CurrentStep;

        await _runner.PlayAsync();

        Assert.Same(step, _session.CurrentStep);
    }

    [Fact]
    public async Task PlayAndStepOnce_EmptyProgram_DoNothing()
    {
        var changes = 0;
        _session.Changed += () => changes++;

        await _runner.PlayAsync();
        _runner.StepOnce();

        Assert.Equal(RunStatus.Ready, _session.Status);
        Assert.Null(_session.CurrentStep);
        Assert.True(_session.IsEditable);
        Assert.Equal(0, changes);
    }

    [Fact]
    public async Task Reset_DuringPlay_AppliesNoFurtherStep()
    {
        Program(TestLevels.DemoSolution);
        var play = _runner.PlayAsync();
        await AdvanceStepAsync();
        Assert.Equal(2, _session.CurrentStep!.StepNumber);
        var ids = _session.Program.Commands.Select(c => c.Id).ToList();

        _runner.Reset();
        await AdvanceStepAsync(10_000);
        await play;

        Assert.Equal(RunStatus.Ready, _session.Status);
        Assert.Null(_session.CurrentStep);
        Assert.Equal(new Position(1, 6), _session.HeroPosition);
        Assert.True(_session.IsEditable);
        Assert.Equal(ids, _session.Program.Commands.Select(c => c.Id));
    }

    [Fact]
    public async Task Reset_ThenPlay_StartsFreshRunFromStart()
    {
        Program(TestLevels.DemoSolution);
        var oldPlay = _runner.PlayAsync();
        await AdvanceStepAsync();
        _runner.Reset();

        var newPlay = _runner.PlayAsync();
        await oldPlay;

        Assert.Equal(RunStatus.Running, _session.Status);
        Assert.Equal(1, _session.CurrentStep!.StepNumber);
        await AdvanceStepAsync();
        Assert.Equal(2, _session.CurrentStep!.StepNumber);

        _runner.Reset();
        await newPlay;
    }

    [Fact]
    public void Reset_AfterEndState_AllowsRunningAgain()
    {
        Program(Direction.Up);
        _runner.StepOnce();
        _runner.Reset();

        _runner.StepOnce();

        Assert.Equal(RunStatus.Failed, _session.Status);
        Assert.Equal(1, _session.CurrentStep!.StepNumber);
    }

    [Fact]
    public async Task Play_IntoTree_FailsWithObstacleAndMarksTreeCell()
    {
        Program(Direction.Right, Direction.Right);

        await FinishAsync(_runner.PlayAsync());

        Assert.Equal(RunStatus.Failed, _session.Status);
        Assert.Equal(ExecutionError.Obstacle, _session.CurrentStep!.Error);
        Assert.Equal(_session.Program.Commands[1].Id, _session.CurrentStep.CommandId);
        Assert.Equal(new Position(2, 6), _session.HeroPosition);
        Assert.Equal(new Position(3, 6), _session.MarkedCell);
    }

    [Fact]
    public async Task Play_OffTheBoard_FailsWithOutOfBoundsAndMarksHeroCell()
    {
        Program(Direction.Left, Direction.Left);

        await FinishAsync(_runner.PlayAsync());

        Assert.Equal(RunStatus.Failed, _session.Status);
        Assert.Equal(ExecutionError.OutOfBounds, _session.CurrentStep!.Error);
        Assert.Equal(new Position(0, 6), _session.MarkedCell);
    }

    [Fact]
    public async Task Play_EndsBeforeGoal_FailsWithGoalNotReachedAndMarksNothing()
    {
        Program(Direction.Up);

        await _runner.PlayAsync();

        Assert.Equal(RunStatus.Failed, _session.Status);
        Assert.Equal(ExecutionError.GoalNotReached, _session.CurrentStep!.Error);
        Assert.Null(_session.MarkedCell);
    }

    [Fact]
    public async Task PlayAndDebug_GiveSameResult()
    {
        Direction[] moves = [Direction.Up, Direction.Up, Direction.Right, Direction.Right, Direction.Right];
        Program(moves);
        var debugSession = new GameSession();
        debugSession.Load(TestLevels.DemoGrid());
        TestLevels.Fill(debugSession, moves);
        var debugRunner = new ExecutionRunner(debugSession, _time);

        await FinishAsync(_runner.PlayAsync());
        for (var i = 0; i < moves.Length; i++)
        {
            debugRunner.StepOnce();
        }

        Assert.Equal(_session.Status, debugSession.Status);
        Assert.Equal(_session.HeroPosition, debugSession.HeroPosition);
        Assert.Equal(_session.CurrentStep!.Error, debugSession.CurrentStep!.Error);
    }

    [Fact]
    public void CanStart_NoGrid_IsFalse()
    {
        var session = new GameSession();
        session.Add(Direction.Up);

        Assert.False(new ExecutionRunner(session, _time).CanStart);
    }

    [Fact]
    public void CanStart_EmptyProgram_IsFalse() => Assert.False(_runner.CanStart);

    [Fact]
    public async Task CanStart_FollowsRunStatus()
    {
        Program(Direction.Up, Direction.Up, Direction.Up);
        Assert.Equal(RunStatus.Ready, _session.Status);
        Assert.True(_runner.CanStart);

        _runner.StepOnce();
        Assert.Equal(RunStatus.Paused, _session.Status);
        Assert.True(_runner.CanStart);

        var play = _runner.PlayAsync();
        Assert.Equal(RunStatus.Running, _session.Status);
        Assert.False(_runner.CanStart);

        await FinishAsync(play);
        Assert.Equal(RunStatus.Failed, _session.Status);
        Assert.False(_runner.CanStart);

        _runner.Reset();
        Assert.True(_runner.CanStart);
    }

    [Fact]
    public void CanStart_Succeeded_IsFalse()
    {
        Program(TestLevels.DemoSolution);
        for (var i = 0; i < TestLevels.DemoSolution.Length; i++)
        {
            _runner.StepOnce();
        }

        Assert.Equal(RunStatus.Succeeded, _session.Status);
        Assert.False(_runner.CanStart);
    }

    [Fact]
    public async Task Play_ChangedHandlerThrows_PausesInsteadOfStayingRunning()
    {
        Program(Direction.Up, Direction.Up, Direction.Up);
        var calls = 0;
        _session.Changed += () =>
        {
            if (++calls == 2)
            {
                throw new InvalidOperationException("handler failed");
            }
        };

        var play = _runner.PlayAsync();
        Assert.Equal(RunStatus.Running, _session.Status);
        _time.Advance(TimeSpan.FromMilliseconds(500));
        await Assert.ThrowsAsync<InvalidOperationException>(() => play);

        Assert.Equal(RunStatus.Paused, _session.Status);
        Assert.Equal(2, _session.CurrentStep!.StepNumber);
        Assert.True(_runner.CanStart);
    }

    [Fact]
    public async Task Play_ChangedHandlerThrowsOnFirstStep_PausesAndPropagates()
    {
        Program(Direction.Up, Direction.Up);
        var thrown = false;
        _session.Changed += () =>
        {
            if (!thrown)
            {
                thrown = true;
                throw new InvalidOperationException("handler failed");
            }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _runner.PlayAsync());

        Assert.Equal(RunStatus.Paused, _session.Status);
        Assert.Equal(1, _session.CurrentStep!.StepNumber);
    }

    [Fact]
    public async Task Stop_DuringPlay_PausesAndAppliesNoFurtherStep()
    {
        Program(TestLevels.DemoSolution);
        var play = _runner.PlayAsync();
        await AdvanceStepAsync();
        Assert.Equal(2, _session.CurrentStep!.StepNumber);

        _runner.Stop();
        await AdvanceStepAsync(10_000);
        await play;

        Assert.Equal(RunStatus.Paused, _session.Status);
        Assert.Equal(2, _session.CurrentStep!.StepNumber);
        Assert.Equal(new Position(1, 4), _session.HeroPosition);
        Assert.Equal(TestLevels.DemoSolution.Length, _session.Program.Commands.Count);
    }

    [Fact]
    public async Task Stop_ThenPlay_ContinuesOnSameInterpreter()
    {
        Program(TestLevels.DemoSolution);
        var firstPlay = _runner.PlayAsync();
        await AdvanceStepAsync();
        _runner.Stop();
        await firstPlay;

        await FinishAsync(_runner.PlayAsync());

        Assert.Equal(RunStatus.Succeeded, _session.Status);
        Assert.Equal(TestLevels.DemoSolution.Length, _session.CurrentStep!.StepNumber);
    }

    [Fact]
    public void Stop_WhenNotRunning_DoesNothing()
    {
        Program(Direction.Up, Direction.Up);
        _runner.StepOnce();
        var changes = 0;
        _session.Changed += () => changes++;

        _runner.Stop();

        Assert.Equal(RunStatus.Paused, _session.Status);
        Assert.Equal(0, changes);
    }

    [Fact]
    public async Task Load_DifferentGridDuringPlay_StopsLoopAndResetsRun()
    {
        Program(TestLevels.DemoSolution);
        var play = _runner.PlayAsync();
        await AdvanceStepAsync();

        _session.Load(new Grid(10, 10, new Position(0, 0), new Position(9, 9), [], [], false));
        await AdvanceStepAsync(10_000);
        await play;

        Assert.Equal(RunStatus.Ready, _session.Status);
        Assert.Null(_session.CurrentStep);
        Assert.Equal(new Position(0, 0), _session.HeroPosition);
        Assert.True(_session.IsEditable);
    }

    [Fact]
    public async Task Reset_ThenNewPlay_OldLoopDoesNotPauseNewRun()
    {
        Program(Direction.Up, Direction.Up, Direction.Up, Direction.Up);
        var oldPlay = _runner.PlayAsync();
        _runner.Reset();

        var newPlay = _runner.PlayAsync();
        await oldPlay;

        Assert.Equal(RunStatus.Running, _session.Status);

        _runner.Reset();
        await newPlay;
    }
}
