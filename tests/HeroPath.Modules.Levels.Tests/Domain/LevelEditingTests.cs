using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Tests.Domain;

public class LevelEditingTests
{
    [Fact]
    public void NewLevel_Is10x10WithStartBottomLeftAndGoalTopRight()
    {
        var level = LevelEditing.NewLevel("custom-1", "My level");

        Assert.Equal("custom-1", level.Id);
        Assert.Equal("My level", level.Name);
        Assert.Equal((10, 10), (level.Width, level.Height));
        Assert.Equal(new Position(0, 9), level.Start);
        Assert.Equal(new Position(9, 0), level.Goal);
        Assert.Empty(level.Obstacles);
        Assert.Empty(level.Items);
        Assert.False(level.RequireAllItems);
        Assert.Empty(LevelValidator.Validate(level));
    }

    [Fact]
    public void FromTemplate_CopiesWithNewIdAndName_AndLeavesTemplateUntouched()
    {
        var template = Small() with { Id = "easy-1", Name = null, Obstacles = [new(2, 2, ObstacleKind.Tree)] };

        var copy = LevelEditing.FromTemplate(template, "custom-2", "Copy");
        var edited = LevelEditing.Apply(copy, EditorTool.Eraser, new(2, 2)).Level;

        Assert.Equal("custom-2", copy.Id);
        Assert.Equal("Copy", copy.Name);
        Assert.Equal(template.Obstacles, copy.Obstacles);
        Assert.Empty(edited.Obstacles);
        Assert.Equal("easy-1", template.Id);
        Assert.Null(template.Name);
        Assert.Single(template.Obstacles);
    }

    [Theory]
    [InlineData(EditorTool.Tree, ObstacleKind.Tree)]
    [InlineData(EditorTool.Rock, ObstacleKind.Rock)]
    [InlineData(EditorTool.Pond, ObstacleKind.Pond)]
    public void Apply_ObstacleOnEmptyCell_SetsIt(EditorTool tool, ObstacleKind kind)
    {
        var result = LevelEditing.Apply(Small(), tool, new(2, 2));

        Assert.Equal(EditHint.None, result.Hint);
        Assert.Equal([new LevelObstacle(2, 2, kind)], result.Level.Obstacles);
    }

    [Fact]
    public void Apply_ObstacleOnOtherObstacle_Replaces()
    {
        var level = Small() with { Obstacles = [new(2, 2, ObstacleKind.Tree)] };

        var result = LevelEditing.Apply(level, EditorTool.Rock, new(2, 2));

        Assert.Equal([new LevelObstacle(2, 2, ObstacleKind.Rock)], result.Level.Obstacles);
    }

    [Fact]
    public void Apply_ObstacleOnItem_ReplacesTheItem()
    {
        var level = Small() with { Items = [new(2, 2, ItemKind.Pizza)] };

        var result = LevelEditing.Apply(level, EditorTool.Pond, new(2, 2));

        Assert.Empty(result.Level.Items);
        Assert.Equal([new LevelObstacle(2, 2, ObstacleKind.Pond)], result.Level.Obstacles);
    }

    [Fact]
    public void Apply_ItemOnObstacle_ReplacesTheObstacle()
    {
        var level = Small() with { Obstacles = [new(2, 2, ObstacleKind.Tree)] };

        var result = LevelEditing.Apply(level, EditorTool.Strawberry, new(2, 2));

        Assert.Empty(result.Level.Obstacles);
        Assert.Equal([new LevelItem(2, 2, ItemKind.Strawberry)], result.Level.Items);
    }

    [Fact]
    public void Apply_StrawberryOnPizza_Replaces()
    {
        var level = Small() with { Items = [new(2, 2, ItemKind.Pizza)] };

        var result = LevelEditing.Apply(level, EditorTool.Strawberry, new(2, 2));

        Assert.Equal([new LevelItem(2, 2, ItemKind.Strawberry)], result.Level.Items);
    }

