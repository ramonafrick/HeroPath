using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Game.UI.Services;

namespace HeroPath.Modules.Game.Tests.Components;

public class ProgramDragStateTests
{
    private readonly GameSession _session = new();
    private readonly ExecutionRunner _runner;
    private readonly ProgramDragState _drag;

    public ProgramDragStateTests()
    {
        _session.Load(TestLevels.DemoGrid());
        _runner = new ExecutionRunner(_session, TimeProvider.System);
        _drag = new ProgramDragState(_session);
    }

    private void LockProgram()
    {
        _session.Add(Direction.Up);
        _runner.StepOnce();
        Assert.False(_session.IsEditable);
    }

    [Fact]
    public void Start_WhileLocked_DoesNothing()
    {
        LockProgram();
        var changes = 0;
        _drag.Changed += () => changes++;

        _drag.Start(new DragSource.NewMove(Direction.Left));

        Assert.False(_drag.IsDragging);
        Assert.Null(_drag.Source);
        Assert.Equal(0, changes);
    }

    [Fact]
    public void Start_WhileEditable_SetsSourceAndRaisesChanged()
    {
        var changes = 0;
        _drag.Changed += () => changes++;

        _drag.Start(new DragSource.NewRepeat());

        Assert.True(_drag.IsDragging);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void Drop_AfterRunStarted_IsNoOpAndEndsDrag()
    {
        _session.Add(Direction.Up);
        _drag.Start(new DragSource.NewMove(Direction.Left));
        _runner.StepOnce();

        _drag.Drop(BlockSlot.Root(0));

        Assert.Equal([Direction.Up], TestLevels.Directions(_session));
        Assert.False(_drag.IsDragging);
    }

    [Fact]
    public void Drop_ExistingBlockAfterRunStarted_IsNoOpAndEndsDrag()
    {
        _session.Add(Direction.Up);
        _session.Add(Direction.Right);
        _drag.Start(new DragSource.Existing(_session.Program.Commands[1].Id));
        _runner.StepOnce();

        _drag.Drop(BlockSlot.Root(0));

        Assert.Equal([Direction.Up, Direction.Right], TestLevels.Directions(_session));
        Assert.False(_drag.IsDragging);
    }

    [Fact]
    public void DropOnPalette_WithNewBlockSource_DoesNothingAndEndsDrag()
    {
        _session.Add(Direction.Up);
        _drag.Start(new DragSource.NewMove(Direction.Left));

        _drag.DropOnPalette();

        Assert.Equal([Direction.Up], TestLevels.Directions(_session));
        Assert.False(_drag.IsDragging);
    }

    [Fact]
    public void DropOnPalette_WithExistingBlock_RemovesIt()
    {
        _session.Add(Direction.Up);
        _drag.Start(new DragSource.Existing(_session.Program.Commands[0].Id));

        _drag.DropOnPalette();

        Assert.Empty(_session.Program.Commands);
        Assert.False(_drag.IsDragging);
    }

    [Fact]
    public void End_RaisesChangedOnceAndIsIdempotent()
    {
        _drag.Start(new DragSource.NewMove(Direction.Up));
        var changes = 0;
        _drag.Changed += () => changes++;

        _drag.End();
        _drag.End();

        Assert.False(_drag.IsDragging);
        Assert.Equal(1, changes);
    }
}
