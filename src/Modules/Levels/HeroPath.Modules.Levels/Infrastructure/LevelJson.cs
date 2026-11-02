using System.Text.Json;
using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Infrastructure;

public static class LevelJson
{
    public static string Write(LevelDefinition level)
    {
        ArgumentNullException.ThrowIfNull(level);
        return JsonSerializer.Serialize(level, LevelJsonContext.Default.LevelDefinition);
    }

    public static LevelReadResult Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Invalid();
        }

        LevelDefinition? level;
        try
        {
            level = JsonSerializer.Deserialize(json, LevelJsonContext.Default.LevelDefinition);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
        {
            return Invalid();
        }

        if (level is null)
        {
            return Invalid();
        }

        var errors = LevelValidator.Validate(level);
        return errors.Count == 0 ? new LevelReadResult(level, errors) : new LevelReadResult(null, errors);
    }

    private static LevelReadResult Invalid() =>
        new(null, [new LevelValidationError(LevelValidationErrorKind.InvalidJson)]);
}
