using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Game.UI;
using HeroPath.Modules.Game.UI.Components;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Modules.Game.Tests.Components;

[Collection("Culture")]
public class ProgramEditorTests : CultureBunitContext
{
    private readonly GameSession _session;

    public ProgramEditorTests()
    {
        Services.AddSharedUI();
        Services.AddGameModule();
        Services.AddSingleton<IAppStorage, InMemoryAppStorage>();
        _session = Services.GetRequiredService<GameSession>();
    }

    private static string[] Paths(IRenderedComponent<ProgramEditor> editor) =>
        [.. editor.FindAll("[data-testid=program-block]").Select(b => b.GetAttribute("data-path")!)];

    private Guid IdAt(params int[] path) => Flatten(_session.Program.Commands)
        .First(c => _session.Program.PathOf(c.Id)!.SequenceEqual(path)).Id;

    private static IEnumerable<Command> Flatten(IEnumerable<Command> commands) =>
        commands.SelectMany(c => c is RepeatCommand r ? new[] { c }.Concat(Flatten(r.Body)) : [c]);

    [Fact]
    public void Render_Empty_ShowsNoBlocks()
    {
        var editor = Render<ProgramEditor>();

        Assert.Empty(editor.FindAll("[data-testid=program-block]"));
        Assert.NotNull(editor.Find("[data-testid=program-placeholder]"));
        Assert.Equal("true", editor.Find("[data-testid=program]").GetAttribute("data-empty"));
        Assert.Equal(string.Empty, editor.Find("[data-testid=program-placeholder]").TextContent.Trim());
    }

    [Fact]
    public void Render_WithBlock_HidesPlaceholder()
    {
        _session.Add(Direction.Up);

        var editor = Render<ProgramEditor>();

        Assert.Empty(editor.FindAll("[data-testid=program-placeholder]"));
        Assert.Null(editor.Find("[data-testid=program]").GetAttribute("data-empty"));
    }

    [Fact]
    public void Render_RepeatHeader_ShowsRepeatIconOnlyOnTopLevel()
    {
        _session.AddRepeat();
        _session.Select(_session.Program.Commands[0].Id);
        _session.AddRepeat();

        var editor = Render<ProgramEditor>();

        Assert.Single(editor.Find("[data-testid=program-select-1]").QuerySelectorAll(".program-icon"));
        Assert.Empty(editor.Find("[data-testid='program-select-1.1']").QuerySelectorAll(".program-icon"));
    }

    [Fact]
    public void Render_WithBlocks_ShowsNumberedBlocksInOrder()
    {
        _session.Add(Direction.Up);
        _session.Add(Direction.Right);
        _session.Add(Direction.Down);

        var editor = Render<ProgramEditor>();

        var blocks = editor.FindAll("[data-testid=program-block]");
        Assert.Equal(["1", "2", "3"], blocks.Select(b => b.GetAttribute("data-path")));
        Assert.Equal(["move", "move", "move"], blocks.Select(b => b.GetAttribute("data-kind")));
        Assert.Equal(["up", "right", "down"], blocks.Select(b => b.GetAttribute("data-direction")));
        Assert.Equal(_session.Program.Commands.Select(c => c.Id.ToString()), blocks.Select(b => b.GetAttribute("data-command-id")));
        Assert.Equal(["1", "2", "3"], blocks.Select(b => b.QuerySelector(".program-number")!.TextContent));
    }

    [Fact]
    public void Render_NestedRepeats_ShowsHierarchicalNumbersAndBodies()
    {
        _session.Add(Direction.Up);
        _session.AddRepeat();
        _session.Select(IdAt(2));
        _session.Add(Direction.Left);
        _session.AddRepeat();
        _session.Select(IdAt(2, 2));
        _session.Add(Direction.Down);

        var editor = Render<ProgramEditor>();

        Assert.Equal(["1", "2", "2.1", "2.2", "2.2.1"], Paths(editor));
        Assert.Equal(["move", "repeat", "move", "repeat", "move"],
            editor.FindAll("[data-testid=program-block]").Select(b => b.GetAttribute("data-kind")));
        Assert.NotNull(editor.Find("[data-testid=repeat-body-2] [data-testid='repeat-body-2.2']"));
        Assert.Equal("3", editor.Find("[data-testid=repeat-count-2]").TextContent);
        Assert.Equal("3", editor.Find("[data-testid='repeat-count-2.2']").TextContent);
    }

