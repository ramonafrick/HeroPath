using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Tests.Application;

public class LevelReadResultTests
{
    [Fact]
    public void Success_Level_IsValidWithNoErrors()
    {
        var level = new LevelDefinition { Id = "test" };

        var result = LevelReadResult.Success(level);

        Assert.True(result.IsValid);
        Assert.Same(level, result.Level);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Success_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => LevelReadResult.Success(null!));

    [Fact]
    public void Failure_Errors_IsInvalidWithoutLevel()
    {
        var error = new LevelValidationError(LevelValidationErrorKind.InvalidId);

        var result = LevelReadResult.Failure([error]);

        Assert.False(result.IsValid);
        Assert.Null(result.Level);
        Assert.Equal(error, Assert.Single(result.Errors));
    }

    [Fact]
    public void Failure_EmptyErrors_Throws() =>
        Assert.Throws<ArgumentException>(() => LevelReadResult.Failure([]));

    [Fact]
    public void Failure_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => LevelReadResult.Failure(null!));
}