    [Theory]
    [InlineData(EditorTool.Tree)]
    [InlineData(EditorTool.Pond)]
    public void Apply_SameObstacleToolOnSameObstacle_Removes(EditorTool tool)
    {
        var first = LevelEditing.Apply(Small(), tool, new(2, 2)).Level;

        var second = LevelEditing.Apply(first, tool, new(2, 2));

        Assert.Empty(second.Level.Obstacles);
        Assert.Equal(EditHint.None, second.Hint);
    }

    [Fact]
    public void Apply_SameItemToolOnSameItem_Removes()
    {
        var first = LevelEditing.Apply(Small(), EditorTool.Pizza, new(2, 2)).Level;

        var second = LevelEditing.Apply(first, EditorTool.Pizza, new(2, 2));

        Assert.Empty(second.Level.Items);
    }

    [Fact]
    public void Apply_StartOnEmptyCell_MovesStart()
    {
        var result = LevelEditing.Apply(Small(), EditorTool.Start, new(2, 2));

        Assert.Equal(new Position(2, 2), result.Level.Start);
        Assert.Equal(Small().Goal, result.Level.Goal);
    }

    [Fact]
    public void Apply_StartOnObstacleOrItem_RemovesThatObject()
    {
        var level = Small() with { Obstacles = [new(2, 2, ObstacleKind.Rock)], Items = [new(3, 3, ItemKind.Pizza)] };

        var onObstacle = LevelEditing.Apply(level, EditorTool.Start, new(2, 2)).Level;
        var onItem = LevelEditing.Apply(level, EditorTool.Start, new(3, 3)).Level;

        Assert.Empty(onObstacle.Obstacles);
        Assert.Single(onObstacle.Items);
        Assert.Empty(onItem.Items);
        Assert.Single(onItem.Obstacles);
    }

    [Fact]
    public void Apply_GoalOnObstacleOrItem_MovesGoalAndRemovesThatObject()
    {
        var level = Small() with { Obstacles = [new(2, 2, ObstacleKind.Rock)], Items = [new(3, 3, ItemKind.Pizza)] };

        var onObstacle = LevelEditing.Apply(level, EditorTool.Goal, new(2, 2)).Level;
        var onItem = LevelEditing.Apply(level, EditorTool.Goal, new(3, 3)).Level;

        Assert.Equal(new Position(2, 2), onObstacle.Goal);
        Assert.Empty(onObstacle.Obstacles);
        Assert.Equal(new Position(3, 3), onItem.Goal);
        Assert.Empty(onItem.Items);
    }

    [Fact]
    public void Apply_StartOnGoalAndGoalOnStart_AreRefusedWithHint()
    {
        var level = Small();

        var startOnGoal = LevelEditing.Apply(level, EditorTool.Start, level.Goal);
        var goalOnStart = LevelEditing.Apply(level, EditorTool.Goal, level.Start);

        Assert.Same(level, startOnGoal.Level);
        Assert.Equal(EditHint.StartOnGoal, startOnGoal.Hint);
        Assert.Same(level, goalOnStart.Level);
        Assert.Equal(EditHint.GoalOnStart, goalOnStart.Hint);
    }

    [Theory]
    [InlineData(EditorTool.Tree)]
    [InlineData(EditorTool.Rock)]
    [InlineData(EditorTool.Pond)]
    public void Apply_ObstacleOnStartOrGoal_IsRefusedWithHint(EditorTool tool)
    {
        var level = Small();

        var onStart = LevelEditing.Apply(level, tool, level.Start);
        var onGoal = LevelEditing.Apply(level, tool, level.Goal);

        Assert.Same(level, onStart.Level);
        Assert.Equal(EditHint.ObstacleOnStart, onStart.Hint);
        Assert.Same(level, onGoal.Level);
        Assert.Equal(EditHint.ObstacleOnGoal, onGoal.Hint);
    }

