namespace HeroPath.Modules.Levels.Application;

/// <summary>Validation errors, solvability status and the length of the shortest solution (when there is one).</summary>
public sealed record LevelCheckResult(IReadOnlyList<LevelValidationError> Errors, LevelCheckStatus Status, int? MinimumSteps)
{
    /// <summary>A level may be saved only when it is valid and solvable within the step limit.</summary>
    public bool CanSave => Status == LevelCheckStatus.Valid;
}
