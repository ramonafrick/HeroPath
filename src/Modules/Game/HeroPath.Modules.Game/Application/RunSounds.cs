using HeroPath.Modules.Game.Domain;
using HeroPath.Shared.Application;
using HeroPath.Shared.Application.Contracts;

namespace HeroPath.Modules.Game.Application;

/// <summary>
/// Plays the sound that belongs to an applied step of the <see cref="GameSession"/> when <see cref="SoundSettings.Enabled"/>.
/// Subscribes once on creation and unsubscribes on <see cref="Dispose"/>. It never waits for the sound and never lets a failing player
/// disturb the run.
/// </summary>
public sealed class RunSounds : IDisposable
{
    private readonly GameSession _session;
    private readonly ISoundPlayer _player;
    private readonly SoundSettings _settings;
    private readonly CancellationTokenSource _disposed = new();

    public RunSounds(GameSession session, ISoundPlayer player, SoundSettings settings)
    {
        _session = session;
        _player = player;
        _settings = settings;
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

        _session.StepApplied -= OnStepApplied;
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
