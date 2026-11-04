using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Game.UI;
using HeroPath.Modules.Game.UI.Components;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using HeroPath.Shared.UI.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace HeroPath.Modules.Game.Tests.Components;

[Collection("Culture")]
public class ItemCounterTests : CultureBunitContext
{
    private readonly GameSession _session;
    private readonly ExecutionRunner _runner;

    public ItemCounterTests()
    {
        Services.AddSingleton<TimeProvider>(new FakeTimeProvider());
        Services.AddSharedUI();
        Services.AddFakeDocumentLanguage();
        Services.AddGameModule();
        Services.AddSingleton<IAppStorage, InMemoryAppStorage>();
        _session = Services.GetRequiredService<GameSession>();
        _runner = Services.GetRequiredService<ExecutionRunner>();
    }

    private void LoadItems(params LevelItem[] items) =>
        _session.Load(TestLevels.WithItems("counter", 8, 3, new Position(0, 1), new Position(7, 1), items));

    [Fact]
    public void Render_LevelWithoutItems_ShowsNoEntries()
    {
        _session.Load(TestLevels.Demo);

        var cut = Render<ItemCounter>();

        Assert.Empty(cut.FindAll("[data-testid^=item-counter-]"));
    }

    [Fact]
    public void Render_OnlyKindsInTheLevel_AreShownWithTotals()
    {
        LoadItems(new LevelItem(1, 1, ItemKind.Strawberry), new LevelItem(2, 1, ItemKind.Strawberry), new LevelItem(3, 1, ItemKind.Strawberry));

        var cut = Render<ItemCounter>();

        var strawberries = cut.Find("[data-testid=item-counter-strawberry]");
        Assert.Equal("0", strawberries.GetAttribute("data-collected"));
        Assert.Equal("3", strawberries.GetAttribute("data-total"));
        Assert.Equal("Erdbeeren 0 von 3", strawberries.QuerySelector(".item-counter-text")!.TextContent.Trim());
        Assert.NotNull(strawberries.QuerySelector("[data-testid=graphic-item-strawberry]"));
        Assert.Empty(cut.FindAll("[data-testid=item-counter-pizza]"));
    }

    [Fact]
    public void Render_CollectingItems_UpdatesTheCount()
    {
        LoadItems(new LevelItem(1, 1, ItemKind.Strawberry), new LevelItem(2, 1, ItemKind.Pizza), new LevelItem(3, 1, ItemKind.Strawberry));
        TestLevels.Fill(_session, Direction.Right, Direction.Right);
        var cut = Render<ItemCounter>();

        _runner.StepOnce();
        _runner.StepOnce();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("1", cut.Find("[data-testid=item-counter-strawberry]").GetAttribute("data-collected"));
            Assert.Equal("Pizza 1 von 1", cut.Find("[data-testid=item-counter-pizza] .item-counter-text").TextContent.Trim());
        });

        _runner.Reset();

        cut.WaitForAssertion(() => Assert.Equal("0", cut.Find("[data-testid=item-counter-pizza]").GetAttribute("data-collected")));
    }

    [Theory]
    [InlineData("de-CH", "Erdbeere 0 von 1")]
    [InlineData("en", "Strawberry 0 of 1")]
    public async Task Render_SingleStrawberry_UsesTheSingularText(string culture, string text)
    {
        LoadItems(new LevelItem(1, 1, ItemKind.Strawberry));
        var cut = Render<ItemCounter>();

        await Services.GetRequiredService<LanguageService>().SetAsync(culture, Xunit.TestContext.Current.CancellationToken);

        cut.WaitForAssertion(() => Assert.Equal(text, cut.Find("[data-testid=item-counter-strawberry] .item-counter-text").TextContent.Trim()));
    }

    [Fact]
    public async Task Render_PluralStrawberriesInEnglish()
    {
        LoadItems(new LevelItem(1, 1, ItemKind.Strawberry), new LevelItem(2, 1, ItemKind.Strawberry));
        var cut = Render<ItemCounter>();

        await Services.GetRequiredService<LanguageService>().SetAsync("en", Xunit.TestContext.Current.CancellationToken);

        cut.WaitForAssertion(() => Assert.Equal("Strawberries 0 of 2", cut.Find("[data-testid=item-counter-strawberry] .item-counter-text").TextContent.Trim()));
    }
}
