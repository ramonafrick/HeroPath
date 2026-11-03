using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.UI.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Game.UI.Components;

public partial class ControlBar : SessionComponentBase
{
    [Inject]
    private ExecutionRunner Runner { get; set; } = default!;

    [Inject]
    private IStringLocalizer<GameResources> Localizer { get; set; } = default!;

    private string? ErrorName => Session.Status == RunStatus.Failed ? Session.CurrentStep?.Error?.ToString() : null;

    private string TempoName => Localizer[$"Tempo_{Session.Tempo}"];

    private void ChangeTempo(ChangeEventArgs args)
    {
        if (Enum.TryParse<Tempo>(args.Value?.ToString(), out var tempo) && Enum.IsDefined(tempo))
        {
            Session.Tempo = tempo;
        }
    }
}
