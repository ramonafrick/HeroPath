using HeroPath.Modules.Game.UI.Pages;

namespace HeroPath.Modules.Game.Tests;

public class PlayPageTests : BunitContext
{
    [Fact]
    public void Render_ShowsPlayArea()
    {
        var page = Render<PlayPage>();

        Assert.NotNull(page.Find("main.play-page"));
    }
}
