using HeroPath.Shared.Application.Contracts;

namespace HeroPath.Testing;

public sealed class FakeSoundPlayer : ISoundPlayer
{
    public List<SoundEffect> Played { get; } = [];

    /// <summary>When set, <see cref="PlayAsync"/> throws it synchronously, to prove a failing player does not disturb callers.</summary>
    public Exception? Throws { get; set; }

    public ValueTask PlayAsync(SoundEffect effect, CancellationToken cancellationToken)
    {
        Played.Add(effect);
        return Throws is { } exception ? throw exception : ValueTask.CompletedTask;
    }
}
