namespace HeroPath.Modules.Game.Domain;

/// <summary>Reason why a run failed. The UI maps each value to a localized message.</summary>
public enum ExecutionError
{
    EmptyProgram,
    EmptyRepeat,
    Obstacle,
    OutOfBounds,
    StepLimitExceeded,
    GoalNotReached,
    ItemsMissing,
}
