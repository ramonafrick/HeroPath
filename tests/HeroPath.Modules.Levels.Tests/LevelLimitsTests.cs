using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Tests;

public class LevelLimitsTests
{
    [Fact]
    public void DefaultGridSize_IsWithinLimits()
    {
        Assert.InRange(LevelLimits.DefaultGridSize, LevelLimits.MinGridSize, LevelLimits.MaxGridSize);
    }
}
