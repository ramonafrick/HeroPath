using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Game.Domain;

public enum MoveOutcome
{
    Moved,
    Blocked,
    OutOfBounds,
}

public sealed record MoveResult(MoveOutcome Outcome, Position NewPosition, ObstacleKind? Hit = null);
