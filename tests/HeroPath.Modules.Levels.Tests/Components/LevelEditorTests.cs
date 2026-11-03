using Bunit.TestDoubles;
using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.UI;
using HeroPath.Modules.Levels.UI.Components;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using HeroPath.Shared.UI.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Modules.Levels.Tests.Components;

[Collection("Culture")]
public class LevelEditorTests : CultureBunitContext
{
    private readonly InMemoryAppStorage _storage = new();

    public LevelEditorTests()
    {
        Services.AddSharedUI();
        Services.AddSingleton<IAppStorage>(_storage);
        Services.AddLevelsModule();
    }

    private LevelRepository Repository => Services.GetRequiredService<LevelRepository>();

    private LevelEditorState State => Services.GetRequiredService<LevelEditorState>();

    private IRenderedComponent<LevelEditor> RenderEditor(string? levelId = null) =>
        Render<LevelEditor>(p => p.Add(c => c.LevelId, levelId));

    private static void Click(IRenderedComponent<LevelEditor> cut, string testId) => cut.Find($"[data-testid={testId}]").Click();

    private static string Text(IRenderedComponent<LevelEditor> cut, string selector)
    {
        var parts = selector.Split(' ', 2);
        return cut.Find($"[data-testid={parts[0]}]" + (parts.Length > 1 ? $" {parts[1]}" : string.Empty)).TextContent.Trim();
    }

    private async Task SwitchLanguageAsync(string culture) =>
        await Services.GetRequiredService<LanguageService>().SetAsync(culture, Xunit.TestContext.Current.CancellationToken);

    [Fact]
    public void Render_NewLevel_ShowsTenByTenBoardNameAndAllEightTools()
    {
        var cut = RenderEditor();

        Assert.Equal(100, cut.FindAll("[data-testid^=edit-cell-]").Count);
        Assert.Equal("Mein Level 1", cut.Find("[data-testid=editor-name]").GetAttribute("value"));
        Assert.Equal("50", cut.Find("[data-testid=editor-name]").GetAttribute("maxlength"));
        Assert.Equal("10", Text(cut, "editor-width"));
        Assert.Equal("10", Text(cut, "editor-height"));
        var tools = new[] { "start", "goal", "tree", "rock", "pond", "strawberry", "pizza", "eraser" };
        Assert.Equal(tools.Select(t => $"editor-tool-{t}"), cut.FindAll("[data-testid^=editor-tool-]").Select(e => e.GetAttribute("data-testid")));
        Assert.Equal("Radiergummi", Text(cut, "editor-tool-eraser .tool-name"));
    }

    [Fact]
    public void Render_Cells_AreButtonsWithLocalizedLabels()
    {
        var cut = RenderEditor();

        var start = cut.Find("[data-testid=edit-cell-0-9]");
        Assert.Equal("button", start.TagName.ToLowerInvariant());
        Assert.Equal("Feld 1, 10: Start", start.GetAttribute("aria-label"));
        Assert.Equal("Feld 10, 1: Ziel", cut.Find("[data-testid=edit-cell-9-0]").GetAttribute("aria-label"));
        Assert.Equal("Feld 5, 5: leer", cut.Find("[data-testid=edit-cell-4-4]").GetAttribute("aria-label"));
    }

    [Fact]
    public void Render_NameField_IsLinkedToThePrivacyHint()
    {
        var cut = RenderEditor();

        var input = cut.Find("[data-testid=editor-name]");
        var hint = cut.Find("[data-testid=editor-name-hint]");
        Assert.Equal(hint.Id, input.GetAttribute("aria-describedby"));
        Assert.Equal("Bitte verwende nicht deinen echten Namen.", hint.TextContent);
    }

    [Fact]
    public void Tool_Click_MarksOnlyThatToolAsPressed()
    {
        var cut = RenderEditor();
        Assert.Equal("true", cut.Find("[data-testid=editor-tool-tree]").GetAttribute("aria-pressed"));

        Click(cut, "editor-tool-pond");

        Assert.Equal("true", cut.Find("[data-testid=editor-tool-pond]").GetAttribute("aria-pressed"));
        Assert.Single(cut.FindAll("[data-testid^=editor-tool-][aria-pressed=true]"));
        Assert.Equal(EditorTool.Pond, State.Tool);
    }

