using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Tests;

public class LevelLimitsTests
{
    [Fact]
    public void DefaultGridSize_IsWithinLimits()
    {
        Assert.InRange(LevelLimits.DefaultGridSize, LevelLimits.MinGridSize, LevelLimits.MaxGridSize);
    }

    [Fact]
    public void EditorLimits_HaveTheDocumentedValues()
    {
        Assert.Equal(10, LevelLimits.MaxStrawberries);
        Assert.Equal(10, LevelLimits.MaxPizzas);
        Assert.Equal(20, LevelLimits.MaxCustomLevels);
        Assert.Equal(1000, LevelLimits.MaxSteps);
    }
}
