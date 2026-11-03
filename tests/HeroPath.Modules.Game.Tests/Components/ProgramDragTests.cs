using AngleSharp.Dom;
using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Game.UI;
using HeroPath.Modules.Game.UI.Components;
using HeroPath.Modules.Game.UI.Services;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Modules.Game.Tests.Components;

[Collection("Culture")]
public class ProgramDragTests : CultureBunitContext
{
    private readonly GameSession _session;
    private readonly ProgramDragState _drag;

    public ProgramDragTests()
    {
        Services.AddSharedUI();
        Services.AddGameModule();
        Services.AddSingleton<IAppStorage, InMemoryAppStorage>();
        _session = Services.GetRequiredService<GameSession>();
        _drag = Services.GetRequiredService<ProgramDragState>();
    }

    private IRenderedComponent<BlockPalette> RenderPalette() => Render<BlockPalette>();

    private static IElement Zone(IRenderedComponent<ProgramEditor> editor, string slot) =>
        editor.Find($"[data-testid=drop-zone][data-slot='{slot}']");

    private static IElement Row(IRenderedComponent<ProgramEditor> editor, string path) =>
        editor.Find($"[data-testid='block-row-{path}']");

    private Guid IdAt(params int[] path) => Flatten(_session.Program.Commands)
        .First(c => _session.Program.PathOf(c.Id)!.SequenceEqual(path)).Id;

    private static IEnumerable<Command> Flatten(IEnumerable<Command> commands) =>
        commands.SelectMany(c => c is RepeatCommand r ? new[] { c }.Concat(Flatten(r.Body)) : [c]);

    private string[] Shape() => [.. _session.Program.Commands.SelectMany(Describe)];

    private static IEnumerable<string> Describe(Command command) => command switch
    {
        RepeatCommand r => new[] { "R" }.Concat(r.Body.SelectMany(Describe)).Append("/"),
        MoveCommand m => [m.Direction.ToString()[0].ToString()],
        _ => [],
    };

    [Fact]
    public void Render_ShowsZonesBetweenBlocksAndAtEnds()
    {
        _session.Add(Direction.Up);
        _session.AddRepeat();

        var editor = Render<ProgramEditor>();

        var slots = editor.FindAll("[data-testid=drop-zone]").Select(z => z.GetAttribute("data-slot"));
        Assert.Equal(["root:0", "root:1", "2:0", "root:2"], slots);
    }

    [Fact]
    public void Zones_HaveNoValidity_WhenNotDragging()
    {
        _session.Add(Direction.Up);

        var editor = Render<ProgramEditor>();

        Assert.All(editor.FindAll("[data-testid=drop-zone]"), z => Assert.Null(z.GetAttribute("data-valid")));
    }

    [Fact]
    public void DragFromPalette_DropOnZone_InsertsAtSlot()
    {
        _session.Add(Direction.Up);
        _session.Add(Direction.Down);
        var palette = RenderPalette();
        var editor = Render<ProgramEditor>();

        palette.Find("[data-testid=palette-left]").DragStart();
        Assert.Equal("true", Zone(editor, "root:1").GetAttribute("data-valid"));
        Zone(editor, "root:1").Drop();

        Assert.Equal(["U", "L", "D"], Shape());
        Assert.False(_drag.IsDragging);
        Assert.Null(Zone(editor, "root:0").GetAttribute("data-valid"));
    }

    [Fact]
    public void DragRepeatFromPalette_DropIntoRepeatBody_Nests()
    {
        _session.AddRepeat();
        var palette = RenderPalette();
        var editor = Render<ProgramEditor>();

        palette.Find("[data-testid=palette-repeat]").DragStart();
        Zone(editor, "1:0").Drop();

        Assert.Equal(["R", "R", "/", "/"], Shape());
    }

    [Fact]
    public void DragRepeat_ToDepthThree_IsInvalidAndChangesNothing()
    {
        _session.AddRepeat();
        _session.AddRepeat(new BlockSlot(IdAt(1), 0));
        var palette = RenderPalette();
        var editor = Render<ProgramEditor>();

        palette.Find("[data-testid=palette-repeat]").DragStart();

        Assert.Equal("false", Zone(editor, "1.1:0").GetAttribute("data-valid"));
        Zone(editor, "1.1:0").Drop();
        Assert.Equal(["R", "R", "/", "/"], Shape());
        Assert.False(_drag.IsDragging);
    }

    [Fact]
    public void DragBlock_ReorderWithinList()
    {
        _session.Add(Direction.Up);
        _session.Add(Direction.Down);
        _session.Add(Direction.Left);
        var editor = Render<ProgramEditor>();

        Row(editor, "1").DragStart();
        Zone(editor, "root:3").Drop();

        Assert.Equal(["D", "L", "U"], Shape());
    }

