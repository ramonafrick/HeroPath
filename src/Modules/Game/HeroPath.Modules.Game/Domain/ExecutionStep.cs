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
/// <c>StepNumber</c>: count of successfully executed moves so far (failed moves are not counted, so a failing
/// step carries the number of the last successful one; the failing move is number StepNumber + 1).
/// </para>
/// <para><c>MissingItems</c>: strawberries still missing, only set together with ItemsMissing, otherwise 0.</para>
/// </summary>
public sealed record ExecutionStep(
    Guid CommandId,
    int StepNumber,
    Position Position,
    ExecutionState State,
    ExecutionError? Error = null,
    ItemKind? CollectedItem = null,
    ObstacleKind? HitObstacle = null,
    int MissingItems = 0);
