using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.Infrastructure;

namespace HeroPath.Modules.Levels.Tests.Infrastructure;

public class LevelJsonTests
{
    private const string ExampleJson = """
        {
          "schemaVersion": 1,
          "id": "free-field",
          "name": null,
          "width": 10,
          "height": 10,
          "start": { "x": 0, "y": 9 },
          "goal": { "x": 9, "y": 0 },
          "requireAllItems": false,
          "obstacles": [ { "x": 4, "y": 5, "kind": "tree" } ],
          "items": [ { "x": 2, "y": 7, "kind": "strawberry" } ]
        }
        """;

    [Fact]
    public void Read_ExampleJson_ParsesExpectedValues()
    {
        var result = LevelJson.Read(ExampleJson);

        Assert.True(result.IsValid, string.Join(",", result.Errors));
        var level = result.Level!;
        Assert.Equal(1, level.SchemaVersion);
        Assert.Equal("free-field", level.Id);
        Assert.Null(level.Name);
        Assert.Equal(10, level.Width);
        Assert.Equal(10, level.Height);
        Assert.Equal(new Position(0, 9), level.Start);
        Assert.Equal(new Position(9, 0), level.Goal);
        Assert.False(level.RequireAllItems);
        Assert.Equal([new LevelObstacle(4, 5, ObstacleKind.Tree)], level.Obstacles);
        Assert.Equal([new LevelItem(2, 7, ItemKind.Strawberry)], level.Items);
    }

    [Fact]
    public void WriteRead_RoundTrip_PreservesAllFields()
    {
        var original = new LevelDefinition
        {
            Id = "round-trip",
            Name = "Mein Level",
            Width = 6,
            Height = 4,
            Start = new Position(0, 3),
            Goal = new Position(5, 0),
            RequireAllItems = true,
            Obstacles = [new LevelObstacle(2, 2, ObstacleKind.Pond), new LevelObstacle(3, 1, ObstacleKind.Rock)],
            Items = [new LevelItem(1, 3, ItemKind.Strawberry), new LevelItem(4, 0, ItemKind.Pizza)],
        };

        var result = LevelJson.Read(LevelJson.Write(original));

        Assert.True(result.IsValid, string.Join(",", result.Errors));
        var copy = result.Level!;
        Assert.Equal(original.SchemaVersion, copy.SchemaVersion);
        Assert.Equal(original.Id, copy.Id);
        Assert.Equal(original.Name, copy.Name);
        Assert.Equal(original.Width, copy.Width);
        Assert.Equal(original.Height, copy.Height);
        Assert.Equal(original.Start, copy.Start);
        Assert.Equal(original.Goal, copy.Goal);
        Assert.Equal(original.RequireAllItems, copy.RequireAllItems);
        Assert.Equal(original.Obstacles, copy.Obstacles);
        Assert.Equal(original.Items, copy.Items);
    }

