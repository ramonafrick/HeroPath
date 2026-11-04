using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.UI;
using HeroPath.Modules.Game.UI.Pages;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace HeroPath.Modules.Game.Tests;

[Collection("Culture")]
public class EditorPageShortcutTests : CultureBunitContext
{
    [Fact]
    public void EditorAndStartPage_DoNotSubscribe()
    {
        var keys = new FakeKeyboardShortcuts();
        Services.AddSingleton<TimeProvider>(new FakeTimeProvider());
        Services.AddSharedUI();
        Services.AddFakeDocumentLanguage();
        Services.AddGameModule();
        Services.AddSingleton<IAppStorage, InMemoryAppStorage>();
        Services.AddSingleton<ISoundPlayer, FakeSoundPlayer>();
        Services.AddSingleton<IKeyboardShortcuts>(keys);
        Services.AddSingleton<IFileDownload, FakeFileDownload>();
        Services.GetRequiredService<HeroChoice>().Choose(HeroAvatar.Max, null, "easy-2");

        Render<EditorPage>();
        Render<StartPage>();

        Assert.Equal(0, keys.SubscribeCount);
    }
}
