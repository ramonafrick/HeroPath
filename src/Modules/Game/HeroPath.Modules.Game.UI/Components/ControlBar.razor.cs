using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.UI.Resources;
using HeroPath.Shared.UI.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Game.UI.Components;

public partial class ControlBar : IDisposable
{
    [Inject]
    private GameSession Session { get; set; } = default!;

    [Inject]
    private ExecutionRunner Runner { get; set; } = default!;

    [Inject]
    private LanguageService Language { get; set; } = default!;

    [Inject]
    private IStringLocalizer<GameResources> Localizer { get; set; } = default!;

    private bool CanStart => Session.Commands.Count > 0 && Session.Status is RunStatus.Ready or RunStatus.Paused;

    private string StateName => Session.Status.ToString().ToLowerInvariant();

    private string? ErrorName => Session.Status == RunStatus.Failed ? Session.CurrentStep?.Error?.ToString() : null;

    private string TempoName => Localizer[$"Tempo_{Runner.Tempo}"];

    protected override void OnInitialized()
    {
        Session.Changed += HandleChanged;
        Language.Changed += HandleChanged;
    }

    public void Dispose()
    {
        Session.Changed -= HandleChanged;
        Language.Changed -= HandleChanged;
    }

    private Task PlayAsync() => Runner.PlayAsync();

    private void ChangeTempo(ChangeEventArgs args)
    {
        if (Enum.TryParse<Tempo>(args.Value?.ToString(), out var tempo) && Enum.IsDefined(tempo))
        {
            Runner.Tempo = tempo;
        }
    }

    private void HandleChanged() => _ = InvokeAsync(StateHasChanged);
}
