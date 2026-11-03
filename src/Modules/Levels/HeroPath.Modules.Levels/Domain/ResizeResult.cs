namespace HeroPath.Modules.Levels.Domain;

/// <summary>Outcome of <c>LevelEditing.Resize</c>: the resized level and how many obstacles and items were removed.</summary>
public sealed record ResizeResult(LevelDefinition Level, int RemovedCount);
