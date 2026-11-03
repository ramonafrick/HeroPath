using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.UI.Components;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Modules.Levels.Tests.Components;

[Collection("Culture")]
public class GameBoardTests : CultureBunitContext
{
    private static readonly Grid _grid = new LevelDefinition
    {
        Id = "test",
        Width = 5,
        Height = 4,
        Start = new Position(0, 3),
        Goal = new Position(4, 0),
        RequireAllItems = false,
        Items =
        [
            new LevelItem(0, 0, ItemKind.Strawberry),
            new LevelItem(4, 1, ItemKind.Pizza),
            new LevelItem(4, 0, ItemKind.Strawberry),
        ],
        Obstacles =
        [
            new LevelObstacle(1, 1, ObstacleKind.Tree),
            new LevelObstacle(2, 2, ObstacleKind.Rock),
            new LevelObstacle(3, 1, ObstacleKind.Pond),
        ],
    }.ToGrid();

    public GameBoardTests()
    {
        Services.AddSharedUI();
        Services.AddSingleton<IAppStorage, InMemoryAppStorage>();
    }

    [Fact]
    public void Render_CreatesOneCellPerGridPosition()
    {
        var cut = RenderBoard();

        Assert.Equal(_grid.Width * _grid.Height, cut.FindAll("[data-testid^=cell-]").Count);
        Assert.Equal("--cols: 5; --rows: 4;", cut.Find("[data-testid=game-board]").GetAttribute("style"));
    }

    [Theory]
    [InlineData(0, 3, "start")]
    [InlineData(4, 0, "goal")]
    [InlineData(1, 1, "tree")]
    [InlineData(2, 2, "rock")]
    [InlineData(3, 1, "pond")]
    [InlineData(1, 0, "empty")]
    [InlineData(4, 3, "empty")]
    public void Render_CellKinds_AreAtTheRightCells(int x, int y, string kind)
    {
        var cut = RenderBoard();

        Assert.Equal(kind, cut.Find($"[data-testid=cell-{x}-{y}]").GetAttribute("data-kind"));
    }

    [Fact]
    public void Render_Cells_ContainTheirGraphic()
    {
        var cut = RenderBoard();

        Assert.NotNull(cut.Find("[data-testid=cell-1-1] [data-testid=graphic-obstacle-tree]"));
        Assert.NotNull(cut.Find("[data-testid=cell-2-2] [data-testid=graphic-obstacle-rock]"));
        Assert.NotNull(cut.Find("[data-testid=cell-3-1] [data-testid=graphic-obstacle-pond]"));
        Assert.NotNull(cut.Find("[data-testid=cell-0-3] [data-testid=graphic-start-field]"));
        Assert.NotNull(cut.Find("[data-testid=cell-4-0] [data-testid=graphic-goal-castle]"));
        Assert.Empty(cut.FindAll("[data-testid=cell-1-0] svg"));
    }

    [Fact]
    public void Render_Hero_HasPositionAttributesAndStyle()
    {
        var cut = RenderBoard(p => p.Add(b => b.HeroPosition, new Position(2, 3)));

        var hero = cut.Find("[data-testid=hero]");
        Assert.Equal("2", hero.GetAttribute("data-x"));
        Assert.Equal("3", hero.GetAttribute("data-y"));
        Assert.Contains("--x: 2; --y: 3;", hero.GetAttribute("style"));
        Assert.Contains("--hp-step-duration: 500ms", hero.GetAttribute("style"));
    }

    [Fact]
    public void Render_StepDuration_IsAppliedToHero()
    {
        var cut = RenderBoard(p => p.Add(b => b.StepDuration, TimeSpan.FromMilliseconds(200)));

        Assert.Contains("--hp-step-duration: 200ms", cut.Find("[data-testid=hero]").GetAttribute("style"));
    }

