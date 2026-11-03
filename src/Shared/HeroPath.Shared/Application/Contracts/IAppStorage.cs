namespace HeroPath.Shared.Application.Contracts;

/// <summary>
/// Key/value storage for settings and custom levels. Each host provides its own implementation
/// (Web: localStorage, MAUI later: Preferences). Never used for the hero's name.
/// Implementations never throw because the storage is unavailable (blocked, full, disconnected): reads return
/// <c>null</c> and writes are dropped. Only a cancellation requested through the caller's token is propagated.
/// </summary>
public interface IAppStorage
{
    /// <summary>Returns the stored value, or <c>null</c> if there is none or the storage is unavailable.</summary>
    ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken);

    /// <summary>Stores the value; silently does nothing if the storage is unavailable.</summary>
    ValueTask SetAsync(string key, string value, CancellationToken cancellationToken);

    /// <summary>Removes the value; silently does nothing if there is none or the storage is unavailable.</summary>
    ValueTask RemoveAsync(string key, CancellationToken cancellationToken);
}