    [Fact]
    public void Cell_Click_AppliesTheActiveTool_AndAClickWithTheSameToolRemovesIt()
    {
        var cut = RenderEditor();
        Click(cut, "editor-tool-rock");

        Click(cut, "edit-cell-4-4");
        Assert.Equal("rock", cut.Find("[data-testid=edit-cell-4-4]").GetAttribute("data-kind"));
        Assert.Equal("Feld 5, 5: Stein", cut.Find("[data-testid=edit-cell-4-4]").GetAttribute("aria-label"));

        Click(cut, "edit-cell-4-4");
        Assert.Equal("empty", cut.Find("[data-testid=edit-cell-4-4]").GetAttribute("data-kind"));
    }

    [Fact]
    public void Cell_Click_Strawberry_SetsTheItem()
    {
        var cut = RenderEditor();
        Click(cut, "editor-tool-strawberry");

        Click(cut, "edit-cell-3-3");

        Assert.Equal("strawberry", cut.Find("[data-testid=edit-cell-3-3]").GetAttribute("data-item"));
    }

    [Fact]
    public void Cell_RefusedClick_ShowsAKidFriendlyHint()
    {
        var cut = RenderEditor();

        Click(cut, "edit-cell-0-9");

        Assert.Equal("Auf dem Start darf nichts im Weg stehen.", Text(cut, "editor-hint"));
        Assert.Equal("start", cut.Find("[data-testid=edit-cell-0-9]").GetAttribute("data-kind"));
    }

    [Fact]
    public void Cell_ElevenStrawberries_ShowsTheLimitHint()
    {
        var cut = RenderEditor();
        Click(cut, "editor-tool-strawberry");

        for (var i = 0; i < 11; i++)
        {
            Click(cut, $"edit-cell-{i % 10}-{4 + i / 10}");
        }

        Assert.Equal("Es passen höchstens 10 Erdbeeren in ein Level.", Text(cut, "editor-hint"));
        Assert.Equal(10, cut.FindAll("[data-item=strawberry]").Count);
    }

    [Fact]
    public async Task Hint_InEnglish_IsTranslated()
    {
        var cut = RenderEditor();
        Click(cut, "edit-cell-0-9");

        await SwitchLanguageAsync("en");

        cut.WaitForAssertion(() => Assert.Equal("Nothing may block the start.", Text(cut, "editor-hint")));
    }

    [Fact]
    public async Task Status_NewLevel_ShowsMinimumStepsInGermanAndEnglish()
    {
        var cut = RenderEditor();

        var status = cut.Find("[data-testid=editor-status]");
        Assert.Equal("solvable", status.GetAttribute("data-state"));
        Assert.Equal("18", status.GetAttribute("data-min-steps"));
        Assert.Equal("Dein Level braucht mindestens 18 Schritte.", cut.Find("[data-testid=editor-status] .status-text").TextContent.Trim());

        await SwitchLanguageAsync("en");

        cut.WaitForAssertion(() => Assert.Equal("Your level needs at least 18 steps.", Text(cut, "editor-status .status-text")));
    }

    [Fact]
    public async Task Status_OneStep_UsesTheSingularForm()
    {
        await Repository.SaveAsync(CustomLevelSamples.Valid("custom-near") with
        {
            Start = new Position(0, 0),
            Goal = new Position(1, 0),
            Obstacles = [],
            Items = [],
        }, Xunit.TestContext.Current.CancellationToken);

        var cut = RenderEditor("custom-near");

        Assert.Equal("1", cut.Find("[data-testid=editor-status]").GetAttribute("data-min-steps"));
        Assert.Equal("Dein Level braucht mindestens 1 Schritt.", Text(cut, "editor-status .status-text"));
        await SwitchLanguageAsync("en");
        cut.WaitForAssertion(() => Assert.Equal("Your level needs at least 1 step.", Text(cut, "editor-status .status-text")));
    }

