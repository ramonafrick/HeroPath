using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Application;

/// <summary>
/// <see cref="LevelCheck"/> for a level that changes often (the editor): the search only runs when the grid differs from the last one,
/// so typing a name re-validates but does not solve again. Not thread safe.
/// </summary>
internal sealed class LevelCheckCache
{
    private Grid? _grid;
    private LevelCheck.SolverOutcome _outcome;

    public LevelCheckResult Evaluate(LevelDefinition level) => LevelCheck.Evaluate(level, LevelLimits.MaxSteps, Solve);

    private LevelCheck.SolverOutcome Solve(Grid grid)
    {
        if (!grid.Equals(_grid))
        {
            _outcome = LevelCheck.Solve(grid);
            _grid = grid;
        }

        return _outcome;
    }
}
