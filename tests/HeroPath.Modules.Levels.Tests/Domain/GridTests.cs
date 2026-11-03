using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Tests.Domain;

public class GridTests
{
    private static readonly Position _start = new(0, 0);
    private static readonly Position _goal = new(4, 3);

    private static Grid Create(
        int width = 5,
        int height = 4,
        Position? start = null,
        Position? goal = null,
        LevelObstacle[]? obstacles = null,
        LevelItem[]? items = null,
        bool requireAllItems = false) =>
        new(width, height, start ?? _start, goal ?? _goal, obstacles ?? [], items ?? [], requireAllItems);

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(4, 0, true)]
    [InlineData(0, 3, true)]
    [InlineData(4, 3, true)]
    [InlineData(-1, 0, false)]
    [InlineData(0, -1, false)]
    [InlineData(5, 0, false)]
    [InlineData(0, 4, false)]
    [InlineData(5, 4, false)]
    public void IsInside_Position_MatchesBounds(int x, int y, bool expected)
    {
        Assert.Equal(expected, Create().IsInside(new Position(x, y)));
    }

    [Fact]
    public void Constructor_NonSquareGrid_StoresDimensions()
    {
        var grid = Create(width: 20, height: 3, goal: new Position(19, 2));

        Assert.Equal(20, grid.Width);
        Assert.Equal(3, grid.Height);
        Assert.True(grid.IsInside(new Position(19, 2)));
        Assert.False(grid.IsInside(new Position(2, 3)));
    }

    [Fact]
    public void GetObstacle_HitAndMiss()
    {
        var grid = Create(obstacles: [new LevelObstacle(2, 1, ObstacleKind.Rock)]);

        Assert.Equal(ObstacleKind.Rock, grid.GetObstacle(new Position(2, 1)));
        Assert.Null(grid.GetObstacle(new Position(1, 1)));
    }

    [Fact]
    public void Obstacles_ReturnsSameViewEachTime()
    {
        var grid = Create(obstacles: [new LevelObstacle(2, 1, ObstacleKind.Rock)]);

        Assert.Same(grid.Obstacles, grid.Obstacles);
        Assert.Equal(ObstacleKind.Rock, Assert.Single(grid.Obstacles).Value);
    }

    [Fact]
    public void GetItem_HitAndMiss()
    {
        var grid = Create(items: [new LevelItem(2, 1, ItemKind.Pizza)]);

        Assert.Equal(ItemKind.Pizza, grid.GetItem(new Position(2, 1)));
        Assert.Null(grid.GetItem(new Position(1, 1)));
    }

    [Fact]
    public void RequiredItemCount_IgnoresPizza()
    {
        var grid = Create(items:
        [
            new LevelItem(1, 0, ItemKind.Strawberry),
            new LevelItem(2, 0, ItemKind.Pizza),
            new LevelItem(3, 0, ItemKind.Strawberry),
        ]);

        Assert.Equal(2, grid.RequiredItemCount);
        Assert.Equal(3, grid.Items.Count);
    }

    [Fact]
    public void Constructor_RequireAllItemsWithStrawberry_Works()
    {
        var grid = Create(items: [new LevelItem(1, 0, ItemKind.Strawberry)], requireAllItems: true);

        Assert.True(grid.RequireAllItems);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Constructor_RequireAllItemsWithoutStrawberry_Throws(bool withPizza)
    {
        LevelItem[] items = withPizza ? [new LevelItem(1, 0, ItemKind.Pizza)] : [];

        Assert.Throws<ArgumentException>(() => Create(items: items, requireAllItems: true));
    }

    [Theory]
    [InlineData(2, 4)]
    [InlineData(21, 4)]
    [InlineData(5, 2)]
    [InlineData(5, 21)]
    public void Constructor_SizeOutsideLimits_Throws(int width, int height)
    {
        Assert.ThrowsAny<ArgumentException>(() => Create(width, height));
    }

    [Theory]
    [InlineData(3, 3)]
    [InlineData(20, 20)]
    public void Constructor_SizeAtLimits_Works(int width, int height)
    {
        var grid = Create(width, height, goal: new Position(2, 2));

        Assert.Equal(width, grid.Width);
        Assert.Equal(height, grid.Height);
    }

    [Fact]
    public void Constructor_ItemOnGoal_Works()
    {
        var grid = Create(items: [new LevelItem(4, 3, ItemKind.Strawberry)]);

        Assert.Equal(ItemKind.Strawberry, grid.GetItem(_goal));
    }

    [Fact]
    public void Constructor_StartOutside_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(start: new Position(5, 0)));
    }

    [Fact]
    public void Constructor_GoalOutside_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(goal: new Position(0, 4)));
    }

    [Fact]
    public void Constructor_StartEqualsGoal_Throws()
    {
        Assert.Throws<ArgumentException>(() => Create(goal: _start));
    }

    [Fact]
    public void Constructor_ObstacleOutside_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(obstacles: [new LevelObstacle(5, 1, ObstacleKind.Tree)]));
    }

    [Fact]
    public void Constructor_ItemOutside_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(items: [new LevelItem(1, -1, ItemKind.Pizza)]));
    }

    public static TheoryData<string, LevelObstacle[], LevelItem[]> ConflictingPlacements => new()
    {
        { "obstacle on start", [new(0, 0, ObstacleKind.Tree)], [] },
        { "obstacle on goal", [new(4, 3, ObstacleKind.Pond)], [] },
        { "item on start", [], [new(0, 0, ItemKind.Strawberry)] },
        { "two obstacles on one cell", [new(2, 1, ObstacleKind.Tree), new(2, 1, ObstacleKind.Rock)], [] },
        { "two items on one cell", [], [new(2, 1, ItemKind.Strawberry), new(2, 1, ItemKind.Pizza)] },
        { "obstacle and item on one cell", [new(2, 1, ObstacleKind.Tree)], [new(2, 1, ItemKind.Pizza)] },
    };

    [Theory]
    [MemberData(nameof(ConflictingPlacements))]
    public void Constructor_ConflictingPlacement_Throws(string description, LevelObstacle[] obstacles, LevelItem[] items)
    {
        Assert.NotNull(description);
        Assert.Throws<ArgumentException>(() => Create(obstacles: obstacles, items: items));
    }

    [Fact]
    public void Equals_SameContentBuiltTwice_IsEqualWithSameHashCode()
    {
        LevelObstacle[] obstacles = [new(1, 1, ObstacleKind.Tree), new(2, 2, ObstacleKind.Rock)];
        LevelItem[] items = [new(3, 1, ItemKind.Strawberry)];

        var first = Create(obstacles: obstacles, items: items);
        var second = Create(obstacles: [.. obstacles.Reverse()], items: items);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentContent_IsNotEqual()
    {
        var grid = Create(obstacles: [new LevelObstacle(1, 1, ObstacleKind.Tree)]);

        Assert.NotEqual(grid, Create());
        Assert.NotEqual(grid, Create(obstacles: [new LevelObstacle(1, 1, ObstacleKind.Rock)]));
        Assert.NotEqual(grid, Create(obstacles: [new LevelObstacle(2, 1, ObstacleKind.Tree)]));
        Assert.NotEqual(Create(), Create(width: 6));
        Assert.NotEqual(Create(), Create(items: [new LevelItem(1, 1, ItemKind.Pizza)]));
        Assert.False(grid.Equals(null));
    }
}
