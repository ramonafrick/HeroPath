using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Game.Application;

/// <summary>
/// The levels the child can play (standard levels first, then custom ones) and which of them are solved in this session. Memory only:
/// nothing is stored, so a reload starts fresh. The levels are read through the <see cref="LevelRepository"/> every time, so later
/// additions and deletions show up. <see cref="NextAfter"/> and <see cref="AllSolved"/> only concern the standard levels.
/// <see cref="ExecutionRunner"/> marks a level when a run succeeds.
/// </summary>
public sealed class LevelProgress
{
    private readonly LevelRepository _repository;
    private readonly HashSet<string> _solved = [];

    public LevelProgress(LevelRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <summary>Raised after a level was marked solved for the first time.</summary>
    public event Action? Changed;

    /// <summary>All playable levels in order: standard levels, then custom levels.</summary>
    public IReadOnlyList<LevelDefinition> Levels => _repository.All;

    /// <summary>Whether every standard level is solved (false when there are none). Custom levels do not count.</summary>
    public bool AllSolved => _repository.Standard.Count > 0 && _repository.Standard.All(level => _solved.Contains(level.Id));

    /// <summary>The level with <paramref name="id"/> (standard or custom), or <see langword="null"/> when there is none.</summary>
    public LevelDefinition? Find(string id) => _repository.Find(id);

    public bool IsSolved(string id) => _solved.Contains(id);

    /// <summary>The standard level after <paramref name="id"/>, or <see langword="null"/> for the last standard level, a custom level or an unknown id.</summary>
    public LevelDefinition? NextAfter(string id)
    {
        var standard = _repository.Standard;
        for (var i = 0; i < standard.Count - 1; i++)
        {
            if (standard[i].Id == id)
            {
                return standard[i + 1];
            }
        }

        return null;
    }

    /// <summary>Marks a level solved. Unknown ids and repeated calls change nothing and raise no event.</summary>
    public void MarkSolved(string id)
    {
        if (Find(id) is not null && _solved.Add(id))
        {
            Changed?.Invoke();
        }
    }
}
