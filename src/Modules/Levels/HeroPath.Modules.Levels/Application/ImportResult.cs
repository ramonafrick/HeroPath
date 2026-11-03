using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Application;

/// <summary>Outcome of an import and, when it succeeded, the new custom level (new id, cleaned or default name).</summary>
public sealed record ImportResult(ImportOutcome Outcome, LevelDefinition? Level = null);
