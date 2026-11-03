using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Application;

/// <summary>Outcome of reading a level: either a valid level or at least one error.</summary>
public sealed class LevelReadResult
{
    private LevelReadResult(LevelDefinition? level, IReadOnlyList<LevelValidationError> errors)
    {
        Level = level;
        Errors = errors;
    }

    /// <summary>The valid level, or <see langword="null"/> on failure.</summary>
    public LevelDefinition? Level { get; }

    /// <summary>Empty on success, otherwise at least one error.</summary>
    public IReadOnlyList<LevelValidationError> Errors { get; }

    public bool IsValid => Level is not null;

    public static LevelReadResult Success(LevelDefinition level)
    {
        ArgumentNullException.ThrowIfNull(level);
        return new(level, []);
    }

    public static LevelReadResult Failure(IReadOnlyList<LevelValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (errors.Count == 0)
        {
            throw new ArgumentException("A failure needs at least one error.", nameof(errors));
        }

        return new(null, errors);
    }
}
