namespace HeroPath.Modules.Levels.Application;

/// <summary>Result of <see cref="LevelRepository.ImportAsync"/>.</summary>
public enum ImportOutcome
{
    /// <summary>The level was imported as a new custom level.</summary>
    Imported,

    /// <summary>The text is not a valid level (broken JSON, too large, rule violations).</summary>
    InvalidFile,

    /// <summary>The level is valid but cannot be saved (not solvable, or its shortest solution is longer than the step limit).</summary>
    NotSavable,

    /// <summary>The child already has <c>LevelLimits.MaxCustomLevels</c> levels.</summary>
    LimitReached,

    /// <summary>The storage could not be written; nothing changed.</summary>
    StorageFailed,
}