    [Fact]
    public void Render_HeroContent_IsRenderedInsideHero()
    {
        var cut = RenderBoard(p => p.Add(b => b.HeroContent, (RenderFragment)(builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "data-testid", "hero-content");
            builder.CloseElement();
        })));

        Assert.NotNull(cut.Find("[data-testid=hero] [data-testid=hero-content]"));
    }

    [Fact]
    public void Render_MarkedCell_IsMarkedOnlyThere()
    {
        var cut = RenderBoard(p => p.Add(b => b.MarkedCell, new Position(1, 1)));

        Assert.Equal("true", cut.Find("[data-testid=cell-1-1]").GetAttribute("data-marked"));
        Assert.Single(cut.FindAll("[data-marked]"));
    }

    [Fact]
    public void Render_WithoutMarkedCell_MarksNothing()
    {
        var cut = RenderBoard();

        Assert.Empty(cut.FindAll("[data-marked]"));
    }

    [Fact]
    public void Render_Board_HasAccessibleLabel()
    {
        var cut = RenderBoard();

        Assert.Equal("Spielfeld", cut.Find("[data-testid=game-board]").GetAttribute("aria-label"));
    }

    [Fact]
    public void Render_Preview_HasNoHeroNoCellTestIdsAndIsHiddenFromAssistiveTechnology()
    {
        var cut = RenderBoard(p => p.Add(b => b.Preview, true).Add(b => b.HeroContent, builder => builder.AddContent(0, "hero-content")));

        var board = cut.Find("[data-testid=board-preview]");
        Assert.Equal("true", board.GetAttribute("aria-hidden"));
        Assert.Null(board.GetAttribute("role"));
        Assert.Null(board.GetAttribute("aria-label"));
        Assert.Empty(cut.FindAll("[data-testid=hero]"));
        Assert.Empty(cut.FindAll("[data-testid^=cell-]"));
        Assert.Empty(cut.FindAll("[data-testid=game-board]"));
        Assert.DoesNotContain("hero-content", cut.Markup);
        Assert.Equal(_grid.Width * _grid.Height, cut.FindAll(".cell").Count);
        Assert.Equal("tree", cut.FindAll(".cell")[1 * _grid.Width + 1].GetAttribute("data-kind"));
    }

    [Fact]
    public void Render_NotPreview_KeepsBoardHeroAndLabel()
    {
        var cut = RenderBoard();

        Assert.Null(cut.Find("[data-testid=game-board]").GetAttribute("aria-hidden"));
        Assert.Equal("group", cut.Find("[data-testid=game-board]").GetAttribute("role"));
        Assert.NotNull(cut.Find("[data-testid=hero]"));
    }

    [Fact]
    public void Render_Items_AreDrawnInTheirCellsWithKindAndItemAttributes()
    {
        var cut = RenderBoard();

        var strawberry = cut.Find("[data-testid=cell-0-0]");
        Assert.Equal("strawberry", strawberry.GetAttribute("data-kind"));
        Assert.Equal("strawberry", strawberry.GetAttribute("data-item"));
        Assert.NotNull(cut.Find("[data-testid=cell-0-0] [data-testid=item-0-0] [data-testid=graphic-item-strawberry]"));
        Assert.Equal("pizza", cut.Find("[data-testid=cell-4-1]").GetAttribute("data-kind"));
        Assert.NotNull(cut.Find("[data-testid=cell-4-1] [data-testid=graphic-item-pizza]"));
        Assert.Equal(3, cut.FindAll("[data-item]").Count);
        Assert.Null(cut.Find("[data-testid=cell-1-1]").GetAttribute("data-item"));
    }

    [Fact]
    public void Render_ItemOnTheGoal_KeepsTheGoalKindAndAddsTheItemAttribute()
    {
        var cut = RenderBoard();

        var goal = cut.Find("[data-testid=cell-4-0]");
        Assert.Equal("goal", goal.GetAttribute("data-kind"));
        Assert.Equal("strawberry", goal.GetAttribute("data-item"));
        Assert.NotNull(goal.QuerySelector("[data-testid=graphic-goal-castle]"));
        Assert.NotNull(goal.QuerySelector("[data-testid=graphic-item-strawberry]"));
    }

    [Fact]
    public void Render_WithoutCollectedItems_MarksNoItemAsCollected()
    {
        var cut = RenderBoard();

        Assert.Empty(cut.FindAll("[data-collected]"));
    }

    [Fact]
    public void Render_CollectedItems_StayInTheDomMarkedAsCollected()
    {
        var cut = RenderBoard(p => p.Add(b => b.CollectedItems, new HashSet<Position> { new(4, 1) }));

        Assert.Equal("true", cut.Find("[data-testid=item-4-1]").GetAttribute("data-collected"));
        Assert.Single(cut.FindAll("[data-collected]"));
        Assert.NotNull(cut.Find("[data-testid=item-4-1] [data-testid=graphic-item-pizza]"));
    }

    [Fact]
    public void Render_CollectedItemsEmptiedAgain_ShowsTheItemsAsUncollected()
    {
        var cut = RenderBoard(p => p.Add(b => b.CollectedItems, new HashSet<Position> { new(0, 0) }));
        Assert.Single(cut.FindAll("[data-collected]"));

        cut.Render(p => p.Add(b => b.Grid, _grid).Add(b => b.CollectedItems, new HashSet<Position>()));

        Assert.Empty(cut.FindAll("[data-collected]"));
        Assert.Equal(3, cut.FindAll("[data-testid^=item-]").Count);
    }

    [Fact]
    public void Render_Preview_ShowsItemsWithoutCollectedStateOrTestIds()
    {
        var cut = RenderBoard(p => p
            .Add(b => b.Preview, true)
            .Add(b => b.CollectedItems, new HashSet<Position> { new(0, 0) }));

        Assert.Equal(3, cut.FindAll(".item").Count);
        Assert.Empty(cut.FindAll("[data-collected]"));
        Assert.Empty(cut.FindAll("[data-testid^=item-]"));
        Assert.Equal(3, cut.FindAll("[data-item]").Count);
    }

    [Fact]
    public void Render_WithoutCellClicked_HasNoButtonsAndKeepsTheHero()
    {
        var cut = RenderBoard();

        Assert.Empty(cut.FindAll("button"));
        Assert.Empty(cut.FindAll("[data-testid^=edit-cell-]"));
        Assert.NotNull(cut.Find("[data-testid=hero]"));
        Assert.Null(cut.Find("[data-testid=game-board]").GetAttribute("data-editable"));
    }

    [Fact]
    public void Render_WithCellClicked_EveryCellIsAButtonWithLabelAndNoHero()
    {
        var cut = RenderBoard(p => p
            .Add(b => b.CellClicked, EventCallback.Factory.Create<Position>(this, _ => { }))
            .Add(b => b.CellLabel, position => $"label {position.X},{position.Y}"));

        var cells = cut.FindAll("[data-testid^=edit-cell-]");
        Assert.Equal(_grid.Width * _grid.Height, cells.Count);
        Assert.All(cells, cell =>
        {
            Assert.Equal("button", cell.TagName.ToLowerInvariant());
            Assert.Equal("button", cell.GetAttribute("type"));
        });
        Assert.Equal("label 2,1", cut.Find("[data-testid=edit-cell-2-1]").GetAttribute("aria-label"));
        Assert.Empty(cut.FindAll("[data-testid=hero]"));
        Assert.Empty(cut.FindAll("[data-testid^=cell-]"));
        Assert.Equal("true", cut.Find("[data-testid=game-board]").GetAttribute("data-editable"));
    }

    [Fact]
    public void Render_WithCellClicked_KeepsKindsItemsAndGraphics()
    {
        var cut = RenderBoard(p => p.Add(b => b.CellClicked, EventCallback.Factory.Create<Position>(this, _ => { })));

        Assert.Equal("tree", cut.Find("[data-testid=edit-cell-1-1]").GetAttribute("data-kind"));
        Assert.Equal("start", cut.Find("[data-testid=edit-cell-0-3]").GetAttribute("data-kind"));
        Assert.Equal("strawberry", cut.Find("[data-testid=edit-cell-4-0]").GetAttribute("data-item"));
        Assert.Equal("goal", cut.Find("[data-testid=edit-cell-4-0]").GetAttribute("data-kind"));
        Assert.NotNull(cut.Find("[data-testid=edit-cell-1-1] [data-testid=graphic-obstacle-tree]"));
    }

    [Fact]
    public void Click_OnACell_ReportsItsPosition()
    {
        Position? clicked = null;
        var cut = RenderBoard(p => p.Add(b => b.CellClicked, EventCallback.Factory.Create<Position>(this, position => clicked = position)));

        cut.Find("[data-testid=edit-cell-3-2]").Click();

        Assert.Equal(new Position(3, 2), clicked);
    }

    [Fact]
    public void Render_PreviewWithCellClicked_StaysAPlainPreview()
    {
        var cut = RenderBoard(p => p
            .Add(b => b.Preview, true)
            .Add(b => b.CellClicked, EventCallback.Factory.Create<Position>(this, _ => { })));

        Assert.Empty(cut.FindAll("button"));
    }

    private IRenderedComponent<GameBoard> RenderBoard(Action<ComponentParameterCollectionBuilder<GameBoard>>? configure = null) =>
        Render<GameBoard>(p =>
        {
            p.Add(b => b.Grid, _grid);
            configure?.Invoke(p);
        });
}
