namespace HeroPath.Modules.Levels.Application;

/// <summary>Result of <see cref="LevelRepository.SaveAsync"/>.</summary>
public enum SaveOutcome
{
    /// <summary>The level is stored (new or overwritten).</summary>
    Saved,

    /// <summary>A new level was refused because the child already has <c>LevelLimits.MaxCustomLevels</c> levels.</summary>
    LimitReached,

    /// <summary>The level is not valid, not solvable, has no name or uses the id of a standard level.</summary>
    Invalid,

    /// <summary>The storage could not be written; nothing changed.</summary>
    StorageFailed,
}
