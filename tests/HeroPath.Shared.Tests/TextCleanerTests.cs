using HeroPath.Shared.Application;

namespace HeroPath.Shared.Tests;

public class TextCleanerTests
{
    private const char ZeroWidthSpace = (char)0x200B;
    private const char ZeroWidthJoiner = (char)0x200D;
    private const char ByteOrderMark = (char)0xFEFF;
    private const char RightToLeftOverride = (char)0x202E;
    private const char LeftToRightIsolate = (char)0x2066;
    private const char PopDirectionalIsolate = (char)0x2069;
    private const char LineSeparator = (char)0x2028;
    private const char ParagraphSeparator = (char)0x2029;
    private const char CombiningAcute = (char)0x0301;

    public static TheoryData<string> EmptyResults =>
    [
        "",
        "   ",
        "\t\n",
        $"{ZeroWidthSpace}{ZeroWidthJoiner}{ByteOrderMark}",
        $"{RightToLeftOverride}{LeftToRightIsolate}{PopDirectionalIsolate}",
        $" {LineSeparator}{ParagraphSeparator} {ZeroWidthSpace} ",
    ];

    [Theory]
    [MemberData(nameof(EmptyResults))]
    public void Clean_NothingLeft_ReturnsEmptyString(string raw) =>
        Assert.Equal(string.Empty, TextCleaner.Clean(raw, 20));

    [Theory]
    [InlineData("  Max  ", "Max")]
    [InlineData("Ma\0x\n", "Max")]
    public void Clean_RemovesControlCharactersAndTrims(string raw, string expected) =>
        Assert.Equal(expected, TextCleaner.Clean(raw, 20));

    [Fact]
    public void Clean_Null_ReturnsEmptyString() =>
        Assert.Equal(string.Empty, TextCleaner.Clean(null, 20));

    [Fact]
    public void Clean_RemovesInvisibleFormatCharacters()
    {
        Assert.Equal("Max", TextCleaner.Clean($"Ma{ZeroWidthSpace}x", 20));
        Assert.Equal("Max", TextCleaner.Clean($"{RightToLeftOverride}Max", 20));
    }

    [Fact]
    public void Clean_LongerThanMax_IsCutToMaxTextElements() =>
        Assert.Equal(new string('a', 5), TextCleaner.Clean(new string('a', 50), 5));

    [Fact]
    public void Clean_CutEndingInSpace_IsTrimmedAgain() =>
        Assert.Equal("abc", TextCleaner.Clean("abc def", 4));

    [Fact]
    public void Clean_DoesNotSplitSurrogatePairOrCombiningSequence()
    {
        var emoji = char.ConvertFromUtf32(0x1F600);

        Assert.Equal("a" + emoji, TextCleaner.Clean("a" + emoji + emoji, 2));
        Assert.Equal($"e{CombiningAcute}x", TextCleaner.Clean($"e{CombiningAcute}xyz", 2));
    }

    [Fact]
    public void Clean_NegativeMax_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => TextCleaner.Clean("a", -1));
}
