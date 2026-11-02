using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace HeroPath.ArchitectureTests;

/// <summary>
/// Guards the module and layer boundaries from docs/Architektur C4.md.
/// Assembly references only contain assemblies that are actually used, so these rules catch real dependencies.
/// The DLLs are read as metadata instead of being loaded, which also works when Windows Application Control blocks loading.
/// </summary>
public class DependencyRuleTests
{
    private static readonly string[] _platform = ["Microsoft.AspNetCore", "Microsoft.JSInterop"];

    public static TheoryData<string, string[]> Rules => new()
    {
        { "HeroPath.Shared", [.. _platform, "HeroPath.Shared.UI", "HeroPath.Modules"] },
        { "HeroPath.Shared.UI", ["HeroPath.Modules"] },
        { "HeroPath.Modules.Levels", [.. _platform, "HeroPath.Shared.UI", "HeroPath.Modules.Levels.UI", "HeroPath.Modules.Game"] },
        { "HeroPath.Modules.Levels.UI", ["HeroPath.Modules.Game"] },
        { "HeroPath.Modules.Game", [.. _platform, "HeroPath.Shared.UI", "HeroPath.Modules.Levels.UI", "HeroPath.Modules.Game.UI"] },
        { "HeroPath.Modules.Game.UI", [] },
    };

    [Theory]
    [MemberData(nameof(Rules))]
    public void Assembly_DoesNotReference_ForbiddenAssemblies(string assemblyName, string[] forbiddenPrefixes)
    {
        string[] forbidden = [.. forbiddenPrefixes, "HeroPath.Web", "HeroPath.Maui"];

        var violations = ReadAssemblyReferences(assemblyName)
            .Where(name => forbidden.Any(prefix => name == prefix || name.StartsWith(prefix + ".", StringComparison.Ordinal)))
            .ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void ReadAssemblyReferences_DetectsRealDependencies()
    {
        Assert.Contains("Microsoft.AspNetCore.Components", ReadAssemblyReferences("HeroPath.Modules.Game.UI"));
    }

    private static List<string> ReadAssemblyReferences(string assemblyName)
    {
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, assemblyName + ".dll"));
        using var peReader = new PEReader(stream);
        var metadata = peReader.GetMetadataReader();

        return [.. metadata.AssemblyReferences.Select(handle => metadata.GetString(metadata.GetAssemblyReference(handle).Name))];
    }
}
