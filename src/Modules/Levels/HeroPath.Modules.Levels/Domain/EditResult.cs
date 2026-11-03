namespace HeroPath.Modules.Levels.Domain;

/// <summary>Outcome of <c>LevelEditing.Apply</c>: the (possibly unchanged) level and a hint when the click was refused.</summary>
public sealed record EditResult(LevelDefinition Level, EditHint Hint = EditHint.None);
