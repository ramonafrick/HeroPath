using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Application;

/// <summary>A single rule violation; <see cref="Position"/> is set when it concerns one cell.</summary>
public sealed record LevelValidationError(LevelValidationErrorKind Kind, Position? Position = null);