    [Theory]
    [InlineData(EditorTool.Strawberry)]
    [InlineData(EditorTool.Pizza)]
    public void Apply_ItemOnStart_IsRefusedAndItemOnGoalIsAllowed(EditorTool tool)
    {
        var level = Small();

        var onStart = LevelEditing.Apply(level, tool, level.Start);
        var onGoal = LevelEditing.Apply(level, tool, level.Goal);

        Assert.Same(level, onStart.Level);
        Assert.Equal(EditHint.ItemOnStart, onStart.Hint);
        Assert.Single(onGoal.Level.Items);
        Assert.Equal(EditHint.None, onGoal.Hint);
        Assert.Empty(LevelValidator.Validate(onGoal.Level));
    }

    [Fact]
    public void Apply_Eraser_RemovesObstacleAndItemButNeverStartOrGoal()
    {
        var level = Small() with { Obstacles = [new(2, 2, ObstacleKind.Rock)], Items = [new(3, 3, ItemKind.Pizza)] };

        Assert.Empty(LevelEditing.Apply(level, EditorTool.Eraser, new(2, 2)).Level.Obstacles);
        Assert.Empty(LevelEditing.Apply(level, EditorTool.Eraser, new(3, 3)).Level.Items);
        Assert.Same(level, LevelEditing.Apply(level, EditorTool.Eraser, level.Start).Level);
        Assert.Same(level, LevelEditing.Apply(level, EditorTool.Eraser, level.Goal).Level);
        Assert.Same(level, LevelEditing.Apply(level, EditorTool.Eraser, new(1, 3)).Level);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(5, 0)]
    [InlineData(0, 5)]
    public void Apply_OutsideTheGrid_ChangesNothingAndDoesNotThrow(int x, int y)
    {
        var level = Small();

        foreach (var tool in Enum.GetValues<EditorTool>())
        {
            var result = LevelEditing.Apply(level, tool, new(x, y));

            Assert.Same(level, result.Level);
            Assert.Equal(EditHint.None, result.Hint);
        }
    }

    [Fact]
    public void Apply_LastStrawberryRemoved_SwitchesRequireAllItemsOff()
    {
        var level = Small() with { Items = [new(2, 2, ItemKind.Strawberry), new(3, 3, ItemKind.Pizza)], RequireAllItems = true };

        var byEraser = LevelEditing.Apply(level, EditorTool.Eraser, new(2, 2)).Level;
        var byToggle = LevelEditing.Apply(level, EditorTool.Strawberry, new(2, 2)).Level;
        var byReplace = LevelEditing.Apply(level, EditorTool.Tree, new(2, 2)).Level;
        var byStart = LevelEditing.Apply(level, EditorTool.Start, new(2, 2)).Level;

        Assert.All([byEraser, byToggle, byReplace, byStart], l => Assert.False(l.RequireAllItems));
        Assert.All([byEraser, byToggle, byReplace, byStart], l => Assert.Empty(LevelValidator.Validate(l)));
    }

    [Fact]
    public void Apply_OneOfTwoStrawberriesRemoved_KeepsRequireAllItems()
    {
        var level = Small() with { Items = [new(2, 2, ItemKind.Strawberry), new(3, 3, ItemKind.Strawberry)], RequireAllItems = true };

        Assert.True(LevelEditing.Apply(level, EditorTool.Eraser, new(2, 2)).Level.RequireAllItems);
    }

    [Fact]
    public void Apply_EleventhStrawberry_IsRefusedWithHintAndRemovingStillWorks()
    {
        var level = LevelEditing.NewLevel("custom-x", "x");
        for (var i = 0; i < LevelLimits.MaxStrawberries; i++)
        {
            level = LevelEditing.Apply(level, EditorTool.Strawberry, new(i, 5)).Level;
        }

        var eleventh = LevelEditing.Apply(level, EditorTool.Strawberry, new(0, 4));
        var removed = LevelEditing.Apply(level, EditorTool.Strawberry, new(0, 5));

        Assert.Equal(LevelLimits.MaxStrawberries, level.Items.Count);
        Assert.Same(level, eleventh.Level);
        Assert.Equal(EditHint.TooManyStrawberries, eleventh.Hint);
        Assert.Equal(LevelLimits.MaxStrawberries - 1, removed.Level.Items.Count);
    }

