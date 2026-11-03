using System.Text.Json;
using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Application.Contracts;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Shared.Application.Contracts;

namespace HeroPath.Modules.Levels.Infrastructure;

/// <summary>
/// Keeps all custom levels as one JSON array under <see cref="StorageKeys.CustomLevels"/>, so every change is a single write.
/// What is read back is untrusted: each element goes through <see cref="LevelJson.Read"/> and <see cref="LevelCheck"/>, anything broken,
/// manipulated, unsolvable, nameless, duplicated or beyond <see cref="LevelLimits.MaxCustomLevels"/> is skipped. Loading never throws.
/// </summary>
public sealed class StorageCustomLevelStore(IAppStorage storage) : ICustomLevelStore
{
    private const int MaxLevelDepth = 16;

    public async Task<IReadOnlyList<LevelDefinition>> LoadAsync(CancellationToken cancellationToken)
    {
        var json = await storage.GetAsync(StorageKeys.CustomLevels, cancellationToken);
        return Parse(json);
    }

    public async Task<bool> SaveAllAsync(IReadOnlyList<LevelDefinition> levels, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(levels);
        return await storage.SetAsync(StorageKeys.CustomLevels, LevelJson.WriteAll(levels), cancellationToken);
    }

    private static List<LevelDefinition> Parse(string? json)
    {
        List<LevelDefinition> levels = [];
        if (string.IsNullOrWhiteSpace(json) || json.Length > (LevelLimits.MaxCustomLevels + 1) * LevelJson.MaxJsonLength)
        {
            return levels;
        }

        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = MaxLevelDepth });
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return levels;
            }

            HashSet<string> ids = [];
            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (levels.Count >= LevelLimits.MaxCustomLevels)
                {
                    break;
                }

                if (LevelJson.Read(element.GetRawText()).Level is { Name: not null } level
                    && LevelCheck.Evaluate(level).CanSave
                    && ids.Add(level.Id))
                {
                    levels.Add(level);
                }
            }
        }
        catch (JsonException)
        {
            // Not JSON at all (or too deep): no custom levels; the next save replaces it.
        }

        return levels;
    }
}
