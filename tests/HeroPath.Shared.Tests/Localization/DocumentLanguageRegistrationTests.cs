using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using HeroPath.Shared.UI.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Shared.Tests.Localization;

[Collection("Culture")]
public class DocumentLanguageRegistrationTests
{
    [Fact]
    public async Task SetAsync_WithoutHostRegistration_IsSilent()
    {
        using var scope = new CultureScope("de-CH");
        var services = new ServiceCollection().AddSharedUI();
        services.AddScoped<IAppStorage, InMemoryAppStorage>();
        await using var provider = services.BuildServiceProvider();

        var exception = await Record.ExceptionAsync(() => provider.GetRequiredService<LanguageService>().SetAsync("en", Xunit.TestContext.Current.CancellationToken));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SetAsync_HostRegistrationAfterAddSharedUI_Wins()
    {
        using var scope = new CultureScope("de-CH");
        var services = new ServiceCollection().AddSharedUI();
        services.AddScoped<IAppStorage, InMemoryAppStorage>();
        var document = services.AddFakeDocumentLanguage();
        await using var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<LanguageService>().SetAsync("en", Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(["en"], document.Values);
    }
}
