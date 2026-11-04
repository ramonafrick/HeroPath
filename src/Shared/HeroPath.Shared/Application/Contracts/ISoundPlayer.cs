namespace HeroPath.Shared.Application.Contracts;

/// <summary>
/// Plays a short sound (Web: Web Audio, MAUI later: a native player). Implementations never throw because audio is unavailable
/// or blocked: they stay silent. Only a cancellation requested through the caller's token is propagated.
/// </summary>
public interface ISoundPlayer
{
    /// <summary>Plays <paramref name="effect"/> without waiting for it to finish.</summary>
    ValueTask PlayAsync(SoundEffect effect, CancellationToken cancellationToken);
}
