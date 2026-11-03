using HeroPath.Modules.Game.Domain;
using Microsoft.AspNetCore.Components;

namespace HeroPath.Modules.Game.UI.Components;

public partial class DropZone : DragAwareComponentBase
{
    private bool _hover;

    [Parameter, EditorRequired]
    public required BlockSlot Slot { get; set; }

    /// <summary>Number of the repeat block whose body this zone belongs to (such as <c>3.2</c>), or <c>root</c>.</summary>
    [Parameter, EditorRequired]
    public required string ParentNumber { get; set; }

    /// <summary>Whether this is the last zone of its list.</summary>
    [Parameter]
    public bool IsEnd { get; set; }

    private string SlotText => $"{ParentNumber}:{Slot.Index}";

    private void HandleDrop()
    {
        _hover = false;
        if (Drag.IsValidTarget(Slot))
        {
            Drag.Drop(Slot);
        }
        else
        {
            Drag.End();
        }
    }
}
