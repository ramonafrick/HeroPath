using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Game.Domain;

/// <summary>Outcome of a single move attempt.</summary>
public enum MoveOutcome
{
    Moved,
    Blocked,
    OutOfBounds,
}

/// <summary>Result of <see cref="Person.Move"/>. <c>NewPosition</c> equals the old position when the move failed; <c>Hit</c> is set only for Blocked.</summary>
public sealed record MoveResult(MoveOutcome Outcome, Position NewPosition, ObstacleKind? Hit = null);
