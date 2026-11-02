namespace HeroPath.Shared.Application.Contracts;

/// <summary>
/// Key/value storage for settings and custom levels. Each host provides its own implementation
/// (Web: localStorage, MAUI later: Preferences). Never used for the hero's name.
/// </summary>
public interface IAppStorage
{
    ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken);

    ValueTask SetAsync(string key, string value, CancellationToken cancellationToken);

    ValueTask RemoveAsync(string key, CancellationToken cancellationToken);
}
