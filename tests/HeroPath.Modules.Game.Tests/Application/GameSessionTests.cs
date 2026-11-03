using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Game.Tests.Application;

public class GameSessionTests
{
    private static LevelDefinition CreateLevel() => TestLevels.Empty("test-plain", 10, 10, new Position(2, 3), new Position(7, 7));

    private static LevelDefinition DemoLevel() => TestLevels.Demo;

    private static (GameSession Session, ExecutionRunner Runner) Started(params Direction[] directions)
    {
        var session = new GameSession();
        session.Load(DemoLevel());
        TestLevels.Fill(session, directions);
        return (session, new ExecutionRunner(session, TestLevels.NewProgress(), TimeProvider.System));
    }

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

        Assert.Equal([Direction.Up, Direction.Right, Direction.Down], TestLevels.Directions(session));
    }

    [Fact]
    public void Remove_KnownId_RemovesOnlyThatBlock()
    {
        var session = SessionWith(Direction.Up, Direction.Right, Direction.Down);
        var id = session.Program.Commands[1].Id;

        session.Remove(id);

        Assert.Equal([Direction.Up, Direction.Down], TestLevels.Directions(session));
    }

    [Fact]
    public void Remove_UnknownId_DoesNothingAndRaisesNoEvent()
    {
        var session = SessionWith(Direction.Up);
        var changes = 0;
        session.Changed += () => changes++;

        session.Remove(Guid.NewGuid());

        Assert.Single(session.Program.Commands);
        Assert.Equal(0, changes);
    }

    [Fact]
    public void Clear_RemovesAllBlocks()
    {
        var session = SessionWith(Direction.Up, Direction.Left);

        session.Clear();

        Assert.Empty(session.Program.Commands);
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
        Assert.Equal(ProgramLimits.MaxProgramCommands, session.Program.Commands.Count);
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

        session.Remove(session.Program.Commands[0].Id);

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
        session.Remove(session.Program.Commands[0].Id);
        session.Clear();
        session.Load(CreateLevel());

        Assert.Equal(5, changes);
    }

    [Fact]
    public void NotEditable_IgnoresAddRemoveAndClear()
    {
        var (session, runner) = Started(Direction.Up, Direction.Down);
        runner.StepOnce();
        var id = session.Program.Commands[0].Id;
        var changes = 0;
        session.Changed += () => changes++;

        session.Add(Direction.Left);
        session.Remove(id);
        session.Clear();

        Assert.False(session.IsEditable);
        Assert.Equal(2, session.Program.Commands.Count);
        Assert.Equal(0, changes);
        runner.Reset();
        Assert.True(session.IsEditable);
    }

    [Fact]
    public void NewSession_IsReadyWithoutStepAndHeroOnStart()
    {
        var session = new GameSession();
        session.Load(DemoLevel());

        Assert.Equal(RunStatus.Ready, session.Status);
        Assert.Null(session.CurrentStep);
        Assert.Null(session.MarkedCell);
        Assert.True(session.IsEditable);
        Assert.Equal(new Position(1, 6), session.HeroPosition);
    }

    [Fact]
    public void Step_MovesHeroAndRaisesChanged()
    {
        var (session, runner) = Started(Direction.Up);
        var changes = 0;
        session.Changed += () => changes++;

        runner.StepOnce();

        Assert.Equal(new Position(1, 5), session.HeroPosition);
        Assert.Equal(session.Program.Commands[0].Id, session.CurrentStep!.CommandId);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void Obstacle_MarksObstacleCellInAttemptedDirection()
    {
        var (session, runner) = Started(Direction.Right, Direction.Right);

        runner.StepOnce();
        runner.StepOnce();

        Assert.Equal(RunStatus.Failed, session.Status);
        Assert.Equal(new Position(2, 6), session.HeroPosition);
        Assert.Equal(new Position(3, 6), session.MarkedCell);
    }

    [Fact]
    public void OutOfBounds_MarksHeroCell()
    {
        var (session, runner) = Started(Direction.Left, Direction.Left);

        runner.StepOnce();
        runner.StepOnce();

        Assert.Equal(new Position(0, 6), session.HeroPosition);
        Assert.Equal(new Position(0, 6), session.MarkedCell);
    }

    [Fact]
    public void Reset_AfterRun_ClearsStepAndMark()
    {
        var (session, runner) = Started(Direction.Right, Direction.Right);
        runner.StepOnce();
        runner.StepOnce();
        var changes = 0;
        session.Changed += () => changes++;

        runner.Reset();

        Assert.Equal(RunStatus.Ready, session.Status);
        Assert.Null(session.CurrentStep);
        Assert.Null(session.MarkedCell);
        Assert.Equal(new Position(1, 6), session.HeroPosition);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void Reset_WhenReady_RaisesNoEvent()
    {
        var (session, runner) = Started();
        var changes = 0;
        session.Changed += () => changes++;

        runner.Reset();

        Assert.Equal(0, changes);
    }

    [Fact]
    public void ToProgram_HasSameCommandsInOrder()
    {
        var session = SessionWith(Direction.Up, Direction.Left, Direction.Up);

        var program = session.ToProgram();

        Assert.Equal(session.Program.Commands.Select(c => c.Id), program.Commands.Select(c => c.Id));
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
    public void Load_SetsLevelGridAndHeroPositionToStart()
    {
        var session = new GameSession();
        var level = CreateLevel();

        session.Load(level);

        Assert.Same(level, session.Level);
        Assert.Equal(level.ToGrid(), session.Grid);
        Assert.Equal(new Position(2, 3), session.HeroPosition);
    }

    [Fact]
    public void Load_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new GameSession().Load(null!));

    [Fact]
    public void Load_SameLevelTwice_KeepsProgramAndRunAndRaisesNoEvent()
    {
        var (session, runner) = Started(Direction.Up, Direction.Up);
        runner.StepOnce();
        var step = session.CurrentStep;
        var changes = 0;
        session.Changed += () => changes++;

        session.Load(session.Level!);

        Assert.Equal(2, session.Program.Commands.Count);
        Assert.Equal(RunStatus.Paused, session.Status);
        Assert.Same(step, session.CurrentStep);
        Assert.False(session.IsEditable);
        Assert.Equal(0, changes);
    }

    [Fact]
    public void Load_EqualLevelBuiltAgain_KeepsGridProgramAndRun()
    {
        var (session, runner) = Started(Direction.Up, Direction.Up);
        runner.StepOnce();
        var grid = session.Grid;

        session.Load(TestLevels.Demo with { });

        Assert.Same(grid, session.Grid);
        Assert.Equal(RunStatus.Paused, session.Status);
        Assert.Equal(new Position(1, 5), session.HeroPosition);
        Assert.Equal(2, session.Program.Commands.Count);
    }

    [Fact]
    public void Load_SameContentInNewLists_KeepsGridAndProgram()
    {
        var (session, _) = Started(Direction.Up);
        var grid = session.Grid;

        session.Load(TestLevels.Demo with { Obstacles = [.. TestLevels.Demo.Obstacles], Items = [] });

        Assert.Same(grid, session.Grid);
        Assert.Single(session.Program.Commands);
    }

    [Fact]
    public void Load_SameIdAndGridWithANewName_KeepsProgramAndRunButTakesTheNewLevel()
    {
        var (session, runner) = Started(Direction.Up, Direction.Up);
        runner.StepOnce();
        var grid = session.Grid;
        var renamed = TestLevels.Demo with { Name = "Neuer Name" };

        session.Load(renamed);

        Assert.Same(renamed, session.Level);
        Assert.Same(grid, session.Grid);
        Assert.Equal(2, session.Program.Commands.Count);
        Assert.Equal(RunStatus.Paused, session.Status);
    }

    [Fact]
    public void Load_EditedLevelWithAnObstacleMoved_ClearsTheProgramAndTakesTheNewGrid()
    {
        var (session, _) = Started(Direction.Up, Direction.Up);
        var edited = TestLevels.Demo with { Obstacles = [] };

        session.Load(edited);

        Assert.Empty(session.Program.Commands);
        Assert.Equal(RunStatus.Ready, session.Status);
        Assert.Empty(session.Grid!.Obstacles);
    }

    [Fact]
    public void Load_SameIdButDifferentGrid_IsADifferentLevel()
    {
        var (session, _) = Started(Direction.Up);

        session.Load(TestLevels.Demo with { Start = new Position(0, 0) });

        Assert.Empty(session.Program.Commands);
        Assert.Equal(new Position(0, 0), session.HeroPosition);
    }

    [Fact]
    public void Load_DifferentLevel_ResetsRunAndClearsProgramSelectionAndHint()
    {
        var (session, runner) = Started(Direction.Right, Direction.Right);
        runner.StepOnce();
        runner.StepOnce();
        Assert.Equal(RunStatus.Failed, session.Status);
        Assert.NotNull(session.MarkedCell);
        runner.Reset();
        session.Select(session.Program.Commands[0].Id);
        session.AddRepeat();
        runner.StepOnce();
        Assert.NotNull(session.HintedId);
        var other = CreateLevel();
        var changes = 0;
        session.Changed += () => changes++;

        session.Load(other);

        Assert.Same(other, session.Level);
        Assert.Equal(RunStatus.Ready, session.Status);
        Assert.Null(session.CurrentStep);
        Assert.Null(session.MarkedCell);
        Assert.Null(session.HintedId);
        Assert.Null(session.SelectedId);
        Assert.True(session.IsEditable);
        Assert.Equal(new Position(2, 3), session.HeroPosition);
        Assert.Empty(session.Program.Commands);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void Load_DifferentLevelAfterFailedRun_ClearsRunMessage()
    {
        var (session, runner) = Started(Direction.Right, Direction.Right);
        runner.StepOnce();
        runner.StepOnce();
        Assert.Equal(GameMessageKind.Error, session.Message!.Kind);

        session.Load(CreateLevel());

        Assert.Equal(GameMessageCode.EmptyProgram, session.Message!.Code);
    }

    [Fact]
    public void Load_DifferentLevel_RunnerStartsFreshInterpreterOnNewGrid()
    {
        var (session, runner) = Started(Direction.Up, Direction.Up);
        runner.StepOnce();

        session.Load(CreateLevel());
        TestLevels.Fill(session, Direction.Up);
        runner.StepOnce();

        Assert.Equal(1, session.CurrentStep!.StepNumber);
        Assert.Equal(new Position(2, 2), session.HeroPosition);
    }

    [Fact]
    public void Tempo_DefaultsToMedium_AndRaisesChangedOnlyWhenDifferent()
    {
        var session = new GameSession();
        var changes = 0;
        session.Changed += () => changes++;
        Assert.Equal(Tempo.Medium, session.Tempo);

        session.Tempo = Tempo.Medium;
        session.Tempo = Tempo.Slow;

        Assert.Equal(Tempo.Slow, session.Tempo);
        Assert.Equal(1, changes);
    }
}
