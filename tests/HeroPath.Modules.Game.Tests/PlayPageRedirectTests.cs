using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.UI;
using HeroPath.Modules.Game.UI.Pages;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Modules.Game.Tests;

[Collection("Culture")]
public class PlayPageRedirectTests : CultureBunitContext
{
    public PlayPageRedirectTests()
    {
        Services.AddSharedUI();
        Services.AddGameModule();
        Services.AddSingleton<IAppStorage, InMemoryAppStorage>();
        Services.AddSingleton<ISoundPlayer, FakeSoundPlayer>();
        Services.AddSingleton<IKeyboardShortcuts, FakeKeyboardShortcuts>();
    }

    [Fact]
    public void Render_WithoutChoice_RedirectsToStartPageAndShowsNothing()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("play");

        var page = Render<PlayPage>();

        Assert.Equal(navigation.BaseUri, navigation.Uri);
        Assert.Empty(page.FindAll("[data-testid=game-board]"));
        Assert.Empty(page.FindAll("[data-testid=side-panel]"));
        Assert.Null(Services.GetRequiredService<GameSession>().Grid);
    }
}
