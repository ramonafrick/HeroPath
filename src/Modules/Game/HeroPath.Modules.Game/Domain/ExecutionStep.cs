using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Game.Domain;

/// <summary>
/// Result of one <see cref="Interpreter.Step"/>.
/// <para>
/// <c>CommandId</c>: the MoveCommand that just ran or caused the failure (the attempted move for
/// Obstacle, OutOfBounds and StepLimitExceeded); the empty RepeatCommand for EmptyRepeat;
/// <see cref="Guid.Empty"/> for EmptyProgram; the last executed move for GoalNotReached and ItemsMissing
/// (the end of the program is detected in the same step as the last move).
/// </para>
/// <para>
/// <c>StepNumber</c>: for Obstacle, OutOfBounds and StepLimitExceeded the number of the attempted move (executed
/// moves + 1), matching the highlighted CommandId; for running steps, GoalNotReached, ItemsMissing and Succeeded the
/// number of the last executed move; 0 for EmptyProgram and EmptyRepeat.
/// </para>
/// <para><c>MissingItems</c>: strawberries still missing, only set together with ItemsMissing, otherwise 0.</para>
/// <para>
/// <c>Repeats</c>: the repeat blocks enclosing the move described by <c>CommandId</c>, outermost first, each with its
/// 1-based round; empty outside repeats and for EmptyProgram and EmptyRepeat. For Obstacle, OutOfBounds and
/// StepLimitExceeded this is the attempted move, otherwise the last executed move. Record equality compares
/// <c>Repeats</c> by reference, not by content.
/// </para>
/// </summary>
public sealed record ExecutionStep(
    Guid CommandId,
    int StepNumber,
    Position Position,
    ExecutionState State,
    ExecutionError? Error = null,
    ItemKind? CollectedItem = null,
    ObstacleKind? HitObstacle = null,
    int MissingItems = 0)
{
    /// <summary>The repeat blocks enclosing the move of <see cref="CommandId"/>, outermost first. See the type remarks.</summary>
    public IReadOnlyList<RepeatProgress> Repeats { get; init; } = [];
}
