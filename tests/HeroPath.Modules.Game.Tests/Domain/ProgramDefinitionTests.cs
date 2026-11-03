using HeroPath.Modules.Game.Domain;

namespace HeroPath.Modules.Game.Tests.Domain;

public class ProgramDefinitionTests
{
    private static readonly MoveCommand _a = new(Direction.Up);
    private static readonly MoveCommand _b = new(Direction.Left);
    private static readonly MoveCommand _c = new(Direction.Right);
    private static readonly RepeatCommand _inner = new(2, [_c]);
    private static readonly RepeatCommand _outer = new(3, [_b, _inner]);
    private static readonly ProgramDefinition _program = new([_a, new MoveCommand(Direction.Down), _outer]);

    [Fact]
    public void PathOf_TopLevel_IsOneBasedIndex() =>
        Assert.Equal([1], _program.PathOf(_a.Id));

    [Fact]
    public void PathOf_InBody_IsHierarchical()
    {
        Assert.Equal([3], _program.PathOf(_outer.Id));
        Assert.Equal([3, 1], _program.PathOf(_b.Id));
        Assert.Equal([3, 2], _program.PathOf(_inner.Id));
        Assert.Equal([3, 2, 1], _program.PathOf(_c.Id));
    }

    [Fact]
    public void PathOf_UnknownId_IsNull() =>
        Assert.Null(_program.PathOf(Guid.NewGuid()));

    [Fact]
    public void Find_ReturnsBlockAnywhereInTree()
    {
        Assert.Same(_c, _program.Find(_c.Id));
        Assert.Null(_program.Find(Guid.NewGuid()));
    }

    [Fact]
    public void SlotOf_ReturnsParentAndIndex()
    {
        Assert.Equal(new BlockSlot(null, 2), _program.SlotOf(_outer.Id));
        Assert.Equal(new BlockSlot(_outer.Id, 1), _program.SlotOf(_inner.Id));
        Assert.Null(_program.SlotOf(Guid.NewGuid()));
    }

    [Fact]
    public void DepthAt_ReportsNestingOfTheList()
    {
        Assert.Equal(0, _program.DepthAt(null));
        Assert.Equal(1, _program.DepthAt(_outer.Id));
        Assert.Equal(2, _program.DepthAt(_inner.Id));
        Assert.Null(_program.DepthAt(_a.Id));
        Assert.Null(_program.DepthAt(Guid.NewGuid()));
    }

    [Fact]
    public void CountAll_CountsRecursively()
    {
        Assert.Equal(6, _program.CountAll());
        Assert.Equal(0, ProgramDefinition.Empty.CountAll());
    }
}
