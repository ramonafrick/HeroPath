using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Tests.Application;

public class LevelValidatorTests
{
    public static TheoryData<string, LevelDefinition, LevelValidationErrorKind, Position?> SingleErrorCases => new()
    {
        { "schema version 2", Valid() with { SchemaVersion = 2 }, LevelValidationErrorKind.UnsupportedSchemaVersion, null },
        { "width 2", Valid() with { Width = 2, Start = new(0, 0), Goal = new(1, 0), Obstacles = [], Items = [] }, LevelValidationErrorKind.WidthOutOfRange, null },
        { "width 21", Valid() with { Width = 21, Start = new(0, 0), Goal = new(1, 0), Obstacles = [], Items = [] }, LevelValidationErrorKind.WidthOutOfRange, null },
        { "height 2", Valid() with { Height = 2, Start = new(0, 0), Obstacles = [], Items = [] }, LevelValidationErrorKind.HeightOutOfRange, null },
        { "height 21", Valid() with { Height = 21, Start = new(0, 0), Obstacles = [], Items = [] }, LevelValidationErrorKind.HeightOutOfRange, null },
        { "start x -1", Valid() with { Start = new(-1, 0) }, LevelValidationErrorKind.StartOutsideGrid, new Position(-1, 0) },
        { "start x 5", Valid() with { Start = new(5, 0) }, LevelValidationErrorKind.StartOutsideGrid, new Position(5, 0) },
        { "start y 5", Valid() with { Start = new(0, 5) }, LevelValidationErrorKind.StartOutsideGrid, new Position(0, 5) },
        { "goal y -1", Valid() with { Goal = new(0, -1) }, LevelValidationErrorKind.GoalOutsideGrid, new Position(0, -1) },
        { "start equals goal", Valid() with { Goal = new(0, 4) }, LevelValidationErrorKind.StartEqualsGoal, new Position(0, 4) },
        { "null obstacle list", Valid() with { Obstacles = null! }, LevelValidationErrorKind.ObstaclesInvalid, null },
        { "null obstacle entry", Valid() with { Obstacles = [null!] }, LevelValidationErrorKind.ObstaclesInvalid, null },
        { "null item list", Valid() with { Items = null! }, LevelValidationErrorKind.ItemsInvalid, null },
        { "null item entry", Valid() with { Items = [null!] }, LevelValidationErrorKind.ItemsInvalid, null },
        { "obstacle outside", Valid() with { Obstacles = [new(5, 2, ObstacleKind.Rock)] }, LevelValidationErrorKind.ObstacleOutsideGrid, new Position(5, 2) },
        { "item outside", Valid() with { Items = [new(-1, 0, ItemKind.Pizza)] }, LevelValidationErrorKind.ItemOutsideGrid, new Position(-1, 0) },
        { "obstacle on start", Valid() with { Obstacles = [new(0, 4, ObstacleKind.Pond)] }, LevelValidationErrorKind.ObstacleOnStart, new Position(0, 4) },
        { "obstacle on goal", Valid() with { Obstacles = [new(4, 0, ObstacleKind.Tree)] }, LevelValidationErrorKind.ObstacleOnGoal, new Position(4, 0) },
        { "item on start", Valid() with { Items = [new(0, 4, ItemKind.Strawberry)] }, LevelValidationErrorKind.ItemOnStart, new Position(0, 4) },
        {
            "two obstacles on one cell",
            Valid() with { Obstacles = [new(2, 2, ObstacleKind.Tree), new(2, 2, ObstacleKind.Rock)] },
            LevelValidationErrorKind.CellOccupiedTwice,
            new Position(2, 2)
        },
        {
            "two items on one cell",
            Valid() with { Items = [new(1, 1, ItemKind.Strawberry), new(1, 1, ItemKind.Pizza)] },
            LevelValidationErrorKind.CellOccupiedTwice,
            new Position(1, 1)
        },
        { "obstacle and item on one cell", Valid() with { Items = [new(2, 2, ItemKind.Strawberry)] }, LevelValidationErrorKind.CellOccupiedTwice, new Position(2, 2) },
        { "require all without items", Valid() with { RequireAllItems = true, Items = [] }, LevelValidationErrorKind.RequiredItemsMissing, null },
        {
            "require all with only pizza",
            Valid() with { RequireAllItems = true, Items = [new(1, 1, ItemKind.Pizza)] },
            LevelValidationErrorKind.RequiredItemsMissing,
            null
        },
    };

    public static TheoryData<string, LevelDefinition> ValidCases => new()
    {
        { "valid base level", Valid() },
        { "default level with distinct goal", new LevelDefinition { Id = "x", Goal = new(9, 9) } },
        { "width 3", Valid() with { Width = 3, Goal = new(2, 0) } },
        { "width 20", Valid() with { Width = 20, Goal = new(2, 0) } },
        { "height 3", Valid() with { Height = 3, Start = new(0, 0) } },
        { "height 20", Valid() with { Height = 20, Start = new(0, 0) } },
        { "start on last cell", Valid() with { Start = new(4, 4) } },
        { "obstacle on last cell", Valid() with { Obstacles = [new(4, 4, ObstacleKind.Rock)] } },
        { "item on goal", Valid() with { Items = [new(4, 0, ItemKind.Strawberry)] } },
        { "require all with strawberry", Valid() with { RequireAllItems = true } },
    };

