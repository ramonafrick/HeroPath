using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Game.UI;
using HeroPath.Modules.Game.UI.Pages;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace HeroPath.Modules.Game.Tests;

[Collection("Culture")]
public class PlayPageTests : CultureBunitContext
{
    private readonly FakeTimeProvider _time = new();

    public PlayPageTests()
    {
        Services.AddSingleton<TimeProvider>(_time);
        Services.AddSharedUI();
        Services.AddGameModule();
        Services.AddSingleton<IAppStorage, InMemoryAppStorage>();
    }

    private GameSession Session => Services.GetRequiredService<GameSession>();

    private ExecutionRunner Runner => Services.GetRequiredService<ExecutionRunner>();

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

    [Fact]
    public void Render_ShowsPaletteAndEmptyProgramInSidePanel()
    {
        var page = Render<PlayPage>();

        Assert.NotNull(page.Find("[data-testid=side-panel] [data-testid=palette-up]"));
        Assert.NotNull(page.Find("[data-testid=side-panel] [data-testid=program]"));
        Assert.NotNull(page.Find("[data-testid=program-empty]"));
    }

    [Fact]
    public void PaletteClick_AddsBlockToProgram()
    {
        var page = Render<PlayPage>();

        page.Find("[data-testid=palette-right]").Click();

        var block = Assert.Single(page.FindAll("[data-testid=program-block]"));
        Assert.Equal("right", block.GetAttribute("data-direction"));
        Assert.Equal("1", page.Find("[data-testid=hero]").GetAttribute("data-x"));
    }

    [Fact]
    public async Task Dispose_DuringPlay_StopsLoopAndPausesRun()
    {
        Render<PlayPage>();
        TestLevels.Fill(Session, TestLevels.DemoSolution);
        var play = Runner.PlayAsync();
        Assert.Equal(1, Session.CurrentStep!.StepNumber);

        await DisposeComponentsAsync();
        _time.Advance(TimeSpan.FromMilliseconds(5_000));
        await play;

        Assert.Equal(RunStatus.Paused, Session.Status);
        Assert.Equal(1, Session.CurrentStep!.StepNumber);
        Assert.Equal(new Position(1, 5), Session.HeroPosition);
        Assert.Equal(TestLevels.DemoSolution.Length, Session.Program.Commands.Count);
    }

    [Fact]
    public async Task Render_Twice_KeepsProgramAndRun()
    {
        Render<PlayPage>();
        TestLevels.Fill(Session, Direction.Up, Direction.Up);
        Runner.StepOnce();
        await DisposeComponentsAsync();

        var page = Render<PlayPage>();

        Assert.Equal(2, page.FindAll("[data-testid=program-block]").Count);
        Assert.Equal("paused", page.Find("[data-testid=status]").GetAttribute("data-state"));
        Assert.Equal("5", page.Find("[data-testid=hero]").GetAttribute("data-y"));
    }

    [Fact]
    public void Render_InGerman_ShowsStatusAndLevelName()
    {
        var page = Render<PlayPage>();

        Assert.Equal("Bereit", page.Find("[data-testid=status] .status-text").TextContent);
        Assert.Equal("Übungsfeld", page.Find("[data-testid=level-name]").TextContent);
    }
}
