using System.Text.Json;
using System.Text.Json.Serialization;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Infrastructure;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    MaxDepth = 8,
    Converters = [typeof(StringOnlyEnumConverter<ObstacleKind>), typeof(StringOnlyEnumConverter<ItemKind>)])]
[JsonSerializable(typeof(LevelDefinition))]
internal sealed partial class LevelJsonContext : JsonSerializerContext;

internal sealed class StringOnlyEnumConverter<T>()
    : JsonStringEnumConverter<T>(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
    where T : struct, Enum;
