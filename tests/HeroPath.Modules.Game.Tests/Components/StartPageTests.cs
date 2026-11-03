using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Game.UI;
using HeroPath.Modules.Game.UI.Pages;
using HeroPath.Modules.Levels.Infrastructure;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using HeroPath.Shared.UI.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Modules.Game.Tests.Components;

[Collection("Culture")]
public class StartPageTests : CultureBunitContext
{
    public StartPageTests()
    {
        Services.AddSharedUI();
        Services.AddGameModule();
        Services.AddSingleton<IAppStorage, InMemoryAppStorage>();
    }

    private HeroChoice Choice => Services.GetRequiredService<HeroChoice>();

    private GameSession Session => Services.GetRequiredService<GameSession>();

    [Fact]
    public void Render_ShowsDefaultsMaxPrefilledNameAndFirstLevel()
    {
        var page = Render<StartPage>();

        Assert.Equal("true", page.Find("[data-testid=hero-max]").GetAttribute("aria-pressed"));
        Assert.Equal("false", page.Find("[data-testid=hero-lisa]").GetAttribute("aria-pressed"));
        var input = page.Find("[data-testid=hero-name]");
        Assert.Equal("Max", input.GetAttribute("value"));
        Assert.Equal("20", input.GetAttribute("maxlength"));
        Assert.Equal("true", page.Find("[data-testid=level-card-free-field]").GetAttribute("aria-pressed"));
        Assert.Equal("Los!", page.Find("[data-testid=start-go]").TextContent);
    }

    [Fact]
    public void Render_ShowsACardWithPreviewForEveryStandardLevelInOrder()
    {
        var page = Render<StartPage>();

        var cards = page.FindAll("[data-testid^=level-card-]");
        Assert.Equal(StandardLevels.All.Select(l => $"level-card-{l.Id}"), cards.Select(c => c.GetAttribute("data-testid")));
        foreach (var level in StandardLevels.All)
        {
            Assert.NotNull(page.Find($"[data-testid=level-card-{level.Id}] [data-testid=level-preview-{level.Id}]"));
        }

        Assert.Equal("Einfach 2", page.Find("[data-testid=level-card-easy-2] .level-card-name").TextContent);
    }

    [Fact]
    public void Render_PreviewsHaveNoHeroAndNoCellTestIds()
    {
        var page = Render<StartPage>();

        Assert.Empty(page.FindAll("[data-testid=hero]"));
        Assert.Empty(page.FindAll("[data-testid^=cell-]"));
        Assert.Empty(page.FindAll("[data-testid=game-board]"));
        Assert.Equal(StandardLevels.All.Count, page.FindAll("[data-testid=board-preview]").Count);
    }

    [Fact]
    public void SelectLisa_SwitchesCardAndReplacesDefaultName()
    {
        var page = Render<StartPage>();

        page.Find("[data-testid=hero-lisa]").Click();

        Assert.Equal("true", page.Find("[data-testid=hero-lisa]").GetAttribute("aria-pressed"));
        Assert.Equal("false", page.Find("[data-testid=hero-max]").GetAttribute("aria-pressed"));
        Assert.Equal("Lisa", page.Find("[data-testid=hero-name]").GetAttribute("value"));
    }

    [Fact]
    public void SelectOtherHero_KeepsTypedName()
    {
        var page = Render<StartPage>();
        page.Find("[data-testid=hero-name]").Input("Zoe");

        page.Find("[data-testid=hero-lisa]").Click();

        Assert.Equal("Zoe", page.Find("[data-testid=hero-name]").GetAttribute("value"));
    }

    [Fact]
    public void Go_StoresChoiceLoadsLevelAndNavigatesToPlay()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        var page = Render<StartPage>();
        page.Find("[data-testid=hero-lisa]").Click();
        page.Find("[data-testid=hero-name]").Input("  Zoe ");
        page.Find("[data-testid=level-card-easy-1]").Click();
        Assert.Equal("true", page.Find("[data-testid=level-card-easy-1]").GetAttribute("aria-pressed"));
        Assert.Equal("false", page.Find("[data-testid=level-card-free-field]").GetAttribute("aria-pressed"));

        page.Find("[data-testid=start-go]").Click();

