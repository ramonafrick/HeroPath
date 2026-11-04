using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using HeroPath.Shared.UI.Graphics;
using HeroPath.Shared.UI.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Shared.Tests.Graphics;

[Collection("Culture")]
public class GraphicsTests : BunitContext
{
    private readonly CultureScope _cultureScope = new();

    public GraphicsTests()
    {
        Services.AddSharedUI();
        Services.AddSingleton<IAppStorage, InMemoryAppStorage>();
    }

    public static TheoryData<Type, string, string> Graphics => new()
    {
        { typeof(HeroMax), "Max, der Held", "Max, the hero" },
        { typeof(HeroLisa), "Lisa, die Heldin", "Lisa, the hero" },
        { typeof(StartField), "Startfeld", "Start field" },
        { typeof(GoalCastle), "Burg, das Ziel", "Castle, the goal" },
        { typeof(ObstacleTree), "Baum", "Tree" },
        { typeof(ObstacleRock), "Stein", "Rock" },
        { typeof(ObstaclePond), "Teich", "Pond" },
        { typeof(ItemStrawberry), "Erdbeere", "Strawberry" },
        { typeof(ItemPizza), "Pizza-Stück", "Pizza slice" },
        { typeof(ToolEraser), "Radiergummi", "Eraser" },
    };

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _cultureScope.Dispose();
        }
    }

    [Theory]
    [MemberData(nameof(Graphics))]
    public async Task Render_IsImageWithLocalizedTitle_InGermanAndEnglish(Type type, string german, string english)
    {
        var language = Services.GetRequiredService<LanguageService>();
        await language.SetAsync(LanguageService.German, Xunit.TestContext.Current.CancellationToken);

        var cut = RenderGraphic(type);

        var svg = cut.Find("svg");
        Assert.Equal("img", svg.GetAttribute("role"));
        Assert.Equal("0 0 100 100", svg.GetAttribute("viewBox"));
        Assert.Null(svg.GetAttribute("width"));
        Assert.Null(svg.GetAttribute("height"));
        Assert.Equal(german, cut.Find("svg > title").TextContent);

        await language.SetAsync(LanguageService.English, Xunit.TestContext.Current.CancellationToken);

        cut.WaitForAssertion(() => Assert.Equal(english, cut.Find("svg > title").TextContent));
    }

    [Theory]
    [InlineData(typeof(StartField))]
    [InlineData(typeof(GoalCastle))]
    public void Render_TwoInstances_HaveDistinctPatternIdsThatAreReferenced(Type type)
    {
        var first = RenderGraphic(type);
        var second = RenderGraphic(type);

        var firstId = first.Find("pattern").GetAttribute("id");
        var secondId = second.Find("pattern").GetAttribute("id");

        Assert.False(string.IsNullOrEmpty(firstId));
        Assert.NotEqual(firstId, secondId);
        Assert.Contains($"url(#{firstId})", first.Markup);
        Assert.Contains($"url(#{secondId})", second.Markup);
    }

    private IRenderedComponent<Bunit.Rendering.ContainerFragment> RenderGraphic(Type type) => Render(builder =>
    {
        builder.OpenComponent(0, type);
        builder.CloseComponent();
    });

    [Fact]
    public void HeroLisa_HasItsTestIdAndDiffersFromMaxInShape()
    {
        var lisa = RenderGraphic(typeof(HeroLisa));
        var max = RenderGraphic(typeof(HeroMax));

        Assert.NotNull(lisa.Find("[data-testid=graphic-hero-lisa]"));
        Assert.Contains("--hp-lisa-dress", lisa.Markup);
        Assert.DoesNotContain("--hp-max-shirt", lisa.Markup);
        Assert.NotEqual(max.Find("svg > g").InnerHtml, lisa.Find("svg > g").InnerHtml);
    }

    [Fact]
    public void Items_HaveTheirTestIdsPaletteAndDifferInShape()
    {
        var strawberry = RenderGraphic(typeof(ItemStrawberry));
        var pizza = RenderGraphic(typeof(ItemPizza));

        Assert.NotNull(strawberry.Find("[data-testid=graphic-item-strawberry]"));
        Assert.NotNull(pizza.Find("[data-testid=graphic-item-pizza]"));
        Assert.Contains("--hp-strawberry)", strawberry.Markup);
        Assert.Contains("--hp-pizza)", pizza.Markup);
        Assert.Contains("--hp-outline", strawberry.Markup);
        Assert.Contains("--hp-outline", pizza.Markup);
        Assert.NotEqual(strawberry.Find("svg > g").InnerHtml, pizza.Find("svg > g").InnerHtml);
    }
}
