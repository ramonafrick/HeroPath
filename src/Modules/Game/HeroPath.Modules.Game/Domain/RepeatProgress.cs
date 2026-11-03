namespace HeroPath.Modules.Game.Domain;

/// <summary>Progress of one repeat block while a move inside it runs. <c>Round</c> is 1-based, <c>Count</c> the total number of rounds.</summary>
public sealed record RepeatProgress(Guid RepeatId, int Round, int Count);
