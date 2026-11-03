using System.Text.RegularExpressions;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Application;

/// <summary>Checks a <see cref="LevelDefinition"/> against all level rules and collects every violation.</summary>
public static partial class LevelValidator
{
    /// <summary>Never throws for a non-null level, even if lists or entries are null.</summary>
    public static IReadOnlyList<LevelValidationError> Validate(LevelDefinition level)
    {
        ArgumentNullException.ThrowIfNull(level);

        var errors = new List<LevelValidationError>();

        AddIf(level.SchemaVersion != LevelDefinition.CurrentSchemaVersion, LevelValidationErrorKind.UnsupportedSchemaVersion);
        AddIf(level.Id is null || !IdPattern().IsMatch(level.Id), LevelValidationErrorKind.InvalidId);
        AddIf(level.Name is not null && !IsValidName(level.Name), LevelValidationErrorKind.InvalidName);
        AddIf(level.Width is < LevelLimits.MinGridSize or > LevelLimits.MaxGridSize, LevelValidationErrorKind.WidthOutOfRange);
        AddIf(level.Height is < LevelLimits.MinGridSize or > LevelLimits.MaxGridSize, LevelValidationErrorKind.HeightOutOfRange);
        AddIf(!level.Start.IsInside(level.Width, level.Height), LevelValidationErrorKind.StartOutsideGrid, level.Start);
        AddIf(!level.Goal.IsInside(level.Width, level.Height), LevelValidationErrorKind.GoalOutsideGrid, level.Goal);
        AddIf(level.Start == level.Goal, LevelValidationErrorKind.StartEqualsGoal, level.Start);

        var obstaclesValid = IsListValid(level.Obstacles);
        var itemsValid = IsListValid(level.Items);
        AddIf(!obstaclesValid, LevelValidationErrorKind.ObstaclesInvalid);
        AddIf(!itemsValid, LevelValidationErrorKind.ItemsInvalid);

        var objectCount = (obstaclesValid ? level.Obstacles.Count : 0) + (itemsValid ? level.Items.Count : 0);
        var maxObjects = Math.Clamp(level.Width, LevelLimits.MinGridSize, LevelLimits.MaxGridSize)
            * Math.Clamp(level.Height, LevelLimits.MinGridSize, LevelLimits.MaxGridSize);
        if (objectCount > maxObjects)
        {
            errors.Add(new(LevelValidationErrorKind.TooManyObjects));
            return errors;
        }

        var occupied = new HashSet<Position>();
        if (obstaclesValid)
        {
            foreach (var obstacle in level.Obstacles)
            {
                CheckObject(obstacle.GetPosition(), isObstacle: true);
            }
        }

        if (itemsValid)
        {
            foreach (var item in level.Items)
            {
                CheckObject(item.GetPosition(), isObstacle: false);
            }

            AddIf(level.RequireAllItems && level.Items.All(i => i.Kind != ItemKind.Strawberry), LevelValidationErrorKind.RequiredItemsMissing);
        }

        return errors;

        void AddIf(bool condition, LevelValidationErrorKind kind, Position? position = null)
        {
            if (condition)
            {
                errors.Add(new(kind, position));
            }
        }

        void CheckObject(Position position, bool isObstacle)
        {
            if (!position.IsInside(level.Width, level.Height))
            {
                errors.Add(new(isObstacle ? LevelValidationErrorKind.ObstacleOutsideGrid : LevelValidationErrorKind.ItemOutsideGrid, position));
            }
            else if (position == level.Start)
            {
                errors.Add(new(isObstacle ? LevelValidationErrorKind.ObstacleOnStart : LevelValidationErrorKind.ItemOnStart, position));
            }
            else if (isObstacle && position == level.Goal)
            {
                errors.Add(new(LevelValidationErrorKind.ObstacleOnGoal, position));
            }
            else if (!occupied.Add(position))
            {
                errors.Add(new(LevelValidationErrorKind.CellOccupiedTwice, position));
            }
        }
    }

    private static bool IsListValid<T>(IReadOnlyList<T>? list) where T : class =>
        list is not null && !list.Any(entry => entry is null);

    private static bool IsValidName(string name) =>
        name.Length is >= 1 and <= LevelLimits.MaxNameLength
        && !string.IsNullOrWhiteSpace(name)
        && !name.Any(char.IsControl);

    [GeneratedRegex(@"^[a-z0-9-]{1,40}\z")]
    private static partial Regex IdPattern();
}
