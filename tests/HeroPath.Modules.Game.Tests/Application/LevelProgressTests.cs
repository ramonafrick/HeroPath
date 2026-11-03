using HeroPath.Modules.Game.Application;
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

    private readonly LevelProgress _progress = new(_levels);

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
        Assert.False(new LevelProgress([]).AllSolved);
    }
}
