using HeroPath.Modules.Game.UI.Components;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Modules.Game.Tests.Components;

[Collection("Culture")]
public class ShortcutHelpTests : CultureBunitContext
{
    public ShortcutHelpTests()
    {
        Services.AddSharedUI();
        Services.AddSingleton<IAppStorage, InMemoryAppStorage>();
    }

    [Fact]
    public void Render_IsClosedCollapsibleWithGermanSummary()
    {
        var cut = Render<ShortcutHelp>();

        var details = cut.Find("[data-testid=shortcut-help]");
        Assert.Equal("details", details.LocalName);
        Assert.False(details.HasAttribute("open"));
        Assert.Equal("Tasten", cut.Find("summary").TextContent);
    }

    [Fact]
    public void Render_ListsEveryShortcutWithKeysAsKbd()
    {
        var cut = Render<ShortcutHelp>();

        Assert.Equal(7, cut.FindAll("tr").Count);
        Assert.All(cut.FindAll("tr"), row => Assert.NotEmpty(row.QuerySelectorAll("th kbd")));
        Assert.Contains(cut.FindAll("kbd"), k => k.TextContent == "D");
        Assert.Contains(cut.FindAll("kbd"), k => k.TextContent == "R");
        Assert.Contains(cut.FindAll("kbd"), k => k.TextContent == "Leertaste");
        Assert.Contains("Debug-Schritt", cut.Markup);
    }

    [Fact]
    public void Render_English_UsesEnglishTexts()
    {
        using var scope = new CultureScope("en");

        var cut = Render<ShortcutHelp>();

        Assert.Equal("Keys", cut.Find("summary").TextContent);
        Assert.Contains(cut.FindAll("kbd"), k => k.TextContent == "Space");
        Assert.Contains("Clear the selection", cut.Markup);
        Assert.DoesNotContain("Tasten", cut.Markup);
    }
}
