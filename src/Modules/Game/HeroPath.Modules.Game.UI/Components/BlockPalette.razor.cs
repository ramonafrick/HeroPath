using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Game.UI.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Game.UI.Components;

public partial class BlockPalette : DragAwareComponentBase
{
    private static readonly Direction[] _directions = [Direction.Up, Direction.Down, Direction.Left, Direction.Right];

    [Inject]
    private IStringLocalizer<GameResources> Localizer { get; set; } = default!;

    private bool CanAdd => Session.CanAdd && Session.IsEditable;

    private bool CanAddRepeat => Session.IsEditable && Session.CanAddRepeat;
}
