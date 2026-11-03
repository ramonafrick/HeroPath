using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Game.UI.Resources;
using HeroPath.Modules.Levels.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Game.UI.Components;

/// <summary>Shows <see cref="GameSession.Message"/> with an icon per kind and the localized text. The hero name is a fixed placeholder until the start dialog exists.</summary>
public partial class MessageBox : SessionComponentBase
{
    private const string HeroName = "Max";

    [Inject]
    private IStringLocalizer<GameResources> Localizer { get; set; } = default!;

    private string Text(GameMessage message)
    {
        var number = message.BlockPath is { } path ? string.Join(".", path) : string.Empty;
        return message.Code switch
        {
            GameMessageCode.Obstacle => Localizer[$"Message_Obstacle_{message.Obstacle ?? ObstacleKind.Tree}", number],
            GameMessageCode.OutOfBounds => Localizer["Message_OutOfBounds", number],
            GameMessageCode.GoalNotReached => Localizer["Message_GoalNotReached", HeroName],
            GameMessageCode.ItemsMissing when message.Count == 1 => Localizer["Message_ItemsMissing_One"],
            GameMessageCode.ItemsMissing => Localizer["Message_ItemsMissing_Other", message.Count],
            GameMessageCode.StepLimitExceeded => Localizer["Message_StepLimitExceeded", ProgramLimits.MaxExecutedSteps],
            GameMessageCode.Succeeded => Localizer["Message_Succeeded", HeroName],
            _ => Localizer[$"Message_{message.Code}"],
        };
    }
}
