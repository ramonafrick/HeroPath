using HeroPath.Modules.Game.UI.Services;
using Microsoft.AspNetCore.Components;

namespace HeroPath.Modules.Game.UI.Components;

/// <summary>Adds the <see cref="ProgramDragState"/> to <see cref="SessionComponentBase"/>: re-renders when a drag starts or ends.</summary>
public abstract class DragAwareComponentBase : SessionComponentBase
{
    [Inject]
    protected ProgramDragState Drag { get; set; } = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Drag.Changed += HandleChanged;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Drag.Changed -= HandleChanged;
        }

        base.Dispose(disposing);
    }
}
