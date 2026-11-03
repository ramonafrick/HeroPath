using Microsoft.AspNetCore.Components;

namespace HeroPath.Modules.Game.UI.Components;

public enum ProgramIconKind
{
    Up,
    Down,
    Remove,
    Minus,
    Plus,
    Repeat,
}

public partial class ProgramIcon
{
    [Parameter, EditorRequired]
    public ProgramIconKind Kind { get; set; }
}
