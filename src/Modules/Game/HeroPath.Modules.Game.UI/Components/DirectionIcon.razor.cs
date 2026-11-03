using HeroPath.Modules.Game.Domain;
using Microsoft.AspNetCore.Components;

namespace HeroPath.Modules.Game.UI.Components;

public partial class DirectionIcon
{
    [Parameter, EditorRequired]
    public Direction Direction { get; set; }

    private string Slug => Direction.ToString().ToLowerInvariant();
}
