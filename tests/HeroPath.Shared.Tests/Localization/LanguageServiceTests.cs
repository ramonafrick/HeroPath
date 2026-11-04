using System.Globalization;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI.Localization;

namespace HeroPath.Shared.Tests.Localization;

[Collection("Culture")]
public class LanguageServiceTests : IDisposable
{
    private readonly CultureScope _cultureScope = new();
    private readonly InMemoryAppStorage _storage = new();
    private readonly FakeDocumentLanguage _document = new();

    public void Dispose() => _cultureScope.Dispose();

    [Fact]
    public void Current_Initially_IsGerman()
    {
        var service = new LanguageService(_storage, _document);

        Assert.Equal("de-CH", service.Current);
    }

    [Fact]
    public async Task ApplyStoredAsync_NothingStored_UsesGerman()
    {
        var service = new LanguageService(_storage, _document);

        await service.ApplyStoredAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Equal("de-CH", service.Current);
        Assert.Equal("de-CH", CultureInfo.CurrentUICulture.Name);
    }

    [Fact]
    public async Task ApplyStoredAsync_StoredEnglish_AppliesEnglish()
    {
        _storage.Values[StorageKeys.Language] = "en";
        var service = new LanguageService(_storage, _document);

        await service.ApplyStoredAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Equal("en", service.Current);
        Assert.Equal("en", CultureInfo.CurrentUICulture.Name);
        Assert.Equal("en", CultureInfo.DefaultThreadCurrentUICulture?.Name);
        Assert.Equal("en", CultureInfo.DefaultThreadCurrentCulture?.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("fr")]
    [InlineData("en-US")]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("not a culture \0")]
    public async Task ApplyStoredAsync_UnknownOrGarbage_FallsBackToGerman(string stored)
    {
        _storage.Values[StorageKeys.Language] = stored;
        var service = new LanguageService(_storage, _document);

        await service.ApplyStoredAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Equal("de-CH", service.Current);
        Assert.Equal("de-CH", CultureInfo.CurrentUICulture.Name);
    }

    [Fact]
    public async Task ApplyStoredAsync_StorageThrows_FallsBackToGerman()
    {
        var service = new LanguageService(new ThrowingAppStorage(), _document);

        await service.ApplyStoredAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Equal("de-CH", service.Current);
    }

    [Fact]
    public async Task SetAsync_English_SwitchesCultureStoresAndRaisesChanged()
    {
        var service = new LanguageService(_storage, _document);
        var changed = 0;
        service.Changed += () => changed++;

        await service.SetAsync("en", Xunit.TestContext.Current.CancellationToken);

        Assert.Equal("en", service.Current);
        Assert.Equal("en", CultureInfo.CurrentUICulture.Name);
        Assert.Equal("en", _storage.Values[StorageKeys.Language]);
        Assert.Equal(1, changed);
    }

    [Fact]
    public async Task SetAsync_UnsupportedCulture_StoresGerman()
    {
        var service = new LanguageService(_storage, _document);

        await service.SetAsync("fr", Xunit.TestContext.Current.CancellationToken);

        Assert.Equal("de-CH", service.Current);
        Assert.Equal("de-CH", _storage.Values[StorageKeys.Language]);
    }

    [Fact]
    public async Task SetAsync_StorageThrows_StillSwitchesAndRaisesChanged()
    {
        var service = new LanguageService(new ThrowingAppStorage(), _document);
        var changed = 0;
        service.Changed += () => changed++;

        await service.SetAsync("en", Xunit.TestContext.Current.CancellationToken);

        Assert.Equal("en", service.Current);
        Assert.Equal("en", CultureInfo.CurrentUICulture.Name);
        Assert.Equal(1, changed);
    }

    [Fact]
    public async Task SetAsync_StorageRefusesTheWrite_StillSwitchesAndRaisesChanged()
    {
        _storage.FailWrites = true;
        var service = new LanguageService(_storage, _document);
        var changed = 0;
        service.Changed += () => changed++;

        await service.SetAsync("en", Xunit.TestContext.Current.CancellationToken);

        Assert.Equal("en", service.Current);
        Assert.Equal("en", CultureInfo.CurrentUICulture.Name);
        Assert.Empty(_storage.Values);
        Assert.Equal(1, changed);
    }

    [Fact]
    public async Task ApplyStoredAsync_StoredEnglish_SetsDocumentLanguage()
    {
        _storage.Values[StorageKeys.Language] = "en";
        var service = new LanguageService(_storage, _document);

        await service.ApplyStoredAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(["en"], _document.Values);
    }

    [Fact]
    public async Task ApplyStoredAsync_NothingStored_SetsDocumentLanguageToGerman()
    {
        var service = new LanguageService(_storage, _document);

        await service.ApplyStoredAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(["de-CH"], _document.Values);
    }

    [Fact]
    public async Task ApplyStoredAsync_StorageThrows_SetsDocumentLanguageToGerman()
    {
        var service = new LanguageService(new ThrowingAppStorage(), _document);

        await service.ApplyStoredAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(["de-CH"], _document.Values);
    }

    [Fact]
    public async Task SetAsync_SwitchesDocumentLanguageEachTime()
    {
        var service = new LanguageService(_storage, _document);

        await service.SetAsync("en", Xunit.TestContext.Current.CancellationToken);
        await service.SetAsync("de-CH", Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(["en", "de-CH"], _document.Values);
    }

    [Fact]
    public async Task SetAsync_UnsupportedCulture_SetsDocumentLanguageToGerman()
    {
        var service = new LanguageService(_storage, _document);

        await service.SetAsync("fr", Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(["de-CH"], _document.Values);
    }

    [Fact]
    public async Task SetAsync_DocumentLanguageThrows_StillSwitchesStoresAndRaisesChanged()
    {
        _document.Throws = new InvalidOperationException("No document.");
        var service = new LanguageService(_storage, _document);
        var changed = 0;
        service.Changed += () => changed++;

        await service.SetAsync("en", Xunit.TestContext.Current.CancellationToken);

        Assert.Equal("en", service.Current);
        Assert.Equal("en", CultureInfo.CurrentUICulture.Name);
        Assert.Equal("en", _storage.Values[StorageKeys.Language]);
        Assert.Equal(1, changed);
    }

    [Fact]
    public async Task ApplyStoredAsync_DocumentLanguageThrows_StillAppliesStoredLanguage()
    {
        _storage.Values[StorageKeys.Language] = "en";
        _document.Throws = new InvalidOperationException("No document.");
        var service = new LanguageService(_storage, _document);

        await service.ApplyStoredAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Equal("en", service.Current);
        Assert.Equal("en", CultureInfo.CurrentUICulture.Name);
    }

    [Fact]
    public async Task SetAsync_DocumentLanguageIsSetAfterTheCultureIsApplied()
    {
        string? cultureSeen = null;
        var document = new CultureRecordingDocument(name => cultureSeen = name);
        var service = new LanguageService(_storage, document);

        await service.SetAsync("en", Xunit.TestContext.Current.CancellationToken);

        Assert.Equal("en", cultureSeen);
    }

    private sealed class CultureRecordingDocument(Action<string> record) : IDocumentLanguage
    {
        public ValueTask SetAsync(string cultureName, CancellationToken cancellationToken)
        {
            record(CultureInfo.CurrentUICulture.Name);
            return ValueTask.CompletedTask;
        }
    }
}
