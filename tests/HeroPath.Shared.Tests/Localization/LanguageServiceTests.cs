using System.Globalization;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI.Localization;

namespace HeroPath.Shared.Tests.Localization;

[Collection("Culture")]
public class LanguageServiceTests : IDisposable
{
    private readonly CultureScope _cultureScope = new();
    private readonly InMemoryAppStorage _storage = new();

    public void Dispose() => _cultureScope.Dispose();

    [Fact]
    public void Current_Initially_IsGerman()
    {
        var service = new LanguageService(_storage);

        Assert.Equal("de-CH", service.Current);
    }

    [Fact]
    public async Task ApplyStoredAsync_NothingStored_UsesGerman()
    {
        var service = new LanguageService(_storage);

        await service.ApplyStoredAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Equal("de-CH", service.Current);
        Assert.Equal("de-CH", CultureInfo.CurrentUICulture.Name);
    }

    [Fact]
    public async Task ApplyStoredAsync_StoredEnglish_AppliesEnglish()
    {
        _storage.Values[StorageKeys.Language] = "en";
        var service = new LanguageService(_storage);

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
        var service = new LanguageService(_storage);

        await service.ApplyStoredAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Equal("de-CH", service.Current);
        Assert.Equal("de-CH", CultureInfo.CurrentUICulture.Name);
    }

    [Fact]
    public async Task ApplyStoredAsync_StorageThrows_FallsBackToGerman()
    {
        var service = new LanguageService(new ThrowingAppStorage());

        await service.ApplyStoredAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Equal("de-CH", service.Current);
    }

    [Fact]
    public async Task SetAsync_English_SwitchesCultureStoresAndRaisesChanged()
    {
        var service = new LanguageService(_storage);
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
        var service = new LanguageService(_storage);

        await service.SetAsync("fr", Xunit.TestContext.Current.CancellationToken);

        Assert.Equal("de-CH", service.Current);
        Assert.Equal("de-CH", _storage.Values[StorageKeys.Language]);
    }

    [Fact]
    public async Task SetAsync_StorageThrows_StillSwitchesAndRaisesChanged()
    {
        var service = new LanguageService(new ThrowingAppStorage());
        var changed = 0;
        service.Changed += () => changed++;

        await service.SetAsync("en", Xunit.TestContext.Current.CancellationToken);

        Assert.Equal("en", service.Current);
        Assert.Equal("en", CultureInfo.CurrentUICulture.Name);
        Assert.Equal(1, changed);
    }
}
