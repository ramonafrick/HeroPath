using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Game.UI;
using HeroPath.Modules.Game.UI.Pages;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace HeroPath.Modules.Game.Tests;

[Collection("Culture")]
public class PlayPageShortcutTests : CultureBunitContext
{
    private readonly FakeKeyboardShortcuts _keys = new();

    public PlayPageShortcutTests()
    {
        Services.AddSingleton<TimeProvider>(new FakeTimeProvider());
        Services.AddSharedUI();
        Services.AddGameModule();
        Services.AddSingleton<IAppStorage, InMemoryAppStorage>();
        Services.AddSingleton<ISoundPlayer, FakeSoundPlayer>();
        Services.AddSingleton<IKeyboardShortcuts>(_keys);
        Services.GetRequiredService<HeroChoice>().Choose(HeroAvatar.Max, null, "easy-2");
    }

    private GameSession Session => Services.GetRequiredService<GameSession>();

    private IRenderedComponent<PlayPage> RenderPage()
    {
        var page = Render<PlayPage>();
        page.WaitForAssertion(() => Assert.True(_keys.IsSubscribed));
        return page;
    }

    [Fact]
    public void Render_SubscribesOnce()
    {
        RenderPage();

        Assert.Equal(1, _keys.SubscribeCount);
    }

    [Fact]
    public async Task ArrowKeys_InsertMoveBlocksAndRender()
    {
        var page = RenderPage();

        await _keys.PressAsync("ArrowRight");
        await _keys.PressAsync("ArrowUp");

        Assert.Equal([Direction.Right, Direction.Up], TestLevels.Directions(Session));
        page.WaitForAssertion(() => Assert.Equal(2, page.FindAll("[data-testid=program-block]").Count));
    }

    [Fact]
    public async Task ArrowKey_InsertsAtTheSamePlaceAsAPaletteClick()
    {
        var page = RenderPage();
        page.Find("[data-testid=palette-left]").Click();
        await _keys.PressAsync("ArrowRight");
        Session.AddRepeat();
        var repeat = Session.Program.Commands[^1].Id;
        Session.Select(repeat);

        await _keys.PressAsync("ArrowDown");
        page.Find("[data-testid=palette-up]").Click();

        var body = ((RepeatCommand)Session.Program.Commands[^1]).Body;
        Assert.Equal([Direction.Down, Direction.Up], body.OfType<MoveCommand>().Select(c => c.Direction));
        Assert.Equal(3, Session.Program.Commands.Count);
    }

    [Fact]
    public async Task ShiftArrows_MoveSelectedBlock()
    {
        RenderPage();
        TestLevels.Fill(Session, Direction.Up, Direction.Down, Direction.Left);
        Session.Select(Session.Program.Commands[2].Id);

        await _keys.PressAsync("ArrowUp", shift: true);
        Assert.Equal([Direction.Up, Direction.Left, Direction.Down], TestLevels.Directions(Session));

        await _keys.PressAsync("ArrowDown", shift: true);
        Assert.Equal([Direction.Up, Direction.Down, Direction.Left], TestLevels.Directions(Session));
    }

    [Theory]
    [InlineData("Delete")]
    [InlineData("Backspace")]
    public async Task DeleteKeys_RemoveSelectedBlock(string key)
    {
        RenderPage();
        TestLevels.Fill(Session, Direction.Up, Direction.Down);
        Session.Select(Session.Program.Commands[0].Id);

        await _keys.PressAsync(key);

        Assert.Equal([Direction.Down], TestLevels.Directions(Session));
        Assert.Null(Session.SelectedId);
    }

    [Fact]
    public async Task MoveAndDelete_WithoutSelection_DoNothing()
    {
        RenderPage();
        TestLevels.Fill(Session, Direction.Up, Direction.Down);

        await _keys.PressAsync("ArrowUp", shift: true);
        await _keys.PressAsync("Delete");

        Assert.Equal([Direction.Up, Direction.Down], TestLevels.Directions(Session));
    }

    [Fact]
    public async Task Escape_ClearsSelection()
    {
        RenderPage();
        TestLevels.Fill(Session, Direction.Up);
        Session.Select(Session.Program.Commands[0].Id);

        await _keys.PressAsync("Escape");

        Assert.Null(Session.SelectedId);
        Assert.Single(Session.Program.Commands);
    }

    [Fact]
    public async Task Escape_WithOpenHelp_ClosesTheHelpFirstAndKeepsTheSelection()
    {
        var page = RenderPage();
        TestLevels.Fill(Session, Direction.Up);
        Session.Select(Session.Program.Commands[0].Id);
        page.Find("[data-testid=shortcut-help-toggle]").Click();
        Assert.True(page.Find("[data-testid=shortcut-help]").HasAttribute("open"));

        await _keys.PressAsync("Escape");

        page.WaitForAssertion(() => Assert.False(page.Find("[data-testid=shortcut-help]").HasAttribute("open")));
        Assert.NotNull(Session.SelectedId);

        await _keys.PressAsync("Escape");

        Assert.Null(Session.SelectedId);
    }

