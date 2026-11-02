using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Tests.Domain;

public class LevelDefinitionTests
{
    [Fact]
    public void ToGrid_ValidDefinition_TransfersAllFields()
    {
        var definition = new LevelDefinition
        {
            Id = "test",
            Width = 6,
            Height = 4,
            Start = new Position(0, 3),
            Goal = new Position(5, 0),
            RequireAllItems = true,
            Obstacles = [new LevelObstacle(2, 2, ObstacleKind.Pond)],
            Items = [new LevelItem(1, 3, ItemKind.Strawberry), new LevelItem(3, 1, ItemKind.Pizza)],
        };

        var grid = definition.ToGrid();

        Assert.Equal(6, grid.Width);
        Assert.Equal(4, grid.Height);
        Assert.Equal(new Position(0, 3), grid.Start);
        Assert.Equal(new Position(5, 0), grid.Goal);
        Assert.True(grid.RequireAllItems);
        Assert.Equal(ObstacleKind.Pond, grid.GetObstacle(new Position(2, 2)));
        Assert.Equal(ItemKind.Strawberry, grid.GetItem(new Position(1, 3)));
        Assert.Equal(ItemKind.Pizza, grid.GetItem(new Position(3, 1)));
        Assert.Equal(1, grid.RequiredItemCount);
    }

    [Fact]
    public void ToGrid_InvalidDefinition_Throws()
    {
        var definition = new LevelDefinition { Id = "bad", Start = new Position(1, 1), Goal = new Position(1, 1) };

        Assert.Throws<ArgumentException>(definition.ToGrid);
    }

    [Fact]
    public void Defaults_UseCurrentSchemaVersionAndEmptyLists()
    {
        var definition = new LevelDefinition();

        Assert.Equal(LevelDefinition.CurrentSchemaVersion, definition.SchemaVersion);
        Assert.Null(definition.Name);
        Assert.Empty(definition.Obstacles);
        Assert.Empty(definition.Items);
    }
}
