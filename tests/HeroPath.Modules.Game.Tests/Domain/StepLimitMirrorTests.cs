using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Game.Tests.Domain;

public class StepLimitMirrorTests
{
    [Fact]
    public void LevelLimitsMaxSteps_EqualsProgramLimitsMaxExecutedSteps() =>
        Assert.Equal(ProgramLimits.MaxExecutedSteps, LevelLimits.MaxSteps);
}
