using System.Reflection;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Infrastructure;

/// <summary>
/// The levels shipped with the app, read from the embedded <c>Infrastructure/Levels/&lt;nn&gt;-&lt;id&gt;.json</c>
/// files and ordered by file name. An invalid embedded level is a programming error and throws on first access.
/// </summary>
public static class StandardLevels
{
    private const string ResourceSuffix = ".json";

    private static readonly Lazy<IReadOnlyList<LevelDefinition>> _all = new(Load);

    /// <summary>All standard levels, ordered by file name.</summary>
    public static IReadOnlyList<LevelDefinition> All => _all.Value;

    /// <summary>Returns the standard level with the given id; throws <see cref="KeyNotFoundException"/> if there is none.</summary>
    public static LevelDefinition Get(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return All.FirstOrDefault(level => level.Id == id)
            ?? throw new KeyNotFoundException($"There is no standard level with id '{id}'.");
    }

    private static List<LevelDefinition> Load()
    {
        var assembly = typeof(StandardLevels).Assembly;
        var resourcePrefix = $"{typeof(StandardLevels).Namespace}.Levels.";

        return assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(resourcePrefix, StringComparison.Ordinal) && name.EndsWith(ResourceSuffix, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .Select(name => ReadLevel(assembly, name))
            .ToList();
    }

    private static LevelDefinition ReadLevel(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded level '{resourceName}' could not be opened.");
        using var reader = new StreamReader(stream);
        var result = LevelJson.Read(reader.ReadToEnd());
        return result.Level
            ?? throw new InvalidOperationException(
                $"Embedded level '{resourceName}' is invalid: {string.Join(", ", result.Errors.Select(error => error.Kind))}.");
    }
}
