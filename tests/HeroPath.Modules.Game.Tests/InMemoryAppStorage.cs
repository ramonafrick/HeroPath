using HeroPath.Shared.Application.Contracts;

namespace HeroPath.Modules.Game.Tests;

public sealed class InMemoryAppStorage : IAppStorage
{
    private readonly Dictionary<string, string> _values = [];

    public ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken) =>
        ValueTask.FromResult(_values.GetValueOrDefault(key));

    public ValueTask SetAsync(string key, string value, CancellationToken cancellationToken)
    {
        _values[key] = value;
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken)
    {
        _values.Remove(key);
        return ValueTask.CompletedTask;
    }
}
