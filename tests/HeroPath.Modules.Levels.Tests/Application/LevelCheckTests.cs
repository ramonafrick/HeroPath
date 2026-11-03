using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Tests.Application;

public class LevelCheckTests
{
    [Fact]
    public void Evaluate_SolvableLevel_IsValidWithMinimumSteps()
    {
        var result = LevelCheck.Evaluate(Base());

        Assert.Equal(LevelCheckStatus.Valid, result.Status);
        Assert.Equal(8, result.MinimumSteps);
        Assert.Empty(result.Errors);
        Assert.True(result.CanSave);
    }

    [Fact]
    public void Evaluate_ValidationErrors_AreInvalidWithoutSteps()
    {
        var result = LevelCheck.Evaluate(Base() with { Goal = new(0, 4) });

        Assert.Equal(LevelCheckStatus.Invalid, result.Status);
        Assert.Contains(result.Errors, e => e.Kind == LevelValidationErrorKind.StartEqualsGoal);
        Assert.Null(result.MinimumSteps);
        Assert.False(result.CanSave);
    }

    [Fact]
    public void Evaluate_GoalWalledIn_IsGoalUnreachable()
    {
        var result = LevelCheck.Evaluate(Base() with { Obstacles = [new(3, 0, ObstacleKind.Rock), new(4, 1, ObstacleKind.Rock)] });

        Assert.Equal(LevelCheckStatus.GoalUnreachable, result.Status);
        Assert.Null(result.MinimumSteps);
        Assert.False(result.CanSave);
    }

    [Fact]
    public void Evaluate_StrawberryWalledIn_IsStrawberriesUnreachableOnlyWhenRequired()
    {
        var walled = Base() with
        {
            Items = [new(2, 2, ItemKind.Strawberry)],
            Obstacles = [new(1, 2, ObstacleKind.Rock), new(3, 2, ObstacleKind.Rock), new(2, 1, ObstacleKind.Rock), new(2, 3, ObstacleKind.Rock)],
        };

        Assert.Equal(LevelCheckStatus.Valid, LevelCheck.Evaluate(walled).Status);
        var required = LevelCheck.Evaluate(walled with { RequireAllItems = true });
        Assert.Equal(LevelCheckStatus.StrawberriesUnreachable, required.Status);
        Assert.False(required.CanSave);
    }

    [Fact]
    public void Evaluate_ShortestAboveLimit_IsTooManyStepsButKeepsMinimumSteps()
    {
        var result = LevelCheck.Evaluate(Base(), maxSteps: 7);

        Assert.Equal(LevelCheckStatus.TooManySteps, result.Status);
        Assert.Equal(8, result.MinimumSteps);
        Assert.False(result.CanSave);
    }

    [Fact]
    public void Evaluate_ShortestExactlyAtLimit_IsValid() =>
        Assert.Equal(LevelCheckStatus.Valid, LevelCheck.Evaluate(Base(), maxSteps: 8).Status);

    [Fact]
    public void Evaluate_NoErrorsFromTheSolverForInvalidLevel_DoesNotThrow() =>
        Assert.False(LevelCheck.Evaluate(Base() with { Width = 99 }).CanSave);

    private static LevelDefinition Base() => new()
    {
        Id = "check",
        Width = 5,
        Height = 5,
        Start = new(0, 4),
        Goal = new(4, 0),
    };
}
