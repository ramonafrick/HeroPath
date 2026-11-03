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

    [Fact]
    public void Render_Empty_ShowsHintAndNoBlocks()
    {
        var editor = Render<ProgramEditor>();

        Assert.NotNull(editor.Find("[data-testid=program-empty]"));
        Assert.Empty(editor.FindAll("[data-testid=program-block]"));
    }

    [Fact]
    public void Render_WithBlocks_ShowsNumberedBlocksInOrder()
    {
        _session.Add(Direction.Up);
        _session.Add(Direction.Right);
        _session.Add(Direction.Down);

        var editor = Render<ProgramEditor>();

        var blocks = editor.FindAll("[data-testid=program-block]");
        Assert.Equal(3, blocks.Count);
        Assert.Equal(["0", "1", "2"], blocks.Select(b => b.GetAttribute("data-index")));
        Assert.Equal(["up", "right", "down"], blocks.Select(b => b.GetAttribute("data-direction")));
        Assert.Equal(_session.Commands.Select(c => c.Id.ToString()), blocks.Select(b => b.GetAttribute("data-command-id")));
        Assert.Equal(["1", "2", "3"], blocks.Select(b => b.QuerySelector(".program-number")!.TextContent));
        Assert.Empty(editor.FindAll("[data-testid=program-empty]"));
    }

    [Fact]
    public void RemoveButton_RemovesThatBlock()
    {
        _session.Add(Direction.Up);
        _session.Add(Direction.Right);
        _session.Add(Direction.Down);
        var editor = Render<ProgramEditor>();

        editor.Find("[data-testid=program-remove-1]").Click();

        Assert.Equal(["up", "down"], editor.FindAll("[data-testid=program-block]").Select(b => b.GetAttribute("data-direction")));
        Assert.Equal([Direction.Up, Direction.Down], _session.Commands.Select(c => c.Direction));
    }

    [Fact]
    public void RemoveButton_HasAccessibleNameWithBlockNumber()
    {
        _session.Add(Direction.Up);
        _session.Add(Direction.Up);

        var editor = Render<ProgramEditor>();

        Assert.Equal("Baustein 2 entfernen", editor.Find("[data-testid=program-remove-1]").GetAttribute("aria-label"));
    }

    [Fact]
    public void ClearButton_RemovesAllBlocksAndShowsHint()
    {
        _session.Add(Direction.Up);
        _session.Add(Direction.Left);
        var editor = Render<ProgramEditor>();

        editor.Find("[data-testid=program-clear]").Click();

        Assert.Empty(editor.FindAll("[data-testid=program-block]"));
        Assert.NotNull(editor.Find("[data-testid=program-empty]"));
    }

    [Fact]
    public void Render_NotEditable_DisablesRemoveAndClear()
    {
        _session.Load(TestLevels.DemoGrid());
        _session.Add(Direction.Up);
        Services.GetRequiredService<ExecutionRunner>().StepOnce();

        var editor = Render<ProgramEditor>();

        Assert.True(editor.Find("[data-testid=program-clear]").HasAttribute("disabled"));
        Assert.True(editor.Find("[data-testid=program-remove-0]").HasAttribute("disabled"));
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
        _session.Load(TestLevels.DemoGrid());
        TestLevels.Fill(_session, Direction.Up, Direction.Up, Direction.Right);
        var runner = Services.GetRequiredService<ExecutionRunner>();
        var editor = Render<ProgramEditor>();

        runner.StepOnce();
        runner.StepOnce();

        editor.WaitForAssertion(() =>
        {
            var current = Assert.Single(editor.FindAll("[data-current=true]"));
            Assert.Equal("1", current.GetAttribute("data-index"));
        });
        Assert.Empty(editor.FindAll("[data-error]"));
    }

    [Fact]
    public void Failure_MarksFailedBlockWithError()
    {
        _session.Load(TestLevels.DemoGrid());
        TestLevels.Fill(_session, Direction.Right, Direction.Right);
        var runner = Services.GetRequiredService<ExecutionRunner>();
        var editor = Render<ProgramEditor>();

        runner.StepOnce();
        runner.StepOnce();

        editor.WaitForAssertion(() =>
        {
            var failed = Assert.Single(editor.FindAll("[data-error=true]"));
            Assert.Equal("1", failed.GetAttribute("data-index"));
            Assert.Equal("true", failed.GetAttribute("data-current"));
        });
    }

    [Fact]
    public void Reset_ClearsMarkers()
    {
        _session.Load(TestLevels.DemoGrid());
        _session.Add(Direction.Up);
        var runner = Services.GetRequiredService<ExecutionRunner>();
        var editor = Render<ProgramEditor>();
        runner.StepOnce();

        runner.Reset();

        editor.WaitForAssertion(() => Assert.Empty(editor.FindAll("[data-current]")));
    }
}
