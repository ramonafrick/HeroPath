using HeroPath.Modules.Game.Domain;

namespace HeroPath.Modules.Game.Application;

/// <summary>
/// Runs the program of a <see cref="GameSession"/> for Play and Debug with one shared <see cref="Interpreter"/>.
/// The first step locks editing; <see cref="Reset"/> unlocks it again. Play executes the first step immediately and
/// waits for the <see cref="GameSession.Tempo"/> duration between steps. The runner lives as long as its session and
/// drops its run whenever the session loads a different level.
/// </summary>
public sealed class ExecutionRunner
{
    private readonly GameSession _session;
    private readonly TimeProvider _timeProvider;
    private Interpreter? _interpreter;
    private CancellationTokenSource? _cancellation;

    public ExecutionRunner(GameSession session, TimeProvider timeProvider)
    {
        _session = session;
        _timeProvider = timeProvider;
        _session.RunDiscarded += DiscardRun;
    }

    /// <summary>
    /// Whether Play or Debug may start or continue: a grid is loaded, the program is not empty and the run is
    /// <see cref="RunStatus.Ready"/> or <see cref="RunStatus.Paused"/>. This is the only place for the rule.
    /// </summary>
    public bool CanStart =>
        _session.Grid is not null
        && _session.Program.Commands.Count > 0
        && _session.Status is RunStatus.Ready or RunStatus.Paused;

    /// <summary>
    /// Executes steps until the run ends, <see cref="Reset"/> or <see cref="Stop"/> cancels it. Does nothing while running,
    /// after an end state or for an empty program. A <see cref="RunStatus.Ready"/> program with an empty repeat block does not start
    /// either: the session shows the empty-repeat hint (no interpreter, status stays Ready, the program stays editable). If anything throws mid-run (for example a <see cref="GameSession.Changed"/>
    /// handler), the run ends <see cref="RunStatus.Paused"/> instead of staying <see cref="RunStatus.Running"/> and the exception propagates.
    /// </summary>
    public async Task PlayAsync()
    {
        if (!CanStart || TryRefuseStart())
        {
            return;
        }

        var interpreter = _interpreter ??= CreateInterpreter();
        var cancellation = _cancellation = new CancellationTokenSource();
        var token = cancellation.Token;

        try
        {
            while (true)
            {
                var step = interpreter.Step();
                var status = StatusOf(step, RunStatus.Running);
                _session.UpdateRun(status, step);
                if (status != RunStatus.Running)
                {
                    return;
                }

                await Task.Delay(_session.Tempo.Duration, _timeProvider, token);
                token.ThrowIfCancellationRequested();
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        finally
        {
            if (ReferenceEquals(_cancellation, cancellation))
            {
                _cancellation = null;
                cancellation.Dispose();
                _session.PauseRun();
            }
        }
    }

    /// <summary>Executes exactly one step (Debug). Does nothing while running, after an end state or for an empty program.</summary>
    public void StepOnce()
    {
        if (!CanStart || TryRefuseStart())
        {
            return;
        }

        var step = (_interpreter ??= CreateInterpreter()).Step();
        _session.UpdateRun(StatusOf(step, RunStatus.Paused), step);
    }

    /// <summary>Stops a running Play and leaves the run <see cref="RunStatus.Paused"/>: program, position and interpreter stay. Does nothing when not running.</summary>
    public void Stop()
    {
        if (_session.Status != RunStatus.Running)
        {
            return;
        }

        ReleaseCancellation();
        _session.PauseRun();
    }

    /// <summary>Cancels a running Play, discards the interpreter and returns the hero to the start. The program stays.</summary>
    public void Reset()
    {
        DiscardRun();
        _session.ResetRun();
    }

    private void DiscardRun()
    {
        ReleaseCancellation();
        _interpreter = null;
    }

    private void ReleaseCancellation()
    {
        var cancellation = _cancellation;
        _cancellation = null;
        cancellation?.Cancel();
        cancellation?.Dispose();
    }

    private bool TryRefuseStart()
    {
        if (_session.Status != RunStatus.Ready || _session.Program.FindEmptyRepeat() is not { } repeat)
        {
            return false;
        }

        _session.ShowEmptyRepeatHint(repeat);
        return true;
    }

    private Interpreter CreateInterpreter() => new(_session.ToProgram(), _session.Grid!);

    private static RunStatus StatusOf(ExecutionStep step, RunStatus whileRunning) => step.State switch
    {
        ExecutionState.Succeeded => RunStatus.Succeeded,
        ExecutionState.Failed => RunStatus.Failed,
        _ => whileRunning,
    };
}