    [Fact]
    public void Apply_EleventhPizza_IsRefusedWithHint()
    {
        var level = LevelEditing.NewLevel("custom-x", "x");
        for (var i = 0; i < LevelLimits.MaxPizzas; i++)
        {
            level = LevelEditing.Apply(level, EditorTool.Pizza, new(i, 5)).Level;
        }

        var eleventh = LevelEditing.Apply(level, EditorTool.Pizza, new(0, 4));

        Assert.Equal(EditHint.TooManyPizzas, eleventh.Hint);
        Assert.Same(level, eleventh.Level);
    }

    [Fact]
    public void Apply_PizzaReplacedByStrawberryAtTenStrawberries_IsRefused()
    {
        var level = LevelEditing.NewLevel("custom-x", "x");
        for (var i = 0; i < LevelLimits.MaxStrawberries; i++)
        {
            level = LevelEditing.Apply(level, EditorTool.Strawberry, new(i, 5)).Level;
        }

        level = LevelEditing.Apply(level, EditorTool.Pizza, new(0, 4)).Level;

        var result = LevelEditing.Apply(level, EditorTool.Strawberry, new(0, 4));

        Assert.Equal(EditHint.TooManyStrawberries, result.Hint);
        Assert.Same(level, result.Level);
    }

    [Fact]
    public void SetRequireAllItems_NeedsAtLeastOneStrawberry()
    {
        var withoutStrawberry = Small() with { Items = [new(2, 2, ItemKind.Pizza)] };
        var withStrawberry = Small() with { Items = [new(2, 2, ItemKind.Strawberry)] };

        Assert.False(LevelEditing.SetRequireAllItems(withoutStrawberry, true).RequireAllItems);
        Assert.True(LevelEditing.SetRequireAllItems(withStrawberry, true).RequireAllItems);
        Assert.False(LevelEditing.SetRequireAllItems(LevelEditing.SetRequireAllItems(withStrawberry, true), false).RequireAllItems);
    }

    [Fact]
    public void Resize_Grow_KeepsEverything()
    {
        var level = Small() with { Obstacles = [new(2, 2, ObstacleKind.Rock)], Items = [new(1, 1, ItemKind.Pizza)] };

        var result = LevelEditing.Resize(level, 8, 7);

        Assert.Equal(0, result.RemovedCount);
        Assert.Equal((8, 7), (result.Level.Width, result.Level.Height));
        Assert.Equal(level.Start, result.Level.Start);
        Assert.Equal(level.Goal, result.Level.Goal);
        Assert.Equal(level.Obstacles, result.Level.Obstacles);
        Assert.Equal(level.Items, result.Level.Items);
    }

    [Fact]
    public void Resize_Shrink_RemovesObjectsOutsideAndCountsThem()
    {
        var level = LevelEditing.NewLevel("custom-x", "x") with
        {
            Start = new(0, 0),
            Goal = new(1, 0),
            Obstacles = [new(8, 8, ObstacleKind.Rock), new(4, 4, ObstacleKind.Tree)],
            Items = [new(9, 1, ItemKind.Strawberry), new(5, 5, ItemKind.Pizza), new(7, 3, ItemKind.Strawberry)],
            RequireAllItems = true,
        };

        var result = LevelEditing.Resize(level, 5, 5);

        Assert.Equal(4, result.RemovedCount);
        Assert.Equal([new LevelObstacle(4, 4, ObstacleKind.Tree)], result.Level.Obstacles);
        Assert.Empty(result.Level.Items);
        Assert.False(result.Level.RequireAllItems);
        Assert.Empty(LevelValidator.Validate(result.Level));
    }

