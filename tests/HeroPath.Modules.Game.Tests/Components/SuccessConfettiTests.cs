using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Game.UI;
using HeroPath.Modules.Game.UI.Pages;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using HeroPath.Shared.UI.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace HeroPath.Modules.Game.Tests.Components;

[Collection("Culture")]
public class SuccessConfettiTests : CultureBunitContext
{
    private const string Selector = "[data-testid=game-board] [data-testid=success-confetti]";

    public SuccessConfettiTests()
    {
        Services.AddSingleton<TimeProvider>(new FakeTimeProvider());
        Services.AddSharedUI();
        Services.AddGameModule();
        Services.AddSingleton<IAppStorage, InMemoryAppStorage>();
        Services.AddSingleton<ISoundPlayer, FakeSoundPlayer>();
        Services.GetRequiredService<HeroChoice>().Choose(HeroAvatar.Max, null, "easy-2");
    }

    private GameSession Session => Services.GetRequiredService<GameSession>();

    private ExecutionRunner Runner => Services.GetRequiredService<ExecutionRunner>();

    private void Solve()
    {
        TestLevels.Fill(Session, TestLevels.DemoSolution);
        for (var i = 0; i < TestLevels.DemoSolution.Length; i++)
        {
            Runner.StepOnce();
        }
    }

    [Fact]
    public void Confetti_IsAbsentBeforeAndPresentAtSucceeded()
    {
        var page = Render<PlayPage>();
        Assert.Empty(page.FindAll(Selector));

        Solve();

        Assert.Equal(RunStatus.Succeeded, Session.Status);
        var confetti = page.Find(Selector);
        Assert.Equal("true", confetti.GetAttribute("aria-hidden"));
        Assert.NotEmpty(confetti.QuerySelectorAll(".piece"));
    }

    [Fact]
    public void Confetti_IsAbsentWhenTheRunFailed()
    {
        var page = Render<PlayPage>();
        TestLevels.Fill(Session, Direction.Up);
        Runner.StepOnce();
        Runner.StepOnce();

        Assert.Equal(RunStatus.Failed, Session.Status);
        Assert.Empty(page.FindAll(Selector));
    }

    [Fact]
    public void Confetti_IsGoneAfterReset()
    {
        var page = Render<PlayPage>();
        Solve();
        Assert.Single(page.FindAll(Selector));

        Runner.Reset();

        Assert.Empty(page.FindAll(Selector));
    }

    [Fact]
    public void Confetti_IsGoneAfterALevelSwitch()
    {
        var page = Render<PlayPage>();
        Solve();
        Assert.Single(page.FindAll(Selector));

        page.Find("[data-testid=next-level]").Click();

        Assert.Empty(page.FindAll(Selector));
    }

    [Fact]
    public async Task Confetti_StaysAcrossALanguageSwitch()
    {
        var page = Render<PlayPage>();
        Solve();

        await Services.GetRequiredService<LanguageService>().SetAsync(LanguageService.English, Xunit.TestContext.Current.CancellationToken);

        page.WaitForAssertion(() => Assert.Equal("Next level", page.Find("[data-testid=next-level]").TextContent.Trim()));
        Assert.Single(page.FindAll(Selector));
    }
}