        Assert.True(Choice.IsChosen);
        Assert.Equal(HeroAvatar.Lisa, Choice.Avatar);
        Assert.Equal("Zoe", Choice.Name);
        Assert.Equal("easy-1", Choice.LevelId);
        Assert.Equal("easy-1", Session.Level!.Id);
        Assert.Equal(new Uri(new Uri(navigation.BaseUri), "play").ToString(), navigation.Uri);
    }

    [Fact]
    public void Go_WithEmptyName_UsesDefaultName()
    {
        var page = Render<StartPage>();
        page.Find("[data-testid=hero-lisa]").Click();
        page.Find("[data-testid=hero-name]").Input("   ");

        page.Find("[data-testid=start-go]").Click();

        Assert.Equal("Lisa", Choice.Name);
    }

    [Fact]
    public void Go_SameLevelAgain_KeepsProgram()
    {
        Choice.Choose(HeroAvatar.Max, null, "easy-2");
        Session.Load(StandardLevels.Get("easy-2"));
        Session.Add(Direction.Up);
        var page = Render<StartPage>();

        page.Find("[data-testid=start-go]").Click();

        Assert.Single(Session.Program.Commands);
    }

    [Fact]
    public void Go_OtherLevel_ClearsProgram()
    {
        Choice.Choose(HeroAvatar.Max, null, "easy-2");
        Session.Load(StandardLevels.Get("easy-2"));
        Session.Add(Direction.Up);
        var page = Render<StartPage>();

        page.Find("[data-testid=level-card-easy-1]").Click();
        page.Find("[data-testid=start-go]").Click();

        Assert.Empty(Session.Program.Commands);
        Assert.Equal("easy-1", Session.Level!.Id);
    }

    [Fact]
    public void Render_AfterEarlierChoice_PrefillsHeroNameAndLevel()
    {
        Choice.Choose(HeroAvatar.Lisa, "Zoe", "small-maze");

        var page = Render<StartPage>();

        Assert.Equal("true", page.Find("[data-testid=hero-lisa]").GetAttribute("aria-pressed"));
        Assert.Equal("Zoe", page.Find("[data-testid=hero-name]").GetAttribute("value"));
        Assert.Equal("true", page.Find("[data-testid=level-card-small-maze]").GetAttribute("aria-pressed"));
    }

    [Fact]
    public async Task LanguageSwitch_KeepsInputsAndTranslatesTexts()
    {
        var page = Render<StartPage>();
        page.Find("[data-testid=hero-lisa]").Click();
        page.Find("[data-testid=hero-name]").Input("Zoe");
        page.Find("[data-testid=level-card-easy-1]").Click();

        await Services.GetRequiredService<LanguageService>().SetAsync("en", Xunit.TestContext.Current.CancellationToken);

        page.WaitForAssertion(() => Assert.Equal("Let's go!", page.Find("[data-testid=start-go]").TextContent));
        Assert.Equal("Zoe", page.Find("[data-testid=hero-name]").GetAttribute("value"));
        Assert.Equal("true", page.Find("[data-testid=hero-lisa]").GetAttribute("aria-pressed"));
        Assert.Equal("true", page.Find("[data-testid=level-card-easy-1]").GetAttribute("aria-pressed"));
        Assert.Equal("Easy 1", page.Find("[data-testid=level-card-easy-1] .level-card-name").TextContent);
    }

    [Fact]
    public void Go_DoesNotWriteTheNameToStorage()
    {
        var storage = (InMemoryAppStorage)Services.GetRequiredService<IAppStorage>();
        var page = Render<StartPage>();
        page.Find("[data-testid=hero-name]").Input("Zoe");

        page.Find("[data-testid=start-go]").Click();

        Assert.DoesNotContain(storage.Values.Values, value => value.Contains("Zoe", StringComparison.Ordinal));
    }

    [Fact]
    public void Render_SolvedLevel_ShowsBadgeAndAccessibleName()
    {
        Services.GetRequiredService<LevelProgress>().MarkSolved("easy-1");

        var page = Render<StartPage>();

        var solved = page.Find("[data-testid=level-card-easy-1]");
        Assert.Equal("true", solved.GetAttribute("data-solved"));
        Assert.Equal("Einfach 1, gelöst", solved.GetAttribute("aria-label"));
        Assert.NotNull(page.Find("[data-testid=level-solved-easy-1]"));
        var open = page.Find("[data-testid=level-card-easy-2]");
        Assert.Equal("false", open.GetAttribute("data-solved"));
        Assert.Null(open.GetAttribute("aria-label"));
        Assert.Empty(page.FindAll("[data-testid=level-solved-easy-2]"));
    }
}
