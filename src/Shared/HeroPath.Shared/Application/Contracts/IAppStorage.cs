namespace HeroPath.Shared.Application.Contracts;

/// <summary>
/// Key/value storage for settings and custom levels. Each host provides its own implementation
/// (Web: localStorage, MAUI later: Preferences). Never used for the hero's name.
/// Implementations never throw because the storage is unavailable (blocked, full, disconnected): reads return
/// <c>null</c> and writes report <c>false</c>. Only a cancellation requested through the caller's token is propagated.
/// </summary>
public interface IAppStorage
{
    /// <summary>Returns the stored value, or <c>null</c> if there is none or the storage is unavailable.</summary>
    ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken);

    /// <summary>Stores the value. Returns <c>true</c> when it was written, <c>false</c> when the storage is unavailable or full (nothing is stored then).</summary>
    ValueTask<bool> SetAsync(string key, string value, CancellationToken cancellationToken);

    /// <summary>Removes the value. Returns <c>true</c> when it was removed or there was nothing to remove, <c>false</c> when the storage is unavailable.</summary>
    ValueTask<bool> RemoveAsync(string key, CancellationToken cancellationToken);
}
