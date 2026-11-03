using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Application.Contracts;

/// <summary>Persistence of the child's custom levels. The content is untrusted: implementations return only levels that may be played.</summary>
public interface ICustomLevelStore
{
    /// <summary>The stored custom levels in saved order. Broken, manipulated or oversized data yields fewer levels or none, never an exception.</summary>
    Task<IReadOnlyList<LevelDefinition>> LoadAsync(CancellationToken cancellationToken);

    /// <summary>Replaces all stored custom levels in one write. Returns <c>false</c> when nothing could be written (the old state stays).</summary>
    Task<bool> SaveAllAsync(IReadOnlyList<LevelDefinition> levels, CancellationToken cancellationToken);
}