    [Fact]
    public void Resize_StartAndGoalOutside_MoveToNearestBorderCell()
    {
        var level = LevelEditing.NewLevel("custom-x", "x") with { Start = new(2, 9), Goal = new(9, 2) };

        var result = LevelEditing.Resize(level, 6, 5);

        Assert.Equal(new Position(2, 4), result.Level.Start);
        Assert.Equal(new Position(5, 2), result.Level.Goal);
        Assert.Equal(0, result.RemovedCount);
    }

    [Fact]
    public void Resize_To3x3_KeepsCorners()
    {
        var level = LevelEditing.NewLevel("custom-x", "x");

        var result = LevelEditing.Resize(level, 3, 3);

        Assert.Equal(new Position(0, 2), result.Level.Start);
        Assert.NotEqual(result.Level.Start, result.Level.Goal);
        Assert.Equal(new Position(2, 0), result.Level.Goal);
        Assert.Empty(LevelValidator.Validate(result.Level));
    }

    [Fact]
    public void Resize_ClampedCornersCollapse_GoalMovesDeterministically()
    {
        var level = LevelEditing.NewLevel("custom-x", "x") with { Start = new(9, 9), Goal = new(8, 8) };

        var first = LevelEditing.Resize(level, 3, 3);
        var second = LevelEditing.Resize(level, 3, 3);

        Assert.Equal(new Position(2, 2), first.Level.Start);
        Assert.Equal(new Position(2, 1), first.Level.Goal);
        Assert.Equal(first.Level.Goal, second.Level.Goal);
        Assert.Equal(first.Level.Start, second.Level.Start);
    }

    [Fact]
    public void Resize_GoalLandsOnObstacle_GoalMovesToFreeCellAndObstacleStays()
    {
        var level = LevelEditing.NewLevel("custom-x", "x") with
        {
            Start = new(0, 0),
            Goal = new(9, 0),
            Obstacles = [new(2, 0, ObstacleKind.Rock), new(5, 5, ObstacleKind.Tree)],
        };

        var result = LevelEditing.Resize(level, 3, 3);

        Assert.Equal(new Position(2, 1), result.Level.Goal);
        Assert.Equal([new LevelObstacle(2, 0, ObstacleKind.Rock)], result.Level.Obstacles);
        Assert.Equal(1, result.RemovedCount);
        Assert.Empty(LevelValidator.Validate(result.Level));
    }

    [Fact]
    public void Resize_StartLandsOnObstacleOrItem_ThatObjectIsRemovedAndCounted()
    {
        var level = LevelEditing.NewLevel("custom-x", "x") with
        {
            Start = new(9, 9),
            Goal = new(0, 0),
            Obstacles = [new(2, 2, ObstacleKind.Rock)],
            Items = [new(2, 1, ItemKind.Pizza)],
        };

        var result = LevelEditing.Resize(level, 3, 3);

        Assert.Equal(new Position(2, 2), result.Level.Start);
        Assert.Empty(result.Level.Obstacles);
        Assert.Equal([new LevelItem(2, 1, ItemKind.Pizza)], result.Level.Items);
        Assert.Equal(1, result.RemovedCount);
        Assert.Empty(LevelValidator.Validate(result.Level));
    }

    [Fact]
    public void Resize_EveryOtherCellIsObstacle_StillProducesValidLevel()
    {
        var obstacles = new List<LevelObstacle>();
        for (var x = 0; x < 3; x++)
        {
            for (var y = 0; y < 3; y++)
            {
                if ((x, y) != (0, 0))
                {
                    obstacles.Add(new(x, y, ObstacleKind.Rock));
                }
            }
        }

        var level = new LevelDefinition { Id = "x", Name = "x", Width = 5, Height = 5, Start = new(0, 0), Goal = new(4, 4), Obstacles = obstacles };

        var result = LevelEditing.Resize(level, 3, 3);

        Assert.Empty(LevelValidator.Validate(result.Level));
        Assert.Equal(new Position(2, 2), result.Level.Goal);
        Assert.Equal(1, result.RemovedCount);
    }

