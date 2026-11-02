using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Tests.Application;

public class LevelValidatorTests
{
    private static LevelDefinition Valid() => new()
    {
        Id = "test",
        Width = 5,
        Height = 5,
        Start = new Position(0, 4),
        Goal = new Position(4, 0),
        Obstacles = [new LevelObstacle(2, 2, ObstacleKind.Tree)],
        Items = [new LevelItem(1, 1, ItemKind.Strawberry)],
    };

    private static void AssertOnly(LevelDefinition level, LevelValidationErrorKind kind, Position? position = null)
    {
        var error = Assert.Single(LevelValidator.Validate(level));
        Assert.Equal(kind, error.Kind);
        Assert.Equal(position, error.Position);
    }

    [Fact]
    public void Validate_ValidLevel_ReturnsNoErrors() =>
        Assert.Empty(LevelValidator.Validate(Valid()));

    [Fact]
    public void Validate_DefaultLevelWithDistinctGoal_ReturnsNoErrors() =>
        Assert.Empty(LevelValidator.Validate(new LevelDefinition { Goal = new Position(9, 9) }));

    [Fact]
    public void Validate_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => LevelValidator.Validate(null!));

    [Fact]
    public void Validate_SchemaVersion2_ReportsUnsupportedVersion() =>
        AssertOnly(Valid() with { SchemaVersion = 2 }, LevelValidationErrorKind.UnsupportedSchemaVersion);

    [Theory]
    [InlineData(3)]
    [InlineData(20)]
    public void Validate_WidthAtLimit_IsValid(int width) =>
        Assert.Empty(LevelValidator.Validate(Valid() with { Width = width, Goal = new Position(2, 0) }));

    [Theory]
    [InlineData(2)]
    [InlineData(21)]
    public void Validate_WidthOutOfRange_ReportsError(int width) =>
        AssertOnly(Valid() with { Width = width, Start = new Position(0, 0), Goal = new Position(1, 0), Obstacles = [], Items = [] }, LevelValidationErrorKind.WidthOutOfRange);

    [Theory]
    [InlineData(3)]
    [InlineData(20)]
    public void Validate_HeightAtLimit_IsValid(int height) =>
        Assert.Empty(LevelValidator.Validate(Valid() with { Height = height, Start = new Position(0, 0) }));

    [Theory]
    [InlineData(2)]
    [InlineData(21)]
    public void Validate_HeightOutOfRange_ReportsError(int height) =>
        AssertOnly(
            Valid() with { Height = height, Start = new Position(0, 0), Obstacles = [], Items = [] },
            LevelValidationErrorKind.HeightOutOfRange);

    [Fact]
    public void Validate_StartOnLastCell_IsValid() =>
        Assert.Empty(LevelValidator.Validate(Valid() with { Start = new Position(4, 4) }));

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(5, 0)]
    [InlineData(0, 5)]
    public void Validate_StartOutsideGrid_ReportsError(int x, int y) =>
        AssertOnly(Valid() with { Start = new Position(x, y) }, LevelValidationErrorKind.StartOutsideGrid, new Position(x, y));

    [Fact]
    public void Validate_GoalOutsideGrid_ReportsError() =>
        AssertOnly(Valid() with { Goal = new Position(0, -1) }, LevelValidationErrorKind.GoalOutsideGrid, new Position(0, -1));

    [Fact]
    public void Validate_StartEqualsGoal_ReportsError() =>
        AssertOnly(Valid() with { Goal = new Position(0, 4) }, LevelValidationErrorKind.StartEqualsGoal, new Position(0, 4));

    [Fact]
    public void Validate_NullObstacles_ReportsErrorWithoutThrowing() =>
        AssertOnly(Valid() with { Obstacles = null! }, LevelValidationErrorKind.ObstaclesMissing);

    [Fact]
    public void Validate_NullObstacleEntry_ReportsErrorWithoutThrowing() =>
        AssertOnly(Valid() with { Obstacles = [null!] }, LevelValidationErrorKind.ObstaclesMissing);

    [Fact]
    public void Validate_NullItems_ReportsErrorWithoutThrowing() =>
        AssertOnly(Valid() with { Items = null! }, LevelValidationErrorKind.ItemsMissing);

    [Fact]
    public void Validate_NullItemEntry_ReportsErrorWithoutThrowing() =>
        AssertOnly(Valid() with { Items = [null!] }, LevelValidationErrorKind.ItemsMissing);

    [Fact]
    public void Validate_ObstacleOutsideGrid_ReportsError() =>
        AssertOnly(
            Valid() with { Obstacles = [new LevelObstacle(5, 2, ObstacleKind.Rock)] },
            LevelValidationErrorKind.ObstacleOutsideGrid,
            new Position(5, 2));

    [Fact]
    public void Validate_ObstacleOnLastCell_IsValid() =>
        Assert.Empty(LevelValidator.Validate(Valid() with { Obstacles = [new LevelObstacle(4, 4, ObstacleKind.Rock)] }));

    [Fact]
    public void Validate_ItemOutsideGrid_ReportsError() =>
        AssertOnly(
            Valid() with { Items = [new LevelItem(-1, 0, ItemKind.Pizza)] },
            LevelValidationErrorKind.ItemOutsideGrid,
            new Position(-1, 0));

    [Fact]
    public void Validate_ObstacleOnStart_ReportsError() =>
        AssertOnly(
            Valid() with { Obstacles = [new LevelObstacle(0, 4, ObstacleKind.Pond)] },
            LevelValidationErrorKind.ObstacleOnStart,
            new Position(0, 4));

    [Fact]
    public void Validate_ObstacleOnGoal_ReportsError() =>
        AssertOnly(
            Valid() with { Obstacles = [new LevelObstacle(4, 0, ObstacleKind.Tree)] },
            LevelValidationErrorKind.ObstacleOnGoal,
            new Position(4, 0));

    [Fact]
    public void Validate_ItemOnStart_ReportsError() =>
        AssertOnly(
            Valid() with { Items = [new LevelItem(0, 4, ItemKind.Strawberry)] },
            LevelValidationErrorKind.ItemOnStart,
            new Position(0, 4));

    [Fact]
    public void Validate_ItemOnGoal_IsValid() =>
        Assert.Empty(LevelValidator.Validate(Valid() with { Items = [new LevelItem(4, 0, ItemKind.Strawberry)] }));

    [Fact]
    public void Validate_TwoObstaclesOnOneCell_ReportsCellOccupiedTwice() =>
        AssertOnly(
            Valid() with { Obstacles = [new LevelObstacle(2, 2, ObstacleKind.Tree), new LevelObstacle(2, 2, ObstacleKind.Rock)] },
            LevelValidationErrorKind.CellOccupiedTwice,
            new Position(2, 2));

    [Fact]
    public void Validate_TwoItemsOnOneCell_ReportsCellOccupiedTwice() =>
        AssertOnly(
            Valid() with { Items = [new LevelItem(1, 1, ItemKind.Strawberry), new LevelItem(1, 1, ItemKind.Pizza)] },
            LevelValidationErrorKind.CellOccupiedTwice,
            new Position(1, 1));

    [Fact]
    public void Validate_ObstacleAndItemOnOneCell_ReportsCellOccupiedTwice() =>
        AssertOnly(
            Valid() with { Items = [new LevelItem(2, 2, ItemKind.Strawberry)] },
            LevelValidationErrorKind.CellOccupiedTwice,
            new Position(2, 2));

    [Fact]
    public void Validate_RequireAllItemsWithStrawberry_IsValid() =>
        Assert.Empty(LevelValidator.Validate(Valid() with { RequireAllItems = true }));

    [Fact]
    public void Validate_RequireAllItemsWithoutItems_ReportsError() =>
        AssertOnly(Valid() with { RequireAllItems = true, Items = [] }, LevelValidationErrorKind.RequiredItemsMissing);

    [Fact]
    public void Validate_RequireAllItemsWithOnlyPizza_ReportsError() =>
        AssertOnly(
            Valid() with { RequireAllItems = true, Items = [new LevelItem(1, 1, ItemKind.Pizza)] },
            LevelValidationErrorKind.RequiredItemsMissing);

    [Fact]
    public void Validate_MultipleProblems_CollectsAllErrors()
    {
        var level = Valid() with
        {
            Width = 99,
            Goal = new Position(0, 4),
            Obstacles = [new LevelObstacle(0, 4, ObstacleKind.Tree)],
            RequireAllItems = true,
            Items = [],
        };

        var kinds = LevelValidator.Validate(level).Select(e => e.Kind).ToList();

        Assert.Contains(LevelValidationErrorKind.WidthOutOfRange, kinds);
        Assert.Contains(LevelValidationErrorKind.StartEqualsGoal, kinds);
        Assert.Contains(LevelValidationErrorKind.ObstacleOnStart, kinds);
        Assert.Contains(LevelValidationErrorKind.RequiredItemsMissing, kinds);
    }

    [Fact]
    public void Validate_NoErrors_ToGridDoesNotThrow()
    {
        var random = new Random(12345);
        var validCount = 0;

        for (var i = 0; i < 20000; i++)
        {
            var level = RandomLevel(random);
            if (LevelValidator.Validate(level).Count == 0)
            {
                validCount++;
                Assert.Null(Record.Exception(level.ToGrid));
            }
        }

        Assert.True(validCount > 100, "The generator should produce a meaningful number of valid levels.");
    }

    [Fact]
    public void Validate_WithErrors_ToGridThrows()
    {
        var random = new Random(54321);

        for (var i = 0; i < 2000; i++)
        {
            var level = RandomLevel(random);
            if (LevelValidator.Validate(level).Count > 0)
            {
                Assert.ThrowsAny<ArgumentException>(level.ToGrid);
            }
        }
    }

    private static LevelDefinition RandomLevel(Random random)
    {
        int Coord() => random.Next(-1, 7);
        var obstacles = Enumerable.Range(0, random.Next(0, 4))
            .Select(_ => new LevelObstacle(Coord(), Coord(), (ObstacleKind)random.Next(3)))
            .ToList();
        var items = Enumerable.Range(0, random.Next(0, 4))
            .Select(_ => new LevelItem(Coord(), Coord(), (ItemKind)random.Next(2)))
            .ToList();

        return new LevelDefinition
        {
            Id = "random",
            Width = random.Next(2, 7),
            Height = random.Next(2, 7),
            Start = new Position(Coord(), Coord()),
            Goal = new Position(Coord(), Coord()),
            RequireAllItems = random.Next(2) == 0,
            Obstacles = obstacles,
            Items = items,
        };
    }
}
