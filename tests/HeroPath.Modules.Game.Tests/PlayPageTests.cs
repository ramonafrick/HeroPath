using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Game.UI;
using HeroPath.Modules.Game.UI.Pages;
using HeroPath.Modules.Game.UI.Services;
using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.Infrastructure;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using Microsoft.AspNetCore.Components;
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
        Services.AddSingleton<ISoundPlayer, FakeSoundPlayer>();
        Services.AddSingleton<IKeyboardShortcuts, FakeKeyboardShortcuts>();
        Choice.Choose(HeroAvatar.Max, null, "easy-2");
    }

    private GameSession Session => Services.GetRequiredService<GameSession>();

    private HeroChoice Choice => Services.GetRequiredService<HeroChoice>();

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
        Assert.Equal("EmptyProgram", page.Find("[data-testid=message]").GetAttribute("data-code"));
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
    public async Task Dispose_DuringDrag_EndsTheDrag()
    {
        Render<PlayPage>();
        var drag = Services.GetRequiredService<ProgramDragState>();
        drag.Start(new DragSource.NewMove(Direction.Up));
        Assert.True(drag.IsDragging);

        await DisposeComponentsAsync();

        Assert.False(drag.IsDragging);
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
        Assert.Equal("Einfach 2", page.Find("[data-testid=level-name]").TextContent);
    }

    [Fact]
    public void Render_ShowsHeroBadgeWithChosenNameAndLevel()
    {
        Choice.Choose(HeroAvatar.Lisa, "Zoe", "easy-1");

        var page = Render<PlayPage>();

        Assert.Equal("Zoe", page.Find("[data-testid=hero-badge-name]").TextContent);
        Assert.Equal("Einfach 1", page.Find("[data-testid=level-name]").TextContent);
        Assert.NotNull(page.Find("[data-testid=hero] [data-testid=graphic-hero-lisa]"));
        Assert.Empty(page.FindAll("[data-testid=hero] [data-testid=graphic-hero-max]"));
    }

    [Fact]
    public void HeroChange_NavigatesToStartPage()
    {
        var page = Render<PlayPage>();
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("play");

        page.Find("[data-testid=hero-change]").Click();

        Assert.Equal(navigation.BaseUri, navigation.Uri);
    }

    [Fact]
    public void Render_OtherLevelChosen_ClearsProgram()
    {
        Render<PlayPage>();
        TestLevels.Fill(Session, Direction.Up);
        Choice.Choose(HeroAvatar.Max, null, "easy-1");

        var page = Render<PlayPage>();

        Assert.Empty(page.FindAll("[data-testid=program-block]"));
    }

    [Fact]
    public void Message_Success_UsesChosenName()
    {
        Choice.Choose(HeroAvatar.Lisa, "Zoe", "easy-2");
        var page = Render<PlayPage>();
        TestLevels.Fill(Session, TestLevels.DemoSolution);
        for (var i = 0; i < TestLevels.DemoSolution.Length; i++)
        {
            Runner.StepOnce();
        }

        Assert.Contains("Zoe", page.Find("[data-testid=message]").TextContent);
    }

    private void SolveEasy2()
    {
        TestLevels.Fill(Session, TestLevels.DemoSolution);
        for (var i = 0; i < TestLevels.DemoSolution.Length; i++)
        {
            Runner.StepOnce();
        }
    }

    [Fact]
    public void Picker_ListsAllLevelsAndSelectsTheCurrentOne()
    {
        var page = Render<PlayPage>();

        var options = page.FindAll("[data-testid=level-picker] option");
        Assert.Equal(7, options.Count);
        Assert.Equal("easy-2", page.Find("[data-testid=level-picker]").GetAttribute("value"));
    }

    [Fact]
    public void Picker_Change_LoadsOtherLevelAndClearsProgram()
    {
        var page = Render<PlayPage>();
        TestLevels.Fill(Session, Direction.Up, Direction.Up);

        page.Find("[data-testid=level-picker]").Change("easy-1");

        Assert.Equal("easy-1", Session.Level!.Id);
        Assert.Equal("easy-1", Choice.LevelId);
        Assert.Equal(HeroAvatar.Max, Choice.Avatar);
        Assert.Empty(page.FindAll("[data-testid=program-block]"));
        Assert.Equal("Einfach 1", page.Find("[data-testid=level-name]").TextContent);
        Assert.Equal("2", page.Find("[data-testid=hero]").GetAttribute("data-x"));
    }

    [Fact]
    public async Task Picker_Change_DuringPlay_CancelsTheRun()
    {
        var page = Render<PlayPage>();
        TestLevels.Fill(Session, TestLevels.DemoSolution);
        var play = Runner.PlayAsync();
        Assert.Equal(RunStatus.Running, Session.Status);

        page.Find("[data-testid=level-picker]").Change("easy-1");
        _time.Advance(TimeSpan.FromSeconds(10));
        await play;

        Assert.Equal(RunStatus.Ready, Session.Status);
        Assert.Equal("ready", page.Find("[data-testid=status]").GetAttribute("data-state"));
    }

    [Fact]
    public void Picker_MarksSolvedLevelsWithCheck()
    {
        var page = Render<PlayPage>();

        SolveEasy2();

        var option = page.Find("[data-testid=level-picker-option-easy-2]");
        Assert.Equal("true", option.GetAttribute("data-solved"));
        Assert.StartsWith("✓", option.TextContent.Trim());
    }

    [Fact]
    public void NextLevel_OnlyAfterSuccess()
    {
        var page = Render<PlayPage>();
        Assert.Empty(page.FindAll("[data-testid=next-level]"));

        TestLevels.Fill(Session, Direction.Up);
        Runner.StepOnce();
        Runner.StepOnce();

        Assert.Equal(RunStatus.Failed, Session.Status);
        Assert.Empty(page.FindAll("[data-testid=next-level]"));

        SolveAfterReset(page);
        Assert.Equal("Nächstes Level", page.Find("[data-testid=next-level]").TextContent.Trim());
    }

    private void SolveAfterReset(IRenderedComponent<PlayPage> page)
    {
        Runner.Reset();
        Session.Clear();
        SolveEasy2();
        Assert.NotNull(page.Find("[data-testid=next-level]"));
    }

    [Fact]
    public void NextLevel_Click_LoadsNextLevelWithEmptyProgram()
    {
        var page = Render<PlayPage>();
        SolveEasy2();

        page.Find("[data-testid=next-level]").Click();

        Assert.Equal("strawberries", Session.Level!.Id);
        Assert.Equal("strawberries", Choice.LevelId);
        Assert.Empty(page.FindAll("[data-testid=program-block]"));
        Assert.Empty(page.FindAll("[data-testid=next-level]"));
        Assert.Equal("ready", page.Find("[data-testid=status]").GetAttribute("data-state"));
    }

    [Fact]
    public void NextLevel_OnLastLevel_IsNotShown()
    {
        var last = StandardLevels.All[^1];
        Choice.Choose(HeroAvatar.Max, null, last.Id);
        var page = Render<PlayPage>();
        Services.GetRequiredService<LevelProgress>().MarkSolved(last.Id);

        Assert.Empty(page.FindAll("[data-testid=next-level]"));
    }

    private async Task SaveCustomLevelAsync()
    {
        var saved = await Services.GetRequiredService<LevelRepository>().SaveAsync(
            TestLevels.Empty("custom-a", 3, 3, new Position(0, 0), new Position(2, 0)) with { Name = "<i>Mein</i> Weg" },
            Xunit.TestContext.Current.CancellationToken);
        Assert.Equal(SaveOutcome.Saved, saved);
    }

    [Fact]
    public async Task CustomLevel_ShowsItsNameAsTextInTheHeading()
    {
        await SaveCustomLevelAsync();
        Choice.SelectLevel("custom-a");

        var page = Render<PlayPage>();

        var heading = page.Find("[data-testid=level-name]");
        Assert.Equal("<i>Mein</i> Weg", heading.TextContent);
        Assert.Empty(heading.QuerySelectorAll("i"));
        Assert.Equal(9, page.FindAll("[data-testid^=cell-]").Count);
    }

    [Fact]
    public async Task CustomLevel_IsListedInThePickerGroup_AndSwitchingLoadsIt()
    {
        await SaveCustomLevelAsync();
        var page = Render<PlayPage>();

        var option = page.Find("[data-testid=level-picker-group-custom] [data-testid=level-picker-option-custom-a]");
        Assert.Equal("<i>Mein</i> Weg", option.TextContent.Trim());
        page.Find("[data-testid=level-picker]").Change("custom-a");

        Assert.Equal("custom-a", Session.Level!.Id);
        Assert.Equal("custom-a", Choice.LevelId);
    }

    [Fact]
    public async Task CustomLevel_SolvedMarksItAndOffersNoNextLevel()
    {
        await SaveCustomLevelAsync();
        Choice.SelectLevel("custom-a");
        var page = Render<PlayPage>();

        Session.Add(Direction.Right);
        Session.Add(Direction.Right);
        Runner.StepOnce();
        Runner.StepOnce();

        Assert.Equal(RunStatus.Succeeded, Session.Status);
        Assert.True(Services.GetRequiredService<LevelProgress>().IsSolved("custom-a"));
        Assert.Empty(page.FindAll("[data-testid=next-level]"));
        Assert.StartsWith("✓", page.Find("[data-testid=level-picker-option-custom-a]").TextContent.Trim());
    }

    [Fact]
    public void EditButton_IsNotShownForAStandardLevel()
    {
        var page = Render<PlayPage>();

        Assert.Empty(page.FindAll("[data-testid=play-edit]"));
    }

    [Fact]
    public async Task EditButton_ForACustomLevel_OpensTheEditorWithThatLevel()
    {
        await SaveCustomLevelAsync();
        Choice.SelectLevel("custom-a");
        var page = Render<PlayPage>();

        var button = page.Find("[data-testid=play-edit]");
        Assert.Equal("Bearbeiten", button.TextContent);
        button.Click();

        var navigation = Services.GetRequiredService<NavigationManager>();
        Assert.Equal(navigation.BaseUri + "editor/custom-a", navigation.Uri);
    }

    [Fact]
    public async Task EditButton_FollowsTheLevelSwitchedInThePicker()
    {
        await SaveCustomLevelAsync();
        var page = Render<PlayPage>();
        Assert.Empty(page.FindAll("[data-testid=play-edit]"));

        page.Find("[data-testid=level-picker]").Change("custom-a");
        Assert.NotNull(page.Find("[data-testid=play-edit]"));

        page.Find("[data-testid=level-picker]").Change("easy-1");
        Assert.Empty(page.FindAll("[data-testid=play-edit]"));
    }

    [Fact]
    public async Task Render_AfterTheLevelWasEdited_LoadsTheNewVersionAndClearsAProgramThatNoLongerFits()
    {
        await SaveCustomLevelAsync();
        Choice.SelectLevel("custom-a");
        var repository = Services.GetRequiredService<LevelRepository>();
        var first = Render<PlayPage>();
        Session.Add(Direction.Right);
        await DisposeComponentsAsync();

        var edited = TestLevels.Empty("custom-a", 4, 3, new Position(0, 0), new Position(3, 0)) with { Name = "Neuer Name" };
        Assert.Equal(SaveOutcome.Saved, await repository.SaveAsync(edited, Xunit.TestContext.Current.CancellationToken));
        var second = Render<PlayPage>();

        Assert.Equal(12, second.FindAll("[data-testid^=cell-]").Count);
        Assert.Equal("Neuer Name", second.Find("[data-testid=level-name]").TextContent);
        Assert.Empty(Session.Program.Commands);
    }

    [Fact]
    public async Task Render_AfterOnlyTheNameWasEdited_KeepsTheProgramAndShowsTheNewName()
    {
        await SaveCustomLevelAsync();
        Choice.SelectLevel("custom-a");
        var repository = Services.GetRequiredService<LevelRepository>();
        var first = Render<PlayPage>();
        Session.Add(Direction.Right);
        await DisposeComponentsAsync();

        var renamed = repository.Find("custom-a")! with { Name = "Neuer Name" };
        Assert.Equal(SaveOutcome.Saved, await repository.SaveAsync(renamed, Xunit.TestContext.Current.CancellationToken));
        var second = Render<PlayPage>();

        Assert.Equal("Neuer Name", second.Find("[data-testid=level-name]").TextContent);
        Assert.Single(Session.Program.Commands);
        Assert.NotNull(first);
    }

    [Fact]
    public void Render_WhenTheChosenLevelWasDeleted_RedirectsToTheStartPage()
    {
        Choice.SelectLevel("custom-gone");
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("play");

        var page = Render<PlayPage>();

        Assert.Equal(navigation.BaseUri, navigation.Uri);
        Assert.Empty(page.FindAll("[data-testid=side-panel]"));
    }
}
