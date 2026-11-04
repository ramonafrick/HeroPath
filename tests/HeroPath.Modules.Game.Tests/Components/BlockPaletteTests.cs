using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Game.UI;
using HeroPath.Modules.Game.UI.Components;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Modules.Game.Tests.Components;

[Collection("Culture")]
public class BlockPaletteTests : CultureBunitContext
{
    private readonly GameSession _session;

    public BlockPaletteTests()
    {
        Services.AddSharedUI();
        Services.AddFakeDocumentLanguage();
        Services.AddGameModule();
        Services.AddSingleton<IAppStorage, InMemoryAppStorage>();
        _session = Services.GetRequiredService<GameSession>();
    }

    private void LockEditing()
    {
        _session.Load(TestLevels.Demo);
        _session.Add(Direction.Up);
        Services.GetRequiredService<ExecutionRunner>().StepOnce();
    }

    [Theory]
    [InlineData("up", Direction.Up)]
    [InlineData("down", Direction.Down)]
    [InlineData("left", Direction.Left)]
    [InlineData("right", Direction.Right)]
    public void Click_AddsBlockWithDirection(string testId, Direction expected)
    {
        var palette = Render<BlockPalette>();

        palette.Find($"[data-testid=palette-{testId}]").Click();

        Assert.Equal([expected], TestLevels.Directions(_session));
    }

    [Fact]
    public void Render_ShowsFiveEnabledButtonsWithIcons()
    {
        var palette = Render<BlockPalette>();

        var buttons = palette.FindAll("button[data-testid^=palette-]");
        Assert.Equal(5, buttons.Count);
        Assert.All(buttons, button => Assert.False(button.HasAttribute("disabled")));
        Assert.All(buttons, button => Assert.NotNull(button.QuerySelector("svg[aria-hidden=true]")));
    }

    [Fact]
    public void Render_NotEditable_DisablesButtons()
    {
        LockEditing();

        var palette = Render<BlockPalette>();

        Assert.All(palette.FindAll("button"), button => Assert.True(button.HasAttribute("disabled")));
    }

    [Fact]
    public void Render_AtLimit_DisablesButtons()
    {
        for (var i = 0; i < ProgramLimits.MaxProgramCommands; i++)
        {
            _session.Add(Direction.Up);
        }

        var palette = Render<BlockPalette>();

        Assert.All(palette.FindAll("button"), button => Assert.True(button.HasAttribute("disabled")));
    }

    [Fact]
    public void SessionChange_ReRendersDisabledState()
    {
        var palette = Render<BlockPalette>();

        LockEditing();

        palette.WaitForAssertion(() =>
            Assert.True(palette.Find("[data-testid=palette-up]").HasAttribute("disabled")));
    }

    [Fact]
    public void Render_ShowsRepeatButton()
    {
        var palette = Render<BlockPalette>();

        var button = palette.Find("[data-testid=palette-repeat]");
        Assert.False(button.HasAttribute("disabled"));
        Assert.Contains("Wiederhole", button.TextContent);
        Assert.Equal(5, palette.FindAll("button[data-testid^=palette-]").Count);
    }

    [Fact]
    public void Render_InEnglish_ShowsRepeatText()
    {
        using var scope = new CultureScope("en");

        var palette = Render<BlockPalette>();

        Assert.Contains("Repeat", palette.Find("[data-testid=palette-repeat]").TextContent);
    }

    [Fact]
    public void RepeatClick_AddsEmptyRepeatWithCountThree()
    {
        var palette = Render<BlockPalette>();

        palette.Find("[data-testid=palette-repeat]").Click();

        var repeat = Assert.IsType<RepeatCommand>(Assert.Single(_session.Program.Commands));
        Assert.Equal(3, repeat.Count);
        Assert.Empty(repeat.Body);
    }

    [Fact]
    public void Click_WithRepeatSelected_AddsIntoBody()
    {
        _session.AddRepeat();
        _session.Select(_session.Program.Commands[0].Id);
        var palette = Render<BlockPalette>();

        palette.Find("[data-testid=palette-left]").Click();

        var repeat = Assert.IsType<RepeatCommand>(Assert.Single(_session.Program.Commands));
        Assert.Equal(Direction.Left, Assert.IsType<MoveCommand>(Assert.Single(repeat.Body)).Direction);
    }

    [Fact]
    public void RepeatButton_InNestedTarget_IsDisabledButMovesStayEnabled()
    {
        _session.AddRepeat();
        _session.Select(_session.Program.Commands[0].Id);
        _session.AddRepeat();
        var inner = ((RepeatCommand)_session.Program.Commands[0]).Body[0];
        _session.Select(inner.Id);

        var palette = Render<BlockPalette>();

        Assert.True(palette.Find("[data-testid=palette-repeat]").HasAttribute("disabled"));
        Assert.False(palette.Find("[data-testid=palette-up]").HasAttribute("disabled"));
    }

    [Fact]
    public void SelectionChange_ReRendersRepeatButtonState()
    {
        _session.AddRepeat();
        _session.Select(_session.Program.Commands[0].Id);
        _session.AddRepeat();
        var palette = Render<BlockPalette>();
        Assert.False(palette.Find("[data-testid=palette-repeat]").HasAttribute("disabled"));

        _session.Select(((RepeatCommand)_session.Program.Commands[0]).Body[0].Id);

        palette.WaitForAssertion(() =>
            Assert.True(palette.Find("[data-testid=palette-repeat]").HasAttribute("disabled")));
    }

    [Fact]
    public void Render_FullProgram_DisablesRepeatButton()
    {
        for (var i = 0; i < ProgramLimits.MaxProgramCommands; i++)
        {
            _session.Add(Direction.Up);
        }

        var palette = Render<BlockPalette>();

        Assert.True(palette.Find("[data-testid=palette-repeat]").HasAttribute("disabled"));
    }
}
