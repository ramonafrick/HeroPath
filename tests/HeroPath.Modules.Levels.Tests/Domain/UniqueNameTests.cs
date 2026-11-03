using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Tests.Domain;

public class UniqueNameTests
{
    [Fact]
    public void UniqueName_NoClash_KeepsTheName() =>
        Assert.Equal("Käse-Weg", LevelEditing.UniqueName("Käse-Weg", ["Anderes", null]));

    [Fact]
    public void UniqueName_Clash_AppendsTwo() =>
        Assert.Equal("Käse-Weg (2)", LevelEditing.UniqueName("Käse-Weg", ["Käse-Weg"]));

    [Fact]
    public void UniqueName_SeveralClashes_TakesTheSmallestFreeNumber() =>
        Assert.Equal("A (4)", LevelEditing.UniqueName("A", ["A", "A (2)", "A (3)", "A (5)"]));

    [Fact]
    public void UniqueName_Comparison_IsOrdinal() =>
        Assert.Equal("kaese", LevelEditing.UniqueName("kaese", ["KAESE"]));

    [Fact]
    public void UniqueName_LongName_IsCutSoTheResultFits()
    {
        var name = new string('x', LevelLimits.MaxNameLength);

        var result = LevelEditing.UniqueName(name, [name]);

        Assert.Equal(LevelLimits.MaxNameLength, result.Length);
        Assert.Equal(new string('x', LevelLimits.MaxNameLength - 4) + " (2)", result);
    }

    [Fact]
    public void UniqueName_CutInsideASurrogatePair_DropsTheWholePair()
    {
        var name = new string('x', 45) + "😀" + "xxx";
        Assert.Equal(LevelLimits.MaxNameLength, name.Length);

        var result = LevelEditing.UniqueName(name, [name]);

        Assert.Equal(new string('x', 45) + " (2)", result);
    }
}
