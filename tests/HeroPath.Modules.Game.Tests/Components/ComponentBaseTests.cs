using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Game.UI;
using HeroPath.Modules.Game.UI.Components;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using HeroPath.Shared.UI.Localization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Modules.Game.Tests.Components;

[Collection("Culture")]
public class ComponentBaseTests : CultureBunitContext
{
    public ComponentBaseTests()
    {
        Services.AddSharedUI();
        Services.AddFakeDocumentLanguage();
        Services.AddGameModule();
        Services.AddSingleton<IAppStorage, InMemoryAppStorage>();
    }

    [Fact]
    public void SessionChange_ReRendersSessionComponent()
    {
        var cut = Render<ProbeComponent>();
        var renders = cut.Instance.Renders;

        Services.GetRequiredService<GameSession>().Add(Direction.Up);

        cut.WaitForAssertion(() => Assert.True(cut.Instance.Renders > renders));
    }

    [Fact]
    public async Task LanguageChange_ReRendersSessionComponent()
    {
        var cut = Render<ProbeComponent>();
        var renders = cut.Instance.Renders;

        await Services.GetRequiredService<LanguageService>().SetAsync(LanguageService.English, Xunit.TestContext.Current.CancellationToken);

        cut.WaitForAssertion(() => Assert.True(cut.Instance.Renders > renders));
    }

    [Fact]
    public async Task Dispose_UnsubscribesFromSessionAndLanguage()
    {
        var cut = Render<ProbeComponent>();
        var probe = cut.Instance;
        await DisposeComponentsAsync();
        var renders = probe.Renders;

        Services.GetRequiredService<GameSession>().Add(Direction.Up);
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

    private sealed class ProbeComponent : SessionComponentBase
    {
        public int Renders { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder) => Renders++;
    }
}
