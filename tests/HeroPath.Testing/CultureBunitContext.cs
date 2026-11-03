using Bunit;

namespace HeroPath.Testing;

/// <summary>
/// bUnit context for tests that assert localized text: the culture is set explicitly (default <c>de-CH</c>) and restored
/// on dispose. Derived test classes belong to <c>[Collection("Culture")]</c> because cultures are process-wide state.
/// </summary>
public abstract class CultureBunitContext(string cultureName = "de-CH") : BunitContext
{
    private readonly CultureScope _cultureScope = new(cultureName);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _cultureScope.Dispose();
        }
    }
}
