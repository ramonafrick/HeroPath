using System.Text.Json;
using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Infrastructure;

/// <summary>Reads and writes level JSON (schema in the game-rules skill).</summary>
public static class LevelJson
{
    /// <summary>Longest accepted input in characters; longer input is rejected before parsing.</summary>
    public const int MaxJsonLength = 64 * 1024;

    public static string Write(LevelDefinition level)
    {
        ArgumentNullException.ThrowIfNull(level);
        return JsonSerializer.Serialize(level, LevelJsonContext.Default.LevelDefinition);
    }

    /// <summary>Writes a JSON array of levels, compact, for the storage. Each element has the format of <see cref="Write"/>.</summary>
    public static string WriteAll(IReadOnlyList<LevelDefinition> levels)
    {
        ArgumentNullException.ThrowIfNull(levels);
        return JsonSerializer.Serialize([.. levels], LevelListJsonContext.Default.ListLevelDefinition);
    }

    /// <summary>
    /// Parses and validates a level. Never throws: malformed, oversized or too deeply nested input
    /// yields <see cref="LevelValidationErrorKind.InvalidJson"/>, rule violations yield the validator's errors.
    /// </summary>
    public static LevelReadResult Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Length > MaxJsonLength)
        {
            return Invalid();
        }

        // System.Text.Json throws only these types on bad input. LevelValidator never throws for a
        // non-null level, so it adds no further exception types.
        try
        {
            var level = JsonSerializer.Deserialize(json, LevelJsonContext.Default.LevelDefinition);
            if (level is null)
            {
                return Invalid();
            }

            var errors = LevelValidator.Validate(level);
            return errors.Count == 0 ? LevelReadResult.Success(level) : LevelReadResult.Failure(errors);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
        {
            return Invalid();
        }
    }

    private static LevelReadResult Invalid() =>
        LevelReadResult.Failure([new LevelValidationError(LevelValidationErrorKind.InvalidJson)]);
}
