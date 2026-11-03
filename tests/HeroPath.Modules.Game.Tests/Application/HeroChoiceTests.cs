using HeroPath.Modules.Game.Application;

namespace HeroPath.Modules.Game.Tests.Application;

public class HeroChoiceTests
{
    [Fact]
    public void New_IsNotChosenAndHasDefaults()
    {
        var choice = new HeroChoice();

        Assert.False(choice.IsChosen);
        Assert.Equal(HeroAvatar.Max, choice.Avatar);
        Assert.Equal("Max", choice.Name);
    }

    [Fact]
    public void New_LevelIdIsTheFirstStandardLevel() =>
        Assert.Equal("free-field", new HeroChoice().LevelId);

    [Fact]
    public void Choose_StoresAvatarNormalizedNameAndLevel()
    {
        var choice = new HeroChoice();

        choice.Choose(HeroAvatar.Lisa, "  Zoe  ", "easy-1");

        Assert.True(choice.IsChosen);
        Assert.Equal(HeroAvatar.Lisa, choice.Avatar);
        Assert.Equal("Zoe", choice.Name);
        Assert.Equal("easy-1", choice.LevelId);
    }

    [Fact]
    public void Choose_NullLevelId_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new HeroChoice().Choose(HeroAvatar.Max, "Max", null!));

    [Fact]
    public void Choose_RaisesChangedOnlyForRealChanges()
    {
        var choice = new HeroChoice();
        var changes = 0;
        choice.Changed += () => changes++;

        choice.Choose(HeroAvatar.Max, "Max", "easy-1");
        choice.Choose(HeroAvatar.Max, "  Max ", "easy-1");
        choice.Choose(HeroAvatar.Max, "Moritz", "easy-1");

        Assert.Equal(2, changes);
    }

    [Theory]
    [InlineData("Zoe", "Zoe")]
    [InlineData("  Zoe  ", "Zoe")]
    [InlineData("\tZo\ne\r", "Zoe")]
    [InlineData("Z\u0000o\u0007e", "Zoe")]
    [InlineData("Anna Lena", "Anna Lena")]
    public void NormalizeName_RemovesControlCharactersAndTrims(string raw, string expected) =>
        Assert.Equal(expected, HeroChoice.NormalizeName(raw, HeroAvatar.Max));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    [InlineData("\u0000\u0001")]
    public void NormalizeName_EmptyOrWhitespaceOnly_FallsBackToDefaultOfTheAvatar(string? raw)
    {
        Assert.Equal("Max", HeroChoice.NormalizeName(raw, HeroAvatar.Max));
        Assert.Equal("Lisa", HeroChoice.NormalizeName(raw, HeroAvatar.Lisa));
    }

    [Fact]
    public void NormalizeName_ExactlyMaxLength_IsKept()
    {
        var name = new string('a', HeroChoice.MaxNameLength);

        Assert.Equal(name, HeroChoice.NormalizeName(name, HeroAvatar.Max));
    }

    [Fact]
    public void NormalizeName_LongerPastedText_IsCutToMaxLength()
    {
        var pasted = new string('b', 500);

        var name = HeroChoice.NormalizeName(pasted, HeroAvatar.Max);

        Assert.Equal(HeroChoice.MaxNameLength, name.Length);
    }

    [Fact]
    public void NormalizeName_CutEndingInSpace_IsTrimmedAgain()
    {
        var raw = new string('a', HeroChoice.MaxNameLength - 1) + " tail";

        Assert.Equal(new string('a', HeroChoice.MaxNameLength - 1), HeroChoice.NormalizeName(raw, HeroAvatar.Max));
    }

    [Fact]
    public void NormalizeName_NeverSplitsASurrogatePair()
    {
        var raw = new string('a', HeroChoice.MaxNameLength - 1) + "\U0001F600\U0001F600";

        var name = HeroChoice.NormalizeName(raw, HeroAvatar.Max);

        Assert.Equal(new string('a', HeroChoice.MaxNameLength - 1) + "\U0001F600", name);
        Assert.False(char.IsHighSurrogate(name[^1]));
    }

    [Fact]
    public void NormalizeName_KeepsACombiningSequenceTogether()
    {
        var raw = new string('a', HeroChoice.MaxNameLength - 1) + "éé";

        var name = HeroChoice.NormalizeName(raw, HeroAvatar.Max);

        Assert.EndsWith("é", name);
        Assert.Equal(HeroChoice.MaxNameLength + 1, name.Length);
    }

    [Theory]
    [InlineData(HeroAvatar.Max, "Max")]
    [InlineData(HeroAvatar.Lisa, "Lisa")]
    public void DefaultName_IsTheProductName(HeroAvatar avatar, string expected) =>
        Assert.Equal(expected, HeroChoice.DefaultName(avatar));

    [Fact]
    public void DefaultName_UndefinedAvatar_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => HeroChoice.DefaultName((HeroAvatar)99));

    [Theory]
    [InlineData("Max", "Lisa")]
    [InlineData("", "Lisa")]
    [InlineData(null, "Lisa")]
    [InlineData("   ", "Lisa")]
    [InlineData(" Max ", "Lisa")]
    [InlineData("Zoe", "Zoe")]
    [InlineData("Lisa", "Lisa")]
    public void NameAfterSwitch_FromMaxToLisa_ReplacesOnlyDefaultOrEmptyInput(string? input, string expected) =>
        Assert.Equal(expected, HeroChoice.NameAfterSwitch(input, HeroAvatar.Max, HeroAvatar.Lisa));

    [Fact]
    public void NameAfterSwitch_FromLisaToMax_ReplacesLisaDefault()
    {
        Assert.Equal("Max", HeroChoice.NameAfterSwitch("Lisa", HeroAvatar.Lisa, HeroAvatar.Max));
        Assert.Equal("Zoe", HeroChoice.NameAfterSwitch("Zoe", HeroAvatar.Lisa, HeroAvatar.Max));
    }

    [Fact]
    public void ToString_DoesNotContainTheName()
    {
        var choice = new HeroChoice();
        choice.Choose(HeroAvatar.Lisa, "Zoe", "easy-1");

        Assert.DoesNotContain("Zoe", choice.ToString());
    }
}
