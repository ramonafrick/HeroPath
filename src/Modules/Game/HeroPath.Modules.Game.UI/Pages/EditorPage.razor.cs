using HeroPath.Modules.Game.UI.Resources;
using HeroPath.Modules.Levels.UI.Resources;
using HeroPath.Shared.UI.Components;
using HeroPath.Shared.UI.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Game.UI.Pages;

/// <summary>
/// The routable editor page (<c>/editor</c> and <c>/editor/{LevelId}</c>). It lives in Game.UI so that later it can reach the hero choice for
/// «Try it»; the editing itself is the <c>LevelEditor</c> component of Levels.UI. An unknown id opens a new level.
/// </summary>
public partial class EditorPage : LocalizedComponentBase
{
    [Parameter]
    public string? LevelId { get; set; }

    [Inject]
    private IStringLocalizer<GameResources> GameLocalizer { get; set; } = default!;

    [Inject]
    private IStringLocalizer<LevelsResources> LevelsLocalizer { get; set; } = default!;

    [Inject]
    private IStringLocalizer<SharedResources> SharedLocalizer { get; set; } = default!;
}
