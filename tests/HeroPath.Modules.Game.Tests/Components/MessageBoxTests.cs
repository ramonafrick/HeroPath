using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Game.UI;
using HeroPath.Modules.Game.UI.Components;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.Infrastructure;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using HeroPath.Shared.UI.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace HeroPath.Modules.Game.Tests.Components;

[Collection("Culture")]
public class MessageBoxTests : CultureBunitContext
{
    private readonly GameSession _session;
    private readonly ExecutionRunner _runner;

    public MessageBoxTests()
    {
        Services.AddSingleton<TimeProvider>(new FakeTimeProvider());
        Services.AddSharedUI();
        Services.AddGameModule();
        Services.AddSingleton<IAppStorage, InMemoryAppStorage>();
        _session = Services.GetRequiredService<GameSession>();
        _runner = Services.GetRequiredService<ExecutionRunner>();
        _session.Load(TestLevels.Demo);
    }

    private static LevelDefinition ItemGrid(params LevelItem[] items) =>
        TestLevels.WithItems("test-items", 5, 3, new Position(0, 0), new Position(2, 0), items);

    private void RunToEnd()
    {
        for (var i = 0; i < 1200 && _session.Status is RunStatus.Ready or RunStatus.Paused; i++)
        {
            _runner.StepOnce();
        }
    }

