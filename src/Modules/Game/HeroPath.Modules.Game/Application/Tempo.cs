namespace HeroPath.Modules.Game.Application;

/// <summary>Speed of Play: the wait between two steps. The board animation uses the same duration.</summary>
public enum Tempo
{
    Slow,
    Medium,
    Fast,
}

/// <summary>Maps a <see cref="Tempo"/> to its step duration.</summary>
public static class TempoExtensions
{
    extension(Tempo tempo)
    {
        /// <summary>Slow 900 ms, medium 500 ms, fast 200 ms.</summary>
        public TimeSpan Duration => tempo switch
        {
            Tempo.Slow => TimeSpan.FromMilliseconds(900),
            Tempo.Medium => TimeSpan.FromMilliseconds(500),
            Tempo.Fast => TimeSpan.FromMilliseconds(200),
            _ => throw new ArgumentOutOfRangeException(nameof(tempo)),
        };
    }
}
