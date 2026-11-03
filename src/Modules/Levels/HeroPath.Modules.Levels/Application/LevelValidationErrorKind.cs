namespace HeroPath.Modules.Levels.Application;

/// <summary>Kinds of level problems. The UI maps each kind to a localized message.</summary>
public enum LevelValidationErrorKind
{
    /// <summary>Input was empty, too long, too deeply nested or not a valid level document.</summary>
    InvalidJson,
    UnsupportedSchemaVersion,

    /// <summary>Id does not match <c>[a-z0-9-]{1,40}</c>.</summary>
    InvalidId,

    /// <summary>Name is empty, whitespace-only, too long or contains control characters.</summary>
    InvalidName,
    WidthOutOfRange,
    HeightOutOfRange,
    StartOutsideGrid,
    GoalOutsideGrid,
    StartEqualsGoal,

    /// <summary>The obstacle list or one of its entries is null.</summary>
    ObstaclesInvalid,

    /// <summary>The item list or one of its entries is null.</summary>
    ItemsInvalid,

    /// <summary>More obstacles and items than the grid has cells; per-object checks are skipped.</summary>
    TooManyObjects,
    ObstacleOutsideGrid,
    ItemOutsideGrid,
    ObstacleOnStart,
    ObstacleOnGoal,
    ItemOnStart,
    CellOccupiedTwice,
    RequiredItemsMissing,
}
