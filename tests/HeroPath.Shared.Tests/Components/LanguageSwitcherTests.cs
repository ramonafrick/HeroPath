using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.Tests.Fakes;
using HeroPath.Shared.UI;
using HeroPath.Shared.UI.Components;
using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Shared.Tests.Components;

[Collection("Culture")]
public class LanguageSwitcherTests : BunitContext
{
    private readonly CultureScope _cultureScope = new();
    private readonly InMemoryAppStorage _storage = new();

    public LanguageSwitcherTests()
    {
        Services.AddSharedUI();
        Services.AddSingleton<IAppStorage>(_storage);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _cultureScope.Dispose();
        }
    }

    [Fact]
    public void Render_Initially_MarksGermanAsActive()
    {
        var cut = Render<LanguageSwitcher>();

        Assert.Equal("true", cut.Find("[data-testid=language-de]").GetAttribute("aria-pressed"));
        Assert.Equal("false", cut.Find("[data-testid=language-en]").GetAttribute("aria-pressed"));
    }

    [Fact]
    public async Task ClickEnglish_SwitchesActiveButtonLabelAndStoresLanguage()
    {
        var cut = Render<LanguageSwitcher>();

        await cut.Find("[data-testid=language-en]").ClickAsync();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("false", cut.Find("[data-testid=language-de]").GetAttribute("aria-pressed"));
            Assert.Equal("true", cut.Find("[data-testid=language-en]").GetAttribute("aria-pressed"));
            Assert.Equal("Choose language", cut.Find("[role=group]").GetAttribute("aria-label"));
        });
        Assert.Equal("en", _storage.Values[StorageKeys.Language]);
    }

    [Fact]
    public async Task ClickGerman_AfterEnglish_SwitchesBack()
    {
        var cut = Render<LanguageSwitcher>();
        await cut.Find("[data-testid=language-en]").ClickAsync();

        await cut.Find("[data-testid=language-de]").ClickAsync();

        cut.WaitForAssertion(() => Assert.Equal("true", cut.Find("[data-testid=language-de]").GetAttribute("aria-pressed")));
        Assert.Equal("de-CH", _storage.Values[StorageKeys.Language]);
    }
}
