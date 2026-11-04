using HeroPath.Modules.Game.UI.Resources;
using HeroPath.Shared.UI.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Game.UI.Components;

/// <summary>
/// Collapsible list of the keyboard shortcuts of the play page (the mapping itself is <c>ShortcutKeys</c>). The open state is bound
/// (<see cref="Open"/>), so the page can close the list (Escape, start of a run) without touching the document. While <see cref="Disabled"/> the summary does not open it.
/// </summary>
public partial class ShortcutHelp : LocalizedComponentBase
{
    [Parameter]
    public bool Open { get; set; }

    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    [Inject]
    private IStringLocalizer<GameResources> Localizer { get; set; } = default!;

    [Parameter]
    public bool Disabled { get; set; }

    private Task ToggleAsync()
    {
        if (Disabled && !Open)
        {
            return Task.CompletedTask;
        }

        Open = !Open;
        return OpenChanged.InvokeAsync(Open);
    }
}
