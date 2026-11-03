using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.Infrastructure;

namespace HeroPath.Modules.Levels.Tests.Infrastructure;

public class StandardLevelsTests
{
    private const string ResourceMarker = ".Infrastructure.Levels.";

    public static TheoryData<string> LevelIds => [.. StandardLevels.All.Select(level => level.Id)];

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

        Assert.True(LevelSolver.ShortestSolution(grid) is not null, $"Level '{id}' cannot be solved.");
    }


    [Fact]
    public void All_ContainsSevenStandardLevelsInOrder()
    {
        Assert.Equal(
            ["free-field", "easy-1", "easy-2", "strawberries", "small-maze", "long-road", "castle-maze"],
            StandardLevels.All.Select(level => level.Id));
    }

    [Theory]
    [InlineData("free-field")]
    [InlineData("easy-1")]
    [InlineData("easy-2")]
    [InlineData("strawberries")]
    public void EarlyLevels_ShortestSolution_IsAtMostTenMoves(string id)
    {
        var moves = LevelSolver.ShortestSolution(StandardLevels.Get(id).ToGrid());

        Assert.NotNull(moves);
        Assert.InRange(moves.Value, 1, 10);
    }

    [Fact]
    public void LongRoad_ShortestSolution_HasThirtyMoves()
    {
        Assert.Equal(30, LevelSolver.ShortestSolution(StandardLevels.Get("long-road").ToGrid()));
    }

    [Fact]
    public void Easy2_HasFormerDemoLayout()
    {
        var level = StandardLevels.Get("easy-2");

        Assert.Equal(10, level.Width);
        Assert.Equal(10, level.Height);
        Assert.Equal(new Position(1, 6), level.Start);
        Assert.Equal(new Position(6, 3), level.Goal);
        Assert.Empty(level.Items);
        Assert.Equal(
            [
                new LevelObstacle(3, 6, ObstacleKind.Tree),
                new LevelObstacle(4, 3, ObstacleKind.Rock),
                new LevelObstacle(6, 5, ObstacleKind.Pond),
            ],
            level.Obstacles);
        Assert.Equal(8, LevelSolver.ShortestSolution(level.ToGrid()));
    }

    [Fact]
    public void Strawberries_RequiresAllItems()
    {
        var level = StandardLevels.Get("strawberries");

        Assert.True(level.RequireAllItems);
        Assert.Equal(3, level.Items.Count(item => item.Kind == ItemKind.Strawberry));
    }

    [Fact]
    public void AtLeastOneLevel_ContainsPizza()
    {
        Assert.Contains(StandardLevels.All, level => level.Items.Any(item => item.Kind == ItemKind.Pizza));
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData("EASY-2")]
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
