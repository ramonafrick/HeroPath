using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.UI.Resources;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Levels.UI;

public static class LevelDisplayExtensions
{
    /// <summary>
    /// The name to show for a level: the child's own name of a custom level (user text, never translated, render it as text only),
    /// otherwise the localized <c>Level_&lt;id&gt;</c> text of a standard level.
    /// </summary>
    public static string NameOf(this IStringLocalizer<LevelsResources> localizer, LevelDefinition level)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        ArgumentNullException.ThrowIfNull(level);
        return level.Name ?? localizer[$"Level_{level.Id}"].Value;
    }
}
