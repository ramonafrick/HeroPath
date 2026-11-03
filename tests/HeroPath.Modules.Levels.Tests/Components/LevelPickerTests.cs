using HeroPath.Modules.Levels.Infrastructure;
using HeroPath.Modules.Levels.UI.Components;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Modules.Levels.Tests.Components;

[Collection("Culture")]
public class LevelPickerTests : CultureBunitContext
{
    public LevelPickerTests()
    {
        Services.AddSharedUI();
        Services.AddSingleton<IAppStorage, InMemoryAppStorage>();
    }

    private IRenderedComponent<LevelPicker> RenderPicker(string selected = "easy-1", Func<string, bool>? solved = null, Action<string>? changed = null) =>
        Render<LevelPicker>(p => p
            .Add(c => c.Levels, StandardLevels.All)
            .Add(c => c.SelectedId, selected)
            .Add(c => c.IsSolved, solved)
            .Add(c => c.SelectedIdChanged, EventCallback.Factory.Create<string>(this, id => changed?.Invoke(id))));

    [Fact]
    public void Render_ListsEveryLevelInOrderWithLocalizedNames()
    {
        var cut = RenderPicker();

        var options = cut.FindAll("[data-testid=level-picker] option");
        Assert.Equal(StandardLevels.All.Select(l => l.Id), options.Select(o => o.GetAttribute("value")));
        Assert.Equal("Freies Feld", options[0].TextContent.Trim());
        Assert.Equal("Level wählen", cut.Find("label").TextContent.Trim());
        Assert.NotNull(cut.Find("option[value=easy-1][selected]"));
    }

    [Fact]
    public void Render_SolvedLevels_GetCheckMarkAndDataAttribute()
    {
        var cut = RenderPicker(solved: id => id == "free-field");

        var solved = cut.Find("[data-testid=level-picker-option-free-field]");
        Assert.Equal("true", solved.GetAttribute("data-solved"));
        Assert.Equal("✓ Freies Feld", solved.TextContent.Trim());
        var open = cut.Find("[data-testid=level-picker-option-easy-1]");
        Assert.Equal("false", open.GetAttribute("data-solved"));
        Assert.DoesNotContain("✓", open.TextContent);
    }

    [Fact]
    public void Change_RaisesCallbackWithTheLevelId()
    {
        string? chosen = null;
        var cut = RenderPicker(changed: id => chosen = id);

        cut.Find("[data-testid=level-picker]").Change("easy-2");

        Assert.Equal("easy-2", chosen);
    }
}