    [Fact]
    public void Status_WalledInGoal_IsUnsolvable_AndSaveIsDisabledWithAReason()
    {
        var cut = RenderEditor();
        Click(cut, "editor-tool-rock");

        Click(cut, "edit-cell-8-0");
        Click(cut, "edit-cell-9-1");

        var status = cut.Find("[data-testid=editor-status]");
        Assert.Equal("unsolvable", status.GetAttribute("data-state"));
        Assert.Null(status.GetAttribute("data-min-steps"));
        Assert.Equal("Der Held kommt nicht zur Burg. Mach einen Weg frei.", cut.Find("[data-testid=editor-status] .status-text").TextContent.Trim());
        Assert.True(cut.Find("[data-testid=editor-save]").HasAttribute("disabled"));
        Assert.Equal("Speichern geht erst, wenn dein Level stimmt.", Text(cut, "editor-save-reason"));
    }

    [Fact]
    public void Status_UnreachableStrawberry_HasItsOwnHint()
    {
        var cut = RenderEditor();
        Click(cut, "editor-tool-strawberry");
        Click(cut, "edit-cell-5-5");
        Click(cut, "editor-tool-rock");
        foreach (var cell in new[] { "4-5", "6-5", "5-4", "5-6" })
        {
            Click(cut, $"edit-cell-{cell}");
        }

        cut.Find("[data-testid=editor-require-all]").Change(true);

        Assert.Equal("unsolvable", cut.Find("[data-testid=editor-status]").GetAttribute("data-state"));
        Assert.StartsWith("Der Held kommt nicht an alle Erdbeeren.", Text(cut, "editor-status .status-text"));
    }

    [Fact]
    public void Name_Empty_BlocksSavingWithAReason()
    {
        var cut = RenderEditor();

        cut.Find("[data-testid=editor-name]").Input(string.Empty);

        var status = cut.Find("[data-testid=editor-status]");
        Assert.Equal("invalid", status.GetAttribute("data-state"));
        Assert.Equal("Gib deinem Level einen Namen.", cut.Find("[data-testid=editor-status] .status-text").TextContent.Trim());
        Assert.True(cut.Find("[data-testid=editor-save]").HasAttribute("disabled"));
        Assert.NotNull(cut.Find("[data-testid=editor-save-reason]"));

        cut.Find("[data-testid=editor-name]").Input("Burgweg");

        Assert.False(cut.Find("[data-testid=editor-save]").HasAttribute("disabled"));
        Assert.Empty(cut.FindAll("[data-testid=editor-save-reason]"));
    }

    [Fact]
    public void Name_TypedWithTrailingSpace_IsNotCutWhileTyping()
    {
        var cut = RenderEditor();

        cut.Find("[data-testid=editor-name]").Input("Mein Weg ");

        Assert.Equal("Mein Weg ", cut.Find("[data-testid=editor-name]").GetAttribute("value"));
        Assert.Equal("Mein Weg", State.Draft.Name);
    }

    [Fact]
    public void RequireAll_IsDisabledWithoutStrawberryAndEnabledWithOne()
    {
        var cut = RenderEditor();
        Assert.True(cut.Find("[data-testid=editor-require-all]").HasAttribute("disabled"));

        Click(cut, "editor-tool-strawberry");
        Click(cut, "edit-cell-3-3");

        var checkbox = cut.Find("[data-testid=editor-require-all]");
        Assert.False(checkbox.HasAttribute("disabled"));
        checkbox.Change(true);
        Assert.True(State.Draft.RequireAllItems);

        Click(cut, "edit-cell-3-3");
        Assert.False(State.Draft.RequireAllItems);
        Assert.True(cut.Find("[data-testid=editor-require-all]").HasAttribute("disabled"));
    }

    [Fact]
    public void Size_PlusAndMinus_ChangeWidthAndHeight()
    {
        var cut = RenderEditor();

        Click(cut, "editor-width-plus");
        Click(cut, "editor-height-minus");

        Assert.Equal("11", Text(cut, "editor-width"));
        Assert.Equal("9", Text(cut, "editor-height"));
        Assert.Equal(11 * 9, cut.FindAll("[data-testid^=edit-cell-]").Count);
    }

