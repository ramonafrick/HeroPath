using HeroPath.Modules.Game.Domain;
using Microsoft.AspNetCore.Components;

namespace HeroPath.Modules.Game.UI.Components;

/// <summary>
/// The items of one block list (the root program or the body of a repeat block): a drop zone before every block, the block,
/// and a drop zone at the end. Renders no wrapper element, the caller supplies the <c>&lt;ol&gt;</c>.
/// </summary>
public partial class BlockList : ComponentBase
{
    private const string EndZoneKey = "zone-end";

    /// <summary>Id of the repeat block whose body this is, or <c>null</c> for the top level.</summary>
    [Parameter]
    public Guid? ParentId { get; set; }

    /// <summary>Number of the repeat block (such as <c>3.2</c>), or <c>root</c>.</summary>
    [Parameter, EditorRequired]
    public required string ParentNumber { get; set; }

    [Parameter, EditorRequired]
    public required IReadOnlyList<Command> Commands { get; set; }

    /// <summary>Hierarchical number of the parent block; empty at the top level.</summary>
    [Parameter]
    public IReadOnlyList<int> PathPrefix { get; set; } = [];

    private static string ZoneBeforeKey(Command command) => $"zone-before-{command.Id}";

    private static object BlockKey(Command command) => command.Id;
}