    private void Setup(string scenario)
    {
        switch (scenario)
        {
            case "Obstacle_Tree":
                TestLevels.Fill(_session, Direction.Right, Direction.Right);
                break;
            case "Obstacle_Rock":
                TestLevels.Fill(_session, Direction.Up, Direction.Up, Direction.Up, Direction.Right, Direction.Right, Direction.Right);
                break;
            case "Obstacle_Pond":
                TestLevels.Fill(_session, Direction.Up, Direction.Right, Direction.Right, Direction.Right, Direction.Right, Direction.Right);
                break;
            case "OutOfBounds":
                TestLevels.Fill(_session, Direction.Left, Direction.Left);
                break;
            case "GoalNotReached":
                TestLevels.Fill(_session, Direction.Up);
                break;
            case "ItemsMissing_Other":
                _session.Load(ItemGrid(new LevelItem(0, 1, ItemKind.Strawberry), new LevelItem(0, 2, ItemKind.Strawberry)));
                TestLevels.Fill(_session, Direction.Right, Direction.Right);
                break;
            case "ItemsMissing_One":
                _session.Load(ItemGrid(new LevelItem(1, 0, ItemKind.Strawberry), new LevelItem(0, 2, ItemKind.Strawberry)));
                TestLevels.Fill(_session, Direction.Right, Direction.Right);
                break;
            case "StepLimitExceeded":
                _session.AddRepeat();
                _session.SetRepeatCount(_session.Program.Commands[0].Id, 20);
                _session.Select(_session.Program.Commands[0].Id);
                _session.AddRepeat();
                var inner = ((RepeatCommand)_session.Program.Commands[0]).Body[0].Id;
                _session.SetRepeatCount(inner, 20);
                _session.Select(inner);
                TestLevels.Fill(_session, Direction.Up, Direction.Down, Direction.Up, Direction.Down);
                break;
            case "Succeeded":
                TestLevels.Fill(_session, TestLevels.DemoSolution);
                break;
            case "EmptyRepeat":
                _session.AddRepeat();
                _runner.StepOnce();
                return;
            case "EmptyProgram":
                return;
            case "ProgramFull":
                TestLevels.Fill(_session, [.. Enumerable.Repeat(Direction.Up, ProgramLimits.MaxProgramCommands)]);
                return;
            case "NestingTooDeep":
                _session.AddRepeat();
                _session.Select(_session.Program.Commands[0].Id);
                _session.AddRepeat();
                _session.Select(((RepeatCommand)_session.Program.Commands[0]).Body[0].Id);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario));
        }

        RunToEnd();
    }

    public static TheoryData<string, string, string, string, string> Texts => new()
    {
        { "Obstacle_Tree", "error", "Obstacle", "de-CH", "Autsch! Da steht ein Baum im Weg. Schau dir Baustein 2 noch einmal an." },
        { "Obstacle_Tree", "error", "Obstacle", "en", "Ouch! There's a tree in the way. Take another look at block 2." },
        { "Obstacle_Rock", "error", "Obstacle", "de-CH", "Autsch! Da liegt ein Stein im Weg. Schau dir Baustein 6 noch einmal an." },
        { "Obstacle_Rock", "error", "Obstacle", "en", "Ouch! There's a rock in the way. Take another look at block 6." },
        { "Obstacle_Pond", "error", "Obstacle", "de-CH", "Autsch! Da ist ein Teich im Weg. Schau dir Baustein 6 noch einmal an." },
        { "Obstacle_Pond", "error", "Obstacle", "en", "Ouch! There's a pond in the way. Take another look at block 6." },
        { "OutOfBounds", "error", "OutOfBounds", "de-CH", "Hoppla! Hier ist die Welt zu Ende. Baustein 2 führt aus dem Spielfeld." },
        { "OutOfBounds", "error", "OutOfBounds", "en", "Oops! The world ends here. Block 2 leads off the board." },
        { "GoalNotReached", "error", "GoalNotReached", "de-CH", "Dein Programm ist fertig, aber Max ist noch nicht bei der Burg. Probier es nochmal!" },
        { "GoalNotReached", "error", "GoalNotReached", "en", "Your program is done, but Max hasn't reached the castle yet. Try again!" },
        { "ItemsMissing_Other", "error", "ItemsMissing", "de-CH", "Fast geschafft! Es fehlen noch 2 Erdbeeren." },
        { "ItemsMissing_Other", "error", "ItemsMissing", "en", "Almost there! 2 strawberries are still missing." },
        { "ItemsMissing_One", "error", "ItemsMissing", "de-CH", "Fast geschafft! Es fehlt noch 1 Erdbeere." },
        { "ItemsMissing_One", "error", "ItemsMissing", "en", "Almost there! 1 strawberry is still missing." },
        { "StepLimitExceeded", "error", "StepLimitExceeded", "de-CH", "Puh! Dein Programm macht mehr als 1000 Schritte. Brauchst du wirklich so viele Wiederholungen?" },
        { "StepLimitExceeded", "error", "StepLimitExceeded", "en", "Phew! Your program takes more than 1000 steps. Do you really need that many repeats?" },
        { "Succeeded", "success", "Succeeded", "de-CH", "Geschafft, Max! Du hast die Burg erreicht." },
        { "Succeeded", "success", "Succeeded", "en", "You did it, Max! You reached the castle." },
        { "EmptyRepeat", "hint", "EmptyRepeat", "de-CH", "Leg zuerst Bausteine in den Wiederhole-Block." },
        { "EmptyRepeat", "hint", "EmptyRepeat", "en", "Put some blocks into the repeat block first." },
        { "EmptyProgram", "hint", "EmptyProgram", "de-CH", "Zieh Bausteine ins Programm oder klick sie an." },
        { "EmptyProgram", "hint", "EmptyProgram", "en", "Drag blocks into your program or click them." },
        { "ProgramFull", "hint", "ProgramFull", "de-CH", "Dein Programm ist voll. Lösch zuerst einen Baustein." },
        { "ProgramFull", "hint", "ProgramFull", "en", "Your program is full. Delete a block first." },
        { "NestingTooDeep", "hint", "NestingTooDeep", "de-CH", "Hier passt kein weiterer Wiederhole-Block hinein." },
        { "NestingTooDeep", "hint", "NestingTooDeep", "en", "There's no room for another repeat block in here." },
    };

    [Theory]
    [MemberData(nameof(Texts))]
    public async Task Render_Message_ShowsKindCodeAndTextInLanguage(string scenario, string kind, string code, string culture, string text)
    {
        Setup(scenario);
        var box = Render<MessageBox>();

        await Services.GetRequiredService<LanguageService>().SetAsync(culture, Xunit.TestContext.Current.CancellationToken);

        box.WaitForAssertion(() => Assert.Equal(text, box.Find("[data-testid=message]").TextContent.Trim()));
        var message = box.Find("[data-testid=message]");
        Assert.Equal(kind, message.GetAttribute("data-kind"));
        Assert.Equal(code, message.GetAttribute("data-code"));
    }

    [Theory]
    [InlineData("GoalNotReached", "de-CH", "Dein Programm ist fertig, aber Zoe ist noch nicht bei der Burg. Probier es nochmal!")]
    [InlineData("GoalNotReached", "en", "Your program is done, but Zoe hasn't reached the castle yet. Try again!")]
    [InlineData("Succeeded", "de-CH", "Geschafft, Zoe! Du hast die Burg erreicht.")]
    [InlineData("Succeeded", "en", "You did it, Zoe! You reached the castle.")]
    public async Task Render_HeroMessages_UseChosenName(string scenario, string culture, string text)
    {
        Services.GetRequiredService<HeroChoice>().Choose(HeroAvatar.Lisa, "Zoe", "easy-2");
        Setup(scenario);
        var box = Render<MessageBox>();

        await Services.GetRequiredService<LanguageService>().SetAsync(culture, Xunit.TestContext.Current.CancellationToken);

        box.WaitForAssertion(() => Assert.Equal(text, box.Find("[data-testid=message]").TextContent.Trim()));
    }

    [Theory]
    [InlineData("de-CH", "Geschafft, Max! Du hast die Burg erreicht. Du hast alle Level geschafft!")]
    [InlineData("en", "You did it, Max! You reached the castle. You've solved all the levels!")]
    public async Task Render_SuccessWhenAllLevelsSolved_AddsSecondSentence(string culture, string text)
    {
        var progress = Services.GetRequiredService<LevelProgress>();
        foreach (var level in StandardLevels.All)
        {
            progress.MarkSolved(level.Id);
        }

        Setup("Succeeded");
        var box = Render<MessageBox>();
        await Services.GetRequiredService<LanguageService>().SetAsync(culture, Xunit.TestContext.Current.CancellationToken);

        box.WaitForAssertion(() => Assert.Equal(text, box.Find("[data-testid=message]").TextContent.Trim()));
    }

    [Fact]
    public void Render_SuccessWithLevelsLeft_HasNoAllSolvedSentence()
    {
        Setup("Succeeded");

        var box = Render<MessageBox>();

        Assert.DoesNotContain("alle Level", box.Find("[data-testid=message]").TextContent);
    }

    [Fact]
    public void Render_SuccessMessage_UpdatesWhenTheLastLevelGetsSolved()
    {
        var progress = Services.GetRequiredService<LevelProgress>();
        foreach (var level in StandardLevels.All.SkipLast(1))
        {
            progress.MarkSolved(level.Id);
        }

        Setup("Succeeded");
        var box = Render<MessageBox>();
        Assert.DoesNotContain("alle Level", box.Find("[data-testid=message]").TextContent);

        progress.MarkSolved(StandardLevels.All[^1].Id);

        box.WaitForAssertion(() => Assert.Contains("alle Level", box.Find("[data-testid=message]").TextContent));
    }

    [Fact]
    public void Render_WithoutChoice_UsesDefaultHeroName()
    {
        Setup("Succeeded");

        var box = Render<MessageBox>();

        Assert.Contains("Max", box.Find("[data-testid=message]").TextContent);
    }

    [Fact]
    public void Render_NestedBlock_ShowsHierarchicalNumber()
    {
        _session.AddRepeat();
        _session.Select(_session.Program.Commands[0].Id);
        TestLevels.Fill(_session, Direction.Right, Direction.Right, Direction.Right);
        RunToEnd();

        var box = Render<MessageBox>();

        Assert.Contains("Baustein 1.2", box.Find("[data-testid=message]").TextContent);
    }

    [Fact]
    public void Render_NoMessage_ShowsNothingButKeepsLiveRegion()
    {
        TestLevels.Fill(_session, Direction.Up);

        var box = Render<MessageBox>();

        Assert.Empty(box.FindAll("[data-testid=message]"));
        Assert.Equal("polite", box.Find("[aria-live]").GetAttribute("aria-live"));
    }

    [Fact]
    public async Task LanguageSwitch_TranslatesVisibleMessageAndKeepsRun()
    {
        Setup("Obstacle_Tree");
        var box = Render<MessageBox>();
        Assert.Contains("Autsch", box.Find("[data-testid=message]").TextContent);
        var step = _session.CurrentStep;

        await Services.GetRequiredService<LanguageService>().SetAsync("en", Xunit.TestContext.Current.CancellationToken);

        box.WaitForAssertion(() => Assert.Contains("Ouch", box.Find("[data-testid=message]").TextContent));
        Assert.Same(step, _session.CurrentStep);
        Assert.Equal(RunStatus.Failed, _session.Status);
    }

    [Fact]
    public void Render_AfterReset_RemovesErrorMessage()
    {
        Setup("OutOfBounds");
        var box = Render<MessageBox>();
        Assert.Equal("OutOfBounds", box.Find("[data-testid=message]").GetAttribute("data-code"));

        _runner.Reset();

        box.WaitForAssertion(() => Assert.Empty(box.FindAll("[data-testid=message]")));
    }

    [Fact]
    public void Render_ShowsDifferentIconShapePerKind()
    {
        var box = Render<MessageBox>();
        string Icon() => box.Find("[data-testid=message] svg").InnerHtml;
        var hint = Icon();

        TestLevels.Fill(_session, Direction.Left, Direction.Left);
        RunToEnd();
        var error = Icon();

        _runner.Reset();
        _session.Clear();
        TestLevels.Fill(_session, TestLevels.DemoSolution);
        RunToEnd();
        var success = Icon();

        Assert.Equal(3, new[] { error, hint, success }.Distinct().Count());
    }

    private void SetupPizzaRun(int pizzas)
    {
        LevelItem[] items = [new(1, 0, ItemKind.Pizza), new(2, 0, ItemKind.Pizza)];
        _session.Load(new LevelDefinition
        {
            Id = "test-pizza",
            Width = 5,
            Height = 3,
            Start = new Position(0, 0),
            Goal = new Position(2, 0),
            Items = [.. items.Take(pizzas)],
        });
        TestLevels.Fill(_session, Direction.Right, Direction.Right);
        RunToEnd();
    }

    [Theory]
    [InlineData(1, "de-CH", "Geschafft, Max! Du hast die Burg erreicht und 1 Pizza-Stück gefunden.")]
    [InlineData(1, "en", "You did it, Max! You reached the castle and found 1 slice of pizza.")]
    [InlineData(2, "de-CH", "Geschafft, Max! Du hast die Burg erreicht und 2 Pizza-Stücke gefunden.")]
    [InlineData(2, "en", "You did it, Max! You reached the castle and found 2 slices of pizza.")]
    public async Task Render_SuccessWithPizzas_MentionsThem(int pizzas, string culture, string text)
    {
        SetupPizzaRun(pizzas);
        var box = Render<MessageBox>();

        await Services.GetRequiredService<LanguageService>().SetAsync(culture, Xunit.TestContext.Current.CancellationToken);

        box.WaitForAssertion(() => Assert.Equal(text, box.Find("[data-testid=message]").TextContent.Trim()));
    }

    [Fact]
    public void Render_SuccessWithPizzasWhenAllLevelsSolved_AppendsTheAllSolvedSentence()
    {
        var progress = Services.GetRequiredService<LevelProgress>();
        foreach (var level in StandardLevels.All)
        {
            progress.MarkSolved(level.Id);
        }

        SetupPizzaRun(1);
        var box = Render<MessageBox>();

        Assert.Equal(
            "Geschafft, Max! Du hast die Burg erreicht und 1 Pizza-Stück gefunden. Du hast alle Level geschafft!",
            box.Find("[data-testid=message]").TextContent.Trim());
    }
}
