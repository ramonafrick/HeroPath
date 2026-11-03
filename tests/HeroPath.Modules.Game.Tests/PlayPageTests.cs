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
        Assert.NotNull(page.Find("[data-testid=side-panel]"));
    }

    [Fact]
    public void Render_ShowsDemoBoardWithHeroOnStart()
    {
        var page = Render<PlayPage>();

        Assert.Equal(100, page.FindAll("[data-testid^=cell-]").Count);
        Assert.Equal("start", page.Find("[data-testid=cell-1-6]").GetAttribute("data-kind"));
        Assert.Equal("goal", page.Find("[data-testid=cell-6-3]").GetAttribute("data-kind"));
        var hero = page.Find("[data-testid=hero]");
        Assert.Equal("1", hero.GetAttribute("data-x"));
        Assert.Equal("6", hero.GetAttribute("data-y"));
        Assert.NotNull(page.Find("[data-testid=hero] [data-testid=graphic-hero-max]"));
    }
}
