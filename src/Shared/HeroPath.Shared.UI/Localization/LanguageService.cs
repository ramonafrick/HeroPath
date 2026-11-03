using System.Globalization;
using HeroPath.Shared.Application.Contracts;

namespace HeroPath.Shared.UI.Localization;

/// <summary>
/// Holds the UI language and switches it at runtime without a reload, so hero, program and level survive.
/// Only <c>de-CH</c> and <c>en</c> are supported; anything else falls back to <c>de-CH</c>.
/// A failing storage never breaks the language switch.
/// </summary>
public sealed class LanguageService(IAppStorage storage)
{
    public const string German = "de-CH";
    public const string English = "en";

    public event Action? Changed;

    /// <summary>Name of the active culture, always one of <see cref="German"/> or <see cref="English"/>.</summary>
    public string Current { get; private set; } = German;

    /// <summary>Returns a supported culture name for any input; unknown, empty or garbage values give <see cref="German"/>.</summary>
    public static string Normalize(string? cultureName) =>
        string.Equals(cultureName, English, StringComparison.OrdinalIgnoreCase) ? English : German;

    public async Task ApplyStoredAsync(CancellationToken cancellationToken)
    {
        string? stored = null;
        try
        {
            stored = await storage.GetAsync(StorageKeys.Language, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Storage is optional: without it the default language is used.
        }

        Apply(Normalize(stored));
    }

    public async Task SetAsync(string cultureName, CancellationToken cancellationToken)
    {
        var culture = Normalize(cultureName);
        Apply(culture);

        try
        {
            await storage.SetAsync(StorageKeys.Language, culture, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The language stays switched for this session even if it cannot be remembered.
        }

        Changed?.Invoke();
    }

    private void Apply(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        Current = cultureName;
    }
}
