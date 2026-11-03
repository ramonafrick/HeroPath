using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Game.Application;

/// <summary>
/// The ordered levels and which of them the child has solved in this session. Memory only: nothing is stored, so a reload starts fresh.
/// <see cref="ExecutionRunner"/> marks a level when a run succeeds.
/// </summary>
public sealed class LevelProgress
{
    private readonly HashSet<string> _solved = [];

    public LevelProgress(IReadOnlyList<LevelDefinition> levels)
    {
        ArgumentNullException.ThrowIfNull(levels);
        Levels = [.. levels];
    }

    /// <summary>Raised after a level was marked solved for the first time.</summary>
    public event Action? Changed;

    /// <summary>The levels in play order.</summary>
    public IReadOnlyList<LevelDefinition> Levels { get; }

    /// <summary>Whether every level is solved.</summary>
    public bool AllSolved => Levels.Count > 0 && Levels.All(level => _solved.Contains(level.Id));

    public bool IsSolved(string id) => _solved.Contains(id);

    /// <summary>The level after <paramref name="id"/>, or <see langword="null"/> for the last or an unknown level.</summary>
    public LevelDefinition? NextAfter(string id)
    {
        for (var i = 0; i < Levels.Count - 1; i++)
        {
            if (Levels[i].Id == id)
            {
                return Levels[i + 1];
            }
        }

        return null;
    }

    /// <summary>Marks a level solved. Unknown ids and repeated calls change nothing and raise no event.</summary>
    public void MarkSolved(string id)
    {
        if (Levels.Any(level => level.Id == id) && _solved.Add(id))
        {
            Changed?.Invoke();
        }
    }
}
