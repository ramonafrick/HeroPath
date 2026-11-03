using HeroPath.Modules.Game.UI.Resources;
using HeroPath.Modules.Levels.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Game.UI.Components;

/// <summary>Shows, per item kind that occurs in the level, how many items the hero has collected in the current run.</summary>
public partial class ItemCounter : SessionComponentBase
{
    [Inject]
    private IStringLocalizer<GameResources> Localizer { get; set; } = default!;

    private string Text(ItemKind kind, int collected, int total) => kind switch
    {
        ItemKind.Strawberry when total == 1 => Localizer["ItemCounter_Strawberry_One", collected, total],
        ItemKind.Strawberry => Localizer["ItemCounter_Strawberry_Other", collected, total],
        _ => Localizer["ItemCounter_Pizza", collected, total],
    };
}
