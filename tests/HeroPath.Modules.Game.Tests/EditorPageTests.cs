using HeroPath.Modules.Game.UI;
using HeroPath.Modules.Game.UI.Pages;
using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using HeroPath.Shared.UI.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Modules.Game.Tests;

[Collection("Culture")]
public class EditorPageTests : CultureBunitContext
{
    public EditorPageTests()
    {
        Services.AddSharedUI();
        Services.AddGameModule();
        Services.AddSingleton<IAppStorage, InMemoryAppStorage>();
    }

    private static CancellationToken Token => Xunit.TestContext.Current.CancellationToken;

    private async Task SaveCustomLevelAsync(string id, string name)
    {
        var level = LevelEditing.NewLevel(id, name);
        var outcome = await Services.GetRequiredService<LevelRepository>().SaveAsync(level, Token);
        Assert.Equal(SaveOutcome.Saved, outcome);
    }

    [Fact]
    public void Render_WithoutId_ShowsTheEditorWithANewLevel()
    {
        var page = Render<EditorPage>();

        Assert.NotNull(page.Find("[data-testid=editor-page]"));
        Assert.NotNull(page.Find("[data-testid=level-editor]"));
        Assert.Equal("Mein Level 1", page.Find("[data-testid=editor-name]").GetAttribute("value"));
        Assert.Equal("Level bauen", page.Find("h2").TextContent);
        Assert.Equal(100, page.FindAll("[data-testid^=edit-cell-]").Count);
    }

    [Fact]
    public async Task Render_WithACustomLevelId_OpensThatLevel()
    {
        await SaveCustomLevelAsync("custom-abc", "Mein Burgweg");

        var page = Render<EditorPage>(p => p.Add(c => c.LevelId, "custom-abc"));

        Assert.Equal("Mein Burgweg", page.Find("[data-testid=editor-name]").GetAttribute("value"));
        Assert.Equal("custom-abc", Services.GetRequiredService<LevelEditorState>().EditingId);
        Assert.Equal("true", page.Find("[data-testid=custom-level-custom-abc]").GetAttribute("aria-current"));
    }

    [Theory]
    [InlineData("unknown-id")]
    [InlineData("easy-1")]
    public void Render_WithAnUnknownOrStandardId_OpensANewLevel(string id)
    {
        var page = Render<EditorPage>(p => p.Add(c => c.LevelId, id));

        Assert.Equal("Mein Level 1", page.Find("[data-testid=editor-name]").GetAttribute("value"));
        Assert.Null(Services.GetRequiredService<LevelEditorState>().EditingId);
    }

    [Fact]
    public void Render_HasALinkBackToTheStartPage()
    {
        var page = Render<EditorPage>();

        Assert.Equal(string.Empty, page.Find("[data-testid=editor-back]").GetAttribute("href"));
    }

    [Fact]
    public async Task LanguageSwitch_KeepsTheOpenLevelAndUnsavedChanges()
    {
        var page = Render<EditorPage>();
        page.Find("[data-testid=edit-cell-4-4]").Click();
        page.Find("[data-testid=editor-name]").Input("Mein Weg");

        await Services.GetRequiredService<LanguageService>().SetAsync(LanguageService.English, Token);

        page.WaitForAssertion(() => Assert.Equal("Build a level", page.Find("h2").TextContent));
        Assert.Equal("tree", page.Find("[data-testid=edit-cell-4-4]").GetAttribute("data-kind"));
        Assert.Equal("Mein Weg", page.Find("[data-testid=editor-name]").GetAttribute("value"));
    }
}
