using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Application;

/// <summary>Decides whether a level can be saved: valid, solvable and solvable within the step limit.</summary>
public static class LevelCheck
{
    public static LevelCheckResult Evaluate(LevelDefinition level) => Evaluate(level, LevelLimits.MaxSteps);

    /// <summary>Same check with a custom step limit (the standard limit is practically out of reach on a 20x20 board, so tests lower it).</summary>
    public static LevelCheckResult Evaluate(LevelDefinition level, int maxSteps)
    {
        ArgumentNullException.ThrowIfNull(level);
        var errors = LevelValidator.Validate(level);
        if (errors.Count > 0)
        {
            return new(errors, LevelCheckStatus.Invalid, null);
        }

        var grid = level.ToGrid();
        var steps = LevelSolver.ShortestSolution(grid);
        var status = steps switch
        {
            null when !LevelSolver.CanReachGoal(grid) => LevelCheckStatus.GoalUnreachable,
            null => LevelCheckStatus.StrawberriesUnreachable,
            { } count when count > maxSteps => LevelCheckStatus.TooManySteps,
            _ => LevelCheckStatus.Valid,
        };
        return new(errors, status, steps);
    }
}
