namespace HeroPath.Modules.Game.Application;

/// <summary>Speed of Play: the wait between two steps. The board animation uses the same duration.</summary>
public enum Tempo
{
    /// <summary>900 ms between two steps.</summary>
    Slow,

    /// <summary>500 ms between two steps (default).</summary>
    Medium,

    /// <summary>200 ms between two steps.</summary>
    Fast,
}
