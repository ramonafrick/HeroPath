using System.Text.RegularExpressions;

namespace HeroPath.ArchitectureTests;

/// <summary>
/// Keeps the Claude workflow in .claude/ in step with the code: a skill that no longer matches the code
/// leads Claude to wrong code, so a stale skill fails the build like a broken dependency rule.
/// Reads source and Markdown files from the repository, nothing is loaded or compiled.
/// </summary>
public partial class ClaudeWorkflowTests
{
    private static readonly string _root = FindRepositoryRoot();

    public static TheoryData<string, string> DomainSkills => new()
    {
        { "src/Modules/Levels/HeroPath.Modules.Levels", "game-rules" },
        { "src/Modules/Game/HeroPath.Modules.Game", "game-rules" },
    };

    [Theory]
    [MemberData(nameof(DomainSkills))]
    public void EveryPublicType_IsNamedInSkill(string projectDirectory, string skill)
    {
        var skillText = File.ReadAllText(SkillPath(skill));

        var missing = Directory.EnumerateFiles(Path.Combine(_root, projectDirectory), "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path))
            .SelectMany(path => PublicTypeRegex().Matches(File.ReadAllText(path)).Select(match => match.Groups["name"].Value))
            .Where(type => !Regex.IsMatch(skillText, $@"`[^`\n]*\b{type}\b[^`\n]*`"))
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(missing.Count == 0, $"Add to .claude/skills/{skill}/SKILL.md: {string.Join(", ", missing)}");
    }

    [Fact]
    public void EverySkill_HasMatchingNameAndIsListedInClaudeMd()
    {
        var claudeMd = File.ReadAllText(Path.Combine(_root, "CLAUDE.md"));

        foreach (var directory in Directory.EnumerateDirectories(Path.Combine(_root, ".claude", "skills")))
        {
            var skill = Path.GetFileName(directory);
            Assert.Equal(skill, FrontmatterName(Path.Combine(directory, "SKILL.md")));
            Assert.Contains($"`{skill}`", claudeMd, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void EveryAgent_HasMatchingName()
    {
        foreach (var path in Directory.EnumerateFiles(Path.Combine(_root, ".claude", "agents"), "*.md"))
        {
            Assert.Equal(Path.GetFileNameWithoutExtension(path), FrontmatterName(path));
        }
    }

    [Fact]
    public void PublicTypeRegex_FindsTypeDeclarations()
    {
        const string source = """
            public sealed record ExecutionStep(int StepNumber);
            public static partial class LevelValidator
            internal sealed class Hidden
            """;

        Assert.Equal(["ExecutionStep", "LevelValidator"], PublicTypeRegex().Matches(source).Select(match => match.Groups["name"].Value));
    }

    private static string SkillPath(string skill) => Path.Combine(_root, ".claude", "skills", skill, "SKILL.md");

    private static string FrontmatterName(string path) =>
        File.ReadLines(path).Skip(1).TakeWhile(line => line != "---")
            .Select(line => FrontmatterNameRegex().Match(line))
            .First(match => match.Success).Groups["name"].Value.Trim();

    private static bool IsBuildOutput(string path)
    {
        var separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        return path.Split(separators).Any(part => part is "bin" or "obj");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "HeroPath.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("HeroPath.slnx not found above " + AppContext.BaseDirectory);
    }

    [GeneratedRegex(@"^\s*public\s+(?:(?:sealed|abstract|static|readonly|partial)\s+)*(?:record\s+struct|record|class|struct|enum|interface)\s+(?<name>\w+)", RegexOptions.Multiline)]
    private static partial Regex PublicTypeRegex();

    [GeneratedRegex(@"^name:\s*(?<name>.+)$")]
    private static partial Regex FrontmatterNameRegex();
}
