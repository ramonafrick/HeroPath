using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Application;

/// <summary>Decides whether a level can be saved: valid, solvable and solvable within the step limit.</summary>
public static class LevelCheck
{
    public static LevelCheckResult Evaluate(LevelDefinition level) => Evaluate(level, LevelLimits.MaxSteps);

    /// <summary>Same check with a custom step limit (the standard limit is practically out of reach on a 20x20 board, so tests lower it).</summary>
    public static LevelCheckResult Evaluate(LevelDefinition level, int maxSteps) => Evaluate(level, maxSteps, Solve);

    internal static LevelCheckResult Evaluate(LevelDefinition level, int maxSteps, Func<Grid, SolverOutcome> solve)
    {
        ArgumentNullException.ThrowIfNull(level);
        var errors = LevelValidator.Validate(level);
        if (errors.Count > 0)
        {
            return new(errors, LevelCheckStatus.Invalid, null);
        }

        var outcome = solve(level.ToGrid());
        var status = outcome.Steps switch
        {
            null when !outcome.GoalReachable => LevelCheckStatus.GoalUnreachable,
            null => LevelCheckStatus.StrawberriesUnreachable,
            { } count when count > maxSteps => LevelCheckStatus.TooManySteps,
            _ => LevelCheckStatus.Valid,
        };
        return new(errors, status, outcome.Steps);
    }

    internal static SolverOutcome Solve(Grid grid)
    {
        var steps = LevelSolver.ShortestSolution(grid);
        return new(steps, steps is not null || LevelSolver.CanReachGoal(grid));
    }

    internal readonly record struct SolverOutcome(int? Steps, bool GoalReachable);
}
