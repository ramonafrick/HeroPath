using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Game.Tests.Application;

public class LevelProgressTests
{
    private static readonly LevelDefinition[] _levels =
    [
        TestLevels.Empty("level-a", 5, 5, new Position(0, 0), new Position(4, 4)),
        TestLevels.Empty("level-b", 5, 5, new Position(0, 0), new Position(4, 0)),
        TestLevels.Empty("level-c", 5, 5, new Position(0, 4), new Position(4, 0)),
    ];

    private readonly LevelProgress _progress = new(TestLevels.NewRepository(_levels));

    [Fact]
    public void Levels_KeepTheGivenOrder()
    {
        Assert.Equal(_levels.Select(l => l.Id), _progress.Levels.Select(l => l.Id));
    }

    [Fact]
    public void Find_KnownId_ReturnsLevel()
    {
        Assert.Same(_levels[1], _progress.Find("level-b"));
    }

    [Fact]
    public void Find_UnknownId_ReturnsNull()
    {
        Assert.Null(_progress.Find("nope"));
    }

    [Fact]
    public void NewProgress_HasNothingSolved()
    {
        Assert.All(_progress.Levels, level => Assert.False(_progress.IsSolved(level.Id)));
        Assert.False(_progress.AllSolved);
    }

    [Fact]
    public void NextAfter_MiddleLevel_ReturnsFollowingLevel()
    {
        Assert.Equal("level-c", _progress.NextAfter("level-b")!.Id);
        Assert.Equal("level-b", _progress.NextAfter("level-a")!.Id);
    }

    [Fact]
    public void NextAfter_LastLevel_ReturnsNull()
    {
        Assert.Null(_progress.NextAfter("level-c"));
    }

    [Fact]
    public void NextAfter_UnknownId_ReturnsNull()
    {
        Assert.Null(_progress.NextAfter("nope"));
    }

    [Fact]
    public void MarkSolved_MarksOnlyThatLevelAndRaisesChangedOnce()
    {
        var changes = 0;
        _progress.Changed += () => changes++;

        _progress.MarkSolved("level-a");
        _progress.MarkSolved("level-a");

        Assert.True(_progress.IsSolved("level-a"));
        Assert.False(_progress.IsSolved("level-b"));
        Assert.Equal(1, changes);
    }

    [Fact]
    public void MarkSolved_UnknownId_IsIgnored()
    {
        var changes = 0;
        _progress.Changed += () => changes++;

        _progress.MarkSolved("nope");

        Assert.False(_progress.IsSolved("nope"));
        Assert.Equal(0, changes);
    }

    [Fact]
    public void AllSolved_OnlyAfterEveryLevelIsMarked()
    {
        foreach (var level in _levels.SkipLast(1))
        {
            _progress.MarkSolved(level.Id);
        }

        Assert.False(_progress.AllSolved);

        _progress.MarkSolved(_levels[^1].Id);

        Assert.True(_progress.AllSolved);
    }

    [Fact]
    public void AllSolved_NoLevels_IsFalse()
    {
        Assert.False(new LevelProgress(TestLevels.NewRepository([])).AllSolved);
    }

    [Fact]
    public async Task CustomLevels_AreFoundAndCanBeMarkedSolved()
    {
        var (progress, repository) = await ProgressWithCustomLevelAsync();

        Assert.Equal(["level-a", "level-b", "level-c", "custom-1"], progress.Levels.Select(l => l.Id));
        Assert.Equal("Mine", progress.Find("custom-1")!.Name);
        var changes = 0;
        progress.Changed += () => changes++;

        progress.MarkSolved("custom-1");

        Assert.True(progress.IsSolved("custom-1"));
        Assert.Equal(1, changes);
        Assert.True(repository.IsCustom("custom-1"));
    }

    [Fact]
    public async Task NextAfter_LastStandardLevelAndCustomLevels_ReturnNull()
    {
        var (progress, _) = await ProgressWithCustomLevelAsync();

        Assert.Null(progress.NextAfter("level-c"));
        Assert.Null(progress.NextAfter("custom-1"));
        Assert.Equal("level-c", progress.NextAfter("level-b")!.Id);
    }

    [Fact]
    public async Task AllSolved_OnlyLooksAtStandardLevels()
    {
        var (progress, _) = await ProgressWithCustomLevelAsync();

        foreach (var level in _levels)
        {
            progress.MarkSolved(level.Id);
        }

        Assert.True(progress.AllSolved);
    }

