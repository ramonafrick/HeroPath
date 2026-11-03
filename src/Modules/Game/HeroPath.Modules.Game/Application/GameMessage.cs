using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Game.Application;

/// <summary>
/// What the child is told, as values only (no text, no hero name): the <see cref="Kind"/>, the <see cref="Code"/> and the numbers
/// the text needs. <see cref="BlockPath"/> is the hierarchical number of the block to look at (for example <c>[3, 2]</c> for 3.2),
/// <see cref="Obstacle"/> the kind of obstacle that was hit, <see cref="Count"/> the number of missing strawberries,
/// <see cref="PizzaCount"/> the number of pizza slices collected in the run (only set for <see cref="GameMessageCode.Succeeded"/>).
/// </summary>
public sealed record GameMessage(
    GameMessageKind Kind,
    GameMessageCode Code,
    IReadOnlyList<int>? BlockPath = null,
    ObstacleKind? Obstacle = null,
    int Count = 0,
    int PizzaCount = 0)
{
    internal static GameMessage Hint(GameMessageCode code, IReadOnlyList<int>? blockPath = null) =>
        new(GameMessageKind.Hint, code, blockPath);

    internal static GameMessage? FromStep(ExecutionStep step, ProgramDefinition program, int pizzaCount = 0)
    {
        var path = step.CommandId == Guid.Empty ? null : program.PathOf(step.CommandId);
        return step switch
        {
            { State: ExecutionState.Succeeded } => new(GameMessageKind.Success, GameMessageCode.Succeeded, PizzaCount: pizzaCount),
            { Error: ExecutionError.Obstacle } => new(GameMessageKind.Error, GameMessageCode.Obstacle, path, step.HitObstacle),
            { Error: ExecutionError.OutOfBounds } => new(GameMessageKind.Error, GameMessageCode.OutOfBounds, path),
            { Error: ExecutionError.StepLimitExceeded } => new(GameMessageKind.Error, GameMessageCode.StepLimitExceeded, path),
            { Error: ExecutionError.GoalNotReached } => new(GameMessageKind.Error, GameMessageCode.GoalNotReached),
            { Error: ExecutionError.ItemsMissing } => new(GameMessageKind.Error, GameMessageCode.ItemsMissing, Count: step.MissingItems),
            { Error: ExecutionError.EmptyRepeat } => Hint(GameMessageCode.EmptyRepeat, path),
            { Error: ExecutionError.EmptyProgram } => Hint(GameMessageCode.EmptyProgram),
            _ => null,
        };
    }
}
