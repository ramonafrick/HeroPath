using HeroPath.Modules.Levels.Domain;
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

    private static readonly LevelDefinition[] _custom =
    [
        CustomLevelSamples.Valid("custom-a", "Mein Burgweg"),
        CustomLevelSamples.Valid("custom-b", "<b>Fett</b> & \"Quote\""),
    ];

    private IRenderedComponent<LevelPicker> RenderMixedPicker(string selected = "easy-1", Func<string, bool>? solved = null) =>
        Render<LevelPicker>(p => p
            .Add(c => c.Levels, [.. StandardLevels.All, .. _custom])
            .Add(c => c.SelectedId, selected)
            .Add(c => c.IsSolved, solved)
            .Add(c => c.IsCustom, id => id.StartsWith("custom-", StringComparison.Ordinal)));

    [Fact]
    public void Render_WithoutCustomLevels_HasNoGroup()
    {
        var cut = RenderPicker();

        Assert.Empty(cut.FindAll("[data-testid=level-picker-group-custom]"));
        Assert.Empty(cut.FindAll("optgroup"));
    }

    [Fact]
    public void Render_WithCustomLevels_ListsThemInTheirOwnGroupAfterTheStandardLevels()
    {
        var cut = RenderMixedPicker();

        var group = cut.Find("[data-testid=level-picker-group-custom]");
        Assert.Equal("Meine Levels", group.GetAttribute("label"));
        Assert.Equal(["custom-a", "custom-b"], group.QuerySelectorAll("option").Select(o => o.GetAttribute("value")));
        var standard = cut.FindAll("[data-testid=level-picker] > option");
        Assert.Equal(StandardLevels.All.Select(l => l.Id), standard.Select(o => o.GetAttribute("value")));
        Assert.Equal("Mein Burgweg", cut.Find("[data-testid=level-picker-option-custom-a]").TextContent.Trim());
    }

    [Fact]
    public void Render_CustomLevelName_IsRenderedAsText()
    {
        var cut = RenderMixedPicker();

        var option = cut.Find("[data-testid=level-picker-option-custom-b]");
        Assert.Equal("<b>Fett</b> & \"Quote\"", option.TextContent.Trim());
        Assert.Empty(option.QuerySelectorAll("b"));
    }

    [Fact]
    public void Render_SolvedCustomLevel_GetsTheCheckMark()
    {
        var cut = RenderMixedPicker(solved: id => id == "custom-a");

        Assert.Equal("✓ Mein Burgweg", cut.Find("[data-testid=level-picker-option-custom-a]").TextContent.Trim());
        Assert.Equal("false", cut.Find("[data-testid=level-picker-option-custom-b]").GetAttribute("data-solved"));
    }

    [Fact]
    public void Render_GroupLabel_IsEnglishInEnglish()
    {
        using var scope = new CultureScope("en");
        var cut = RenderMixedPicker();

        Assert.Equal("My levels", cut.Find("[data-testid=level-picker-group-custom]").GetAttribute("label"));
    }
}
