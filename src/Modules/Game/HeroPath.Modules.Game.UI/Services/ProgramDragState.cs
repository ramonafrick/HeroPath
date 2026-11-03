using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;

namespace HeroPath.Modules.Game.UI.Services;

/// <summary>What is being dragged: a new move block, a new repeat block or an existing block of the program.</summary>
public abstract record DragSource
{
    private DragSource()
    {
    }

    /// <summary>A new move block from the palette.</summary>
    public sealed record NewMove(Direction Direction) : DragSource;

    /// <summary>A new repeat block from the palette.</summary>
    public sealed record NewRepeat : DragSource;

    /// <summary>An existing block of the program.</summary>
    public sealed record Existing(Guid Id) : DragSource;
}

/// <summary>
/// UI state of a drag &amp; drop in progress (what is dragged), shared by palette and editor. Holds no rules: every question about a
/// target goes to the <see cref="GameSession"/>, and every drop is validated again, so a program that changed since the drag
/// started (for example a run) makes the drop a no-op. Dragging does not change the selection. Nothing here throws.
/// </summary>
public sealed class ProgramDragState(GameSession session)
{
    /// <summary>Raised when a drag starts or ends.</summary>
    public event Action? Changed;

    /// <summary>The block being dragged, or <c>null</c>.</summary>
    public DragSource? Source { get; private set; }

    /// <summary>Whether a drag is in progress.</summary>
    public bool IsDragging => Source is not null;

    /// <summary>Whether the dragged block is an existing program block (the palette then acts as a delete target).</summary>
    public bool IsDraggingExisting => Source is DragSource.Existing;

    /// <summary>Starts a drag. Ignored (stays not dragging) while the program is locked.</summary>
    public void Start(DragSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!session.IsEditable)
        {
            End();
            return;
        }

        Source = source;
        Changed?.Invoke();
    }

    /// <summary>Ends the drag, whether it was dropped or not.</summary>
    public void End()
    {
        if (Source is null)
        {
            return;
        }

        Source = null;
        Changed?.Invoke();
    }

    /// <summary>Whether dropping the dragged block at <paramref name="slot"/> would change the program.</summary>
    public bool IsValidTarget(BlockSlot slot) => Source switch
    {
        DragSource.NewMove => session.CanAddAt(slot),
        DragSource.NewRepeat => session.CanAddRepeatAt(slot),
        DragSource.Existing existing => session.CanMove(existing.Id, slot),
        _ => false,
    };

    /// <summary>Whether dropping on the palette would remove the dragged block.</summary>
    public bool IsValidDelete => Source is DragSource.Existing existing && session.CanRemove(existing.Id);

    /// <summary>Drops the dragged block at <paramref name="slot"/> if that is valid, then ends the drag.</summary>
    public void Drop(BlockSlot slot)
    {
        switch (Source)
        {
            case DragSource.NewMove move:
                session.Add(move.Direction, slot);
                break;
            case DragSource.NewRepeat:
                session.AddRepeat(slot);
                break;
            case DragSource.Existing existing:
                session.Move(existing.Id, slot);
                break;
        }

        End();
    }

    /// <summary>Drops the dragged block onto the palette: removes an existing block (with its body), then ends the drag.</summary>
    public void DropOnPalette()
    {
        if (Source is DragSource.Existing existing)
        {
            session.Remove(existing.Id);
        }

        End();
    }
}
