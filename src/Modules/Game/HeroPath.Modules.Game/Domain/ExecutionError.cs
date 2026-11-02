namespace HeroPath.Modules.Game.Domain;

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
