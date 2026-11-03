using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.UI.Resources;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Levels.UI.Components;

/// <summary>Maps editor state (hints, save outcomes, cell content) to localized texts, so the editor component stays about behaviour.</summary>
internal static class EditorTexts
{
    /// <summary>The text of the last refused click, or of the last resize that removed things; <c>null</c> when there is none.</summary>
    public static string? Hint(IStringLocalizer<LevelsResources> localizer, EditHint hint, int removedCount)
    {
        if (hint != EditHint.None)
        {
            return hint switch
            {
                EditHint.TooManyStrawberries => localizer["Editor_Hint_TooManyStrawberries", LevelLimits.MaxStrawberries].Value,
                EditHint.TooManyPizzas => localizer["Editor_Hint_TooManyPizzas", LevelLimits.MaxPizzas].Value,
                _ => localizer[$"Editor_Hint_{hint}"].Value,
            };
        }

        return removedCount switch
        {
            0 => null,
            1 => localizer["Editor_Removed_One", 1].Value,
            _ => localizer["Editor_Removed_Other", removedCount].Value,
        };
    }

    public static string? Saved(IStringLocalizer<LevelsResources> localizer, SaveOutcome? outcome) => outcome switch
    {
        SaveOutcome.Saved => localizer["Editor_Save_Saved"].Value,
        SaveOutcome.LimitReached => localizer["Editor_Save_LimitReached", LevelLimits.MaxCustomLevels].Value,
        SaveOutcome.StorageFailed => localizer["Editor_Save_StorageFailed"].Value,
        SaveOutcome.Invalid => localizer["Editor_Save_Invalid"].Value,
        _ => null,
    };

    public static string CellLabel(IStringLocalizer<LevelsResources> localizer, Grid grid, Position position)
    {
        var item = grid.GetItem(position);
        var content = position == grid.Start ? "Cell_Start"
            : position == grid.Goal ? (item switch { ItemKind.Strawberry => "Cell_Goal_Strawberry", ItemKind.Pizza => "Cell_Goal_Pizza", _ => "Cell_Goal" })
            : grid.GetObstacle(position) is { } obstacle ? $"Cell_{obstacle}"
            : item is { } itemKind ? $"Cell_{itemKind}"
            : "Cell_Empty";
        return localizer["Cell_Label", position.X + 1, position.Y + 1, localizer[content].Value].Value;
    }
}
