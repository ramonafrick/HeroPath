using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.UI.Components;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Modules.Levels.Tests.Components;

public class GameBoardTests : BunitContext
{
    private static readonly Grid _grid = new LevelDefinition
    {
        Id = "test",
        Width = 5,
        Height = 4,
        Start = new Position(0, 3),
        Goal = new Position(4, 0),
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
    [InlineData(0, 0, "empty")]
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
        Assert.Empty(cut.FindAll("[data-testid=cell-0-0] svg"));
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

        Assert.False(string.IsNullOrWhiteSpace(cut.Find("[data-testid=game-board]").GetAttribute("aria-label")));
    }

    private IRenderedComponent<GameBoard> RenderBoard(Action<ComponentParameterCollectionBuilder<GameBoard>>? configure = null) =>
        Render<GameBoard>(p =>
        {
            p.Add(b => b.Grid, _grid);
            configure?.Invoke(p);
        });
}
