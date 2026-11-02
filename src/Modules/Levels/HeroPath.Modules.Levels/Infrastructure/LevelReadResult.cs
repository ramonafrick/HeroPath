using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Infrastructure;

public sealed record LevelReadResult(LevelDefinition? Level, IReadOnlyList<LevelValidationError> Errors)
{
    public bool IsValid => Level is not null && Errors.Count == 0;
}
