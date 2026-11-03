using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Tests.Domain;

public class ExportFileNameTests
{
    [Theory]
    [InlineData("Mein Level 1", "mein-level-1.heropath.json")]
    [InlineData("Käse Öl Übung", "kaese-oel-uebung.heropath.json")]
    [InlineData("KÄSE ÖL ÜBUNG", "kaese-oel-uebung.heropath.json")]
    [InlineData("Strasse Straße", "strasse-strasse.heropath.json")]
    [InlineData("Café Señor", "cafe-senor.heropath.json")]
    [InlineData("  --Hallo---Welt--  ", "hallo-welt.heropath.json")]
    [InlineData("a_b.c", "a-b-c.heropath.json")]
    public void ExportFileName_Names_AreMappedToLowercaseAsciiWithHyphens(string name, string expected) =>
        Assert.Equal(expected, LevelEditing.ExportFileName(name));

    [Theory]
    [InlineData("<script>alert(1)</script>", "script-alert-1-script.heropath.json")]
    [InlineData("<b>fett</b>", "b-fett-b.heropath.json")]
    [InlineData("../../etc/passwd", "etc-passwd.heropath.json")]
    [InlineData("..\\..\\windows\\system32", "windows-system32.heropath.json")]
    [InlineData("C:\\temp\\x", "c-temp-x.heropath.json")]
    public void ExportFileName_MarkupAndPaths_NeverKeepSeparatorsOrBrackets(string name, string expected)
    {
        var result = LevelEditing.ExportFileName(name);

        Assert.Equal(expected, result);
        Assert.DoesNotContain('/', result);
        Assert.DoesNotContain('\\', result);
        Assert.DoesNotContain('<', result);
    }

    [Theory]
    [InlineData("con", "level-con.heropath.json")]
    [InlineData("CON", "level-con.heropath.json")]
    [InlineData("Prn", "level-prn.heropath.json")]
    [InlineData("aux", "level-aux.heropath.json")]
    [InlineData("NUL", "level-nul.heropath.json")]
    [InlineData("com1", "level-com1.heropath.json")]
    [InlineData("COM9", "level-com9.heropath.json")]
    [InlineData("lpt1", "level-lpt1.heropath.json")]
    [InlineData("LPT9", "level-lpt9.heropath.json")]
    [InlineData("  con  ", "level-con.heropath.json")]
    public void ExportFileName_ReservedWindowsDeviceName_GetsPrefix(string name, string expected) =>
        Assert.Equal(expected, LevelEditing.ExportFileName(name));

    [Theory]
    [InlineData("com0", "com0.heropath.json")]
    [InlineData("com10", "com10.heropath.json")]
    [InlineData("lpt", "lpt.heropath.json")]
    [InlineData("console", "console.heropath.json")]
    [InlineData("con 1", "con-1.heropath.json")]
    [InlineData("nul level", "nul-level.heropath.json")]
    public void ExportFileName_NamesThatOnlyLookReserved_AreUnchanged(string name, string expected) =>
        Assert.Equal(expected, LevelEditing.ExportFileName(name));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!! ???")]
    [InlineData("\U0001F600\U0001F600")]
    [InlineData("---")]
    public void ExportFileName_NothingUsable_FallsBackToLevel(string? name) =>
        Assert.Equal("level.heropath.json", LevelEditing.ExportFileName(name));

    [Fact]
    public void ExportFileName_EmojiBetweenWords_IsASingleHyphen() =>
        Assert.Equal("a-b.heropath.json", LevelEditing.ExportFileName("a\U0001F600\U0001F600b"));

    [Fact]
    public void ExportFileName_LongName_IsCutAtFortyCharactersWithoutTrailingHyphen()
    {
        var name = new string('a', 39) + " " + new string('b', 20);

        Assert.Equal(new string('a', 39) + ".heropath.json", LevelEditing.ExportFileName(name));
        Assert.Equal(
            LevelEditing.MaxExportBaseLength + LevelEditing.ExportExtension.Length,
            LevelEditing.ExportFileName(new string('x', 100)).Length);
    }

    [Theory]
    [InlineData("Ä")]
    [InlineData("ß")]
    [InlineData("x y")]
    [InlineData("\u0301")]
    [InlineData("a\u0308")]
    [InlineData("İ")]
    [InlineData("ǅ")]
    public void ExportFileName_AnyInput_MatchesTheSafePattern(string name) =>
        Assert.Matches("^[a-z0-9]+(-[a-z0-9]+)*\\.heropath\\.json$", LevelEditing.ExportFileName(name));
}
