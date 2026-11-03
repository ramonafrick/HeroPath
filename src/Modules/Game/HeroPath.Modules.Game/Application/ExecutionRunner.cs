using HeroPath.Modules.Game.Domain;

namespace HeroPath.Modules.Game.Application;

/// <summary>
/// Runs the program of a <see cref="GameSession"/> for Play and Debug with one shared <see cref="Interpreter"/>.
/// The first step locks editing; <see cref="Reset"/> unlocks it again. Play executes the first step immediately and
/// waits for the <see cref="Tempo"/> duration between steps.
/// </summary>
public sealed class ExecutionRunner(GameSession session, TimeProvider timeProvider)
{
    private Interpreter? _interpreter;
    private CancellationTokenSource? _cancellation;
    private Tempo _tempo = Tempo.Medium;

    /// <summary>Speed of Play. A change during a run applies to the next wait.</summary>
    public Tempo Tempo
    {
        get => _tempo;
        set
        {
            if (_tempo == value)
            {
                return;
            }

            _tempo = value;
            session.NotifyChanged();
        }
    }

    /// <summary>Executes steps until the run ends or <see cref="Reset"/> cancels it. Does nothing while running, after an end state or for an empty program.</summary>
    public async Task PlayAsync()
    {
        if (!CanStart() || session.Status == RunStatus.Running)
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
                session.UpdateRun(status, step);
                if (status != RunStatus.Running)
                {
                    return;
                }

                await Task.Delay(_tempo.Duration, timeProvider, token);
                token.ThrowIfCancellationRequested();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>Executes exactly one step (Debug). Does nothing while running, after an end state or for an empty program.</summary>
    public void StepOnce()
    {
        if (!CanStart() || session.Status == RunStatus.Running)
        {
            return;
        }

        var step = (_interpreter ??= CreateInterpreter()).Step();
        session.UpdateRun(StatusOf(step, RunStatus.Paused), step);
    }

    /// <summary>Cancels a running Play, discards the interpreter and returns the hero to the start. The program stays.</summary>
    public void Reset()
    {
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _cancellation = null;
        _interpreter = null;
        session.ResetRun();
    }

    private bool CanStart() =>
        session.Grid is not null
        && session.Commands.Count > 0
        && session.Status is RunStatus.Ready or RunStatus.Paused or RunStatus.Running;

    private Interpreter CreateInterpreter() => new(session.ToProgram(), session.Grid!);

    private static RunStatus StatusOf(ExecutionStep step, RunStatus whileRunning) => step.State switch
    {
        ExecutionState.Succeeded => RunStatus.Succeeded,
        ExecutionState.Failed => RunStatus.Failed,
        _ => whileRunning,
    };
}
