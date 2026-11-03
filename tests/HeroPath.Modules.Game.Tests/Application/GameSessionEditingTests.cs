using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Game.Tests.Application;

public class GameSessionEditingTests
{
    private static GameSession SessionWith(params Direction[] directions)
    {
        var session = new GameSession();
        TestLevels.Fill(session, directions);
        return session;
    }

    private static RepeatCommand RepeatAt(GameSession session, params int[] path)
    {
        IReadOnlyList<Command> list = session.Program.Commands;
        Command? command = null;
        foreach (var number in path)
        {
            command = list[number - 1];
            list = (command as RepeatCommand)?.Body ?? [];
        }

        return (RepeatCommand)command!;
    }

    [Fact]
    public void AddRepeat_AppendsEmptyRepeatWithCountThree()
    {
        var session = SessionWith(Direction.Up);

        session.AddRepeat();

        var repeat = Assert.IsType<RepeatCommand>(session.Program.Commands[1]);
        Assert.Equal(3, repeat.Count);
        Assert.Empty(repeat.Body);
    }

    [Fact]
    public void InsertTarget_NothingSelected_IsEndOfProgram()
    {
        var session = SessionWith(Direction.Up, Direction.Down);

        Assert.Equal(BlockSlot.Root(2), session.InsertTarget);
        Assert.Null(session.SelectedId);
    }

    [Fact]
    public void InsertTarget_SelectedRepeat_IsEndOfItsBody()
    {
        var session = SessionWith(Direction.Up);
        session.AddRepeat();
        var repeat = RepeatAt(session, 2);
        session.Select(repeat.Id);
        session.Add(Direction.Left);

        Assert.Equal(new BlockSlot(repeat.Id, 1), session.InsertTarget);
        session.Add(Direction.Right);
        Assert.Equal(new BlockSlot(repeat.Id, 2), session.InsertTarget);
        Assert.Equal([Direction.Left, Direction.Right], RepeatAt(session, 2).Body.Cast<MoveCommand>().Select(m => m.Direction));
    }

    [Fact]
    public void InsertTarget_SelectedBlockInsideBody_IsEndOfThatBody()
    {
        var session = new GameSession();
        session.AddRepeat();
        session.Select(RepeatAt(session, 1).Id);
        session.Add(Direction.Left);
        session.Add(Direction.Right);
        session.Select(RepeatAt(session, 1).Body[0].Id);

        Assert.Equal(new BlockSlot(RepeatAt(session, 1).Id, 2), session.InsertTarget);
    }

    [Fact]
    public void InsertTarget_SelectedTopLevelMove_IsEndOfProgram()
    {
        var session = SessionWith(Direction.Up, Direction.Down);
        session.Select(session.Program.Commands[0].Id);

        Assert.Equal(BlockSlot.Root(2), session.InsertTarget);
    }

    [Fact]
    public void Select_SameIdAgain_TogglesOff()
    {
        var session = SessionWith(Direction.Up);
        var id = session.Program.Commands[0].Id;

        session.Select(id);
        Assert.Equal(id, session.SelectedId);
        session.Select(id);

        Assert.Null(session.SelectedId);
    }

    [Fact]
    public void Select_OtherId_ChangesSelection()
    {
        var session = SessionWith(Direction.Up, Direction.Down);

        session.Select(session.Program.Commands[0].Id);
        session.Select(session.Program.Commands[1].Id);

        Assert.Equal(session.Program.Commands[1].Id, session.SelectedId);
    }

    [Fact]
    public void Select_UnknownIdOrNullWithoutSelection_RaisesNoEvent()
    {
        var session = SessionWith(Direction.Up);
        var changes = 0;
        session.Changed += () => changes++;

        session.Select(Guid.NewGuid());
        session.Select(null);

        Assert.Null(session.SelectedId);
        Assert.Equal(0, changes);
    }

    [Fact]
    public void Remove_SelectedBlock_ClearsSelectionAndTarget()
    {
        var session = new GameSession();
        session.AddRepeat();
        var repeat = RepeatAt(session, 1);
        session.Select(repeat.Id);
        session.Add(Direction.Left);

        session.Remove(repeat.Id);

        Assert.Null(session.SelectedId);
        Assert.Equal(BlockSlot.Root(0), session.InsertTarget);
    }

    [Fact]
    public void Remove_ChildOfSelectedRepeat_KeepsSelectionOnRepeat()
    {
        var session = new GameSession();
        session.AddRepeat();
        var repeat = RepeatAt(session, 1);
        session.Select(repeat.Id);
        session.Add(Direction.Left);

        session.Remove(RepeatAt(session, 1).Body[0].Id);

        Assert.Equal(repeat.Id, session.SelectedId);
    }

    [Fact]
    public void Clear_ClearsSelection()
    {
        var session = SessionWith(Direction.Up);
        session.Select(session.Program.Commands[0].Id);

        session.Clear();

        Assert.Null(session.SelectedId);
    }

    [Fact]
    public void MoveUpAndDown_KeepSelection()
    {
        var session = SessionWith(Direction.Up, Direction.Down);
        var id = session.Program.Commands[1].Id;
        session.Select(id);

        session.MoveUp(id);

        Assert.Equal(id, session.SelectedId);
        Assert.Equal(id, session.Program.Commands[0].Id);
        Assert.False(session.CanMoveUp(id));
        Assert.True(session.CanMoveDown(id));
        session.MoveDown(id);
        Assert.Equal(id, session.Program.Commands[1].Id);
    }

