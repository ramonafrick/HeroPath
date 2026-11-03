using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Game.UI.Resources;
using HeroPath.Modules.Game.UI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Game.UI.Components;

public partial class ProgramBlock : DragAwareComponentBase
{
    [Parameter, EditorRequired]
    public required Command Command { get; set; }

    [Parameter, EditorRequired]
    public required IReadOnlyList<int> Path { get; set; }

    [Inject]
    private IStringLocalizer<GameResources> Localizer { get; set; } = default!;

    private bool IsMove => Command is MoveCommand;

    private string Number => string.Join(".", Path);

    private bool IsSelected => Session.SelectedId == Command.Id;

    private bool IsCurrent => Session.CurrentStep?.CommandId == Command.Id;

    private bool IsError => IsCurrent && Session.Status == RunStatus.Failed;

    private bool IsHinted => Session.HintedId == Command.Id;

    private bool IsEditable => Session.IsEditable;

    private RepeatProgress? Progress => Session.CurrentStep?.Repeats.FirstOrDefault(r => r.RepeatId == Command.Id);

    private bool IsActiveRepeat => Command is RepeatCommand && Progress is not null;

    private string Name => Command switch
    {
        MoveCommand move => Localizer[$"Block_{move.Direction}"].Value,
        RepeatCommand repeat => Localizer["Block_Repeat", repeat.Count].Value,
        _ => string.Empty,
    };

    private void StartDrag() => Drag.Start(new DragSource.Existing(Command.Id));
}
