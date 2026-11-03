using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Game.UI;
using HeroPath.Modules.Game.UI.Components;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Modules.Game.Tests.Components;

[Collection("Culture")]
public class ProgramRepeatProgressTests : CultureBunitContext
{
    private readonly GameSession _session;
    private readonly ExecutionRunner _runner;

    public ProgramRepeatProgressTests()
    {
        Services.AddSharedUI();
        Services.AddGameModule();
        Services.AddSingleton<IAppStorage, InMemoryAppStorage>();
        _session = Services.GetRequiredService<GameSession>();
        _runner = Services.GetRequiredService<ExecutionRunner>();
        _session.Load(TestLevels.DemoGrid());
    }

    private void BuildNested()
    {
        _session.Add(Direction.Up);
        _session.Add(Direction.Up);
        _session.AddRepeat();
        var outer = _session.Program.Commands[2].Id;
        _session.Select(outer);
        _session.Add(Direction.Right);
        _session.AddRepeat();
        var inner = ((RepeatCommand)_session.Program.Commands[2]).Body[1].Id;
        _session.Select(inner);
        _session.SetRepeatCount(inner, 2);
        _session.Add(Direction.Right);
    }

    private static string[] ActivePaths(IRenderedComponent<ProgramEditor> editor) =>
        [.. editor.FindAll("[data-active-repeat=true]").Select(e => e.GetAttribute("data-path")!)];

    [Fact]
    public void BeforeRun_NoRepeatIsActive()
    {
        BuildNested();

        var editor = Render<ProgramEditor>();

        Assert.Empty(editor.FindAll("[data-active-repeat]"));
        Assert.Empty(editor.FindAll(".repeat-round"));
    }

    [Fact]
    public void Debug_InsideRepeat_ShowsRoundAndKeepsCurrentMove()
    {
        _session.Add(Direction.Up);
        _session.AddRepeat();
        _session.Select(_session.Program.Commands[1].Id);
        _session.Add(Direction.Right);
        var editor = Render<ProgramEditor>();

        _runner.StepOnce();
        Assert.Empty(editor.FindAll("[data-active-repeat]"));

        _runner.StepOnce();
        Assert.Equal(["2"], ActivePaths(editor));
        Assert.Equal("Runde 1 von 3", editor.Find("[data-testid=repeat-round-2]").TextContent);
        Assert.Equal("true", editor.Find("[data-path='2.1']").GetAttribute("data-current"));
        Assert.Null(editor.Find("[data-path='2.1']").GetAttribute("data-active-repeat"));

        _runner.StepOnce();
        Assert.Equal("Runde 2 von 3", editor.Find("[data-testid=repeat-round-2]").TextContent);
    }

    [Fact]
    public void Debug_NestedRepeats_EachShowsItsOwnRound()
    {
        BuildNested();
        var editor = Render<ProgramEditor>();

        for (var i = 0; i < 5; i++)
        {
            _runner.StepOnce();
        }

        Assert.Equal(["3", "3.2"], ActivePaths(editor));
        Assert.Equal("Runde 1 von 3", editor.Find("[data-testid=repeat-round-3]").TextContent);
        Assert.Equal("Runde 2 von 2", editor.Find("[data-testid='repeat-round-3.2']").TextContent);
    }

    [Fact]
    public void Round_IsEnglish_WhenCultureIsEnglish()
    {
        using var scope = new CultureScope("en");
        BuildNested();
        var editor = Render<ProgramEditor>();

        _runner.StepOnce();
        _runner.StepOnce();
        _runner.StepOnce();
        _runner.StepOnce();

        Assert.Equal("Round 1 of 3", editor.Find("[data-testid=repeat-round-3]").TextContent);
        Assert.Equal("Round 1 of 2", editor.Find("[data-testid='repeat-round-3.2']").TextContent);
    }

    [Fact]
    public void Failure_InsideRepeat_KeepsRoundAndMarksBlock()
    {
        _session.AddRepeat();
        _session.Select(_session.Program.Commands[0].Id);
        _session.Add(Direction.Left);
        _session.Add(Direction.Left);
        var editor = Render<ProgramEditor>();

        for (var i = 0; i < 2; i++)
        {
            _runner.StepOnce();
        }

        Assert.Equal(RunStatus.Failed, _session.Status);
        Assert.Equal(["1"], ActivePaths(editor));
        Assert.Equal("Runde 1 von 3", editor.Find("[data-testid=repeat-round-1]").TextContent);
        Assert.Equal("true", editor.Find("[data-path='1.2']").GetAttribute("data-error"));
    }

    [Fact]
    public void Reset_RemovesRoundDisplay()
    {
        BuildNested();
        var editor = Render<ProgramEditor>();
        _runner.StepOnce();
        _runner.StepOnce();
        _runner.StepOnce();
        Assert.NotEmpty(editor.FindAll("[data-active-repeat=true]"));

        _runner.Reset();

        editor.WaitForAssertion(() => Assert.Empty(editor.FindAll("[data-active-repeat]")));
        Assert.Empty(editor.FindAll(".repeat-round"));
    }

    [Fact]
    public void StartingRun_ClearsSelectedRepeat()
    {
        BuildNested();
        _session.Select(_session.Program.Commands[2].Id);
        var editor = Render<ProgramEditor>();
        Assert.Equal("true", editor.Find("[data-path='3']").GetAttribute("data-selected"));

        _runner.StepOnce();

        Assert.Null(_session.SelectedId);
        editor.WaitForAssertion(() => Assert.Empty(editor.FindAll("[data-selected=true]")));
    }
}