    [Fact]
    public void Size_Buttons_AreDisabledAtTheLimits()
    {
        var cut = RenderEditor();
        State.Resize(3, 20);

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Find("[data-testid=editor-width-minus]").HasAttribute("disabled"));
            Assert.True(cut.Find("[data-testid=editor-height-plus]").HasAttribute("disabled"));
            Assert.False(cut.Find("[data-testid=editor-width-plus]").HasAttribute("disabled"));
        });
    }

    [Fact]
    public async Task Size_Shrinking_ShowsHowManyThingsWereRemoved()
    {
        var cut = RenderEditor();
        Click(cut, "editor-tool-tree");
        Click(cut, "edit-cell-9-4");

        Click(cut, "editor-width-minus");
        Assert.Equal("1 Ding wurde entfernt.", Text(cut, "editor-hint"));

        Click(cut, "edit-cell-8-4");
        Click(cut, "edit-cell-8-5");
        Click(cut, "editor-width-minus");
        Assert.Equal("2 Dinge wurden entfernt.", Text(cut, "editor-hint"));

        await SwitchLanguageAsync("en");
        cut.WaitForAssertion(() => Assert.Equal("2 things were removed.", Text(cut, "editor-hint")));
    }

    [Fact]
    public void Save_Valid_ShowsTheMessageAndListsTheLevelWithItsNameAsText()
    {
        var cut = RenderEditor();
        cut.Find("[data-testid=editor-name]").Input("<b>Mein</b> Weg");

        Click(cut, "editor-save");

        cut.WaitForAssertion(() => Assert.Equal("Gespeichert! Du findest dein Level bei «Meine Levels».", Text(cut, "editor-saved")));
        var entry = cut.Find($"[data-testid=custom-level-{State.Draft.Id}]");
        Assert.Equal("<b>Mein</b> Weg", entry.TextContent.Trim());
        Assert.Empty(entry.QuerySelectorAll("b"));
        Assert.Equal("true", entry.GetAttribute("aria-current"));
        Assert.Single(Repository.Custom);
    }

    [Fact]
    public void Save_StorageFailure_ShowsTheErrorAndKeepsTheDraftOpen()
    {
        var cut = RenderEditor();
        Click(cut, "edit-cell-4-4");
        _storage.FailWrites = true;

        Click(cut, "editor-save");

        cut.WaitForAssertion(() => Assert.Equal("Speichern hat nicht geklappt. Du kannst dein Level exportieren.", Text(cut, "editor-saved")));
        Assert.Equal("StorageFailed", cut.Find("[data-testid=editor-saved]").GetAttribute("data-outcome"));
        Assert.Equal("tree", cut.Find("[data-testid=edit-cell-4-4]").GetAttribute("data-kind"));
        Assert.False(cut.Find("[data-testid=editor-save]").HasAttribute("disabled"));
        Assert.Empty(Repository.Custom);
    }

    [Fact]
    public async Task Save_TwentyFirstLevel_ShowsTheLimitMessage()
    {
        for (var i = 0; i < LevelLimits.MaxCustomLevels; i++)
        {
            await Repository.SaveAsync(CustomLevelSamples.Valid($"custom-{i}"), Xunit.TestContext.Current.CancellationToken);
        }

        var cut = RenderEditor();
        Click(cut, "editor-save");

        cut.WaitForAssertion(() => Assert.Equal("Du hast schon 20 Levels. Lösche zuerst eines.", Text(cut, "editor-saved")));
        Assert.Equal(LevelLimits.MaxCustomLevels, Repository.Custom.Count);
    }

    [Fact]
    public void Save_Twice_OverwritesTheSameLevel()
    {
        var cut = RenderEditor();
        Click(cut, "editor-save");
        cut.WaitForAssertion(() => Assert.Single(Repository.Custom));
        Click(cut, "edit-cell-4-4");

        Click(cut, "editor-save");

        cut.WaitForAssertion(() => Assert.Single(Repository.Custom[0].Obstacles));
        Assert.Single(Repository.Custom);
    }

    [Fact]
    public async Task LevelId_OpensThatCustomLevel()
    {
        await Repository.SaveAsync(CustomLevelSamples.Valid("custom-a", "Burgweg"), Xunit.TestContext.Current.CancellationToken);

        var cut = RenderEditor("custom-a");

        Assert.Equal("Burgweg", cut.Find("[data-testid=editor-name]").GetAttribute("value"));
        Assert.Equal("5", Text(cut, "editor-width"));
        Assert.Equal("true", cut.Find("[data-testid=custom-level-custom-a]").GetAttribute("aria-current"));
    }

    [Theory]
    [InlineData("does-not-exist")]
    [InlineData("easy-1")]
    public void LevelId_UnknownOrStandard_OpensANewLevel(string id)
    {
        var cut = RenderEditor(id);

        Assert.Equal("Mein Level 1", cut.Find("[data-testid=editor-name]").GetAttribute("value"));
        Assert.Equal("10", Text(cut, "editor-width"));
        Assert.Null(State.EditingId);
    }

    [Fact]
    public async Task List_ClickOpensTheLevel_AndNewLevelStartsAFreshOne()
    {
        await Repository.SaveAsync(CustomLevelSamples.Valid("custom-a", "Eins"), Xunit.TestContext.Current.CancellationToken);
        await Repository.SaveAsync(CustomLevelSamples.Valid("custom-b", "Zwei"), Xunit.TestContext.Current.CancellationToken);
        var cut = RenderEditor();

        Click(cut, "custom-level-custom-b");
        Assert.Equal("Zwei", cut.Find("[data-testid=editor-name]").GetAttribute("value"));
        Assert.Equal("custom-b", State.EditingId);

        Click(cut, "editor-new");
        Assert.Equal("Mein Level 1", cut.Find("[data-testid=editor-name]").GetAttribute("value"));
        Assert.Null(State.EditingId);
        Assert.Equal(2, cut.FindAll("[data-testid^=custom-level-custom]").Count);
    }

    [Fact]
    public void List_WithoutLevels_ShowsAnEmptyText()
    {
        var cut = RenderEditor();

        Assert.Contains("noch keine eigenen Levels", cut.Find("[data-testid=custom-level-list]").TextContent);
    }

    [Fact]
    public async Task LanguageSwitch_KeepsTheDraftAndUnsavedChanges()
    {
        var cut = RenderEditor();
        Click(cut, "editor-tool-pond");
        Click(cut, "edit-cell-2-2");
        cut.Find("[data-testid=editor-name]").Input("Mein Teich");

        await SwitchLanguageAsync("en");

        cut.WaitForAssertion(() => Assert.Equal("Eraser", Text(cut, "editor-tool-eraser .tool-name")));
        Assert.Equal("pond", cut.Find("[data-testid=edit-cell-2-2]").GetAttribute("data-kind"));
        Assert.Equal("Mein Teich", cut.Find("[data-testid=editor-name]").GetAttribute("value"));
        Assert.Equal("true", cut.Find("[data-testid=editor-tool-pond]").GetAttribute("aria-pressed"));
        Assert.True(State.IsDirty);
        Assert.Equal("Cell 3, 3: Pond", cut.Find("[data-testid=edit-cell-2-2]").GetAttribute("aria-label"));
    }

    private BunitNavigationManager Navigation => (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();

    private static void ChooseTemplate(IRenderedComponent<LevelEditor> cut, string id) =>
        cut.Find("[data-testid=editor-template]").Change(id);

    private static int DialogCount(IRenderedComponent<LevelEditor> cut) => cut.FindAll("[data-testid=confirm-dialog]").Count;

    private static string DialogMessage(IRenderedComponent<LevelEditor> cut) => Text(cut, "confirm-message");

    private void MakeDirty(IRenderedComponent<LevelEditor> cut)
    {
        Click(cut, "edit-cell-4-4");
        Assert.True(State.IsDirty);
    }

    [Fact]
    public void Template_ListsAllSevenStandardLevelsWithLocalizedNames()
    {
        var cut = RenderEditor();

        var options = cut.FindAll("[data-testid^=editor-template-]");

        Assert.Equal(Repository.Standard.Select(level => $"editor-template-{level.Id}"), options.Select(o => o.GetAttribute("data-testid")));
        Assert.Equal(7, options.Count);
        Assert.Equal("Freies Feld", Text(cut, "editor-template-free-field"));
    }

    [Fact]
    public void Template_Choose_OpensAnUnsavedCopyAndLeavesTheOriginalAlone()
    {
        var cut = RenderEditor();
        var original = Repository.Find("easy-1")!;

        ChooseTemplate(cut, "easy-1");

        Assert.Equal(0, DialogCount(cut));
        Assert.NotEqual("easy-1", State.Draft.Id);
        Assert.Null(State.EditingId);
        Assert.Equal("Mein Level 1", cut.Find("[data-testid=editor-name]").GetAttribute("value"));
        Assert.Equal(original.Obstacles.Count, State.Draft.Obstacles.Count);
        var tree = original.Obstacles[0];
        Assert.Equal(tree.Kind.ToString().ToLowerInvariant(), cut.Find($"[data-testid=edit-cell-{tree.X}-{tree.Y}]").GetAttribute("data-kind"));
        Assert.Same(original, Repository.Find("easy-1"));
        Assert.Empty(Repository.Custom);
        Assert.EndsWith("/editor", Navigation.Uri);
    }

    [Fact]
    public void Template_Choose_WhileDirty_AsksFirstAndCancelKeepsTheDraft()
    {
        var cut = RenderEditor();
        MakeDirty(cut);
        var draft = State.Draft;

        ChooseTemplate(cut, "easy-1");

        Assert.Equal(1, DialogCount(cut));
        Assert.Equal("Änderungen verwerfen?", Text(cut, "confirm-title"));
        Assert.Same(draft, State.Draft);

        Click(cut, "confirm-no");

        Assert.Equal(0, DialogCount(cut));
        Assert.Same(draft, State.Draft);
        Assert.True(State.IsDirty);
    }

    [Fact]
    public void Template_Choose_WhileDirty_ConfirmReplacesTheDraft()
    {
        var cut = RenderEditor();
        MakeDirty(cut);

        ChooseTemplate(cut, "easy-1");
        Click(cut, "confirm-yes");

        Assert.Equal(0, DialogCount(cut));
        Assert.False(State.IsDirty);
        Assert.NotEqual("easy-1", State.Draft.Id);
        Assert.Equal(Repository.Find("easy-1")!.Obstacles.Count, State.Draft.Obstacles.Count);
    }

    [Fact]
    public void Template_ChooseTwiceInARow_WorksBothTimes()
    {
        var cut = RenderEditor();

        ChooseTemplate(cut, "easy-1");
        var first = State.Draft.Id;
        ChooseTemplate(cut, "easy-1");

        Assert.NotEqual(first, State.Draft.Id);
    }

    [Fact]
    public async Task NewAndOpen_WhileDirty_AskFirst_AndWhileCleanDoNot()
    {
        await Repository.SaveAsync(CustomLevelSamples.Valid("custom-a", "Eins"), Xunit.TestContext.Current.CancellationToken);
        var cut = RenderEditor();
        Click(cut, "editor-new");
        Assert.Equal(0, DialogCount(cut));

        MakeDirty(cut);
        Click(cut, "editor-new");
        Assert.Equal(1, DialogCount(cut));
        Click(cut, "confirm-no");
        Assert.True(State.IsDirty);

        Click(cut, "custom-level-custom-a");
        Assert.Equal(1, DialogCount(cut));
        Assert.Equal("Änderungen verwerfen?", Text(cut, "confirm-title"));
        Click(cut, "confirm-yes");

        Assert.Equal("custom-a", State.EditingId);
        Assert.False(State.IsDirty);
        Assert.EndsWith("/editor/custom-a", Navigation.Uri);
    }

    [Fact]
    public async Task Delete_AsksWithTheLevelNameAsText_AndCancelKeepsTheLevel()
    {
        await Repository.SaveAsync(CustomLevelSamples.Valid("custom-a", "<i>Eins</i>"), Xunit.TestContext.Current.CancellationToken);
        var cut = RenderEditor();

        Click(cut, "custom-level-delete-custom-a");

        Assert.Equal(1, DialogCount(cut));
        Assert.Equal("Level «<i>Eins</i>» wirklich löschen? Das kannst du nicht rückgängig machen.", DialogMessage(cut));
        Assert.Empty(cut.FindAll("[data-testid=confirm-message] i"));
        Assert.Equal("Ja, löschen", Text(cut, "confirm-yes"));

        Click(cut, "confirm-no");

        Assert.Equal(0, DialogCount(cut));
        Assert.Single(Repository.Custom);
        Assert.NotNull(cut.Find("[data-testid=custom-level-custom-a]"));
    }

    [Fact]
    public async Task Delete_Confirm_RemovesTheLevelFromListAndRepository_AndKeepsAnotherOpenDraft()
    {
        await Repository.SaveAsync(CustomLevelSamples.Valid("custom-a", "Eins"), Xunit.TestContext.Current.CancellationToken);
        await Repository.SaveAsync(CustomLevelSamples.Valid("custom-b", "Zwei"), Xunit.TestContext.Current.CancellationToken);
        var cut = RenderEditor();
        MakeDirty(cut);
        var draft = State.Draft;

        Click(cut, "custom-level-delete-custom-a");
        Click(cut, "confirm-yes");

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-testid=custom-level-custom-a]")));
        Assert.Equal(["custom-b"], Repository.Custom.Select(level => level.Id));
        Assert.Same(draft, State.Draft);
        Assert.True(State.IsDirty);
    }

    [Fact]
    public async Task Delete_TheOpenLevel_StartsANewCleanLevelAndNavigatesToTheEmptyEditor()
    {
        await Repository.SaveAsync(CustomLevelSamples.Valid("custom-a", "Eins"), Xunit.TestContext.Current.CancellationToken);
        var cut = RenderEditor("custom-a");

        Click(cut, "custom-level-delete-custom-a");
        Click(cut, "confirm-yes");

        cut.WaitForAssertion(() => Assert.Null(State.EditingId));
        Assert.False(State.IsDirty);
        Assert.Equal("Mein Level 1", cut.Find("[data-testid=editor-name]").GetAttribute("value"));
        Assert.EndsWith("/editor", Navigation.Uri);
        Assert.DoesNotContain(Navigation.History, entry => entry.State == NavigationState.Prevented);
    }

    [Fact]
    public async Task Delete_StorageFailure_ShowsAMessageAndKeepsTheLevel()
    {
        await Repository.SaveAsync(CustomLevelSamples.Valid("custom-a", "Eins"), Xunit.TestContext.Current.CancellationToken);
        var cut = RenderEditor();
        _storage.FailWrites = true;

        Click(cut, "custom-level-delete-custom-a");
        Click(cut, "confirm-yes");

        cut.WaitForAssertion(() => Assert.Contains("nicht gelöscht", Text(cut, "editor-delete-failed")));
        Assert.Single(Repository.Custom);
    }

    [Fact]
    public void Clear_AsksAndCancelKeepsEverything()
    {
        var cut = RenderEditor();
        MakeDirty(cut);

        Click(cut, "editor-clear");

        Assert.Equal("Alles leeren?", Text(cut, "confirm-title"));
        Click(cut, "confirm-no");
        Assert.Equal(0, DialogCount(cut));
        Assert.Equal("tree", cut.Find("[data-testid=edit-cell-4-4]").GetAttribute("data-kind"));
    }

    [Fact]
    public void Clear_Confirm_RemovesObstaclesAndItems()
    {
        var cut = RenderEditor();
        MakeDirty(cut);
        Click(cut, "editor-tool-strawberry");
        Click(cut, "edit-cell-5-5");

        Click(cut, "editor-clear");
        Click(cut, "confirm-yes");

        Assert.Equal(0, DialogCount(cut));
        Assert.Equal("empty", cut.Find("[data-testid=edit-cell-4-4]").GetAttribute("data-kind"));
        Assert.Equal("empty", cut.Find("[data-testid=edit-cell-5-5]").GetAttribute("data-kind"));
        Assert.Equal("start", cut.Find("[data-testid=edit-cell-0-9]").GetAttribute("data-kind"));
    }

    [Fact]
    public void TryOut_IsHiddenWithoutAHandler()
    {
        var cut = RenderEditor();

        Assert.Empty(cut.FindAll("[data-testid=editor-try]"));
    }

    [Fact]
    public void TryOut_SavesFirstAndThenRaisesTheCallbackWithTheLevelId()
    {
        string? tried = null;
        var savedBeforeCallback = 0;
        var cut = Render<LevelEditor>(p => p.Add(c => c.OnTryOut, EventCallback.Factory.Create<string>(this, id =>
        {
            tried = id;
            savedBeforeCallback = Repository.Custom.Count;
        })));

        Click(cut, "editor-try");

        cut.WaitForAssertion(() => Assert.NotNull(tried));
        Assert.Equal(State.Draft.Id, tried);
        Assert.Equal(1, savedBeforeCallback);
    }

    [Fact]
    public void TryOut_WhenTheLevelCannotBeSaved_IsDisabled()
    {
        var cut = Render<LevelEditor>(p => p.Add(c => c.OnTryOut, EventCallback.Factory.Create<string>(this, _ => { })));
        cut.Find("[data-testid=editor-name]").Input("   ");

        Assert.True(cut.Find("[data-testid=editor-try]").HasAttribute("disabled"));
    }

    [Fact]
    public void TryOut_WhenSavingFails_RaisesNoCallbackAndShowsTheError()
    {
        var raised = 0;
        var cut = Render<LevelEditor>(p => p.Add(c => c.OnTryOut, EventCallback.Factory.Create<string>(this, _ => raised++)));
        _storage.FailWrites = true;

        Click(cut, "editor-try");

        cut.WaitForAssertion(() => Assert.Equal("StorageFailed", cut.Find("[data-testid=editor-saved]").GetAttribute("data-outcome")));
        Assert.Equal(0, raised);
        Assert.Empty(Repository.Custom);
    }

    [Fact]
    public void TryOut_DoesNotAskEvenWhenTheCallbackNavigates()
    {
        var cut = Render<LevelEditor>(p => p.Add(c => c.OnTryOut, EventCallback.Factory.Create<string>(this, _ => Navigation.NavigateTo("play"))));
        MakeDirty(cut);

        Click(cut, "editor-try");

        cut.WaitForAssertion(() => Assert.EndsWith("/play", Navigation.Uri));
        Assert.Equal(0, DialogCount(cut));
    }

    [Fact]
    public void NavigationLock_WhenClean_LetsTheChildLeave()
    {
        var cut = RenderEditor();

        Navigation.NavigateTo("play");

        Assert.Equal(0, DialogCount(cut));
        Assert.EndsWith("/play", Navigation.Uri);
    }

    [Fact]
    public void NavigationLock_WhenDirty_PreventsAndAsks_CancelStays()
    {
        var cut = RenderEditor();
        MakeDirty(cut);

        Navigation.NavigateTo("play");

        cut.WaitForAssertion(() => Assert.Equal(1, DialogCount(cut)));
        Assert.Equal("Weggehen?", Text(cut, "confirm-title"));
        Assert.Equal("Deine Änderungen sind noch nicht gespeichert. Trotzdem weggehen?", DialogMessage(cut));
        Assert.DoesNotContain("/play", Navigation.Uri);

        Click(cut, "confirm-no");

        Assert.Equal(0, DialogCount(cut));
        Assert.DoesNotContain("/play", Navigation.Uri);
        Assert.True(State.IsDirty);
        Assert.Equal("tree", cut.Find("[data-testid=edit-cell-4-4]").GetAttribute("data-kind"));
    }

    [Fact]
    public void NavigationLock_WhenDirty_ConfirmLeavesToTheTarget()
    {
        var cut = RenderEditor();
        MakeDirty(cut);
        Navigation.NavigateTo("play");
        cut.WaitForAssertion(() => Assert.Equal(1, DialogCount(cut)));

        Click(cut, "confirm-yes");

        Assert.EndsWith("/play", Navigation.Uri);
    }

    [Fact]
    public void NavigationLock_AfterSaving_NoLongerAsks()
    {
        var cut = RenderEditor();
        MakeDirty(cut);
        Click(cut, "editor-save");
        cut.WaitForAssertion(() => Assert.False(State.IsDirty));

        Navigation.NavigateTo("play");

        Assert.Equal(0, DialogCount(cut));
        Assert.EndsWith("/play", Navigation.Uri);
    }

    [Fact]
    public void FirstSave_ReplacesTheUrlWithoutAsking()
    {
        var cut = RenderEditor();
        MakeDirty(cut);

        Click(cut, "editor-save");

        cut.WaitForAssertion(() => Assert.EndsWith($"/editor/{State.Draft.Id}", Navigation.Uri));
        Assert.Equal(0, DialogCount(cut));
    }
}
