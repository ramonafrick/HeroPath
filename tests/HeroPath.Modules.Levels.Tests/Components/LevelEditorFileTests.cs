using System.Text.Json;
using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.Infrastructure;
using HeroPath.Modules.Levels.UI;
using HeroPath.Modules.Levels.UI.Components;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using HeroPath.Shared.UI.Localization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Modules.Levels.Tests.Components;

[Collection("Culture")]
public class LevelEditorFileTests : CultureBunitContext
{
    private readonly InMemoryAppStorage _storage = new();
    private readonly FakeFileDownload _download = new();

    public LevelEditorFileTests()
    {
        Services.AddSharedUI();
        Services.AddFakeDocumentLanguage();
        Services.AddSingleton<IAppStorage>(_storage);
        Services.AddSingleton<IFileDownload>(_download);
        Services.AddLevelsModule();
    }

    private static CancellationToken Token => Xunit.TestContext.Current.CancellationToken;

    private LevelRepository Repository => Services.GetRequiredService<LevelRepository>();

    private LevelEditorState EditorState => Services.GetRequiredService<LevelEditorState>();

    private IRenderedComponent<LevelEditor> RenderEditor() => Render<LevelEditor>();

    private static void Upload(IRenderedComponent<LevelEditor> cut, InputFileContent file) =>
        cut.FindComponent<InputFile>().UploadFiles(file);

    private static InputFileContent File(string text, string name = "level.json") => InputFileContent.CreateFromText(text, name);

    private static string Message(IRenderedComponent<LevelEditor> cut) =>
        cut.Find("[data-testid=editor-import-message]").TextContent.Trim();

    private static string MessageState(IRenderedComponent<LevelEditor> cut) =>
        cut.Find("[data-testid=editor-import-message]").GetAttribute("data-state")!;

    [Fact]
    public async Task Export_Click_SavesTheStoredLevelAsValidJsonWithASanitizedFileName()
    {
        var level = CustomLevelSamples.Valid("custom-1", "Käse <b>Weg</b>");
        await Repository.SaveAsync(level, Token);
        var cut = RenderEditor();

        cut.Find("[data-testid=custom-level-export-custom-1]").Click();

        cut.WaitForAssertion(() => Assert.Single(_download.Saved));
        var (fileName, content) = _download.Saved[0];
        Assert.Equal("kaese-b-weg-b.heropath.json", fileName);
        Assert.Equal(level, LevelJson.Read(content).Level, LevelComparer.Instance);
        Assert.Empty(cut.FindAll("[data-testid=editor-export-failed]"));
    }

    [Fact]
    public async Task Export_Content_HasOnlyLevelData()
    {
        await Repository.SaveAsync(CustomLevelSamples.Valid("custom-1", "Mein Weg"), Token);
        var cut = RenderEditor();

        cut.Find("[data-testid=custom-level-export-custom-1]").Click();

        cut.WaitForAssertion(() => Assert.Single(_download.Saved));
        using var document = JsonDocument.Parse(_download.Saved[0].Content);
        var properties = document.RootElement.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(["goal", "height", "id", "items", "name", "obstacles", "requireAllItems", "schemaVersion", "start", "width"], properties);
    }