    [Fact]
    public async Task AllSolved_SolvedCustomLevelDoesNotReplaceAStandardLevel()
    {
        var (progress, _) = await ProgressWithCustomLevelAsync();
        progress.MarkSolved("level-a");
        progress.MarkSolved("level-b");
        progress.MarkSolved("custom-1");

        Assert.False(progress.AllSolved);
    }

    [Fact]
    public async Task Levels_SeeLaterAdditionsAndDeletions()
    {
        var repository = TestLevels.NewRepository(_levels);
        var progress = new LevelProgress(repository);
        Assert.Equal(3, progress.Levels.Count);

        await repository.SaveAsync(Custom("custom-1"), Xunit.TestContext.Current.CancellationToken);
        Assert.Equal(4, progress.Levels.Count);
        Assert.NotNull(progress.Find("custom-1"));

        await repository.DeleteAsync("custom-1", Xunit.TestContext.Current.CancellationToken);
        Assert.Equal(3, progress.Levels.Count);
        Assert.Null(progress.Find("custom-1"));
    }

    [Fact]
    public async Task Save_SolvedCustomLevelWithChangedGrid_ForgetsTheTick()
    {
        var (progress, repository) = await ProgressWithCustomLevelAsync();
        progress.MarkSolved("custom-1");
        var changes = 0;
        progress.Changed += () => changes++;

        await repository.SaveAsync(Custom("custom-1") with { Goal = new Position(4, 3) }, Xunit.TestContext.Current.CancellationToken);

        Assert.False(progress.IsSolved("custom-1"));
        Assert.Equal(1, changes);
    }

    [Fact]
    public async Task Save_SolvedCustomLevelRenamedOnly_KeepsTheTick()
    {
        var (progress, repository) = await ProgressWithCustomLevelAsync();
        progress.MarkSolved("custom-1");
        var changes = 0;
        progress.Changed += () => changes++;

        await repository.SaveAsync(Custom("custom-1") with { Name = "Renamed" }, Xunit.TestContext.Current.CancellationToken);

        Assert.True(progress.IsSolved("custom-1"));
        Assert.Equal(0, changes);
    }

    [Fact]
    public async Task Save_OtherLevelChanges_KeepsTheTickOfTheSolvedOne()
    {
        var (progress, repository) = await ProgressWithCustomLevelAsync();
        progress.MarkSolved("custom-1");
        progress.MarkSolved("level-a");

        await repository.SaveAsync(Custom("custom-2"), Xunit.TestContext.Current.CancellationToken);

        Assert.True(progress.IsSolved("custom-1"));
        Assert.True(progress.IsSolved("level-a"));
    }

    [Fact]
    public async Task Delete_SolvedCustomLevel_RemovesTheTick()
    {
        var (progress, repository) = await ProgressWithCustomLevelAsync();
        progress.MarkSolved("custom-1");

        await repository.DeleteAsync("custom-1", Xunit.TestContext.Current.CancellationToken);

        Assert.False(progress.IsSolved("custom-1"));
    }

    [Fact]
    public async Task Save_ChangedGridAfterForgetting_CanBeSolvedAgain()
    {
        var (progress, repository) = await ProgressWithCustomLevelAsync();
        progress.MarkSolved("custom-1");
        await repository.SaveAsync(Custom("custom-1") with { Goal = new Position(4, 3) }, Xunit.TestContext.Current.CancellationToken);

        progress.MarkSolved("custom-1");

        Assert.True(progress.IsSolved("custom-1"));
        await repository.SaveAsync(Custom("custom-1") with { Goal = new Position(4, 3), Name = "Again" }, Xunit.TestContext.Current.CancellationToken);
        Assert.True(progress.IsSolved("custom-1"));
    }

    private static LevelDefinition Custom(string id) =>
        TestLevels.Empty(id, 5, 5, new Position(0, 0), new Position(4, 4)) with { Name = "Mine" };

    private static async Task<(LevelProgress Progress, LevelRepository Repository)> ProgressWithCustomLevelAsync()
    {
        var repository = TestLevels.NewRepository(_levels);
        await repository.SaveAsync(Custom("custom-1"), Xunit.TestContext.Current.CancellationToken);
        return (new LevelProgress(repository), repository);
    }
}
