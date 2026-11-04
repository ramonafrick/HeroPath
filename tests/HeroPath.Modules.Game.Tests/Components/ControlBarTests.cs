using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Game.UI;
using HeroPath.Modules.Game.UI.Components;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using HeroPath.Shared.UI.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace HeroPath.Modules.Game.Tests.Components;

[Collection("Culture")]
public class ControlBarTests : CultureBunitContext
{
    private readonly GameSession _session;
    private readonly ExecutionRunner _runner;
    private readonly FakeTimeProvider _time = new();

    public ControlBarTests()
    {
        Services.AddSingleton<TimeProvider>(_time);
        Services.AddSharedUI();
        Services.AddFakeDocumentLanguage();
        Services.AddGameModule();
        Services.AddSingleton<IAppStorage, InMemoryAppStorage>();
        _session = Services.GetRequiredService<GameSession>();
        _runner = Services.GetRequiredService<ExecutionRunner>();
        _session.Load(TestLevels.Demo);
    }

    private static bool IsDisabled(IRenderedComponent<ControlBar> bar, string testId) =>
        bar.Find($"[data-testid={testId}]").HasAttribute("disabled");

    [Fact]
    public void Render_EmptyProgram_DisablesPlayAndDebugButNotReset()
    {
        var bar = Render<ControlBar>();

        Assert.True(IsDisabled(bar, "control-play"));
        Assert.True(IsDisabled(bar, "control-debug"));
        Assert.False(IsDisabled(bar, "control-reset"));
        Assert.Equal("ready", bar.Find("[data-testid=status]").GetAttribute("data-state"));
    }

    [Fact]
    public void Render_WithProgram_EnablesPlayAndDebug()
    {
        _session.Add(Direction.Up);

        var bar = Render<ControlBar>();

        Assert.False(IsDisabled(bar, "control-play"));
        Assert.False(IsDisabled(bar, "control-debug"));
    }

    [Fact]
    public void Render_Paused_KeepsPlayAndDebugEnabled()
    {
        TestLevels.Fill(_session, Direction.Up, Direction.Up);
        _runner.StepOnce();

        var bar = Render<ControlBar>();

        Assert.Equal("paused", bar.Find("[data-testid=status]").GetAttribute("data-state"));
        Assert.False(IsDisabled(bar, "control-play"));
        Assert.False(IsDisabled(bar, "control-debug"));
    }

    [Fact]
    public async Task Render_Running_DisablesPlayAndDebugButNotReset()
    {
        TestLevels.Fill(_session, Direction.Up, Direction.Up);
        var play = _runner.PlayAsync();

        var bar = Render<ControlBar>();

        Assert.Equal("running", bar.Find("[data-testid=status]").GetAttribute("data-state"));
        Assert.True(IsDisabled(bar, "control-play"));
        Assert.True(IsDisabled(bar, "control-debug"));
        Assert.False(IsDisabled(bar, "control-reset"));

        _runner.Reset();
        await play;
    }

    [Fact]
    public void Render_Succeeded_DisablesPlayAndDebug()
    {
        TestLevels.Fill(_session, TestLevels.DemoSolution);
        for (var i = 0; i < TestLevels.DemoSolution.Length; i++)
        {
            _runner.StepOnce();
        }

        var bar = Render<ControlBar>();

        var status = bar.Find("[data-testid=status]");
        Assert.Equal("succeeded", status.GetAttribute("data-state"));
        Assert.Null(status.GetAttribute("data-error"));
        Assert.True(IsDisabled(bar, "control-play"));
        Assert.True(IsDisabled(bar, "control-debug"));
        Assert.False(IsDisabled(bar, "control-reset"));
    }

    [Fact]
    public void Render_Failed_ShowsErrorNameAndDisablesPlayAndDebug()
    {
        TestLevels.Fill(_session, Direction.Right, Direction.Right);
        _runner.StepOnce();
        _runner.StepOnce();

        var bar = Render<ControlBar>();

        var status = bar.Find("[data-testid=status]");
        Assert.Equal("failed", status.GetAttribute("data-state"));
        Assert.Equal("Obstacle", status.GetAttribute("data-error"));
        Assert.True(IsDisabled(bar, "control-play"));
        Assert.True(IsDisabled(bar, "control-debug"));
    }

    [Fact]
    public void Status_IsLiveRegionWithIcon()
    {
        var bar = Render<ControlBar>();

        Assert.Equal("polite", bar.Find("[data-testid=status]").GetAttribute("aria-live"));
        Assert.NotNull(bar.Find("[data-testid=status] svg[aria-hidden=true]"));
    }

