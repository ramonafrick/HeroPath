namespace HeroPath.Modules.Game.Application;

/// <summary>Which situation a <see cref="GameMessage"/> describes. The UI maps every code to one localized text.</summary>
public enum GameMessageCode
{
    Obstacle,
    OutOfBounds,
    GoalNotReached,
    ItemsMissing,
    StepLimitExceeded,
    Succeeded,
    EmptyRepeat,
    EmptyProgram,
    ProgramFull,
    NestingTooDeep,
}
