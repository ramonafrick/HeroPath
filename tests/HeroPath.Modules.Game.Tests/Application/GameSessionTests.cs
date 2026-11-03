using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Game.Tests.Application;

public class GameSessionTests
{
    private static Grid CreateGrid() => new(10, 10, new Position(2, 3), new Position(7, 7), [], [], false);

    private static GameSession SessionWith(params Direction[] directions)
    {
        var session = new GameSession();
        foreach (var direction in directions)
        {
            session.Add(direction);
        }

        return session;
    }

    [Fact]
    public void Add_AppendsInOrder()
    {
        var session = SessionWith(Direction.Up, Direction.Right, Direction.Down);

        Assert.Equal([Direction.Up, Direction.Right, Direction.Down], session.Commands.Select(c => c.Direction));
    }

    [Fact]
    public void Remove_KnownId_RemovesOnlyThatBlock()
    {
        var session = SessionWith(Direction.Up, Direction.Right, Direction.Down);
        var id = session.Commands[1].Id;

        session.Remove(id);

        Assert.Equal([Direction.Up, Direction.Down], session.Commands.Select(c => c.Direction));
    }

    [Fact]
    public void Remove_UnknownId_DoesNothingAndRaisesNoEvent()
    {
        var session = SessionWith(Direction.Up);
        var changes = 0;
        session.Changed += () => changes++;

        session.Remove(Guid.NewGuid());

        Assert.Single(session.Commands);
        Assert.Equal(0, changes);
    }

    [Fact]
    public void Clear_RemovesAllBlocks()
    {
        var session = SessionWith(Direction.Up, Direction.Left);

        session.Clear();

        Assert.Empty(session.Commands);
    }

    [Fact]
    public void Clear_EmptyProgram_RaisesNoEvent()
    {
        var session = new GameSession();
        var changes = 0;
        session.Changed += () => changes++;

        session.Clear();

        Assert.Equal(0, changes);
    }

    [Fact]
    public void Add_AtLimit_CanAddIsFalseAndAddIsNoOp()
    {
        var session = new GameSession();
        for (var i = 0; i < ProgramLimits.MaxProgramCommands; i++)
        {
            session.Add(Direction.Right);
        }

        Assert.False(session.CanAdd);
        var changes = 0;
        session.Changed += () => changes++;

        var exception = Record.Exception(() => session.Add(Direction.Up));

        Assert.Null(exception);
        Assert.Equal(ProgramLimits.MaxProgramCommands, session.Commands.Count);
        Assert.Equal(0, changes);
    }

    [Fact]
    public void Remove_AtLimit_CanAddIsTrueAgain()
    {
        var session = new GameSession();
        for (var i = 0; i < ProgramLimits.MaxProgramCommands; i++)
        {
            session.Add(Direction.Right);
        }

        session.Remove(session.Commands[0].Id);

        Assert.True(session.CanAdd);
    }

    [Fact]
    public void Changed_FiresOncePerRealChange()
    {
        var session = new GameSession();
        var changes = 0;
        session.Changed += () => changes++;

        session.Add(Direction.Up);
        session.Add(Direction.Down);
        session.Remove(session.Commands[0].Id);
        session.Clear();
        session.Load(CreateGrid());
        session.SetEditable(false);

        Assert.Equal(6, changes);
    }

    [Fact]
    public void NotEditable_IgnoresAddRemoveAndClear()
    {
        var session = SessionWith(Direction.Up, Direction.Down);
        var id = session.Commands[0].Id;
        session.SetEditable(false);
        var changes = 0;
        session.Changed += () => changes++;

        session.Add(Direction.Left);
        session.Remove(id);
        session.Clear();

        Assert.Equal(2, session.Commands.Count);
        Assert.Equal(0, changes);
    }

    [Fact]
    public void SetEditable_True_AllowsChangesAgain()
    {
        var session = new GameSession();
        session.SetEditable(false);
        session.SetEditable(true);

        session.Add(Direction.Up);

        Assert.Single(session.Commands);
    }

    [Fact]
    public void ToProgram_HasSameCommandsInOrder()
    {
        var session = SessionWith(Direction.Up, Direction.Left, Direction.Up);

        var program = session.ToProgram();

        Assert.Equal(session.Commands.Select(c => c.Id), program.Commands.Select(c => c.Id));
    }

    [Fact]
    public void ToProgram_IsSnapshot()
    {
        var session = SessionWith(Direction.Up);
        var program = session.ToProgram();

        session.Add(Direction.Down);

        Assert.Single(program.Commands);
    }

    [Fact]
    public void Load_SetsGridAndHeroPositionToStart()
    {
        var session = new GameSession();
        var grid = CreateGrid();

        session.Load(grid);

        Assert.Same(grid, session.Grid);
        Assert.Equal(new Position(2, 3), session.HeroPosition);
    }

    [Fact]
    public void Load_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new GameSession().Load(null!));
}
