using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Game.UI.Resources;
using HeroPath.Modules.Levels.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Game.UI.Components;

/// <summary>Shows <see cref="GameSession.Message"/> with an icon per kind and the localized text. The hero name comes from the <see cref="HeroChoice"/>.</summary>
public partial class MessageBox : SessionComponentBase
{
    [Inject]
    private HeroChoice Choice { get; set; } = default!;

    [Inject]
    private LevelProgress Progress { get; set; } = default!;

    [Inject]
    private IStringLocalizer<GameResources> Localizer { get; set; } = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Progress.Changed += HandleChanged;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Progress.Changed -= HandleChanged;
        }

        base.Dispose(disposing);
    }

    private string Text(GameMessage message)
    {
        var number = message.BlockPath is { } path ? string.Join(".", path) : string.Empty;
        return message.Code switch
        {
            GameMessageCode.Obstacle => Localizer[$"Message_Obstacle_{message.Obstacle ?? ObstacleKind.Tree}", number],
            GameMessageCode.OutOfBounds => Localizer["Message_OutOfBounds", number],
            GameMessageCode.GoalNotReached => Localizer["Message_GoalNotReached", Choice.Name],
            GameMessageCode.ItemsMissing when message.Count == 1 => Localizer["Message_ItemsMissing_One"],
            GameMessageCode.ItemsMissing => Localizer["Message_ItemsMissing_Other", message.Count],
            GameMessageCode.StepLimitExceeded => Localizer["Message_StepLimitExceeded", ProgramLimits.MaxExecutedSteps],
            GameMessageCode.Succeeded when Progress.AllSolved => $"{SucceededText(message)} {Localizer["Message_AllSolved"]}",
            GameMessageCode.Succeeded => SucceededText(message),
            _ => Localizer[$"Message_{message.Code}"],
        };
    }

    private string SucceededText(GameMessage message) => message.PizzaCount switch
    {
        0 => Localizer["Message_Succeeded", Choice.Name],
        1 => Localizer["Message_Succeeded_Pizza_One", Choice.Name],
        _ => Localizer["Message_Succeeded_Pizza_Other", Choice.Name, message.PizzaCount],
    };
}
