using HeroPath.Modules.Game.Domain;
using HeroPath.Shared.Application;
using HeroPath.Shared.Application.Contracts;

namespace HeroPath.Modules.Game.Application;

/// <summary>
/// Plays the sound that belongs to an applied step of the <see cref="GameSession"/> when <see cref="SoundSettings.Enabled"/>.
/// Subscribes when <see cref="Start"/> is called (idempotent, the play page does it) and unsubscribes on <see cref="Dispose"/>. It never waits for the sound and never lets a failing player
/// disturb the run.
/// </summary>
public sealed class RunSounds : IDisposable
{
    private readonly GameSession _session;
    private readonly ISoundPlayer _player;
    private readonly SoundSettings _settings;
    private readonly CancellationTokenSource _disposed = new();
    private bool _started;

    public RunSounds(GameSession session, ISoundPlayer player, SoundSettings settings)
    {
        _session = session;
        _player = player;
        _settings = settings;
    }

    /// <summary>Starts listening to the session. Safe to call again: the session is subscribed only once. Does nothing after <see cref="Dispose"/>.</summary>
    public void Start()
    {
        if (_started || _disposed.IsCancellationRequested)
        {
            return;
        }

        _started = true;
        _session.StepApplied += OnStepApplied;
    }

    /// <summary>The sound of a step: success first, then error, then item, or <c>null</c> for a plain step.</summary>
    public static SoundEffect? EffectOf(ExecutionStep step) => step.State switch
    {
        ExecutionState.Succeeded => SoundEffect.Success,
        ExecutionState.Failed => SoundEffect.Error,
        _ when step.CollectedItem is not null => SoundEffect.Item,
        _ => null,
    };

    public void Dispose()
    {
        if (_disposed.IsCancellationRequested)
        {
            return;
        }

        if (_started)
        {
            _session.StepApplied -= OnStepApplied;
        }

        _disposed.Cancel();
        _disposed.Dispose();
    }

    private void OnStepApplied(ExecutionStep step)
    {
        if (_settings.Enabled && EffectOf(step) is { } effect)
        {
            _ = PlaySafeAsync(effect, _disposed.Token);
        }
    }

    private async Task PlaySafeAsync(SoundEffect effect, CancellationToken cancellationToken)
    {
        try
        {
            await _player.PlayAsync(effect, cancellationToken);
        }
        catch (Exception)
        {
            // A sound is a bonus: whatever the player does, the run goes on.
        }
    }
}