    [Theory]
    [InlineData(0, 0, 3, 3)]
    [InlineData(2, 99, 3, 20)]
    [InlineData(-5, 21, 3, 20)]
    public void Resize_ClampsTo3To20(int width, int height, int expectedWidth, int expectedHeight)
    {
        var result = LevelEditing.Resize(LevelEditing.NewLevel("custom-x", "x"), width, height);

        Assert.Equal((expectedWidth, expectedHeight), (result.Level.Width, result.Level.Height));
        Assert.Empty(LevelValidator.Validate(result.Level));
    }

    [Fact]
    public void Resize_ShrinkRemovesLastStrawberry_SwitchesRequireAllItemsOff()
    {
        var level = Small() with { Items = [new(4, 4, ItemKind.Strawberry)], RequireAllItems = true };

        var result = LevelEditing.Resize(level, 4, 4);

        Assert.False(result.Level.RequireAllItems);
        Assert.Equal(1, result.RemovedCount);
    }

    [Fact]
    public void ClearObjects_RemovesObstaclesAndItemsAndTheRule()
    {
        var level = Small() with
        {
            Obstacles = [new(2, 2, ObstacleKind.Rock)],
            Items = [new(3, 3, ItemKind.Strawberry)],
            RequireAllItems = true,
        };

        var cleared = LevelEditing.ClearObjects(level);

        Assert.Empty(cleared.Obstacles);
        Assert.Empty(cleared.Items);
        Assert.False(cleared.RequireAllItems);
        Assert.Equal(level.Start, cleared.Start);
        Assert.Equal(level.Goal, cleared.Goal);
    }

    [Fact]
    public void NextDefaultNumber_UsesSmallestFreeNumber()
    {
        string Format(int n) => $"Level {n}";

        Assert.Equal(1, LevelEditing.NextDefaultNumber([], Format));
        Assert.Equal(3, LevelEditing.NextDefaultNumber(["Level 1", "Level 2"], Format));
        Assert.Equal(2, LevelEditing.NextDefaultNumber(["Level 1", "Level 3", null], Format));
        Assert.Equal(1, LevelEditing.NextDefaultNumber(["level 1", "Mein Level"], Format));
    }

    [Theory]
    [InlineData("  Hello  ", "Hello")]
    [InlineData("Hel\u0000lo", "Hello")]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    public void CleanName_CleansAndTrims(string? raw, string expected) =>
        Assert.Equal(expected, LevelEditing.CleanName(raw));

    [Fact]
    public void CleanName_LongText_IsCutToMaxNameLength() =>
        Assert.Equal(LevelLimits.MaxNameLength, LevelEditing.CleanName(new string('a', 200)).Length);

    [Fact]
    public void CleanName_EmojiAtTheCut_NeverSplitsASurrogatePairAndValidatorAccepts()
    {
        var raw = new string('a', LevelLimits.MaxNameLength - 1) + "\U0001F600\U0001F600";

        var name = LevelEditing.CleanName(raw);

        Assert.True(name.Length <= LevelLimits.MaxNameLength);
        Assert.Equal(new string('a', LevelLimits.MaxNameLength - 1), name);
        Assert.Empty(LevelValidator.Validate(Small() with { Name = name }));
    }

    [Fact]
    public void CleanName_ManyEmojis_ValidatorAccepts()
    {
        var name = LevelEditing.CleanName(string.Concat(Enumerable.Repeat("\U0001F600", 40)));

        Assert.NotEmpty(name);
        Assert.Empty(LevelValidator.Validate(Small() with { Name = name }));
    }

    private static LevelDefinition Small() => new()
    {
        Id = "custom-small",
        Name = "Small",
        Width = 5,
        Height = 5,
        Start = new(0, 4),
        Goal = new(4, 0),
    };
}
