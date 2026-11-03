namespace HeroPath.Modules.Levels.Application;

/// <summary>Result of checking a level for saving. The UI maps each value to a localized text.</summary>
public enum LevelCheckStatus
{
    Valid,

    /// <summary>The level breaks a validation rule.</summary>
    Invalid,

    /// <summary>No path leads from start to goal.</summary>
    GoalUnreachable,

    /// <summary>The goal is reachable but not after all required strawberries.</summary>
    StrawberriesUnreachable,

    /// <summary>The shortest solution needs more moves than a run may execute.</summary>
    TooManySteps,
}
