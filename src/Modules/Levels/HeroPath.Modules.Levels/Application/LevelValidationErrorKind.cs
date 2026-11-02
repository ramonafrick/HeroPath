namespace HeroPath.Modules.Levels.Application;

public enum LevelValidationErrorKind
{
    InvalidJson,
    UnsupportedSchemaVersion,
    WidthOutOfRange,
    HeightOutOfRange,
    StartOutsideGrid,
    GoalOutsideGrid,
    StartEqualsGoal,
    ObstaclesMissing,
    ItemsMissing,
    ObstacleOutsideGrid,
    ItemOutsideGrid,
    ObstacleOnStart,
    ObstacleOnGoal,
    ItemOnStart,
    CellOccupiedTwice,
    RequiredItemsMissing,
}
