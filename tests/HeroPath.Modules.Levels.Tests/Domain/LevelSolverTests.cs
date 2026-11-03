using System.Diagnostics;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Tests.Domain;

public class LevelSolverTests
{
    [Fact]
    public void ShortestSolution_FreeField_IsManhattanDistance()
    {
        var grid = Level(10, 10, new(0, 9), new(9, 0)).ToGrid();

        Assert.Equal(18, LevelSolver.ShortestSolution(grid));
        Assert.True(LevelSolver.CanReachGoal(grid));
    }

    [Fact]
    public void ShortestSolution_GoalWalledIn_IsNull()
    {
        var grid = (Level(5, 5, new(0, 4), new(4, 0)) with
        {
            Obstacles = [new(3, 0, ObstacleKind.Rock), new(4, 1, ObstacleKind.Rock)],
        }).ToGrid();

        Assert.Null(LevelSolver.ShortestSolution(grid));
        Assert.False(LevelSolver.CanReachGoal(grid));
    }

    [Fact]
    public void ShortestSolution_WalledStrawberry_NeededOnlyWithRequireAllItems()
    {
        var level = WalledStrawberry();

        Assert.Equal(8, LevelSolver.ShortestSolution((level with { RequireAllItems = false }).ToGrid()));
        var required = (level with { RequireAllItems = true }).ToGrid();
        Assert.Null(LevelSolver.ShortestSolution(required));
        Assert.True(LevelSolver.CanReachGoal(required));
    }

    [Fact]
    public void ShortestSolution_PassingGoalBeforeLastStrawberry_Continues()
    {
        var level = Level(5, 3, new(0, 0), new(2, 0)) with
        {
            Items = [new(4, 0, ItemKind.Strawberry)],
            RequireAllItems = true,
        };

        Assert.Equal(6, LevelSolver.ShortestSolution(level.ToGrid()));
    }

    [Fact]
    public void ShortestSolution_PizzaNeverCounts()
    {
        var level = Level(5, 5, new(0, 4), new(4, 0)) with
        {
            Items = [new(0, 0, ItemKind.Pizza), new(2, 2, ItemKind.Strawberry)],
            Obstacles = [new(1, 0, ObstacleKind.Tree), new(0, 1, ObstacleKind.Tree)],
            RequireAllItems = true,
        };

        Assert.Equal(8, LevelSolver.ShortestSolution(level.ToGrid()));
    }

    [Fact]
    public void ShortestSolution_PizzaOnly_DoesNotChangeDistance()
    {
        var level = Level(5, 5, new(0, 4), new(4, 0)) with { Items = [new(2, 2, ItemKind.Pizza)] };

        Assert.Equal(8, LevelSolver.ShortestSolution(level.ToGrid()));
    }

    [Fact]
    public void ShortestSolution_WorstCase20x20WithTenStrawberries_FinishesQuickly()
    {
        var strawberries = Enumerable.Range(0, LevelLimits.MaxStrawberries)
            .Select(i => new LevelItem(1 + i * 2, 1 + i * 7 % 18, ItemKind.Strawberry))
            .ToList();
        var grid = (Level(20, 20, new(0, 19), new(19, 0)) with { Items = strawberries, RequireAllItems = true }).ToGrid();

        var stopwatch = Stopwatch.StartNew();
        var steps = LevelSolver.ShortestSolution(grid);
        stopwatch.Stop();
        Xunit.TestContext.Current.SendDiagnosticMessage($"Solver 20x20, 10 strawberries: {stopwatch.ElapsedMilliseconds} ms, {steps} steps");

        Assert.NotNull(steps);
        Assert.True(stopwatch.ElapsedMilliseconds < 500, $"Took {stopwatch.ElapsedMilliseconds} ms.");
    }

    [Fact]
    public void ShortestSolution_NullGrid_Throws() =>
        Assert.Throws<ArgumentNullException>(() => LevelSolver.ShortestSolution(null!));

    private static LevelDefinition Level(int width, int height, Position start, Position goal) => new()
    {
        Id = "solver",
        Width = width,
        Height = height,
        Start = start,
        Goal = goal,
    };

    private static LevelDefinition WalledStrawberry() => Level(5, 5, new(0, 4), new(4, 0)) with
    {
        Items = [new(2, 2, ItemKind.Strawberry)],
        Obstacles =
        [
            new(1, 2, ObstacleKind.Rock),
            new(3, 2, ObstacleKind.Rock),
            new(2, 1, ObstacleKind.Rock),
            new(2, 3, ObstacleKind.Rock),
        ],
    };
}
