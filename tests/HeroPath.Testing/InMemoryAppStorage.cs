using HeroPath.Shared.Application.Contracts;

namespace HeroPath.Testing;

public sealed class InMemoryAppStorage : IAppStorage
{
    public Dictionary<string, string> Values { get; } = [];

    /// <summary>When true, writes and removals store nothing and report <c>false</c> (blocked or full storage).</summary>
    public bool FailWrites { get; set; }

    public ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Values.GetValueOrDefault(key));

    public ValueTask<bool> SetAsync(string key, string value, CancellationToken cancellationToken)
    {
        if (FailWrites)
        {
            return ValueTask.FromResult(false);
        }

        Values[key] = value;
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> RemoveAsync(string key, CancellationToken cancellationToken)
    {
        if (FailWrites)
        {
            return ValueTask.FromResult(false);
        }

        Values.Remove(key);
        return ValueTask.FromResult(true);
    }
}
