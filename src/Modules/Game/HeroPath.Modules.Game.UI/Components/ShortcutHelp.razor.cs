using HeroPath.Modules.Game.UI.Resources;
using HeroPath.Shared.UI.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Game.UI.Components;

/// <summary>Collapsible list of the keyboard shortcuts of the play page (the mapping itself is <c>ShortcutKeys</c>).</summary>
public partial class ShortcutHelp : LocalizedComponentBase
{
    [Inject]
    private IStringLocalizer<GameResources> Localizer { get; set; } = default!;
}