    [Fact]
    public async Task Export_Failure_ShowsAVisibleMessage_AndTheNextSuccessClearsIt()
    {
        await Repository.SaveAsync(CustomLevelSamples.Valid("custom-1", "Eins"), Token);
        _download.Result = false;
        var cut = RenderEditor();

        cut.Find("[data-testid=custom-level-export-custom-1]").Click();

        cut.WaitForAssertion(() => Assert.Equal(
            "Die Datei konnte nicht gespeichert werden. Probier es noch einmal.",
            cut.Find("[data-testid=editor-export-failed]").TextContent.Trim()));
        Assert.Single(Repository.Custom);

        _download.Result = true;
        cut.Find("[data-testid=custom-level-export-custom-1]").Click();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-testid=editor-export-failed]")));
    }

    [Fact]
    public async Task Export_Button_HasALocalizedNameWithTheLevelNameAsText()
    {
        await Repository.SaveAsync(CustomLevelSamples.Valid("custom-1", "<i>Eins</i>"), Token);

        var cut = RenderEditor();

        var button = cut.Find("[data-testid=custom-level-export-custom-1]");
        Assert.Equal("Exportieren", button.TextContent.Trim());
        Assert.Equal("<i>Eins</i> exportieren", button.GetAttribute("aria-label"));
    }

    [Fact]
    public void Import_Input_AcceptsJsonOnlyOneFile()
    {
        var cut = RenderEditor();

        var input = cut.Find("[data-testid=editor-import]");
        Assert.Equal("input", input.TagName.ToLowerInvariant());
        Assert.Equal("file", input.GetAttribute("type"));
        Assert.Equal(".json,application/json", input.GetAttribute("accept"));
        Assert.Null(input.GetAttribute("multiple"));
        Assert.Contains("Level importieren", cut.Find(".import-button").TextContent);
    }

    [Fact]
    public void Import_ValidFile_AddsANewLevelToTheListWithoutOpeningIt()
    {
        var cut = RenderEditor();
        var before = cut.Find("[data-testid=editor-name]").GetAttribute("value");

        Upload(cut, File(LevelJson.Write(CustomLevelSamples.Valid("custom-file", "Aus Datei"))));

        cut.WaitForAssertion(() => Assert.Equal("«Aus Datei» ist jetzt in deiner Liste.", Message(cut)));
        Assert.Equal("ok", MessageState(cut));
        var imported = Assert.Single(Repository.Custom);
        Assert.NotEqual("custom-file", imported.Id);
        var entry = cut.Find($"[data-testid=custom-level-{imported.Id}]");
        Assert.Equal("Aus Datei", entry.TextContent.Trim());
        Assert.Null(entry.GetAttribute("aria-current"));
        Assert.Equal(before, cut.Find("[data-testid=editor-name]").GetAttribute("value"));
    }

    [Fact]
    public void Import_SameFileTwice_WorksBothTimes()
    {
        var cut = RenderEditor();
        var file = LevelJson.Write(CustomLevelSamples.Valid("custom-file", "Doppelt"));

        Upload(cut, File(file));
        cut.WaitForAssertion(() => Assert.Single(Repository.Custom));
        Upload(cut, File(file));

        cut.WaitForAssertion(() => Assert.Equal(2, Repository.Custom.Count));
        Assert.Equal(2, cut.FindAll("[data-testid^=custom-level-export-]").Count);
    }

    [Fact]
    public void Import_Garbage_ShowsTheInvalidMessage_AndChangesNothing()
    {
        var cut = RenderEditor();

        Upload(cut, File("this is not json"));

        cut.WaitForAssertion(() => Assert.Equal("Das ist kein Level für dieses Spiel. Probier eine andere Datei.", Message(cut)));
        Assert.Equal("error", MessageState(cut));
        Assert.Empty(Repository.Custom);
        Assert.Empty(_storage.Values);
    }

    [Fact]
    public void Import_Unsolvable_ShowsTheNotSavableMessage()
    {
        var cut = RenderEditor();

        Upload(cut, File(LevelJson.Write(CustomLevelSamples.Unsolvable("custom-x"))));

        cut.WaitForAssertion(() => Assert.Equal(
            "Dieses Level kann man nicht lösen oder es braucht zu viele Schritte. Darum wurde es nicht importiert.",
            Message(cut)));
        Assert.Empty(Repository.Custom);
    }

    [Fact]
    public void Import_ElevenStrawberries_ShowsTheInvalidMessage()
    {
        var cut = RenderEditor();

        Upload(cut, File(LevelJson.Write(CustomLevelSamples.TooManyStrawberries("custom-x"))));

        cut.WaitForAssertion(() => Assert.Equal("error", MessageState(cut)));
        Assert.Contains("kein Level", Message(cut));
        Assert.Empty(Repository.Custom);
    }

    [Fact]
    public void Import_FileOver256Kb_ShowsTheTooLargeMessage()
    {
        var cut = RenderEditor();

        Upload(cut, InputFileContent.CreateFromBinary(new byte[300 * 1024], "big.json"));

        cut.WaitForAssertion(() => Assert.Equal("Die Datei ist zu gross.", Message(cut)));
        Assert.Equal("error", MessageState(cut));
        Assert.Empty(Repository.Custom);
    }

    [Fact]
    public void Import_TextOverTheJsonLimitButUnder256Kb_ShowsTheTooLargeMessage()
    {
        var cut = RenderEditor();

        Upload(cut, File(new string(' ', LevelJson.MaxJsonLength + 10)));

        cut.WaitForAssertion(() => Assert.Equal("Die Datei ist zu gross.", Message(cut)));
        Assert.Empty(Repository.Custom);
    }

    [Fact]
    public async Task Import_WhenFull_ShowsTheLimitMessage()
    {
        for (var i = 0; i < LevelLimits.MaxCustomLevels; i++)
        {
            await Repository.SaveAsync(CustomLevelSamples.Valid($"custom-{i}"), Token);
        }

        var cut = RenderEditor();

        Upload(cut, File(LevelJson.Write(CustomLevelSamples.Valid("custom-x", "Zu viel"))));

        cut.WaitForAssertion(() => Assert.Equal("Du hast schon 20 eigene Levels. Lösche zuerst eines.", Message(cut)));
        Assert.Equal(LevelLimits.MaxCustomLevels, Repository.Custom.Count);
    }

    [Fact]
    public void Import_StorageFailure_ShowsTheStorageMessage()
    {
        _storage.FailWrites = true;
        var cut = RenderEditor();

        Upload(cut, File(LevelJson.Write(CustomLevelSamples.Valid("custom-x", "Nicht gespeichert"))));

        cut.WaitForAssertion(() => Assert.Contains("nicht gespeichert werden", Message(cut)));
        Assert.Equal("error", MessageState(cut));
        Assert.Empty(Repository.Custom);
    }

    [Fact]
    public void Import_FileName_IsNeverRenderedOrUsed()
    {
        var cut = RenderEditor();

        Upload(cut, File(LevelJson.Write(CustomLevelSamples.Valid("custom-x", "Brav")), "geheim-<b>name</b>.json"));
        cut.WaitForAssertion(() => Assert.Equal("ok", MessageState(cut)));
        Upload(cut, File("kaputt", "geheim-zwei.json"));
        cut.WaitForAssertion(() => Assert.Equal("error", MessageState(cut)));

        Assert.DoesNotContain("geheim", cut.Markup);
        Assert.Equal("Brav", Assert.Single(Repository.Custom).Name);
    }

    [Fact]
    public void Import_LevelNameWithMarkup_IsRenderedAsText()
    {
        var cut = RenderEditor();

        Upload(cut, File(LevelJson.Write(CustomLevelSamples.Valid("custom-x", "<script>x</script>"))));

        cut.WaitForAssertion(() => Assert.Equal("ok", MessageState(cut)));
        Assert.Empty(cut.FindAll("[data-testid=editor-import-message] script"));
        Assert.Contains("<script>x</script>", Message(cut));
    }

    [Fact]
    public void Import_DoesNotTouchADirtyDraft_AndNeverAsks()
    {
        var cut = RenderEditor();
        cut.Find("[data-testid=edit-cell-4-4]").Click();
        var draft = EditorState.Draft;

        Upload(cut, File(LevelJson.Write(CustomLevelSamples.Valid("custom-x", "Fremd"))));

        cut.WaitForAssertion(() => Assert.Equal("ok", MessageState(cut)));
        Assert.Same(draft, EditorState.Draft);
        Assert.True(EditorState.IsDirty);
        Assert.Empty(cut.FindAll("[data-testid=confirm-dialog]"));
    }

    [Fact]
    public async Task Messages_FollowALanguageSwitch()
    {
        var cut = RenderEditor();
        Upload(cut, File("kaputt"));
        cut.WaitForAssertion(() => Assert.Equal("error", MessageState(cut)));

        await Services.GetRequiredService<LanguageService>().SetAsync("en", Token);

        cut.WaitForAssertion(() => Assert.Equal("That is not a level for this game. Try another file.", Message(cut)));
        Assert.Contains("Import level", cut.Find(".import-button").TextContent);
    }

    private static Task UploadAsync(IRenderedComponent<LevelEditor> cut, IBrowserFile file) =>
        cut.InvokeAsync(() => cut.FindComponent<InputFile>().Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs([file])));

    public static TheoryData<Exception> ReadFailures() =>
    [
        new IOException("read failed"),
        new InvalidOperationException("detached"),
        new UnauthorizedAccessException("denied"),
        new NotSupportedException("no"),
        new Microsoft.JSInterop.JSException("js"),
        new TaskCanceledException("aborted by the browser"),
    ];

    [Theory]
    [MemberData(nameof(ReadFailures))]
    public async Task Import_ReadFails_ShowsTheUnreadableMessage_AndChangesNothing(Exception failure)
    {
        var cut = RenderEditor();

        await UploadAsync(cut, new FakeBrowserFile(_ => throw failure));

        cut.WaitForAssertion(() => Assert.Equal("Die Datei konnte nicht gelesen werden.", Message(cut)));
        Assert.Equal("error", MessageState(cut));
        Assert.Empty(Repository.Custom);
    }

    [Fact]
    public async Task Import_ReadFailsWhileReading_ShowsTheUnreadableMessage()
    {
        var cut = RenderEditor();

        await UploadAsync(cut, new FakeBrowserFile(_ => new ThrowingStream()));

        cut.WaitForAssertion(() => Assert.Equal("Die Datei konnte nicht gelesen werden.", Message(cut)));
    }

    [Fact]
    public async Task Import_UnreadableMessage_IsEnglishAfterASwitch_AndAnImportAfterwardsStillWorks()
    {
        var cut = RenderEditor();
        await UploadAsync(cut, new FakeBrowserFile(_ => throw new IOException()));
        cut.WaitForAssertion(() => Assert.Equal("error", MessageState(cut)));

        await Services.GetRequiredService<LanguageService>().SetAsync("en", Token);
        cut.WaitForAssertion(() => Assert.Equal("The file could not be read.", Message(cut)));

        Upload(cut, File(LevelJson.Write(CustomLevelSamples.Valid("custom-x", "Danach"))));
        cut.WaitForAssertion(() => Assert.Equal("ok", MessageState(cut)));
        Assert.Single(Repository.Custom);
    }

    [Fact]
    public async Task Import_StreamDeliversShortReads_IsReadUntilTheEnd()
    {
        var cut = RenderEditor();
        var bytes = System.Text.Encoding.UTF8.GetBytes(LevelJson.Write(CustomLevelSamples.Valid("custom-x", "Stueckweise")));

        await UploadAsync(cut, new FakeBrowserFile(_ => new TrickleStream(bytes), bytes.Length));

        cut.WaitForAssertion(() => Assert.Equal("ok", MessageState(cut)));
        Assert.Equal("Stueckweise", Assert.Single(Repository.Custom).Name);
    }

    [Fact]
    public async Task Import_StreamLongerThanTheJsonLimit_IsTooLargeEvenWithShortReads()
    {
        var cut = RenderEditor();
        var bytes = new byte[LevelJson.MaxJsonLength + 50];
        Array.Fill(bytes, (byte)' ');

        await UploadAsync(cut, new FakeBrowserFile(_ => new TrickleStream(bytes), bytes.Length));

        cut.WaitForAssertion(() => Assert.Equal("Die Datei ist zu gross.", Message(cut)));
    }

    [Fact]
    public async Task Render_WithoutCustomLevels_HasNoExportHint_WithOneTheHintWarnsAboutRealNames()
    {
        var cut = RenderEditor();
        Assert.Empty(cut.FindAll("[data-testid=editor-export-hint]"));

        await Repository.SaveAsync(CustomLevelSamples.Valid("custom-1", "Eins"), Token);

        cut.WaitForAssertion(() => Assert.Equal(
            "Der Name deines Levels steht in der Datei. Verwende nicht deinen echten Namen.",
            cut.Find("[data-testid=editor-export-hint]").TextContent.Trim()));
        await Services.GetRequiredService<LanguageService>().SetAsync("en", Token);
        cut.WaitForAssertion(() => Assert.Equal(
            "Your level's name is stored in the file. Please do not use your real name.",
            cut.Find("[data-testid=editor-export-hint]").TextContent.Trim()));
    }

    private sealed class FakeBrowserFile(Func<CancellationToken, Stream> open, long size = 10) : IBrowserFile
    {
        public string Name => "x.json";

        public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;

        public long Size => size;

        public string ContentType => "application/json";

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) => open(cancellationToken);
    }

    private sealed class ThrowingStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new IOException("broken");

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => throw new IOException("broken");

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class TrickleStream(byte[] data) : Stream
    {
        private int _position;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => data.Length;

        public override long Position { get => _position; set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= data.Length || count == 0)
            {
                return 0;
            }

            buffer[offset] = data[_position++];
            return 1;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_position >= data.Length || buffer.Length == 0)
            {
                return ValueTask.FromResult(0);
            }

            buffer.Span[0] = data[_position++];
            return ValueTask.FromResult(1);
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    [Fact]
    public void Import_Result_TakesTheFocusSoThePanelScrollsToIt()
    {
        var cut = RenderEditor();

        Upload(cut, File(LevelJson.Write(CustomLevelSamples.Valid("custom-x", "Neu"))));

        cut.WaitForAssertion(() => Assert.Equal("ok", MessageState(cut)));
        JSInterop.VerifyFocusAsyncInvoke();
        Assert.Equal("-1", cut.Find("[data-testid=editor-import-message]").GetAttribute("tabindex"));
    }
}
