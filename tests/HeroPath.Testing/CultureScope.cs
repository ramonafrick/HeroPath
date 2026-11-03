using System.Globalization;

namespace HeroPath.Testing;

/// <summary>
/// Cultures are process-wide state; this restores them after a test. Tests that assert localized text pass the
/// culture explicitly and run in <c>[Collection("Culture")]</c> so they never overlap.
/// </summary>
public sealed class CultureScope : IDisposable
{
    private readonly CultureInfo? _defaultCulture = CultureInfo.DefaultThreadCurrentCulture;
    private readonly CultureInfo? _defaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;
    private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;

    /// <summary>Keeps the current culture and only restores it on dispose.</summary>
    public CultureScope()
    {
    }

    /// <summary>Switches all culture settings to <paramref name="cultureName"/> until disposed.</summary>
    public CultureScope(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public void Dispose()
    {
        CultureInfo.DefaultThreadCurrentCulture = _defaultCulture;
        CultureInfo.DefaultThreadCurrentUICulture = _defaultUiCulture;
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _uiCulture;
    }
}