    public static TheoryData<string> ValidIds => ["a", "free-field", "level-01", "0", "-", new string('a', LevelLimits.MaxIdLength)];

    public static TheoryData<string> InvalidIds =>
        ["", "Upper", "has space", "under_score", "ümlaut", "dot.dot", "new\nline", "trailing\n", new string('a', LevelLimits.MaxIdLength + 1)];

    public static TheoryData<string> ValidNames => ["Mein Level", "a", "Hällo «Welt»", new string('x', LevelLimits.MaxNameLength)];

    public static TheoryData<string> InvalidNames =>
        ["", " ", "   \t", "tab\there", "line\nbreak", "bell\a", new string('x', LevelLimits.MaxNameLength + 1)];

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

    private static void AssertOnly(LevelDefinition level, LevelValidationErrorKind kind)
    {
        var error = Assert.Single(LevelValidator.Validate(level));
        Assert.Equal(kind, error.Kind);
    }

    private static IEnumerable<Position> Cells(int width, int height) =>
        Enumerable.Range(0, height).SelectMany(y => Enumerable.Range(0, width).Select(x => new Position(x, y)));

    [Theory]
    [MemberData(nameof(SingleErrorCases))]
    public void Validate_RuleViolation_ReportsExactlyOneError(string description, LevelDefinition level, LevelValidationErrorKind kind, Position? position)
    {
        Assert.NotNull(description);

        var error = Assert.Single(LevelValidator.Validate(level));

        Assert.Equal(kind, error.Kind);
        Assert.Equal(position, error.Position);
    }

    [Theory]
    [MemberData(nameof(ValidCases))]
    public void Validate_ValidVariant_ReturnsNoErrors(string description, LevelDefinition level)
    {
        Assert.NotNull(description);
        Assert.Empty(LevelValidator.Validate(level));
    }

    [Fact]
    public void Validate_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => LevelValidator.Validate(null!));

    [Theory]
    [MemberData(nameof(ValidIds))]
    public void Validate_ValidId_ReturnsNoErrors(string id) =>
        Assert.Empty(LevelValidator.Validate(Valid() with { Id = id }));

    [Theory]
    [MemberData(nameof(InvalidIds))]
    public void Validate_InvalidId_ReportsInvalidId(string id) =>
        AssertOnly(Valid() with { Id = id }, LevelValidationErrorKind.InvalidId);

    [Theory]
    [MemberData(nameof(ValidNames))]
    public void Validate_ValidName_ReturnsNoErrors(string name) =>
        Assert.Empty(LevelValidator.Validate(Valid() with { Name = name }));

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public void Validate_InvalidName_ReportsInvalidName(string name) =>
        AssertOnly(Valid() with { Name = name }, LevelValidationErrorKind.InvalidName);

    [Fact]
    public void Validate_NullId_ReportsInvalidId() =>
        AssertOnly(Valid() with { Id = null! }, LevelValidationErrorKind.InvalidId);

    [Fact]
    public void Validate_NullName_ReturnsNoErrors() =>
        Assert.Empty(LevelValidator.Validate(Valid() with { Name = null }));

    [Fact]
    public void Validate_ObjectCountEqualToCellCount_IsNotTooMany()
    {
        var level = Valid() with
        {
            Width = 3,
            Height = 3,
            Start = new(0, 0),
            Goal = new(2, 2),
            Obstacles = [.. Cells(3, 3).Skip(1).Take(4).Select(p => new LevelObstacle(p.X, p.Y, ObstacleKind.Tree))],
            Items = [.. Cells(3, 3).Skip(5).Take(4).Select(p => new LevelItem(p.X, p.Y, ItemKind.Pizza))],
        };

        Assert.DoesNotContain(LevelValidator.Validate(level), e => e.Kind == LevelValidationErrorKind.TooManyObjects);
    }

    [Fact]
    public void Validate_MoreObjectsThanCells_ReportsOnlyTooManyObjects()
    {
        var level = Valid() with
        {
            Width = 3,
            Height = 3,
            Start = new(0, 0),
            Goal = new(2, 2),
            Obstacles = [.. Enumerable.Range(0, 6).Select(_ => new LevelObstacle(50, 50, ObstacleKind.Tree))],
            Items = [.. Enumerable.Range(0, 4).Select(_ => new LevelItem(-5, -5, ItemKind.Pizza))],
        };

        AssertOnly(level, LevelValidationErrorKind.TooManyObjects);
    }

    [Fact]
    public void Validate_HugeObjectList_ReportsOneError()
    {
        var level = Valid() with
        {
            Obstacles = [.. Enumerable.Range(0, 100_000).Select(_ => new LevelObstacle(-1, -1, ObstacleKind.Tree))],
        };

        AssertOnly(level, LevelValidationErrorKind.TooManyObjects);
    }

    [Fact]
    public void Validate_MultipleProblems_CollectsAllErrors()
    {
        var level = Valid() with
        {
            Id = "BAD",
            Name = " ",
            Width = 99,
            Goal = new Position(0, 4),
            Obstacles = [new LevelObstacle(0, 4, ObstacleKind.Tree)],
            RequireAllItems = true,
            Items = [],
        };

        var kinds = LevelValidator.Validate(level).Select(e => e.Kind).ToList();

        Assert.Contains(LevelValidationErrorKind.InvalidId, kinds);
        Assert.Contains(LevelValidationErrorKind.InvalidName, kinds);
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
