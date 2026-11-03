using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;

namespace HeroPath.Modules.Game.Tests.Application;

public class GameSessionDragTests
{
    private static GameSession SessionWith(params Direction[] directions)
    {
        var session = new GameSession();
        TestLevels.Fill(session, directions);
        return session;
    }

    private static GameSession Locked()
    {
        var session = new GameSession();
        session.Load(TestLevels.Demo);
        session.Add(Direction.Up);
        new ExecutionRunner(session, TestLevels.NewProgress(), TimeProvider.System).StepOnce();
        return session;
    }

    [Fact]
    public void AddAtSlot_InsertsMoveBetweenBlocks()
    {
        var session = SessionWith(Direction.Up, Direction.Down);

        session.Add(Direction.Left, BlockSlot.Root(1));

        Assert.Equal([Direction.Up, Direction.Left, Direction.Down], TestLevels.Directions(session));
    }

    [Fact]
    public void AddRepeatAtSlot_IntoRepeatBody_Nests()
    {
        var session = SessionWith(Direction.Up);
        session.AddRepeat();
        var outer = (RepeatCommand)session.Program.Commands[1];

        session.AddRepeat(new BlockSlot(outer.Id, 0));

        Assert.Single(((RepeatCommand)session.Program.Commands[1]).Body);
    }

    [Fact]
    public void CanAddRepeatAt_AtDepthThree_IsFalse_ButMoveFits()
    {
        var session = new GameSession();
        session.AddRepeat();
        var outer = (RepeatCommand)session.Program.Commands[0];
        session.AddRepeat(new BlockSlot(outer.Id, 0));
        var inner = (RepeatCommand)((RepeatCommand)session.Program.Commands[0]).Body[0];

        Assert.False(session.CanAddRepeatAt(new BlockSlot(inner.Id, 0)));
        Assert.True(session.CanAddAt(new BlockSlot(inner.Id, 0)));
        session.AddRepeat(new BlockSlot(inner.Id, 0));
        Assert.Equal(2, session.Program.CountAll());
    }

    [Fact]
    public void CanAddAt_UnknownParentOrIndexOutOfRange_IsFalse()
    {
        var session = SessionWith(Direction.Up);

        Assert.False(session.CanAddAt(new BlockSlot(Guid.NewGuid(), 0)));
        Assert.False(session.CanAddAt(BlockSlot.Root(5)));
    }

    [Fact]
    public void CanAddAt_WhenFull_IsFalse()
    {
        var session = SessionWith([.. Enumerable.Repeat(Direction.Up, ProgramLimits.MaxProgramCommands)]);

        Assert.False(session.CanAddAt(BlockSlot.Root(0)));
        Assert.False(session.CanAddRepeatAt(BlockSlot.Root(0)));
    }

    [Fact]
    public void AddAndCan_WhenLocked_DoNothing()
    {
        var session = Locked();
        var slot = BlockSlot.Root(0);

        Assert.False(session.CanAddAt(slot));
        Assert.False(session.CanAddRepeatAt(slot));
        session.Add(Direction.Down, slot);
        session.AddRepeat(slot);
        Assert.Equal(1, session.Program.CountAll());
    }

    [Fact]
    public void CanMove_ToOtherPosition_IsTrue_ToOwnPosition_IsFalse()
    {
        var session = SessionWith(Direction.Up, Direction.Down, Direction.Left);
        var first = session.Program.Commands[0].Id;

        Assert.True(session.CanMove(first, BlockSlot.Root(3)));
        Assert.False(session.CanMove(first, BlockSlot.Root(0)));
        Assert.False(session.CanMove(first, BlockSlot.Root(1)));
    }

    [Fact]
    public void CanMove_RepeatIntoOwnBody_IsFalse()
    {
        var session = new GameSession();
        session.AddRepeat();
        var repeat = session.Program.Commands[0];

        Assert.False(session.CanMove(repeat.Id, new BlockSlot(repeat.Id, 0)));
    }

    [Fact]
    public void CanMoveAndCanRemove_WhenLocked_AreFalse()
    {
        var session = Locked();
        var id = session.Program.Commands[0].Id;

        Assert.False(session.CanMove(id, BlockSlot.Root(1)));
        Assert.False(session.CanRemove(id));
    }

    [Fact]
    public void CanRemove_KnownIdWhenEditable_IsTrue_UnknownIsFalse()
    {
        var session = SessionWith(Direction.Up);

        Assert.True(session.CanRemove(session.Program.Commands[0].Id));
        Assert.False(session.CanRemove(Guid.NewGuid()));
    }
}
