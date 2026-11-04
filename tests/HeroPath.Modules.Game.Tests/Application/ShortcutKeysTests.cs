using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;

namespace HeroPath.Modules.Game.Tests.Application;

public class ShortcutKeysTests
{
    [Theory]
    [InlineData("ArrowUp", ShortcutAction.InsertUp)]
    [InlineData("ArrowDown", ShortcutAction.InsertDown)]
    [InlineData("ArrowLeft", ShortcutAction.InsertLeft)]
    [InlineData("ArrowRight", ShortcutAction.InsertRight)]
    [InlineData("Delete", ShortcutAction.DeleteSelected)]
    [InlineData("Backspace", ShortcutAction.DeleteSelected)]
    [InlineData("Escape", ShortcutAction.ClearSelection)]
    [InlineData(" ", ShortcutAction.Play)]
    [InlineData("d", ShortcutAction.Debug)]
    [InlineData("r", ShortcutAction.Reset)]
    public void Resolve_Key_GivesAction(string key, ShortcutAction expected) =>
        Assert.Equal(expected, ShortcutKeys.Resolve(key, shift: false));

    [Theory]
    [InlineData("D", ShortcutAction.Debug)]
    [InlineData("R", ShortcutAction.Reset)]
    public void Resolve_UpperCaseLetter_GivesSameAction(string key, ShortcutAction expected)
    {
        Assert.Equal(expected, ShortcutKeys.Resolve(key, shift: false));
        Assert.Equal(expected, ShortcutKeys.Resolve(key, shift: true));
    }

    [Fact]
    public void Resolve_ShiftUpAndDown_MoveTheSelectedBlock()
    {
        Assert.Equal(ShortcutAction.MoveSelectedUp, ShortcutKeys.Resolve("ArrowUp", shift: true));
        Assert.Equal(ShortcutAction.MoveSelectedDown, ShortcutKeys.Resolve("ArrowDown", shift: true));
    }

    [Theory]
    [InlineData("ArrowLeft")]
    [InlineData("ArrowRight")]
    public void Resolve_ShiftLeftOrRight_HasNoAction(string key) => Assert.Null(ShortcutKeys.Resolve(key, shift: true));

    [Theory]
    [InlineData("Enter")]
    [InlineData("Tab")]
    [InlineData("a")]
    [InlineData("1")]
    [InlineData("Spacebar")]
    [InlineData("F5")]
    [InlineData("")]
    [InlineData("arrowup")]
    [InlineData(null)]
    public void Resolve_UnknownKey_HasNoAction(string? key)
    {
        Assert.Null(ShortcutKeys.Resolve(key, shift: false));
        Assert.Null(ShortcutKeys.Resolve(key, shift: true));
    }

    [Theory]
    [InlineData(ShortcutAction.InsertUp, Direction.Up)]
    [InlineData(ShortcutAction.InsertDown, Direction.Down)]
    [InlineData(ShortcutAction.InsertLeft, Direction.Left)]
    [InlineData(ShortcutAction.InsertRight, Direction.Right)]
    public void InsertDirection_InsertAction_GivesDirection(ShortcutAction action, Direction expected) =>
        Assert.Equal(expected, ShortcutKeys.InsertDirection(action));

    [Theory]
    [InlineData(ShortcutAction.MoveSelectedUp)]
    [InlineData(ShortcutAction.MoveSelectedDown)]
    [InlineData(ShortcutAction.DeleteSelected)]
    [InlineData(ShortcutAction.ClearSelection)]
    [InlineData(ShortcutAction.Play)]
    [InlineData(ShortcutAction.Debug)]
    [InlineData(ShortcutAction.Reset)]
    public void InsertDirection_OtherActions_GiveNull(ShortcutAction action) => Assert.Null(ShortcutKeys.InsertDirection(action));
}
