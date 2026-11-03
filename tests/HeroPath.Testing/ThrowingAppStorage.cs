using HeroPath.Shared.Application.Contracts;

namespace HeroPath.Testing;

public sealed class ThrowingAppStorage : IAppStorage
{
    public ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Storage is blocked.");

    public ValueTask SetAsync(string key, string value, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Storage is blocked.");

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Storage is blocked.");
}
