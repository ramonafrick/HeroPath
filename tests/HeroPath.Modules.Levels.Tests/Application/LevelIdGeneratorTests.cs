using HeroPath.Modules.Levels.Application;

namespace HeroPath.Modules.Levels.Tests.Application;

public class LevelIdGeneratorTests
{
    [Fact]
    public void NewId_HasPrefixAndTwelveLowercaseHexCharacters() =>
        Assert.Matches(@"^custom-[0-9a-f]{12}\z", LevelIdGenerator.NewId(_ => false));

    [Fact]
    public void NewId_PassesTheValidatorIdRule()
    {
        var level = new Levels.Domain.LevelDefinition { Id = LevelIdGenerator.NewId(_ => false), Start = new(0, 9), Goal = new(9, 0) };

        Assert.Empty(LevelValidator.Validate(level));
    }

    [Fact]
    public void NewId_DrawsAgainOnCollision()
    {
        var seen = new List<string>();

        var id = LevelIdGenerator.NewId(candidate =>
        {
            seen.Add(candidate);
            return seen.Count < 3;
        });

        Assert.Equal(3, seen.Count);
        Assert.Equal(seen[^1], id);
        Assert.Equal(3, seen.Distinct().Count());
    }
}
