using HeroPath.Shared.Application;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using HeroPath.Shared.UI.Components;
using HeroPath.Shared.UI.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Shared.Tests.Components;

[Collection("Culture")]
public class SoundToggleTests : CultureBunitContext
{
    private readonly InMemoryAppStorage _storage = new();

    public SoundToggleTests()
    {
        Services.AddSharedUI();
        Services.AddFakeDocumentLanguage();
        Services.AddSingleton<IAppStorage>(_storage);
    }

    [Fact]
    public void Render_Initially_IsOffWithCrossedOutIcon()
    {
        var cut = Render<SoundToggle>();

        var button = cut.Find("[data-testid=sound-toggle]");
        Assert.Equal("false", button.GetAttribute("aria-pressed"));
        Assert.Equal("Ton", button.GetAttribute("aria-label"));
        Assert.Equal("Ton einschalten", button.GetAttribute("title"));
        Assert.Single(cut.FindAll("[data-testid=sound-icon-off]"));
        Assert.Empty(cut.FindAll("[data-testid=sound-icon-on]"));
    }

    [Fact]
    public async Task Click_SwitchesOnAndStores()
    {
        var cut = Render<SoundToggle>();

        await cut.Find("[data-testid=sound-toggle]").ClickAsync();

        cut.WaitForAssertion(() =>
        {
            var button = cut.Find("[data-testid=sound-toggle]");
            Assert.Equal("true", button.GetAttribute("aria-pressed"));
            Assert.Equal("Ton ausschalten", button.GetAttribute("title"));
            Assert.Single(cut.FindAll("[data-testid=sound-icon-on]"));
            Assert.Empty(cut.FindAll("[data-testid=sound-icon-off]"));
        });
        Assert.Equal("on", _storage.Values[StorageKeys.Sound]);
    }

    [Fact]
    public async Task Click_Twice_SwitchesBackOff()
    {
        var cut = Render<SoundToggle>();

        await cut.Find("[data-testid=sound-toggle]").ClickAsync();
        await cut.Find("[data-testid=sound-toggle]").ClickAsync();

        cut.WaitForAssertion(() => Assert.Equal("false", cut.Find("[data-testid=sound-toggle]").GetAttribute("aria-pressed")));
        Assert.Equal("off", _storage.Values[StorageKeys.Sound]);
    }

    [Fact]
    public async Task Render_WithStoredOn_ShowsOn()
    {
        _storage.Values[StorageKeys.Sound] = "on";
        await Services.GetRequiredService<SoundSettings>().ApplyStoredAsync(Xunit.TestContext.Current.CancellationToken);

        var cut = Render<SoundToggle>();

        Assert.Equal("true", cut.Find("[data-testid=sound-toggle]").GetAttribute("aria-pressed"));
    }

    [Fact]
    public async Task LanguageSwitch_TranslatesNameAndTooltip()
    {
        var cut = Render<SoundToggle>();

        await Services.GetRequiredService<LanguageService>().SetAsync(LanguageService.English, Xunit.TestContext.Current.CancellationToken);

        cut.WaitForAssertion(() =>
        {
            var button = cut.Find("[data-testid=sound-toggle]");
            Assert.Equal("Sound", button.GetAttribute("aria-label"));
            Assert.Equal("Turn sound on", button.GetAttribute("title"));
        });
    }

    [Fact]
    public async Task ChangeFromOutside_ReRenders()
    {
        var cut = Render<SoundToggle>();

        await Services.GetRequiredService<SoundSettings>().SetAsync(true, Xunit.TestContext.Current.CancellationToken);

        cut.WaitForAssertion(() => Assert.Equal("true", cut.Find("[data-testid=sound-toggle]").GetAttribute("aria-pressed")));
    }
}
