using HeroPath.Modules.Game.UI.Resources;
using HeroPath.Shared.UI.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Game.UI.Components;

/// <summary>
/// Collapsible list of the keyboard shortcuts of the play page (the mapping itself is <c>ShortcutKeys</c>). The open state is bound
/// (<see cref="Open"/>), so the page can close the list with Escape without touching the document.
/// </summary>
public partial class ShortcutHelp : LocalizedComponentBase
{
    [Parameter]
    public bool Open { get; set; }

    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    [Inject]
    private IStringLocalizer<GameResources> Localizer { get; set; } = default!;

    private Task ToggleAsync()
    {
        Open = !Open;
        return OpenChanged.InvokeAsync(Open);
    }
}
