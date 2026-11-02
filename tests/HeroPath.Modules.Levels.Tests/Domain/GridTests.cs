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

    [Fact]
    public void Constructor_RequireAllItemsWithOnlyPizza_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Create(items: [new LevelItem(1, 0, ItemKind.Pizza)], requireAllItems: true));
    }

    [Fact]
    public void Constructor_RequireAllItemsWithoutItems_Throws()
    {
        Assert.Throws<ArgumentException>(() => Create(requireAllItems: true));
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
    public void Constructor_StartOutside_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() => Create(start: new Position(5, 0)));
    }

    [Fact]
    public void Constructor_GoalOutside_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() => Create(goal: new Position(0, 4)));
    }

    [Fact]
    public void Constructor_StartEqualsGoal_Throws()
    {
        Assert.Throws<ArgumentException>(() => Create(goal: _start));
    }

    [Fact]
    public void Constructor_ObstacleOutside_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() => Create(obstacles: [new LevelObstacle(5, 1, ObstacleKind.Tree)]));
    }

    [Fact]
    public void Constructor_ItemOutside_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() => Create(items: [new LevelItem(1, -1, ItemKind.Pizza)]));
    }

    [Fact]
    public void Constructor_ObstacleOnStart_Throws()
    {
        Assert.Throws<ArgumentException>(() => Create(obstacles: [new LevelObstacle(0, 0, ObstacleKind.Tree)]));
    }

    [Fact]
    public void Constructor_ObstacleOnGoal_Throws()
    {
        Assert.Throws<ArgumentException>(() => Create(obstacles: [new LevelObstacle(4, 3, ObstacleKind.Pond)]));
    }

    [Fact]
    public void Constructor_ItemOnStart_Throws()
    {
        Assert.Throws<ArgumentException>(() => Create(items: [new LevelItem(0, 0, ItemKind.Strawberry)]));
    }

    [Fact]
    public void Constructor_ItemOnGoal_Works()
    {
        var grid = Create(items: [new LevelItem(4, 3, ItemKind.Strawberry)]);

        Assert.Equal(ItemKind.Strawberry, grid.GetItem(_goal));
    }

    [Fact]
    public void Constructor_TwoObstaclesOnSameCell_Throws()
    {
        Assert.Throws<ArgumentException>(() => Create(obstacles:
        [
            new LevelObstacle(2, 1, ObstacleKind.Tree),
            new LevelObstacle(2, 1, ObstacleKind.Rock),
        ]));
    }

    [Fact]
    public void Constructor_TwoItemsOnSameCell_Throws()
    {
        Assert.Throws<ArgumentException>(() => Create(items:
        [
            new LevelItem(2, 1, ItemKind.Strawberry),
            new LevelItem(2, 1, ItemKind.Pizza),
        ]));
    }

    [Fact]
    public void Constructor_ObstacleAndItemOnSameCell_Throws()
    {
        Assert.Throws<ArgumentException>(() => Create(
            obstacles: [new LevelObstacle(2, 1, ObstacleKind.Tree)],
            items: [new LevelItem(2, 1, ItemKind.Pizza)]));
    }
}
