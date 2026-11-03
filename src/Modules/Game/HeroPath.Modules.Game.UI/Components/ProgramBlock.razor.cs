using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Game.UI.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Game.UI.Components;

public partial class ProgramBlock : SessionComponentBase
{
    [Parameter, EditorRequired]
    public required Command Command { get; set; }

    [Parameter, EditorRequired]
    public required IReadOnlyList<int> Path { get; set; }

    [Inject]
    private IStringLocalizer<GameResources> Localizer { get; set; } = default!;
}
