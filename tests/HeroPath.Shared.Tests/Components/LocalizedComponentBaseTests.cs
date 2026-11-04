using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using HeroPath.Shared.UI.Components;
using HeroPath.Shared.UI.Localization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Shared.Tests.Components;

[Collection("Culture")]
public class LocalizedComponentBaseTests : CultureBunitContext
{
    public LocalizedComponentBaseTests()
    {
        Services.AddSharedUI();
        Services.AddFakeDocumentLanguage();
        Services.AddSingleton<IAppStorage, InMemoryAppStorage>();
    }

    [Fact]
    public async Task LanguageChange_ReRendersComponent()
    {
        var cut = Render<ProbeComponent>();
        var renders = cut.Instance.Renders;

        await Services.GetRequiredService<LanguageService>().SetAsync(LanguageService.English, Xunit.TestContext.Current.CancellationToken);

        cut.WaitForAssertion(() => Assert.True(cut.Instance.Renders > renders));
    }

    [Fact]
    public async Task Dispose_UnsubscribesFromLanguage()
    {
        var probe = Render<ProbeComponent>().Instance;
        await DisposeComponentsAsync();
        var renders = probe.Renders;

        await Services.GetRequiredService<LanguageService>().SetAsync(LanguageService.English, Xunit.TestContext.Current.CancellationToken);
        await Task.Yield();

        Assert.Equal(renders, probe.Renders);
    }

    [Fact]
    public void Dispose_CalledTwice_IsHarmless()
    {
        var probe = Render<ProbeComponent>().Instance;

        probe.Dispose();
        probe.Dispose();
    }

    private sealed class ProbeComponent : LocalizedComponentBase
    {
        public int Renders { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder) => Renders++;
    }
}