    [Fact]
    public void Click_SelectsBlockAndTogglesOff()
    {
        _session.Add(Direction.Up);
        _session.Add(Direction.Right);
        var editor = Render<ProgramEditor>();

        editor.Find("[data-testid=program-select-2]").Click();

        Assert.Equal(_session.Program.Commands[1].Id, _session.SelectedId);
        Assert.Equal("true", editor.Find("[data-testid=program-select-2]").GetAttribute("aria-pressed"));
        Assert.Equal("false", editor.Find("[data-testid=program-select-1]").GetAttribute("aria-pressed"));
        Assert.Equal(["false", "true"], editor.FindAll("[data-testid=program-block]").Select(b => b.GetAttribute("data-selected")));

        editor.Find("[data-testid=program-select-2]").Click();

        Assert.Null(_session.SelectedId);
        Assert.Equal("false", editor.Find("[data-testid=program-select-2]").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void ActionButtons_AppearOnlyOnSelectedBlock()
    {
        _session.Add(Direction.Up);
        _session.Add(Direction.Right);
        var editor = Render<ProgramEditor>();
        Assert.Empty(editor.FindAll("[data-testid=program-up]"));
        Assert.Empty(editor.FindAll("[data-testid=program-down]"));
        Assert.Empty(editor.FindAll("[data-testid=program-remove]"));

        editor.Find("[data-testid=program-select-1]").Click();

        var block = editor.Find("[data-path='1']");
        Assert.NotNull(block.QuerySelector("[data-testid=program-up]"));
        Assert.NotNull(block.QuerySelector("[data-testid=program-down]"));
        Assert.NotNull(block.QuerySelector("[data-testid=program-remove]"));
        Assert.Single(editor.FindAll("[data-testid=program-remove]"));
    }

    [Fact]
    public void MoveButtons_AtEdges_AreDisabled()
    {
        _session.Add(Direction.Up);
        _session.Add(Direction.Right);
        var editor = Render<ProgramEditor>();

        editor.Find("[data-testid=program-select-1]").Click();
        Assert.True(editor.Find("[data-testid=program-up]").HasAttribute("disabled"));
        Assert.False(editor.Find("[data-testid=program-down]").HasAttribute("disabled"));

        editor.Find("[data-testid=program-select-2]").Click();
        Assert.False(editor.Find("[data-testid=program-up]").HasAttribute("disabled"));
        Assert.True(editor.Find("[data-testid=program-down]").HasAttribute("disabled"));
    }

    [Fact]
    public void MoveUpAndDown_ReordersAndKeepsSelection()
    {
        _session.Add(Direction.Up);
        _session.Add(Direction.Right);
        _session.Add(Direction.Down);
        var editor = Render<ProgramEditor>();
        editor.Find("[data-testid=program-select-3]").Click();

        editor.Find("[data-testid=program-up]").Click();

        Assert.Equal(["up", "down", "right"], editor.FindAll("[data-testid=program-block]").Select(b => b.GetAttribute("data-direction")));
        Assert.Equal("true", editor.Find("[data-path='2']").GetAttribute("data-selected"));

        editor.Find("[data-testid=program-down]").Click();

        Assert.Equal(["up", "right", "down"], editor.FindAll("[data-testid=program-block]").Select(b => b.GetAttribute("data-direction")));
        Assert.Equal("true", editor.Find("[data-path='3']").GetAttribute("data-selected"));
    }

    [Fact]
    public void MoveUp_FirstInBody_LeavesBody()
    {
        _session.AddRepeat();
        _session.Select(_session.Program.Commands[0].Id);
        _session.Add(Direction.Left);
        var editor = Render<ProgramEditor>();
        editor.Find("[data-testid='program-select-1.1']").Click();

        editor.Find("[data-testid=program-up]").Click();

        Assert.Equal(["1", "2"], Paths(editor));
        Assert.Equal(["move", "repeat"], editor.FindAll("[data-testid=program-block]").Select(b => b.GetAttribute("data-kind")));
    }

    [Fact]
    public void RemoveButton_RemovesSelectedBlockAndClearsSelection()
    {
        _session.Add(Direction.Up);
        _session.Add(Direction.Right);
        _session.Add(Direction.Down);
        var editor = Render<ProgramEditor>();
        editor.Find("[data-testid=program-select-2]").Click();

        editor.Find("[data-testid=program-remove]").Click();

        Assert.Equal(["up", "down"], editor.FindAll("[data-testid=program-block]").Select(b => b.GetAttribute("data-direction")));
        Assert.Null(_session.SelectedId);
        Assert.Empty(editor.FindAll("[data-testid=program-remove]"));
    }

    [Fact]
    public void RemoveButton_OnRepeat_RemovesBodyToo()
    {
        _session.AddRepeat();
        _session.Select(_session.Program.Commands[0].Id);
        _session.Add(Direction.Left);
        _session.Add(Direction.Up);
        var editor = Render<ProgramEditor>();
        Assert.Equal(3, editor.FindAll("[data-testid=program-block]").Count);

        editor.Find("[data-testid=program-remove]").Click();

        Assert.Empty(editor.FindAll("[data-testid=program-block]"));
    }

    [Fact]
    public void ActionButtons_HaveAccessibleNamesWithBlockNumber()
    {
        _session.Add(Direction.Up);
        _session.Add(Direction.Up);
        var editor = Render<ProgramEditor>();
        editor.Find("[data-testid=program-select-2]").Click();

        Assert.Equal("Baustein 2 entfernen", editor.Find("[data-testid=program-remove]").GetAttribute("aria-label"));
        Assert.Equal("Baustein 2 nach oben", editor.Find("[data-testid=program-up]").GetAttribute("aria-label"));
        Assert.Equal("Baustein 2 nach unten", editor.Find("[data-testid=program-down]").GetAttribute("aria-label"));
        Assert.Equal("Baustein 2: Hoch", editor.Find("[data-testid=program-select-2]").GetAttribute("aria-label"));
    }

    [Fact]
    public void ActionButtons_InEnglish_AreTranslated()
    {
        using var scope = new CultureScope("en");
        _session.Add(Direction.Up);
        var editor = Render<ProgramEditor>();
        editor.Find("[data-testid=program-select-1]").Click();

        Assert.Equal("Remove block 1", editor.Find("[data-testid=program-remove]").GetAttribute("aria-label"));
        Assert.Equal("Block 1: Up", editor.Find("[data-testid=program-select-1]").GetAttribute("aria-label"));
    }

    [Fact]
    public void RepeatHeader_PlusAndMinus_ChangeCountWithinLimits()
    {
        _session.AddRepeat();
        var editor = Render<ProgramEditor>();

        editor.Find("[data-testid=repeat-plus-1]").Click();
        Assert.Equal("4", editor.Find("[data-testid=repeat-count-1]").TextContent);

        editor.Find("[data-testid=repeat-minus-1]").Click();
        editor.Find("[data-testid=repeat-minus-1]").Click();
        editor.Find("[data-testid=repeat-minus-1]").Click();
        Assert.Equal("1", editor.Find("[data-testid=repeat-count-1]").TextContent);
        Assert.True(editor.Find("[data-testid=repeat-minus-1]").HasAttribute("disabled"));
        Assert.False(editor.Find("[data-testid=repeat-plus-1]").HasAttribute("disabled"));
    }

    [Fact]
    public void RepeatHeader_AtMaximum_DisablesPlus()
    {
        _session.AddRepeat();
        _session.SetRepeatCount(_session.Program.Commands[0].Id, ProgramLimits.MaxRepeatCount);

        var editor = Render<ProgramEditor>();

        Assert.True(editor.Find("[data-testid=repeat-plus-1]").HasAttribute("disabled"));
        Assert.False(editor.Find("[data-testid=repeat-minus-1]").HasAttribute("disabled"));
    }

    [Fact]
    public void RepeatHeader_CountChange_KeepsId()
    {
        _session.AddRepeat();
        var id = _session.Program.Commands[0].Id;
        var editor = Render<ProgramEditor>();

        editor.Find("[data-testid=repeat-plus-1]").Click();

        Assert.Equal(id.ToString(), editor.Find("[data-testid=program-block]").GetAttribute("data-command-id"));
    }

    [Fact]
    public void InsertTarget_FollowsSelection()
    {
        _session.AddRepeat();
        _session.Add(Direction.Up);
        var editor = Render<ProgramEditor>();
        Assert.Equal("root", editor.Find("[data-testid=insert-target]").GetAttribute("data-target"));
        Assert.Equal("Neue Bausteine kommen ans Programmende", editor.Find("[data-testid=insert-target]").TextContent.Trim());

        editor.Find("[data-testid=program-select-1]").Click();

        Assert.Equal("1", editor.Find("[data-testid=insert-target]").GetAttribute("data-target"));
        Assert.Contains("Wiederhole-Block 1", editor.Find("[data-testid=insert-target]").TextContent);

        editor.Find("[data-testid=program-select-1]").Click();

        Assert.Equal("root", editor.Find("[data-testid=insert-target]").GetAttribute("data-target"));
    }

    [Fact]
    public void ClearButton_RemovesAllBlocks()
    {
        _session.Add(Direction.Up);
        _session.Add(Direction.Left);
        var editor = Render<ProgramEditor>();

        editor.Find("[data-testid=program-clear]").Click();

        Assert.Empty(editor.FindAll("[data-testid=program-block]"));
    }

    [Fact]
    public void Render_NotEditable_DisablesEditButtons()
    {
        _session.Load(TestLevels.Demo);
        _session.AddRepeat();
        _session.Select(_session.Program.Commands[0].Id);
        _session.Add(Direction.Up);
        _session.Select(null);
        Services.GetRequiredService<ExecutionRunner>().StepOnce();

        var editor = Render<ProgramEditor>();

        Assert.True(editor.Find("[data-testid=program-clear]").HasAttribute("disabled"));
        Assert.True(editor.Find("[data-testid=repeat-plus-1]").HasAttribute("disabled"));
        Assert.True(editor.Find("[data-testid=repeat-minus-1]").HasAttribute("disabled"));
        Assert.Empty(editor.FindAll("[data-testid=program-remove]"));
    }

    [Fact]
    public void Click_WhileLocked_DoesNotSelect()
    {
        _session.Load(TestLevels.Demo);
        _session.Add(Direction.Up);
        Services.GetRequiredService<ExecutionRunner>().StepOnce();
        var editor = Render<ProgramEditor>();

        editor.Find("[data-testid=program-select-1]").Click();

        Assert.Null(_session.SelectedId);
        Assert.Empty(editor.FindAll("[data-testid=program-remove]"));
    }

    [Fact]
    public void SessionAdd_ReRendersNewBlock()
    {
        var editor = Render<ProgramEditor>();

        _session.Add(Direction.Left);

        editor.WaitForAssertion(() => Assert.Single(editor.FindAll("[data-testid=program-block]")));
    }

    [Fact]
    public void Render_AtLimit_ShowsAllBlocks()
    {
        for (var i = 0; i < ProgramLimits.MaxProgramCommands; i++)
        {
            _session.Add(Direction.Up);
        }

        var editor = Render<ProgramEditor>();

        Assert.Equal(ProgramLimits.MaxProgramCommands, editor.FindAll("[data-testid=program-block]").Count);
    }

    [Fact]
    public void Render_BeforeRun_MarksNoBlock()
    {
        _session.Add(Direction.Up);

        var editor = Render<ProgramEditor>();

        Assert.Empty(editor.FindAll("[data-current]"));
        Assert.Empty(editor.FindAll("[data-error]"));
    }

    [Fact]
    public void Debug_MarksCurrentBlockAndMovesWithEachStep()
    {
        _session.Load(TestLevels.Demo);
        TestLevels.Fill(_session, Direction.Up, Direction.Up, Direction.Right);
        var runner = Services.GetRequiredService<ExecutionRunner>();
        var editor = Render<ProgramEditor>();

        runner.StepOnce();
        runner.StepOnce();

        editor.WaitForAssertion(() =>
        {
            var current = Assert.Single(editor.FindAll("[data-current=true]"));
            Assert.Equal("2", current.GetAttribute("data-path"));
        });
        Assert.Empty(editor.FindAll("[data-error]"));
    }

    [Fact]
    public void Debug_MarksMoveInsideRepeatBody()
    {
        _session.Load(TestLevels.Demo);
        _session.AddRepeat();
        _session.Select(_session.Program.Commands[0].Id);
        _session.Add(Direction.Up);
        _session.Add(Direction.Up);
        var runner = Services.GetRequiredService<ExecutionRunner>();
        var editor = Render<ProgramEditor>();

        runner.StepOnce();
        runner.StepOnce();

        editor.WaitForAssertion(() =>
        {
            var current = Assert.Single(editor.FindAll("[data-current=true]"));
            Assert.Equal("1.2", current.GetAttribute("data-path"));
            Assert.Equal("move", current.GetAttribute("data-kind"));
        });
    }

    [Fact]
    public void Failure_MarksFailedBlockWithError()
    {
        _session.Load(TestLevels.Demo);
        TestLevels.Fill(_session, Direction.Right, Direction.Right);
        var runner = Services.GetRequiredService<ExecutionRunner>();
        var editor = Render<ProgramEditor>();

        runner.StepOnce();
        runner.StepOnce();

        editor.WaitForAssertion(() =>
        {
            var failed = Assert.Single(editor.FindAll("[data-error=true]"));
            Assert.Equal("2", failed.GetAttribute("data-path"));
            Assert.Equal("true", failed.GetAttribute("data-current"));
        });
    }

    [Fact]
    public void Failure_InsideRepeat_MarksFailedBlockWithError()
    {
        _session.Load(TestLevels.Demo);
        _session.AddRepeat();
        _session.Select(_session.Program.Commands[0].Id);
        _session.Add(Direction.Right);
        var runner = Services.GetRequiredService<ExecutionRunner>();
        var editor = Render<ProgramEditor>();

        runner.StepOnce();
        runner.StepOnce();

        editor.WaitForAssertion(() =>
        {
            var failed = Assert.Single(editor.FindAll("[data-error=true]"));
            Assert.Equal("1.1", failed.GetAttribute("data-path"));
        });
    }

    [Fact]
    public void Reset_ClearsMarkers()
    {
        _session.Load(TestLevels.Demo);
        _session.Add(Direction.Up);
        var runner = Services.GetRequiredService<ExecutionRunner>();
        var editor = Render<ProgramEditor>();
        runner.StepOnce();

        runner.Reset();

        editor.WaitForAssertion(() => Assert.Empty(editor.FindAll("[data-current]")));
    }

    [Fact]
    public void StartingRun_ClearsSelection()
    {
        _session.Load(TestLevels.Demo);
        _session.Add(Direction.Up);
        _session.Select(_session.Program.Commands[0].Id);
        var editor = Render<ProgramEditor>();
        Assert.Single(editor.FindAll("[data-testid=program-remove]"));

        Services.GetRequiredService<ExecutionRunner>().StepOnce();

        editor.WaitForAssertion(() => Assert.Empty(editor.FindAll("[data-testid=program-remove]")));
        Assert.Null(_session.SelectedId);
    }
}
