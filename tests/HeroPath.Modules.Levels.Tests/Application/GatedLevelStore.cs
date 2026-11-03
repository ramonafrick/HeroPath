using HeroPath.Modules.Levels.Application.Contracts;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Tests.Application;

/// <summary>Store whose first write waits until the test opens the gate, so a second operation can start while it is in flight.</summary>
internal sealed class GatedLevelStore(IReadOnlyList<LevelDefinition>? initial = null) : ICustomLevelStore
{
    private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _writes;

    public IReadOnlyList<LevelDefinition> Stored { get; private set; } = initial ?? [];

    public int Loads { get; private set; }

    public Task<IReadOnlyList<LevelDefinition>> LoadAsync(CancellationToken cancellationToken)
    {
        Loads++;
        return Task.FromResult(Stored);
    }

    public async Task<bool> SaveAllAsync(IReadOnlyList<LevelDefinition> levels, CancellationToken cancellationToken)
    {
        if (_writes++ == 0)
        {
            await _gate.Task;
        }

        Stored = levels;
        return true;
    }

    public void OpenGate() => _gate.TrySetResult();
}
