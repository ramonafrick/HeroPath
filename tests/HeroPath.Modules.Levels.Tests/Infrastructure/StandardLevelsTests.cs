using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.Infrastructure;

namespace HeroPath.Modules.Levels.Tests.Infrastructure;

public class StandardLevelsTests
{
    private const string ResourceMarker = ".Infrastructure.Levels.";

    public static TheoryData<string> LevelIds => [.. StandardLevels.All.Select(level => level.Id)];

    [Fact]
    public void All_ContainsEmbeddedLevels_DemoFirst()
    {
        Assert.NotEmpty(StandardLevels.All);
        Assert.Equal("demo", StandardLevels.All[0].Id);
    }

    [Fact]
    public void All_IdsAreUniqueAndMatchEmbeddedFileNamesInOrder()
    {
        var fileNames = typeof(StandardLevels).Assembly.GetManifestResourceNames()
            .Where(name => name.Contains(ResourceMarker, StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.Ordinal))
            .Select(name => name[(name.IndexOf(ResourceMarker, StringComparison.Ordinal) + ResourceMarker.Length)..^".json".Length])
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Equal(StandardLevels.All.Count, StandardLevels.All.Select(level => level.Id).Distinct().Count());
        Assert.All(fileNames, fileName => Assert.Matches(@"^\d{2}-[a-z0-9-]+$", fileName));
        Assert.Equal(fileNames.Select(fileName => fileName[3..]), StandardLevels.All.Select(level => level.Id));
    }

    [Theory]
    [MemberData(nameof(LevelIds))]
    public void Level_IsValid(string id)
    {
        var level = StandardLevels.Get(id);

        Assert.Empty(LevelValidator.Validate(level));
        Assert.Null(level.Name);
        Assert.Equal(LevelDefinition.CurrentSchemaVersion, level.SchemaVersion);
    }

    [Theory]
    [MemberData(nameof(LevelIds))]
    public void Level_IsSolvable(string id)
    {
        var grid = StandardLevels.Get(id).ToGrid();

        Assert.True(LevelSolver.IsSolvable(grid), $"Level '{id}' cannot be solved.");
    }

    [Fact]
    public void Get_Demo_ReturnsDemoLevel()
    {
        var level = StandardLevels.Get("demo");

        Assert.Equal("demo", level.Id);
        Assert.Equal(10, level.Width);
        Assert.Equal(10, level.Height);
        Assert.Equal(new Position(1, 6), level.Start);
        Assert.Equal(new Position(6, 3), level.Goal);
        Assert.False(level.RequireAllItems);
        Assert.Empty(level.Items);
        Assert.Equal(
            [
                new LevelObstacle(3, 6, ObstacleKind.Tree),
                new LevelObstacle(4, 3, ObstacleKind.Rock),
                new LevelObstacle(6, 5, ObstacleKind.Pond),
            ],
            level.Obstacles);
    }

    [Fact]
    public void Demo_ShortestSolution_HasEightMoves()
    {
        var grid = StandardLevels.Get("demo").ToGrid();

        Assert.Equal(8, LevelSolver.ShortestPath(grid, grid.Start, grid.Goal));
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData("DEMO")]
    public void Get_UnknownId_ThrowsKeyNotFound(string id)
    {
        Assert.Throws<KeyNotFoundException>(() => StandardLevels.Get(id));
    }

    [Fact]
    public void Get_Null_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => StandardLevels.Get(null!));
    }
}
