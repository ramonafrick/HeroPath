using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Levels.Infrastructure;

namespace HeroPath.Modules.Game.Tests.Application;

public class LevelProgressTests
{
    private readonly LevelProgress _progress = new(StandardLevels.All);

    [Fact]
    public void Levels_KeepTheGivenOrder()
    {
        Assert.Equal(StandardLevels.All.Select(l => l.Id), _progress.Levels.Select(l => l.Id));
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
        Assert.Equal("easy-2", _progress.NextAfter("easy-1")!.Id);
        Assert.Equal("easy-1", _progress.NextAfter("free-field")!.Id);
    }

    [Fact]
    public void NextAfter_LastLevel_ReturnsNull()
    {
        Assert.Null(_progress.NextAfter(StandardLevels.All[^1].Id));
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

        _progress.MarkSolved("easy-1");
        _progress.MarkSolved("easy-1");

        Assert.True(_progress.IsSolved("easy-1"));
        Assert.False(_progress.IsSolved("easy-2"));
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
        foreach (var level in StandardLevels.All.SkipLast(1))
        {
            _progress.MarkSolved(level.Id);
        }

        Assert.False(_progress.AllSolved);

        _progress.MarkSolved(StandardLevels.All[^1].Id);

        Assert.True(_progress.AllSolved);
    }

    [Fact]
    public void AllSolved_NoLevels_IsFalse()
    {
        Assert.False(new LevelProgress([]).AllSolved);
    }
}