    [Fact]
    public void MoveUp_FirstBlock_IsNoOpWithoutEvent()
    {
        var session = SessionWith(Direction.Up, Direction.Down);
        var changes = 0;
        session.Changed += () => changes++;

        session.MoveUp(session.Program.Commands[0].Id);
        session.MoveDown(session.Program.Commands[1].Id);

        Assert.Equal(0, changes);
    }

    [Fact]
    public void SetRepeatCount_ChangesCountAndRaisesChangedOnlyForRealChange()
    {
        var session = new GameSession();
        session.AddRepeat();
        var id = session.Program.Commands[0].Id;
        var changes = 0;
        session.Changed += () => changes++;

        session.SetRepeatCount(id, 5);
        session.SetRepeatCount(id, 5);
        session.SetRepeatCount(id, 0);
        session.SetRepeatCount(id, 21);

        Assert.Equal(5, RepeatAt(session, 1).Count);
        Assert.Equal(1, changes);
        Assert.Equal(id, session.Program.Commands[0].Id);
    }

    [Fact]
    public void Move_ToSlot_MovesBlockIntoRepeat()
    {
        var session = SessionWith(Direction.Up);
        session.AddRepeat();
        var moveId = session.Program.Commands[0].Id;
        var repeatId = session.Program.Commands[1].Id;

        session.Move(moveId, new BlockSlot(repeatId, 0));

        Assert.Single(session.Program.Commands);
        Assert.Single(RepeatAt(session, 1).Body);
    }

    [Fact]
    public void CanAddRepeat_FollowsInsertTargetDepth()
    {
        var session = new GameSession();
        Assert.True(session.CanAddRepeat);
        session.AddRepeat();
        session.Select(RepeatAt(session, 1).Id);
        Assert.True(session.CanAddRepeat);
        session.AddRepeat();
        session.Select(RepeatAt(session, 1, 1).Id);

        Assert.False(session.CanAddRepeat);
        Assert.True(session.CanAdd);
        var changes = 0;
        session.Changed += () => changes++;
        session.AddRepeat();
        Assert.Equal(0, changes);
        session.Add(Direction.Up);
        Assert.Single(RepeatAt(session, 1, 1).Body);
    }

    [Fact]
    public void CanAdd_CountsNestedBlocks()
    {
        var session = new GameSession();
        session.AddRepeat();
        session.Select(RepeatAt(session, 1).Id);
        for (var i = 0; i < ProgramLimits.MaxProgramCommands - 1; i++)
        {
            session.Add(Direction.Up);
        }

        Assert.Equal(ProgramLimits.MaxProgramCommands, session.Program.CountAll());
        Assert.False(session.CanAdd);
        Assert.False(session.CanAddRepeat);
        session.Add(Direction.Down);
        Assert.Equal(ProgramLimits.MaxProgramCommands, session.Program.CountAll());
    }

    [Fact]
    public void NotEditable_IgnoresAllTreeEditsAndSelection()
    {
        var session = new GameSession();
        session.Load(TestLevels.Demo);
        session.AddRepeat();
        var repeat = RepeatAt(session, 1);
        session.Select(repeat.Id);
        session.Add(Direction.Up);
        session.Add(Direction.Up);
        var runner = new ExecutionRunner(session, TimeProvider.System);
        var program = session.Program;
        runner.StepOnce();
        var changes = 0;
        session.Changed += () => changes++;
        var moveId = RepeatAt(session, 1).Body[1].Id;

        session.AddRepeat();
        session.Add(Direction.Left);
        session.MoveUp(moveId);
        session.MoveDown(RepeatAt(session, 1).Body[0].Id);
        session.Move(moveId, BlockSlot.Root(0));
        session.SetRepeatCount(repeat.Id, 9);
        session.Remove(moveId);
        session.Select(moveId);
        session.Clear();

        Assert.Same(program, session.Program);
        Assert.Null(session.SelectedId);
        Assert.False(session.CanMoveUp(moveId));
        Assert.Equal(0, changes);
    }

    [Fact]
    public void StartingRun_ClearsSelection()
    {
        var session = new GameSession();
        session.Load(TestLevels.Demo);
        TestLevels.Fill(session, Direction.Up);
        session.Select(session.Program.Commands[0].Id);

        new ExecutionRunner(session, TimeProvider.System).StepOnce();

        Assert.Null(session.SelectedId);
    }

    [Fact]
    public void ToProgram_IsTheCurrentProgramWithRepeats()
    {
        var session = new GameSession();
        session.AddRepeat();

        Assert.Same(session.Program, session.ToProgram());
        Assert.IsType<RepeatCommand>(session.ToProgram().Commands[0]);
    }

    [Fact]
    public void Obstacle_InsideRepeat_MarksObstacleCell()
    {
        var session = new GameSession();
        session.Load(TestLevels.Demo);
        session.AddRepeat();
        session.Select(RepeatAt(session, 1).Id);
        session.Add(Direction.Right);
        var runner = new ExecutionRunner(session, TimeProvider.System);

        runner.StepOnce();
        runner.StepOnce();

        Assert.Equal(RunStatus.Failed, session.Status);
        Assert.Equal(new Position(3, 6), session.MarkedCell);
    }

    [Fact]
    public void Changed_FiresOncePerRealTreeChange()
    {
        var session = new GameSession();
        var changes = 0;
        session.Changed += () => changes++;

        session.AddRepeat();
        session.Select(RepeatAt(session, 1).Id);
        session.Add(Direction.Up);
        session.SetRepeatCount(RepeatAt(session, 1).Id, 4);
        session.MoveUp(RepeatAt(session, 1).Id);

        Assert.Equal(4, changes);
    }
}
