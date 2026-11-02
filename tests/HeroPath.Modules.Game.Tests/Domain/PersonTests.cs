using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Game.Tests.Domain;

public class PersonTests
{
    private static Grid CreateGrid(params LevelObstacle[] obstacles) =>
        new(5, 5, new(0, 4), new(4, 0), obstacles, [new LevelItem(3, 3, ItemKind.Strawberry)], false);

    [Theory]
    [InlineData(Direction.Up, 2, 1)]
    [InlineData(Direction.Down, 2, 3)]
    [InlineData(Direction.Left, 1, 2)]
    [InlineData(Direction.Right, 3, 2)]
    public void Move_FreeCell_MovesOneStep(Direction direction, int x, int y)
    {
        var person = new Person(new(2, 2));

        var result = person.Move(direction, CreateGrid());

        Assert.Equal(MoveOutcome.Moved, result.Outcome);
        Assert.Equal(new Position(x, y), result.NewPosition);
        Assert.Equal(new Position(x, y), person.Position);
        Assert.Null(result.Hit);
    }

    [Theory]
    [InlineData(Direction.Up, 2, 0)]
    [InlineData(Direction.Down, 2, 4)]
    [InlineData(Direction.Left, 0, 2)]
    [InlineData(Direction.Right, 4, 2)]
    public void Move_OutsideGrid_IsOutOfBoundsAndKeepsPosition(Direction direction, int x, int y)
    {
        var person = new Person(new(x, y));

        var result = person.Move(direction, CreateGrid());

        Assert.Equal(MoveOutcome.OutOfBounds, result.Outcome);
        Assert.Equal(new Position(x, y), result.NewPosition);
        Assert.Equal(new Position(x, y), person.Position);
    }

    [Theory]
    [InlineData(ObstacleKind.Tree)]
    [InlineData(ObstacleKind.Rock)]
    [InlineData(ObstacleKind.Pond)]
    public void Move_Obstacle_IsBlockedWithHitAndKeepsPosition(ObstacleKind kind)
    {
        var person = new Person(new(2, 2));
        var grid = CreateGrid(new LevelObstacle(3, 2, kind));

        var result = person.Move(Direction.Right, grid);

        Assert.Equal(MoveOutcome.Blocked, result.Outcome);
        Assert.Equal(kind, result.Hit);
        Assert.Equal(new Position(2, 2), result.NewPosition);
        Assert.Equal(new Position(2, 2), person.Position);
    }

    [Fact]
    public void Move_OntoStart_Moves()
    {
        var person = new Person(new(1, 4));

        var result = person.Move(Direction.Left, CreateGrid());

        Assert.Equal(MoveOutcome.Moved, result.Outcome);
        Assert.Equal(new Position(0, 4), person.Position);
    }

    [Fact]
    public void Move_OntoGoal_Moves()
    {
        var person = new Person(new(3, 0));

        var result = person.Move(Direction.Right, CreateGrid());

        Assert.Equal(MoveOutcome.Moved, result.Outcome);
        Assert.Equal(new Position(4, 0), person.Position);
    }

    [Fact]
    public void Move_OntoItemCell_MovesWithoutCollecting()
    {
        var person = new Person(new(3, 2));
        var grid = CreateGrid();

        var result = person.Move(Direction.Down, grid);

        Assert.Equal(MoveOutcome.Moved, result.Outcome);
        Assert.Equal(new Position(3, 3), person.Position);
        Assert.Equal(ItemKind.Strawberry, grid.GetItem(new(3, 3)));
    }

    [Fact]
    public void Move_NullGrid_Throws()
    {
        var person = new Person(new(0, 0));

        Assert.Throws<ArgumentNullException>(() => person.Move(Direction.Up, null!));
    }
}