    [Fact]
    public void DebugClick_StepsOnceAndShowsPause()
    {
        TestLevels.Fill(_session, Direction.Up, Direction.Up);
        var bar = Render<ControlBar>();

        bar.Find("[data-testid=control-debug]").Click();

        bar.WaitForAssertion(() => Assert.Equal("paused", bar.Find("[data-testid=status]").GetAttribute("data-state")));
        Assert.Equal(new Position(1, 5), _session.HeroPosition);
    }

    [Fact]
    public void ResetClick_RestoresReady()
    {
        TestLevels.Fill(_session, Direction.Up, Direction.Up);
        _runner.StepOnce();
        var bar = Render<ControlBar>();

        bar.Find("[data-testid=control-reset]").Click();

        bar.WaitForAssertion(() => Assert.Equal("ready", bar.Find("[data-testid=status]").GetAttribute("data-state")));
        Assert.True(_session.IsEditable);
    }

    [Fact]
    public async Task PlayClick_StartsRunAndRendersRunning()
    {
        TestLevels.Fill(_session, Direction.Up, Direction.Up, Direction.Up);
        var bar = Render<ControlBar>();

        bar.Find("[data-testid=control-play]").Click();

        bar.WaitForAssertion(() => Assert.Equal("running", bar.Find("[data-testid=status]").GetAttribute("data-state")));
        Assert.Equal(new Position(1, 5), _session.HeroPosition);

        _runner.Reset();
        await Task.Yield();
    }

    [Fact]
    public void Tempo_DefaultsToMediumWithRangeAttributes()
    {
        var bar = Render<ControlBar>();

        var tempo = bar.Find("[data-testid=control-tempo]");
        Assert.Equal("range", tempo.GetAttribute("type"));
        Assert.Equal("0", tempo.GetAttribute("min"));
        Assert.Equal("2", tempo.GetAttribute("max"));
        Assert.Equal("1", tempo.GetAttribute("step"));
        Assert.Equal("1", tempo.GetAttribute("value"));
        Assert.Equal(bar.Find("[data-testid=tempo-value]").TextContent, tempo.GetAttribute("aria-valuetext"));
    }

    [Theory]
    [InlineData("0", Tempo.Slow, "Langsam")]
    [InlineData("1", Tempo.Medium, "Mittel")]
    [InlineData("2", Tempo.Fast, "Schnell")]
    public async Task Tempo_Input_UpdatesRunnerAndLabel(string value, Tempo expected, string label)
    {
        await Services.GetRequiredService<LanguageService>().SetAsync("de-CH", Xunit.TestContext.Current.CancellationToken);
        var bar = Render<ControlBar>();

        bar.Find("[data-testid=control-tempo]").Input(value);

        bar.WaitForAssertion(() => Assert.Equal(expected, _session.Tempo));
        Assert.Equal(label, bar.Find("[data-testid=tempo-value]").TextContent);
        Assert.Equal(label, bar.Find("[data-testid=control-tempo]").GetAttribute("aria-valuetext"));
    }

    [Fact]
    public void Tempo_InvalidInput_IsIgnored()
    {
        var bar = Render<ControlBar>();

        bar.Find("[data-testid=control-tempo]").Input("7");
        bar.Find("[data-testid=control-tempo]").Input("abc");

        Assert.Equal(Tempo.Medium, _session.Tempo);
    }

    [Theory]
    [InlineData("de-CH", "Bereit", "Zurücksetzen")]
    [InlineData("en", "Ready", "Reset")]
    public async Task Texts_FollowLanguage(string culture, string ready, string reset)
    {
        var bar = Render<ControlBar>();

        await Services.GetRequiredService<LanguageService>().SetAsync(culture, Xunit.TestContext.Current.CancellationToken);

        bar.WaitForAssertion(() => Assert.Contains(ready, bar.Find("[data-testid=status]").TextContent));
        Assert.Contains(reset, bar.Find("[data-testid=control-reset]").TextContent);
    }

    [Fact]
    public async Task LanguageSwitch_WhilePaused_KeepsStatusAndHero()
    {
        TestLevels.Fill(_session, Direction.Up, Direction.Up);
        _runner.StepOnce();
        var bar = Render<ControlBar>();

        await Services.GetRequiredService<LanguageService>().SetAsync("en", Xunit.TestContext.Current.CancellationToken);

        bar.WaitForAssertion(() => Assert.Contains("Paused", bar.Find("[data-testid=status]").TextContent));
        Assert.Equal("paused", bar.Find("[data-testid=status]").GetAttribute("data-state"));
        Assert.Equal(new Position(1, 5), _session.HeroPosition);
        Assert.Equal(2, _session.Program.Commands.Count);
    }
}
