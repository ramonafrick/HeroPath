using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Application;

public sealed record LevelValidationError(LevelValidationErrorKind Kind, Position? Position = null);
