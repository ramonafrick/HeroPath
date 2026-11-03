using HeroPath.Modules.Game.UI.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Game.UI.Components;

public partial class ProgramEditor : SessionComponentBase
{
    [Inject]
    private IStringLocalizer<GameResources> Localizer { get; set; } = default!;
}