    [Fact]
    public void Write_Level_UsesCamelCasePropertiesAndEnumStrings()
    {
        var json = LevelJson.Write(new LevelDefinition
        {
            Id = "x",
            Goal = new Position(9, 9),
            Obstacles = [new LevelObstacle(1, 1, ObstacleKind.Tree)],
            Items = [new LevelItem(2, 2, ItemKind.Strawberry)],
        });

        Assert.Contains("\"schemaVersion\": 1", json);
        Assert.Contains("\"requireAllItems\"", json);
        Assert.Contains("\"kind\": \"tree\"", json);
        Assert.Contains("\"kind\": \"strawberry\"", json);
        Assert.DoesNotContain("Tree", json);
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("[1, 2, 3]")]
    [InlineData("42")]
    [InlineData("{\"width\": \"wide\"}")]
    [InlineData("{\"start\": 5}")]
    [InlineData("{\"obstacles\": [1]}")]
    [InlineData("{\"width\": 99999999999999999999}")]
    public void Read_BrokenJson_ReturnsInvalidJsonWithoutThrowing(string json)
    {
        var result = LevelJson.Read(json);

        Assert.False(result.IsValid);
        Assert.Null(result.Level);
        Assert.Equal(LevelValidationErrorKind.InvalidJson, Assert.Single(result.Errors).Kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("null")]
    public void Read_EmptyOrNull_ReturnsInvalidJson(string? json)
    {
        var result = LevelJson.Read(json);

        Assert.False(result.IsValid);
        Assert.Equal(LevelValidationErrorKind.InvalidJson, Assert.Single(result.Errors).Kind);
    }

    [Fact]
    public void Read_DeeplyNestedJson_DoesNotThrow()
    {
        var json = new string('[', 5000) + new string(']', 5000);

        var result = LevelJson.Read(json);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Read_InputLongerThanLimit_ReturnsInvalidJsonWithoutParsing()
    {
        var padded = ExampleJson + new string(' ', LevelJson.MaxJsonLength);

        var result = LevelJson.Read(padded);

        Assert.False(result.IsValid);
        Assert.Equal(LevelValidationErrorKind.InvalidJson, Assert.Single(result.Errors).Kind);
    }

    [Fact]
    public void Read_InputAtLimit_IsParsed()
    {
        var padded = ExampleJson + new string(' ', LevelJson.MaxJsonLength - ExampleJson.Length);

        Assert.Equal(LevelJson.MaxJsonLength, padded.Length);
        Assert.True(LevelJson.Read(padded).IsValid);
    }

    [Theory]
    [InlineData(6, true)]
    [InlineData(7, true)]
    [InlineData(8, false)]
    [InlineData(20, false)]
    public void Read_NestedUnknownProperty_HonorsMaxDepth(int arrayDepth, bool expectedValid)
    {
        var nested = new string('[', arrayDepth) + new string(']', arrayDepth);
        var json = ExampleJson.Replace("\"schemaVersion\": 1,", $"\"schemaVersion\": 1, \"extra\": {nested},");

        var result = LevelJson.Read(json);

        Assert.Equal(expectedValid, result.IsValid);
        if (!expectedValid)
        {
            Assert.Equal(LevelValidationErrorKind.InvalidJson, Assert.Single(result.Errors).Kind);
        }
    }

    [Fact]
    public void Read_InvalidIdInJson_ReturnsInvalidId()
    {
        var result = LevelJson.Read(ExampleJson.Replace("\"free-field\"", "\"Free Field\""));

        Assert.False(result.IsValid);
        Assert.Equal(LevelValidationErrorKind.InvalidId, Assert.Single(result.Errors).Kind);
    }

    [Fact]
    public void Read_UnknownEnumValue_ReturnsInvalidJson()
    {
        var result = LevelJson.Read(ExampleJson.Replace("\"tree\"", "\"lava\""));

        Assert.False(result.IsValid);
        Assert.Equal(LevelValidationErrorKind.InvalidJson, Assert.Single(result.Errors).Kind);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("\"0\"")]
    public void Read_IntegerEnumValue_ReturnsInvalidJson(string value)
    {
        var result = LevelJson.Read(ExampleJson.Replace("\"tree\"", value));

        Assert.False(result.IsValid);
        Assert.Equal(LevelValidationErrorKind.InvalidJson, Assert.Single(result.Errors).Kind);
    }

    [Fact]
    public void Read_SchemaVersion2_ReturnsUnsupportedSchemaVersion()
    {
        var result = LevelJson.Read(ExampleJson.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 2"));

        Assert.False(result.IsValid);
        Assert.Null(result.Level);
        Assert.Equal(LevelValidationErrorKind.UnsupportedSchemaVersion, Assert.Single(result.Errors).Kind);
    }

    [Fact]
    public void Read_MissingSchemaVersion_ReturnsUnsupportedSchemaVersion()
    {
        var result = LevelJson.Read(ExampleJson.Replace("\"schemaVersion\": 1,", string.Empty));

        Assert.False(result.IsValid);
        Assert.Equal(LevelValidationErrorKind.UnsupportedSchemaVersion, Assert.Single(result.Errors).Kind);
    }

    [Fact]
    public void Read_NullLists_ReturnsInvalidListErrors()
    {
        var json = ExampleJson
            .Replace("[ { \"x\": 4, \"y\": 5, \"kind\": \"tree\" } ]", "null")
            .Replace("[ { \"x\": 2, \"y\": 7, \"kind\": \"strawberry\" } ]", "null");

        var result = LevelJson.Read(json);

        Assert.False(result.IsValid);
        var kinds = result.Errors.Select(e => e.Kind).ToList();
        Assert.Contains(LevelValidationErrorKind.ObstaclesInvalid, kinds);
        Assert.Contains(LevelValidationErrorKind.ItemsInvalid, kinds);
    }

    [Fact]
    public void Read_ListsOmitted_ReturnsInvalidListErrors()
    {
        const string json = """{ "schemaVersion": 1, "id": "a", "width": 10, "height": 10, "start": { "x": 0, "y": 0 }, "goal": { "x": 9, "y": 9 } }""";

        var kinds = LevelJson.Read(json).Errors.Select(e => e.Kind).ToList();

        Assert.Contains(LevelValidationErrorKind.ObstaclesInvalid, kinds);
        Assert.Contains(LevelValidationErrorKind.ItemsInvalid, kinds);
    }

    [Fact]
    public void Read_ValidJsonWithRuleViolations_ReturnsValidatorErrors()
    {
        var result = LevelJson.Read(ExampleJson.Replace("\"goal\": { \"x\": 9, \"y\": 0 }", "\"goal\": { \"x\": 0, \"y\": 9 }"));

        Assert.False(result.IsValid);
        Assert.Null(result.Level);
        Assert.Equal(LevelValidationErrorKind.StartEqualsGoal, Assert.Single(result.Errors).Kind);
    }
}