    [Fact]
    public void DragBlock_IntoRepeatBodyAndBackOut()
    {
        _session.Add(Direction.Up);
        _session.AddRepeat();
        var editor = Render<ProgramEditor>();

        Row(editor, "1").DragStart();
        Zone(editor, "2:0").Drop();
        Assert.Equal(["R", "U", "/"], Shape());

        Row(editor, "1.1").DragStart();
        Zone(editor, "root:1").Drop();
        Assert.Equal(["R", "/", "U"], Shape());
    }

    [Fact]
    public void DragRepeat_IntoOwnBody_IsInvalidAndChangesNothing()
    {
        _session.AddRepeat();
        _session.Add(Direction.Up, new BlockSlot(IdAt(1), 0));
        var editor = Render<ProgramEditor>();

        Row(editor, "1").DragStart();

        Assert.Equal("false", Zone(editor, "1:0").GetAttribute("data-valid"));
        Zone(editor, "1:0").Drop();
        Assert.Equal(["R", "U", "/"], Shape());
    }

    [Fact]
    public void DragBlock_ToOwnPosition_ChangesNothing()
    {
        _session.Add(Direction.Up);
        _session.Add(Direction.Down);
        var editor = Render<ProgramEditor>();

        Row(editor, "1").DragStart();
        Assert.Equal("false", Zone(editor, "root:0").GetAttribute("data-valid"));
        Zone(editor, "root:0").Drop();

        Assert.Equal(["U", "D"], Shape());
        Assert.False(_drag.IsDragging);
    }

    [Fact]
    public void DragBlockOntoPalette_RemovesBlockWithBody()
    {
        _session.Add(Direction.Up);
        _session.AddRepeat();
        _session.Add(Direction.Down, new BlockSlot(IdAt(2), 0));
        var palette = RenderPalette();
        var editor = Render<ProgramEditor>();

        Row(editor, "2").DragStart();
        Assert.NotNull(palette.Find("[data-testid=palette-delete]"));
        palette.Find("[data-testid=palette]").Drop();

        Assert.Equal(["U"], Shape());
        Assert.Empty(palette.FindAll("[data-testid=palette-delete]"));
    }

    [Fact]
    public void DropOnPalette_OfNewBlock_ChangesNothing()
    {
        _session.Add(Direction.Up);
        var palette = RenderPalette();

        palette.Find("[data-testid=palette-left]").DragStart();
        Assert.Empty(palette.FindAll("[data-testid=palette-delete]"));
        palette.Find("[data-testid=palette]").Drop();

        Assert.Equal(["U"], Shape());
        Assert.False(_drag.IsDragging);
    }

    [Fact]
    public void DragEnd_ClearsDragState()
    {
        _session.Add(Direction.Up);
        var editor = Render<ProgramEditor>();

        Row(editor, "1").DragStart();
        Assert.True(_drag.IsDragging);
        Row(editor, "1").DragEnd();

        Assert.False(_drag.IsDragging);
        Assert.Null(Zone(editor, "root:0").GetAttribute("data-valid"));
    }

    [Fact]
    public void Locked_DragStartDoesNothing()
    {
        _session.Load(TestLevels.DemoGrid());
        _session.Add(Direction.Up);
        Services.GetRequiredService<ExecutionRunner>().StepOnce();
        var editor = Render<ProgramEditor>();

        Row(editor, "1").DragStart();

        Assert.False(_drag.IsDragging);
        Assert.Equal("false", Row(editor, "1").GetAttribute("draggable"));
    }

    [Fact]
    public void RunStartsBetweenDragStartAndDrop_DropDoesNothing()
    {
        _session.Load(TestLevels.DemoGrid());
        _session.Add(Direction.Up);
        _session.Add(Direction.Up);
        var palette = RenderPalette();
        var editor = Render<ProgramEditor>();

        palette.Find("[data-testid=palette-left]").DragStart();
        Services.GetRequiredService<ExecutionRunner>().StepOnce();
        Assert.Equal("false", Zone(editor, "root:1").GetAttribute("data-valid"));
        Zone(editor, "root:1").Drop();

        Assert.Equal(["U", "U"], Shape());
        Assert.False(_drag.IsDragging);
    }

    [Fact]
    public void Drag_DoesNotChangeSelection()
    {
        _session.Add(Direction.Up);
        _session.Add(Direction.Down);
        var selected = _session.Program.Commands[0].Id;
        _session.Select(selected);
        var editor = Render<ProgramEditor>();

        Row(editor, "2").DragStart();
        Zone(editor, "root:0").Drop();

        Assert.Equal(["D", "U"], Shape());
        Assert.Equal(selected, _session.SelectedId);
    }
}
