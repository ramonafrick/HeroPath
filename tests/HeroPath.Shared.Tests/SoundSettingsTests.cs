using HeroPath.Shared.Application;
using HeroPath.Shared.Application.Contracts;

namespace HeroPath.Shared.Tests;

public class SoundSettingsTests
{
    private readonly InMemoryAppStorage _storage = new();

    [Fact]
    public void Enabled_Initially_IsOff()
    {
        Assert.False(new SoundSettings(_storage).Enabled);
    }

    [Fact]
    public async Task ApplyStoredAsync_NothingStored_StaysOffAndRaisesNothing()
    {
        var settings = new SoundSettings(_storage);
        var raised = false;
        settings.Changed += () => raised = true;

        await settings.ApplyStoredAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.False(settings.Enabled);
        Assert.False(raised);
    }

    [Fact]
    public async Task SetAsync_On_StoresAndRaisesChanged()
    {
        var settings = new SoundSettings(_storage);
        var raised = 0;
        settings.Changed += () => raised++;

        await settings.SetAsync(true, Xunit.TestContext.Current.CancellationToken);

        Assert.True(settings.Enabled);
        Assert.Equal("on", _storage.Values[StorageKeys.Sound]);
        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task ApplyStoredAsync_AfterSetOn_ReloadsOn()
    {
        await new SoundSettings(_storage).SetAsync(true, Xunit.TestContext.Current.CancellationToken);
        var reloaded = new SoundSettings(_storage);

        await reloaded.ApplyStoredAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.True(reloaded.Enabled);
    }

    [Fact]
    public async Task ApplyStoredAsync_AfterSetOnThenOff_ReloadsOff()
    {
        var settings = new SoundSettings(_storage);
        await settings.SetAsync(true, Xunit.TestContext.Current.CancellationToken);
        await settings.SetAsync(false, Xunit.TestContext.Current.CancellationToken);
        var reloaded = new SoundSettings(_storage);

        await reloaded.ApplyStoredAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.False(reloaded.Enabled);
    }

    [Theory]
    [InlineData("")]
    [InlineData("true")]
    [InlineData("1")]
    [InlineData("ON")]
    [InlineData("garbage")]
    [InlineData("off")]
    public async Task ApplyStoredAsync_UnknownValue_MeansOff(string stored)
    {
        _storage.Values[StorageKeys.Sound] = stored;
        var settings = new SoundSettings(_storage);

        await settings.ApplyStoredAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.False(settings.Enabled);
    }

    [Fact]
    public async Task ApplyStoredAsync_ThrowingStorage_StaysOff()
    {
        var settings = new SoundSettings(new ThrowingAppStorage());

        await settings.ApplyStoredAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.False(settings.Enabled);
    }

    [Fact]
    public async Task SetAsync_ThrowingStorage_StillSwitchesAndRaisesChanged()
    {
        var settings = new SoundSettings(new ThrowingAppStorage());
        var raised = false;
        settings.Changed += () => raised = true;

        await settings.SetAsync(true, Xunit.TestContext.Current.CancellationToken);

        Assert.True(settings.Enabled);
        Assert.True(raised);
    }

    [Fact]
    public async Task SetAsync_Cancelled_Propagates()
    {
        using var cancellation = new CancellationTokenSource();
        var settings = new SoundSettings(new CancellingAppStorage());

        await Assert.ThrowsAsync<OperationCanceledException>(() => settings.SetAsync(true, cancellation.Token));
    }

    private sealed class CancellingAppStorage : IAppStorage
    {
        public ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken) => throw new OperationCanceledException();

        public ValueTask<bool> SetAsync(string key, string value, CancellationToken cancellationToken) => throw new OperationCanceledException();

        public ValueTask<bool> RemoveAsync(string key, CancellationToken cancellationToken) => throw new OperationCanceledException();
    }
}
