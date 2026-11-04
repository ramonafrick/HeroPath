using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.UI;
using HeroPath.Modules.Game.UI.Pages;
using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using HeroPath.Shared.UI.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Modules.Game.Tests;

[Collection("Culture")]
public class EditorPageTests : CultureBunitContext
{
    private readonly InMemoryAppStorage _storage = new();
    private readonly FakeFileDownload _download = new();

    public EditorPageTests()
    {
        Services.AddSharedUI();
        Services.AddGameModule();
        Services.AddSingleton<IAppStorage>(_storage);
        Services.AddSingleton<ISoundPlayer, FakeSoundPlayer>();
        Services.AddSingleton<IKeyboardShortcuts, FakeKeyboardShortcuts>();
        Services.AddSingleton<IFileDownload>(_download);
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

    private HeroChoice Choice => Services.GetRequiredService<HeroChoice>();

    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    [Fact]
    public void TryOut_WithAHero_SelectsTheLevelAndOpensThePlayPage()
    {
        Choice.Choose(HeroAvatar.Lisa, "Zoe", "easy-1");
        var page = Render<EditorPage>();
        var id = Services.GetRequiredService<LevelEditorState>().Draft.Id;

        page.Find("[data-testid=editor-try]").Click();

        page.WaitForAssertion(() => Assert.Equal(Navigation.BaseUri + "play", Navigation.Uri));
        Assert.Equal(id, Choice.LevelId);
        Assert.Equal(HeroAvatar.Lisa, Choice.Avatar);
        Assert.Equal("Zoe", Choice.Name);
        Assert.Single(Services.GetRequiredService<LevelRepository>().Custom);
    }

    [Fact]
    public void TryOut_WithoutAHero_PreselectsTheLevelAndOpensTheStartPage()
    {
        Navigation.NavigateTo("editor");
        var page = Render<EditorPage>();
        var id = Services.GetRequiredService<LevelEditorState>().Draft.Id;

        page.Find("[data-testid=editor-try]").Click();

        page.WaitForAssertion(() => Assert.Equal($"{Navigation.BaseUri}#level-card-{id}", Navigation.Uri));
        Assert.False(Choice.IsChosen);
        Assert.Equal(id, Choice.LevelId);
    }

    [Fact]
    public void TryOut_WhenSavingFails_StaysOnThePageAndChangesNoChoice()
    {
        _storage.FailWrites = true;
        Choice.Choose(HeroAvatar.Max, null, "easy-1");
        Navigation.NavigateTo("editor");
        var page = Render<EditorPage>();

        page.Find("[data-testid=editor-try]").Click();

        page.WaitForAssertion(() => Assert.Equal("StorageFailed", page.Find("[data-testid=editor-saved]").GetAttribute("data-outcome")));
        Assert.Equal(Navigation.BaseUri + "editor", Navigation.Uri);
        Assert.Equal("easy-1", Choice.LevelId);
    }

    [Fact]
    public async Task Delete_OfTheChosenLevel_FallsBackToTheFirstStandardLevel()
    {
        await SaveCustomLevelAsync("custom-a", "Eins");
        Choice.Choose(HeroAvatar.Max, null, "custom-a");
        var page = Render<EditorPage>();

        page.Find("[data-testid=custom-level-delete-custom-a]").Click();
        page.Find("[data-testid=confirm-yes]").Click();

        page.WaitForAssertion(() => Assert.Equal("free-field", Choice.LevelId));
        Assert.True(Choice.IsChosen);
    }

    [Fact]
    public async Task Delete_OfAnotherLevel_KeepsTheChosenLevel()
    {
        await SaveCustomLevelAsync("custom-a", "Eins");
        await SaveCustomLevelAsync("custom-b", "Zwei");
        Choice.Choose(HeroAvatar.Max, null, "custom-a");
        var page = Render<EditorPage>();

        page.Find("[data-testid=custom-level-delete-custom-b]").Click();
        page.Find("[data-testid=confirm-yes]").Click();

        page.WaitForAssertion(() => Assert.Empty(page.FindAll("[data-testid=custom-level-custom-b]")));
        Assert.Equal("custom-a", Choice.LevelId);
    }
}
