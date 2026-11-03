using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Levels.Domain;
using Microsoft.Extensions.Time.Testing;

namespace HeroPath.Modules.Game.Tests.Application;

public class GameSessionItemsTests
{
    private static readonly LevelDefinition _level = TestLevels.WithItems(
        "items",
        6,
        3,
        new Position(0, 1),
        new Position(5, 1),
        new LevelItem(1, 1, ItemKind.Strawberry),
        new LevelItem(2, 1, ItemKind.Pizza),
        new LevelItem(3, 1, ItemKind.Strawberry),
        new LevelItem(2, 0, ItemKind.Pizza));

    private readonly GameSession _session = new();
    private readonly ExecutionRunner _runner;

    public GameSessionItemsTests()
    {
        _session.Load(_level);
        _runner = new ExecutionRunner(_session, TestLevels.NewProgress(), new FakeTimeProvider());
    }

    private void Steps(int count)
    {
        for (var i = 0; i < count; i++)
        {
            _runner.StepOnce();
        }
    }

    [Fact]
    public void CollectedItems_BeforeAnyStep_IsEmpty()
    {
        Assert.Empty(_session.CollectedItems);
        Assert.Equal(0, _session.CollectedCount(ItemKind.Strawberry));
    }

    [Fact]
    public void CollectedItems_AfterEnteringItemCells_ContainsThoseCellsInOrderOfWalking()
    {
        TestLevels.Fill(_session, Direction.Right, Direction.Right, Direction.Right);

        Steps(2);

        Assert.Equal([new Position(1, 1), new Position(2, 1)], _session.CollectedItems.OrderBy(p => p.X));
        Assert.Equal(1, _session.CollectedCount(ItemKind.Strawberry));
        Assert.Equal(1, _session.CollectedCount(ItemKind.Pizza));
    }

    [Fact]
    public void CollectedItems_WalkingOverACellAgain_CountsItOnce()
    {
        TestLevels.Fill(_session, Direction.Right, Direction.Left, Direction.Right);

        Steps(3);

        Assert.Single(_session.CollectedItems);
        Assert.Equal(1, _session.CollectedCount(ItemKind.Strawberry));
    }

    [Fact]
    public void CollectedItems_Reset_ClearsThem()
    {
        TestLevels.Fill(_session, Direction.Right);
        Steps(1);

        _runner.Reset();

        Assert.Empty(_session.CollectedItems);
        Assert.Equal(0, _session.CollectedCount(ItemKind.Strawberry));
    }

    [Fact]
    public void CollectedItems_LoadingADifferentLevel_ClearsThem()
    {
        TestLevels.Fill(_session, Direction.Right);
        Steps(1);

        _session.Load(TestLevels.Demo);

        Assert.Empty(_session.CollectedItems);
    }

    [Fact]
    public void CollectedItems_LoadingTheSameLevelAgain_KeepsThem()
    {
        TestLevels.Fill(_session, Direction.Right);
        Steps(1);

        _session.Load(_level);

        Assert.Single(_session.CollectedItems);
    }

    [Fact]
    public void CollectedCount_WithoutLevel_IsZero() =>
        Assert.Equal(0, new GameSession().CollectedCount(ItemKind.Pizza));

    [Fact]
    public void Message_SucceededWithPizzas_CarriesThePizzaCount()
    {
        TestLevels.Fill(_session, Direction.Right, Direction.Right, Direction.Right, Direction.Right, Direction.Right);

        Steps(5);

        Assert.Equal(GameMessageCode.Succeeded, _session.Message!.Code);
        Assert.Equal(1, _session.Message.PizzaCount);
    }

    [Fact]
    public void Message_SucceededWithoutPizza_HasPizzaCountZero()
    {
        var level = TestLevels.WithItems("items-2", 4, 3, new Position(0, 1), new Position(3, 1), new LevelItem(1, 1, ItemKind.Strawberry));
        _session.Load(level);
        TestLevels.Fill(_session, Direction.Right, Direction.Right, Direction.Right);

        Steps(3);

        Assert.Equal(GameMessageCode.Succeeded, _session.Message!.Code);
        Assert.Equal(0, _session.Message.PizzaCount);
    }

    [Fact]
    public void Message_TwoPizzasThenGoal_CountsBoth()
    {
        TestLevels.Fill(_session, Direction.Right, Direction.Right, Direction.Up, Direction.Down, Direction.Right, Direction.Right, Direction.Right);

        for (var i = 0; i < 20 && _session.Status is RunStatus.Ready or RunStatus.Paused; i++)
        {
            _runner.StepOnce();
        }

        Assert.Equal(GameMessageCode.Succeeded, _session.Message!.Code);
        Assert.Equal(2, _session.Message.PizzaCount);
    }

    [Fact]
    public void Message_FailedRun_HasPizzaCountZero()
    {
        TestLevels.Fill(_session, Direction.Right, Direction.Right);

        Steps(2);

        Assert.Equal(0, _session.Message?.PizzaCount ?? 0);
    }
}
