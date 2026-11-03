namespace HeroPath.Modules.Levels.Domain;

/// <summary>Why an editor click changed nothing. The UI maps each value to a localized hint.</summary>
public enum EditHint
{
    None,
    StartOnGoal,
    GoalOnStart,
    ObstacleOnStart,
    ObstacleOnGoal,
    ItemOnStart,
    TooManyStrawberries,
    TooManyPizzas,
}
