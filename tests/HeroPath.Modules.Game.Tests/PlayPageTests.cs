using HeroPath.Modules.Game.UI.Pages;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Modules.Game.Tests;

public class PlayPageTests : BunitContext
{
    public PlayPageTests()
    {
        Services.AddSharedUI();
        Services.AddSingleton<IAppStorage, InMemoryAppStorage>();
    }

    [Fact]
    public void Render_ShowsPlayArea()
    {
        var page = Render<PlayPage>();

        Assert.NotNull(page.Find("main.play-page"));
    }
}
