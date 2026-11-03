using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.UI.Resources;
using HeroPath.Shared.UI.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Levels.UI.Components;

/// <summary>The child's own levels as a list of buttons (names rendered as plain text) plus the «New level» button.</summary>
public partial class CustomLevelList : LocalizedComponentBase
{
    [Parameter, EditorRequired]
    public required IReadOnlyList<LevelDefinition> Levels { get; set; }

    /// <summary>The id of the level open in the editor, marked with <c>aria-current</c>.</summary>
    [Parameter]
    public string? OpenId { get; set; }

    [Parameter]
    public EventCallback<string> LevelOpened { get; set; }

    [Parameter]
    public EventCallback NewRequested { get; set; }

    /// <summary>Raised with the id of the level whose export button was clicked.</summary>
    [Parameter]
    public EventCallback<string> ExportRequested { get; set; }

    /// <summary>Raised with the id of the level whose delete button was clicked. The parent asks before it deletes.</summary>
    [Parameter]
    public EventCallback<string> DeleteRequested { get; set; }

    [Inject]
    private IStringLocalizer<LevelsResources> Localizer { get; set; } = default!;
}
