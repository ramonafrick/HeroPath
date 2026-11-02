using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Application;

public static class LevelValidator
{
    public static IReadOnlyList<LevelValidationError> Validate(LevelDefinition level)
    {
        ArgumentNullException.ThrowIfNull(level);

        var errors = new List<LevelValidationError>();

        if (level.SchemaVersion != LevelDefinition.CurrentSchemaVersion)
        {
            errors.Add(new(LevelValidationErrorKind.UnsupportedSchemaVersion));
        }

        if (level.Width is < LevelLimits.MinGridSize or > LevelLimits.MaxGridSize)
        {
            errors.Add(new(LevelValidationErrorKind.WidthOutOfRange));
        }

        if (level.Height is < LevelLimits.MinGridSize or > LevelLimits.MaxGridSize)
        {
            errors.Add(new(LevelValidationErrorKind.HeightOutOfRange));
        }

        var startInside = IsInside(level, level.Start);
        var goalInside = IsInside(level, level.Goal);

        if (!startInside)
        {
            errors.Add(new(LevelValidationErrorKind.StartOutsideGrid, level.Start));
        }

        if (!goalInside)
        {
            errors.Add(new(LevelValidationErrorKind.GoalOutsideGrid, level.Goal));
        }

        if (level.Start == level.Goal)
        {
            errors.Add(new(LevelValidationErrorKind.StartEqualsGoal, level.Start));
        }

        var occupied = new HashSet<Position>();

        if (level.Obstacles is null || level.Obstacles.Any(o => o is null))
        {
            errors.Add(new(LevelValidationErrorKind.ObstaclesMissing));
        }
        else
        {
            foreach (var position in level.Obstacles.Select(o => o.GetPosition()))
            {
                if (!IsInside(level, position))
                {
                    errors.Add(new(LevelValidationErrorKind.ObstacleOutsideGrid, position));
                    continue;
                }

                if (position == level.Start)
                {
                    errors.Add(new(LevelValidationErrorKind.ObstacleOnStart, position));
                }
                else if (position == level.Goal)
                {
                    errors.Add(new(LevelValidationErrorKind.ObstacleOnGoal, position));
                }
                else if (!occupied.Add(position))
                {
                    errors.Add(new(LevelValidationErrorKind.CellOccupiedTwice, position));
                }
            }
        }

        if (level.Items is null || level.Items.Any(i => i is null))
        {
            errors.Add(new(LevelValidationErrorKind.ItemsMissing));
        }
        else
        {
            foreach (var position in level.Items.Select(i => i.GetPosition()))
            {
                if (!IsInside(level, position))
                {
                    errors.Add(new(LevelValidationErrorKind.ItemOutsideGrid, position));
                    continue;
                }

                if (position == level.Start)
                {
                    errors.Add(new(LevelValidationErrorKind.ItemOnStart, position));
                }
                else if (!occupied.Add(position))
                {
                    errors.Add(new(LevelValidationErrorKind.CellOccupiedTwice, position));
                }
            }

            if (level.RequireAllItems && level.Items.All(i => i.Kind != ItemKind.Strawberry))
            {
                errors.Add(new(LevelValidationErrorKind.RequiredItemsMissing));
            }
        }

        return errors;
    }

    private static bool IsInside(LevelDefinition level, Position position) =>
        position.X >= 0 && position.X < level.Width && position.Y >= 0 && position.Y < level.Height;
}