    [Fact]
    public void Render_SubscribesWithTheHandledKeyTable()
    {
        RenderPage();

        Assert.Same(ShortcutKeys.Handled, _keys.Handled);
    }

    [Fact]
    public async Task D_StepsOnceAndR_Resets()
    {
        RenderPage();
        TestLevels.Fill(Session, TestLevels.DemoSolution);

        await _keys.PressAsync("d");
        Assert.Equal(RunStatus.Paused, Session.Status);
        Assert.Equal(1, Session.CurrentStep!.StepNumber);

        await _keys.PressAsync("R");
        Assert.Equal(RunStatus.Ready, Session.Status);
        Assert.Null(Session.CurrentStep);
    }

    [Fact]
    public async Task OpenHelp_ClosesWhenDebugStartsByKey_AndStaysClosedWhileTheRunLasts()
    {
        var page = RenderPage();
        TestLevels.Fill(Session, TestLevels.DemoSolution);
        page.Find("[data-testid=shortcut-help-toggle]").Click();
        Assert.True(page.Find("[data-testid=shortcut-help]").HasAttribute("open"));

        await _keys.PressAsync("d");

        page.WaitForAssertion(() => Assert.False(page.Find("[data-testid=shortcut-help]").HasAttribute("open")));
        page.Find("[data-testid=shortcut-help-toggle]").Click();
        Assert.False(page.Find("[data-testid=shortcut-help]").HasAttribute("open"));

        await _keys.PressAsync("r");
        page.Find("[data-testid=shortcut-help-toggle]").Click();
        Assert.True(page.Find("[data-testid=shortcut-help]").HasAttribute("open"));
    }

    [Fact]
    public async Task OpenHelp_ClosesWhenPlayStartsByButton()
    {
        var page = RenderPage();
        TestLevels.Fill(Session, TestLevels.DemoSolution);
        page.Find("[data-testid=shortcut-help-toggle]").Click();
        Assert.True(page.Find("[data-testid=shortcut-help]").HasAttribute("open"));

        page.Find("[data-testid=control-play]").Click();

        page.WaitForAssertion(() => Assert.False(page.Find("[data-testid=shortcut-help]").HasAttribute("open")));
        await _keys.PressAsync("r");
    }

    [Fact]
    public async Task Space_PlaysAndResetWorksDuringTheRun()
    {
        var page = RenderPage();
        TestLevels.Fill(Session, TestLevels.DemoSolution);

        var play = _keys.PressAsync(" ");

        Assert.Equal(RunStatus.Running, Session.Status);
        await _keys.PressAsync("r");
        await play;
        Assert.Equal(RunStatus.Ready, Session.Status);
        page.WaitForAssertion(() => Assert.Equal("ready", page.Find("[data-testid=status]").GetAttribute("data-state")));
    }

    [Fact]
    public async Task EditingKeys_WhileRunning_AreLocked()
    {
        RenderPage();
        TestLevels.Fill(Session, TestLevels.DemoSolution);
        Session.Select(Session.Program.Commands[1].Id);
        var play = _keys.PressAsync(" ");
        Assert.Equal(RunStatus.Running, Session.Status);
        var before = TestLevels.Directions(Session);

        await _keys.PressAsync("ArrowLeft");
        await _keys.PressAsync("Delete");
        await _keys.PressAsync("ArrowUp", shift: true);

        Assert.Equal(before, TestLevels.Directions(Session));
        await _keys.PressAsync("r");
        await play;
    }

    [Fact]
    public async Task ArrowKey_WithFullProgram_AddsNothing()
    {
        RenderPage();
        for (var i = 0; i < ProgramLimits.MaxProgramCommands; i++)
        {
            Session.Add(Direction.Up);
        }

        await _keys.PressAsync("ArrowDown");

        Assert.Equal(ProgramLimits.MaxProgramCommands, Session.Program.CountAll());
    }

    [Fact]
    public async Task UnknownKey_DoesNothing()
    {
        RenderPage();

        await _keys.PressAsync("a");
        await _keys.PressAsync("ArrowLeft", shift: true);

        Assert.Empty(Session.Program.Commands);
    }

    [Fact]
    public async Task Dispose_StopsTheSubscriptionAndThePlay()
    {
        RenderPage();
        TestLevels.Fill(Session, TestLevels.DemoSolution);
        var play = _keys.PressAsync(" ");

        await DisposeComponentsAsync();

        Assert.False(_keys.IsSubscribed);
        Assert.Equal(RunStatus.Paused, Session.Status);
        await play;
    }

    [Fact]
    public async Task KeyAfterDispose_IsIgnored()
    {
        RenderPage();
        await DisposeComponentsAsync();

        await _keys.PressAsync("ArrowUp");

        Assert.Empty(Session.Program.Commands);
    }
}
