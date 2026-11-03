using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.UI.Resources;
using HeroPath.Shared.UI.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Levels.UI.Components;

/// <summary>The eight editor tools as toggle buttons: picture and text, the active one marked by a thick frame and a check mark.</summary>
public partial class EditorToolbar : LocalizedComponentBase
{
    private static IReadOnlyList<EditorTool> Tools { get; } = Enum.GetValues<EditorTool>();

    [Parameter]
    public EditorTool Tool { get; set; }

    [Parameter]
    public EventCallback<EditorTool> ToolChanged { get; set; }

    [Inject]
    private IStringLocalizer<LevelsResources> Localizer { get; set; } = default!;
}
