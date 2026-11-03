using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.UI.Resources;
using HeroPath.Shared.UI.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Levels.UI.Components;

/// <summary>Shows whether the level can be saved: the shortest solution when it is solvable, otherwise a kid-friendly reason. The path itself is never shown.</summary>
public partial class EditorStatus : LocalizedComponentBase
{
    [Parameter, EditorRequired]
    public required LevelCheckResult Check { get; set; }

    [Inject]
    private IStringLocalizer<LevelsResources> Localizer { get; set; } = default!;

    private string StateName => Check.Status switch
    {
        LevelCheckStatus.Valid => "solvable",
        LevelCheckStatus.Invalid => "invalid",
        _ => "unsolvable",
    };

    private string Glyph => Check.Status switch
    {
        LevelCheckStatus.Valid => "✓",
        LevelCheckStatus.Invalid => "i",
        _ => "!",
    };

    private string Text => Check.Status switch
    {
        LevelCheckStatus.Valid => Check.MinimumSteps is 1
            ? Localizer["Editor_Status_Solvable_One", 1].Value
            : Localizer["Editor_Status_Solvable_Other", Check.MinimumSteps ?? 0].Value,
        LevelCheckStatus.GoalUnreachable => Localizer["Editor_Status_GoalUnreachable"].Value,
        LevelCheckStatus.StrawberriesUnreachable => Localizer["Editor_Status_StrawberriesUnreachable"].Value,
        LevelCheckStatus.TooManySteps => Localizer["Editor_Status_TooManySteps", LevelLimits.MaxSteps].Value,
        _ => Check.Errors.Any(error => error.Kind == LevelValidationErrorKind.InvalidName)
            ? Localizer["Editor_Status_NameMissing"].Value
            : Localizer["Editor_Status_Invalid"].Value,
    };
}
