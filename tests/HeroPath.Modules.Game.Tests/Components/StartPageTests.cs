using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Game.UI;
using HeroPath.Modules.Game.UI.Pages;
using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;
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
    public void Render_HasABuildButtonThatOpensTheEditor()
    {
        var page = Render<StartPage>();

        var button = page.Find("[data-testid=start-build]");
        Assert.Equal("Level bauen", button.TextContent);

        button.Click();

        var navigation = Services.GetRequiredService<NavigationManager>();
        Assert.Equal(navigation.BaseUri + "editor", navigation.Uri);
    }

    [Fact]
    public void Render_UsesTheLevelsOfLevelProgress_AndPreselectsTheFirst()
    {
        var levels = new[]
        {
            TestLevels.Empty("synthetic-a", 4, 4, new Position(0, 0), new Position(3, 3)),
            TestLevels.Empty("synthetic-b", 4, 4, new Position(0, 0), new Position(3, 0)),
        };
        Services.AddScoped(_ => TestLevels.NewRepository(levels));

        var page = Render<StartPage>();

        Assert.Equal(["level-card-synthetic-a", "level-card-synthetic-b"], page.FindAll("[data-testid^=level-card-]").Select(c => c.GetAttribute("data-testid")));
        Assert.Equal("true", page.Find("[data-testid=level-card-synthetic-a]").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void Render_ShowsACardWithPreviewForEveryStandardLevelInOrder()
    {
        var page = Render<StartPage>();

        var cards = page.FindAll("[data-testid^=level-card-]");
        Assert.Equal(StandardLevels.All.Select(l => $"level-card-{l.Id}"), cards.Select(c => c.GetAttribute("data-testid")));
        foreach (var level in StandardLevels.All)
        {
            Assert.NotNull(page.Find($"[data-testid=level-preview-{level.Id}]"));
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
    public void Go_ThenPlayPage_SameLevelAgain_KeepsProgram()
    {
        Choice.Choose(HeroAvatar.Max, null, "easy-2");
        Session.Load(StandardLevels.Get("easy-2"));
        Session.Add(Direction.Up);
        var page = Render<StartPage>();

        page.Find("[data-testid=start-go]").Click();
        Render<PlayPage>();

        Assert.Single(Session.Program.Commands);
    }

    [Fact]
    public void Go_ThenPlayPage_OtherLevel_ClearsProgram()
    {
        Choice.Choose(HeroAvatar.Max, null, "easy-2");
        Session.Load(StandardLevels.Get("easy-2"));
        Session.Add(Direction.Up);
        var page = Render<StartPage>();

        page.Find("[data-testid=level-card-easy-1]").Click();
        page.Find("[data-testid=start-go]").Click();
        Render<PlayPage>();

        Assert.Empty(Session.Program.Commands);
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

    private async Task<LevelRepository> AddCustomLevelsAsync(params (string Id, string Name)[] levels)
    {
        var repository = Services.GetRequiredService<LevelRepository>();
        foreach (var (id, name) in levels)
        {
            var saved = await repository.SaveAsync(
                TestLevels.Empty(id, 5, 5, new Position(0, 0), new Position(4, 4)) with { Name = name },
                Xunit.TestContext.Current.CancellationToken);
            Assert.Equal(SaveOutcome.Saved, saved);
        }

        return repository;
    }

    [Fact]
    public void Render_WithoutCustomLevels_HasNoCustomSection()
    {
        var page = Render<StartPage>();

        Assert.Empty(page.FindAll("[data-testid=custom-levels]"));
    }

    [Fact]
    public async Task Render_WithCustomLevels_ShowsThemInASectionAfterTheStandardCards()
    {
        await AddCustomLevelsAsync(("custom-a", "Mein Burgweg"), ("custom-b", "Zweiter"));

        var page = Render<StartPage>();

        var section = page.Find("[data-testid=custom-levels]");
        Assert.Equal("Meine Levels", section.QuerySelector("h3")!.TextContent);
        Assert.Equal(["level-card-custom-a", "level-card-custom-b"], section.QuerySelectorAll("[data-testid^=level-card-]").Select(c => c.GetAttribute("data-testid")));
        Assert.Equal("Mein Burgweg", page.Find("[data-testid=level-card-custom-a] .level-card-name").TextContent);
        Assert.NotNull(page.Find("[data-testid=level-preview-custom-a]"));
        Assert.Equal(
            [.. StandardLevels.All.Select(l => $"level-card-{l.Id}"), "level-card-custom-a", "level-card-custom-b"],
            page.FindAll("[data-testid^=level-card-]").Select(c => c.GetAttribute("data-testid")));
        Assert.Empty(section.QuerySelectorAll("[data-testid=level-card-easy-1]"));
    }

    [Fact]
    public async Task Render_CustomLevelNameWithHtml_IsShownAsText()
    {
        await AddCustomLevelsAsync(("custom-a", "<b>Fett</b><script>x()</script>"));

        var page = Render<StartPage>();

        var name = page.Find("[data-testid=level-card-custom-a] .level-card-name");
        Assert.Equal("<b>Fett</b><script>x()</script>", name.TextContent);
        Assert.Empty(name.QuerySelectorAll("b, script"));
        Assert.Empty(page.FindAll("script"));
    }

    [Fact]
    public async Task Render_SolvedCustomLevel_ShowsBadgeAndAccessibleName()
    {
        await AddCustomLevelsAsync(("custom-a", "Mein Burgweg"));
        Services.GetRequiredService<LevelProgress>().MarkSolved("custom-a");

        var page = Render<StartPage>();

        Assert.Equal("Mein Burgweg, gelöst", page.Find("[data-testid=level-card-custom-a]").GetAttribute("aria-label"));
        Assert.NotNull(page.Find("[data-testid=level-solved-custom-a]"));
    }

    [Fact]
    public async Task Render_CustomSectionTitle_IsTranslated()
    {
        await AddCustomLevelsAsync(("custom-a", "Mine"));
        var page = Render<StartPage>();

        await Services.GetRequiredService<LanguageService>().SetAsync("en", Xunit.TestContext.Current.CancellationToken);

        page.WaitForAssertion(() => Assert.Equal("My levels", page.Find("[data-testid=custom-levels] h3").TextContent));
        Assert.Equal("Mine", page.Find("[data-testid=level-card-custom-a] .level-card-name").TextContent);
    }

    [Fact]
    public async Task Render_CustomLevelAddedLater_AppearsWithoutNewRender()
    {
        var page = Render<StartPage>();
        Assert.Empty(page.FindAll("[data-testid=custom-levels]"));

        await AddCustomLevelsAsync(("custom-a", "Mine"));

        page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-testid=level-card-custom-a]")));
    }

    [Fact]
    public async Task Go_WithACustomLevelSelected_ChoosesIt()
    {
        await AddCustomLevelsAsync(("custom-a", "Mine"));
        var page = Render<StartPage>();

        page.Find("[data-testid=level-card-custom-a]").Click();
        page.Find("[data-testid=start-go]").Click();

        Assert.Equal("custom-a", Choice.LevelId);
    }

    [Fact]
    public async Task Go_StoresNoHeroNameInTheCustomLevelStorage()
    {
        var storage = (InMemoryAppStorage)Services.GetRequiredService<IAppStorage>();
        await AddCustomLevelsAsync(("custom-a", "Mine"));
        var page = Render<StartPage>();
        page.Find("[data-testid=hero-name]").Input("Zoe");

        page.Find("[data-testid=start-go]").Click();

        Assert.DoesNotContain("Zoe", storage.Values[StorageKeys.CustomLevels], StringComparison.Ordinal);
    }
}
