using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Game.UI;
using HeroPath.Modules.Game.UI.Components;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Modules.Game.Tests.Components;

public class BlockPaletteTests : BunitContext
{
    private readonly GameSession _session;

    public BlockPaletteTests()
    {
        Services.AddSharedUI();
        Services.AddGameModule();
        Services.AddSingleton<IAppStorage, InMemoryAppStorage>();
        _session = Services.GetRequiredService<GameSession>();
    }

    private void LockEditing()
    {
        _session.Load(TestLevels.DemoGrid());
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

        Assert.Equal(expected, Assert.Single(_session.Commands).Direction);
    }

    [Fact]
    public void Render_ShowsFourEnabledButtonsWithIcons()
    {
        var palette = Render<BlockPalette>();

        var buttons = palette.FindAll("button[data-testid^=palette-]");
        Assert.Equal(4, buttons.Count);
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
}
