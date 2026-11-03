using HeroPath.Shared.Application.Contracts;

namespace HeroPath.Shared.Tests.Fakes;

public sealed class InMemoryAppStorage : IAppStorage
{
    public Dictionary<string, string> Values { get; } = [];

    public ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Values.GetValueOrDefault(key));

    public ValueTask SetAsync(string key, string value, CancellationToken cancellationToken)
    {
        Values[key] = value;
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken)
    {
        Values.Remove(key);
        return ValueTask.CompletedTask;
    }
}
