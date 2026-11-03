using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.UI.Resources;
using HeroPath.Shared.UI.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Game.UI.Components;

/// <summary>Small picture and name of the chosen hero above the board, with a button back to the start page.</summary>
public partial class HeroBadge : LocalizedComponentBase
{
    [Inject]
    private HeroChoice Choice { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private IStringLocalizer<GameResources> Localizer { get; set; } = default!;

    private void Change() => Navigation.NavigateTo(string.Empty);
}
